using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class StartupEngine
{
    private static readonly string UserStartupPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Windows\Start Menu\Programs\Startup"
    );

    private static readonly string CommonStartupPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        @"Microsoft\Windows\Start Menu\Programs\Startup"
    );

    // Startup Apps Management
    public List<StartupEntry> GetStartupEntries()
    {
        var entries = new List<StartupEntry>();

        // 1. Registry Run HKCU
        ReadRegistryStartup(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupSource.RegistryRunHKCU, true, entries);
        ReadRegistryStartup(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunDisabled", StartupSource.RegistryRunHKCU, false, entries);

        // 2. Registry Run HKLM
        ReadRegistryStartup(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupSource.RegistryRunHKLM, true, entries);
        ReadRegistryStartup(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunDisabled", StartupSource.RegistryRunHKLM, false, entries);

        // 3. Registry Run Wow64
        ReadRegistryStartup(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run-Wow", StartupSource.RegistryRunWow64, true, entries); // fallback mock or Wow6432Node
        ReadRegistryStartup(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", StartupSource.RegistryRunWow64, true, entries);
        ReadRegistryStartup(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunDisabled", StartupSource.RegistryRunWow64, false, entries);

        // 4. Startup folders
        ReadFolderStartup(UserStartupPath, StartupSource.StartupFolderUser, entries);
        ReadFolderStartup(CommonStartupPath, StartupSource.StartupFolderCommon, entries);

        return entries;
    }

    private void ReadRegistryStartup(RegistryKey hive, string subkey, StartupSource source, bool isEnabled, List<StartupEntry> list)
    {
        try
        {
            using var key = hive.OpenSubKey(subkey, false);
            if (key == null) return;

            foreach (var valueName in key.GetValueNames())
            {
                var val = key.GetValue(valueName)?.ToString() ?? "";
                list.Add(new StartupEntry
                {
                    Name = valueName,
                    Command = val,
                    Path = CleanCommandPath(val),
                    Source = source,
                    IsEnabled = isEnabled
                });
            }
        }
        catch { }
    }

    private void ReadFolderStartup(string directoryPath, StartupSource source, List<StartupEntry> list)
    {
        try
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath))
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".lnk" || ext == ".disabled" || ext == ".exe")
                {
                    bool isEnabled = ext != ".disabled";
                    list.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Path = file,
                        Source = source,
                        IsEnabled = isEnabled
                    });
                }
            }
        }
        catch { }
    }

    private string CleanCommandPath(string cmd)
    {
        if (string.IsNullOrEmpty(cmd)) return "";
        cmd = cmd.Trim();
        if (cmd.StartsWith("\""))
        {
            int nextQuote = cmd.IndexOf("\"", 1);
            if (nextQuote > 1) return cmd.Substring(1, nextQuote - 1);
        }
        int space = cmd.IndexOf(" ");
        if (space > 0) return cmd.Substring(0, space);
        return cmd;
    }

    public bool ToggleStartupEntry(StartupEntry entry, bool enable)
    {
        if (entry.IsEnabled == enable) return true;

        try
        {
            if (entry.Source == StartupSource.StartupFolderUser || entry.Source == StartupSource.StartupFolderCommon)
            {
                // Toggle folder item by renaming extension .lnk <-> .disabled
                string currentPath = entry.Command;
                string newPath;

                if (enable)
                {
                    newPath = currentPath.Replace(".disabled", ".lnk");
                }
                else
                {
                    newPath = currentPath.Replace(".lnk", ".disabled");
                    if (!newPath.EndsWith(".disabled")) newPath += ".disabled";
                }

                if (File.Exists(currentPath))
                {
                    File.Move(currentPath, newPath);
                    entry.Command = newPath;
                    entry.Path = newPath;
                    entry.IsEnabled = enable;
                    Database.DbManager.LogAction($"Toggled Startup File {entry.Name} to {(enable ? "Enabled" : "Disabled")}", "Startup Manager", "Success");
                    return true;
                }
            }
            else
            {
                // Registry toggling: Move between "Run" and "RunDisabled"
                RegistryKey hive = entry.Source switch
                {
                    StartupSource.RegistryRunHKLM => Registry.LocalMachine,
                    StartupSource.RegistryRunWow64 => Registry.LocalMachine,
                    _ => Registry.CurrentUser
                };

                string originalKeyPath = entry.Source switch
                {
                    StartupSource.RegistryRunWow64 => @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                    _ => @"Software\Microsoft\Windows\CurrentVersion\Run"
                };

                string targetKeyPath = originalKeyPath + (enable ? "" : "Disabled");
                string sourceKeyPath = originalKeyPath + (enable ? "Disabled" : "");

                using (var srcKey = hive.OpenSubKey(sourceKeyPath, true))
                using (var dstKey = hive.CreateSubKey(targetKeyPath, true))
                {
                    if (srcKey != null && dstKey != null)
                    {
                        var value = srcKey.GetValue(entry.Name);
                        if (value != null)
                        {
                            dstKey.SetValue(entry.Name, value, srcKey.GetValueKind(entry.Name));
                            srcKey.DeleteValue(entry.Name);
                            entry.IsEnabled = enable;
                            Database.DbManager.LogAction($"Toggled Startup Registry {entry.Name} to {(enable ? "Enabled" : "Disabled")}", "Startup Manager", "Success");
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Toggle Startup {entry.Name} failed: {ex.Message}", "Startup Manager", "Failed");
        }
        return false;
    }

    public bool RemoveStartupEntry(StartupEntry entry)
    {
        try
        {
            if (entry.Source == StartupSource.StartupFolderUser || entry.Source == StartupSource.StartupFolderCommon)
            {
                if (File.Exists(entry.Command))
                {
                    File.Delete(entry.Command);
                    Database.DbManager.LogAction($"Deleted Startup File {entry.Name}", "Startup Manager", "Success");
                    return true;
                }
            }
            else
            {
                RegistryKey hive = entry.Source switch
                {
                    StartupSource.RegistryRunHKLM => Registry.LocalMachine,
                    StartupSource.RegistryRunWow64 => Registry.LocalMachine,
                    _ => Registry.CurrentUser
                };

                string runKeyPath = entry.Source switch
                {
                    StartupSource.RegistryRunWow64 => @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                    _ => @"Software\Microsoft\Windows\CurrentVersion\Run"
                };

                string activeKey = runKeyPath + (entry.IsEnabled ? "" : "Disabled");

                using var key = hive.OpenSubKey(activeKey, true);
                if (key != null)
                {
                    key.DeleteValue(entry.Name, false);
                    Database.DbManager.LogAction($"Removed Startup Registry {entry.Name}", "Startup Manager", "Success");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Remove Startup {entry.Name} failed: {ex.Message}", "Startup Manager", "Failed");
        }
        return false;
    }

    // Startup Services
    public List<ServiceEntry> GetServices()
    {
        var list = new List<ServiceEntry>();
        try
        {
            foreach (var svc in ServiceController.GetServices())
            {
                string startupType = "Unknown";
                try
                {
                    // Query startup type using registry or ServiceController
                    startupType = svc.StartType.ToString();
                }
                catch { }

                list.Add(new ServiceEntry
                {
                    Name = svc.ServiceName,
                    DisplayName = svc.DisplayName,
                    Status = svc.Status.ToString(),
                    StartupType = startupType,
                    CanStop = svc.CanStop
                });
            }
        }
        catch { }
        return list.OrderBy(x => x.DisplayName).ToList();
    }

    public bool SetServiceStartupType(string serviceName, ServiceStartMode startMode)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true);
            if (key != null)
            {
                int val = startMode switch
                {
                    ServiceStartMode.Automatic => 2,
                    ServiceStartMode.Manual => 3,
                    ServiceStartMode.Disabled => 4,
                    _ => 3
                };
                key.SetValue("Start", val, RegistryValueKind.DWord);
                Database.DbManager.LogAction($"Changed Service {serviceName} startup type to {startMode}", "Service Manager", "Success");
                return true;
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Change Service {serviceName} startup failed: {ex.Message}", "Service Manager", "Failed");
        }
        return false;
    }

    public bool ControlService(string serviceName, string action)
    {
        try
        {
            using var svc = new ServiceController(serviceName);
            if (action == "Start")
            {
                svc.Start();
                svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
            }
            else if (action == "Stop")
            {
                svc.Stop();
                svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
            }
            else if (action == "Restart")
            {
                if (svc.Status == ServiceControllerStatus.Running)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                }
                svc.Start();
                svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
            }
            Database.DbManager.LogAction($"{action} Service {serviceName}", "Service Manager", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"{action} Service {serviceName} failed: {ex.Message}", "Service Manager", "Failed");
            return false;
        }
    }

    // Scheduled Tasks
    public List<ScheduledTaskEntry> GetScheduledTasks()
    {
        var list = new List<ScheduledTaskEntry>();
        try
        {
            using var ts = new TaskService();
            EnumTasks(ts.RootFolder, list);
        }
        catch { }
        return list;
    }

    private void EnumTasks(TaskFolder folder, List<ScheduledTaskEntry> list)
    {
        try
        {
            foreach (var task in folder.Tasks)
            {
                string action = "";
                try
                {
                    action = task.Definition.Actions.FirstOrDefault()?.ToString() ?? "";
                }
                catch { }

                list.Add(new ScheduledTaskEntry
                {
                    Name = task.Name,
                    Path = task.Path,
                    Action = action,
                    Status = task.State.ToString(),
                    IsEnabled = task.Enabled,
                    LastRunTime = task.LastRunTime == DateTime.MinValue ? null : task.LastRunTime,
                    NextRunTime = task.NextRunTime == DateTime.MinValue ? null : task.NextRunTime
                });
            }

            foreach (var sub in folder.SubFolders)
            {
                EnumTasks(sub, list);
            }
        }
        catch { }
    }

    public bool ToggleScheduledTask(string path, bool enable)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(path);
            if (task != null)
            {
                task.Enabled = enable;
                Database.DbManager.LogAction($"Toggled Task {task.Name} to {(enable ? "Enabled" : "Disabled")}", "Startup Manager", "Success");
                return true;
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Toggle Task {path} failed: {ex.Message}", "Startup Manager", "Failed");
        }
        return false;
    }

    public bool DeleteScheduledTask(string path)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(path);
            if (task != null)
            {
                var folder = task.Folder;
                folder.DeleteTask(task.Name);
                Database.DbManager.LogAction($"Deleted Task {path}", "Startup Manager", "Success");
                return true;
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Delete Task {path} failed: {ex.Message}", "Startup Manager", "Failed");
        }
        return false;
    }
}

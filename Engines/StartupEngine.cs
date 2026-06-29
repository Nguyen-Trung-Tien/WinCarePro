using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Xml;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using WinCarePro.Models;
using WinCarePro.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

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

    private readonly IconCacheService _iconCache;
    private readonly ServiceSafetyService _safety;

    private class MetadataCacheItem
    {
        public string Publisher { get; set; } = "Unknown";
        public string Company { get; set; } = "Unknown";
        public string IconPath { get; set; } = "";
        public string StartupImpact { get; set; } = "Medium";
        public bool IsMicrosoft { get; set; }
        public bool IsSystemItem { get; set; }
        public int EstimatedLaunchTimeMs { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<string, MetadataCacheItem> _metadataCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public StartupEngine()
    {
        try
        {
            _iconCache = App.Services?.GetService<IconCacheService>() ?? new IconCacheService();
            _safety = App.Services?.GetService<ServiceSafetyService>() ?? new ServiceSafetyService();
        }
        catch
        {
            _iconCache = new IconCacheService();
            _safety = new ServiceSafetyService();
        }
    }

    public StartupEngine(IconCacheService iconCache, ServiceSafetyService safety)
    {
        _iconCache = iconCache ?? new IconCacheService();
        _safety = safety ?? new ServiceSafetyService();
    }

    private MetadataCacheItem GetOrAddMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new MetadataCacheItem
            {
                Publisher = "Unknown",
                Company = "Unknown",
                IconPath = "",
                StartupImpact = "Low",
                IsMicrosoft = false,
                IsSystemItem = false,
                EstimatedLaunchTimeMs = 0
            };
        }

        if (_metadataCache.TryGetValue(filePath, out var cached) && (DateTime.UtcNow - cached.CachedAt) < CacheTtl)
        {
            return cached;
        }

        var item = new MetadataCacheItem();
        try
        {
            var fileInfo = FileVersionInfo.GetVersionInfo(filePath);
            item.Publisher = fileInfo.CompanyName ?? "Unknown";
            item.Company = item.Publisher;
            item.IsMicrosoft = item.Publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);

            string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLowerInvariant();
            item.IsSystemItem = filePath.ToLowerInvariant().StartsWith(systemRoot);

            // Synchronous call to resolve icon
            item.IconPath = _iconCache.GetIconForExecutable(filePath);

            long fileSize = 0;
            try
            {
                fileSize = new FileInfo(filePath).Length;
            }
            catch { }

            int baseTime = 50;
            if (fileSize > 50 * 1024 * 1024) baseTime += 800;
            else if (fileSize > 10 * 1024 * 1024) baseTime += 300;
            else if (fileSize > 2 * 1024 * 1024) baseTime += 100;

            if (item.IsMicrosoft) baseTime = (int)(baseTime * 0.7);

            item.EstimatedLaunchTimeMs = baseTime;

            if (baseTime < 150) item.StartupImpact = "Low";
            else if (baseTime < 500) item.StartupImpact = "Medium";
            else if (baseTime < 2000) item.StartupImpact = "High";
            else item.StartupImpact = "Critical";
        }
        catch
        {
            item.Publisher = "Unknown";
            item.Company = "Unknown";
            item.IconPath = "";
            item.StartupImpact = "Medium";
            item.IsMicrosoft = false;
            item.IsSystemItem = false;
            item.EstimatedLaunchTimeMs = 150;
        }

        item.CachedAt = DateTime.UtcNow;
        _metadataCache[filePath] = item;
        return item;
    }

    private string GetServiceImagePath(string serviceName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
            if (key != null)
            {
                string rawPath = key.GetValue("ImagePath")?.ToString() ?? "";
                if (string.IsNullOrEmpty(rawPath)) return "";

                string expanded = Environment.ExpandEnvironmentVariables(rawPath);
                if (expanded.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
                {
                    string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    expanded = windir + expanded.Substring(11);
                }

                if (expanded.StartsWith(@"system32\", StringComparison.OrdinalIgnoreCase) || 
                    expanded.StartsWith(@"syswow64\", StringComparison.OrdinalIgnoreCase))
                {
                    string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    expanded = Path.Combine(windir, expanded);
                }

                expanded = expanded.Trim();
                if (expanded.StartsWith("\""))
                {
                    int endQuote = expanded.IndexOf("\"", 1);
                    if (endQuote > 1) return expanded.Substring(1, endQuote - 1);
                }

                int firstSpace = expanded.IndexOf(" ");
                if (firstSpace > 0)
                {
                    string part = expanded.Substring(0, firstSpace);
                    if (File.Exists(part)) return part;
                    if (File.Exists(expanded)) return expanded;
                    return part;
                }

                return expanded;
            }
        }
        catch { }
        return "";
    }

    public double GetLastBootTimeSeconds()
    {
        try
        {
            string query = "*[System/EventID=100]";
            var logQuery = new EventLogQuery(
                "Microsoft-Windows-Diagnostics-Performance/Operational", 
                PathType.LogName, 
                query)
            {
                ReverseDirection = true
            };
            using var reader = new EventLogReader(logQuery);
            var eventInstance = reader.ReadEvent();
            if (eventInstance != null)
            {
                string xml = eventInstance.ToXml();
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/win/2004/08/events/event");
                var node = doc.SelectSingleNode("//ns:EventData/ns:Data[@Name='BootTime']", nsmgr);
                if (node != null && double.TryParse(node.InnerText, out double bootMs))
                {
                    return bootMs / 1000.0;
                }
            }
        }
        catch { }
        return -1;
    }

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
                string cleanPath = CleanCommandPath(val);
                var meta = GetOrAddMetadata(cleanPath);

                list.Add(new StartupEntry
                {
                    Name = valueName,
                    Command = val,
                    Path = cleanPath,
                    Source = source,
                    IsEnabled = isEnabled,
                    
                    // Rich Metadata
                    IconPath = meta.IconPath,
                    Publisher = meta.Publisher,
                    StartupImpact = meta.StartupImpact,
                    IsMicrosoft = meta.IsMicrosoft,
                    IsSystemItem = meta.IsSystemItem,
                    EstimatedLaunchTimeMs = meta.EstimatedLaunchTimeMs,
                    IsRecommendedDisable = !meta.IsMicrosoft && (meta.StartupImpact == "High" || meta.StartupImpact == "Critical")
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
                    var meta = GetOrAddMetadata(file);

                    list.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Path = file,
                        Source = source,
                        IsEnabled = isEnabled,
                        
                        // Rich Metadata
                        IconPath = meta.IconPath,
                        Publisher = meta.Publisher,
                        StartupImpact = meta.StartupImpact,
                        IsMicrosoft = meta.IsMicrosoft,
                        IsSystemItem = meta.IsSystemItem,
                        EstimatedLaunchTimeMs = meta.EstimatedLaunchTimeMs,
                        IsRecommendedDisable = !meta.IsMicrosoft && (meta.StartupImpact == "High" || meta.StartupImpact == "Critical")
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
                    startupType = svc.StartType.ToString();
                }
                catch { }

                string imagePath = GetServiceImagePath(svc.ServiceName);
                var meta = GetOrAddMetadata(imagePath);

                string description = "";
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{svc.ServiceName}");
                    description = key?.GetValue("Description")?.ToString() ?? "";
                }
                catch { }

                bool isCritical = _safety.IsCriticalService(svc.ServiceName);
                bool isSystem = _safety.IsCriticalService(svc.ServiceName) || meta.IsSystemItem || svc.ServiceName.StartsWith("wuauserv", StringComparison.OrdinalIgnoreCase);
                bool isMicrosoft = meta.IsMicrosoft || svc.ServiceName.StartsWith("wuauserv", StringComparison.OrdinalIgnoreCase);

                string riskLevel = "Low";
                if (!isMicrosoft)
                {
                    riskLevel = "Medium";
                }

                list.Add(new ServiceEntry
                {
                    Name = svc.ServiceName,
                    DisplayName = svc.DisplayName,
                    Status = svc.Status.ToString(),
                    StartupType = startupType,
                    CanStop = svc.CanStop && !isCritical,
                    
                    // Rich Metadata
                    ImagePath = imagePath,
                    CompanyName = meta.Company,
                    Publisher = meta.Publisher,
                    IsSystemService = isSystem,
                    IsCriticalService = isCritical,
                    IsMicrosoftService = isMicrosoft,
                    IconPath = meta.IconPath,
                    ServiceDescription = description,
                    RiskLevel = riskLevel
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

                string author = "";
                string desc = "";
                try
                {
                    author = task.Definition.RegistrationInfo.Author ?? "";
                    desc = task.Definition.RegistrationInfo.Description ?? "";
                }
                catch { }

                bool isMsTask = author.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) || 
                               task.Path.Contains(@"\Microsoft\Windows", StringComparison.OrdinalIgnoreCase);
                
                bool isCriticalTask = isMsTask && (task.Name.Contains("Update", StringComparison.OrdinalIgnoreCase) || 
                                                  task.Name.Contains("Defender", StringComparison.OrdinalIgnoreCase) ||
                                                  task.Name.Contains("Maintenance", StringComparison.OrdinalIgnoreCase));

                string riskLevel = isMsTask ? "Low" : "Medium";

                list.Add(new ScheduledTaskEntry
                {
                    Name = task.Name,
                    Path = task.Path,
                    Action = action,
                    Status = task.State.ToString(),
                    IsEnabled = task.Enabled,
                    LastRunTime = task.LastRunTime == DateTime.MinValue ? null : task.LastRunTime,
                    NextRunTime = task.NextRunTime == DateTime.MinValue ? null : task.NextRunTime,
                    
                    // Rich Metadata
                    Author = author,
                    Folder = task.Folder.Path,
                    IsMicrosoftTask = isMsTask,
                    IsCriticalTask = isCriticalTask,
                    LastResult = task.LastTaskResult,
                    TaskDescription = desc,
                    RiskLevel = riskLevel
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

    public bool RegisterScheduledMaintenanceTask(bool enable)
    {
        string taskName = "WinCarePro_Maintenance";
        try
        {
            using var ts = new TaskService();
            var existingTask = ts.GetTask(taskName) ?? ts.GetTask($@"\{taskName}");
            if (existingTask != null)
            {
                existingTask.Folder.DeleteTask(taskName);
            }

            if (!enable)
            {
                Database.DbManager.LogAction("Disabled WinCarePro automated task scheduler", "Task Scheduler", "Success");
                return true;
            }

            var td = ts.NewTask();
            td.RegistrationInfo.Description = "WinCarePro Automated System Junk Clean and Maintenance";

            int freqIndex = 1; // Default: Weekly
            try
            {
                string raw = Database.DbManager.GetSettings();
                if (!string.IsNullOrEmpty(raw))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("MaintenanceFrequencyIndex", out var freqProp))
                    {
                        freqIndex = freqProp.GetInt32();
                    }
                }
            }
            catch { }

            Trigger trigger;
            string frequencyName;
            switch (freqIndex)
            {
                case 0:
                    trigger = new DailyTrigger { StartBoundary = DateTime.Today.AddHours(3) };
                    frequencyName = "daily";
                    break;
                case 2:
                    trigger = new MonthlyTrigger { StartBoundary = DateTime.Today.AddHours(3), DaysOfMonth = new int[] { 1 } };
                    frequencyName = "monthly";
                    break;
                default:
                    trigger = new WeeklyTrigger { StartBoundary = DateTime.Today.AddHours(3), DaysOfWeek = DaysOfTheWeek.Sunday };
                    frequencyName = "weekly";
                    break;
            }
            td.Triggers.Add(trigger);

            string appExe = Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "";
            if (string.IsNullOrEmpty(appExe)) return false;

            td.Actions.Add(new ExecAction(appExe, "/background"));

            ts.RootFolder.RegisterTaskDefinition(taskName, td);
            Database.DbManager.LogAction($"Enabled WinCarePro automated {frequencyName} maintenance task scheduler", "Task Scheduler", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Failed to register task scheduler: {ex.Message}", "Task Scheduler", "Failed");
            return false;
        }
    }
}

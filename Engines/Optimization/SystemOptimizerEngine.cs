using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Engines;

public class SystemOptimizerEngine
{
    public event Action<string>? ProgressMessage;
    private void Log(string msg) => ProgressMessage?.Invoke(msg);

    // RAM Booster Win32 API imports
    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_SET_QUOTA = 0x0100;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    // Memory status structure
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public ulong GetAvailablePhysicalMemory()
    {
        var status = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(ref status))
        {
            return status.ullAvailPhys;
        }
        return 0;
    }

    public ulong GetTotalPhysicalMemory()
    {
        var status = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(ref status))
        {
            return status.ullTotalPhys;
        }
        return 0;
    }

    /// <summary>
    /// Safe and legitimate memory maintenance.
    /// Cleans WinCare process working set, purges managed runtime garbage collection,
    /// and safely prompts the OS to optimize memory without forcing hard page faults on system apps.
    /// </summary>
    public async Task<(int processesOptimized, long memoryReclaimedBytes)> OptimizeRamAsync()
    {
        Log("Starting safe system physical memory (RAM) optimization...");
        ulong ramBefore = GetAvailablePhysicalMemory();
        long selfReclaimed = 0;

        await Task.Run(() =>
        {
            try
            {
                using var curProc = Process.GetCurrentProcess();
                long wsBefore = curProc.WorkingSet64;

                // 1. Force full Garbage Collection on managed heap
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 2. Trim WinCarePro's own working set safely
                EmptyWorkingSet(curProc.Handle);

                curProc.Refresh();
                long wsAfter = curProc.WorkingSet64;
                if (wsBefore > wsAfter)
                {
                    selfReclaimed = wsBefore - wsAfter;
                }
            }
            catch { }
        });

        ulong ramAfter = GetAvailablePhysicalMemory();
        long actualDiff = (long)ramAfter - (long)ramBefore;
        long memoryReclaimed = Math.Max(selfReclaimed, actualDiff);

        Log($"RAM Maintenance complete. Freed {(memoryReclaimed / 1024.0 / 1024.0):F1} MB.");
        Database.DbManager.LogAction($"RAM Optimized: Freed {memoryReclaimed} bytes safely", "System Optimizer", "Success");

        return (1, Math.Max(0, memoryReclaimed));
    }

    public List<SystemTweak> GetTweaks()
    {
        var list = new List<SystemTweak>();

        // 1. Menu Hover Delay
        list.Add(new SystemTweak
        {
            Id = "MenuShowDelay",
            Name = "Menu Hover Delay Speedup".T(),
            Description = "Reduces the wait time before menus expand on hover from 400ms to 50ms, making the Windows desktop interface feel much faster.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKCU\Control Panel\Desktop -> MenuShowDelay",
            RecommendedValue = "50",
            DefaultValue = "400",
            RequiresRestart = false,
            RequiresAdmin = false,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400")
        });

        // 2. Auto-End Hung Tasks
        list.Add(new SystemTweak
        {
            Id = "AutoEndTasks",
            Name = "Auto-Close Hung Tasks on Shutdown".T(),
            Description = "Automatically terminates frozen programs during shutdown/restart instead of displaying the standard prompt delay.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKCU\Control Panel\Desktop -> AutoEndTasks",
            RecommendedValue = "1",
            DefaultValue = "0",
            RequiresRestart = false,
            RequiresAdmin = false,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks", "0")
        });

        // 3. App Kill Timeout
        list.Add(new SystemTweak
        {
            Id = "WaitToKillAppTimeout",
            Name = "App Termination Shutdown Speedup".T(),
            Description = "Reduces wait time before terminating unresponsive apps during shutdown from 20 seconds to 2 seconds.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKCU\Control Panel\Desktop -> WaitToKillAppTimeout",
            RecommendedValue = "2000",
            DefaultValue = "20000",
            RequiresRestart = false,
            RequiresAdmin = false,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "WaitToKillAppTimeout", "20000")
        });

        // 4. NTFS Last Access Update
        list.Add(new SystemTweak
        {
            Id = "NtfsDisableLastAccessUpdate",
            Name = "Disable NTFS File Last Access Logs".T(),
            Description = "Disables updating the last-access timestamp on files. Reduces disk write cycles on SSDs, extending lifespan and speed.".T(),
            Category = "System & Disk".T(),
            IconGlyph = "\uE949",
            RegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem -> NtfsDisableLastAccessUpdate",
            RecommendedValue = "1",
            DefaultValue = "0",
            RequiresRestart = true,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", "0")
        });

        // 5. Network Packet Throttling
        list.Add(new SystemTweak
        {
            Id = "NetworkThrottlingIndex",
            Name = "Disable Network Packet Throttling".T(),
            Description = "Disables default Windows network throttling for multimedia/gaming tasks, ensuring full network bandwidth usage.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile -> NetworkThrottlingIndex",
            RecommendedValue = "-1",
            DefaultValue = "10",
            RequiresRestart = false,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", "10")
        });

        // 6. System Responsiveness Priority
        list.Add(new SystemTweak
        {
            Id = "SystemResponsiveness",
            Name = "Prioritize Active UI Applications".T(),
            Description = "Allocates 100% CPU resource priority to active foreground applications and games, disabling default system service reservations.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile -> SystemResponsiveness",
            RecommendedValue = "0",
            DefaultValue = "20",
            RequiresRestart = false,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", "20")
        });

        // 7. Hardware Accelerated GPU Scheduling (HAGS)
        list.Add(new SystemTweak
        {
            Id = "HwSchMode",
            Name = "Enable Hardware Accelerated GPU Scheduling".T(),
            Description = "Reduces graphic rendering latency and improves GPU rendering throughput by allowing direct GPU memory scheduling.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE7F1",
            RegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers -> HwSchMode",
            RecommendedValue = "2",
            DefaultValue = "1",
            RequiresRestart = true,
            RequiresAdmin = true,
            RiskLevel = "Medium",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", "1")
        });

        // 8. Disable Telemetry & Diagnostic Data
        list.Add(new SystemTweak
        {
            Id = "AllowTelemetry",
            Name = "Disable Telemetry & Diagnostic Data".T(),
            Description = "Disables background Windows telemetry data gathering, freeing CPU, memory, and network resources.".T(),
            Category = "Privacy & Logs".T(),
            IconGlyph = "\uE727",
            RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection -> AllowTelemetry",
            RecommendedValue = "0",
            DefaultValue = "1",
            RequiresRestart = true,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", "1")
        });

        // 10. Disable Cortana Background Assistant
        list.Add(new SystemTweak
        {
            Id = "AllowCortana",
            Name = "Disable Cortana Background Assistant".T(),
            Description = "Stops Cortana background assistant from running, freeing system memory and CPU cycles.".T(),
            Category = "Privacy & Logs".T(),
            IconGlyph = "\uE727",
            RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search -> AllowCortana",
            RecommendedValue = "0",
            DefaultValue = "1",
            RequiresRestart = false,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", "1")
        });

        // 11. Disable Windows Error Reporting
        list.Add(new SystemTweak
        {
            Id = "WerDisabled",
            Name = "Disable Windows Error Reporting".T(),
            Description = "Disables sending error logs and reports to Microsoft, saving background resources and speed.".T(),
            Category = "Privacy & Logs".T(),
            IconGlyph = "\uE727",
            RegistryPath = @"HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting -> Disabled",
            RecommendedValue = "1",
            DefaultValue = "0",
            RequiresRestart = false,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", "0")
        });

        // 12. Optimize Window Animations
        list.Add(new SystemTweak
        {
            Id = "MinAnimate",
            Name = "Optimize Window Animations".T(),
            Description = "Disables minimize and maximize window transition animations, making UI navigation feel instant.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE9D9",
            RegistryPath = @"HKCU\Control Panel\Desktop\WindowMetrics -> MinAnimate",
            RecommendedValue = "0",
            DefaultValue = "1",
            RequiresRestart = false,
            RequiresAdmin = false,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "1")
        });

        // 13. Disable GameDVR Background Recording Overhead (Performance)
        list.Add(new SystemTweak
        {
            Id = "GameDVR_Enabled",
            Name = "Disable Xbox GameDVR Recording Overhead".T(),
            Description = "Disables background gameplay recording services to prevent frame drops, stutter, and GPU resource contention.".T(),
            Category = "Performance".T(),
            IconGlyph = "\uE7FC",
            RegistryPath = @"HKCU\System\GameConfigStore -> GameDVR_Enabled",
            RecommendedValue = "0",
            DefaultValue = "1",
            RequiresRestart = false,
            RequiresAdmin = false,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", "1")
        });

        // 14. Disable Background Location & Sensor Tracking (Privacy & Logs)
        list.Add(new SystemTweak
        {
            Id = "DisableLocation",
            Name = "Disable Background Location Tracking".T(),
            Description = "Disables background location sensors and geo-telemetry tracking, saving CPU cycles and safeguarding privacy.".T(),
            Category = "Privacy & Logs".T(),
            IconGlyph = "\uE727",
            RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors -> DisableLocation",
            RecommendedValue = "1",
            DefaultValue = "0",
            RequiresRestart = false,
            RequiresAdmin = true,
            RiskLevel = "Low",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", "0")
        });

        // Determine IsOptimized states
        foreach (var tweak in list)
        {
            tweak.IsOptimized = IsValueOptimized(tweak.Id, tweak.CurrentValue, tweak.RecommendedValue);
        }

        return list;
    }

    private string GetTcpAckFrequencyStatus()
    {
        try
        {
            using var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
            if (interfacesKey != null)
            {
                foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                {
                    using var ifKey = interfacesKey.OpenSubKey(subKeyName);
                    var val = ifKey?.GetValue("TcpAckFrequency");
                    if (val != null && val.ToString() == "1")
                    {
                        return "1";
                    }
                }
            }
        }
        catch { }
        return "0";
    }

    private string GetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, string defaultValue)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath, false);
            if (key == null) return defaultValue;

            var val = key.GetValue(valueName);
            if (val == null) return defaultValue;

            return val.ToString() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private bool IsValueOptimized(string id, string current, string recommended)
    {
        if (id == "NtfsDisableLastAccessUpdate")
        {
            // Microsoft defaults to 0x80000000 or 0 for enabled. 1 means disabled.
            return current == "1";
        }
        if (id == "NetworkThrottlingIndex")
        {
            // -1 represents disabled throttling (represented as 0xFFFFFFFF dword)
            return current == "-1" || current == "4294967295";
        }
        return current == recommended;
    }

    public async Task<bool> ApplyTweakAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Log($"Applying optimization: {tweak.Name}...");
                string originalValue = tweak.CurrentValue;
                bool success = false;

                switch (tweak.Id)
                {
                    case "MenuShowDelay":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "50", RegistryValueKind.String);
                        break;
                    case "AutoEndTasks":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String);
                        break;
                    case "WaitToKillAppTimeout":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "WaitToKillAppTimeout", "2000", RegistryValueKind.String);
                        break;
                    case "NtfsDisableLastAccessUpdate":
                        success = SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1, RegistryValueKind.DWord);
                        break;
                    case "NetworkThrottlingIndex":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1, RegistryValueKind.DWord);
                        break;
                    case "SystemResponsiveness":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord);
                        break;
                    case "HwSchMode":
                        success = SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
                        break;
                    case "AllowTelemetry":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
                        break;
                    case "AllowCortana":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, RegistryValueKind.DWord);
                        break;
                    case "WerDisabled":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord);
                        break;
                    case "MinAnimate":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MinAnimate", "0", RegistryValueKind.String);
                        break;
                    case "GameDVR_Enabled":
                        success = SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
                        break;
                    case "DisableLocation":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1, RegistryValueKind.DWord);
                        break;
                }

                if (success)
                {
                    Database.DbManager.SaveSnapshot("SystemTweak", tweak.Id, originalValue, tweak.RecommendedValue);
                    tweak.CurrentValue = tweak.RecommendedValue;
                    tweak.IsOptimized = true;
                    Database.DbManager.LogAction($"Applied System Tweak {tweak.Id}", "System Optimizer", "Success");
                }
                else
                {
                    Database.DbManager.LogAction($"Failed System Tweak {tweak.Id}", "System Optimizer", "Failed");
                }

                return success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log($"Error applying {tweak.Id}: {ex.Message}");
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> RevertTweakAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Log($"Reverting optimization to default: {tweak.Name}...");
                string originalValue = tweak.CurrentValue;
                bool success = false;

                switch (tweak.Id)
                {
                    case "MenuShowDelay":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400", RegistryValueKind.String);
                        tweak.CurrentValue = "400";
                        break;
                    case "AutoEndTasks":
                        success = DeleteRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks");
                        tweak.CurrentValue = "0";
                        break;
                    case "WaitToKillAppTimeout":
                        success = DeleteRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "WaitToKillAppTimeout");
                        tweak.CurrentValue = "20000";
                        break;
                    case "NtfsDisableLastAccessUpdate":
                        success = SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 0, RegistryValueKind.DWord);
                        tweak.CurrentValue = "0";
                        break;
                    case "NetworkThrottlingIndex":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                        tweak.CurrentValue = "10";
                        break;
                    case "SystemResponsiveness":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20, RegistryValueKind.DWord);
                        tweak.CurrentValue = "20";
                        break;
                    case "HwSchMode":
                        success = SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord);
                        tweak.CurrentValue = "1";
                        break;
                    case "AllowTelemetry":
                        success = DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
                        tweak.CurrentValue = "1";
                        break;
                    case "AllowCortana":
                        success = DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
                        tweak.CurrentValue = "1";
                        break;
                    case "WerDisabled":
                        success = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 0, RegistryValueKind.DWord);
                        tweak.CurrentValue = "0";
                        break;
                    case "MinAnimate":
                        success = SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MinAnimate", "1", RegistryValueKind.String);
                        tweak.CurrentValue = "1";
                        break;
                    case "GameDVR_Enabled":
                        success = SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1, RegistryValueKind.DWord);
                        tweak.CurrentValue = "1";
                        break;
                    case "DisableLocation":
                        success = DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation");
                        tweak.CurrentValue = "0";
                        break;
                }

                if (success)
                {
                    Database.DbManager.SaveSnapshot("SystemTweak", tweak.Id, originalValue, tweak.DefaultValue);
                    tweak.IsOptimized = false;
                    Database.DbManager.LogAction($"Reverted System Tweak {tweak.Id}", "System Optimizer", "Success");
                }
                else
                {
                    Database.DbManager.LogAction($"Failed to Revert System Tweak {tweak.Id}", "System Optimizer", "Failed");
                }

                return success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log($"Error reverting {tweak.Id}: {ex.Message}");
                return false;
            }
        }, cancellationToken);
    }

    private bool SetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath, true);
            if (key == null)
            {
                using var createdKey = rootKey.CreateSubKey(subKeyPath, true);
                createdKey?.SetValue(valueName, value, valueKind);
            }
            else
            {
                key.SetValue(valueName, value, valueKind);
            }
            Log($"Successfully wrote registry value: {rootKey}\\{subKeyPath} -> {valueName} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Registry write error for {rootKey}\\{subKeyPath}\\{valueName}: {ex.Message}");
            return false;
        }
    }

    private bool DeleteRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath, true);
            if (key != null)
            {
                key.DeleteValue(valueName, false);
            }
            return true;
        }
        catch (Exception ex)
        {
            Log($"Registry delete error: {ex.Message}");
            return false;
        }
    }

    public async Task<long> CleanDeliveryOptimizationCacheAsync()
    {
        Log("Scanning Delivery Optimization cache files...");
        long bytesFreed = 0;
        int count = 0;

        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
        string doPath = Path.Combine(systemRoot, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");

        if (!Directory.Exists(doPath))
        {
            Log("Delivery Optimization cache is empty or unavailable.");
            return 0;
        }

        await Task.Run(() =>
        {
            try
            {
                var files = Directory.GetFiles(doPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        File.Delete(file);
                        bytesFreed += size;
                        count++;
                    }
                    catch { } // Skip locked files
                }

                foreach (var dir in Directory.GetDirectories(doPath))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"Error cleaning DO cache directory: {ex.Message}");
            }
        });

        Log($"Cleaned {count} Delivery Optimization cache files. Freed {(bytesFreed / 1024.0 / 1024.0):F2} MB.");
        Database.DbManager.LogAction($"Cleaned Delivery Optimization Cache: freed {bytesFreed} bytes", "System Optimizer", "Success");
        return bytesFreed;
    }

    public (double totalGb, double availGb, double usedGb, double percentage) GetRamStatus()
    {
        var status = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(ref status))
        {
            double total = status.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
            double avail = status.ullAvailPhys / 1024.0 / 1024.0 / 1024.0;
            double used = total - avail;
            double pct = status.dwMemoryLoad;
            return (total, avail, used, pct);
        }
        return (0, 0, 0, 0);
    }
}

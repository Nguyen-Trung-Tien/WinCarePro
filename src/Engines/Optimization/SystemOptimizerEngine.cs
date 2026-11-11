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
using WinCarePro.Core.Interop;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Engines;

public class SystemOptimizerEngine
{
    public event Action<string>? ProgressMessage;
    private void Log(string msg) => ProgressMessage?.Invoke(msg);

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerEngine.IsAdministrator", ex);
            return false;
        }
    }

    public ulong GetAvailablePhysicalMemory()
    {
        var status = NativeApi.MEMORYSTATUSEX.Create();
        if (NativeApi.GlobalMemoryStatusEx(ref status))
        {
            return status.ullAvailPhys;
        }
        return 0;
    }

    public ulong GetTotalPhysicalMemory()
    {
        var status = NativeApi.MEMORYSTATUSEX.Create();
        if (NativeApi.GlobalMemoryStatusEx(ref status))
        {
            return status.ullTotalPhys;
        }
        return 0;
    }

    /// <summary>
    /// Safe memory inspection without calling EmptyWorkingSet or forcing page-file swapping.
    /// Gimmick memory purging is removed to eliminate system stutter and hard page faults.
    /// </summary>
    public Task<(int processesOptimized, long memoryReclaimedBytes)> OptimizeRamAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((0, 0L));
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
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerEngine.GetTcpAckFrequencyStatus", ex);
        }
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
        catch (Exception ex)
        {
            CrashLogger.LogException($"SystemOptimizerEngine.GetRegistryValue({subKeyPath}\\{valueName})", ex);
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

    public bool ApplyTweak(SystemTweak tweak)
    {
        return ApplyTweakCore(tweak);
    }

    public OperationResult<bool> ApplyTweakWithResult(SystemTweak tweak)
    {
        if (tweak.RequiresAdmin && !IsAdministrator())
        {
            return OperationResult<bool>.Fail($"Administrator privileges are required to modify tweak: {tweak.Name}");
        }
        bool success = ApplyTweakCore(tweak);
        return success 
            ? OperationResult<bool>.Ok(true, $"Applied tweak: {tweak.Name}")
            : OperationResult<bool>.Fail($"Failed to apply tweak: {tweak.Name}");
    }

    public bool RevertTweak(SystemTweak tweak)
    {
        return RevertTweakCore(tweak);
    }

    public OperationResult<bool> RevertTweakWithResult(SystemTweak tweak)
    {
        if (tweak.RequiresAdmin && !IsAdministrator())
        {
            return OperationResult<bool>.Fail($"Administrator privileges are required to revert tweak: {tweak.Name}");
        }
        bool success = RevertTweakCore(tweak);
        return success
            ? OperationResult<bool>.Ok(true, $"Reverted tweak: {tweak.Name}")
            : OperationResult<bool>.Fail($"Failed to revert tweak: {tweak.Name}");
    }

    public async Task<bool> ApplyTweakAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ApplyTweakCore(tweak);
        }, cancellationToken);
    }

    public async Task<OperationResult<bool>> ApplyTweakWithResultAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ApplyTweakWithResult(tweak);
        }, cancellationToken);
    }

    private bool ApplyTweakCore(SystemTweak tweak)
    {
        try
        {
            if (tweak.RequiresAdmin && !IsAdministrator())
            {
                Log($"Administrator privileges required for tweak: {tweak.Name}");
                return false;
            }

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
            CrashLogger.LogException($"SystemOptimizerEngine.ApplyTweakCore({tweak.Id})", ex);
            Log($"Error applying {tweak.Id}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RevertTweakAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RevertTweakCore(tweak);
        }, cancellationToken);
    }

    public async Task<OperationResult<bool>> RevertTweakWithResultAsync(SystemTweak tweak, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RevertTweakWithResult(tweak);
        }, cancellationToken);
    }

    private bool RevertTweakCore(SystemTweak tweak)
    {
        try
        {
            if (tweak.RequiresAdmin && !IsAdministrator())
            {
                Log($"Administrator privileges required for tweak: {tweak.Name}");
                return false;
            }

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
            CrashLogger.LogException($"SystemOptimizerEngine.RevertTweakCore({tweak.Id})", ex);
            Log($"Error reverting {tweak.Id}: {ex.Message}");
            return false;
        }
    }

    private bool SetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, object value, RegistryValueKind valueKind)
    {
        string fullPath = (rootKey == Registry.LocalMachine ? @"HKLM\" : @"HKCU\") + subKeyPath;
        try
        {
            if (!SafeRegistryGuard.IsSafeToModifyValue(fullPath, valueName))
            {
                Log($"Security Guard blocked modification of protected registry value: {fullPath}\\{valueName}");
                return false;
            }

            try
            {
                RegistryBackupEngine.BackupKey(fullPath);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("SystemOptimizerEngine.BackupKey", ex);
            }

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
            Log($"Successfully wrote registry value: {fullPath} -> {valueName} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerEngine.SetRegistryValue", ex);
            Log($"Registry write error for {fullPath}\\{valueName}: {ex.Message}");
            return false;
        }
    }

    private bool DeleteRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName)
    {
        string fullPath = (rootKey == Registry.LocalMachine ? @"HKLM\" : @"HKCU\") + subKeyPath;
        try
        {
            if (!SafeRegistryGuard.IsSafeToDeleteValue(fullPath, valueName))
            {
                Log($"Security Guard blocked deletion of protected registry value: {fullPath}\\{valueName}");
                return false;
            }

            try
            {
                RegistryBackupEngine.BackupKey(fullPath);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("SystemOptimizerEngine.BackupKey", ex);
            }

            using var key = rootKey.OpenSubKey(subKeyPath, true);
            if (key != null)
            {
                key.DeleteValue(valueName, false);
            }
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerEngine.DeleteRegistryValue", ex);
            Log($"Registry delete error: {ex.Message}");
            return false;
        }
    }

    public async Task<long> CleanDeliveryOptimizationCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Log("Scanning Delivery Optimization cache files...");
        long bytesFreed = 0;
        int count = 0;

        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
        string doPath = Path.Combine(systemRoot, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");

        if (!Directory.Exists(doPath) || !SafePathGuard.IsSafeToDelete(doPath))
        {
            Log("Delivery Optimization cache is empty or restricted.");
            return 0;
        }

        await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (freed, filesCount) = SafePathGuard.SafeCleanDirectoryWithStats(doPath, recursive: true);
                bytesFreed = freed;
                count = filesCount;
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("SystemOptimizerEngine.CleanDeliveryOptimizationCacheAsync", ex);
                Log($"Error cleaning DO cache directory: {ex.Message}");
            }
        });

        Log($"Cleaned {count} Delivery Optimization cache files. Freed {(bytesFreed / 1024.0 / 1024.0):F2} MB.");
        Database.DbManager.LogAction($"Cleaned Delivery Optimization Cache: freed {bytesFreed} bytes", "System Optimizer", "Success");
        return bytesFreed;
    }

    public (double totalGb, double availGb, double usedGb, double percentage) GetRamStatus()
    {
        var (pct, availBytes, totalBytes) = WinCarePro.Core.Interop.NativeApi.GetMemoryStatus();
        if (totalBytes > 0)
        {
            double total = totalBytes / 1024.0 / 1024.0 / 1024.0;
            double avail = availBytes / 1024.0 / 1024.0 / 1024.0;
            double used = total - avail;
            return (total, avail, used, pct);
        }
        return (0, 0, 0, 0);
    }

    public string GenerateOptimizationReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine("WINCARE PRO - SYSTEM OPTIMIZER AUDIT REPORT");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("==================================================");
        sb.AppendLine();

        var (total, avail, used, pct) = GetRamStatus();
        sb.AppendLine("--- MEMORY (RAM) STATUS ---");
        sb.AppendLine($"Total Physical RAM: {total:F2} GB");
        sb.AppendLine($"Available RAM:      {avail:F2} GB");
        sb.AppendLine($"Used RAM:           {used:F2} GB ({pct:F1}%)");
        sb.AppendLine();

        var tweaks = GetTweaks();
        int optimized = tweaks.Count(t => t.IsOptimized);
        sb.AppendLine("--- SYSTEM TWEAKS OVERVIEW ---");
        sb.AppendLine($"Total Tweaks Configured: {tweaks.Count}");
        sb.AppendLine($"Optimized / Active:     {optimized}");
        sb.AppendLine($"Available / Pending:    {tweaks.Count - optimized}");
        sb.AppendLine();

        sb.AppendLine("--- DETAILED TWEAK SPECIFICATIONS ---");
        foreach (var t in tweaks)
        {
            sb.AppendLine($"[{(t.IsOptimized ? "OPTIMIZED" : "PENDING")}] {t.Name} ({t.Category})");
            sb.AppendLine($"   Registry Path:     {t.RegistryPath}");
            sb.AppendLine($"   Current Value:     {t.CurrentValue}");
            sb.AppendLine($"   Recommended Value: {t.RecommendedValue} (Default: {t.DefaultValue})");
            sb.AppendLine($"   Risk Level:        {t.RiskLevel} | Requires Admin: {t.RequiresAdmin} | Restart: {t.RequiresRestart}");
            sb.AppendLine();
        }

        sb.AppendLine("==================================================");
        sb.AppendLine("End of WinCare Pro System Optimizer Report");
        return sb.ToString();
    }
}

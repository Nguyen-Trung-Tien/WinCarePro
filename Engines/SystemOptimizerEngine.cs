using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class SystemOptimizerEngine
{
    public event Action<string>? ProgressMessage;
    private void Log(string msg) => ProgressMessage?.Invoke(msg);

    // RAM Booster imports
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

    public async Task<(int processesOptimized, long memoryReclaimedBytes)> OptimizeRamAsync()
    {
        Log("Starting physical memory (RAM) optimization...");
        int count = 0;
        long memoryReclaimed = 0;

        ulong ramBefore = GetAvailablePhysicalMemory();

        await Task.Run(() =>
        {
            var processes = Process.GetProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                var proc = processes[i];
                if (proc.Id <= 4) continue; // Skip Idle, System, Registry

                IntPtr hProcess = IntPtr.Zero;
                try
                {
                    hProcess = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, proc.Id);
                    if (hProcess != IntPtr.Zero)
                    {
                        long wsBefore = proc.WorkingSet64;
                        if (EmptyWorkingSet(hProcess))
                        {
                            proc.Refresh();
                            long wsAfter = proc.WorkingSet64;
                            if (wsBefore > wsAfter)
                            {
                                memoryReclaimed += (wsBefore - wsAfter);
                            }
                            count++;
                        }
                    }
                }
                catch
                {
                    // Access denied for protected system processes
                }
                finally
                {
                    if (hProcess != IntPtr.Zero)
                    {
                        CloseHandle(hProcess);
                    }
                    proc.Dispose();
                }
            }
        });

        ulong ramAfter = GetAvailablePhysicalMemory();
        long actualDiff = (long)ramAfter - (long)ramBefore;
        if (actualDiff > memoryReclaimed)
        {
            memoryReclaimed = actualDiff;
        }

        Log($"RAM Optimization complete. Optimized {count} processes. Freed {(memoryReclaimed / 1024.0 / 1024.0):F1} MB.");
        Database.DbManager.LogAction($"RAM Boosted: Optimized {count} processes, freed {memoryReclaimed} bytes", "System Optimizer", "Success");

        return (count, Math.Max(0, memoryReclaimed));
    }

    public List<SystemTweak> GetTweaks()
    {
        var list = new List<SystemTweak>();

        // 1. Menu Hover Delay
        list.Add(new SystemTweak
        {
            Id = "MenuShowDelay",
            Name = "Menu Hover Delay Speedup",
            Description = "Reduces the wait time before menus expand on hover from 400ms to 50ms, making the Windows desktop interface feel much faster.",
            Category = "UI Responsiveness",
            RecommendedValue = "50",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400")
        });

        // 2. Auto-End Hung Tasks
        list.Add(new SystemTweak
        {
            Id = "AutoEndTasks",
            Name = "Auto-Close Hung Tasks on Shutdown",
            Description = "Automatically terminates frozen programs during shutdown/restart instead of displaying the standard prompt delay.",
            Category = "Performance",
            RecommendedValue = "1",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks", "0")
        });

        // 3. App Kill Timeout
        list.Add(new SystemTweak
        {
            Id = "WaitToKillAppTimeout",
            Name = "App Termination Shutdown Speedup",
            Description = "Reduces wait time before terminating unresponsive apps during shutdown from 20 seconds to 2 seconds.",
            Category = "Performance",
            RecommendedValue = "2000",
            CurrentValue = GetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "WaitToKillAppTimeout", "20000")
        });

        // 4. NTFS Last Access Update
        list.Add(new SystemTweak
        {
            Id = "NtfsDisableLastAccessUpdate",
            Name = "Disable NTFS File Last Access Logs",
            Description = "Disables updating the last-access timestamp on files. Reduces disk write cycles on SSDs, extending lifespan and speed.",
            Category = "Disk & SSD",
            RecommendedValue = "1",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", "0")
        });

        // 5. Network Packet Throttling
        list.Add(new SystemTweak
        {
            Id = "NetworkThrottlingIndex",
            Name = "Disable Network Packet Throttling",
            Description = "Disables default Windows network throttling for multimedia/gaming tasks, ensuring full network bandwidth usage.",
            Category = "Network Center",
            RecommendedValue = "-1",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", "10")
        });

        // 6. System Responsiveness Priority
        list.Add(new SystemTweak
        {
            Id = "SystemResponsiveness",
            Name = "Prioritize Active UI Applications",
            Description = "Allocates 100% CPU resource priority to active foreground applications and games, disabling default system service reservations.",
            Category = "Performance",
            RecommendedValue = "0",
            CurrentValue = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", "20")
        });

        // Determine IsOptimized states
        foreach (var tweak in list)
        {
            tweak.IsOptimized = IsValueOptimized(tweak.Id, tweak.CurrentValue, tweak.RecommendedValue);
        }

        return list;
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

    public async Task<bool> ApplyTweakAsync(SystemTweak tweak)
    {
        return await Task.Run(() =>
        {
            try
            {
                Log($"Applying optimization: {tweak.Name}...");
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
                }

                if (success)
                {
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
            catch (Exception ex)
            {
                Log($"Error applying {tweak.Id}: {ex.Message}");
                return false;
            }
        });
    }

    public async Task<bool> RevertTweakAsync(SystemTweak tweak)
    {
        return await Task.Run(() =>
        {
            try
            {
                Log($"Reverting optimization to default: {tweak.Name}...");
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
                        // Windows default last access state is 0x80000000 (User managed, disabled by default)
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
                }

                if (success)
                {
                    tweak.IsOptimized = false;
                    Database.DbManager.LogAction($"Reverted System Tweak {tweak.Id}", "System Optimizer", "Success");
                }
                else
                {
                    Database.DbManager.LogAction($"Failed to Revert System Tweak {tweak.Id}", "System Optimizer", "Failed");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error reverting {tweak.Id}: {ex.Message}");
                return false;
            }
        });
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
            return true;
        }
        catch (Exception ex)
        {
            Log($"Registry write error: {ex.Message}");
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
}

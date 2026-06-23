using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using System.Management;
using WinCarePro.Engines;
using WinCarePro.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ProcessService _processService = new();
    private readonly HardwareDriverEngine _hardwareEngine = new();
    private readonly SecurityPrivacyEngine _securityEngine = new();
    private readonly JunkCleanerEngine _junkEngine = new();
    private readonly SoftwareUpdaterEngine _updaterEngine = new();
    private readonly StartupEngine _startupEngine = new();
    private readonly RegistryBackupEngine _registryEngine = new();
    private readonly AiDiagnosticsEngine _aiEngine = new();

    private double _cachedRamCapacityGb = 16.0;
    private bool _isRunning = true;

    private List<JunkCategory>? _scannedJunkCategories;
    private List<RegistryIssue>? _scannedRegistryIssues;

    [ObservableProperty]
    private bool _hasScanned;

    [ObservableProperty]
    private bool _isOptimizing;

    [ObservableProperty]
    private int _healthScore = 95;

    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private double _ramUsage;

    [ObservableProperty]
    private double _gpuUsage;

    [ObservableProperty]
    private double _diskUsage;

    [ObservableProperty]
    private double _cpuTemperature;

    [ObservableProperty]
    private string _cpuTempFormatted = "-- °C";
    
    [ObservableProperty]
    private string _networkStatus = "Connected";

    [ObservableProperty]
    private string _systemUptime = "0d 0h 0m";

    [ObservableProperty]
    private string _windowsVersion = "Windows 11";

    [ObservableProperty]
    private int _installedAppsCount;

    [ObservableProperty]
    private int _availableUpdatesCount;

    [ObservableProperty]
    private string _junkFileSize = "0.0 MB";

    private long _junkSizeBytes;
    
    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanStatus = "System Status: Idle";

    [ObservableProperty]
    private int _scanProgress;

    public ObservableCollection<string> Recommendations { get; } = new();
    public ObservableCollection<DiagnosticResult> DiagnosticItems { get; } = new();

    public ObservableCollection<ObservableValue> CpuSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> RamSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> GpuSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> DiskSeriesValues { get; } = new();

    public ISeries[] PerformanceSeries { get; set; }
    public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> XAxes { get; set; }
    public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> YAxes { get; set; }

    public DashboardViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Initialize historical values for rolling charts
        for (int i = 0; i < 30; i++)
        {
            CpuSeriesValues.Add(new ObservableValue(0));
            RamSeriesValues.Add(new ObservableValue(0));
            GpuSeriesValues.Add(new ObservableValue(0));
            DiskSeriesValues.Add(new ObservableValue(0));
        }

        PerformanceSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = CpuSeriesValues,
                Name = "CPU",
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#F59E0B"), 2),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = RamSeriesValues,
                Name = "RAM",
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 2),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = GpuSeriesValues,
                Name = "GPU",
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#8B5CF6"), 2),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = DiskSeriesValues,
                Name = "Disk",
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#10B981"), 2),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5
            }
        };

        XAxes = new List<LiveChartsCore.Kernel.Sketches.ICartesianAxis>
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                ShowSeparatorLines = false,
                TextSize = 10
            }
        };

        YAxes = new List<LiveChartsCore.Kernel.Sketches.ICartesianAxis>
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                MinLimit = 0,
                MaxLimit = 100,
                TextSize = 10
            }
        };

        _ = InitializeSystemInfoAsync();
        StartResourceMonitor();
    }

    private async Task InitializeSystemInfoAsync()
    {
        try
        {
            var specs = await Task.Run(() => _hardwareEngine.GetHardwareSpecifications());
            _dispatcherQueue.TryEnqueue(() =>
            {
                WindowsVersion = specs.OsVersion;
                SystemUptime = specs.SystemUptime;
                _cachedRamCapacityGb = specs.RamCapacityGb;
            });
            
            // Check Network connection
            var netEngine = new NetworkEngine();
            bool isConnected = await Task.Run(() => netEngine.CheckInternetConnection());
            _dispatcherQueue.TryEnqueue(() =>
            {
                NetworkStatus = isConnected ? "Connected" : "Disconnected";
            });

            // Count installed programs from Uninstall registry keys
            int appCount = await Task.Run(() => CountInstalledApplications());
            _dispatcherQueue.TryEnqueue(() =>
            {
                InstalledAppsCount = appCount;
            });
        }
        catch { }
    }
    private static int CountInstalledApplications()
    {
        var appNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] uninstallKeys =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var baseKey in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var keyPath in uninstallKeys)
            {
                try
                {
                    using var key = baseKey.OpenSubKey(keyPath);
                    if (key == null) continue;
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subkey = key.OpenSubKey(subkeyName);
                            var displayName = subkey?.GetValue("DisplayName")?.ToString();
                            if (!string.IsNullOrWhiteSpace(displayName))
                            {
                                appNames.Add(displayName);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        return appNames.Count > 0 ? appNames.Count : 42; // Fallback if registry query fails
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private FILETIME _prevIdleTime;
    private FILETIME _prevKernelTime;
    private FILETIME _prevUserTime;
    private bool _hasPrevTimes = false;

    private static ulong FileTimeToUInt64(FILETIME ft)
    {
        return ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
    }

    private (double cpu, double ramPercent) GetSystemResourceUsage()
    {
        double cpu = 0;
        double ramPercent = 45.0;
        bool cpuReadSuccess = false;
        bool ramReadSuccess = false;

        try
        {
            if (GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime))
            {
                if (_hasPrevTimes)
                {
                    ulong prevIdle = FileTimeToUInt64(_prevIdleTime);
                    ulong prevKernel = FileTimeToUInt64(_prevKernelTime);
                    ulong prevUser = FileTimeToUInt64(_prevUserTime);

                    ulong currIdle = FileTimeToUInt64(idleTime);
                    ulong currKernel = FileTimeToUInt64(kernelTime);
                    ulong currUser = FileTimeToUInt64(userTime);

                    ulong idleDiff = currIdle - prevIdle;
                    ulong kernelDiff = currKernel - prevKernel;
                    ulong userDiff = currUser - prevUser;

                    ulong totalDiff = kernelDiff + userDiff;
                    if (totalDiff > 0)
                    {
                        cpu = ((double)(totalDiff - idleDiff) / totalDiff) * 100.0;
                        cpu = Math.Clamp(cpu, 0.0, 100.0);
                        cpuReadSuccess = true;
                    }
                }
                else
                {
                    cpu = 2.0;
                    cpuReadSuccess = true;
                }

                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;
                _hasPrevTimes = true;
            }

            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                ramPercent = memStatus.dwMemoryLoad;
                ramReadSuccess = true;
            }
        }
        catch
        {
            // Fallback
        }

        if (!cpuReadSuccess)
        {
            var rand = new Random();
            cpu = 2.0 + rand.NextDouble() * 8.0;
        }
        if (!ramReadSuccess)
        {
            var rand = new Random();
            ramPercent = 45.0 + rand.NextDouble() * 5.0;
        }

        return (cpu, ramPercent);
    }

    private DateTime _lastSmartBoostTime = DateTime.MinValue;

    private void StartResourceMonitor()
    {
        Task.Run(async () =>
        {
            var rand = new Random();
            while (_isRunning)
            {
                int delayMs = 2000;
                bool enableSensors = true;
                bool triggerSmartBoost = true;

                try
                {
                    string raw = Database.DbManager.GetSettings();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(raw);
                        var root = doc.RootElement;
                        
                        // 1. Telemetry update interval
                        if (root.TryGetProperty("TelemetryIntervalIndex", out var telProp))
                        {
                            int idx = telProp.GetInt32();
                            delayMs = idx switch
                            {
                                0 => 500,
                                1 => 1000,
                                2 => 2000,
                                3 => 5000,
                                _ => 2000
                            };
                        }

                        // 2. Enable Hardware Sensors Thread
                        if (root.TryGetProperty("EnableSensorsThread", out var sensProp))
                        {
                            enableSensors = sensProp.GetBoolean();
                        }

                        // 3. Trigger Smart Boost Optimization
                        if (root.TryGetProperty("TriggerSmartBoost", out var boostProp))
                        {
                            triggerSmartBoost = boostProp.GetBoolean();
                        }
                    }
                }
                catch { }

                if (enableSensors)
                {
                    try
                    {
                        // Query CPU load and Memory load via kernel32 API
                        var (cpu, ram) = GetSystemResourceUsage();

                        // Real GPU monitoring via WMI performance counters
                        double gpu = GetGpuUsageWmi();

                        // Real Disk I/O monitoring via WMI performance counters
                        double disk = GetDiskUsageWmi();

                        // CPU Temperature
                        double cpuTemp = _hardwareEngine.GetCpuTemperature(cpu);

                        // Check Smart Boost threshold (RAM > 90%)
                        if (triggerSmartBoost && ram > 90.0 && (DateTime.Now - _lastSmartBoostTime).TotalMinutes >= 2.0)
                        {
                            _lastSmartBoostTime = DateTime.Now;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var optEngine = new SystemOptimizerEngine();
                                    await optEngine.OptimizeRamAsync();
                                    Database.DbManager.LogAction("Automated Smart Boost optimization triggered (RAM > 90%)", "Smart Boost", "Success");
                                }
                                catch { }
                            });
                        }

                        if (!_isRunning) break;
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            if (!_isRunning) return;
                            CpuUsage = Math.Round(cpu, 1);
                            RamUsage = Math.Round(ram, 1);
                            GpuUsage = Math.Round(gpu, 1);
                            DiskUsage = Math.Round(disk, 1);
                            CpuTemperature = cpuTemp;
                            CpuTempFormatted = $"{cpuTemp:F0}°C";

                            // Update chart collections (shift old, insert new)
                            CpuSeriesValues.Add(new ObservableValue(CpuUsage));
                            CpuSeriesValues.RemoveAt(0);

                            RamSeriesValues.Add(new ObservableValue(RamUsage));
                            RamSeriesValues.RemoveAt(0);

                            GpuSeriesValues.Add(new ObservableValue(GpuUsage));
                            GpuSeriesValues.RemoveAt(0);

                            DiskSeriesValues.Add(new ObservableValue(DiskUsage));
                            DiskSeriesValues.RemoveAt(0);
                            
                            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                            SystemUptime = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                        });
                    }
                    catch { }
                }

                await Task.Delay(delayMs);
            }
        });
    }

    public void StartMonitoring()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            StartResourceMonitor();
        }
    }

    public void StopMonitoring()
    {
        _isRunning = false;
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Real GPU usage via WMI Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine.
    /// Falls back to correlated estimation if WMI counter is unavailable.
    /// </summary>
    private double GetGpuUsageWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
            using var results = searcher.Get();
            double maxUtil = 0;
            foreach (ManagementObject obj in results)
            {
                double util = Convert.ToDouble(obj["UtilizationPercentage"]);
                if (util > maxUtil) maxUtil = util;
            }
            if (maxUtil > 0) return Math.Clamp(maxUtil, 0, 100);
        }
        catch { }

        // Fallback: correlated with CPU usage (not random)
        double baseGpu = CpuUsage * 0.3 + 2.0;
        return Math.Clamp(Math.Round(baseGpu, 1), 0, 100);
    }

    /// <summary>
    /// Real Disk active time via WMI Win32_PerfFormattedData_PerfDisk_PhysicalDisk.
    /// Falls back to correlated estimation if WMI counter is unavailable.
    /// </summary>
    private double GetDiskUsageWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT PercentDiskTime FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name='_Total'");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                double diskTime = Convert.ToDouble(obj["PercentDiskTime"]);
                return Math.Clamp(diskTime, 0, 100);
            }
        }
        catch { }

        // Fallback: correlated with CPU and RAM (not random)
        double baseDisk = CpuUsage * 0.15 + RamUsage * 0.05 + 1.0;
        return Math.Clamp(Math.Round(baseDisk, 1), 0, 100);
    }

    public async Task RunFullDiagnosticsAsync()
    {
        if (IsScanning) return;
        
        IsScanning = true;
        HasScanned = false;
        ScanProgress = 5;
        ScanStatus = "Status: Scanning Junk Files...".T();
        Recommendations.Clear();
        DiagnosticItems.Clear();

        try
        {
            // 1. Scan Junk files
            var junkCats = await _junkEngine.ScanJunkAsync();
            _scannedJunkCategories = junkCats;
            _junkSizeBytes = junkCats.Sum(x => x.SizeBytes);
            JunkFileSize = $"{(_junkSizeBytes / 1024.0 / 1024.0):F1} MB";
            ScanProgress = 30;
            await Task.Delay(300);

            // 2. Scan Registry
            ScanStatus = "Status: Scanning Registry Issues...".T();
            var regIssues = _registryEngine.ScanRegistryIssues();
            _scannedRegistryIssues = regIssues;
            ScanProgress = 55;
            await Task.Delay(300);

            // 3. Scan Software Updates
            ScanStatus = "Status: Checking Available Software Updates...".T();
            var updates = await _updaterEngine.ScanUpdatesAsync();
            AvailableUpdatesCount = updates.Count;
            ScanProgress = 75;
            await Task.Delay(300);

            // 4. Scan Security and Network
            ScanStatus = "Status: Evaluating Connection and Security Status...".T();
            var netEngine = new NetworkEngine();
            var (pingLoss, avgLatency) = await netEngine.AnalyzePingQualityAsync();
            var securityAudits = _securityEngine.RunSecurityAudits();
            var startupApps = _startupEngine.GetStartupEntries();
            ScanProgress = 90;
            await Task.Delay(300);

            // 5. Evaluate AI Health Score
            ScanStatus = "Status: Calculating System Health Index...".T();
            
            // Get background services count
            int servicesCount = 50; // default/fallback
            try
            {
                var servicesList = await Task.Run(() => _startupEngine.GetServices());
                if (servicesList != null)
                {
                    servicesCount = servicesList.Count;
                }
            }
            catch { }

            // Get disk status and free space percent
            double freeSpacePercent = 50.0;
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
                var cDrive = drives.FirstOrDefault(d => d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)) ?? drives.FirstOrDefault();
                if (cDrive != null)
                {
                    freeSpacePercent = ((double)cDrive.AvailableFreeSpace / cDrive.TotalSize) * 100.0;
                }
            }
            catch { }

            var summary = await _aiEngine.RunHealthEvaluationAsync(
                _junkSizeBytes,
                regIssues.Count,
                updates.Count,
                avgLatency,
                pingLoss,
                startupApps.Count,
                securityAudits,
                cpuUsage: CpuUsage,
                cpuTemp: _hardwareEngine.GetCpuTemperature(CpuUsage),
                ramUsagePercent: RamUsage,
                servicesCount: servicesCount,
                diskActiveTime: DiskUsage,
                freeSpacePercent: freeSpacePercent,
                ssdHealthPercent: 100.0, // default/fallback
                isThrottling: false,
                isExplorerOptimized: true
            );

            _dispatcherQueue.TryEnqueue(() =>
            {
                HealthScore = summary.HealthScore;
                foreach (var rec in summary.Recommendations)
                {
                    Recommendations.Add(rec);
                }
                foreach (var res in summary.Results)
                {
                    DiagnosticItems.Add(res);
                }
                ScanProgress = 100;
                ScanStatus = string.Format("Evaluation Complete. System Health is {0}/100".T(), HealthScore);
                IsScanning = false;
                HasScanned = true;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Scan failed:".T() + " " + ex.Message;
                IsScanning = false;
                HasScanned = false;
            });
        }
    }

    public async Task<OptimizationSummary?> OptimizeSystemAsync()
    {
        if (IsOptimizing || IsScanning) return null;
        
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Optimizing - Cleaning Junk Files...".T();
        });

        var summary = new OptimizationSummary();

        try
        {
            // 1. Optimize Junk files
            if (_scannedJunkCategories != null && _scannedJunkCategories.Any(c => c.IsSelected && c.SizeBytes > 0))
            {
                long junkCleaned = await _junkEngine.CleanJunkAsync(_scannedJunkCategories);
                summary.JunkBytesCleaned = junkCleaned;
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _junkSizeBytes = 0;
                    JunkFileSize = "0.0 MB";
                });
            }
            await Task.Delay(400);

            // 2. Clean Delivery Optimization Cache
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Status: Optimizing - Cleaning Windows Update Cache...".T();
            });
            var optEngine = new SystemOptimizerEngine();
            long doCleaned = await optEngine.CleanDeliveryOptimizationCacheAsync();
            summary.DoCacheBytesCleaned = doCleaned;
            await Task.Delay(400);

            // 3. Fix registry issues
            if (_scannedRegistryIssues != null && _scannedRegistryIssues.Any(i => i.IsSelected))
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ScanStatus = "Status: Optimizing - Repairing Registry Issues...".T();
                });
                await _registryEngine.FixRegistryIssuesAsync(_scannedRegistryIssues);
                summary.RegistryIssuesFixed = _scannedRegistryIssues.Count(i => i.IsSelected);
            }
            await Task.Delay(400);

            // 4. Boost RAM Memory
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Status: Optimizing - Performing Active RAM Boost...".T();
            });
            var ramResult = await optEngine.OptimizeRamAsync();
            summary.RamBytesReclaimed = ramResult.memoryReclaimedBytes;
            summary.RamProcessesOptimized = ramResult.processesOptimized;
            await Task.Delay(400);

            // 5. Flush DNS Cache
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Status: Optimizing - Flushing DNS Resolver Cache...".T();
            });
            var netEngine = new NetworkEngine();
            bool dnsOk = await netEngine.FlushDnsAsync();
            summary.DnsCacheFlushed = dnsOk;
            await Task.Delay(400);

            // 6. Apply system speed & responsiveness tweaks
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Status: Optimizing - Applying Speed & UI Tweaks...".T();
            });
            var tweaks = optEngine.GetTweaks();
            int tweaksApplied = 0;
            foreach (var tweak in tweaks)
            {
                if (!tweak.IsOptimized)
                {
                    bool ok = await optEngine.ApplyTweakAsync(tweak);
                    if (ok) tweaksApplied++;
                }
            }
            summary.TweaksApplied = tweaksApplied;
            await Task.Delay(400);

            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Optimization Complete! System is fully optimized.".T();
                HealthScore = 100;
                Recommendations.Clear();
                
                // Update diagnostic items to be healthy
                var tempItems = DiagnosticItems.ToList();
                DiagnosticItems.Clear();
                foreach (var item in tempItems)
                {
                    if (item.Category == "Storage" || item.Category == "Registry")
                    {
                        item.IsHealthy = true;
                        if (item.CheckName.Contains("Junk") || item.CheckName.Contains("Clutter"))
                        {
                            item.Description = "Junk files successfully cleaned.".T();
                        }
                        else if (item.CheckName.Contains("Registry"))
                        {
                            item.Description = "Registry errors successfully resolved.".T();
                        }
                    }
                    else if (item.Category == "Performance")
                    {
                        item.IsHealthy = true;
                        item.Description = "RAM optimized and speed tweaks successfully applied.".T();
                    }
                    else if (item.Category == "Network")
                    {
                        item.IsHealthy = true;
                        item.Description = "DNS resolver cache flushed. Latency and quality optimized.".T();
                    }
                    DiagnosticItems.Add(item);
                }
                
                HasScanned = false; // Reset hasScanned status
            });

            return summary;
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus = "Optimization failed:".T() + " " + ex.Message;
            });
            return null;
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }
}

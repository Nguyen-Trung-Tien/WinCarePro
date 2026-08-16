using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using WinCarePro.Engines;
using WinCarePro.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

using WinCarePro.Database;

namespace WinCarePro.ViewModels;

public enum OptimizationMode
{
    Safe,
    Recommended,
    Advanced
}

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private DispatcherQueue? _dispatcherQueue;
    public DispatcherQueue? DispatcherQueue
    {
        get => _dispatcherQueue;
        set
        {
            if (value != null)
            {
                _dispatcherQueue = value;
            }
        }
    }

    // Engine dependencies
    private readonly ProcessService _processService = App.Services.GetService<ProcessService>() ?? new();
    private readonly HardwareDriverEngine _hardwareEngine = App.Services.GetService<HardwareDriverEngine>() ?? new();
    private readonly SecurityPrivacyEngine _securityEngine = App.Services.GetService<SecurityPrivacyEngine>() ?? new();
    private readonly JunkCleanerEngine _junkEngine = App.Services.GetService<JunkCleanerEngine>() ?? new();
    private readonly SoftwareUpdaterEngine _updaterEngine = App.Services.GetService<SoftwareUpdaterEngine>() ?? new();
    private readonly StartupEngine _startupEngine = App.Services.GetService<StartupEngine>() ?? new();
    private readonly RegistryBackupEngine _registryEngine = App.Services.GetService<RegistryBackupEngine>() ?? new();
    private readonly AiDiagnosticsEngine _aiEngine = App.Services.GetService<AiDiagnosticsEngine>() ?? new();
    private readonly SystemOptimizerEngine _optimizerEngine = App.Services.GetService<SystemOptimizerEngine>() ?? new();
    private readonly NetworkEngine _networkEngine = App.Services.GetService<NetworkEngine>() ?? new();

    // Service dependencies
    private readonly ISystemSnapshotService _snapshotService = App.Services.GetService<ISystemSnapshotService>() ?? new SystemSnapshotService();
    private readonly INotificationService _notificationService = App.Services.GetService<INotificationService>() ?? new NotificationService();
    private readonly IMaintenanceSchedulerService _schedulerService = App.Services.GetService<IMaintenanceSchedulerService>() ?? new MaintenanceSchedulerService();
    
    private bool _isDisposed = false;

    private double _cachedRamCapacityGb = 16.0;
    private CancellationTokenSource? _monitorCts;
    private CancellationTokenSource? _scanCts;       // class-level scan CTS: có thể cancel từ bên ngoài
    private int _monitorRunning = 0; // 0 = stopped, 1 = running (Interlocked)


    private bool _isGpuQueryRunning = false;
    private bool _isDiskQueryRunning = false;
    private bool _isTempQueryRunning = false;

    // Dùng static Random thay vì new Random() mỗi lần gọi
    private static readonly Random _rand = new();

    private List<JunkCategory>? _scannedJunkCategories;
    private List<RegistryIssue>? _scannedRegistryIssues;

    private System.Diagnostics.PerformanceCounter? _diskTimeCounter;
    private string? _lastSnapshotId;

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
    private string _ramCapacityFormatted = "16 GB";

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
    private string _scanStatus = "System Status: Idle".T();

    [ObservableProperty]
    private int _scanProgress;

    // Bottleneck and health score breakdown extensions
    [ObservableProperty]
    private string _healthBreakdownText = "No diagnostic scan performed yet.".T();

    [ObservableProperty]
    private string _bottleneckStatus = "System Status: Stable".T();

    [ObservableProperty]
    private bool _hasBottleneck;

    [ObservableProperty]
    private bool _isExtendedLayerLoaded;

    public ObservableCollection<string> Recommendations { get; } = new();
    public ObservableCollection<DiagnosticResult> DiagnosticItems { get; } = new();
    public ObservableCollection<LogEntry> ActionLogs { get; } = new();

    // ============================================================
    // v4.0.0 — Embedded AI WinCare Engine Properties
    // ============================================================

    [ObservableProperty]
    private int _aiHealthScore = 100;

    [ObservableProperty]
    private string _aiStatusText = "AI Engine Ready".T();

    [ObservableProperty]
    private string _aiSummaryText = "Assessing predictive metrics...".T();

    [ObservableProperty]
    private string _aiPredictiveStorageText = "Calculating...".T();

    [ObservableProperty]
    private string _aiPredictiveBootText = "Analyzing...".T();

    [ObservableProperty]
    private bool _isAiExpanded;

    [ObservableProperty]
    private bool _isAiScanning;

    [ObservableProperty]
    private bool _hasAiScanned;

    public ObservableCollection<Modules.AiAssistant.AiHealthRecommendation> AiRecommendations { get; } = new();

    /// <summary>
    /// Runs the embedded AI WinCare Engine diagnostic scan.
    /// Automatically triggered on Dashboard load; also callable via button.
    /// </summary>
    public async Task RunEmbeddedAiScanAsync()
    {
        if (IsAiScanning) return;
        IsAiScanning = true;
        AiStatusText = "Analyzing system health...".T();

        try
        {
            var report = await Modules.AiAssistant.AiHealthEngine.AnalyzeSystemHealthAsync();

            _dispatcherQueue?.TryEnqueue(() =>
            {
                AiHealthScore = report.OverallScore;
                AiStatusText = report.HealthStatus;
                AiSummaryText = report.SummaryText;
                AiPredictiveStorageText = report.PredictiveStorageDaysText;
                AiPredictiveBootText = report.PredictiveBootTimeSavingsText;

                // Dynamically synchronize the main Dashboard circular gauge on startup
                if (!HasScanned)
                {
                    HealthScore = report.OverallScore;
                    HealthBreakdownText = report.SummaryText;
                }

                AiRecommendations.Clear();
                foreach (var rec in report.Recommendations)
                {
                    AiRecommendations.Add(rec);
                }

                HasAiScanned = true;
                IsAiScanning = false;
            });
        }
        catch
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                AiStatusText = "Analysis failed".T();
                IsAiScanning = false;
            });
        }
    }

    public void RefreshActionLogs()
    {
        Task.Run(() =>
        {
            try
            {
                var logs = Database.DbManager.GetLogs();
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    ActionLogs.Clear();
                    foreach (var log in logs.Take(15))
                    {
                        ActionLogs.Add(log);
                    }
                });
            }
            catch { }
        });
    }

    public ObservableCollection<ObservableValue> CpuSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> RamSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> GpuSeriesValues { get; } = new();
    public ObservableCollection<ObservableValue> DiskSeriesValues { get; } = new();

    // Chart series list using ObservableCollection for dynamic re-binding/filtering
    public ObservableCollection<ISeries> PerformanceSeries { get; } = new();
    public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> XAxes { get; set; }
    public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> YAxes { get; set; }

    private LineSeries<ObservableValue>? _cpuLineSeries;
    private LineSeries<ObservableValue>? _ramLineSeries;
    private LineSeries<ObservableValue>? _gpuLineSeries;
    private LineSeries<ObservableValue>? _diskLineSeries;

    public DashboardViewModel() : this(null)
    {
    }

    public DashboardViewModel(DispatcherQueue? dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread() ?? WinCarePro.App.MainDispatcherQueue;

        // Initialize historical values for rolling charts
        for (int i = 0; i < 30; i++)
        {
            CpuSeriesValues.Add(new ObservableValue(0));
            RamSeriesValues.Add(new ObservableValue(0));
            GpuSeriesValues.Add(new ObservableValue(0));
            DiskSeriesValues.Add(new ObservableValue(0));
        }

        _cpuLineSeries = new LineSeries<ObservableValue>
        {
            Values = CpuSeriesValues,
            Name = "CPU",
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#F59E0B"), 2),
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0.5
        };

        _ramLineSeries = new LineSeries<ObservableValue>
        {
            Values = RamSeriesValues,
            Name = "RAM",
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 2),
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0.5
        };

        _gpuLineSeries = new LineSeries<ObservableValue>
        {
            Values = GpuSeriesValues,
            Name = "GPU",
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#8B5CF6"), 2),
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0.5
        };

        _diskLineSeries = new LineSeries<ObservableValue>
        {
            Values = DiskSeriesValues,
            Name = "Disk",
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#10B981"), 2),
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0.5
        };

        PerformanceSeries.Add(_cpuLineSeries);
        PerformanceSeries.Add(_ramLineSeries);
        PerformanceSeries.Add(_gpuLineSeries);
        PerformanceSeries.Add(_diskLineSeries);

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
        InitializeCounters();
    }

    private void InitializeCounters()
    {
        Task.Run(() =>
        {
            try
            {
                var counter = new System.Diagnostics.PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                counter.NextValue();
                if (_isDisposed)
                {
                    counter.Dispose();
                }
                else
                {
                    _diskTimeCounter = counter;
                }
            }
            catch { }
        });
    }

    private async Task InitializeSystemInfoAsync()
    {
        try
        {
            var specs = await Task.Run(() => _hardwareEngine.GetHardwareSpecifications());
            _dispatcherQueue?.TryEnqueue(() =>
            {
                WindowsVersion = specs.OsVersion;
                SystemUptime = specs.SystemUptime;
                _cachedRamCapacityGb = specs.RamCapacityGb;
                RamCapacityFormatted = $"{specs.RamCapacityGb:F0} GB";
            });
            
            // Check Network connection
            bool isConnected = await Task.Run(() => _networkEngine.CheckInternetConnection());
            _dispatcherQueue?.TryEnqueue(() =>
            {
                NetworkStatus = isConnected ? "Connected" : "Disconnected";
            });

            // Count installed programs from Uninstall registry keys
            int appCount = await Task.Run(() => CountInstalledApplications());
            _dispatcherQueue?.TryEnqueue(() =>
            {
                InstalledAppsCount = appCount;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] RefreshSpecsAsync error: {ex.Message}");
        }
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

        return appNames.Count; // Accurate installed app count (or 0 if registry query fails)
    }

    public void Dispose()
    {
        _isDisposed = true;
        StopMonitoring();
        _monitorCts?.Dispose();
        _monitorCts = null;
        _scanCts?.Dispose();
        _scanCts = null;
        CleanupCounters();
        GC.SuppressFinalize(this);
    }

    private void CleanupCounters()
    {
        try
        {
            _diskTimeCounter?.Dispose();
            _diskTimeCounter = null;
        }
        catch { }
    }
}

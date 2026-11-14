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
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.ViewModels;

public enum OptimizationMode
{
    Safe,
    Recommended,
    Advanced
}

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    public static DashboardViewModel? ActiveInstance { get; private set; }

    private DispatcherQueue? _dispatcherQueue;
    public DispatcherQueue? DispatcherQueue
    {
        get => _dispatcherQueue;
        set
        {
            if (value != null)
            {
                _dispatcherQueue = value;
                DispatcherQueueInstance = value;
            }
        }
    }

    // Engine dependencies
    private readonly ProcessService _processService;
    private readonly HardwareDriverEngine _hardwareEngine;
    private readonly JunkCleanerEngine _junkEngine;
    private readonly SoftwareUpdaterEngine _updaterEngine;
    private readonly RegistryBackupEngine _registryEngine;
    private readonly NetworkEngine _networkEngine;

    // Service dependencies
    private readonly ISystemSnapshotService _snapshotService;
    private readonly INotificationService _notificationService;
    private readonly IMaintenanceSchedulerService _schedulerService;

    private bool _isDisposed = false;
    private readonly EventHandler? _languageChangedHandler;

    private double _cachedRamCapacityGb = 16.0;
    private CancellationTokenSource? _monitorCts;
    private CancellationTokenSource? _scanCts;       // class-level scan CTS: có thể cancel từ bên ngoài
    private int _monitorRunning = 0; // 0 = stopped, 1 = running (Interlocked)

    private List<JunkCategory>? _scannedJunkCategories;

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
    private bool _isSafetyNoticeVisible;

    [ObservableProperty]
    private bool _isSummaryCardVisible;

    [ObservableProperty]
    private string _summaryDashboardText = string.Empty;

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

    [ObservableProperty]
    private string _resourceInsightsText = "System resource headroom is optimal with stable average load.".T();

    [ObservableProperty]
    private string _resourceDominantBottleneck = "None".T();

    [ObservableProperty]
    private string _resourceAverageLoadText = "Telemetry history ready".T();

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

    public ObservableCollection<AiRecommendationItem> AiRecommendations { get; } = new();

    /// <summary>
    /// Runs the embedded AI WinCare Engine diagnostic scan.
    /// Automatically triggered on Dashboard load; also callable via button.
    /// </summary>
    public Task RunEmbeddedAiScanAsync()
    {
        if (IsAiScanning) return Task.CompletedTask;
        IsAiScanning = true;
        AiStatusText = "Analyzing system health...".T();

        try
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                int score = 100;
                if (CpuUsage > 80) score -= 15;
                if (RamUsage > 85) score -= 15;
                if (DiskUsage > 90) score -= 15;

                AiHealthScore = Math.Clamp(score, 10, 100);
                AiStatusText = score >= 90 ? "Optimal".T() : score >= 70 ? "Good".T() : "Attention Needed".T();
                AiSummaryText = $"System running with {score}% health score.".T();
                AiPredictiveStorageText = "Optimal";
                AiPredictiveBootText = "Stable";

                if (!HasScanned)
                {
                    HealthScore = AiHealthScore;
                    HealthBreakdownText = AiSummaryText;
                }

                HasAiScanned = true;
                IsAiScanning = false;
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.RunEmbeddedAiScanAsync", ex);
            _dispatcherQueue?.TryEnqueue(() =>
            {
                AiStatusText = "Analysis failed".T();
                IsAiScanning = false;
            });
        }
        return Task.CompletedTask;
    }

    public void RefreshActionLogs()
    {
        Task.Run(() =>
        {
            try
            {
                var logs = Database.DbManager.GetLogs();
                RunOnUI(() =>
                {
                    ActionLogs.Clear();
                    foreach (var log in logs.Take(15))
                    {
                        ActionLogs.Add(log);
                    }
                });
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("DashboardViewModel.RefreshActionLogs", ex);
            }
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
        : this(dispatcherQueue, null, null, null, null, null, null, null, null, null)
    {
    }

    public DashboardViewModel(
        ProcessService processService,
        HardwareDriverEngine hardwareEngine,
        JunkCleanerEngine junkEngine,
        SoftwareUpdaterEngine updaterEngine,
        RegistryBackupEngine registryEngine,
        NetworkEngine networkEngine,
        ISystemSnapshotService snapshotService,
        INotificationService notificationService,
        IMaintenanceSchedulerService schedulerService)
        : this(null, processService, hardwareEngine, junkEngine, updaterEngine, registryEngine, networkEngine, snapshotService, notificationService, schedulerService)
    {
    }

    public DashboardViewModel(
        DispatcherQueue? dispatcherQueue,
        ProcessService? processService,
        HardwareDriverEngine? hardwareEngine,
        JunkCleanerEngine? junkEngine,
        SoftwareUpdaterEngine? updaterEngine,
        RegistryBackupEngine? registryEngine,
        NetworkEngine? networkEngine,
        ISystemSnapshotService? snapshotService,
        INotificationService? notificationService,
        IMaintenanceSchedulerService? schedulerService)
    {
        _processService = processService ?? App.Services?.GetService<ProcessService>() ?? new();
        _hardwareEngine = hardwareEngine ?? App.Services?.GetService<HardwareDriverEngine>() ?? new();
        _junkEngine = junkEngine ?? App.Services?.GetService<JunkCleanerEngine>() ?? new();
        _updaterEngine = updaterEngine ?? App.Services?.GetService<SoftwareUpdaterEngine>() ?? new();
        _registryEngine = registryEngine ?? App.Services?.GetService<RegistryBackupEngine>() ?? new();
        _networkEngine = networkEngine ?? App.Services?.GetService<NetworkEngine>() ?? new();

        _snapshotService = snapshotService ?? App.Services?.GetService<ISystemSnapshotService>() ?? new SystemSnapshotService();
        _notificationService = notificationService ?? App.Services?.GetService<INotificationService>() ?? new NotificationService();
        _schedulerService = schedulerService ?? App.Services?.GetService<IMaintenanceSchedulerService>() ?? new MaintenanceSchedulerService();

        _dispatcherQueue = dispatcherQueue ?? SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;

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
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1AFFFFFF")),
                MinLimit = 0,
                MaxLimit = 100,
                TextSize = 10
            }
        };

        _languageChangedHandler = (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                AiStatusText = "AI Engine Ready".T();
                AiSummaryText = "Assessing predictive metrics...".T();
                UpdateHealthScoreBreakdown();
                try
                {
                    var insights = Database.DbManager.GetResourceInsights();
                    ResourceInsightsText = insights.HealthInsight.T();
                    ResourceDominantBottleneck = insights.DominantBottleneck.T();
                    ResourceAverageLoadText = string.Format("Avg CPU: {0}% | RAM: {1}%".T(), insights.AverageCpu, insights.AverageRam);
                }
                catch (Exception ex)
                {
                    CrashLogger.LogException("DashboardViewModel.LanguageChanged.GetResourceInsights", ex);
                }
            });
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;

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
            catch (Exception ex)
            {
                CrashLogger.LogException("DashboardViewModel.InitializeCounters", ex);
            }
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
        if (_languageChangedHandler != null)
        {
            TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        }
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
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.CleanupCounters", ex);
        }
    }
}

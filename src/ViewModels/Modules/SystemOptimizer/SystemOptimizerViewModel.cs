using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;

namespace WinCarePro.ViewModels;

public class ServiceStatusItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public string ServiceName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string RawStatus { get; set; } = "Unknown";

    private string _status = "Unknown";
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusGlyph));
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public bool IsRunning => RawStatus.Equals("Running", StringComparison.OrdinalIgnoreCase) ||
                             Status.Equals("Running", StringComparison.OrdinalIgnoreCase) || 
                             Status.Equals("Đang chạy", StringComparison.OrdinalIgnoreCase);

    public Microsoft.UI.Xaml.Media.Brush StatusColor => (RawStatus.Length > 0 ? RawStatus : Status) switch
    {
        var s when s.Equals("Running", StringComparison.OrdinalIgnoreCase) || s.Equals("Đang chạy", StringComparison.OrdinalIgnoreCase)
            => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),  // Green
        var s when s.Equals("Stopped", StringComparison.OrdinalIgnoreCase) || s.Equals("Đã dừng", StringComparison.OrdinalIgnoreCase)
            => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),    // Red
        var s when s.Equals("Optimized", StringComparison.OrdinalIgnoreCase) || s.Equals("Đã tối ưu", StringComparison.OrdinalIgnoreCase)
            => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)),  // Purple
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175))     // Gray
    };

    public string StatusGlyph => (RawStatus.Length > 0 ? RawStatus : Status) switch
    {
        var s when s.Equals("Running", StringComparison.OrdinalIgnoreCase) || s.Equals("Đang chạy", StringComparison.OrdinalIgnoreCase) => "\uE73E",  // CheckMark
        var s when s.Equals("Stopped", StringComparison.OrdinalIgnoreCase) || s.Equals("Đã dừng", StringComparison.OrdinalIgnoreCase) => "\uF140",  // Warning Info
        var s when s.Equals("Optimized", StringComparison.OrdinalIgnoreCase) || s.Equals("Đã tối ưu", StringComparison.OrdinalIgnoreCase) => "\uEA3A", // Flash / Thunder
        _ => "\uF16C" // Unknown / Alert
    };
}

public class SystemOptimizerViewModel : ViewModelBase, IDisposable
{
    private DispatcherQueue? _dispatcherQueue;
    private readonly SystemOptimizerEngine _optimizerEngine;
    private readonly INotificationService? _notificationService;
    private CancellationTokenSource? _aiScanCts;
    private bool _isDisposed;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(HasNoTweaks));
            }
        }
    }

    public bool IsBusy => IsLoading || IsBoosting || IsCleaningCache || IsAiScanning;

    private string _statusText = "Status: Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<SystemTweak> Tweaks { get; } = new();
    public ObservableCollection<SystemTweak> FilteredTweaks { get; } = new();
    public ObservableCollection<ServiceStatusItem> BackgroundServices { get; } = new();

    private string _currentSort = "Default";
    public string CurrentSort
    {
        get => _currentSort;
        set
        {
            if (SetProperty(ref _currentSort, value))
            {
                ApplyFilter();
            }
        }
    }

    private bool _isInfoBarOpen;
    public bool IsInfoBarOpen
    {
        get => _isInfoBarOpen;
        set => SetProperty(ref _isInfoBarOpen, value);
    }

    private string _infoBarTitle = "";
    public string InfoBarTitle
    {
        get => _infoBarTitle;
        set => SetProperty(ref _infoBarTitle, value);
    }

    private string _infoBarMessage = "";
    public string InfoBarMessage
    {
        get => _infoBarMessage;
        set => SetProperty(ref _infoBarMessage, value);
    }

    private Microsoft.UI.Xaml.Controls.InfoBarSeverity _infoBarSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
    public Microsoft.UI.Xaml.Controls.InfoBarSeverity InfoBarSeverity
    {
        get => _infoBarSeverity;
        set => SetProperty(ref _infoBarSeverity, value);
    }

    public bool HasNoTweaks => FilteredTweaks.Count == 0 && !IsLoading;

    public void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational)
    {
        RunOnUI(() =>
        {
            InfoBarTitle = title;
            InfoBarMessage = message;
            InfoBarSeverity = severity;
            IsInfoBarOpen = true;
        });
    }

    // ============================================================
    // AI HEALTH & EFFICIENCY METRICS
    // ============================================================

    private int _aiHealthScore = 96;
    public int AiHealthScore
    {
        get => _aiHealthScore;
        set
        {
            if (SetProperty(ref _aiHealthScore, value))
            {
                OnPropertyChanged(nameof(EfficiencyGradeText));
                OnPropertyChanged(nameof(EfficiencyGradeBadgeBg));
                OnPropertyChanged(nameof(EfficiencyGradeBadgeFg));
            }
        }
    }

    public string EfficiencyGradeText => AiHealthScore switch
    {
        >= 90 => "A+ Optimal".T(),
        >= 75 => "B+ Good".T(),
        >= 60 => "B Fair".T(),
        _ => "Needs Tuning".T()
    };

    public Microsoft.UI.Xaml.Media.Brush EfficiencyGradeBadgeBg => AiHealthScore switch
    {
        >= 90 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(38, 16, 185, 129)),
        >= 75 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(38, 59, 130, 246)),
        >= 60 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(38, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(38, 239, 68, 68))
    };

    public Microsoft.UI.Xaml.Media.Brush EfficiencyGradeBadgeFg => AiHealthScore switch
    {
        >= 90 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
        >= 75 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)),
        >= 60 => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68))
    };

    private string _aiStatusText = "Optimal".T();
    public string AiStatusText
    {
        get => _aiStatusText;
        set => SetProperty(ref _aiStatusText, value);
    }

    private string _aiSummaryText = "WinCare AI Engine predicts optimal system responsiveness and low latency.".T();
    public string AiSummaryText
    {
        get => _aiSummaryText;
        set => SetProperty(ref _aiSummaryText, value);
    }

    private string _currentProfile = "Recommended";
    public string CurrentProfile
    {
        get => _currentProfile;
        set
        {
            if (SetProperty(ref _currentProfile, value))
            {
                OnPropertyChanged(nameof(IsRecommendedActive));
                OnPropertyChanged(nameof(IsGamingActive));
                OnPropertyChanged(nameof(IsPerformanceActive));
                OnPropertyChanged(nameof(IsPrivacyActive));
                OnPropertyChanged(nameof(IsEcoActive));
                OnPropertyChanged(nameof(CurrentProfileDescription));
            }
        }
    }

    public bool IsRecommendedActive => CurrentProfile == "Recommended";
    public bool IsGamingActive => CurrentProfile == "Gaming";
    public bool IsPerformanceActive => CurrentProfile == "Performance";
    public bool IsPrivacyActive => CurrentProfile == "Privacy";
    public bool IsEcoActive => CurrentProfile == "Eco";

    public string CurrentProfileDescription => CurrentProfile switch
    {
        "Gaming" => "Ultra Low Latency, 0ms input lag, maximum GPU scheduling priority.".T(),
        "Performance" => "Full system horsepower, rapid menu transitions, aggressive memory cleanup.".T(),
        "Privacy" => "Zero telemetry, telemetry services muted, privacy protected.".T(),
        "Eco" => "Balanced energy efficiency, quiet operation for laptops and battery power.".T(),
        _ => "AI-balanced optimization for daily responsiveness, stability & gaming.".T()
    };

    public string AiOptimizationInsight
    {
        get
        {
            if (RamUsagePercentage > 85)
                return "High RAM usage detected. Purging cache will prevent background stutter.".T();
            if (AvailableTweaksCount > 0)
                return string.Format("{0} system optimizations available for your PC configuration.".T(), AvailableTweaksCount);
            return "All key system tweaks are active. Latency is minimized and memory is balanced.".T();
        }
    }

    private bool _isAiScanning;
    public bool IsAiScanning
    {
        get => _isAiScanning;
        set
        {
            if (SetProperty(ref _isAiScanning, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public void CancelAiScan()
    {
        try
        {
            _aiScanCts?.Cancel();
        }
        catch { }
        IsAiScanning = false;
        SetOperationState(OperationState.Idle);
    }

    public async Task RunAiScanAsync()
    {
        if (IsAiScanning || _isDisposed) return;
        _aiScanCts?.Cancel();
        _aiScanCts?.Dispose();
        _aiScanCts = new CancellationTokenSource();
        var token = _aiScanCts.Token;

        IsAiScanning = true;
        SetOperationState(OperationState.Running);
        AiStatusText = "Analyzing system health...".T();

        try
        {
            var report = await Modules.AiAssistant.AiWinCareEngine.AnalyzeSystemHealthAsync(token);
            if (token.IsCancellationRequested || _isDisposed) return;
            
            RunOnUI(() =>
            {
                if (token.IsCancellationRequested || _isDisposed)
                {
                    IsAiScanning = false;
                    SetOperationState(OperationState.Idle);
                    return;
                }
                RecalculateEfficiencyScore();
                AiStatusText = report.HealthStatus;
                AiSummaryText = report.SummaryText;
                IsAiScanning = false;
                SetOperationState(OperationState.Completed);
            });
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() =>
            {
                if (_isDisposed) return;
                AiStatusText = "Scan cancelled.".T();
                IsAiScanning = false;
                SetOperationState(OperationState.Idle);
            });
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.RunAiScanAsync", ex);
            RunOnUI(() =>
            {
                if (_isDisposed) return;
                AiStatusText = string.Format("Scan failed: {0}".T(), ex.Message);
                RecalculateEfficiencyScore();
                IsAiScanning = false;
                SetOperationState(OperationState.Failed);
            });
        }
        finally
        {
            RunOnUI(() =>
            {
                IsAiScanning = false;
                if (CurrentOperationState == OperationState.Running)
                {
                    SetOperationState(OperationState.Idle);
                }
            });
        }
    }

    private void RecalculateEfficiencyScore()
    {
        if (Tweaks.Count == 0)
        {
            AiHealthScore = 90;
            return;
        }

        double tweakRatio = (double)OptimizedTweaksCount / Tweaks.Count;
        double ramFactor = Math.Max(0, (100 - RamUsagePercentage) / 100.0);

        int score = (int)Math.Clamp(Math.Round(55 + (tweakRatio * 35) + (ramFactor * 10)), 45, 100);
        AiHealthScore = score;
        OnPropertyChanged(nameof(AiOptimizationInsight));
    }

    // ============================================================
    // SYSTEM RAM & SERVICES TELEMETRY
    // ============================================================

    public string ActiveServicesCountText => $"{BackgroundServices.Count} Monitored";

    private string _gamingRamFreedText = "0 MB";
    public string GamingRamFreedText
    {
        get => _gamingRamFreedText;
        set => SetProperty(ref _gamingRamFreedText, value);
    }

    private int _gamingOptimizedProcessesCount;
    public int GamingOptimizedProcessesCount
    {
        get => _gamingOptimizedProcessesCount;
        set => SetProperty(ref _gamingOptimizedProcessesCount, value);
    }

    public async Task OptimizeRamAsync(CancellationToken ct = default)
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        SetOperationState(OperationState.Running);
        StatusText = "Optimizing RAM...".T();
        try
        {
            ct.ThrowIfCancellationRequested();
            var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync(ct);
            double freedMB = reclaimed / (1024.0 * 1024.0);

            GamingRamFreedText = $"{freedMB:N0} MB";
            GamingOptimizedProcessesCount = procs;
            StatusText = string.Format("Optimized RAM: Freed {0} MB on {1} processes".T(), freedMB.ToString("N0"), procs);
            RecalculateEfficiencyScore();
            UpdateRamAndServices();
        }
        catch (OperationCanceledException)
        {
            StatusText = "RAM optimization cancelled.".T();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.OptimizeRamAsync", ex);
            Log($"Error optimizing RAM: {ex.Message}");
            StatusText = string.Format("Error: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
            SetOperationState(OperationState.Idle);
        }
    }

    // ============================================================
    // SEARCH & CATEGORY FILTERING
    // ============================================================

    private string _currentCategory = "All";
    public string CurrentCategory
    {
        get => _currentCategory;
        set
        {
            if (SetProperty(ref _currentCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    // Tweak Summary Counters
    private int _totalTweaksCount;
    public int TotalTweaksCount
    {
        get => _totalTweaksCount;
        set => SetProperty(ref _totalTweaksCount, value);
    }

    private int _optimizedTweaksCount;
    public int OptimizedTweaksCount
    {
        get => _optimizedTweaksCount;
        set => SetProperty(ref _optimizedTweaksCount, value);
    }

    private int _availableTweaksCount;
    public int AvailableTweaksCount
    {
        get => _availableTweaksCount;
        set => SetProperty(ref _availableTweaksCount, value);
    }

    private int _selectedTweaksCount;
    public int SelectedTweaksCount
    {
        get => _selectedTweaksCount;
        set => SetProperty(ref _selectedTweaksCount, value);
    }

    public void UpdateSelectedCount()
    {
        SelectedTweaksCount = Tweaks.Count(t => t.IsSelected);
    }

    // RAM Booster Properties
    private double _ramUsagePercentage;
    public double RamUsagePercentage
    {
        get => _ramUsagePercentage;
        set
        {
            if (SetProperty(ref _ramUsagePercentage, value))
            {
                OnPropertyChanged(nameof(RamUsagePercentageText));
            }
        }
    }

    public string RamUsagePercentageText => $"{RamUsagePercentage:F0}%";

    private string _ramUsageText = "";
    public string RamUsageText
    {
        get => _ramUsageText;
        set => SetProperty(ref _ramUsageText, value);
    }

    private string _ramOptimizedText = "";
    public string RamOptimizedText
    {
        get => _ramOptimizedText;
        set => SetProperty(ref _ramOptimizedText, value);
    }

    private bool _autoBoostEnabled;
    public bool AutoBoostEnabled
    {
        get => _autoBoostEnabled;
        set => SetProperty(ref _autoBoostEnabled, value);
    }

    private bool _isBoosting;
    public bool IsBoosting
    {
        get => _isBoosting;
        set
        {
            if (SetProperty(ref _isBoosting, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    // Detailed RAM stats
    private string _totalRamText = "";
    public string TotalRamText
    {
        get => _totalRamText;
        set => SetProperty(ref _totalRamText, value);
    }

    private string _availableRamText = "";
    public string AvailableRamText
    {
        get => _availableRamText;
        set => SetProperty(ref _availableRamText, value);
    }

    private string _usedRamText = "";
    public string UsedRamText
    {
        get => _usedRamText;
        set => SetProperty(ref _usedRamText, value);
    }

    // Kernel & Latency Profile Telemetry
    private string _multimediaSchedulingStatus = "Realtime High Priority".T();
    public string MultimediaSchedulingStatus
    {
        get => _multimediaSchedulingStatus;
        set => SetProperty(ref _multimediaSchedulingStatus, value);
    }

    private string _networkThrottlingStatus = "Disabled (Low Latency)".T();
    public string NetworkThrottlingStatus
    {
        get => _networkThrottlingStatus;
        set => SetProperty(ref _networkThrottlingStatus, value);
    }

    private string _kernelPagingStatus = "RAM Resident (Fast)".T();
    public string KernelPagingStatus
    {
        get => _kernelPagingStatus;
        set => SetProperty(ref _kernelPagingStatus, value);
    }

    private bool _isCleaningCache;
    public bool IsCleaningCache
    {
        get => _isCleaningCache;
        set
        {
            if (SetProperty(ref _isCleaningCache, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public async Task<long> CleanDeliveryCacheAsync(CancellationToken ct = default)
    {
        if (IsCleaningCache || _isDisposed) return 0;
        IsCleaningCache = true;
        SetOperationState(OperationState.Running);
        StatusText = "Purging Delivery Optimization Cache...".T();
        try
        {
            ct.ThrowIfCancellationRequested();
            long freed = await _optimizerEngine.CleanDeliveryOptimizationCacheAsync(ct);
            double mb = freed / 1024.0 / 1024.0;
            StatusText = string.Format("Purged {0:F1} MB Delivery Optimization Cache.".T(), mb);

            var notificationService = _notificationService ?? App.Services?.GetService<INotificationService>();
            notificationService?.ShowToast(
                "Cache Cleanup".T(), 
                string.Format("Successfully purged {0:F1} MB from Delivery Optimization cache.".T(), mb),
                NotificationSeverity.Success
            );

            return freed;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cache purge cancelled.".T();
            return 0;
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.CleanDeliveryCacheAsync", ex);
            StatusText = "Error: ".T() + ex.Message;
            return 0;
        }
        finally
        {
            IsCleaningCache = false;
            SetOperationState(OperationState.Idle);
        }
    }

    public void Log(string message)
    {
        // Lightweight debug logging without terminal UI allocations
        System.Diagnostics.Debug.WriteLine($"[SystemOptimizer] {message}");
    }

    private readonly Action<string> _progressHandler;
    private readonly EventHandler _languageChangedHandler;

    public SystemOptimizerViewModel() : this(null, null, null)
    {
    }

    public SystemOptimizerViewModel(SystemOptimizerEngine? optimizerEngine = null, INotificationService? notificationService = null, DispatcherQueue? dispatcherQueue = null)
    {
        _optimizerEngine = optimizerEngine ?? App.Services?.GetService<SystemOptimizerEngine>() ?? new();
        _notificationService = notificationService ?? App.Services?.GetService<INotificationService>();
        _dispatcherQueue = dispatcherQueue ?? SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;
        
        _progressHandler = (msg) => Log(msg.T());
        _optimizerEngine.ProgressMessage += _progressHandler;

        _languageChangedHandler = (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                OnPropertyChanged(nameof(EfficiencyGradeText));
                StatusText = "Status: Ready".T();
                AiSummaryText = "WinCare AI Engine predicts optimal system responsiveness and low latency.".T();
                MultimediaSchedulingStatus = "Realtime High Priority".T();
                NetworkThrottlingStatus = "Disabled (Low Latency)".T();
                KernelPagingStatus = "RAM Resident (Fast)".T();
                InitializeBackgroundServices();
                UpdateRamAndServices();
            });
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;

        LoadTweaks();
        InitializeBackgroundServices();
        UpdateRamAndServices();
    }

    private void InitializeBackgroundServices()
    {
        RunOnUI(() =>
        {
            BackgroundServices.Clear();
            BackgroundServices.Add(new ServiceStatusItem { ServiceName = "SysMain", DisplayName = "SysMain (Superfetch)".T() });
            BackgroundServices.Add(new ServiceStatusItem { ServiceName = "DiagTrack", DisplayName = "Telemetry & Diagnostics".T() });
            BackgroundServices.Add(new ServiceStatusItem { ServiceName = "WSearch", DisplayName = "Windows Search Indexer".T() });
            BackgroundServices.Add(new ServiceStatusItem { ServiceName = "wuauserv", DisplayName = "Windows Update".T() });
            BackgroundServices.Add(new ServiceStatusItem { ServiceName = "DoSvc", DisplayName = "Delivery Optimization".T() });
        });
    }

    public void UpdateRamAndServices()
    {
        if (_isDisposed) return;

        var (total, avail, used, pct) = _optimizerEngine.GetRamStatus();
        RamUsagePercentage = pct;
        RamUsageText = string.Format("{0:F1} GB / {1:F1} GB ({2:F0}%)", used, total, pct);

        TotalRamText = string.Format("{0:F1} GB", total);
        AvailableRamText = string.Format("{0:F1} GB", avail);
        UsedRamText = string.Format("{0:F1} GB", used);

        var serviceNames = BackgroundServices.Select(s => s.ServiceName).ToList();
        Task.Run(() =>
        {
            try
            {
                if (_isDisposed) return;
                var statuses = new Dictionary<string, (string raw, string localized)>();
                foreach (var name in serviceNames)
                {
                    statuses[name] = GetServiceStatus(name);
                }
                RunOnUI(() =>
                {
                    if (_isDisposed) return;
                    foreach (var svc in BackgroundServices)
                    {
                        if (statuses.TryGetValue(svc.ServiceName, out var status))
                        {
                            svc.RawStatus = status.raw;
                            svc.Status = status.localized;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.UpdateRamAndServices", ex);
            }
        });
    }

    public async Task BoostRamAsync(bool silent = false, CancellationToken ct = default)
    {
        if (IsBoosting || _isDisposed) return;
        IsBoosting = true;
        try
        {
            ct.ThrowIfCancellationRequested();
            if (!silent)
            {
                RamOptimizedText = "Purging memory cache...".T();
                Log("RAM Booster: Purging process working sets and system memory cache...".T());
            }

            var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync(ct);
            double mb = reclaimed / 1024.0 / 1024.0;

            RunOnUI(() =>
            {
                if (_isDisposed) return;

                var (total, avail, used, pct) = _optimizerEngine.GetRamStatus();
                RamUsagePercentage = pct;
                RamUsageText = string.Format("{0:F1} GB / {1:F1} GB ({2:F0}%)", used, total, pct);
                
                TotalRamText = string.Format("{0:F1} GB", total);
                AvailableRamText = string.Format("{0:F1} GB", avail);
                UsedRamText = string.Format("{0:F1} GB", used);

                RecalculateEfficiencyScore();

                if (!silent)
                {
                    string res = string.Format("Reclaimed {0} MB of physical memory.".T(), mb.ToString("F1"));
                    RamOptimizedText = res;
                    Log(string.Format("RAM Booster completed. Purged {0} processes and freed {1} MB.".T(), procs, mb.ToString("F1")));

                    var notificationService = _notificationService ?? App.Services?.GetService<INotificationService>();
                    notificationService?.ShowToast(
                        "RAM Booster".T(), 
                        string.Format("Successfully reclaimed {0} MB of physical memory across {1} processes.".T(), mb.ToString("F1"), procs),
                        NotificationSeverity.Success
                    );
                }
            });
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.BoostRamAsync", ex);
        }
        finally
        {
            IsBoosting = false;
        }
    }

    private (string raw, string localized) GetServiceStatus(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            string raw = sc.Status.ToString();
            return (raw, raw.T());
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogMessage("SystemOptimizerViewModel", $"GetServiceStatus({name}) unavailable: {ex.Message}");
            return ("Unavailable", "Unavailable".T());
        }
    }

    public void LoadTweaks()
    {
        var tweaks = _optimizerEngine.GetTweaks();
        RunOnUI(() =>
        {
            Tweaks.Clear();
            foreach (var t in tweaks)
            {
                Tweaks.Add(t);
            }

            // Update summary counters
            TotalTweaksCount = Tweaks.Count;
            OptimizedTweaksCount = Tweaks.Count(t => t.IsOptimized);
            AvailableTweaksCount = Tweaks.Count(t => !t.IsOptimized);
            UpdateSelectedCount();

            ApplyFilter();
            RecalculateEfficiencyScore();
        });
    }

    public void ApplyFilter()
    {
        RunOnUI(() =>
        {
            FilteredTweaks.Clear();
            string category = CurrentCategory;
            string query = SearchQuery?.Trim() ?? "";

            var matches = Tweaks.Where(t =>
            {
                bool categoryMatch = category == "All" ||
                                     string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(t.Category, category.T(), StringComparison.OrdinalIgnoreCase);

                bool searchMatch = string.IsNullOrEmpty(query) ||
                                   t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   t.RegistryPath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   t.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   t.RiskLevel.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   t.StatusFormatted.Contains(query, StringComparison.OrdinalIgnoreCase);

                return categoryMatch && searchMatch;
            });

            IEnumerable<SystemTweak> sorted = CurrentSort switch
            {
                "Name" => matches.OrderBy(t => t.Name),
                "Risk" => matches.OrderBy(t => t.RiskLevel),
                "Status" => matches.OrderByDescending(t => t.IsOptimized).ThenBy(t => t.Name),
                _ => matches
            };

            foreach (var t in sorted)
            {
                FilteredTweaks.Add(t);
            }

            OnPropertyChanged(nameof(HasNoTweaks));
        });
    }

    public void FilterTweaks(string category)
    {
        CurrentCategory = category;
    }

    public void SelectAll()
    {
        foreach (var t in FilteredTweaks)
        {
            t.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    public void DeselectAll()
    {
        foreach (var t in FilteredTweaks)
        {
            t.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    public void SelectUnoptimizedOnly()
    {
        foreach (var t in Tweaks)
        {
            t.IsSelected = !t.IsOptimized;
        }
        UpdateSelectedCount();
    }

    // ============================================================
    // PRESET PROFILES
    // ============================================================

    public int SelectPresetProfile(string profile)
    {
        int count = 0;
        CurrentProfile = profile;
        switch (profile)
        {
            case "Recommended":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = (t.Id != "WerDisabled" && t.Id != "DisableLocation");
                    if (t.IsSelected) count++;
                }
                break;
            case "Gaming":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = t.Id == "HwSchMode" || 
                                   t.Id == "GameDVR_Enabled" || 
                                   t.Id == "NetworkThrottlingIndex" || 
                                   t.Id == "SystemResponsiveness" || 
                                   t.Id == "MenuShowDelay" || 
                                   t.Id == "WaitToKillAppTimeout";
                    if (t.IsSelected) count++;
                }
                break;
            case "Performance":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = t.Category.Contains("Performance", StringComparison.OrdinalIgnoreCase) || 
                                   t.Category.Contains("System", StringComparison.OrdinalIgnoreCase);
                    if (t.IsSelected) count++;
                }
                break;
            case "Privacy":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = t.Category.Contains("Privacy", StringComparison.OrdinalIgnoreCase) ||
                                   t.Id == "AllowTelemetry" || t.Id == "AllowCortana" || t.Id == "WerDisabled" || t.Id == "DisableLocation";
                    if (t.IsSelected) count++;
                }
                break;
            case "Eco":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = t.Id == "NtfsDisableLastAccessUpdate" || 
                                   t.Id == "MinAnimate" || 
                                   t.Id == "AllowTelemetry";
                    if (t.IsSelected) count++;
                }
                break;
            case "All":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = true;
                    count++;
                }
                break;
            case "None":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = false;
                }
                break;
        }
        UpdateSelectedCount();
        return count;
    }

    public async Task<int> ApplySmartAutoTuneAsync()
    {
        if (IsLoading) return 0;
        Log("Profile: Applying Smart Auto-Tune (Performance, System & Network tweaks)...".T());

        // Target all safe performance, system, disk, and gaming tweaks
        foreach (var t in Tweaks)
        {
            if (t.Id != "WerDisabled" && t.Id != "DisableLocation")
            {
                t.IsSelected = true;
            }
        }

        int applied = await ApplySelectedAsync();
        return applied;
    }

    public async Task<int> ApplyPerformanceProfileAsync()
    {
        if (IsLoading) return 0;
        Log("Profile: Activating Performance Profile...".T());

        await OptimizeRamAsync();

        // Select Performance & System tweaks
        foreach (var t in Tweaks)
        {
            bool isPerf = t.Category.Contains("Performance", StringComparison.OrdinalIgnoreCase) || 
                          t.Category.Contains("System", StringComparison.OrdinalIgnoreCase);
            t.IsSelected = isPerf;
        }

        int applied = await ApplySelectedAsync();
        return applied;
    }

    public async Task<int> ApplyPrivacyProfileAsync()
    {
        if (IsLoading) return 0;
        Log("Profile: Activating Privacy Shield Profile...".T());

        foreach (var t in Tweaks)
        {
            bool isPrivacy = t.Category.Contains("Privacy", StringComparison.OrdinalIgnoreCase) ||
                             t.Id == "AllowTelemetry" || t.Id == "AllowCortana" || t.Id == "WerDisabled" || t.Id == "DisableLocation";
            t.IsSelected = isPrivacy;
        }

        int applied = await ApplySelectedAsync();
        return applied;
    }

    public async Task<int> ApplySelectedAsync(CancellationToken ct = default)
    {
        if (IsLoading || _isDisposed) return 0;
        IsLoading = true;
        SetOperationState(OperationState.Running);
        StatusText = "Applying selected tweaks...".T();
        Log("Registry Sweep: Initiating application of selected adjustments.".T());

        int applied = 0;
        try
        {
            ct.ThrowIfCancellationRequested();
            var targetTweaks = Tweaks.Where(t => t.IsSelected && !t.IsOptimized).ToList();

            if (targetTweaks.Count == 0)
            {
                // If nothing unapplied is selected, check if user wanted to reapply all selected
                targetTweaks = Tweaks.Where(t => t.IsSelected).ToList();
            }

            foreach (var t in targetTweaks)
            {
                ct.ThrowIfCancellationRequested();
                Log(string.Format("Registry Sweep: Applying tweak: {0} (Path: {1})".T(), t.Id, t.RegistryPath));
                bool ok = await _optimizerEngine.ApplyTweakAsync(t, ct);
                if (ok)
                {
                    applied++;
                    t.IsOptimized = true;
                    Log(string.Format("Registry Sweep: Successfully applied: {0}".T(), t.Id));
                }
                else
                {
                    Log(string.Format("Registry Sweep Warning: Failed to apply: {0}".T(), t.Id));
                }
            }

            // Also purge RAM cache for instant boost
            await BoostRamAsync(silent: true);

            StatusText = string.Format("Applied {0} tweaks successfully.".T(), applied);
            Log(string.Format("Registry Sweep completed. Successfully adjusted {0} settings.".T(), applied));
            
            var notificationService = _notificationService ?? App.Services?.GetService<INotificationService>();
            notificationService?.ShowToast(
                "System Optimizer".T(), 
                string.Format("Successfully applied {0} performance tweaks and purged RAM cache.".T(), applied),
                NotificationSeverity.Success
            );

            ShowNotification("Optimization Complete".T(), string.Format("Successfully applied {0} tweaks.".T(), applied), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);

            LoadTweaks();
            return applied;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Registry Sweep cancelled.".T();
            return applied;
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.ApplySelectedAsync", ex);
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Registry Sweep Error: {0}".T(), ex.Message));
            ShowNotification("Optimization Error".T(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            return 0;
        }
        finally
        {
            IsLoading = false;
            SetOperationState(OperationState.Idle);
        }
    }

    public async Task RestoreDefaultsAsync(CancellationToken ct = default)
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        SetOperationState(OperationState.Running);
        StatusText = "Restoring Windows defaults for tweaks...".T();
        Log("Registry Restore: Reverting all optimized tweaks to standard Windows settings.".T());

        int reverted = 0;
        try
        {
            ct.ThrowIfCancellationRequested();
            foreach (var t in Tweaks)
            {
                ct.ThrowIfCancellationRequested();
                if (t.IsOptimized)
                {
                    Log(string.Format("Registry Restore: Reverting tweak: {0} (Path: {1})".T(), t.Id, t.RegistryPath));
                    bool ok = await _optimizerEngine.RevertTweakAsync(t, ct);
                    if (ok)
                    {
                        reverted++;
                        Log(string.Format("Registry Restore: Successfully reverted: {0}".T(), t.Id));
                    }
                    else
                    {
                        Log(string.Format("Registry Restore Warning: Failed to revert: {0}".T(), t.Id));
                    }
                }
            }
            StatusText = string.Format("Reverted {0} tweaks successfully.".T(), reverted);
            Log(string.Format("Registry Restore completed. Reverted {0} tweaks back to standard Windows defaults.".T(), reverted));
            ShowNotification("Restore Complete".T(), string.Format("Reverted {0} tweaks back to Windows standard defaults.".T(), reverted), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
            LoadTweaks();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Restore cancelled.".T();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.RestoreDefaultsAsync", ex);
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Registry Restore Error: {0}".T(), ex.Message));
            ShowNotification("Restore Error".T(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
            SetOperationState(OperationState.Idle);
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        SetOperationState(OperationState.Running);
        StatusText = "Refreshing system telemetry & tweaks...".T();
        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                UpdateRamAndServices();
            }, ct);

            LoadTweaks();
            _ = RunAiScanAsync();
            StatusText = "Status: Ready".T();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Refresh cancelled.".T();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.RefreshAsync", ex);
            StatusText = string.Format("Refresh error: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
            SetOperationState(OperationState.Idle);
        }
    }

    public async Task<WinCarePro.Core.Models.OperationResult> ExportReportAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            string content = _optimizerEngine.GenerateOptimizationReport();
            await System.IO.File.WriteAllTextAsync(filePath, content, ct);
            StatusText = "Optimization report saved successfully.".T();
            ShowNotification("Export Report".T(), string.Format("Report saved to {0}".T(), filePath), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
            return WinCarePro.Core.Models.OperationResult.Ok("Report generated successfully.");
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SystemOptimizerViewModel.ExportReportAsync", ex);
            ShowNotification("Export Error".T(), ex.Message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            return WinCarePro.Core.Models.OperationResult.Fail($"Failed to export report: {ex.Message}");
        }
    }

    public async Task ToggleTweakAsync(SystemTweak tweak, CancellationToken ct = default)
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        SetOperationState(OperationState.Running);
        try
        {
            ct.ThrowIfCancellationRequested();
            if (tweak.IsOptimized)
            {
                Log(string.Format("Tweak Toggle: Reverting {0}".T(), tweak.Id));
                bool ok = await _optimizerEngine.RevertTweakAsync(tweak, ct);
                if (ok)
                {
                    StatusText = string.Format("Reverted tweak: {0}".T(), tweak.Name);
                    Log(string.Format("Tweak Toggle: Reverted {0} successfully.".T(), tweak.Id));
                }
            }
            else
            {
                Log(string.Format("Tweak Toggle: Applying {0}".T(), tweak.Id));
                bool ok = await _optimizerEngine.ApplyTweakAsync(tweak, ct);
                if (ok)
                {
                    StatusText = string.Format("Applied tweak: {0}".T(), tweak.Name);
                    Log(string.Format("Tweak Toggle: Applied {0} successfully.".T(), tweak.Id));
                }
            }
            LoadTweaks();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Tweak toggle cancelled.".T();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException($"SystemOptimizerViewModel.ToggleTweakAsync({tweak.Id})", ex);
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Tweak Toggle Error: {0}".T(), ex.Message));
        }
        finally
        {
            IsLoading = false;
            SetOperationState(OperationState.Idle);
        }
    }

    public void Initialize()
    {
        _isDisposed = false;
        _optimizerEngine.ProgressMessage -= _progressHandler;
        _optimizerEngine.ProgressMessage += _progressHandler;
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;
    }

    public void Cleanup()
    {
        // Transient cleanup when navigating away: cancel scans & reset busy states
        try
        {
            _aiScanCts?.Cancel();
            _aiScanCts?.Dispose();
        }
        catch { }
        _aiScanCts = null;
        IsLoading = false;
        IsBoosting = false;
        IsCleaningCache = false;
        IsAiScanning = false;
        IsInfoBarOpen = false;
        SetOperationState(OperationState.Idle);
        _optimizerEngine.ProgressMessage -= _progressHandler;
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
    }
}

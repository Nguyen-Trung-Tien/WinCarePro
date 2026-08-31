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

    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase) || 
                             Status.Equals("Đang chạy", StringComparison.OrdinalIgnoreCase);

    public Microsoft.UI.Xaml.Media.Brush StatusColor => Status switch
    {
        "Running" or "Đang chạy" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),  // Green
        "Stopped" or "Đã dừng" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),    // Red
        "Optimized" or "Đã tối ưu" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)),  // Purple
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175))           // Gray
    };

    public string StatusGlyph => Status switch
    {
        "Running" or "Đang chạy" => "\uE73E",  // CheckMark
        "Stopped" or "Đã dừng" => "\uF140",  // Warning Info
        "Optimized" or "Đã tối ưu" => "\uEA3A", // Flash / Thunder
        _ => "\uF16C" // Unknown / Alert
    };
}

public class SystemOptimizerViewModel : ViewModelBase, IDisposable
{
    private DispatcherQueue? _dispatcherQueue;
    private readonly SystemOptimizerEngine _optimizerEngine = App.Services?.GetService<SystemOptimizerEngine>() ?? new();
    private bool _isDisposed;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusText = "Status: Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<SystemTweak> Tweaks { get; } = new();
    public ObservableCollection<SystemTweak> FilteredTweaks { get; } = new();
    public ObservableCollection<ServiceStatusItem> BackgroundServices { get; } = new();

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

    private bool _isAiScanning;
    public bool IsAiScanning
    {
        get => _isAiScanning;
        set => SetProperty(ref _isAiScanning, value);
    }

    public async Task RunAiScanAsync()
    {
        if (IsAiScanning || _isDisposed) return;
        IsAiScanning = true;
        AiStatusText = "Analyzing system health...".T();

        try
        {
            var report = await Modules.AiAssistant.AiWinCareEngine.AnalyzeSystemHealthAsync();
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                RecalculateEfficiencyScore();
                AiStatusText = report.HealthStatus;
                AiSummaryText = report.SummaryText;
                IsAiScanning = false;
            });
        }
        catch
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                AiStatusText = "Scan completed".T();
                RecalculateEfficiencyScore();
                IsAiScanning = false;
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
        double ramBonus = GamingOptimizedProcessesCount > 0 ? 5 : 0;

        int score = (int)Math.Clamp(Math.Round(50 + (tweakRatio * 35) + (ramFactor * 10) + ramBonus), 45, 100);
        AiHealthScore = score;
    }

    // ============================================================
    // SYSTEM RAM MAINTENANCE & OPTIMIZATION
    // ============================================================

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

    public async Task OptimizeRamAsync()
    {
        IsLoading = true;
        StatusText = "Optimizing RAM...".T();
        try
        {
            var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync();
            double freedMB = reclaimed / (1024.0 * 1024.0);

            GamingRamFreedText = $"{freedMB:N0} MB";
            GamingOptimizedProcessesCount = procs;
            StatusText = string.Format("Optimized RAM: Freed {0} MB on {1} processes".T(), freedMB.ToString("N0"), procs);
            RecalculateEfficiencyScore();
            UpdateRamAndServices();
        }
        catch (Exception ex)
        {
            Log($"Error optimizing RAM: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
        set => SetProperty(ref _isBoosting, value);
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
        set => SetProperty(ref _isCleaningCache, value);
    }

    public async Task<long> CleanDeliveryCacheAsync()
    {
        if (IsCleaningCache || _isDisposed) return 0;
        IsCleaningCache = true;
        StatusText = "Purging Delivery Optimization Cache...".T();
        try
        {
            long freed = await _optimizerEngine.CleanDeliveryOptimizationCacheAsync();
            double mb = freed / 1024.0 / 1024.0;
            StatusText = string.Format("Purged {0:F1} MB Delivery Optimization Cache.".T(), mb);

            var notificationService = App.Services?.GetService<INotificationService>();
            notificationService?.ShowToast(
                "Cache Cleanup".T(), 
                string.Format("Successfully purged {0:F1} MB from Delivery Optimization cache.".T(), mb),
                NotificationSeverity.Success
            );

            return freed;
        }
        catch (Exception ex)
        {
            StatusText = "Error: ".T() + ex.Message;
            return 0;
        }
        finally
        {
            IsCleaningCache = false;
        }
    }

    public void Log(string message)
    {
        // Lightweight debug logging without terminal UI allocations
        System.Diagnostics.Debug.WriteLine($"[SystemOptimizer] {message}");
    }

    private readonly Action<string> _progressHandler;
    private readonly EventHandler _languageChangedHandler;

    public SystemOptimizerViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue;
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
        BackgroundServices.Clear();
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "SysMain", DisplayName = "SysMain (Superfetch)".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "DiagTrack", DisplayName = "Telemetry & Diagnostics".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "WSearch", DisplayName = "Windows Search Indexer".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "wuauserv", DisplayName = "Windows Update".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "DoSvc", DisplayName = "Delivery Optimization".T() });
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
            if (_isDisposed) return;
            var statuses = new Dictionary<string, string>();
            foreach (var name in serviceNames)
            {
                statuses[name] = GetServiceStatus(name);
            }
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                foreach (var svc in BackgroundServices)
                {
                    if (statuses.TryGetValue(svc.ServiceName, out string? status))
                    {
                        svc.Status = status;
                    }
                }
            });
        });

        if (AutoBoostEnabled && pct > 85 && !IsBoosting && !IsLoading)
        {
            Log("Auto-Boost: Memory load exceeds threshold (85%). Initiating purge.".T());
            _ = BoostRamAsync(silent: true);
        }
    }

    public async Task BoostRamAsync(bool silent = false)
    {
        if (IsBoosting || _isDisposed) return;
        IsBoosting = true;
        if (!silent)
        {
            RamOptimizedText = "Purging memory cache...".T();
            Log("RAM Booster: Purging process working sets and system memory cache...".T());
        }

        var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync();
        double mb = reclaimed / 1024.0 / 1024.0;

        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (_isDisposed) return;

            var (total, avail, used, pct) = _optimizerEngine.GetRamStatus();
            RamUsagePercentage = pct;
            RamUsageText = string.Format("{0:F1} GB / {1:F1} GB ({2:F0}%)", used, total, pct);
            
            TotalRamText = string.Format("{0:F1} GB", total);
            AvailableRamText = string.Format("{0:F1} GB", avail);
            UsedRamText = string.Format("{0:F1} GB", used);

            IsBoosting = false;
            RecalculateEfficiencyScore();

            if (!silent)
            {
                string res = string.Format("Reclaimed {0} MB of physical memory.".T(), mb.ToString("F1"));
                RamOptimizedText = res;
                Log(string.Format("RAM Booster completed. Purged {0} processes and freed {1} MB.".T(), procs, mb.ToString("F1")));

                var notificationService = App.Services?.GetService<INotificationService>();
                notificationService?.ShowToast(
                    "RAM Booster".T(), 
                    string.Format("Successfully reclaimed {0} MB of physical memory across {1} processes.".T(), mb.ToString("F1"), procs),
                    NotificationSeverity.Success
                );
            }
        });
    }

    private string GetServiceStatus(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return sc.Status.ToString().T();
        }
        catch
        {
            return "Unavailable".T();
        }
    }

    public void LoadTweaks()
    {
        Tweaks.Clear();
        var tweaks = _optimizerEngine.GetTweaks();
        foreach (var t in tweaks)
        {
            Tweaks.Add(t);
        }

        // Update summary counters
        TotalTweaksCount = Tweaks.Count;
        OptimizedTweaksCount = Tweaks.Count(t => t.IsOptimized);
        AvailableTweaksCount = Tweaks.Count(t => !t.IsOptimized);

        ApplyFilter();
        RecalculateEfficiencyScore();
    }

    public void ApplyFilter()
    {
        FilteredTweaks.Clear();
        string category = CurrentCategory;
        string query = SearchQuery?.Trim() ?? "";

        foreach (var t in Tweaks)
        {
            bool categoryMatch = category == "All" ||
                                 string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(t.Category, category.T(), StringComparison.OrdinalIgnoreCase);

            bool searchMatch = string.IsNullOrEmpty(query) ||
                               t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               t.RegistryPath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               t.Category.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (categoryMatch && searchMatch)
            {
                FilteredTweaks.Add(t);
            }
        }
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
    }

    public void DeselectAll()
    {
        foreach (var t in FilteredTweaks)
        {
            t.IsSelected = false;
        }
    }

    public void SelectUnoptimizedOnly()
    {
        foreach (var t in Tweaks)
        {
            t.IsSelected = !t.IsOptimized;
        }
    }

    // ============================================================
    // PRESET PROFILES
    // ============================================================

    public int SelectPresetProfile(string profile)
    {
        int count = 0;
        switch (profile)
        {
            case "Recommended":
                foreach (var t in Tweaks)
                {
                    t.IsSelected = (t.Id != "WerDisabled" && t.Id != "DisableLocation");
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

    public async Task<int> ApplySelectedAsync()
    {
        if (IsLoading || _isDisposed) return 0;
        IsLoading = true;
        StatusText = "Applying selected tweaks...".T();
        Log("Registry Sweep: Initiating application of selected adjustments.".T());

        int applied = 0;
        try
        {
            var targetTweaks = Tweaks.Where(t => t.IsSelected && !t.IsOptimized).ToList();

            if (targetTweaks.Count == 0)
            {
                // If nothing unapplied is selected, check if user wanted to reapply all selected
                targetTweaks = Tweaks.Where(t => t.IsSelected).ToList();
            }

            foreach (var t in targetTweaks)
            {
                Log(string.Format("Registry Sweep: Applying tweak: {0} (Path: {1})".T(), t.Id, t.RegistryPath));
                bool ok = await _optimizerEngine.ApplyTweakAsync(t);
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
            
            var notificationService = App.Services?.GetService<INotificationService>();
            notificationService?.ShowToast(
                "System Optimizer".T(), 
                string.Format("Successfully applied {0} performance tweaks and purged RAM cache.".T(), applied),
                NotificationSeverity.Success
            );

            LoadTweaks();
            return applied;
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Registry Sweep Error: {0}".T(), ex.Message));
            return 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestoreDefaultsAsync()
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        StatusText = "Restoring Windows defaults for tweaks...".T();
        Log("Registry Restore: Reverting all optimized tweaks to standard Windows settings.".T());

        int reverted = 0;
        try
        {
            foreach (var t in Tweaks)
            {
                if (t.IsOptimized)
                {
                    Log(string.Format("Registry Restore: Reverting tweak: {0} (Path: {1})".T(), t.Id, t.RegistryPath));
                    bool ok = await _optimizerEngine.RevertTweakAsync(t);
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
            LoadTweaks();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Registry Restore Error: {0}".T(), ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleTweakAsync(SystemTweak tweak)
    {
        if (IsLoading || _isDisposed) return;
        IsLoading = true;
        try
        {
            if (tweak.IsOptimized)
            {
                Log(string.Format("Tweak Toggle: Reverting {0}".T(), tweak.Id));
                bool ok = await _optimizerEngine.RevertTweakAsync(tweak);
                if (ok)
                {
                    StatusText = string.Format("Reverted tweak: {0}".T(), tweak.Name);
                    Log(string.Format("Tweak Toggle: Reverted {0} successfully.".T(), tweak.Id));
                }
            }
            else
            {
                Log(string.Format("Tweak Toggle: Applying {0}".T(), tweak.Id));
                bool ok = await _optimizerEngine.ApplyTweakAsync(tweak);
                if (ok)
                {
                    StatusText = string.Format("Applied tweak: {0}".T(), tweak.Name);
                    Log(string.Format("Tweak Toggle: Applied {0} successfully.".T(), tweak.Id));
                }
            }
            LoadTweaks();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Tweak Toggle Error: {0}".T(), ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _optimizerEngine.ProgressMessage -= _progressHandler;
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
    }
}

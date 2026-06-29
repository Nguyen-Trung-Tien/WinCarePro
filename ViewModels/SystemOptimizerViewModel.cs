using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

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
            }
        }
    }

    public Microsoft.UI.Xaml.Media.Brush StatusColor => Status switch
    {
        "Running" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),  // Green
        "Stopped" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),    // Red
        "Optimized" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)),  // Purple
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175))           // Gray
    };

    public string StatusGlyph => Status switch
    {
        "Running" => "\uE73E",  // CheckMark
        "Stopped" => "\uF140",  // Warning Info
        "Optimized" => "\uEA3A", // Flash / Thunder
        _ => "\uF16C" // Unknown / Alert
    };
}

public class SystemOptimizerViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemOptimizerEngine _optimizerEngine = new();

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

    private string _currentCategory = "All";
    public string CurrentCategory
    {
        get => _currentCategory;
        set => SetProperty(ref _currentCategory, value);
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

    // Live terminal log
    private string _consoleLogText = "";
    public string ConsoleLogText
    {
        get => _consoleLogText;
        set => SetProperty(ref _consoleLogText, value);
    }

    public void Log(string message)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ConsoleLogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        });
    }

    // Game Boost Properties
    private bool _gameBoostActive;
    public bool GameBoostActive
    {
        get => _gameBoostActive;
        set
        {
            if (SetProperty(ref _gameBoostActive, value))
            {
                _ = ToggleGameBoostAsync(value);
            }
        }
    }

    private string _gameBoostStatus = "Inactive. Background services restored.".T();
    public string GameBoostStatus
    {
        get => _gameBoostStatus;
        set => SetProperty(ref _gameBoostStatus, value);
    }

    public SystemOptimizerViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Pipe engine progress logs to our console
        _optimizerEngine.ProgressMessage += (msg) => Log(msg.T());

        Log("System Optimizer panel initialized.".T());
        LoadTweaks();
        InitializeBackgroundServices();
        UpdateRamAndServices();
    }

    private void InitializeBackgroundServices()
    {
        BackgroundServices.Clear();
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "wuauserv", DisplayName = "Windows Update".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "SysMain", DisplayName = "SysMain (Superfetch)".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "DiagTrack", DisplayName = "Telemetry & Diagnostics".T() });
        BackgroundServices.Add(new ServiceStatusItem { ServiceName = "WSearch", DisplayName = "Windows Search Indexer".T() });
    }

    public void UpdateRamAndServices()
    {
        var (total, avail, used, pct) = _optimizerEngine.GetRamStatus();
        RamUsagePercentage = pct;
        RamUsageText = string.Format("{0:F1} GB / {1:F1} GB ({2:F0}%)", used, total, pct);

        TotalRamText = string.Format("{0:F1} GB", total);
        AvailableRamText = string.Format("{0:F1} GB", avail);
        UsedRamText = string.Format("{0:F1} GB", used);

        foreach (var svc in BackgroundServices)
        {
            svc.Status = GetServiceStatus(svc.ServiceName);
        }

        if (AutoBoostEnabled && pct > 85 && !IsBoosting && !IsLoading)
        {
            Log("Auto-Boost: Memory load exceeds threshold (85%). Initiating purge.".T());
            _ = BoostRamAsync(silent: true);
        }
    }

    public async Task BoostRamAsync(bool silent = false)
    {
        if (IsBoosting) return;
        IsBoosting = true;
        if (!silent)
        {
            RamOptimizedText = "Purging memory cache...".T();
            Log("RAM Booster: Purging process working sets and file cache...".T());
        }

        var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync();
        double mb = reclaimed / 1024.0 / 1024.0;

        _dispatcherQueue?.TryEnqueue(() =>
        {
            var (total, avail, used, pct) = _optimizerEngine.GetRamStatus();
            RamUsagePercentage = pct;
            RamUsageText = string.Format("{0:F1} GB / {1:F1} GB ({2:F0}%)", used, total, pct);
            
            TotalRamText = string.Format("{0:F1} GB", total);
            AvailableRamText = string.Format("{0:F1} GB", avail);
            UsedRamText = string.Format("{0:F1} GB", used);

            IsBoosting = false;

            if (!silent)
            {
                string res = string.Format("Reclaimed {0} MB of physical memory.".T(), mb.ToString("F1"));
                RamOptimizedText = res;
                Log(string.Format("RAM Booster completed. Purged {0} processes and freed {1} MB.".T(), procs, mb.ToString("F1")));
            }
        });
    }

    private string GetServiceStatus(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return sc.Status.ToString().T(); // Translate standard service status (Running, Stopped)
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

        FilterTweaks(CurrentCategory);
    }

    public async Task ApplySelectedAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusText = "Applying selected tweaks...".T();
        Log("Registry Sweep: Initiating application of selected adjustments.".T());

        int applied = 0;
        try
        {
            foreach (var t in Tweaks)
            {
                if (t.IsSelected && !t.IsOptimized)
                {
                    Log(string.Format("Registry Sweep: Applying tweak: {0} (Path: {1})".T(), t.Id, t.RegistryPath));
                    bool ok = await _optimizerEngine.ApplyTweakAsync(t);
                    if (ok)
                    {
                        applied++;
                        Log(string.Format("Registry Sweep: Successfully applied: {0}".T(), t.Id));
                    }
                    else
                    {
                        Log(string.Format("Registry Sweep Warning: Failed to apply: {0}".T(), t.Id));
                    }
                }
            }
            StatusText = string.Format("Applied {0} tweaks successfully.".T(), applied);
            Log(string.Format("Registry Sweep completed. Successfully adjusted {0} settings.".T(), applied));
            LoadTweaks();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
            Log(string.Format("Registry Sweep Error: {0}".T(), ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestoreDefaultsAsync()
    {
        if (IsLoading) return;
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
        if (IsLoading) return;
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

    public void FilterTweaks(string category)
    {
        _currentCategory = category;
        FilteredTweaks.Clear();
        foreach (var t in Tweaks)
        {
            string translatedTarget = category.T();
            if (category == "All" || string.Equals(t.Category, translatedTarget, StringComparison.OrdinalIgnoreCase))
            {
                FilteredTweaks.Add(t);
            }
        }
        Log(string.Format("Registry Filter: Visual list updated for category '{0}' (Items shown: {1}).".T(), category, FilteredTweaks.Count));
    }

    private async Task ToggleGameBoostAsync(bool active)
    {
        IsLoading = true;
        string[] targetServices = { "wuauserv", "SysMain", "DiagTrack", "WSearch" };

        if (active)
        {
            GameBoostStatus = "Activating Game Boost... Halting services and freeing RAM cache lines.".T();
            Log("Game Boost: Activating gaming focus engine.".T());
            await Task.Delay(500);

            await Task.Run(() =>
            {
                foreach (var sName in targetServices)
                {
                    try
                    {
                        Log(string.Format("Game Boost: Querying service status for '{0}'".T(), sName));
                        using var sc = new ServiceController(sName);
                        if (sc.Status == ServiceControllerStatus.Running || sc.CanStop)
                        {
                            Log(string.Format("Game Boost: Terminating background daemon: {0}".T(), sName));
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(3));
                            Log(string.Format("Game Boost: Daemon {0} stopped successfully.".T(), sName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format("Game Boost Warning: Could not stop daemon '{0}' ({1}).".T(), sName, ex.Message));
                    }
                }
            });

            var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync();
            double mb = reclaimed / 1024.0 / 1024.0;

            _dispatcherQueue?.TryEnqueue(() =>
            {
                GameBoostStatus = "Active. Background services halted. Priorities raised.".T();
                Log(string.Format("Game Boost: RAM Flush completed. Freed {0} MB.".T(), mb.ToString("F1")));
                UpdateRamAndServices();
                Database.DbManager.LogAction("Game Boost Enabled: Suspended background daemons, optimized RAM", "System Optimizer", "Success");
                Log("Game Boost Engine is now active.".T());
            });
        }
        else
        {
            GameBoostStatus = "Deactivating Game Boost... Re-enabling background services.".T();
            Log("Game Boost: Deactivating gaming focus engine.".T());
            await Task.Delay(500);

            await Task.Run(() =>
            {
                foreach (var sName in targetServices)
                {
                    try
                    {
                        using var sc = new ServiceController(sName);
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            Log(string.Format("Game Boost: Restarting service: {0}".T(), sName));
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(3));
                            Log(string.Format("Game Boost: Service {0} is now running.".T(), sName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format("Game Boost Warning: Could not restart service '{0}' ({1}).".T(), sName, ex.Message));
                    }
                }
            });

            _dispatcherQueue?.TryEnqueue(() =>
            {
                GameBoostStatus = "Inactive. Background services restored.".T();
                UpdateRamAndServices();
                Database.DbManager.LogAction("Game Boost Disabled: Restored services", "System Optimizer", "Success");
                Log("Game Boost Engine deactivated. System services restored to windows defaults.".T());
            });
        }
        IsLoading = false;
    }
}

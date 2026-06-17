using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class SystemOptimizerViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemOptimizerEngine _engine = new();

    private bool _isBusy;
    private bool _isScanning;
    private bool _isBoosting;
    private string _progressMessage = "Ready to analyze system configuration";
    private int _progressPercent;
    private double _ramLoad;
    private string _ramUsageFormatted = "0.0 GB / 0.0 GB";
    private string _optimizationsCountText = "0/0 Tweaks Optimized";
    private string _cleanableDoSize = "0.0 MB";
    private string _logConsole = "System Optimizer Console Initialized.\n";

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !_isBusy;

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public bool IsBoosting
    {
        get => _isBoosting;
        set => SetProperty(ref _isBoosting, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public double RamLoad
    {
        get => _ramLoad;
        set => SetProperty(ref _ramLoad, value);
    }

    public string RamUsageFormatted
    {
        get => _ramUsageFormatted;
        set => SetProperty(ref _ramUsageFormatted, value);
    }

    public string OptimizationsCountText
    {
        get => _optimizationsCountText;
        set => SetProperty(ref _optimizationsCountText, value);
    }

    public string CleanableDoSize
    {
        get => _cleanableDoSize;
        set => SetProperty(ref _cleanableDoSize, value);
    }

    public string LogConsole
    {
        get => _logConsole;
        set => SetProperty(ref _logConsole, value);
    }

    public ObservableCollection<SystemTweak> Tweaks { get; } = new();

    public SystemOptimizerViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.ProgressMessage += Msg => LogText(Msg);
        
        // Load initial RAM memory stats
        UpdateRamStats();
        _ = ScanAsync();
    }

    private void LogText(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            LogConsole += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
            ProgressMessage = msg;
        });
    }

    public void UpdateRamStats()
    {
        ulong total = _engine.GetTotalPhysicalMemory();
        ulong avail = _engine.GetAvailablePhysicalMemory();

        if (total > 0)
        {
            double totalGb = total / 1024.0 / 1024.0 / 1024.0;
            double availGb = avail / 1024.0 / 1024.0 / 1024.0;
            double usedGb = totalGb - availGb;

            RamLoad = Math.Round((usedGb / totalGb) * 100.0, 1);
            RamUsageFormatted = $"{usedGb:F1} GB / {totalGb:F1} GB ({(100.0 - RamLoad):F1}% Free)";
        }
        else
        {
            RamLoad = 0;
            RamUsageFormatted = "Unavailable";
        }
    }

    public async Task ScanAsync()
    {
        if (IsScanning || IsBusy) return;

        IsScanning = true;
        IsBusy = true;
        ProgressPercent = 5;
        LogText("Scanning system performance configurations...");

        try
        {
            // 1. Update memory stats
            UpdateRamStats();
            ProgressPercent = 20;

            // 2. Fetch tweaks from engine
            var list = await Task.Run(() => _engine.GetTweaks());
            ProgressPercent = 60;

            // 3. Scan Delivery Optimization Cache Size
            LogText("Scanning Delivery Optimization cache size...");
            long cacheSize = await Task.Run(() =>
            {
                try
                {
                    string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
                    string doPath = Path.Combine(systemRoot, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");
                    if (!Directory.Exists(doPath)) return 0L;
                    return Directory.GetFiles(doPath, "*", SearchOption.AllDirectories)
                                    .Select(f => new FileInfo(f).Length)
                                    .Sum();
                }
                catch { return 0L; }
            });
            ProgressPercent = 90;

            _dispatcherQueue.TryEnqueue(() =>
            {
                Tweaks.Clear();
                foreach (var tweak in list)
                {
                    tweak.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SystemTweak.IsSelected))
                        {
                            UpdateCounts();
                        }
                    };
                    Tweaks.Add(tweak);
                }

                CleanableDoSize = $"{(cacheSize / 1024.0 / 1024.0):F1} MB";
                ProgressPercent = 100;
                UpdateCounts();
                LogText("Scan completed successfully.");
            });
        }
        catch (Exception ex)
        {
            LogText($"Error scanning optimizations: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
            IsBusy = false;
        }
    }

    private void UpdateCounts()
    {
        int optimized = Tweaks.Count(t => t.IsOptimized);
        OptimizationsCountText = $"{optimized}/{Tweaks.Count} Tweaks Optimized";
    }

    public async Task ApplySelectedTweaksAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ProgressPercent = 0;
        LogText("Applying selected system performance tweaks...");

        try
        {
            var selected = Tweaks.Where(t => t.IsSelected && !t.IsOptimized).ToList();
            if (selected.Count == 0)
            {
                LogText("No unoptimized tweaks selected.");
                ProgressPercent = 100;
                return;
            }

            double step = 100.0 / selected.Count;
            double currentPercent = 0;
            int successful = 0;

            foreach (var tweak in selected)
            {
                bool ok = await _engine.ApplyTweakAsync(tweak);
                if (ok) successful++;

                currentPercent += step;
                ProgressPercent = (int)currentPercent;
            }

            ProgressPercent = 100;
            LogText($"Tweak optimization completed. Applied {successful}/{selected.Count} successfully.");
            UpdateCounts();
        }
        catch (Exception ex)
        {
            LogText($"Error applying tweaks: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RevertSelectedTweaksAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ProgressPercent = 0;
        LogText("Reverting selected optimizations to Windows defaults...");

        try
        {
            var selected = Tweaks.Where(t => t.IsSelected && t.IsOptimized).ToList();
            if (selected.Count == 0)
            {
                LogText("No optimized tweaks selected to revert.");
                ProgressPercent = 100;
                return;
            }

            double step = 100.0 / selected.Count;
            double currentPercent = 0;
            int successful = 0;

            foreach (var tweak in selected)
            {
                bool ok = await _engine.RevertTweakAsync(tweak);
                if (ok) successful++;

                currentPercent += step;
                ProgressPercent = (int)currentPercent;
            }

            ProgressPercent = 100;
            LogText($"Tweak reversion completed. Reverted {successful}/{selected.Count} successfully.");
            UpdateCounts();
        }
        catch (Exception ex)
        {
            LogText($"Error reverting tweaks: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BoostRamAsync()
    {
        if (IsBusy || IsBoosting) return;

        IsBoosting = true;
        IsBusy = true;
        ProgressPercent = 20;
        LogText("Initiating active RAM optimization and working set cleanup...");

        try
        {
            var result = await _engine.OptimizeRamAsync();
            ProgressPercent = 80;

            await Task.Delay(500); // Small pause for UI visual completion
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateRamStats();
                ProgressPercent = 100;
                LogText($"RAM boosted! Cleared working memory set for {result.processesOptimized} active processes, reclaimed {(result.memoryReclaimedBytes / 1024.0 / 1024.0):F1} MB of physical RAM.");
            });
        }
        catch (Exception ex)
        {
            LogText($"Error boosting RAM memory: {ex.Message}");
        }
        finally
        {
            IsBoosting = false;
            IsBusy = false;
        }
    }

    public async Task CleanDoCacheAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ProgressPercent = 30;

        try
        {
            long freedBytes = await _engine.CleanDeliveryOptimizationCacheAsync();
            ProgressPercent = 100;
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                CleanableDoSize = "0.0 MB";
                LogText($"Delivery Optimization cache successfully cleaned. Reclaimed {(freedBytes / 1024.0 / 1024.0):F2} MB.");
            });
        }
        catch (Exception ex)
        {
            LogText($"Error cleaning Delivery Optimization Cache: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

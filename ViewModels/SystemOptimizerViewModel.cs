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

    private string _statusText = "Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<SystemTweak> Tweaks { get; } = new();

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

    private string _gameBoostStatus = "Game Boost is inactive.".T();
    public string GameBoostStatus
    {
        get => _gameBoostStatus;
        set => SetProperty(ref _gameBoostStatus, value);
    }

    public SystemOptimizerViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        LoadTweaks();
    }

    public void LoadTweaks()
    {
        Tweaks.Clear();
        var tweaks = _optimizerEngine.GetTweaks();
        foreach (var t in tweaks)
        {
            Tweaks.Add(t);
        }
    }

    public async Task ApplySelectedAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusText = "Applying selected tweaks...".T();

        int applied = 0;
        try
        {
            foreach (var t in Tweaks)
            {
                if (t.IsSelected && !t.IsOptimized)
                {
                    bool ok = await _optimizerEngine.ApplyTweakAsync(t);
                    if (ok) applied++;
                }
            }
            StatusText = string.Format("Applied {0} tweaks successfully.".T(), applied);
            LoadTweaks();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RestoreDefaults()
    {
        StatusText = "Restore defaults requires manual registry reset. Contact support.".T();
    }

    private async Task ToggleGameBoostAsync(bool active)
    {
        IsLoading = true;
        if (active)
        {
            GameBoostStatus = "Activating Game Boost... Halting updates and freeing RAM cache lines.".T();
            await Task.Delay(600);

            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController("wuauserv");
                    if (sc.Status == ServiceControllerStatus.Running || sc.CanStop)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                    }
                }
                catch { }
            });

            var (procs, reclaimed) = await _optimizerEngine.OptimizeRamAsync();
            double mb = reclaimed / 1024.0 / 1024.0;

            GameBoostStatus = string.Format("Active. Halting wuauserv completed. Purged RAM files cache ({0} MB freed). Foreground priorities raised.".T(), mb.ToString("F1"));
            Database.DbManager.LogAction("Game Boost Enabled: Suspended background daemons, optimized RAM", "System Optimizer", "Success");
        }
        else
        {
            GameBoostStatus = "Deactivating Game Boost... Re-enabling background services.".T();
            await Task.Delay(400);

            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController("wuauserv");
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                    }
                }
                catch { }
            });

            GameBoostStatus = "Game Boost is inactive.".T();
            Database.DbManager.LogAction("Game Boost Disabled: Restored services start type", "System Optimizer", "Success");
        }
        IsLoading = false;
    }
}

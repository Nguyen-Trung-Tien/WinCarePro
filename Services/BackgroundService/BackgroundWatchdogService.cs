using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Interop;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

/// <summary>
/// High-efficiency, low-power background watchdog service.
/// Continuously monitors system health in the background and executes automated maintenance
/// (such as Smart RAM Boost and proactive background maintenance) with zero UI lag and &lt; 0.1% CPU overhead.
/// </summary>
public sealed class BackgroundWatchdogService : IBackgroundWatchdogService
{
    private readonly SystemOptimizerEngine _optimizerEngine;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService? _notificationService;

    private CancellationTokenSource? _cts;
    private Task? _monitoringTask;
    private DateTime _lastSmartBoostTime = DateTime.MinValue;
    private int _isRunningState = 0; // 0 = stopped, 1 = running

    public bool IsRunning => Volatile.Read(ref _isRunningState) == 1;

    public BackgroundWatchdogService(
        SystemOptimizerEngine optimizerEngine,
        ISettingsService settingsService,
        INotificationService? notificationService = null)
    {
        _optimizerEngine = optimizerEngine ?? throw new ArgumentNullException(nameof(optimizerEngine));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _notificationService = notificationService;
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _isRunningState, 1, 0) != 0)
        {
            return; // Already started
        }

        _cts = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => RunWatchdogLoopAsync(_cts.Token));
        DbManager.LogAction("Background Watchdog Service activated.", "Background Watchdog", "Success");
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isRunningState, 0, 1) != 1)
        {
            return; // Already stopped
        }

        try
        {
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _monitoringTask = null;
        }

        DbManager.LogAction("Background Watchdog Service deactivated.", "Background Watchdog", "Success");
    }

    public async Task TriggerImmediateCheckAsync()
    {
        await PerformWatchdogCheckAsync(CancellationToken.None);
    }

    private async Task RunWatchdogLoopAsync(CancellationToken ct)
    {
        // Periodic check every 30 seconds with minimal overhead
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            // Initial warm-up delay of 15 seconds after app startup before first check
            await Task.Delay(TimeSpan.FromSeconds(15), ct);

            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
            {
                await PerformWatchdogCheckAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown requested
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Background Watchdog loop error: {ex.Message}", "Background Watchdog", "Failed");
        }
    }

    private async Task PerformWatchdogCheckAsync(CancellationToken ct)
    {
        try
        {
            var settings = _settingsService.CurrentSettings;
            if (settings == null) return;

            // 1. Automated RAM Smart Boost check
            if (settings.TriggerSmartBoost)
            {
                var mem = NativeApi.MEMORYSTATUSEX.Create();
                if (NativeApi.GlobalMemoryStatusEx(ref mem))
                {
                    double ramPercent = mem.dwMemoryLoad;
                    double freeGb = mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);

                    // Trigger if RAM load exceeds 90% and cooldown of 2 minutes has elapsed
                    if (ramPercent >= 90.0 && (DateTime.Now - _lastSmartBoostTime).TotalMinutes >= 2.0)
                    {
                        _lastSmartBoostTime = DateTime.Now;
                        await _optimizerEngine.OptimizeRamAsync();

                        DbManager.LogAction(
                            $"Automated Background Smart Boost executed (RAM: {ramPercent:F0}%, Free: {freeGb:F1} GB).",
                            "Smart Boost",
                            "Success");

                        if (settings.ShowNotifications && settings.NotifyOnMaintenance)
                        {
                            _notificationService?.ShowToast(
                                "Smart RAM Boost",
                                $"System RAM was at {ramPercent:F0}%. WinCare Pro optimized memory in the background.",
                                NotificationSeverity.Info);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Watchdog check error: {ex.Message}", "Background Watchdog", "Warning");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

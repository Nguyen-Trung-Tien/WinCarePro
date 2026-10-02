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
    private DateTime _lastDiskWarningTime = DateTime.MinValue;
    private DateTime _lastTelemetrySnapshotTime = DateTime.MinValue;
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
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("BackgroundWatchdogService.Stop", ex);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _monitoringTask = null;
        }

        DbManager.LogAction("Background Watchdog Service deactivated.", "Background Watchdog", "Success");
    }

    public async Task StopAsync()
    {
        if (Interlocked.CompareExchange(ref _isRunningState, 0, 1) != 1)
        {
            return; // Already stopped
        }

        var task = _monitoringTask;
        try
        {
            _cts?.Cancel();
            if (task != null)
            {
                await Task.WhenAny(task, Task.Delay(2000));
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("BackgroundWatchdogService.StopAsync", ex);
        }
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

            // Check battery & power status to avoid heavy background spikes on low battery
            bool isOnLowBattery = false;
            try
            {
                if (NativeApi.GetSystemPowerStatus(out var powerStatus))
                {
                    // ACLineStatus == 0 (battery power) and battery < 20%
                    if (powerStatus.ACLineStatus == 0 && powerStatus.BatteryLifePercent < 20)
                    {
                        isOnLowBattery = true;
                    }
                }
            }
            catch { }

            // 1. Automated RAM Smart Boost check
            if (settings.TriggerSmartBoost && !isOnLowBattery)
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

            // 2. Proactive Low Disk Space Alert on System Drive
            try
            {
                string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                if (NativeApi.GetDiskFreeSpaceEx(systemDrive, out ulong freeBytes, out ulong totalBytes, out _))
                {
                    double freeGB = freeBytes / (1024.0 * 1024.0 * 1024.0);
                    double totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                    double freePct = totalGB > 0 ? (freeGB / totalGB) * 100.0 : 100.0;

                    if ((freeGB < 5.0 || freePct < 8.0) && (DateTime.Now - _lastDiskWarningTime).TotalMinutes >= 15.0)
                    {
                        _lastDiskWarningTime = DateTime.Now;
                        DbManager.LogAction($"Proactive Low Disk Space Alert on {systemDrive} (Free: {freeGB:F1} GB, {freePct:F1}%)", "Storage Watchdog", "Warning");
                        
                        if (settings.ShowNotifications)
                        {
                            _notificationService?.ShowToast(
                                "Low Disk Space Warning",
                                $"System drive ({systemDrive}) has only {freeGB:F1} GB free. Consider running Junk Cleaner.",
                                NotificationSeverity.Warning);
                        }
                    }
                }
            }
            catch { }

            // 3. Periodic telemetry snapshot recording (every 10 minutes)
            if ((DateTime.Now - _lastTelemetrySnapshotTime).TotalMinutes >= 10.0)
            {
                _lastTelemetrySnapshotTime = DateTime.Now;
                try
                {
                    var mem = NativeApi.MEMORYSTATUSEX.Create();
                    double ramUsage = NativeApi.GlobalMemoryStatusEx(ref mem) ? mem.dwMemoryLoad : 0.0;
                    DbManager.SaveResourceSnapshot(0.0, ramUsage, 0.0, 0.0, ramUsage > 85.0 ? "High Memory Load" : null);
                }
                catch { }
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

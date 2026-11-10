using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Interop;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

/// <summary>
/// Zero-Bloat On-Demand Watchdog Service:
/// Eliminates continuous background polling loops and forced RAM working-set trimming.
/// Checks are executed strictly on-demand when explicitly triggered.
/// </summary>
public sealed class BackgroundWatchdogService : IBackgroundWatchdogService
{
    private readonly SystemOptimizerEngine _optimizerEngine;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService? _notificationService;
    private int _isRunningState = 0;

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
        Interlocked.Exchange(ref _isRunningState, 1);
    }

    public void Stop()
    {
        Interlocked.Exchange(ref _isRunningState, 0);
    }

    public Task StopAsync()
    {
        Interlocked.Exchange(ref _isRunningState, 0);
        return Task.CompletedTask;
    }

    public Task TriggerImmediateCheckAsync()
    {
        PerformOnDemandDiskCheck();
        return Task.CompletedTask;
    }

    private void PerformOnDemandDiskCheck()
    {
        try
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            if (NativeApi.GetDiskFreeSpaceEx(systemDrive, out ulong freeBytes, out ulong totalBytes, out _) && totalBytes > 0)
            {
                double freeGB = freeBytes / (1024.0 * 1024.0 * 1024.0);
                double totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                double freePct = (freeGB / totalGB) * 100.0;

                if (freeGB < 5.0 || freePct < 8.0)
                {
                    DbManager.LogAction($"Low Disk Space detected on {systemDrive} (Free: {freeGB:F1} GB, {freePct:F1}%)", "Storage Check", "Warning");
                }
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("BackgroundWatchdogService.PerformOnDemandDiskCheck", ex);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}


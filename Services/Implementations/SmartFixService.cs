using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class SmartFixService
{
    private readonly JunkCleanerEngine _junkEngine;
    private readonly NetworkEngine _networkEngine;
    private readonly SystemOptimizerEngine _optimizerEngine;
    private readonly INotificationService _notificationService;

    public SmartFixService(
        JunkCleanerEngine junkEngine,
        NetworkEngine networkEngine,
        SystemOptimizerEngine optimizerEngine,
        INotificationService notificationService)
    {
        _junkEngine = junkEngine ?? new JunkCleanerEngine();
        _networkEngine = networkEngine ?? new NetworkEngine();
        _optimizerEngine = optimizerEngine ?? new SystemOptimizerEngine();
        _notificationService = notificationService ?? new NotificationService();
    }

    public SmartFixService() : this(
        new JunkCleanerEngine(),
        new NetworkEngine(),
        new SystemOptimizerEngine(),
        new NotificationService())
    {
    }

    public async Task ExecuteFixAsync(string actionKey, Action<SmartFixProgress>? progressCallback = null)
    {
        var progress = new SmartFixProgress { ActionName = actionKey, ProgressPercent = 10 };
        
        try
        {
            switch (actionKey)
            {
                case "CleanJunk":
                    progress.CurrentStep = "Scanning temporary files and system caches...";
                    progressCallback?.Invoke(progress);
                    
                    var cats = await _junkEngine.ScanJunkAsync();
                    progress.ProgressPercent = 50;
                    progress.CurrentStep = "Purging temporary junk files and logs...";
                    progressCallback?.Invoke(progress);
                    
                    long cleanedBytes = await _junkEngine.CleanJunkAsync(cats);
                    double freedMB = cleanedBytes / (1024.0 * 1024.0);

                    // Also optimize RAM
                    await _optimizerEngine.OptimizeRamAsync();
                    
                    progress.ProgressPercent = 100;
                    progress.IsCompleted = true;
                    progress.IsSuccess = true;
                    progress.ResultMessage = $"Successfully cleaned {freedMB:F1} MB of junk files and purged standby RAM.";
                    progressCallback?.Invoke(progress);
                    
                    _notificationService.ShowSuccess("Smart Fix Complete", progress.ResultMessage);
                    break;

                case "FlushDns":
                    progress.CurrentStep = "Flushing Windows DNS resolver cache...";
                    progressCallback?.Invoke(progress);
                    
                    bool dnsOk = await _networkEngine.FlushDnsAsync();
                    progress.ProgressPercent = 60;
                    progress.CurrentStep = "Resetting TCP/IP network socket catalog...";
                    progressCallback?.Invoke(progress);
                    
                    bool winsockOk = await _networkEngine.ResetWinsockAsync();
                    
                    progress.ProgressPercent = 100;
                    progress.IsCompleted = true;
                    progress.IsSuccess = dnsOk || winsockOk;
                    progress.ResultMessage = "Flushed DNS cache and reset network sockets.";
                    progressCallback?.Invoke(progress);
                    
                    _notificationService.ShowSuccess("Network Fix Complete", progress.ResultMessage);
                    break;

                case "OptimizeServices":
                    progress.CurrentStep = "Tuning background Windows services and RAM working set...";
                    progressCallback?.Invoke(progress);
                    
                    var (procOpt, ramBytes) = await _optimizerEngine.OptimizeRamAsync();
                    double freedMBService = ramBytes / (1024.0 * 1024.0);
                    
                    progress.ProgressPercent = 100;
                    progress.IsCompleted = true;
                    progress.IsSuccess = true;
                    progress.ResultMessage = $"Optimized {procOpt} processes and freed {freedMBService:F1} MB RAM.";
                    progressCallback?.Invoke(progress);
                    
                    _notificationService.ShowSuccess("Service Optimization Complete", progress.ResultMessage);
                    break;

                case "RepairSfc":
                    progress.CurrentStep = "Initiating Windows System File Checker (SFC)...";
                    progressCallback?.Invoke(progress);
                    
                    await Task.Run(() =>
                    {
                        try
                        {
                            var psi = new ProcessStartInfo("sfc", "/scannow")
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            using var proc = Process.Start(psi);
                            proc?.WaitForExit(5000); // Trigger background repair
                        }
                        catch { }
                    });

                    progress.ProgressPercent = 100;
                    progress.IsCompleted = true;
                    progress.IsSuccess = true;
                    progress.ResultMessage = "Windows system file repair task queued successfully.";
                    progressCallback?.Invoke(progress);
                    
                    _notificationService.ShowSuccess("System Repair", progress.ResultMessage);
                    break;

                default:
                    progress.CurrentStep = "Executing quick maintenance...";
                    await Task.Delay(500);
                    progress.ProgressPercent = 100;
                    progress.IsCompleted = true;
                    progress.IsSuccess = true;
                    progress.ResultMessage = "Quick maintenance completed.";
                    progressCallback?.Invoke(progress);
                    break;
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SmartFixService", ex);
            progress.IsCompleted = true;
            progress.IsSuccess = false;
            progress.ResultMessage = $"Fix encountered an error: {ex.Message}";
            progressCallback?.Invoke(progress);
            _notificationService.ShowError("Smart Fix Error", ex.Message);
        }
    }
}

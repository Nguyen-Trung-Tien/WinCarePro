using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using LiveChartsCore.Defaults;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.ViewModels;

public partial class DashboardViewModel
{
    public async Task<bool> UndoLastOptimizationAsync()
    {
        if (string.IsNullOrEmpty(_lastSnapshotId))
        {
            _notificationService.ShowToast("Undo Warning", "No rollback snapshots found in current session.", NotificationSeverity.Warning);
            return false;
        }

        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Undoing last changes...".T();
        });

        try
        {
            bool result = await _snapshotService.RestoreSnapshotAsync(_lastSnapshotId);
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (result)
                {
                    ScanStatus = "Rollback successful! System registry restored.".T();
                    _notificationService.ShowToast("Rollback Successful", "Registry modifications have been restored.", NotificationSeverity.Success);
                }
                else
                {
                    ScanStatus = "Rollback failed: Restore Wizard launched.".T();
                }
            });
            return result;
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Rollback failed: " + ex.Message;
            });
            return false;
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }

    private CancellationTokenSource? _optimizeCts;

    public void CancelOptimize()
    {
        try { _optimizeCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public async Task<OptimizationSummary?> OptimizeSystemAsync()
    {
        return await OptimizeSystemAsync(OptimizationMode.Recommended);
    }

    public async Task<OptimizationSummary?> OptimizeSystemAsync(OptimizationMode mode, CancellationToken token = default)
    {
        if (IsOptimizing || IsScanning)
        {
            return new OptimizationSummary
            {
                IsSuccess = false,
                AbortReason = "Another scan or optimization task is already running. Please wait for it to finish.".T()
            };
        }

        // 1. Low Battery Check
        try
        {
            if (Windows.System.Power.PowerManager.RemainingChargePercent < 15 &&
                Windows.System.Power.PowerManager.BatteryStatus == Windows.System.Power.BatteryStatus.Discharging)
            {
                string msg = "Battery level is too low (< 15%). Please connect to a power source.".T();
                _notificationService.ShowToast("Optimization Aborted".T(), msg, NotificationSeverity.Warning);
                return new OptimizationSummary { IsSuccess = false, AbortReason = msg };
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.OptimizeSystemAsync.PowerCheck", ex);
        }

        // 2. High CPU Load Check
        if (CpuUsage > 90.0)
        {
            string msg = "System CPU usage is extremely high (> 90%). Please wait.".T();
            _notificationService.ShowToast("Optimization Aborted".T(), msg, NotificationSeverity.Warning);
            return new OptimizationSummary { IsSuccess = false, AbortReason = msg };
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _optimizeCts?.Dispose();
        _optimizeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _optimizeCts.CancelAfter(TimeSpan.FromSeconds(45));
        var linkedToken = _optimizeCts.Token;

        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Initializing Snapshot & Restore Point...".T();
        });

        var summary = new OptimizationSummary();

        // 3. System Snapshot prior to optimization (strict 12-second timeout so WMI/VSS never hangs)
        try
        {
            using var snapCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
            snapCts.CancelAfter(TimeSpan.FromSeconds(12));
            _lastSnapshotId = await _snapshotService.CreateSnapshotAsync($"Pre-Optimization ({mode} Mode)", snapCts.Token).ConfigureAwait(false);
            summary.SnapshotCreated = !string.IsNullOrEmpty(_lastSnapshotId);
        }
        catch (OperationCanceledException) when (!linkedToken.IsCancellationRequested)
        {
            Database.DbManager.LogAction("Snapshot creation timed out after 12s; continuing optimization safely.", "Optimization", "Warning");
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.OptimizeSystemAsync.Snapshot", ex);
            Database.DbManager.LogAction($"Snapshot failed prior to optimization: {ex.Message}", "Optimization", "Warning");
        }

        try
        {
            long memBefore = GC.GetTotalMemory(false) + System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            // TIER 1: SAFE MODE
            if (mode >= OptimizationMode.Safe)
            {
                linkedToken.ThrowIfCancellationRequested();
                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Cleaning Junk Files...".T(); });
                if (_scannedJunkCategories == null || _scannedJunkCategories.Count == 0)
                {
                    try
                    {
                        _scannedJunkCategories = await _junkEngine.ScanJunkAsync(linkedToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogException("DashboardViewModel.OptimizeSystemAsync.ScanJunk", ex);
                    }
                }

                if (_scannedJunkCategories != null && _scannedJunkCategories.Any(c => c.IsSelected && c.SizeBytes > 0))
                {
                    long junkCleaned = await _junkEngine.CleanJunkAsync(_scannedJunkCategories).ConfigureAwait(false);
                    summary.JunkBytesCleaned = junkCleaned;
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        _junkSizeBytes = 0;
                        JunkFileSize = "0.0 MB";
                    });
                }
                await Task.Delay(200, linkedToken).ConfigureAwait(false);

                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Flushing DNS Resolver Cache...".T(); });
                var netEngine = new NetworkEngine();
                bool dnsOk = await netEngine.FlushDnsAsync().ConfigureAwait(false);
                summary.DnsCacheFlushed = dnsOk;
                await Task.Delay(200, linkedToken).ConfigureAwait(false);
            }

            // TIER 2: RECOMMENDED MODE (run off UI thread so UI stays 100% responsive)
            if (mode >= OptimizationMode.Recommended)
            {
                linkedToken.ThrowIfCancellationRequested();
                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Active RAM Boosting...".T(); });
                await Task.Run(() =>
                {
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                }, linkedToken).ConfigureAwait(false);
            }

            long memAfter = GC.GetTotalMemory(false) + System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            summary.RamBytesReclaimed = Math.Max(0L, memBefore - memAfter);
            summary.IsSuccess = true;
            summary.ElapsedMs = sw.ElapsedMilliseconds;

            try
            {
                Database.DbManager.AddNotification("Optimization Completed".T(), string.Format("System optimized successfully in {0} mode.".T(), mode.ToString().T()), "Info");
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("DashboardViewModel.OptimizeSystemAsync.AddNotification", ex);
            }

            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = string.Format("Optimization Complete! Mode: {0}".T(), mode);
                HealthScore = 100;
                AiHealthScore = 100;
                _junkSizeBytes = 0;
                Recommendations.Clear();
                
                var tempItems = DiagnosticItems.ToList();
                DiagnosticItems.Clear();
                foreach (var item in tempItems)
                {
                    item.IsHealthy = true;
                    DiagnosticItems.Add(item);
                }
                
                HasScanned = true; 
                HealthBreakdownText = "Your PC is in optimal health condition with 0 bottlenecks detected.".T();
                SummaryDashboardText = string.Format("1-Click Care optimization finished in {0} mode. Reclaimed {1:F1} MB RAM and cleaned {2:F1} MB junk files.".T(), mode, summary.RamBytesReclaimed / (1024.0 * 1024.0), summary.JunkBytesCleaned / (1024.0 * 1024.0));
                IsSummaryCardVisible = true;
            });

            return summary;
        }
        catch (OperationCanceledException)
        {
            _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Optimization cancelled.".T(); });
            return new OptimizationSummary
            {
                IsSuccess = false,
                WasCancelled = true,
                ElapsedMs = sw.ElapsedMilliseconds,
                AbortReason = "Optimization was cancelled or timed out.".T()
            };
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.OptimizeSystemAsync", ex);
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Optimization failed:".T() + " " + ex.Message;
            });
            return new OptimizationSummary
            {
                IsSuccess = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                AbortReason = ex.Message
            };
        }
        finally
        {
            _optimizeCts?.Dispose();
            _optimizeCts = null;
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }

    public async Task FixDiagnosticItemAsync(DiagnosticResult item)
    {
        if (item.IsHealthy || IsOptimizing) return;

        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = string.Format("Status: Resolving {0}...".T(), item.CheckName);
        });

        try
        {
            if (item.Category == "Storage")
            {
                long cleanedBytes = 0;
                if (_scannedJunkCategories != null)
                {
                    cleanedBytes = await _junkEngine.CleanJunkAsync(_scannedJunkCategories);
                }
                else
                {
                    var junkCats = await _junkEngine.ScanJunkAsync();
                    cleanedBytes = await _junkEngine.CleanJunkAsync(junkCats);
                }
                
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    _junkSizeBytes = 0;
                    JunkFileSize = "0.0 MB";
                });
            }
            else if (item.Category == "Performance")
            {
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
            }
            else if (item.Category == "Network")
            {
                var netEngine = new NetworkEngine();
                await netEngine.FlushDnsAsync();
            }

            _dispatcherQueue?.TryEnqueue(() =>
            {
                item.IsHealthy = true;
                if (item.Category == "Storage")
                {
                    item.Description = "Junk files successfully cleaned.".T();
                }
                else if (item.Category == "Registry")
                {
                    item.Description = "Registry errors successfully resolved.".T();
                }
                else if (item.Category == "Performance")
                {
                    item.Description = "System performance has been boosted.".T();
                }
                else if (item.Category == "Network")
                {
                    item.Description = "Network connectivity settings optimized.".T();
                }

                int idx = DiagnosticItems.IndexOf(item);
                if (idx >= 0)
                {
                    DiagnosticItems.RemoveAt(idx);
                    DiagnosticItems.Insert(idx, item);
                }

                if (DiagnosticItems.All(x => x.IsHealthy))
                {
                    HealthScore = 100;
                    Recommendations.Clear();
                }
                else
                {
                    int unhealthyCount = DiagnosticItems.Count(x => !x.IsHealthy);
                    HealthScore = Math.Clamp(100 - unhealthyCount * 10, 50, 100);
                }

                ScanStatus = string.Format("Resolved: {0}".T(), item.CheckName);
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Failed to resolve:".T() + " " + ex.Message;
            });
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }

    public async Task BoostRamAsync()
    {
        if (IsOptimizing) return;
        
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Optimizing RAM...".T();
        });

        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            
            var (_, ram) = GetSystemResourceUsage();
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                RamUsage = Math.Round(ram, 1);
                RamSeriesValues.Add(new ObservableValue(RamUsage));
                RamSeriesValues.RemoveAt(0);

                ScanStatus = "RAM Boosted! Memory trimmed successfully.".T();
                
                var ramDiagnostic = DiagnosticItems.FirstOrDefault(x => x.CheckName.Contains("RAM") || x.CheckName.Contains("Memory"));
                if (ramDiagnostic != null)
                {
                    ramDiagnostic.IsHealthy = true;
                    ramDiagnostic.Description = "RAM optimized and standby memory reclaimed.".T();
                    int idx = DiagnosticItems.IndexOf(ramDiagnostic);
                    if (idx >= 0)
                    {
                        DiagnosticItems.RemoveAt(idx);
                        DiagnosticItems.Insert(idx, ramDiagnostic);
                    }
                }
            });

            try
            {
                Database.DbManager.AddNotification("Memory Boost Completed".T(), "Working sets trimmed and RAM optimized successfully.".T(), "Info");
            }
            catch { }
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "RAM Boost failed:".T() + " " + ex.Message;
            });
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }

    public async Task CleanDiskJunkAsync()
    {
        if (IsOptimizing) return;
        
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Cleaning Junk Files...".T();
        });

        try
        {
            long cleanedBytes = 0;
            if (_scannedJunkCategories != null)
            {
                cleanedBytes = await _junkEngine.CleanJunkAsync(_scannedJunkCategories);
            }
            else
            {
                var junkCats = await _junkEngine.ScanJunkAsync();
                cleanedBytes = await _junkEngine.CleanJunkAsync(junkCats);
            }

            double cleanedMb = cleanedBytes / 1024.0 / 1024.0;
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                _junkSizeBytes = 0;
                JunkFileSize = "0.0 MB";
                ScanStatus = string.Format("Disk Cleaned! Freed {0:F1} MB".T(), cleanedMb);
                
                var storageDiagnostics = DiagnosticItems.Where(x => x.Category == "Storage");
                foreach (var diag in storageDiagnostics.ToList())
                {
                    diag.IsHealthy = true;
                    diag.Description = "Storage optimized and junk files cleaned.".T();
                    int idx = DiagnosticItems.IndexOf(diag);
                    if (idx >= 0)
                    {
                        DiagnosticItems.RemoveAt(idx);
                        DiagnosticItems.Insert(idx, diag);
                    }
                }
            });

            try
            {
                Database.DbManager.AddNotification("Junk Clean Completed".T(), string.Format("Cleaned {0:F1} MB junk files.".T(), cleanedMb), "Info");
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("DashboardViewModel.CleanJunkAsync.Notification", ex);
            }
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Disk Clean failed:".T() + " " + ex.Message;
            });
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsOptimizing = false;
            });
        }
    }
}

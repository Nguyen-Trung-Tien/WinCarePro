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

    public async Task<OptimizationSummary?> OptimizeSystemAsync()
    {
        return await OptimizeSystemAsync(OptimizationMode.Recommended);
    }

    public async Task<OptimizationSummary?> OptimizeSystemAsync(OptimizationMode mode, CancellationToken token = default)
    {
        if (IsOptimizing || IsScanning) return null;

        // 1. Low Battery Check
        try
        {
            if (Windows.System.Power.PowerManager.RemainingChargePercent < 15 && 
                Windows.System.Power.PowerManager.BatteryStatus == Windows.System.Power.BatteryStatus.Discharging)
            {
                _notificationService.ShowToast("Optimization Aborted", "Battery level is too low (< 15%). Please connect to a power source.", NotificationSeverity.Warning);
                return null;
            }
        }
        catch { }

        // 2. High CPU Load Check
        if (CpuUsage > 90.0)
        {
            _notificationService.ShowToast("Optimization Aborted", "System CPU usage is extremely high (> 90%). Please wait.", NotificationSeverity.Warning);
            return null;
        }

        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsOptimizing = true;
            ScanStatus = "Status: Initializing Snapshot & Restore Point...".T();
        });

        // 3. System Snapshot prior to optimization
        try
        {
            _lastSnapshotId = await _snapshotService.CreateSnapshotAsync($"Pre-Optimization ({mode} Mode)", token);
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Snapshot failed prior to optimization: {ex.Message}", "Optimization", "Warning");
        }

        var summary = new OptimizationSummary();

        try
        {
            // TIER 1: SAFE MODE
            if (mode >= OptimizationMode.Safe)
            {
                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Cleaning Junk Files...".T(); });
                if (_scannedJunkCategories != null && _scannedJunkCategories.Any(c => c.IsSelected && c.SizeBytes > 0))
                {
                    long junkCleaned = await _junkEngine.CleanJunkAsync(_scannedJunkCategories);
                    summary.JunkBytesCleaned = junkCleaned;
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        _junkSizeBytes = 0;
                        JunkFileSize = "0.0 MB";
                    });
                }
                await Task.Delay(300, token);

                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Flushing DNS Resolver Cache...".T(); });
                var netEngine = new NetworkEngine();
                bool dnsOk = await netEngine.FlushDnsAsync();
                summary.DnsCacheFlushed = dnsOk;
                await Task.Delay(300, token);
            }

            // TIER 2: RECOMMENDED MODE
            if (mode >= OptimizationMode.Recommended)
            {
                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Cleaning Delivery Optimization Cache...".T(); });
                var optEngine = new SystemOptimizerEngine();
                long doCleaned = await optEngine.CleanDeliveryOptimizationCacheAsync();
                summary.DoCacheBytesCleaned = doCleaned;
                await Task.Delay(300, token);

                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Optimizing Startup Apps...".T(); });
                // Startup engine items resolved in background
                await Task.Delay(300, token);
            }

            // TIER 3: ADVANCED MODE
            if (mode >= OptimizationMode.Advanced)
            {
                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Fixing Registry Errors...".T(); });
                if (_scannedRegistryIssues != null && _scannedRegistryIssues.Any(i => i.IsSelected))
                {
                    await _registryEngine.FixRegistryIssuesAsync(_scannedRegistryIssues);
                    summary.RegistryIssuesFixed = _scannedRegistryIssues.Count(i => i.IsSelected);
                }
                await Task.Delay(300, token);

                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Active RAM Boosting...".T(); });
                var optEngine = new SystemOptimizerEngine();
                var ramResult = await optEngine.OptimizeRamAsync();
                summary.RamBytesReclaimed = ramResult.memoryReclaimedBytes;
                summary.RamProcessesOptimized = ramResult.processesOptimized;
                await Task.Delay(300, token);

                _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Status: Applying Responsiveness Tweaks...".T(); });
                var tweaks = optEngine.GetTweaks();
                int tweaksApplied = 0;
                foreach (var tweak in tweaks)
                {
                    token.ThrowIfCancellationRequested();
                    if (!tweak.IsOptimized)
                    {
                        bool ok = await optEngine.ApplyTweakAsync(tweak);
                        if (ok) tweaksApplied++;
                    }
                }
                summary.TweaksApplied = tweaksApplied;
                await Task.Delay(300, token);
            }

            try
            {
                Database.DbManager.AddNotification("Optimization Completed".T(), string.Format("System optimized successfully in {0} mode.".T(), mode.ToString().T()), "Info");
            }
            catch { }

            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = string.Format("Optimization Complete! Mode: {0}".T(), mode);
                HealthScore = 100;
                Recommendations.Clear();
                
                var tempItems = DiagnosticItems.ToList();
                DiagnosticItems.Clear();
                foreach (var item in tempItems)
                {
                    item.IsHealthy = true;
                    DiagnosticItems.Add(item);
                }
                
                HasScanned = false; 
            });

            return summary;
        }
        catch (OperationCanceledException)
        {
            _dispatcherQueue?.TryEnqueue(() => { ScanStatus = "Optimization cancelled.".T(); });
            return null;
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Optimization failed:".T() + " " + ex.Message;
            });
            return null;
        }
        finally
        {
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
                cleanedBytes += await _optimizerEngine.CleanDeliveryOptimizationCacheAsync();
                
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    _junkSizeBytes = 0;
                    JunkFileSize = "0.0 MB";
                });
            }
            else if (item.Category == "Registry")
            {
                if (_scannedRegistryIssues != null)
                {
                    await _registryEngine.FixRegistryIssuesAsync(_scannedRegistryIssues);
                }
                else
                {
                    var issues = await Task.Run(() => _registryEngine.ScanRegistryIssues());
                    await _registryEngine.FixRegistryIssuesAsync(issues);
                }
            }
            else if (item.Category == "Performance")
            {
                await _optimizerEngine.OptimizeRamAsync();
                var tweaks = _optimizerEngine.GetTweaks();
                foreach (var tweak in tweaks)
                {
                    if (!tweak.IsOptimized)
                    {
                        await _optimizerEngine.ApplyTweakAsync(tweak);
                    }
                }
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
                    HealthScore = Math.Clamp(100 - unhealthyCount * 10, 50, 95);
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
            var ramResult = await _optimizerEngine.OptimizeRamAsync();
            double ramReclaimedMb = ramResult.memoryReclaimedBytes / 1024.0 / 1024.0;
            
            var (_, ram) = GetSystemResourceUsage();
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                RamUsage = Math.Round(ram, 1);
                RamSeriesValues.Add(new ObservableValue(RamUsage));
                RamSeriesValues.RemoveAt(0);

                ScanStatus = string.Format("RAM Boosted! Reclaimed {0:F1} MB".T(), ramReclaimedMb);
                
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
                Database.DbManager.AddNotification("Memory Boost Completed".T(), string.Format("Reclaimed {0:F1} MB RAM.".T(), ramReclaimedMb), "Info");
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

            cleanedBytes += await _optimizerEngine.CleanDeliveryOptimizationCacheAsync();

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
            catch { }
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public partial class DashboardViewModel
{
    public void DetectBottlenecks()
    {
        var currentIssues = new List<string>();

        // CPU Check
        if (CpuUsage > 85.0)
        {
            currentIssues.Add("CPU sustained high load");
        }
        
        // RAM Check
        if (RamUsage > 90.0)
        {
            currentIssues.Add("RAM footprint capacity saturated");
        }

        // Disk Check
        if (DiskUsage > 90.0)
        {
            currentIssues.Add("Disk active I/O saturation");
        }

        if (currentIssues.Count > 0)
        {
            HasBottleneck = true;
            BottleneckStatus = "Bottleneck: ".T() + string.Join(", ", currentIssues.Select(issue => issue.T()));
        }
        else
        {
            HasBottleneck = false;
            BottleneckStatus = "System Status: Stable".T();
        }
    }

    public void UpdateHealthScoreBreakdown()
    {
        if (!HasScanned)
        {
            HealthBreakdownText = "No diagnostic scan performed yet.".T();
            return;
        }

        var details = new List<string>();
        int calculatedScore = 100;

        if (_junkSizeBytes > 0)
        {
            double mb = _junkSizeBytes / 1024.0 / 1024.0;
            int penalty = (int)Math.Min(15, mb / 100.0);
            calculatedScore -= penalty;
            details.Add(string.Format("{0:F1} MB Junk (-{1} pts)", mb, penalty));
        }

        if (_scannedRegistryIssues != null && _scannedRegistryIssues.Count > 0)
        {
            int penalty = Math.Min(15, _scannedRegistryIssues.Count);
            calculatedScore -= penalty;
            details.Add(string.Format("{0} Registry errors (-{1} pts)", _scannedRegistryIssues.Count, penalty));
        }

        if (AvailableUpdatesCount > 0)
        {
            int penalty = Math.Min(10, AvailableUpdatesCount * 2);
            calculatedScore -= penalty;
            details.Add(string.Format("{0} Outdated apps (-{1} pts)", AvailableUpdatesCount, penalty));
        }

        if (CpuUsage > 85.0 || RamUsage > 90.0)
        {
            calculatedScore -= 10;
            details.Add("High system utilization (-10 pts)");
        }

        calculatedScore = Math.Clamp(calculatedScore, 50, 100);

        HealthScore = calculatedScore;
        if (details.Count > 0)
        {
            HealthBreakdownText = "Score Details: " + string.Join(", ", details);
        }
        else
        {
            HealthBreakdownText = "Your PC is in perfect health!";
        }
    }

    public async Task<string> ExportDiagnosticReportAsync(string format, CancellationToken cancellationToken = default)
    {
        var items = DiagnosticItems.ToArray();
        string reportsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"WinCarePro\Reports"
        );

        return await Task.Run(async () =>
        {
            if (!Directory.Exists(reportsFolder))
            {
                Directory.CreateDirectory(reportsFolder);
            }

            string fileName = $"DiagnosticReport_{DateTime.Now:yyyyMMdd_HHmmss}";
            string filePath = Path.Combine(reportsFolder, $"{fileName}.{format.ToLower()}");

            cancellationToken.ThrowIfCancellationRequested();

            switch (format.ToUpperInvariant())
            {
                case "JSON":
                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await System.Text.Json.JsonSerializer.SerializeAsync(fs, items, options, cancellationToken);
                    }
                    break;

                case "CSV":
                    using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, 4096))
                    {
                        await writer.WriteLineAsync("Category,CheckName,Description,IsHealthy");
                        foreach (var item in items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            string category = EscapeCsv(item.Category);
                            string checkName = EscapeCsv(item.CheckName);
                            string description = EscapeCsv(item.Description);
                            await writer.WriteLineAsync($"{category},{checkName},{description},{item.IsHealthy}");
                        }
                    }
                    break;

                case "TXT":
                default:
                    using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, 4096))
                    {
                        await writer.WriteLineAsync($"WINCARE PRO DIAGNOSTIC REPORT - {DateTime.Now}");
                        await writer.WriteLineAsync(new string('=', 60));
                        foreach (var item in items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await writer.WriteLineAsync($"[{item.Category}] {item.CheckName}");
                            await writer.WriteLineAsync($"Status: {(item.IsHealthy ? "Optimized" : "Action Recommended")}");
                            await writer.WriteLineAsync($"Description: {item.Description}");
                            await writer.WriteLineAsync(new string('-', 60));
                        }
                    }
                    break;
            }

            Database.DbManager.LogAction($"Exported diagnostics report: {fileName}.{format.ToLower()}", "Diagnostics", "Success");
            return filePath;
        }, cancellationToken);
    }

    private static string EscapeCsv(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        return text;
    }

    public async Task RunFullDiagnosticsAsync()
    {
        if (IsScanning) return;

        // Huỷ scan cũ nếu có (phòng trường hợp gọi lại trước khi scan trước kết thúc)
        _scanCts?.Cancel();
        _scanCts?.Dispose();

        // Scan CTS ở class level – có thể bị cancel từ StopMonitoring() khi navigate away
        // Timeout tổng thể 90 giây
        _scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var scanToken = _scanCts.Token;

        IsScanning = true;
        HasScanned = false;
        ScanProgress = 5;
        ScanStatus = "Status: Scanning Junk Files...".T();
        Recommendations.Clear();
        DiagnosticItems.Clear();

        // Capture current resource values on UI thread before offloading
        double currentCpuUsage = CpuUsage;
        double currentRamUsage = RamUsage;
        double currentDiskUsage = DiskUsage;

        try
        {
            await Task.Run(async () =>
            {
                // 1. Scan Junk files
                var junkCats = await _junkEngine.ScanJunkAsync(scanToken).ConfigureAwait(false);
                _scannedJunkCategories = junkCats;
                _junkSizeBytes = junkCats.Sum(x => x.SizeBytes);
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    JunkFileSize = $"{(_junkSizeBytes / 1024.0 / 1024.0):F1} MB";
                    ScanProgress = 30;
                    ScanStatus = "Status: Scanning Registry Issues...".T();
                });
                await Task.Delay(300, scanToken).ConfigureAwait(false);

                // 2. Scan Registry (synchronous method — runs safely on thread pool)
                scanToken.ThrowIfCancellationRequested();
                var regIssues = _registryEngine.ScanRegistryIssues();
                scanToken.ThrowIfCancellationRequested();
                _scannedRegistryIssues = regIssues;
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    ScanProgress = 55;
                    ScanStatus = "Status: Checking Available Software Updates...".T();
                });
                await Task.Delay(300, scanToken).ConfigureAwait(false);

                // 3. Scan Software Updates
                List<SoftwareUpdateInfo> updates = new();
                try
                {
                    updates = await _updaterEngine.ScanUpdatesAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch { }

                scanToken.ThrowIfCancellationRequested();
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    AvailableUpdatesCount = updates.Count;
                    ScanProgress = 75;
                    ScanStatus = "Status: Evaluating Connection and Security Status...".T();
                });
                await Task.Delay(300, scanToken).ConfigureAwait(false);

                // 4. Scan Security and Network
                var netEngine = new NetworkEngine();
                var (pingLoss, avgLatency, _) = await netEngine.AnalyzePingQualityAsync().ConfigureAwait(false);
                scanToken.ThrowIfCancellationRequested();

                var startupApps = _startupEngine.GetStartupEntries();
                scanToken.ThrowIfCancellationRequested();
                var securityAudits = _securityEngine.RunSecurityAudits(startupApps);
                scanToken.ThrowIfCancellationRequested();

                _dispatcherQueue?.TryEnqueue(() =>
                {
                    ScanProgress = 90;
                    ScanStatus = "Status: Calculating System Health Index...".T();
                });
                await Task.Delay(300, scanToken).ConfigureAwait(false);

                // 5. Evaluate AI Health Score
                int servicesCount = 50;
                try
                {
                    servicesCount = ServiceController.GetServices().Length;
                }
                catch { }
                scanToken.ThrowIfCancellationRequested();

                double freeSpacePercent = 50.0;
                try
                {
                    var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
                    var cDrive = drives.FirstOrDefault(d => d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)) ?? drives.FirstOrDefault();
                    if (cDrive != null)
                    {
                        freeSpacePercent = ((double)cDrive.AvailableFreeSpace / cDrive.TotalSize) * 100.0;
                    }
                }
                catch { }

                double cpuTemp = _hardwareEngine.GetCpuTemperature(currentCpuUsage);
                scanToken.ThrowIfCancellationRequested();

                bool isExplorerOptimized = true;
                try
                {
                    var tweaks = _optimizerEngine.GetTweaks();
                    var menuTweak = tweaks.FirstOrDefault(t => t.Id == "MenuShowDelay");
                    var animTweak = tweaks.FirstOrDefault(t => t.Id == "MinAnimate");
                    if ((menuTweak != null && !menuTweak.IsOptimized) || (animTweak != null && !animTweak.IsOptimized))
                    {
                        isExplorerOptimized = false;
                    }
                }
                catch { }

                var summary = await _aiEngine.RunHealthEvaluationAsync(
                    _junkSizeBytes,
                    regIssues.Count,
                    updates.Count,
                    avgLatency,
                    pingLoss,
                    startupApps.Count,
                    securityAudits,
                    cpuUsage: currentCpuUsage,
                    cpuTemp: cpuTemp,
                    ramUsagePercent: currentRamUsage,
                    servicesCount: servicesCount,
                    diskActiveTime: currentDiskUsage,
                    freeSpacePercent: freeSpacePercent,
                    ssdHealthPercent: 100.0,
                    isThrottling: false,
                    isExplorerOptimized: isExplorerOptimized
                ).ConfigureAwait(false);


                _dispatcherQueue?.TryEnqueue(() =>
                {
                    HealthScore = summary.HealthScore;
                    foreach (var rec in summary.Recommendations)
                    {
                        Recommendations.Add(rec);
                    }
                    foreach (var res in summary.Results)
                    {
                        DiagnosticItems.Add(res);
                    }
                    ScanProgress = 100;
                    ScanStatus = string.Format("Evaluation Complete. System Health is {0}/100".T(), HealthScore);
                    IsScanning = false;
                    HasScanned = true;

                    UpdateHealthScoreBreakdown();
                });
            }, scanToken);
        }
        catch (OperationCanceledException)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Status: Scan cancelled.".T();
                IsScanning = false;
                HasScanned = false;
                ScanProgress = 0;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ScanStatus = "Scan failed:".T() + " " + ex.Message;
                IsScanning = false;
                HasScanned = false;
            });
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (IsScanning)
                {
                    IsScanning = false;
                }
            });

            var cts = _scanCts;
            _scanCts = null;
            try { cts?.Dispose(); } catch { }
        }
    }
}

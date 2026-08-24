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
        var details = new List<string>();
        int calculatedScore = 100;

        // 1. Live Telemetry Load Deductions
        if (CpuUsage > 85.0)
        {
            calculatedScore -= 15;
            details.Add(string.Format("High CPU load: {0:F0}% (-15 pts)".T(), CpuUsage));
        }
        else if (CpuUsage > 65.0)
        {
            calculatedScore -= 5;
            details.Add(string.Format("Moderate CPU: {0:F0}% (-5 pts)".T(), CpuUsage));
        }

        if (RamUsage > 88.0)
        {
            calculatedScore -= 15;
            details.Add(string.Format("RAM memory saturated: {0:F0}% (-15 pts)".T(), RamUsage));
        }
        else if (RamUsage > 75.0)
        {
            calculatedScore -= 8;
            details.Add(string.Format("Elevated RAM usage: {0:F0}% (-8 pts)".T(), RamUsage));
        }

        // 2. Drive C: Free Space Assessment
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            var cDrive = drives.FirstOrDefault(d => d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)) ?? drives.FirstOrDefault();
            if (cDrive != null)
            {
                double freeGB = cDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                if (freeGB < 10.0)
                {
                    calculatedScore -= 20;
                    details.Add(string.Format("Critical disk space: {0:F1} GB free (-20 pts)".T(), freeGB));
                }
                else if (freeGB < 25.0)
                {
                    calculatedScore -= 10;
                    details.Add(string.Format("Low disk space: {0:F1} GB free (-10 pts)".T(), freeGB));
                }
            }
        }
        catch { }

        // 3. Junk Files Deductions
        if (_junkSizeBytes > 0)
        {
            double mb = _junkSizeBytes / 1024.0 / 1024.0;
            int penalty = (int)Math.Clamp(mb / 150.0, 2, 20);
            calculatedScore -= penalty;
            details.Add(string.Format("{0:F1} MB Junk (-{1} pts)".T(), mb, penalty));
        }

        // 4. Registry Issues Deductions
        if (_scannedRegistryIssues != null && _scannedRegistryIssues.Count > 0)
        {
            int penalty = Math.Min(15, _scannedRegistryIssues.Count);
            calculatedScore -= penalty;
            details.Add(string.Format("{0} Registry errors (-{1} pts)".T(), _scannedRegistryIssues.Count, penalty));
        }

        // 5. Outdated Software Deductions
        if (AvailableUpdatesCount > 0)
        {
            int penalty = Math.Min(10, AvailableUpdatesCount * 2);
            calculatedScore -= penalty;
            details.Add(string.Format("{0} Outdated apps (-{1} pts)".T(), AvailableUpdatesCount, penalty));
        }

        calculatedScore = Math.Clamp(calculatedScore, 20, 100);

        // If AI Health Engine already produced a deep neural score and we haven't scanned yet, blend it
        if (!HasScanned && AiHealthScore > 0)
        {
            HealthScore = Math.Min(calculatedScore, AiHealthScore);
        }
        else
        {
            HealthScore = calculatedScore;
        }

        if (details.Count > 0)
        {
            HealthBreakdownText = "Score Details: ".T() + string.Join(" • ", details);
        }
        else
        {
            HealthBreakdownText = "Your PC is in optimal health condition with 0 bottlenecks detected.".T();
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

                case "HTML":
                case "HTM":
                    var htmlBuilder = new System.Text.StringBuilder();
                    string scoreColor = HealthScore >= 80 ? "#10B981" : (HealthScore >= 50 ? "#F59E0B" : "#EF4444");
                    string scoreBg = HealthScore >= 80 ? "rgba(16, 185, 129, 0.15)" : (HealthScore >= 50 ? "rgba(245, 158, 11, 0.15)" : "rgba(239, 68, 68, 0.15)");

                    htmlBuilder.AppendLine("<!DOCTYPE html>");
                    htmlBuilder.AppendLine("<html lang=\"en\">");
                    htmlBuilder.AppendLine("<head>");
                    htmlBuilder.AppendLine("  <meta charset=\"UTF-8\">");
                    htmlBuilder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                    htmlBuilder.AppendLine("  <title>WinCare Pro — System Diagnostics Report</title>");
                    htmlBuilder.AppendLine("  <style>");
                    htmlBuilder.AppendLine("    * { box-sizing: border-box; margin: 0; padding: 0; }");
                    htmlBuilder.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #0f172a; color: #f8fafc; padding: 32px 16px; line-height: 1.6; }");
                    htmlBuilder.AppendLine("    .container { max-width: 900px; margin: 0 auto; background: rgba(30, 41, 59, 0.75); backdrop-filter: blur(16px); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 20px; padding: 32px; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5); }");
                    htmlBuilder.AppendLine("    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid rgba(255, 255, 255, 0.1); padding-bottom: 24px; margin-bottom: 28px; }");
                    htmlBuilder.AppendLine("    .title { font-size: 24px; font-weight: 700; background: linear-gradient(135deg, #a78bfa, #60a5fa); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }");
                    htmlBuilder.AppendLine("    .score-badge { display: inline-flex; align-items: center; gap: 8px; padding: 8px 18px; border-radius: 9999px; font-weight: 700; font-size: 18px; }");
                    htmlBuilder.AppendLine("    .section-title { font-size: 16px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #94a3b8; margin: 24px 0 12px 0; }");
                    htmlBuilder.AppendLine("    .diag-item { background: rgba(15, 23, 42, 0.5); border: 1px solid rgba(255, 255, 255, 0.05); border-radius: 12px; padding: 16px; margin-bottom: 10px; }");
                    htmlBuilder.AppendLine("    .diag-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }");
                    htmlBuilder.AppendLine("    .badge-healthy { color: #10B981; background: rgba(16, 185, 129, 0.15); padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600; }");
                    htmlBuilder.AppendLine("    .badge-warn { color: #F59E0B; background: rgba(245, 158, 11, 0.15); padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600; }");
                    htmlBuilder.AppendLine("    .footer { text-align: center; margin-top: 32px; font-size: 12px; color: #64748b; border-top: 1px solid rgba(255, 255, 255, 0.08); padding-top: 20px; }");
                    htmlBuilder.AppendLine("  </style>");
                    htmlBuilder.AppendLine("</head>");
                    htmlBuilder.AppendLine("<body>");
                    htmlBuilder.AppendLine("  <div class=\"container\">");
                    htmlBuilder.AppendLine("    <div class=\"header\">");
                    htmlBuilder.AppendLine("      <div>");
                    htmlBuilder.AppendLine("        <div class=\"title\">🚀 WinCare Pro System Health Report</div>");
                    htmlBuilder.AppendLine($"        <div style=\"color: #64748b; font-size: 13px; margin-top: 4px;\">Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | User: {System.Net.WebUtility.HtmlEncode(Environment.UserName)}</div>");
                    htmlBuilder.AppendLine("      </div>");
                    htmlBuilder.AppendLine($"      <div class=\"score-badge\" style=\"background: {scoreBg}; color: {scoreColor}; border: 1px solid {scoreColor};\">Health Score: {HealthScore}/100</div>");
                    htmlBuilder.AppendLine("    </div>");
                    htmlBuilder.AppendLine("    <div class=\"section-title\">Diagnostic Findings & Health Index</div>");
                    foreach (var item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string statusClass = item.IsHealthy ? "badge-healthy" : "badge-warn";
                        string statusText = item.IsHealthy ? "OPTIMIZED" : "ACTION RECOMMENDED";
                        htmlBuilder.AppendLine("    <div class=\"diag-item\">");
                        htmlBuilder.AppendLine($"      <div class=\"diag-header\"><strong style=\"font-size: 14px;\">[{System.Net.WebUtility.HtmlEncode(item.Category)}] {System.Net.WebUtility.HtmlEncode(item.CheckName)}</strong><span class=\"{statusClass}\">{statusText}</span></div>");
                        htmlBuilder.AppendLine($"      <div style=\"font-size: 13px; color: #cbd5e1; margin-top: 4px;\">{System.Net.WebUtility.HtmlEncode(item.Description)}</div>");
                        htmlBuilder.AppendLine("    </div>");
                    }
                    htmlBuilder.AppendLine("    <div class=\"footer\">WinCare Pro Suite • Aura Glassmorphic Architecture • Diagnostics Engine Report</div>");
                    htmlBuilder.AppendLine("  </div>");
                    htmlBuilder.AppendLine("</body>");
                    htmlBuilder.AppendLine("</html>");

                    using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, 4096))
                    {
                        await writer.WriteAsync(htmlBuilder.ToString());
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
                List<JunkCategory> junkCats = new();
                try
                {
                    junkCats = await _junkEngine.ScanJunkAsync(scanToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Junk scan fallback: " + ex.Message);
                }

                _scannedJunkCategories = junkCats;
                _junkSizeBytes = junkCats.Sum(x => x.SizeBytes);
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    JunkFileSize = $"{(_junkSizeBytes / 1024.0 / 1024.0):F1} MB";
                    ScanProgress = 30;
                    ScanStatus = "Status: Scanning Registry Issues...".T();
                });
                await Task.Delay(250, scanToken).ConfigureAwait(false);

                // 2. Scan Registry (synchronous method — runs safely on thread pool)
                scanToken.ThrowIfCancellationRequested();
                List<RegistryIssue> regIssues = new();
                try
                {
                    regIssues = _registryEngine.ScanRegistryIssues();
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Registry scan fallback: " + ex.Message);
                }

                _scannedRegistryIssues = regIssues;
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    ScanProgress = 55;
                    ScanStatus = "Status: Checking Available Software Updates...".T();
                });
                await Task.Delay(250, scanToken).ConfigureAwait(false);

                // 3. Scan Software Updates
                List<SoftwareUpdateInfo> updates = new();
                try
                {
                    updates = await _updaterEngine.ScanUpdatesAsync(cancellationToken: scanToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Software updates scan fallback: " + ex.Message);
                }

                scanToken.ThrowIfCancellationRequested();
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    AvailableUpdatesCount = updates.Count;
                    ScanProgress = 75;
                    ScanStatus = "Status: Evaluating Connection and Security Status...".T();
                });
                await Task.Delay(250, scanToken).ConfigureAwait(false);

                // 4. Scan Security and Network
                double avgLatency = 15.0;
                double pingLoss = 0.0;
                try
                {
                    var netEngine = new NetworkEngine();
                    var pingResult = await netEngine.AnalyzePingQualityAsync().ConfigureAwait(false);
                    avgLatency = pingResult.avgLatencyMs;
                    pingLoss = pingResult.packetLossPercent;
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                scanToken.ThrowIfCancellationRequested();

                List<StartupEntry> startupApps = new();
                try
                {
                    startupApps = _startupEngine.GetStartupEntries();
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                scanToken.ThrowIfCancellationRequested();

                List<string> securityAudits = new();
                try
                {
                    securityAudits = _securityEngine.RunSecurityAudits(startupApps);
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                scanToken.ThrowIfCancellationRequested();

                _dispatcherQueue?.TryEnqueue(() =>
                {
                    ScanProgress = 90;
                    ScanStatus = "Status: Calculating System Health Index...".T();
                });
                await Task.Delay(250, scanToken).ConfigureAwait(false);

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
                    if (cDrive != null && cDrive.TotalSize > 0)
                    {
                        freeSpacePercent = ((double)cDrive.AvailableFreeSpace / cDrive.TotalSize) * 100.0;
                    }
                }
                catch { }

                double cpuTemp = 45.0;
                try
                {
                    cpuTemp = _hardwareEngine.GetCpuTemperature(currentCpuUsage);
                }
                catch { }
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

                double ssdHealth = 100;
                bool isThrottling = false;
                try
                {
                    ssdHealth = _hardwareEngine.GetSsdHealthPercent();
                    isThrottling = _hardwareEngine.IsCpuThrottling(currentCpuUsage);
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
                    ssdHealthPercent: ssdHealth,
                    isThrottling: isThrottling,
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
                    IsScanning = false;
                    HasScanned = true;

                    UpdateHealthScoreBreakdown();
                    ScanStatus = string.Format("Evaluation Complete. System Health is {0}/100".T(), HealthScore);
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

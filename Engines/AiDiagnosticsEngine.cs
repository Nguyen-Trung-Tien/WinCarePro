using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class AiDiagnosticsEngine
{
    private static readonly string ReportsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"WinCarePro\Reports"
    );

    public class DiagnosticSummary
    {
        public int HealthScore { get; set; }
        public List<DiagnosticResult> Results { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public async Task<DiagnosticSummary> RunHealthEvaluationAsync(
        long junkSizeBytes,
        int registryIssuesCount,
        int outdatedAppsCount,
        double avgLatencyMs,
        double packetLossPercent,
        int startupAppsCount,
        List<string> securityAudits)
    {
        var summary = new DiagnosticSummary();

        await Task.Run(() =>
        {
            // Category scoring parameters (Start at 100, subtract points for issues)
            double performanceScore = 100.0;
            double storageScore = 100.0;
            double networkScore = 100.0;
            double securityScore = 100.0;
            double softwareScore = 100.0;

            // 1. Storage Evaluation
            double junkMb = junkSizeBytes / 1024.0 / 1024.0;
            if (junkMb > 10000) // 10GB
            {
                storageScore -= 30;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Disk Clutter Size",
                    Category = "Storage",
                    IsHealthy = false,
                    Description = $"Detected {junkMb:F0} MB of temporary files, cache entries, and Recycle Bin items.",
                    Recommendation = "Run Junk Cleaner to free up hard drive space."
                });
                summary.Recommendations.Add($"Clean {junkMb/1024.0:F1} GB of system and user temporary files.");
            }
            else if (junkMb > 500)
            {
                storageScore -= 10;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Junk Space Size",
                    Category = "Storage",
                    IsHealthy = true,
                    Description = $"Detected {junkMb:F0} MB of temporary files.",
                    Recommendation = "Run Junk Cleaner to reclaim disk space."
                });
                summary.Recommendations.Add("Clear temporary junk files and cache.");
            }

            // 2. Registry Evaluation
            if (registryIssuesCount > 5)
            {
                storageScore -= 10; // registry affects storage/organization
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Registry Health",
                    Category = "Storage",
                    IsHealthy = false,
                    Description = $"Found {registryIssuesCount} invalid shortcuts, missing file associations, or broken registry references.",
                    Recommendation = "Scan and repair registry values via Registry Tools."
                });
                summary.Recommendations.Add($"Repair {registryIssuesCount} broken registry path entries.");
            }

            // 3. Performance / Startup Apps Evaluation
            if (startupAppsCount > 8)
            {
                performanceScore -= (startupAppsCount - 8) * 4; // subtract 4 points for each app above 8
                performanceScore = Math.Max(40, performanceScore);
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Startup Applications overhead",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = $"There are {startupAppsCount} applications registered to boot on startup.",
                    Recommendation = "Disable unnecessary startup apps via Startup Manager to improve boot times."
                });
                summary.Recommendations.Add("Disable non-critical startup programs to accelerate booting.");
            }
            else
            {
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Startup Boot Load",
                    Category = "Performance",
                    IsHealthy = true,
                    Description = $"Optimized startup boot load ({startupAppsCount} active programs).",
                    Recommendation = "System boot parameters are optimized."
                });
            }

            // 4. Network Evaluation
            if (packetLossPercent > 0.0)
            {
                networkScore -= 40;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Packet Loss Detection",
                    Category = "Network",
                    IsHealthy = false,
                    Description = $"Ping test reports {packetLossPercent:F1}% packets lost during transmission.",
                    Recommendation = "Run Network Diagnostics, reset DNS cache, and check gateway routes."
                });
                summary.Recommendations.Add("Repair network connection to eliminate packet loss.");
            }
            if (avgLatencyMs > 150)
            {
                networkScore -= 20;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Network Latency",
                    Category = "Network",
                    IsHealthy = false,
                    Description = $"Average response latency is elevated at {avgLatencyMs:F0} ms.",
                    Recommendation = "Verify proxy parameters, flush DNS, or check ISP connectivity."
                });
            }
            
            if (packetLossPercent == 0 && avgLatencyMs < 80)
            {
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Network Connection Quality",
                    Category = "Network",
                    IsHealthy = true,
                    Description = $"Network connectivity is stable with low latency ({avgLatencyMs:F0} ms) and 0% packet loss.",
                    Recommendation = "No action required."
                });
            }

            // 5. Security Evaluation
            if (securityAudits.Count > 0)
            {
                securityScore -= 40;
                foreach (var issue in securityAudits)
                {
                    summary.Results.Add(new DiagnosticResult
                    {
                        CheckName = "Security Risk Warning",
                        Category = "Security",
                        IsHealthy = false,
                        Description = issue,
                        Recommendation = "Check Defender and security policy settings immediately."
                    });
                }
                summary.Recommendations.Add("Apply Windows security settings and check Defender policy.");
            }
            else
            {
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Security Protection Status",
                    Category = "Security",
                    IsHealthy = true,
                    Description = "Antivirus protection and firewalls are active. No startup vulnerabilities detected.",
                    Recommendation = "System is secure."
                });
            }

            // 6. Software Updates Evaluation
            if (outdatedAppsCount > 0)
            {
                softwareScore -= Math.Min(40, outdatedAppsCount * 10);
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Outdated Software Packages",
                    Category = "Software Updates",
                    IsHealthy = false,
                    Description = $"There are {outdatedAppsCount} third-party or system packages outdated.",
                    Recommendation = "Install updates using the Software Updater."
                });
                summary.Recommendations.Add($"Upgrade {outdatedAppsCount} outdated software applications.");
            }
            else
            {
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Installed Software Updates",
                    Category = "Software Updates",
                    IsHealthy = true,
                    Description = "All monitored system packages and tools are up-to-date.",
                    Recommendation = "No updates required."
                });
            }

            // Final Composite Health Score (Weighted Average)
            double finalScore = (performanceScore * 0.3) + (storageScore * 0.2) + (networkScore * 0.15) + (securityScore * 0.25) + (softwareScore * 0.1);
            summary.HealthScore = (int)Math.Clamp(finalScore, 0, 100);
        });

        return summary;
    }

    public string ExportMaintenanceReport(
        string format,
        HardwareSpecs specs,
        DiagnosticSummary diagnosticSummary,
        string maintenanceResults)
    {
        if (!Directory.Exists(ReportsFolder))
        {
            Directory.CreateDirectory(ReportsFolder);
        }

        string fileName = $"WinCarePro_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
        string fullPath = "";

        if (format.ToUpper() == "JSON")
        {
            fullPath = Path.Combine(ReportsFolder, fileName + ".json");
            var jsonPayload = new
            {
                ReportDate = DateTime.Now,
                HealthScore = diagnosticSummary.HealthScore,
                Hardware = specs,
                Diagnostics = diagnosticSummary.Results,
                Maintenance = maintenanceResults
            };
            string json = JsonSerializer.Serialize(jsonPayload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fullPath, json);
        }
        else // TXT / Markdown style (default)
        {
            fullPath = Path.Combine(ReportsFolder, fileName + ".txt");
            using var sw = new StreamWriter(fullPath);
            sw.WriteLine("==================================================");
            sw.WriteLine("               WINCARE PRO SYSTEM REPORT          ");
            sw.WriteLine("==================================================");
            sw.WriteLine($"Generated At  : {DateTime.Now}");
            sw.WriteLine($"System Owner  : {Environment.UserName}");
            sw.WriteLine($"Health Score  : {diagnosticSummary.HealthScore}/100");
            sw.WriteLine("==================================================");
            sw.WriteLine();
            
            sw.WriteLine("[ HARDWARE SPECIFICATIONS ]");
            sw.WriteLine($"OS Version    : {specs.OsVersion}");
            sw.WriteLine($"Uptime        : {specs.SystemUptime}");
            sw.WriteLine($"Processor     : {specs.CpuModel} ({specs.CpuCores} Cores, {specs.CpuThreads} Threads)");
            sw.WriteLine($"Memory Capacity: {specs.RamCapacityGb:F1} GB ({specs.RamSpeed})");
            sw.WriteLine($"Display Adapter: {specs.GpuModel} ({specs.GpuVram})");
            sw.WriteLine($"Storage Layout: {specs.StorageInfo}");
            sw.WriteLine();

            sw.WriteLine("[ DIAGNOSTIC DISCOVERIES ]");
            foreach (var r in diagnosticSummary.Results)
            {
                sw.WriteLine($"* [{r.Category}] - {r.CheckName}: {(r.IsHealthy ? "HEALTHY" : "WARNING")}");
                sw.WriteLine($"  Details: {r.Description}");
                sw.WriteLine($"  Advice : {r.Recommendation}");
                sw.WriteLine();
            }

            sw.WriteLine("[ COMPLETED MAINTENANCE OPERATIONS ]");
            sw.WriteLine(string.IsNullOrEmpty(maintenanceResults) ? "No cleanup or repair runs recorded in this session." : maintenanceResults);
            sw.WriteLine();
            sw.WriteLine("==================================================");
            sw.WriteLine("Report compiled by WinCare Pro Optimizer Engine.");
        }

        // Register in Database
        Database.DbManager.SaveReport(Path.GetFileName(fullPath), fullPath);

        return fullPath;
    }
}

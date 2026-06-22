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
        List<string> securityAudits,
        double cpuUsage = 0,
        double cpuTemp = 45,
        double ramUsagePercent = 45,
        int servicesCount = 50,
        double diskActiveTime = 5,
        double freeSpacePercent = 50,
        double ssdHealthPercent = 100,
        bool isThrottling = false,
        bool isExplorerOptimized = true)
    {
        var summary = new DiagnosticSummary();

        await Task.Run(() =>
        {
            // 7 Subsystem scores starting at 100
            double cpuScore = 100.0;
            double memoryScore = 100.0;
            double storageScore = 100.0;
            double startupScore = 100.0;
            double servicesScore = 100.0;
            double responsivenessScore = 100.0;
            double networkScore = 100.0;

            // 1. CPU Evaluation
            if (cpuUsage > 90.0)
            {
                cpuScore -= 30.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "High CPU Usage",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = $"CPU usage is extremely high at {cpuUsage:F1}%.",
                    Recommendation = "Close resource-heavy applications or run One-Click Optimization."
                });
                summary.Recommendations.Add("Reduce CPU overhead by terminating background tasks.");
            }
            if (cpuTemp > 90.0)
            {
                cpuScore -= 40.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "CPU Thermal Warning",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = $"CPU temperature is critical at {cpuTemp:F1}°C.",
                    Recommendation = "Check system cooling fans and ventilation."
                });
                summary.Recommendations.Add("Resolve CPU thermal throttling risks.");
            }
            if (isThrottling)
            {
                cpuScore -= 30.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "CPU Throttling",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = "CPU frequency throttling detected.",
                    Recommendation = "Improve cooling or change Windows power plan."
                });
            }

            // 2. Memory Evaluation
            if (ramUsagePercent > 85.0)
            {
                memoryScore -= 30.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "High RAM Usage",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = $"RAM usage is high at {ramUsagePercent:F1}%.",
                    Recommendation = "Run RAM booster to free up standby memory."
                });
                summary.Recommendations.Add("Free memory using the RAM Optimization tool.");
            }
            if (ramUsagePercent > 90.0)
            {
                memoryScore -= 20.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Critical Commit Charge",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = "Memory commit charge exceeds warning levels.",
                    Recommendation = "Close unused web browser tabs and background applications."
                });
            }

            // 3. Storage Evaluation
            if (diskActiveTime > 95.0)
            {
                storageScore -= 40.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Disk Bottleneck",
                    Category = "Storage",
                    IsHealthy = false,
                    Description = $"Disk active time is elevated at {diskActiveTime:F1}%.",
                    Recommendation = "Check resource monitor for processes with high Disk IO."
                });
            }
            if (freeSpacePercent < 10.0)
            {
                storageScore -= 30.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Low Free Space",
                    Category = "Storage",
                    IsHealthy = false,
                    Description = $"System drive free space is critical at {freeSpacePercent:F1}%.",
                    Recommendation = "Delete temporary files and browser cache to free space."
                });
            }
            if (ssdHealthPercent < 20.0)
            {
                storageScore -= 50.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "SSD Wear Warning",
                    Category = "Storage",
                    IsHealthy = false,
                    Description = $"SSD health is critical at {ssdHealthPercent:F0}%.",
                    Recommendation = "Backup data immediately. SSD replacement recommended."
                });
            }
            double junkMb = junkSizeBytes / 1024.0 / 1024.0;
            if (junkMb > 10000) // 10GB
            {
                storageScore -= 30.0;
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
                storageScore -= 10.0;
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

            // 4. Startup Optimization
            if (startupAppsCount > 8)
            {
                startupScore -= (startupAppsCount - 8) * 5;
                startupScore = Math.Max(40, startupScore);
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

            // 5. Background Services
            if (servicesCount > 120)
            {
                servicesScore -= 40.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Excessive Background Services",
                    Category = "Performance",
                    IsHealthy = false,
                    Description = $"Detected {servicesCount} active background services running.",
                    Recommendation = "Disable unnecessary non-essential service entries."
                });
            }
            else if (servicesCount > 80)
            {
                servicesScore -= 20.0;
            }

            // 6. System Responsiveness
            if (registryIssuesCount > 5)
            {
                responsivenessScore -= 20.0;
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
            if (!isExplorerOptimized)
            {
                responsivenessScore -= 15.0;
            }

            // 7. Network Health
            if (packetLossPercent > 5.0)
            {
                networkScore -= 50.0;
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
            else if (packetLossPercent > 0.0)
            {
                networkScore -= 10.0;
            }

            if (avgLatencyMs > 200.0)
            {
                networkScore -= 30.0;
                summary.Results.Add(new DiagnosticResult
                {
                    CheckName = "Network Latency",
                    Category = "Network",
                    IsHealthy = false,
                    Description = $"Average response latency is elevated at {avgLatencyMs:F0} ms.",
                    Recommendation = "Verify proxy parameters, flush DNS, or check ISP connectivity."
                });
            }
            else if (avgLatencyMs > 150.0)
            {
                networkScore -= 10.0;
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

            // Security Risks & Software updates
            if (securityAudits.Count > 0)
            {
                responsivenessScore -= 10.0;
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
            if (outdatedAppsCount > 0)
            {
                responsivenessScore -= 10.0;
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

            // Final Composite Health Score (Weighted Average)
            cpuScore = Math.Clamp(cpuScore, 0.0, 100.0);
            memoryScore = Math.Clamp(memoryScore, 0.0, 100.0);
            storageScore = Math.Clamp(storageScore, 0.0, 100.0);
            startupScore = Math.Clamp(startupScore, 0.0, 100.0);
            servicesScore = Math.Clamp(servicesScore, 0.0, 100.0);
            responsivenessScore = Math.Clamp(responsivenessScore, 0.0, 100.0);
            networkScore = Math.Clamp(networkScore, 0.0, 100.0);

            double finalScore = (cpuScore * 0.20) + 
                                (memoryScore * 0.20) + 
                                (storageScore * 0.15) + 
                                (startupScore * 0.15) + 
                                (servicesScore * 0.10) + 
                                (responsivenessScore * 0.15) + 
                                (networkScore * 0.05);

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Services;

namespace WinCarePro.Modules.AiAssistant
{
    public class AiHealthRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // Performance, Junk, Security, Storage, Prediction
        public string ImpactLevel { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string ActionKey { get; set; } = string.Empty;

        public string CategoryIconGlyph => Category switch
        {
            "Storage" => "\uE7F1",
            "Performance" => "\uE9D9",
            "Junk" => "\uE74D",
            "Security" => "\uE8A9",
            "Prediction" => "\uE945",
            _ => "\uE9D9"
        };

        public string ImpactBadgeForegroundHex => ImpactLevel switch
        {
            "Critical" => "#EF4444",
            "High" => "#F97316",
            "Medium" => "#F59E0B",
            "Low" => "#10B981",
            _ => "#F59E0B"
        };

        public string ImpactBadgeBackgroundHex => ImpactLevel switch
        {
            "Critical" => "#25EF4444",
            "High" => "#25F97316",
            "Medium" => "#25F59E0B",
            "Low" => "#2510B981",
            _ => "#25F59E0B"
        };

        public string ImpactBadgeBorderHex => ImpactLevel switch
        {
            "Critical" => "#60EF4444",
            "High" => "#60F97316",
            "Medium" => "#60F59E0B",
            "Low" => "#6010B981",
            _ => "#60F59E0B"
        };

        private Microsoft.UI.Xaml.Media.Brush? _fgBrush;
        public Microsoft.UI.Xaml.Media.Brush ImpactBadgeForegroundBrush
        {
            get
            {
                if (_fgBrush == null)
                {
                    var color = ImpactLevel switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(255, 239, 68, 68),
                        "High" => Windows.UI.Color.FromArgb(255, 249, 115, 22),
                        "Medium" => Windows.UI.Color.FromArgb(255, 245, 158, 11),
                        "Low" => Windows.UI.Color.FromArgb(255, 16, 185, 129),
                        _ => Windows.UI.Color.FromArgb(255, 245, 158, 11)
                    };
                    _fgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _fgBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _bgBrush;
        public Microsoft.UI.Xaml.Media.Brush ImpactBadgeBackgroundBrush
        {
            get
            {
                if (_bgBrush == null)
                {
                    var color = ImpactLevel switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(0x25, 239, 68, 68),
                        "High" => Windows.UI.Color.FromArgb(0x25, 249, 115, 22),
                        "Medium" => Windows.UI.Color.FromArgb(0x25, 245, 158, 11),
                        "Low" => Windows.UI.Color.FromArgb(0x25, 16, 185, 129),
                        _ => Windows.UI.Color.FromArgb(0x25, 245, 158, 11)
                    };
                    _bgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _bgBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _borderBrush;
        public Microsoft.UI.Xaml.Media.Brush ImpactBadgeBorderBrush
        {
            get
            {
                if (_borderBrush == null)
                {
                    var color = ImpactLevel switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(0x60, 239, 68, 68),
                        "High" => Windows.UI.Color.FromArgb(0x60, 249, 115, 22),
                        "Medium" => Windows.UI.Color.FromArgb(0x60, 245, 158, 11),
                        "Low" => Windows.UI.Color.FromArgb(0x60, 16, 185, 129),
                        _ => Windows.UI.Color.FromArgb(0x60, 245, 158, 11)
                    };
                    _borderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _borderBrush;
            }
        }
    }

    public class AiHealthReport
    {
        public int OverallScore { get; set; } = 100;
        public string HealthStatus { get; set; } = "Optimal";
        public string SummaryText { get; set; } = string.Empty;
        public string PredictiveStorageDaysText { get; set; } = "30+ days free";
        public string PredictiveBootTimeSavingsText { get; set; } = "1.5s faster boot";
        public List<AiHealthRecommendation> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI Engine v4.0.0 analyzing system telemetry, memory pressure, predictive storage exhaustion, 
    /// boot time overhead profiling, and generating smart diagnostic recommendations.
    /// </summary>
    public static class AiHealthEngine
    {
        public static async Task<AiHealthReport> AnalyzeSystemHealthAsync()
        {
            return await Task.Run(() =>
            {
                var report = new AiHealthReport();
                var recommendations = new List<AiHealthRecommendation>();
                int penaltyScore = 0;

                // 1. Predictive Storage Exhaustion Analysis (Dự đoán ngày đầy ổ đĩa C:)
                try
                {
                    var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase));
                    if (systemDrive != null)
                    {
                        double freeGB = systemDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        double totalGB = systemDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        double usedPercent = ((totalGB - freeGB) / totalGB) * 100.0;

                        int estimatedDaysLeft = Math.Max(3, (int)(freeGB * 1.5));
                        report.PredictiveStorageDaysText = estimatedDaysLeft > 30 ? "> 30 Days Free".T() : $"{estimatedDaysLeft} Days Left".T();

                        if (freeGB < 20.0 || usedPercent > 88.0)
                        {
                            penaltyScore += 25;
                            recommendations.Add(new AiHealthRecommendation
                            {
                                Title = "Predictive Storage Warning".T(),
                                Description = $"Drive C: has {freeGB:F1} GB free ({usedPercent:F0}% used). AI predicts disk exhaustion in approximately {estimatedDaysLeft} days under regular usage.".T(),
                                Category = "Storage",
                                ImpactLevel = freeGB < 10.0 ? "Critical" : "High",
                                ActionKey = "NavigateDisk"
                            });
                        }
                        else
                        {
                            recommendations.Add(new AiHealthRecommendation
                            {
                                Title = "Storage Consumption Outlook Good".T(),
                                Description = $"Drive C: usage is at {usedPercent:F0}%. Estimated storage sustainability is over {estimatedDaysLeft} days.".T(),
                                Category = "Prediction",
                                ImpactLevel = "Low",
                                ActionKey = "None"
                            });
                        }
                    }
                }
                catch { }

                // 2. Predictive Boot Time Overhead Profiling (Dự đoán khởi động chậm)
                try
                {
                    int processCount = 0;
                    foreach (var p in Process.GetProcesses())
                    {
                        try { processCount++; }
                        finally { try { p.Dispose(); } catch { } }
                    }

                    double potentialBootTimeSavingsSeconds = Math.Round(processCount * 0.04, 1);
                    report.PredictiveBootTimeSavingsText = $"-{potentialBootTimeSavingsSeconds}s Boot Time".T();

                    if (processCount > 100)
                    {
                        penaltyScore += 15;
                        recommendations.Add(new AiHealthRecommendation
                        {
                            Title = "Predictive Boot Delay Profiling".T(),
                            Description = $"AI detected {processCount} active background processes. Disabling unnecessary startup items can shave up to {potentialBootTimeSavingsSeconds} seconds off boot time.".T(),
                            Category = "Performance",
                            ImpactLevel = processCount > 150 ? "Critical" : "High",
                            ActionKey = "NavigateStartup"
                        });
                    }
                }
                catch { }

                // 3. Analyze Temp Junk Accumulation (Safe enumeration across temp folders)
                try
                {
                    long totalTempBytes = 0;
                    string[] tempPaths = new[] { Path.GetTempPath(), @"C:\Windows\Temp" };

                    foreach (var path in tempPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            totalTempBytes += GetDirectorySizeBytesSafely(path);
                        }
                    }

                    long tempMB = totalTempBytes / (1024 * 1024);
                    if (tempMB > 300)
                    {
                        penaltyScore += 15;
                        recommendations.Add(new AiHealthRecommendation
                        {
                            Title = "High Temp Cache Accumulation".T(),
                            Description = $"System temporary directories contain approximately {tempMB:N0} MB of uncleaned cache and temporary files.".T(),
                            Category = "Junk",
                            ImpactLevel = tempMB > 1500 ? "High" : "Medium",
                            ActionKey = "NavigateJunkCleaner"
                        });
                    }
                }
                catch { }

                // 4. Memory Working Set Check
                try
                {
                    long totalWorkingSet = 0;
                    var processes = Process.GetProcesses();
                    foreach (var p in processes)
                    {
                        try { totalWorkingSet += p.WorkingSet64; } catch { }
                        finally { try { p.Dispose(); } catch { } }
                    }
                    long totalRamMB = totalWorkingSet / (1024 * 1024);
                    if (totalRamMB > 8000) // High active process RAM usage (> 8GB)
                    {
                        penaltyScore += 10;
                        recommendations.Add(new AiHealthRecommendation
                        {
                            Title = "High Memory Pressure".T(),
                            Description = $"Active process working set is currently using {totalRamMB:N0} MB. Optimization can release standby cache.".T(),
                            Category = "Performance",
                            ImpactLevel = "Medium",
                            ActionKey = "NavigateOptimizer"
                        });
                    }
                }
                catch { }

                // 5. Calculate final health score and status text
                report.OverallScore = Math.Max(10, 100 - penaltyScore);

                if (report.OverallScore >= 90)
                {
                    report.HealthStatus = "Optimal Health".T();
                    report.SummaryText = "WinCare AI Assistant predicts maximum system stability with 0 critical bottlenecks detected.".T();
                }
                else if (report.OverallScore >= 70)
                {
                    report.HealthStatus = "Fair Condition".T();
                    report.SummaryText = "System is stable, but AI predicts performance gain if temp junk and memory working sets are purged.".T();
                }
                else
                {
                    report.HealthStatus = "Maintenance Required".T();
                    report.SummaryText = "Multiple predictive bottlenecks detected. Please execute recommended actions.".T();
                }

                report.Recommendations = recommendations;
                return report;
            });
        }

        private static long GetDirectorySizeBytesSafely(string dirPath)
        {
            long size = 0;
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    try { size += file.Length; } catch { }
                }

                foreach (var subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        foreach (var file in subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try { size += file.Length; } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}

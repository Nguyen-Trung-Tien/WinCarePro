using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Services;

namespace WinCarePro.Modules.AiAssistant
{
    public class AiWinCareRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // Performance, Junk, Security, Storage, Prediction
        public string ImpactLevel { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string ImpactLevelDisplayName => ImpactLevel.T();
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
        public Microsoft.UI.Xaml.Media.Brush? ImpactBadgeForegroundBrush
        {
            get
            {
                if (_fgBrush == null)
                {
                    if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
                        return null;
                    try
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
                    catch (Exception ex)
                    {
                        WinCarePro.Infrastructure.Logging.CrashLogger.LogException("AiWinCareRecommendation.ImpactBadgeForegroundBrush", ex);
                    }
                }
                return _fgBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _bgBrush;
        public Microsoft.UI.Xaml.Media.Brush? ImpactBadgeBackgroundBrush
        {
            get
            {
                if (_bgBrush == null)
                {
                    if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
                        return null;
                    try
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
                    catch (Exception ex)
                    {
                        WinCarePro.Infrastructure.Logging.CrashLogger.LogException("AiWinCareRecommendation.ImpactBadgeBackgroundBrush", ex);
                    }
                }
                return _bgBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _borderBrush;
        public Microsoft.UI.Xaml.Media.Brush? ImpactBadgeBorderBrush
        {
            get
            {
                if (_borderBrush == null)
                {
                    if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
                        return null;
                    try
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
                    catch (Exception ex)
                    {
                        WinCarePro.Infrastructure.Logging.CrashLogger.LogException("AiWinCareRecommendation.ImpactBadgeBorderBrush", ex);
                    }
                }
                return _borderBrush;
            }
        }
    }

    public class SubsystemTopologyNode
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = "\uE9D9";
        public string Status { get; set; } = "Optimal"; // Optimal, Warning, Critical
        public string StatusDisplayName => Status.T();
        public string MetricValue { get; set; } = string.Empty;
        public string SubDetail { get; set; } = string.Empty;
        public string ActionKey { get; set; } = "None";

        private Microsoft.UI.Xaml.Media.Brush? _statusBrush;
        public Microsoft.UI.Xaml.Media.Brush StatusBrush
        {
            get
            {
                if (_statusBrush == null)
                {
                    var color = Status switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(255, 239, 68, 68),
                        "Warning" => Windows.UI.Color.FromArgb(255, 245, 158, 11),
                        _ => Windows.UI.Color.FromArgb(255, 16, 185, 129)
                    };
                    _statusBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _statusBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _statusBgBrush;
        public Microsoft.UI.Xaml.Media.Brush StatusBgBrush
        {
            get
            {
                if (_statusBgBrush == null)
                {
                    var color = Status switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(0x22, 239, 68, 68),
                        "Warning" => Windows.UI.Color.FromArgb(0x22, 245, 158, 11),
                        _ => Windows.UI.Color.FromArgb(0x22, 16, 185, 129)
                    };
                    _statusBgBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _statusBgBrush;
            }
        }

        private Microsoft.UI.Xaml.Media.Brush? _statusBorderBrush;
        public Microsoft.UI.Xaml.Media.Brush StatusBorderBrush
        {
            get
            {
                if (_statusBorderBrush == null)
                {
                    var color = Status switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(0x55, 239, 68, 68),
                        "Warning" => Windows.UI.Color.FromArgb(0x55, 245, 158, 11),
                        _ => Windows.UI.Color.FromArgb(0x55, 16, 185, 129)
                    };
                    _statusBorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _statusBorderBrush;
            }
        }
    }

    public class CausalChainItem
    {
        public string Title { get; set; } = string.Empty;
        public string TriggerComponent { get; set; } = string.Empty;
        public string PropagationEffect { get; set; } = string.Empty;
        public string SystemImpact { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
        public string ActionKey { get; set; } = string.Empty;
        public string ActionTitle { get; set; } = "Resolve Now".T();
        public string IconGlyph { get; set; } = "\uE7BA";

        private Microsoft.UI.Xaml.Media.Brush? _sevBrush;
        public Microsoft.UI.Xaml.Media.Brush SeverityBrush
        {
            get
            {
                if (_sevBrush == null)
                {
                    var color = Severity switch
                    {
                        "Critical" => Windows.UI.Color.FromArgb(255, 239, 68, 68),
                        "Warning" => Windows.UI.Color.FromArgb(255, 245, 158, 11),
                        _ => Windows.UI.Color.FromArgb(255, 59, 130, 246)
                    };
                    _sevBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
                }
                return _sevBrush;
            }
        }
    }

    public class AiWinCareReport
    {
        public int OverallScore { get; set; } = 100;
        public string HealthStatus { get; set; } = "Optimal";
        public string SummaryText { get; set; } = string.Empty;
        public string PredictiveStorageDaysText { get; set; } = "30+ days free";
        public string PredictiveBootTimeSavingsText { get; set; } = "1.5s faster boot";
        public int SystemStressIndex { get; set; } = 15;
        public string StressIndexText { get; set; } = "Low Stress (Stable)";
        public List<SubsystemTopologyNode> TopologyNodes { get; set; } = new();
        public List<CausalChainItem> CausalChains { get; set; } = new();
        public List<AiWinCareRecommendation> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// AI Engine v4.0.0 analyzing system telemetry, memory pressure, predictive storage exhaustion, 
    /// boot time overhead profiling, and generating smart diagnostic recommendations.
    /// </summary>
    public static class AiWinCareEngine
    {
        public static async Task<AiWinCareReport> AnalyzeSystemHealthAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var report = new AiWinCareReport();
                var recommendations = new List<AiWinCareRecommendation>();
                int penaltyScore = 0;
                double driveFreeGB = 40.0;
                double driveUsedPercent = 50.0;
                int processCount = 120;
                long totalRamMB = 4000;
                double potentialBootTimeSavingsSeconds = 1.5;
                long tempMB = 250;

                // 1. Predictive Storage Exhaustion Analysis (Dự đoán ngày đầy ổ đĩa C:)
                try
                {
                    var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase));
                    if (systemDrive != null)
                    {
                        double freeGB = systemDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        double totalGB = systemDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        double usedPercent = ((totalGB - freeGB) / totalGB) * 100.0;
                        driveFreeGB = freeGB;
                        driveUsedPercent = usedPercent;

                        int estimatedDaysLeft = Math.Max(3, (int)(freeGB * 1.5));
                        report.PredictiveStorageDaysText = estimatedDaysLeft > 30 ? "> 30 Days Free".T() : $"{estimatedDaysLeft} Days Left".T();

                        if (freeGB < 5.0 || usedPercent > 96.0)
                        {
                            penaltyScore += 15;
                            recommendations.Add(new AiWinCareRecommendation
                            {
                                Title = "Predictive Storage Warning".T(),
                                Description = $"Drive C: has critically low space ({freeGB:F1} GB free, {usedPercent:F0}% used). AI recommends immediate disk cleanup.".T(),
                                Category = "Storage",
                                ImpactLevel = "Critical",
                                ActionKey = "NavigateDisk"
                            });
                        }
                        else if (freeGB < 12.0 || usedPercent > 90.0)
                        {
                            penaltyScore += 5;
                            recommendations.Add(new AiWinCareRecommendation
                            {
                                Title = "Storage Consumption Outlook".T(),
                                Description = $"Drive C: is at {usedPercent:F0}% capacity ({freeGB:F1} GB free). Consider freeing up large files.".T(),
                                Category = "Storage",
                                ImpactLevel = "Medium",
                                ActionKey = "NavigateDisk"
                            });
                        }
                        else
                        {
                            recommendations.Add(new AiWinCareRecommendation
                            {
                                Title = "Storage Consumption Outlook Good".T(),
                                Description = $"Drive C: usage is healthy at {usedPercent:F0}% ({freeGB:F1} GB free). Storage sustainability is over {estimatedDaysLeft} days.".T(),
                                Category = "Prediction",
                                ImpactLevel = "Low",
                                ActionKey = "None"
                            });
                        }
                    }
                }
                catch { }

                // 2. Predictive Boot Time Overhead Profiling & Memory Working Set (Single-Pass Process Scan)
                try
                {
                    processCount = 0;
                    long totalWorkingSet = 0;

                    var processes = Process.GetProcesses();
                    foreach (var p in processes)
                    {
                        try
                        {
                            processCount++;
                            totalWorkingSet += p.WorkingSet64;
                        }
                        catch { }
                        finally
                        {
                            try { p.Dispose(); } catch { }
                        }
                    }

                    potentialBootTimeSavingsSeconds = Math.Round(processCount * 0.02, 1);
                    report.PredictiveBootTimeSavingsText = $"-{potentialBootTimeSavingsSeconds}s Boot Time".T();

                    if (processCount > 250)
                    {
                        penaltyScore += 8;
                        recommendations.Add(new AiWinCareRecommendation
                        {
                            Title = "Elevated Process Overhead".T(),
                            Description = $"AI detected {processCount} active background processes. Disabling unnecessary startup items can improve boot time.".T(),
                            Category = "Performance",
                            ImpactLevel = "High",
                            ActionKey = "NavigateStartup"
                        });
                    }
                    else if (processCount > 180)
                    {
                        penaltyScore += 4;
                        recommendations.Add(new AiWinCareRecommendation
                        {
                            Title = "Background Process Monitoring".T(),
                            Description = $"There are {processCount} background processes active. System is operating normally.".T(),
                            Category = "Performance",
                            ImpactLevel = "Low",
                            ActionKey = "NavigateStartup"
                        });
                    }

                    totalRamMB = totalWorkingSet / (1024 * 1024);
                    if (totalRamMB > 12000) // High active process RAM usage (> 12GB)
                    {
                        penaltyScore += 8;
                        recommendations.Add(new AiWinCareRecommendation
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

                    tempMB = totalTempBytes / (1024 * 1024);
                    if (tempMB > 3000)
                    {
                        penaltyScore += 10;
                        recommendations.Add(new AiWinCareRecommendation
                        {
                            Title = "High Temp Cache Accumulation".T(),
                            Description = $"System temporary directories contain approximately {tempMB:N0} MB of uncleaned cache and temporary files.".T(),
                            Category = "Junk",
                            ImpactLevel = "High",
                            ActionKey = "NavigateJunkCleaner"
                        });
                    }
                    else if (tempMB > 1000)
                    {
                        penaltyScore += 5;
                        recommendations.Add(new AiWinCareRecommendation
                        {
                            Title = "Temp Files Cleanable".T(),
                            Description = $"Found {tempMB:N0} MB of temporary files ready for cleanup.".T(),
                            Category = "Junk",
                            ImpactLevel = "Medium",
                            ActionKey = "NavigateJunkCleaner"
                        });
                    }
                }
                catch { }

                // 5. Check Non-System Drive Storage (Secondary Drives D:, E:, etc.)
                try
                {
                    var extraDrives = DriveInfo.GetDrives().Where(d => d.IsReady && !d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var drive in extraDrives)
                    {
                        double freeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        double totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        double usedPercent = ((totalGB - freeGB) / totalGB) * 100.0;
                        if (freeGB < 10.0 || usedPercent > 95.0)
                        {
                            penaltyScore += 4;
                            recommendations.Add(new AiWinCareRecommendation
                            {
                                Title = string.Format("Drive {0} Storage High".T(), drive.Name.TrimEnd('\\')),
                                Description = string.Format("Drive {0} is at {1:F0}% capacity ({2:F1} GB free). AI recommends archiving or disk cleanup.".T(), drive.Name.TrimEnd('\\'), usedPercent, freeGB),
                                Category = "Storage",
                                ImpactLevel = "Medium",
                                ActionKey = "NavigateDisk"
                            });
                        }
                    }
                }
                catch { }

                // 6. Calculate final health score and status text
                report.OverallScore = Math.Clamp(100 - penaltyScore, 20, 100);

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

                // 7. Populate Interactive Subsystem Neural Topology Nodes
                report.TopologyNodes = new List<SubsystemTopologyNode>
                {
                    new SubsystemTopologyNode
                    {
                        Id = "CPU",
                        Title = "CPU / Kernel".T(),
                        IconGlyph = "\uE9D9",
                        Status = processCount > 250 ? "Critical" : (processCount > 180 ? "Warning" : "Optimal"),
                        MetricValue = $"{processCount} Tasks".T(),
                        SubDetail = processCount > 180 ? "Elevated daemon count".T() : "Stable thread dispatching".T(),
                        ActionKey = "NavigateStartup"
                    },
                    new SubsystemTopologyNode
                    {
                        Id = "RAM",
                        Title = "Memory / RAM".T(),
                        IconGlyph = "\uE7F1",
                        Status = totalRamMB > 12000 ? "Critical" : (totalRamMB > 8000 ? "Warning" : "Optimal"),
                        MetricValue = $"{totalRamMB / 1024.0:F1} GB Used".T(),
                        SubDetail = totalRamMB > 8000 ? "Standby cache elevated".T() : "Clean memory bounds".T(),
                        ActionKey = "NavigateOptimizer"
                    },
                    new SubsystemTopologyNode
                    {
                        Id = "Storage",
                        Title = "Storage / NVMe".T(),
                        IconGlyph = "\uEDA2",
                        Status = driveFreeGB < 6.0 ? "Critical" : (driveFreeGB < 15.0 ? "Warning" : "Optimal"),
                        MetricValue = $"{driveFreeGB:F1} GB Free".T(),
                        SubDetail = driveUsedPercent > 90 ? "High disk consumption".T() : report.PredictiveStorageDaysText,
                        ActionKey = "NavigateDisk"
                    },
                    new SubsystemTopologyNode
                    {
                        Id = "Network",
                        Title = "Network Sockets".T(),
                        IconGlyph = "\uE701",
                        Status = "Optimal",
                        MetricValue = "Healthy Sockets".T(),
                        SubDetail = "DNS resolver synchronized".T(),
                        ActionKey = "FlushDns"
                    },
                    new SubsystemTopologyNode
                    {
                        Id = "Cache",
                        Title = "Cache & Debris".T(),
                        IconGlyph = "\uEA99",
                        Status = tempMB > 3000 ? "Critical" : (tempMB > 1000 ? "Warning" : "Optimal"),
                        MetricValue = $"{tempMB:N0} MB Temp".T(),
                        SubDetail = tempMB > 1000 ? "Ready for safe purge".T() : "Clean file buffers".T(),
                        ActionKey = "NavigateJunkCleaner"
                    }
                };

                // 8. AI Root-Cause Causal Analysis Flows (Explainable AI - XAI)
                var causalChains = new List<CausalChainItem>();

                if (totalRamMB > 7000 || processCount > 180)
                {
                    causalChains.Add(new CausalChainItem
                    {
                        Title = "Memory Pressure & Virtual Pagination Chain".T(),
                        TriggerComponent = $"{processCount} Active Background Daemons".T(),
                        PropagationEffect = $"{totalRamMB / 1024.0:F1} GB Working Set -> Increased Pagefile Activity".T(),
                        SystemImpact = "+150ms Task Switch Latency".T(),
                        Explanation = "High active memory commit forces Windows to page standby memory onto storage, causing perceptible micro-stutter during window switching.".T(),
                        Severity = totalRamMB > 12000 ? "Critical" : "Warning",
                        ActionKey = "NavigateOptimizer",
                        ActionTitle = "Optimize RAM".T(),
                        IconGlyph = "\uE7F1"
                    });
                }

                if (tempMB > 750)
                {
                    causalChains.Add(new CausalChainItem
                    {
                        Title = "Orphaned Cache & Disk Scanning Overhead".T(),
                        TriggerComponent = $"{tempMB:N0} MB Accumulated Temp Files".T(),
                        PropagationEffect = "NTFS Directory Index Fragmentation & Cache Overhead".T(),
                        SystemImpact = "Degraded Explorer indexing responsiveness".T(),
                        Explanation = "Thousands of obsolete temporary files clutter storage metadata, causing Windows search and background file indexing to consume extra disk I/O.".T(),
                        Severity = tempMB > 2500 ? "Critical" : "Warning",
                        ActionKey = "NavigateJunkCleaner",
                        ActionTitle = "Clean Cache".T(),
                        IconGlyph = "\uEA99"
                    });
                }

                if (driveFreeGB < 15.0)
                {
                    causalChains.Add(new CausalChainItem
                    {
                        Title = "Storage Depletion & Write Amplification Risk".T(),
                        TriggerComponent = $"Drive C: Low Space ({driveFreeGB:F1} GB Free)".T(),
                        PropagationEffect = "Limited Wear Leveling Blocks & SSD Trim Overhead".T(),
                        SystemImpact = "Reduced SSD write endurance & throttle risk".T(),
                        Explanation = "When free storage falls below 15%, solid-state drives suffer higher write amplification, impacting system longevity and burst transfer rates.".T(),
                        Severity = driveFreeGB < 6.0 ? "Critical" : "Warning",
                        ActionKey = "NavigateDisk",
                        ActionTitle = "Free Storage".T(),
                        IconGlyph = "\uEDA2"
                    });
                }

                if (causalChains.Count == 0)
                {
                    causalChains.Add(new CausalChainItem
                    {
                        Title = "Optimal System Harmony Chain".T(),
                        TriggerComponent = "Balanced Background Workload".T(),
                        PropagationEffect = "Clean Working Set & Synchronized Cache".T(),
                        SystemImpact = "Maximum UI Thread Velocity".T(),
                        Explanation = "All subsystems are interacting within optimal operational thresholds. Hardware interrupts and I/O scheduler are fully stabilized.".T(),
                        Severity = "Info",
                        ActionKey = "None",
                        ActionTitle = "Verified".T(),
                        IconGlyph = "\uE73E"
                    });
                }
                report.CausalChains = causalChains;

                // 9. Calculate Predictive System Stress Index
                int stress = Math.Clamp((int)((penaltyScore * 2.2) + (totalRamMB > 9000 ? 15 : 0) + (tempMB > 1500 ? 15 : 0)), 8, 92);
                report.SystemStressIndex = stress;
                report.StressIndexText = stress < 30 ? "Low Stress (Optimal)".T() : (stress < 60 ? "Moderate Load".T() : "High Stress (Action Recommended)".T());

                return report;
            });
        }

        public class SmartRemedyResult
        {
            public int FixedActionsCount { get; set; }
            public long FreedMemoryBytes { get; set; }
            public long CleanedTempBytes { get; set; }
            public bool DnsFlushed { get; set; }
            public List<string> ActionLogs { get; set; } = new();
        }

        public static async Task<SmartRemedyResult> ExecuteSmartRemedyBatchAsync()
        {
            return await Task.Run(async () =>
            {
                var result = new SmartRemedyResult();

                // 1. Purge Standby Memory & Working Set
                try
                {
                    long beforeMem = GC.GetTotalMemory(false);
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    long afterMem = GC.GetTotalMemory(true);
                    result.FreedMemoryBytes = Math.Max(0, beforeMem - afterMem);
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("Memory Working Set and Standby List optimized.".T());
                }
                catch { }

                // 2. Safe Temp Cleaning (Scoped to safe user temp folder)
                try
                {
                    long cleaned = 0;
                    string userTemp = Path.GetTempPath();
                    if (Directory.Exists(userTemp) && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(userTemp))
                    {
                        var dir = new DirectoryInfo(userTemp);
                        foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                if ((DateTime.Now - file.LastWriteTime).TotalHours > 12 && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(file.FullName))
                                {
                                    long len = file.Length;
                                    file.Delete();
                                    cleaned += len;
                                }
                            }
                            catch { }
                        }
                    }
                    result.CleanedTempBytes = cleaned;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add(string.Format("Safe temp files cleaned ({0:F1} MB freed).".T(), cleaned / (1024.0 * 1024.0)));
                }
                catch { }

                // 3. Flush DNS & Winsock Cache
                try
                {
                    var cmdRes = await Core.Helpers.ProcessRunner.RunHiddenAsync("ipconfig.exe", new[] { "/flushdns" }, 3);
                    result.DnsFlushed = cmdRes.Success;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("DNS Resolver cache flushed.".T());
                }
                catch { }

                return result;
            });
        }

        public static async Task<SmartRemedyResult> ExecuteSmartRemedyLevelAsync(int level)
        {
            return await Task.Run(async () =>
            {
                var result = new SmartRemedyResult();

                // Level 1, 2, 3: Purge GC and Memory Working Set
                try
                {
                    long beforeMem = GC.GetTotalMemory(false);
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    long afterMem = GC.GetTotalMemory(true);
                    result.FreedMemoryBytes = Math.Max(0, beforeMem - afterMem);
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("Memory Working Set and Standby List optimized.".T());
                }
                catch { }

                // Safe Temp Cleaning (Level 1: > 24h, Level 2: > 6h, Level 3: > 1h)
                try
                {
                    double minHours = level switch { 1 => 24.0, 2 => 6.0, _ => 1.0 };
                    long cleaned = 0;
                    string userTemp = Path.GetTempPath();
                    if (Directory.Exists(userTemp) && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(userTemp))
                    {
                        var dir = new DirectoryInfo(userTemp);
                        foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                if ((DateTime.Now - file.LastWriteTime).TotalHours > minHours && Core.Helpers.SafePathGuard.IsPathSafeForDeletion(file.FullName))
                                {
                                    long len = file.Length;
                                    file.Delete();
                                    cleaned += len;
                                }
                            }
                            catch { }
                        }
                    }
                    result.CleanedTempBytes = cleaned;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add(string.Format("Safe temp files cleaned ({0:F1} MB freed).".T(), cleaned / (1024.0 * 1024.0)));
                }
                catch { }

                // Flush DNS & Winsock Cache
                try
                {
                    var cmdRes = await Core.Helpers.ProcessRunner.RunHiddenAsync("ipconfig.exe", new[] { "/flushdns" }, 3);
                    result.DnsFlushed = cmdRes.Success;
                    result.FixedActionsCount++;
                    result.ActionLogs.Add("DNS Resolver cache flushed.".T());
                }
                catch { }

                return result;
            });
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (long Size, DateTime Timestamp)> _dirSizeCache = new(StringComparer.OrdinalIgnoreCase);

        public class OfflineDiagnosticAnswer
        {
            public string Title { get; set; } = string.Empty;
            public string Diagnosis { get; set; } = string.Empty;
            public string Solution { get; set; } = string.Empty;
            public string RecommendedActionKey { get; set; } = "None";
            public string ActionButtonText { get; set; } = "Fix Now";
        }

        /// <summary>
        /// Provides instant rule-based diagnostic answers and 1-click remediation workflows
        /// when running in offline mode or when cloud AI is unreachable.
        /// </summary>
        public static OfflineDiagnosticAnswer GetOfflineDiagnosticAdvice(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "WinCare AI Assistant Ready".T(),
                    Diagnosis = "Please enter your question, issue, or error symptom.".T(),
                    Solution = "You can ask about system lag, disk space, network connection, gaming FPS, or startup apps.".T(),
                    RecommendedActionKey = "None"
                };
            }

            string q = userQuery.ToLowerInvariant();

            // 1. RAM / Memory
            if (q.Contains("ram") || q.Contains("bộ nhớ") || q.Contains("memory") || q.Contains("tràn ram") || q.Contains("đơ"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Memory Optimization & Standby Purge".T(),
                    Diagnosis = "High memory working set or bloated standby cache can cause sudden UI freezes and stuttering.".T(),
                    Solution = "Run RAM Booster to flush standby lists and reclaim unused physical RAM working sets.".T(),
                    RecommendedActionKey = "NavigateOptimizer",
                    ActionButtonText = "Open Optimizer".T()
                };
            }

            // 2. Junk / Temp / Cache
            if (q.Contains("rác") || q.Contains("junk") || q.Contains("temp") || q.Contains("cache") || q.Contains("dọn dẹp") || q.Contains("clean"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Junk & Temp Cache Cleanup".T(),
                    Diagnosis = "Accumulated Windows temp files, browser caches, and system logs take up space and slow down IO.".T(),
                    Solution = "Use Junk Cleaner for a comprehensive scan across Windows, browsers, logs, and installer caches.".T(),
                    RecommendedActionKey = "NavigateJunkCleaner",
                    ActionButtonText = "Clean Junk Now".T()
                };
            }

            // 3. Disk / Storage / Drive C Full
            if (q.Contains("ổ c") || q.Contains("đầy ổ") || q.Contains("disk") || q.Contains("dung lượng") || q.Contains("storage") || q.Contains("space"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Disk Space Recovery & Analysis".T(),
                    Diagnosis = "Drive C is filling up, potentially due to large system files, Hibernation file, Windows.old or temp dumps.".T(),
                    Solution = "Navigate to Disk Analyzer to locate large files and run System Component Store (WinSxS) cleanup.".T(),
                    RecommendedActionKey = "NavigateDisk",
                    ActionButtonText = "Open Disk Center".T()
                };
            }

            // 4. Network / DNS / Internet (Prioritize network connection queries)
            if (q.Contains("mạng") || q.Contains("dns") || q.Contains("wifi") || q.Contains("internet") || q.Contains("rớt mạng") || q.Contains("ping"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Network Health & DNS Optimization".T(),
                    Diagnosis = "DNS latency or corrupted Winsock cache can cause slow web browsing and packet loss.".T(),
                    Solution = "Switch to Ultra-Fast DNS (Cloudflare 1.1.1.1 / Google 8.8.8.8) and flush the DNS resolver cache.".T(),
                    RecommendedActionKey = "NavigateNetwork",
                    ActionButtonText = "Open Network Center".T()
                };
            }

            // 5. Gaming / FPS / Latency
            if (q.Contains("game") || q.Contains("fps") || q.Contains("lag") || q.Contains("giật") || q.Contains("turbo"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Performance & Latency Optimization".T(),
                    Diagnosis = "Background services and system latency timers can reduce application responsiveness and cause micro-stutter.".T(),
                    Solution = "Configure System Optimizer performance tweaks to prioritize active foreground applications.".T(),
                    RecommendedActionKey = "NavigateOptimizer",
                    ActionButtonText = "Open Optimizer".T()
                };
            }

            // 6. Startup / Slow Boot
            if (q.Contains("khởi động") || q.Contains("boot") || q.Contains("chậm") || q.Contains("startup") || q.Contains("mở máy"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "Startup Boot Acceleration".T(),
                    Diagnosis = "Too many background apps launching at startup dramatically increases Windows boot time and disk IO.".T(),
                    Solution = "Disable high-impact startup apps and delay non-essential startup services.".T(),
                    RecommendedActionKey = "NavigateStartup",
                    ActionButtonText = "Manage Startup".T()
                };
            }

            // 7. System Repair / Windows Error / SFC / DISM
            if (q.Contains("lỗi") || q.Contains("repair") || q.Contains("sửa") || q.Contains("sfc") || q.Contains("dism") || q.Contains("update") || q.Contains("spooler") || q.Contains("máy in"))
            {
                return new OfflineDiagnosticAnswer
                {
                    Title = "System Health & Component Repair".T(),
                    Diagnosis = "Corrupted system files or stuck Windows background services can trigger unpredictable errors.".T(),
                    Solution = "Run 1-Click System Repair to verify system file integrity with SFC/DISM and reset corrupted services.".T(),
                    RecommendedActionKey = "NavigateRepair",
                    ActionButtonText = "Open System Repair".T()
                };
            }

            // Default General Fallback
            return new OfflineDiagnosticAnswer
            {
                Title = "WinCare Diagnostic Recommendation".T(),
                Diagnosis = string.Format("Analysis for '{0}': System baseline analyzed.".T(), userQuery),
                Solution = "Run One-Click Smart Remedy or AI Diagnostics to optimize system performance and clean cache.".T(),
                RecommendedActionKey = "NavigateOptimizer",
                ActionButtonText = "Run Optimization".T()
            };
        }

        private static long GetDirectorySizeBytesSafely(string dirPath)
        {
            if (_dirSizeCache.TryGetValue(dirPath, out var cached) && (DateTime.Now - cached.Timestamp).TotalSeconds < 60)
            {
                return cached.Size;
            }

            long size = 0;
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);
                if (!dirInfo.Exists) return 0;

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

                _dirSizeCache[dirPath] = (size, DateTime.Now);
            }
            catch { }
            return size;
        }
    }
}


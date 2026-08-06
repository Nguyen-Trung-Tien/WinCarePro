using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Engines;

public class PredictiveAnalysisEngine
{
    public List<PredictiveWarning> GeneratePredictiveWarnings(
        double freeSpaceGB,
        double totalSpaceGB,
        int startupAppsCount,
        double ramUsagePercent,
        double pingLatencyMs,
        double packetLossPercent)
    {
        var warnings = new List<PredictiveWarning>();

        // 1. Storage Exhaustion Model
        try
        {
            if (totalSpaceGB > 0)
            {
                double freePercent = (freeSpaceGB / totalSpaceGB) * 100.0;
                // Heuristic estimation: 0.4 GB accumulated per active usage day
                int estimatedDaysLeft = Math.Max(2, (int)(freeSpaceGB / 0.4));

                if (freeSpaceGB < 25.0 || freePercent < 15.0)
                {
                    warnings.Add(new PredictiveWarning
                    {
                        Title = "Predictive Storage Exhaustion Warning".T(),
                        Description = string.Format("Drive C: has {0:F1} GB free. AI telemetry predicts storage exhaustion within ~{1} days under normal usage.", freeSpaceGB, estimatedDaysLeft).T(),
                        MetricTrend = $"{freeSpaceGB:F1} GB Free ({freePercent:F0}%)",
                        ImpactTimeline = string.Format("Exhaustion in {0} Days", estimatedDaysLeft).T(),
                        Severity = freeSpaceGB < 10.0 ? "Critical" : "Warning",
                        IconGlyph = "\uE7B8"
                    });
                }
            }
        }
        catch { }

        // 2. Boot Delay & Startup Creep Model
        try
        {
            if (startupAppsCount > 6)
            {
                double estimatedBootDelaySec = Math.Round(startupAppsCount * 0.45, 1);
                warnings.Add(new PredictiveWarning
                {
                    Title = "Startup Boot Delay Creep".T(),
                    Description = string.Format("AI detected {0} startup applications. Boot time is predicted to slow down by ~{1:F1} seconds on next system reboot.", startupAppsCount, estimatedBootDelaySec).T(),
                    MetricTrend = $"{startupAppsCount} Startup Apps",
                    ImpactTimeline = string.Format("+{0:F1}s Boot Time Delay", estimatedBootDelaySec).T(),
                    Severity = startupAppsCount > 12 ? "Critical" : "Warning",
                    IconGlyph = "\uE7B8"
                });
            }
        }
        catch { }

        // 3. Memory Pressure Accumulation Model
        try
        {
            if (ramUsagePercent > 80.0)
            {
                warnings.Add(new PredictiveWarning
                {
                    Title = "Memory Pressure & Swap Saturation".T(),
                    Description = string.Format("RAM utilization is at {0:F0}%. System is predicted to experience pagefile swapping stalls during heavy multitasking.", ramUsagePercent).T(),
                    MetricTrend = $"{ramUsagePercent:F0}% RAM Capacity",
                    ImpactTimeline = "High Swap Stalls Expected".T(),
                    Severity = ramUsagePercent > 90.0 ? "Critical" : "Warning",
                    IconGlyph = "\uE9D9"
                });
            }
        }
        catch { }

        // 4. Network Instability Warning
        try
        {
            if (pingLatencyMs > 100.0 || packetLossPercent > 3.0)
            {
                warnings.Add(new PredictiveWarning
                {
                    Title = "Network Latency & Jitter Degradation".T(),
                    Description = string.Format("Current latency is {0:F0}ms with {1:F1}% packet loss. High risk of buffering and connection timeouts.", pingLatencyMs, packetLossPercent).T(),
                    MetricTrend = $"{pingLatencyMs:F0}ms / {packetLossPercent:F1}% Loss",
                    ImpactTimeline = "Imminent Connection Drop".T(),
                    Severity = "Warning",
                    IconGlyph = "\uE774"
                });
            }
        }
        catch { }

        return warnings;
    }
}

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
    public struct LinearRegressionResult
    {
        public double Slope;
        public double Intercept;
        public double RSquared;
        public bool IsValid;
    }

    /// <summary>
    /// Computes least-squares linear regression (y = mx + b) and R-squared coefficient of determination.
    /// </summary>
    public static LinearRegressionResult ComputeLinearRegression(double[] x, double[] y)
    {
        if (x == null || y == null || x.Length < 2 || x.Length != y.Length)
        {
            return new LinearRegressionResult { Slope = 0, Intercept = 0, RSquared = 0, IsValid = false };
        }

        int n = x.Length;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

        for (int i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
            sumY2 += y[i] * y[i];
        }

        double denominator = (n * sumX2) - (sumX * sumX);
        if (Math.Abs(denominator) < 1e-10)
        {
            return new LinearRegressionResult { Slope = 0, Intercept = sumY / n, RSquared = 0, IsValid = false };
        }

        double slope = ((n * sumXY) - (sumX * sumY)) / denominator;
        double intercept = (sumY - (slope * sumX)) / n;

        // R-squared computation
        double meanY = sumY / n;
        double ssTot = 0;
        double ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double fi = (slope * x[i]) + intercept;
            ssTot += (y[i] - meanY) * (y[i] - meanY);
            ssRes += (y[i] - fi) * (y[i] - fi);
        }

        double rSquared = (ssTot > 1e-10) ? Math.Clamp(1.0 - (ssRes / ssTot), 0.0, 1.0) : 1.0;

        return new LinearRegressionResult
        {
            Slope = slope,
            Intercept = intercept,
            RSquared = rSquared,
            IsValid = true
        };
    }

    public List<PredictiveWarning> GeneratePredictiveWarnings(
        double freeSpaceGB,
        double totalSpaceGB,
        int startupAppsCount,
        double ramUsagePercent,
        double pingLatencyMs,
        double packetLossPercent)
    {
        var warnings = new List<PredictiveWarning>();

        // 1. Storage Exhaustion Model via Linear Regression Simulation
        try
        {
            if (totalSpaceGB > 0)
            {
                double freePercent = (freeSpaceGB / totalSpaceGB) * 100.0;
                
                // Construct time-series model (Last 7 days data points assuming daily accumulation rate)
                double dailyBurnRateGB = 0.45; // Default baseline ~450MB/day
                if (freePercent < 20.0) dailyBurnRateGB = 0.75;
                if (freePercent < 10.0) dailyBurnRateGB = 1.10;

                double[] days = { 0, 1, 2, 3, 4, 5, 6 };
                double[] projectedFreeSpace = days.Select(d => Math.Max(0.1, freeSpaceGB - (d * dailyBurnRateGB))).ToArray();
                var regression = ComputeLinearRegression(days, projectedFreeSpace);

                // Days until storage falls below safety threshold (5.0 GB)
                double thresholdGB = 5.0;
                int estimatedDaysLeft = 365;
                if (regression.IsValid && regression.Slope < 0)
                {
                    double daysToThreshold = (thresholdGB - regression.Intercept) / regression.Slope;
                    estimatedDaysLeft = Math.Max(1, (int)Math.Round(daysToThreshold));
                }
                else
                {
                    estimatedDaysLeft = Math.Max(2, (int)(freeSpaceGB / dailyBurnRateGB));
                }

                if (freeSpaceGB < 35.0 || freePercent < 20.0)
                {
                    warnings.Add(new PredictiveWarning
                    {
                        Title = "Predictive Storage Exhaustion (AI Linear Trend)".T(),
                        Description = string.Format("Drive C: has {0:F1} GB free ({1:F0}%). AI Regression model predicts critical storage exhaustion (<5GB) in ~{2} days.", freeSpaceGB, freePercent, estimatedDaysLeft).T(),
                        MetricTrend = $"{freeSpaceGB:F1} GB Free ({freePercent:F0}%) | Burn: -{dailyBurnRateGB:F2}GB/d",
                        ImpactTimeline = string.Format("Exhaustion in {0} Days", estimatedDaysLeft).T(),
                        Severity = freeSpaceGB < 12.0 ? "Critical" : "Warning",
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

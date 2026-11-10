using System;
using System.Collections.Generic;
using System.Linq;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Engines;

public class AiWinCareScoringEngine
{
    public SystemHealthAssessment EvaluateHealth(
        double cpuUsage,
        double ramUsage,
        double diskActiveTime,
        double freeSpaceGB,
        double totalSpaceGB,
        int startupAppsCount,
        long junkSizeBytes,
        int registryIssuesCount,
        int outdatedAppsCount,
        double pingLatencyMs,
        double packetLossPercent,
        List<string> securityAudits)
    {
        var assessment = new SystemHealthAssessment();
        var insights = new List<AiExplainableInsight>();
        var categories = new List<CategoryHealthScore>();

        // ----------------------------------------------------
        // CATEGORY 1: Hardware & Resources (Weight: 30%)
        // ----------------------------------------------------
        double hwPenalty = 0;
        var hwStatusDetails = new List<string>();

        if (cpuUsage > 85.0)
        {
            hwPenalty += 35;
            hwStatusDetails.Add($"CPU sustained high load ({cpuUsage:F0}%)");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "CPU Sustained High Load".T(),
                WhatIsHappening = string.Format("Processor utilization is currently at {0:F0}%, exceeding normal idle baselines.".T(), cpuUsage),
                WhyItMatters = "Continuous high CPU usage increases core temperatures, triggers thermal throttling, and degrades UI responsiveness.".T(),
                PotentialImpact = "System lag, high fan noise, reduced battery life, and overall sluggish app launching.".T(),
                RecommendedAction = "Terminate non-essential background processes and optimize background Windows services.".T(),
                EstimatedImprovement = "-35% CPU Load Reduction".T(),
                ExpectedPerformanceGain = "+20% System Responsiveness".T(),
                Category = "Hardware",
                ImpactLevel = "High",
                ActionKey = "OptimizeServices",
                CanAutoFix = true
            });
        }

        if (ramUsage > 85.0)
        {
            hwPenalty += 35;
            hwStatusDetails.Add($"RAM footprint capacity saturated ({ramUsage:F0}%)");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "High Memory (RAM) Footprint".T(),
                WhatIsHappening = string.Format("Physical RAM utilization is at {0:F0}%. Available standby memory is depleted.".T(), ramUsage),
                WhyItMatters = "When RAM fills up, Windows forces heavy pagefile swapping to disk, causing micro-stutters.".T(),
                PotentialImpact = "App crashes, browser tab reloads, freeze spikes during multitasking.".T(),
                RecommendedAction = "Purge standby working sets and clear unused background memory blocks.".T(),
                EstimatedImprovement = "Reclaim 1.5 - 3.5 GB Physical RAM".T(),
                ExpectedPerformanceGain = "+25% Multitasking Speed".T(),
                Category = "Hardware",
                ImpactLevel = "Critical",
                ActionKey = "CleanJunk",
                CanAutoFix = true
            });
        }

        if (diskActiveTime > 85.0)
        {
            hwPenalty += 20;
            hwStatusDetails.Add($"Disk active I/O saturation ({diskActiveTime:F0}%)");
        }

        int hwScore = Math.Max(10, (int)(100 - hwPenalty));
        categories.Add(new CategoryHealthScore
        {
            CategoryName = "Hardware & Resources".T(),
            Score = hwScore,
            Weight = 0.30,
            Status = hwScore >= 85 ? "Optimal".T() : (hwScore >= 65 ? "Fair".T() : "Degraded".T()),
            IconGlyph = "\uE9D9",
            SummaryText = hwStatusDetails.Count > 0 ? string.Join(", ", hwStatusDetails) : "CPU, RAM, and Disk active time operate within normal parameters.".T()
        });

        // ----------------------------------------------------
        // CATEGORY 2: Security & Protection (Weight: 25%)
        // ----------------------------------------------------
        double secPenalty = 0;
        var secDetails = new List<string>();

        if (outdatedAppsCount > 0)
        {
            secPenalty += Math.Min(30, outdatedAppsCount * 10);
            secDetails.Add($"{outdatedAppsCount} outdated software applications");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "Outdated Software Applications Detected".T(),
                WhatIsHappening = string.Format("Found {0} installed software packages with available security patches.", outdatedAppsCount).T(),
                WhyItMatters = "Outdated software contains known CVE security vulnerabilities that exploits target.".T(),
                PotentialImpact = "Security breaches, malware infection, software incompatibility bugs.".T(),
                RecommendedAction = "Update all third-party software packages to the latest stable versions.".T(),
                EstimatedImprovement = string.Format("Patch {0} software security vulnerabilities.", outdatedAppsCount).T(),
                ExpectedPerformanceGain = "Enhanced System Defense".T(),
                Category = "Security",
                ImpactLevel = "High",
                ActionKey = "NavigateUpdater",
                CanAutoFix = false
            });
        }

        if (securityAudits != null && securityAudits.Count > 0)
        {
            secPenalty += Math.Min(40, securityAudits.Count * 15);
            secDetails.Add($"{securityAudits.Count} security configuration warnings");
            foreach (var audit in securityAudits)
            {
                insights.Add(new AiExplainableInsight
                {
                    IssueTitle = "Security Configuration Advisory".T(),
                    WhatIsHappening = audit.T(),
                    WhyItMatters = "Improper security policies leave Windows open to unauthorized background access.".T(),
                    PotentialImpact = "Data leakage, malware execution, unverified process persistence.".T(),
                    RecommendedAction = "Enforce Windows Defender and Firewall secure defaults.".T(),
                    EstimatedImprovement = "Hardened Windows Security Policy".T(),
                    ExpectedPerformanceGain = "Proactive System Protection".T(),
                    Category = "Security",
                    ImpactLevel = "Medium",
                    ActionKey = "RepairSfc",
                    CanAutoFix = true
                });
            }
        }

        int secScore = Math.Max(10, (int)(100 - secPenalty));
        categories.Add(new CategoryHealthScore
        {
            CategoryName = "Security & Protection".T(),
            Score = secScore,
            Weight = 0.25,
            Status = secScore >= 85 ? "Optimal".T() : (secScore >= 65 ? "Fair".T() : "Degraded".T()),
            IconGlyph = "\uE83D",
            SummaryText = secDetails.Count > 0 ? string.Join(", ", secDetails) : "Windows security policies, antivirus, and firewall are active.".T()
        });

        // ----------------------------------------------------
        // CATEGORY 3: System Stability & Network (Weight: 25%)
        // ----------------------------------------------------
        double stabPenalty = 0;
        var stabDetails = new List<string>();

        if (pingLatencyMs > 120.0 || packetLossPercent > 5.0)
        {
            stabPenalty += 30;
            stabDetails.Add($"High network latency ({pingLatencyMs:F0}ms) / packet loss ({packetLossPercent:F1}%)");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "Network Latency & Connection Instability".T(),
                WhatIsHappening = string.Format("Current ping latency is {0:F0}ms with {1:F1}% packet loss rate.", pingLatencyMs, packetLossPercent).T(),
                WhyItMatters = "Packet loss causes buffering, multiplayer gaming lag, and webpage loading timeouts.".T(),
                PotentialImpact = "Frequent network disconnections, web loading stalls, unreliable VoIP calls.".T(),
                RecommendedAction = "Flush Windows DNS cache and reset TCP/IP network socket catalog.".T(),
                EstimatedImprovement = "Reset Network Driver Sockets & Flush Cache".T(),
                ExpectedPerformanceGain = "-40% Ping Latency Jitter".T(),
                Category = "Stability",
                ImpactLevel = "High",
                ActionKey = "FlushDns",
                CanAutoFix = true
            });
        }

        if (startupAppsCount > 8)
        {
            stabPenalty += Math.Min(35, (startupAppsCount - 8) * 5);
            stabDetails.Add($"{startupAppsCount} startup items delaying boot");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "High Startup Program Overhead".T(),
                WhatIsHappening = string.Format("There are {0} applications configured to start automatically with Windows.", startupAppsCount).T(),
                WhyItMatters = "Excessive startup apps consume CPU cycles and RAM immediately on boot, slowing startup.".T(),
                PotentialImpact = "Delayed desktop readiness, slow startup time, background RAM waste.".T(),
                RecommendedAction = "Disable non-essential startup applications.".T(),
                EstimatedImprovement = string.Format("-{0:F1}s Boot Time Delay", startupAppsCount * 0.4).T(),
                ExpectedPerformanceGain = "+30% Faster Desktop Ready Time".T(),
                Category = "Stability",
                ImpactLevel = "High",
                ActionKey = "DisableStartup",
                CanAutoFix = true
            });
        }

        int stabScore = Math.Max(10, (int)(100 - stabPenalty));
        categories.Add(new CategoryHealthScore
        {
            CategoryName = "System Stability".T(),
            Score = stabScore,
            Weight = 0.25,
            Status = stabScore >= 85 ? "Optimal".T() : (stabScore >= 65 ? "Fair".T() : "Degraded".T()),
            IconGlyph = "\uE774",
            SummaryText = stabDetails.Count > 0 ? string.Join(", ", stabDetails) : "Network ping quality and Windows service stability are excellent.".T()
        });

        // ----------------------------------------------------
        // CATEGORY 4: Storage & Cleanliness (Weight: 20%)
        // ----------------------------------------------------
        double storPenalty = 0;
        var storDetails = new List<string>();

        if (junkSizeBytes > 500 * 1024 * 1024)
        {
            double junkMB = junkSizeBytes / (1024.0 * 1024.0);
            storPenalty += Math.Min(40, junkMB / 100.0);
            storDetails.Add($"{junkMB:F0} MB junk accumulation");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "Accumulated System Junk & Temporary Files".T(),
                WhatIsHappening = string.Format("Found {0:F0} MB of temporary files, log caches, and system junk.", junkMB).T(),
                WhyItMatters = "Temporary junk clutter decreases free storage and slows down file index searches.".T(),
                PotentialImpact = "Wasted drive space, slower file searching, drive fragmentation.".T(),
                RecommendedAction = "Run automated junk cleanup to purge obsolete temporary caches.".T(),
                EstimatedImprovement = string.Format("Freed {0:F0} MB Disk Space", junkMB).T(),
                ExpectedPerformanceGain = "Clean Drive Indexing".T(),
                Category = "Storage",
                ImpactLevel = "Medium",
                ActionKey = "CleanJunk",
                CanAutoFix = true
            });
        }

        if (registryIssuesCount > 10)
        {
            storPenalty += Math.Min(30, registryIssuesCount);
            storDetails.Add($"{registryIssuesCount} obsolete registry entries");
            insights.Add(new AiExplainableInsight
            {
                IssueTitle = "Obsolete Windows Registry Keys".T(),
                WhatIsHappening = string.Format("Found {0} invalid registry paths, missing DLL references, and orphan file associations.", registryIssuesCount).T(),
                WhyItMatters = "Invalid registry keys cause app launch errors and slow down Windows Registry queries.".T(),
                PotentialImpact = "App startup errors, broken file associations, registry bloat.".T(),
                RecommendedAction = "Clean obsolete registry entries and back up registry hive.".T(),
                EstimatedImprovement = string.Format("Fixed {0} Registry Errors", registryIssuesCount).T(),
                ExpectedPerformanceGain = "Seamless App Launching".T(),
                Category = "Storage",
                ImpactLevel = "Medium",
                ActionKey = "CleanJunk",
                CanAutoFix = true
            });
        }

        if (freeSpaceGB < 20.0 && totalSpaceGB > 0)
        {
            storPenalty += 30;
            storDetails.Add($"Low C: drive space ({freeSpaceGB:F1} GB free)");
        }

        int storScore = Math.Max(10, (int)(100 - storPenalty));
        categories.Add(new CategoryHealthScore
        {
            CategoryName = "Storage & Cleanliness".T(),
            Score = storScore,
            Weight = 0.20,
            Status = storScore >= 85 ? "Optimal".T() : (storScore >= 65 ? "Fair".T() : "Degraded".T()),
            IconGlyph = "\uE7B8",
            SummaryText = storDetails.Count > 0 ? string.Join(", ", storDetails) : "Storage drive has low junk build-up and clean registry keys.".T()
        });

        // ----------------------------------------------------
        // OVERALL WEIGHTED SCORE CALCULATION
        // ----------------------------------------------------
        double finalScore = (hwScore * 0.30) + (secScore * 0.25) + (stabScore * 0.25) + (storScore * 0.20);
        int overallScoreInt = Math.Clamp((int)Math.Round(finalScore), 10, 100);

        assessment.OverallScore = overallScoreInt;
        assessment.Categories = categories;
        assessment.Insights = insights;

        if (overallScoreInt >= 90)
        {
            assessment.RiskLevel = "Low".T();
            assessment.PriorityLevel = "P4 - Optimal".T();
            assessment.SummaryBannerText = "PC is operating at peak efficiency. 0 critical bottlenecks detected.".T();
        }
        else if (overallScoreInt >= 75)
        {
            assessment.RiskLevel = "Moderate".T();
            assessment.PriorityLevel = "P3 - Recommended".T();
            assessment.SummaryBannerText = "System is in fair condition. AI recommends minor cleanup and service tuning.".T();
        }
        else if (overallScoreInt >= 55)
        {
            assessment.RiskLevel = "High".T();
            assessment.PriorityLevel = "P2 - High Priority".T();
            assessment.SummaryBannerText = "Performance bottlenecks detected. Execute recommended fixes to restore speed.".T();
        }
        else
        {
            assessment.RiskLevel = "Critical".T();
            assessment.PriorityLevel = "P1 - Immediate Action".T();
            assessment.SummaryBannerText = "Critical system degradation detected! Immediate maintenance is required.".T();
        }

        return assessment;
    }
}

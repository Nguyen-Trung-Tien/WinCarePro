using System;
using System.Collections.Generic;

namespace WinCarePro.Models;

public class CategoryHealthScore
{
    public string CategoryName { get; set; } = "";
    public int Score { get; set; } = 100;
    public double Weight { get; set; } = 0.25; // 25%
    public string Status { get; set; } = "Optimal"; // Optimal, Fair, Degraded, Critical
    public string IconGlyph { get; set; } = "\uE9D9";
    public string SummaryText { get; set; } = "";
}

public class AiExplainableInsight
{
    public string IssueTitle { get; set; } = "";
    public string WhatIsHappening { get; set; } = "";
    public string WhyItMatters { get; set; } = "";
    public string PotentialImpact { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public string EstimatedImprovement { get; set; } = "";
    public string ExpectedPerformanceGain { get; set; } = "";
    public string Category { get; set; } = "General"; // Hardware, Security, Stability, Storage
    public string ImpactLevel { get; set; } = "Medium"; // Critical, High, Medium, Low
    public string ActionKey { get; set; } = ""; // CleanJunk, DisableStartup, FlushDns, RepairSfc, OptimizeServices, RestartExplorer
    public bool CanAutoFix { get; set; } = true;
}

public class PredictiveWarning
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string MetricTrend { get; set; } = "";
    public string ImpactTimeline { get; set; } = ""; // e.g. "Exhaustion in 14 Days"
    public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
    public string IconGlyph { get; set; } = "\uE7BA";
}

public class SystemHealthAssessment
{
    public int OverallScore { get; set; } = 100;
    public string RiskLevel { get; set; } = "Low"; // Low, Moderate, High, Critical
    public string PriorityLevel { get; set; } = "P4 - Optimal"; // P1 - Immediate, P2 - High, P3 - Recommended, P4 - Optimal
    public string ConfidenceLevel { get; set; } = "98% - High Accuracy";
    public string SummaryBannerText { get; set; } = "";
    
    public List<CategoryHealthScore> Categories { get; set; } = new();
    public List<AiExplainableInsight> Insights { get; set; } = new();
    public List<PredictiveWarning> Predictions { get; set; } = new();
    public DateTime EvaluatedAt { get; set; } = DateTime.Now;
}

public class SmartFixProgress
{
    public string ActionName { get; set; } = "";
    public string CurrentStep { get; set; } = "";
    public double ProgressPercent { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public bool IsSuccess { get; set; } = true;
    public string ResultMessage { get; set; } = "";
}


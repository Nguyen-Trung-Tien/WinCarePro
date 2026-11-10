using System.Collections.Generic;
using WinCarePro.Engines;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests;

public class AiWinCareEngineTests
{
    [Fact]
    public void EvaluateHealth_WithOptimalMetrics_ShouldReturnHighScore()
    {
        // Arrange
        var scoringEngine = new AiWinCareScoringEngine();

        // Act
        var assessment = scoringEngine.EvaluateHealth(
            cpuUsage: 15.0,
            ramUsage: 40.0,
            diskActiveTime: 5.0,
            freeSpaceGB: 100.0,
            totalSpaceGB: 500.0,
            startupAppsCount: 3,
            junkSizeBytes: 50 * 1024 * 1024,
            registryIssuesCount: 2,
            outdatedAppsCount: 0,
            pingLatencyMs: 15.0,
            packetLossPercent: 0.0,
            securityAudits: new List<string>()
        );

        // Assert
        Assert.NotNull(assessment);
        Assert.True(assessment.OverallScore >= 90);
        Assert.Equal(4, assessment.Categories.Count);
    }

    [Fact]
    public void GeneratePredictiveWarnings_WithLowDiskSpace_ShouldGenerateWarning()
    {
        // Arrange
        var predictiveEngine = new PredictiveAnalysisEngine();

        // Act
        var warnings = predictiveEngine.GeneratePredictiveWarnings(
            freeSpaceGB: 8.0,
            totalSpaceGB: 256.0,
            startupAppsCount: 15,
            ramUsagePercent: 88.0,
            pingLatencyMs: 150.0,
            packetLossPercent: 4.0
        );

        // Assert
        Assert.NotNull(warnings);
        Assert.True(warnings.Count >= 2);
    }

    [Fact]
    public async Task AiWinCareEngine_AnalyzeSystemHealthAsync_ShouldReturnValidReport()
    {
        // Act
        var report = await Modules.AiAssistant.AiWinCareEngine.AnalyzeSystemHealthAsync();

        // Assert
        Assert.NotNull(report);
        Assert.InRange(report.OverallScore, 20, 100);
        Assert.False(string.IsNullOrWhiteSpace(report.HealthStatus));
        Assert.False(string.IsNullOrWhiteSpace(report.SummaryText));
        Assert.NotNull(report.Recommendations);
    }

    [Fact]
    public async Task AiWinCareEngine_ExecuteSmartRemedyBatchAsync_ShouldExecuteSafely()
    {
        // Act
        var remedyResult = await Modules.AiAssistant.AiWinCareEngine.ExecuteSmartRemedyBatchAsync();

        // Assert
        Assert.NotNull(remedyResult);
        Assert.True(remedyResult.FixedActionsCount > 0);
        Assert.NotEmpty(remedyResult.ActionLogs);
    }

    [Theory]
    [InlineData("máy bị tràn ram", "NavigateOptimizer")]
    [InlineData("dọn rác ổ đĩa", "NavigateJunkCleaner")]
    [InlineData("ổ c bị đầy dung lượng", "NavigateDisk")]
    [InlineData("chơi game bị lag drop fps", "NavigateOptimizer")]
    [InlineData("mạng wifi bị chậm ping cao", "NavigateNetwork")]
    [InlineData("khởi động win chậm", "NavigateStartup")]
    [InlineData("lỗi hệ thống sfc", "NavigateRepair")]
    public void AiWinCareEngine_GetOfflineDiagnosticAdvice_ShouldRouteCorrectly(string query, string expectedActionKey)
    {
        // Act
        var advice = Modules.AiAssistant.AiWinCareEngine.GetOfflineDiagnosticAdvice(query);

        // Assert
        Assert.NotNull(advice);
        Assert.False(string.IsNullOrWhiteSpace(advice.Title));
        Assert.False(string.IsNullOrWhiteSpace(advice.Diagnosis));
        Assert.False(string.IsNullOrWhiteSpace(advice.Solution));
        Assert.Equal(expectedActionKey, advice.RecommendedActionKey);
    }

    [Fact]
    public void AiWinCareEngine_GetOfflineDiagnosticAdvice_EmptyQuery_ReturnsReadyState()
    {
        // Act
        var advice = Modules.AiAssistant.AiWinCareEngine.GetOfflineDiagnosticAdvice("");

        // Assert
        Assert.NotNull(advice);
        Assert.Equal("None", advice.RecommendedActionKey);
    }
}


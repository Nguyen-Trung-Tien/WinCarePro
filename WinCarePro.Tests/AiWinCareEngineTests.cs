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
    public async Task SmartFixService_CleanJunkAction_ShouldExecuteSuccessfully()
    {
        // Arrange
        var fixService = new SmartFixService();
        bool progressCalled = false;

        // Act
        await fixService.ExecuteFixAsync("CleanJunk", p =>
        {
            progressCalled = true;
        });

        // Assert
        Assert.True(progressCalled);
    }
}

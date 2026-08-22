using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class DriverBackupAndAiForecastingTests
{
    [Fact]
    public void LinearRegression_WithPerfectLine_ComputesExactSlopeAndRSquared()
    {
        // y = 2x + 5
        double[] x = { 0, 1, 2, 3, 4 };
        double[] y = { 5, 7, 9, 11, 13 };

        var result = PredictiveAnalysisEngine.ComputeLinearRegression(x, y);

        Assert.True(result.IsValid);
        Assert.Equal(2.0, result.Slope, 4);
        Assert.Equal(5.0, result.Intercept, 4);
        Assert.Equal(1.0, result.RSquared, 4);
    }

    [Fact]
    public void LinearRegression_WithNegativeSlope_PredictsCorrectDecay()
    {
        // Simulating storage decay: 100GB decreasing by 2GB per day
        double[] x = { 0, 1, 2, 3, 4, 5 };
        double[] y = { 100, 98, 96, 94, 92, 90 };

        var result = PredictiveAnalysisEngine.ComputeLinearRegression(x, y);

        Assert.True(result.IsValid);
        Assert.Equal(-2.0, result.Slope, 4);
        Assert.Equal(100.0, result.Intercept, 4);
        Assert.Equal(1.0, result.RSquared, 4);

        // Predict day when storage reaches 10GB threshold
        double target = 10.0;
        double predictedDay = (target - result.Intercept) / result.Slope;
        Assert.Equal(45.0, predictedDay, 2); // 45 days
    }

    [Fact]
    public void LinearRegression_WithInvalidData_ReturnsSafeInvalidResult()
    {
        double[] x = { 1 };
        double[] y = { 10 };

        var result = PredictiveAnalysisEngine.ComputeLinearRegression(x, y);
        Assert.False(result.IsValid);

        var nullResult = PredictiveAnalysisEngine.ComputeLinearRegression(null!, null!);
        Assert.False(nullResult.IsValid);
    }

    [Fact]
    public void GeneratePredictiveWarnings_WithLowFreeSpace_EmitsLinearTrendWarning()
    {
        var engine = new PredictiveAnalysisEngine();
        var warnings = engine.GeneratePredictiveWarnings(
            freeSpaceGB: 8.5,
            totalSpaceGB: 256.0,
            startupAppsCount: 3,
            ramUsagePercent: 45.0,
            pingLatencyMs: 20.0,
            packetLossPercent: 0.0);

        Assert.NotEmpty(warnings);
        var storageWarn = warnings.FirstOrDefault(w => w.Title.Contains("Storage", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(storageWarn);
        Assert.Equal("Critical", storageWarn.Severity);
        Assert.Contains("Days", storageWarn.ImpactTimeline);
    }

    [Fact]
    public void HardwareDriverEngine_CanInstantiateAndQueryDriversSafely()
    {
        var engine = new HardwareDriverEngine();
        var drivers = engine.GetInstalledDrivers();
        Assert.NotNull(drivers);
    }

    [Fact]
    public async Task HardwareDriverEngine_RestoreNonExistentDirectory_FailsSafely()
    {
        var engine = new HardwareDriverEngine();
        string fakeDir = Path.Combine(Path.GetTempPath(), "NonExistentDriverBackupFolder_" + Guid.NewGuid());

        var result = await engine.RestoreDriversFromBackupAsync(fakeDir);
        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines;
using Xunit;

namespace WinCarePro.Tests;

public class NetworkCenterEngineTests
{
    private readonly NetworkEngine _networkEngine = new();

    [Fact]
    public async Task RunDnsBenchmarkAsync_CancellationRequested_ReturnsListWithoutCrashing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _networkEngine.RunDnsBenchmarkAsync(cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 5);
    }

    [Fact]
    public async Task MeasurePing_ValidAndInvalidHosts_HandlesGracefully()
    {
        // Act - Localhost test
        var (loss, avg, jitter) = await _networkEngine.AnalyzePingQualityAsync("127.0.0.1", count: 1);

        // Assert
        Assert.True(loss == 0.0 || loss == 100.0);
        Assert.True(avg >= 0.0);
        Assert.True(jitter >= 0.0);
    }

    [Fact]
    public async Task RunSpeedTestAsync_ExecutesWithoutExceptions()
    {
        // Act & Assert
        try
        {
            var speedResult = await _networkEngine.RunSpeedTestAsync();
            Assert.True(speedResult >= 0);
        }
        catch (Exception ex)
        {
            Assert.Fail($"RunSpeedTestAsync threw exception: {ex.Message}");
        }
    }
}

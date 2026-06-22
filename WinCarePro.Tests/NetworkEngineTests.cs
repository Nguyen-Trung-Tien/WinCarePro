using System;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class NetworkEngineTests
{
    private readonly NetworkEngine _engine = new();

    [Fact]
    public async Task AnalyzePingQuality_ReturnsValidRanges()
    {
        // Act
        // We use a small count (e.g. 2) to keep the test execution fast.
        var (packetLoss, avgLatency) = await _engine.AnalyzePingQualityAsync(target: "127.0.0.1", count: 2);

        // Assert
        Assert.True(packetLoss >= 0.0 && packetLoss <= 100.0, $"Packet loss ({packetLoss}%) out of range [0, 100]");
        Assert.True(avgLatency >= 0.0, $"Average latency ({avgLatency} ms) must be non-negative");
    }

    [Fact]
    public void CheckInternetConnection_ReturnsBoolean()
    {
        // Act & Assert
        // This test only verifies that the method runs without throwing exceptions
        try
        {
            var isConnected = _engine.CheckInternetConnection();
            // Just verify it's a boolean response
            Assert.True(isConnected || !isConnected);
        }
        catch (Exception ex)
        {
            Assert.Fail($"CheckInternetConnection threw an exception: {ex.Message}");
        }
    }
}

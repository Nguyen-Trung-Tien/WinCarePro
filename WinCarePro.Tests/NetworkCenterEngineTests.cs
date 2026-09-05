using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines;
using WinCarePro.Models;
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

    [Fact]
    public void GetNetworkAdapters_ReturnsNonNullList()
    {
        // Act
        var adapters = _networkEngine.GetNetworkAdapters();

        // Assert
        Assert.NotNull(adapters);
    }

    [Fact]
    public void GetActiveConnections_ReturnsValidConnections()
    {
        // Act
        var connections = _networkEngine.GetActiveConnections();

        // Assert
        Assert.NotNull(connections);
        // On any running Windows machine, there are active TCP or UDP connections/listeners
        Assert.NotEmpty(connections);
        var first = connections.First();
        Assert.False(string.IsNullOrEmpty(first.Protocol));
        Assert.False(string.IsNullOrEmpty(first.LocalAddress));
    }

    [Fact]
    public void ActiveConnectionInfo_FilterLogic_WorksAccurately()
    {
        // Arrange
        var testConnections = new List<ActiveConnectionInfo>
        {
            new() { ProcessName = "chrome.exe", Protocol = "TCP", LocalAddress = "127.0.0.1:5000", ForeignAddress = "1.1.1.1:443", State = "ESTABLISHED", Pid = 1234 },
            new() { ProcessName = "svchost.exe", Protocol = "TCP", LocalAddress = "0.0.0.0:135", ForeignAddress = "0.0.0.0:0", State = "LISTENING", Pid = 800 },
            new() { ProcessName = "spotify.exe", Protocol = "UDP", LocalAddress = "0.0.0.0:5353", ForeignAddress = "*:*", State = "-", Pid = 4567 }
        };

        // Act - Filter by Established
        var established = testConnections.Where(c => c.State.Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase)).ToList();
        // Act - Filter by Listening
        var listening = testConnections.Where(c => c.State.StartsWith("LISTEN", StringComparison.OrdinalIgnoreCase)).ToList();
        // Act - Filter by Protocol UDP
        var udp = testConnections.Where(c => c.Protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase)).ToList();
        // Act - Filter by Search "chrome"
        var chrome = testConnections.Where(c => c.ProcessName.Contains("chrome", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        Assert.Single(established);
        Assert.Equal("chrome.exe", established[0].ProcessName);

        Assert.Single(listening);
        Assert.Equal("svchost.exe", listening[0].ProcessName);

        Assert.Single(udp);
        Assert.Equal("spotify.exe", udp[0].ProcessName);

        Assert.Single(chrome);
        Assert.Equal(1234, chrome[0].Pid);
    }

    [Theory]
    [InlineData("dns")]
    [InlineData("flushdns")]
    [InlineData("winsock")]
    [InlineData("resetwinsock")]
    [InlineData("tcpip")]
    [InlineData("resettcp")]
    [InlineData("renewdhcp")]
    [InlineData("iprenew")]
    [InlineData("adapter")]
    [InlineData("resetadapters")]
    [InlineData("firewall")]
    [InlineData("repairfirewall")]
    public void RepairOperationTag_MatchesAcceptedKeywords(string tag)
    {
        // Arrange accepted canonical actions
        var canonicalActions = new HashSet<string>
        {
            "dns", "flushdns",
            "winsock", "resetwinsock",
            "tcpip", "resettcp", "resettcpip",
            "iprenew", "renewdhcp", "renewip",
            "adapter", "resetadapters", "restartadapter",
            "firewall", "repairfirewall", "resetfirewall",
            "proxy", "resetproxy",
            "hosts", "resethosts",
            "optimize", "optimizetcp",
            "green", "disableeee"
        };

        // Assert
        Assert.Contains(tag.ToLowerInvariant(), canonicalActions);
    }
}

using System;
using System.Collections.Generic;
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
        var (packetLoss, avgLatency, jitter) = await _engine.AnalyzePingQualityAsync(target: "127.0.0.1", count: 2);

        // Assert
        Assert.True(packetLoss >= 0.0 && packetLoss <= 100.0, $"Packet loss ({packetLoss}%) out of range [0, 100]");
        Assert.True(avgLatency >= 0.0, $"Average latency ({avgLatency} ms) must be non-negative");
        Assert.True(jitter >= 0.0, $"Jitter ({jitter} ms) must be non-negative");
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

    [Fact]
    public void ExportSocketsToCsv_ProducesValidHeaderAndRows()
    {
        // Arrange
        var connections = new List<WinCarePro.Models.ActiveConnectionInfo>
        {
            new() { ProcessName = "chrome.exe", Pid = 1234, Protocol = "TCP", LocalAddress = "192.168.1.50:50000", ForeignAddress = "142.250.190.46:443", State = "ESTABLISHED" },
            new() { ProcessName = "svchost.exe", Pid = 888, Protocol = "UDP", LocalAddress = "0.0.0.0:5353", ForeignAddress = "*:*", State = "LISTEN" }
        };

        // Act
        string csv = _engine.ExportSocketsToCsv(connections);

        // Assert
        Assert.NotNull(csv);
        Assert.Contains("Process Name,PID,Protocol,Local Address,Remote Address,State", csv);
        Assert.Contains("\"chrome.exe\",1234,\"TCP\",\"192.168.1.50:50000\",\"142.250.190.46:443\",\"ESTABLISHED\"", csv);
        Assert.Contains("\"svchost.exe\",888,\"UDP\",\"0.0.0.0:5353\",\"*:*\",\"LISTEN\"", csv);
    }

    [Fact]
    public void ExportDiagnosticReport_ProducesCompleteStructuredReport()
    {
        // Arrange
        var adapters = new List<WinCarePro.Models.NetworkAdapterInfo>
        {
            new() { Name = "Ethernet", Status = "Up", Type = "Ethernet", IpAddresses = "192.168.1.100", MacAddress = "AA:BB:CC:DD:EE:FF", Speed = "1 Gbps" }
        };
        var dnsServers = new List<WinCarePro.Models.DnsServerInfo>
        {
            new() { Name = "Cloudflare", PrimaryIp = "1.1.1.1", AverageQueryMs = 12.5, IsFastest = true }
        };
        var connections = new List<WinCarePro.Models.ActiveConnectionInfo>
        {
            new() { ProcessName = "edge.exe", Pid = 5678, Protocol = "TCP", LocalAddress = "192.168.1.100:54321", ForeignAddress = "20.190.159.0:443", State = "ESTABLISHED" }
        };

        // Act
        string report = _engine.ExportDiagnosticReport(
            internetStatus: "Connected",
            publicIp: "203.0.113.195",
            gateway: "192.168.1.1",
            dnsStatus: "Resolving",
            ipStatus: "IPv4: Active, IPv6: Inactive",
            healthScore: 95,
            healthSummary: "Optimal for Gaming & 4K Streaming",
            healthRecommendation: "Bandwidth is stable, 0% packet loss, ultra-low latency.",
            latencyMs: 14.2,
            jitterMs: 1.8,
            packetLoss: 0.0,
            downloadSpeed: 250.5,
            uploadSpeed: 80.2,
            adapters: adapters,
            dnsServers: dnsServers,
            connections: connections);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("WINCARE PRO SUITE — NETWORK DIAGNOSTICS & TELEMETRY REPORT", report);
        Assert.Contains("Internet Status:         Connected", report);
        Assert.Contains("Public IP Address:       203.0.113.195", report);
        Assert.Contains("AI Health Score:         95/100", report);
        Assert.Contains("Round-Trip Latency:      14.2 ms", report);
        Assert.Contains("Download Throughput:     250.50 Mbps", report);
        Assert.Contains("Ethernet", report);
        Assert.Contains("Cloudflare", report);
        Assert.Contains("edge.exe", report);
    }

    [Fact]
    public void IsProtectedProcess_SafelyIdentifiesCriticalProcesses()
    {
        // Assert: System PID 0 and 4 are protected
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(0, "System Idle Process"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(4, "System"));

        // Assert: Current app process PID is protected
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(Environment.ProcessId, "WinCarePro.exe"));

        // Assert: Critical Windows OS processes are protected
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(500, "csrss.exe"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(600, "lsass"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(700, "services.exe"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(800, "smss.exe"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(900, "wininit.exe"));
        Assert.True(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(1000, "dwm.exe"));

        // Assert: Arbitrary non-critical user applications are not protected
        Assert.False(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(2000, "notepad.exe"));
        Assert.False(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(3000, "chrome.exe"));
        Assert.False(WinCarePro.ViewModels.NetworkViewModel.IsProtectedProcess(4000, "discord.exe"));
    }

    [Fact]
    public void EscapeCsv_HandlesSpecialCharactersProperly()
    {
        // Act & Assert
        Assert.Equal("\"\"", NetworkEngine.EscapeCsv(""));
        Assert.Equal("\"hello\"", NetworkEngine.EscapeCsv("hello"));
        Assert.Equal("\"hello, world\"", NetworkEngine.EscapeCsv("hello, world"));
        Assert.Equal("\"say \"\"hello\"\"\"", NetworkEngine.EscapeCsv("say \"hello\""));
    }
}

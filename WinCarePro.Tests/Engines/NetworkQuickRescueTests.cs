using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.ViewModels;
using Xunit;

namespace WinCarePro.Tests;

public class NetworkQuickRescueTests
{
    private readonly NetworkEngine _engine = new();

    [Fact]
    public void GetPresetDnsProfiles_ReturnsRequiredProfiles()
    {
        var profiles = NetworkEngine.GetPresetDnsProfiles();

        Assert.NotNull(profiles);
        Assert.True(profiles.Count >= 5);

        // Verify Cloudflare (1.1.1.1 / 1.0.0.1)
        var cloudflare = profiles.FirstOrDefault(p => p.Id == "cloudflare");
        Assert.NotNull(cloudflare);
        Assert.Equal("1.1.1.1", cloudflare.PrimaryDns);
        Assert.Equal("1.0.0.1", cloudflare.SecondaryDns);

        // Verify Google (8.8.8.8 / 8.8.4.4)
        var google = profiles.FirstOrDefault(p => p.Id == "google");
        Assert.NotNull(google);
        Assert.Equal("8.8.8.8", google.PrimaryDns);
        Assert.Equal("8.8.4.4", google.SecondaryDns);

        // Verify AdGuard (94.140.14.14 / 94.140.15.15)
        var adguard = profiles.FirstOrDefault(p => p.Id == "adguard");
        Assert.NotNull(adguard);
        Assert.Equal("94.140.14.14", adguard.PrimaryDns);
        Assert.Equal("94.140.15.15", adguard.SecondaryDns);

        // Verify Quad9 (9.9.9.9 / 149.112.112.112)
        var quad9 = profiles.FirstOrDefault(p => p.Id == "quad9");
        Assert.NotNull(quad9);
        Assert.Equal("9.9.9.9", quad9.PrimaryDns);
        Assert.Equal("149.112.112.112", quad9.SecondaryDns);

        // Verify DHCP (Automatic)
        var dhcp = profiles.FirstOrDefault(p => p.Id == "dhcp");
        Assert.NotNull(dhcp);
        Assert.True(dhcp.IsDhcp);
    }

    [Fact]
    public void GetActiveAdapterSummary_ExecutesSafely()
    {
        var summary = _engine.GetActiveAdapterSummary();

        // Either returns an active adapter or null if offline/mocked
        if (summary != null)
        {
            Assert.False(string.IsNullOrWhiteSpace(summary.Name));
            Assert.False(string.IsNullOrWhiteSpace(summary.Status));
        }
    }

    [Fact]
    public void BackupCurrentDns_SetsHasDnsBackup()
    {
        var backup = _engine.BackupCurrentDns();

        Assert.True(_engine.HasDnsBackup);
        Assert.NotNull(backup.DnsServers);
    }

    [Fact]
    public async Task RunOnDemandDiagnosticsAsync_ExecutesGracefully()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await _engine.RunOnDemandDiagnosticsAsync(cts.Token);

        Assert.NotNull(result);
        Assert.NotNull(result.GatewayLatencyText);
        Assert.NotNull(result.CloudflareLatencyText);
        Assert.NotNull(result.GoogleLatencyText);
    }

    [Fact]
    public void NetworkViewModel_InitializesFluentCoreCorrectly()
    {
        var vm = new NetworkViewModel();
        vm.InitializeFluentCore();

        Assert.NotNull(vm.DnsProfiles);
        Assert.True(vm.DnsProfiles.Count >= 5);
        Assert.NotNull(vm.SelectedDnsProfile);
        Assert.False(string.IsNullOrWhiteSpace(vm.DiagnosticsStatusText));
    }
}


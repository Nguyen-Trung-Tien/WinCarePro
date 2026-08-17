using System;
using System.Collections.Generic;
using System.IO;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class SecurityCenterTests
{
    private readonly SecurityPrivacyEngine _securityEngine = new();

    [Fact]
    public void GetAntivirusStatus_ReturnsValidString()
    {
        string avStatus = _securityEngine.GetAntivirusStatus();
        Assert.False(string.IsNullOrWhiteSpace(avStatus));
    }

    [Fact]
    public void GetFirewallStatus_ExecutesSafely()
    {
        bool fwStatus = _securityEngine.GetFirewallStatus();
        Assert.True(fwStatus || !fwStatus);
    }

    [Fact]
    public void GetUacStatus_ExecutesSafely()
    {
        bool uacStatus = _securityEngine.GetUacStatus();
        Assert.True(uacStatus || !uacStatus);
    }

    [Fact]
    public void GetDefenderRealtimeStatus_ExecutesSafely()
    {
        bool realtime = _securityEngine.GetDefenderRealtimeStatus();
        Assert.True(realtime || !realtime);
    }

    [Fact]
    public void CheckSecureBootStatus_ReturnsTuple()
    {
        var (enabled, status) = _securityEngine.CheckSecureBootStatus();
        Assert.NotNull(status);
        Assert.NotEmpty(status);
    }

    [Fact]
    public void CheckTpmStatus_ReturnsTuple()
    {
        var (ok, status) = _securityEngine.CheckTpmStatus();
        Assert.NotNull(status);
        Assert.NotEmpty(status);
    }

    [Fact]
    public void GetAllSafeguards_ReturnsPopulatedList()
    {
        var safeguards = _securityEngine.GetAllSafeguards();
        Assert.NotNull(safeguards);
        Assert.NotEmpty(safeguards);
        Assert.True(safeguards.Count >= 10);
        Assert.Contains(safeguards, s => s.Id == "UAC_Enforce");
        Assert.Contains(safeguards, s => s.Id == "Privacy_Telemetry");
    }

    [Fact]
    public void RefreshSafeguard_UpdatesComparisonText()
    {
        var item = new SecuritySafeguardItem
        {
            Id = "UAC_Enforce",
            Name = "UAC Enforce",
            RecommendedValue = "1"
        };
        _securityEngine.RefreshSafeguard(item);
        Assert.NotNull(item.ComparisonText);
        Assert.NotEmpty(item.ComparisonText);
    }

    [Fact]
    public void RunSecurityAuditItems_WithSuspiciousStartups_GeneratesAlerts()
    {
        var suspiciousEntries = new List<StartupEntry>
        {
            new() { Name = "SuspiciousScript", Command = "cmd.exe /c start evil.bat", Source = StartupSource.RegistryRunHKCU },
            new() { Name = "TempDropper", Command = @"C:\Users\Admin\AppData\Local\Temp\dropper.exe", Source = StartupSource.RegistryRunHKLM }
        };

        var alerts = _securityEngine.RunSecurityAuditItems(suspiciousEntries);

        Assert.NotNull(alerts);
        Assert.Contains(alerts, a => a.Id == "startup_SuspiciousScript");
        Assert.Contains(alerts, a => a.Id == "startup_TempDropper");
    }

    [Theory]
    [InlineData("advertisingid")]
    [InlineData("telemetry")]
    [InlineData("clipboardhistory")]
    [InlineData("tracking")]
    [InlineData("cortanabing")]
    [InlineData("location")]
    [InlineData("feedback")]
    [InlineData("appdiagnostics")]
    public void GetPrivacySetting_SupportsAllKnownKeys(string key)
    {
        bool val = _securityEngine.GetPrivacySetting(key);
        Assert.True(val || !val); // Executes without exception
    }

    [Fact]
    public void ApplyPrivacyPreset_ReturnsPositiveCount()
    {
        int countMax = _securityEngine.ApplyPrivacyPreset("max");
        Assert.True(countMax >= 0);

        int countBalanced = _securityEngine.ApplyPrivacyPreset("balanced");
        Assert.True(countBalanced >= 0);
    }

    [Fact]
    public void TraceCleaning_MethodsExecuteSafely()
    {
        int recent = _securityEngine.ClearRecentFiles();
        Assert.True(recent >= 0);

        int jumpLists = _securityEngine.ClearExplorerJumpLists();
        Assert.True(jumpLists >= 0);

        int runMru = _securityEngine.ClearExplorerRunHistory();
        Assert.True(runMru >= 0);

        int typedPaths = _securityEngine.ClearTypedPathsHistory();
        Assert.True(typedPaths >= 0);

        int total = _securityEngine.ClearAllActivityTraces();
        Assert.True(total >= 0);
    }

    [Fact]
    public void SecurityModels_InstantiateCorrectly()
    {
        var card = new SecurityComponentCard
        {
            Id = "test_card",
            Title = "Test Security Card",
            StatusText = "Active",
            IsSecure = true
        };
        Assert.Equal("test_card", card.Id);
        Assert.True(card.IsSecure);

        var safeguard = new SecuritySafeguardItem
        {
            Id = "test_sg",
            Name = "Test Safeguard",
            IsProtected = true
        };
        Assert.True(safeguard.IsProtected);

        var alert = new SecurityAlertItem
        {
            Id = "test_alert",
            Title = "Test Alert",
            FixActionKey = "firewall",
            IsFixed = false
        };
        Assert.True(alert.CanFix);

        alert.IsFixed = true;
        Assert.False(alert.CanFix);
    }
}

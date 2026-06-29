using System;
using System.Linq;
using Xunit;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Tests;

public class SystemOptimizerEngineTests
{
    private readonly SystemOptimizerEngine _engine = new();

    public SystemOptimizerEngineTests()
    {
        Services.TranslationManager.Instance.CurrentLanguage = Services.AppLanguage.English;
    }

    [Fact]
    public void GetTweaks_ReturnsAllConfiguredTweaks()
    {
        // Act
        var tweaks = _engine.GetTweaks();

        // Assert
        Assert.NotNull(tweaks);
        Assert.Equal(13, tweaks.Count);
        
        // Verify specific tweak IDs exist
        Assert.Contains(tweaks, t => t.Id == "MenuShowDelay");
        Assert.Contains(tweaks, t => t.Id == "AutoEndTasks");
        Assert.Contains(tweaks, t => t.Id == "WaitToKillAppTimeout");
        Assert.Contains(tweaks, t => t.Id == "NtfsDisableLastAccessUpdate");
        Assert.Contains(tweaks, t => t.Id == "NetworkThrottlingIndex");
        Assert.Contains(tweaks, t => t.Id == "SystemResponsiveness");
        Assert.Contains(tweaks, t => t.Id == "AllowAutoGameMode");
        Assert.Contains(tweaks, t => t.Id == "HwSchMode");
        Assert.Contains(tweaks, t => t.Id == "AllowTelemetry");
        Assert.Contains(tweaks, t => t.Id == "AllowCortana");
        Assert.Contains(tweaks, t => t.Id == "WerDisabled");
        Assert.Contains(tweaks, t => t.Id == "DisableBackoff");
        Assert.Contains(tweaks, t => t.Id == "MinAnimate");
    }

    [Theory]
    [InlineData("MenuShowDelay", "50", "Performance")]
    [InlineData("AutoEndTasks", "1", "Performance")]
    [InlineData("WaitToKillAppTimeout", "2000", "Performance")]
    [InlineData("NtfsDisableLastAccessUpdate", "1", "System & Disk")]
    [InlineData("NetworkThrottlingIndex", "-1", "Performance")]
    [InlineData("SystemResponsiveness", "0", "Performance")]
    [InlineData("AllowAutoGameMode", "1", "Gaming & GPU")]
    [InlineData("HwSchMode", "2", "Gaming & GPU")]
    [InlineData("AllowTelemetry", "0", "Privacy & Logs")]
    [InlineData("AllowCortana", "0", "Privacy & Logs")]
    [InlineData("WerDisabled", "1", "Privacy & Logs")]
    [InlineData("DisableBackoff", "1", "System & Disk")]
    [InlineData("MinAnimate", "0", "Performance")]
    public void GetTweaks_VerifiesTweakConfigurations(string id, string recommendedValue, string category)
    {
        // Act
        var tweaks = _engine.GetTweaks();
        var tweak = tweaks.FirstOrDefault(t => t.Id == id);

        // Assert
        Assert.NotNull(tweak);
        Assert.Equal(recommendedValue, tweak.RecommendedValue);
        Assert.Equal(category, tweak.Category);
        Assert.False(string.IsNullOrWhiteSpace(tweak.Name));
        Assert.False(string.IsNullOrWhiteSpace(tweak.Description));
    }
}

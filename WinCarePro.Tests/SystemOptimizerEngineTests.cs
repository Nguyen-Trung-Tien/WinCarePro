using System;
using System.Linq;
using Xunit;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Tests;

public class SystemOptimizerEngineTests
{
    private readonly SystemOptimizerEngine _engine = new();

    [Fact]
    public void GetTweaks_ReturnsAllConfiguredTweaks()
    {
        // Act
        var tweaks = _engine.GetTweaks();

        // Assert
        Assert.NotNull(tweaks);
        Assert.Equal(6, tweaks.Count);
        
        // Verify specific tweak IDs exist
        Assert.Contains(tweaks, t => t.Id == "MenuShowDelay");
        Assert.Contains(tweaks, t => t.Id == "AutoEndTasks");
        Assert.Contains(tweaks, t => t.Id == "WaitToKillAppTimeout");
        Assert.Contains(tweaks, t => t.Id == "NtfsDisableLastAccessUpdate");
        Assert.Contains(tweaks, t => t.Id == "NetworkThrottlingIndex");
        Assert.Contains(tweaks, t => t.Id == "SystemResponsiveness");
    }

    [Theory]
    [InlineData("MenuShowDelay", "50", "UI Responsiveness")]
    [InlineData("AutoEndTasks", "1", "Performance")]
    [InlineData("WaitToKillAppTimeout", "2000", "Performance")]
    [InlineData("NtfsDisableLastAccessUpdate", "1", "Disk & SSD")]
    [InlineData("NetworkThrottlingIndex", "-1", "Network Center")]
    [InlineData("SystemResponsiveness", "0", "Performance")]
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

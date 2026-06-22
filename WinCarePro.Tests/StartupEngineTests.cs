using Xunit;
using WinCarePro.Models;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class StartupEngineTests
{
    [Theory]
    [InlineData(50, "Low")]
    [InlineData(149, "Low")]
    [InlineData(150, "Medium")]
    [InlineData(400, "Medium")]
    [InlineData(500, "High")]
    [InlineData(1500, "High")]
    [InlineData(2000, "Critical")]
    [InlineData(5000, "Critical")]
    public void StartupEntry_ImpactClassification_MatchesSpecification(int delayMs, string expectedImpact)
    {
        // Arrange
        var entry = new StartupEntry
        {
            Name = "TestApp",
            Command = "test.exe",
            StartupDelayMs = delayMs
        };

        // Act
        var impact = entry.Impact;

        // Assert
        Assert.Equal(expectedImpact, impact);
    }
}

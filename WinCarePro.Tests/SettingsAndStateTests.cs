using System;
using System.IO;
using System.Text.Json;
using WinCarePro.Database;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class SettingsAndStateTests
{
    [Fact]
    public void SettingsProfile_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var profile = new SettingsProfile();

        // Assert
        Assert.Equal("Dark", profile.Theme);
        Assert.False(profile.AutoScan);
        Assert.Equal("TXT", profile.ReportFormat);
        Assert.True(profile.AutoCheckUpdates);
        Assert.True(profile.MinimizeToTray);
        Assert.Equal("Default", profile.AccentColor);
        Assert.True(profile.EnableAnimations);
        Assert.True(profile.ShowNotifications);
    }

    [Fact]
    public void SettingsProfile_Serialization_DeserializesAccurately()
    {
        // Arrange
        var original = new SettingsProfile
        {
            Theme = "Light",
            AutoScan = true,
            AccentColor = "Green",
            LanguageIndex = 1,
            TransparencyLevel = 90.0,
            EnableAnimations = false,
            AutoCleanupTriggerSizeGB = 10.0
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SettingsProfile>(json);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal("Light", restored.Theme);
        Assert.True(restored.AutoScan);
        Assert.Equal("Green", restored.AccentColor);
        Assert.Equal(1, restored.LanguageIndex);
        Assert.Equal(90.0, restored.TransparencyLevel);
        Assert.False(restored.EnableAnimations);
        Assert.Equal(10.0, restored.AutoCleanupTriggerSizeGB);
    }

    [Fact]
    public void DbManager_GetSettings_HandlesDatabaseInitializationAndCaching()
    {
        // Arrange & Act
        DbManager.InitializeDatabase();
        string settingsJson = DbManager.GetSettings();

        // Assert
        Assert.NotNull(settingsJson);
        Assert.NotEmpty(settingsJson);
    }

    [Fact]
    public void DbManager_SaveAndGetSettings_PersistsChangesCorrectly()
    {
        // Arrange
        DbManager.InitializeDatabase();
        var newProfile = new SettingsProfile
        {
            Theme = "Dark",
            AccentColor = "Purple",
            LanguageIndex = 0,
            AutoCheckUpdates = true,
            MinimizeToTray = true
        };
        string serialized = JsonSerializer.Serialize(newProfile);

        // Act
        DbManager.SaveSettings(serialized);
        string retrievedJson = DbManager.GetSettings();

        // Assert
        Assert.NotNull(retrievedJson);
        var retrievedProfile = JsonSerializer.Deserialize<SettingsProfile>(retrievedJson);
        Assert.NotNull(retrievedProfile);
        Assert.Equal("Purple", retrievedProfile.AccentColor);
    }
}

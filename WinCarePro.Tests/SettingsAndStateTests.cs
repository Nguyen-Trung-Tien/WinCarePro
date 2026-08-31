using System;
using System.IO;
using System.Text.Json;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
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

    [Fact]
    public void SettingsService_UpdateSettings_FiresEventAndUpdatesMemoryCache()
    {
        // Arrange
        var service = SettingsService.Instance;
        bool eventFired = false;
        string? changedProp = null;

        void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
        {
            eventFired = true;
            changedProp = e.PropertyName;
        }

        service.SettingsChanged += OnSettingsChanged;

        try
        {
            // Act
            service.UpdateSettings(s =>
            {
                s.AccentColor = "Amber";
                s.TransparencyLevel = 65.0;
            }, "AccentColor");

            // Assert
            Assert.True(eventFired);
            Assert.Equal("AccentColor", changedProp);
            Assert.Equal("Amber", service.CurrentSettings.AccentColor);
            Assert.Equal(65.0, service.CurrentSettings.TransparencyLevel);
        }
        finally
        {
            service.SettingsChanged -= OnSettingsChanged;
        }
    }

    [Fact]
    public void SettingsService_ExportAndImportJson_RoundTripsCorrectly()
    {
        // Arrange
        var service = SettingsService.Instance;
        service.UpdateSettings(s =>
        {
            s.Theme = "Light";
            s.AccentColor = "Pink";
            s.AutoCleanupTriggerSizeGB = 12.5;
            s.LanguageIndex = 1;
        });

        // Act
        string exportedJson = service.ExportSettingsJson();
        Assert.NotNull(exportedJson);
        Assert.Contains("\"AccentColor\": \"Pink\"", exportedJson);

        // Reset to default first
        service.ResetToDefaults();
        Assert.Equal("Default", service.CurrentSettings.AccentColor);

        // Import the exported backup
        bool importSuccess = service.ImportSettingsJson(exportedJson);

        // Assert
        Assert.True(importSuccess);
        Assert.Equal("Pink", service.CurrentSettings.AccentColor);
        Assert.Equal("Light", service.CurrentSettings.Theme);
        Assert.Equal(12.5, service.CurrentSettings.AutoCleanupTriggerSizeGB);
        Assert.Equal(1, service.CurrentSettings.LanguageIndex);
    }

    [Fact]
    public void SettingsService_ResetToDefaults_RestoresFactoryDefaults()
    {
        // Arrange
        var service = SettingsService.Instance;
        service.UpdateSettings(s =>
        {
            s.Theme = "Light";
            s.AccentColor = "Green";
            s.TransparencyLevel = 30.0;
        });

        // Act
        service.ResetToDefaults();

        // Assert
        Assert.Equal("Dark", service.CurrentSettings.Theme);
        Assert.Equal("Default", service.CurrentSettings.AccentColor);
        Assert.Equal(10.0, service.CurrentSettings.TransparencyLevel);
    }

    [Fact]
    public void SettingsService_CriticalSettings_PersistImmediatelyToDatabase()
    {
        // Arrange
        var service = SettingsService.Instance;
        
        // Act - Theme change
        service.UpdateSettings(s => s.Theme = "Light", "Theme");
        string dbJsonTheme = DbManager.GetSettings();
        var profileTheme = JsonSerializer.Deserialize<SettingsProfile>(dbJsonTheme);
        Assert.NotNull(profileTheme);
        Assert.Equal("Light", profileTheme.Theme);

        // Act - Language change
        service.UpdateSettings(s => s.LanguageIndex = 1, "LanguageIndex");
        string dbJsonLang = DbManager.GetSettings();
        var profileLang = JsonSerializer.Deserialize<SettingsProfile>(dbJsonLang);
        Assert.NotNull(profileLang);
        Assert.Equal(1, profileLang.LanguageIndex);

        // Act - AccentColor change
        service.UpdateSettings(s => s.AccentColor = "Purple", "AccentColor");
        string dbJsonAccent = DbManager.GetSettings();
        var profileAccent = JsonSerializer.Deserialize<SettingsProfile>(dbJsonAccent);
        Assert.NotNull(profileAccent);
        Assert.Equal("Purple", profileAccent.AccentColor);

        // Cleanup
        service.ResetToDefaults();
    }

    [Fact]
    public void SettingsService_FlushPendingSave_FlushesNonCriticalDebouncedSettings()
    {
        // Arrange
        var service = SettingsService.Instance;
        service.UpdateSettings(s => s.TransparencyLevel = 42.0); // Non-critical setting queued in debounce

        // Act
        service.FlushPendingSave();

        // Assert
        string dbJson = DbManager.GetSettings();
        var profile = JsonSerializer.Deserialize<SettingsProfile>(dbJson);
        Assert.NotNull(profile);
        Assert.Equal(42.0, profile.TransparencyLevel);

        // Cleanup
        service.ResetToDefaults();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class UninstallEngineTests
{
    [Fact]
    public void CleanAppNameForMatching_StripsVersionsAndTrademarks()
    {
        var engine = new UninstallEngine();

        string raw1 = "Google Chrome (R) version 125.0.6422.113 (64-bit)";
        string clean1 = engine.CleanAppNameForMatching(raw1);
        Assert.Equal("Google Chrome", clean1);

        string raw2 = "Microsoft Visual Studio Code™ v1.89.1";
        string clean2 = engine.CleanAppNameForMatching(raw2);
        Assert.Equal("Microsoft Visual Studio Code", clean2);

        string raw3 = "VLC Media Player 3.0.20";
        string clean3 = engine.CleanAppNameForMatching(raw3);
        Assert.Equal("VLC Media Player", clean3);
    }

    [Fact]
    public void IsSystemFolder_ProtectsWindowsAndCriticalDirectories()
    {
        var engine = new UninstallEngine();

        Assert.True(engine.IsSystemFolder("Windows"));
        Assert.True(engine.IsSystemFolder("System32"));
        Assert.True(engine.IsSystemFolder("SysWOW64"));
        Assert.True(engine.IsSystemFolder("Microsoft"));
        Assert.True(engine.IsSystemFolder("Windows Defender"));
        Assert.True(engine.IsSystemFolder("WindowsApps"));

        Assert.False(engine.IsSystemFolder("Spotify"));
        Assert.False(engine.IsSystemFolder("Discord"));
        Assert.False(engine.IsSystemFolder("VLC"));
    }

    [Fact]
    public void IsSystemKey_ProtectsCriticalRegistryKeys()
    {
        var engine = new UninstallEngine();

        Assert.True(engine.IsSystemKey("Microsoft"));
        Assert.True(engine.IsSystemKey("Windows"));
        Assert.True(engine.IsSystemKey("Classes"));
        Assert.True(engine.IsSystemKey("Policies"));

        Assert.False(engine.IsSystemKey("Adobe"));
        Assert.False(engine.IsSystemKey("Steam"));
    }

    [Fact]
    public void IsMatch_CorrectlyIdentifiesRelatedResidualFolders()
    {
        var engine = new UninstallEngine();

        string fullDisplayName = "Spotify Music";
        string cleanName = "Spotify";
        string fullPublisher = "Spotify AB";
        string cleanPublisher = "Spotify";

        Assert.True(engine.IsMatch("Spotify", fullDisplayName, cleanName, fullPublisher, cleanPublisher));
        Assert.True(engine.IsMatch("SpotifyMusic", fullDisplayName, cleanName, fullPublisher, cleanPublisher));
        Assert.True(engine.IsMatch("Spotify Music", fullDisplayName, cleanName, fullPublisher, cleanPublisher));

        Assert.False(engine.IsMatch("Mozilla", fullDisplayName, cleanName, fullPublisher, cleanPublisher));
        Assert.False(engine.IsMatch("", fullDisplayName, cleanName, fullPublisher, cleanPublisher));
    }

    [Fact]
    public void InstalledAppInfo_ModelPropertiesAndSizeCalculations()
    {
        var app = new InstalledAppInfo
        {
            DisplayName = "Test App",
            Publisher = "Test Publisher",
            Version = "1.0.0",
            SizeBytes = 1024 * 1024 * 600, // 600 MB
            IsStoreApp = false
        };

        Assert.True(app.IsDesktopApp);
        Assert.False(app.IsStoreApp);
        Assert.True(app.IsLargeApp);
        Assert.Equal(WinCarePro.Core.Helpers.FormatHelper.FormatBytes(1024 * 1024 * 600), app.SizeFormatted);
        Assert.Equal("Win32 Desktop", app.TypeBadgeText);
        Assert.Equal("\uE736", app.DefaultIconGlyph);

        // Store App
        var storeApp = new InstalledAppInfo
        {
            DisplayName = "Test Store App",
            SizeBytes = 1024 * 1024 * 50, // 50 MB
            IsStoreApp = true
        };

        Assert.False(storeApp.IsDesktopApp);
        Assert.True(storeApp.IsStoreApp);
        Assert.False(storeApp.IsLargeApp);
        Assert.Equal(WinCarePro.Core.Helpers.FormatHelper.FormatBytes(1024 * 1024 * 50), storeApp.SizeFormatted);
        Assert.Equal("Store App", storeApp.TypeBadgeText);
        Assert.Equal("\uE719", storeApp.DefaultIconGlyph);
    }

    [Fact]
    public void LeftoverItem_ModelPropertiesAndFormatting()
    {
        var dirItem = new LeftoverItem
        {
            Path = @"C:\Users\Test\AppData\Local\TestApp",
            DisplayName = "Residual Folder",
            Type = LeftoverType.Directory,
            SizeBytes = 1024 * 1024 * 10,
            IsSelected = true
        };

        Assert.Equal(WinCarePro.Core.Helpers.FormatHelper.FormatBytes(1024 * 1024 * 10), dirItem.SizeFormatted);
        Assert.Equal("\uE8B7", dirItem.IconGlyph);

        var regItem = new LeftoverItem
        {
            Path = @"HKCU\SOFTWARE\TestApp",
            DisplayName = "Residual Registry Key",
            Type = LeftoverType.RegistryKey
        };

        Assert.Equal("N/A", regItem.SizeFormatted);
        Assert.Equal("\uE945", regItem.IconGlyph);
    }

    [Fact]
    public void AppList_FilteringAndSorting_BehavesCorrectly()
    {
        var apps = new List<InstalledAppInfo>
        {
            new() { DisplayName = "Zoom", Publisher = "Zoom Video Communications", SizeBytes = 200 * 1024 * 1024, IsStoreApp = false, InstallDate = "2026-01-10" },
            new() { DisplayName = "Photoshop", Publisher = "Adobe Inc.", SizeBytes = 2500L * 1024 * 1024, IsStoreApp = false, InstallDate = "2026-05-15" },
            new() { DisplayName = "Calculator", Publisher = "Microsoft Corporation", SizeBytes = 30 * 1024 * 1024, IsStoreApp = true, InstallDate = "2025-11-20" }
        };

        // Filter: Desktop Apps only
        var desktopOnly = apps.Where(x => !x.IsStoreApp).ToList();
        Assert.Equal(2, desktopOnly.Count);

        // Filter: Store Apps only
        var storeOnly = apps.Where(x => x.IsStoreApp).ToList();
        Assert.Single(storeOnly);
        Assert.Equal("Calculator", storeOnly[0].DisplayName);

        // Filter: Large Apps (>500MB)
        var largeApps = apps.Where(x => x.IsLargeApp).ToList();
        Assert.Single(largeApps);
        Assert.Equal("Photoshop", largeApps[0].DisplayName);

        // Sort: Size descending (Max to Min)
        var sortedBySizeDesc = apps.OrderByDescending(x => x.SizeBytes).ToList();
        Assert.Equal("Photoshop", sortedBySizeDesc[0].DisplayName);
        Assert.Equal("Zoom", sortedBySizeDesc[1].DisplayName);
        Assert.Equal("Calculator", sortedBySizeDesc[2].DisplayName);

        // Sort: Name A-Z
        var sortedByName = apps.OrderBy(x => x.DisplayName).ToList();
        Assert.Equal("Calculator", sortedByName[0].DisplayName);
        Assert.Equal("Photoshop", sortedByName[1].DisplayName);
        Assert.Equal("Zoom", sortedByName[2].DisplayName);
    }
}

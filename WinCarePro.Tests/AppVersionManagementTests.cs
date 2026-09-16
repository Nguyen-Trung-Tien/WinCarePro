using System;
using System.IO;
using System.Text.RegularExpressions;
using WinCarePro.Core;
using WinCarePro.Infrastructure.Security;
using Xunit;

namespace WinCarePro.Tests;

public class AppVersionManagementTests
{
    [Fact]
    public void AppConstants_VersionProperties_AreStandardizedTo493()
    {
        Assert.Equal(4, AppConstants.CurrentVersion.Major);
        Assert.Equal(9, AppConstants.CurrentVersion.Minor);
        Assert.Equal(3, AppConstants.CurrentVersion.Build);
        Assert.Equal("4.9.3", AppConstants.VersionString);
        Assert.Equal("v4.9.3", AppConstants.DisplayVersion);
        Assert.Equal("v4.9.3", AppConstants.DisplayVersionFull);
        Assert.Equal("WinCare Pro", AppConstants.AppName);
        Assert.Equal("Orion", AppConstants.Codename);
        Assert.Contains("WinCare Pro v4.9.3", AppConstants.TitleWithVersion);
        Assert.Contains("Version 4.9.3 (Codename: Orion)", AppConstants.SystemBadgeText);
    }

    [Fact]
    public void ConfigurationFiles_And_Manifests_AreSynchronizedWithAppConstants()
    {
        // Locate workspace root
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        
        // 1. Check WinCarePro.csproj
        string csprojPath = Path.Combine(projectDir, "WinCarePro.csproj");
        if (File.Exists(csprojPath))
        {
            string csprojText = File.ReadAllText(csprojPath);
            Assert.Contains($"<Version>{AppConstants.VersionString}</Version>", csprojText);
            Assert.Contains($"<AssemblyVersion>{AppConstants.DefaultAssemblyVersionString}</AssemblyVersion>", csprojText);
            Assert.Contains($"<FileVersion>{AppConstants.DefaultAssemblyVersionString}</FileVersion>", csprojText);
        }

        // 2. Check setup.iss
        string setupIssPath = Path.Combine(projectDir, "setup.iss");
        if (File.Exists(setupIssPath))
        {
            string setupIssText = File.ReadAllText(setupIssPath);
            Assert.Contains($"#define MyAppVersion \"{AppConstants.VersionString}\"", setupIssText);
        }

        // 3. Check update.json
        string updateJsonPath = Path.Combine(projectDir, "update.json");
        if (File.Exists(updateJsonPath))
        {
            string updateJsonText = File.ReadAllText(updateJsonPath);
            Assert.Contains($"\"version\": \"{AppConstants.VersionString}\"", updateJsonText);
            var match = Regex.Match(updateJsonText, "\"sha256\":\\s*\"([a-fA-F0-9]{64})\"");
            Assert.True(match.Success, "update.json must contain a valid 64-character SHA-256 hash");

            // If a compiled installer exists in PublishOutput, verify hash matches exactly
            string setupPath = Path.Combine(projectDir, "PublishOutput", "WinCareProSetup.exe");
            if (File.Exists(setupPath))
            {
                string actualSetupHash = CryptoHelper.ComputeFileHash(setupPath);
                Assert.Equal(actualSetupHash.ToLowerInvariant(), match.Groups[1].Value.ToLowerInvariant());
            }
        }

        // 4. Check app.manifest
        string appManifestPath = Path.Combine(projectDir, "app.manifest");
        if (File.Exists(appManifestPath))
        {
            string manifestText = File.ReadAllText(appManifestPath);
            Assert.Contains($"<assemblyIdentity version=\"{AppConstants.DefaultAssemblyVersionString}\"", manifestText);
        }

        // 5. Check Package.appxmanifest
        string appxManifestPath = Path.Combine(projectDir, "Package.appxmanifest");
        if (File.Exists(appxManifestPath))
        {
            string appxText = File.ReadAllText(appxManifestPath);
            Assert.Contains($"Version=\"{AppConstants.DefaultAssemblyVersionString}\"", appxText);
        }

        // 6. Check MainWindow.xaml
        string mainWindowPath = Path.Combine(projectDir, "MainWindow.xaml");
        if (File.Exists(mainWindowPath))
        {
            string mainWindowText = File.ReadAllText(mainWindowPath);
            Assert.Contains($"x:Name=\"AppTitleVersionBadge\" Text=\"{AppConstants.DisplayVersion}\"", mainWindowText);
        }

        // 7. Check SettingsPage.xaml
        string settingsPagePath = Path.Combine(projectDir, "Modules", "Settings", "SettingsPage.xaml");
        if (File.Exists(settingsPagePath))
        {
            string settingsPageText = File.ReadAllText(settingsPagePath);
            Assert.Contains($"Text=\"{AppConstants.DisplayVersion}\"", settingsPageText);
            Assert.Contains($"Text=\"{AppConstants.DisplayVersion} {AppConstants.Codename}\"", settingsPageText);
        }
    }
}

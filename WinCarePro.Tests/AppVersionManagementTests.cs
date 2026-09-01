using System;
using System.IO;
using System.Text.RegularExpressions;
using WinCarePro.Core;
using Xunit;

namespace WinCarePro.Tests;

public class AppVersionManagementTests
{
    [Fact]
    public void AppConstants_VersionProperties_AreStandardizedTo460()
    {
        Assert.Equal(4, AppConstants.CurrentVersion.Major);
        Assert.Equal(6, AppConstants.CurrentVersion.Minor);
        Assert.Equal(0, AppConstants.CurrentVersion.Build);
        Assert.Equal("4.6.0", AppConstants.VersionString);
        Assert.Equal("v4.6", AppConstants.DisplayVersion);
        Assert.Equal("v4.6.0", AppConstants.DisplayVersionFull);
        Assert.Equal("WinCare Pro", AppConstants.AppName);
        Assert.Equal("Nova", AppConstants.Codename);
        Assert.Contains("WinCare Pro v4.6", AppConstants.TitleWithVersion);
        Assert.Contains("Version 4.6.0 (Codename: Nova)", AppConstants.SystemBadgeText);
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
    }
}

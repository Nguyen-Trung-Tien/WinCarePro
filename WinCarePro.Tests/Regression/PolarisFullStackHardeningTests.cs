using System;
using System.IO;
using WinCarePro.Core;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Security;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests.Regression;

public class PolarisFullStackHardeningTests
{
    [Fact]
    public void PolarisV600_MetadataAndVersion_StrictCompliance()
    {
        Assert.Equal("6.0.0", AppConstants.VersionString);
        Assert.Equal("Polaris", AppConstants.Codename);
        Assert.Equal("v6.0.0", AppConstants.DisplayVersion);
        Assert.Contains("Polaris", AppConstants.SystemBadgeText);
        Assert.Equal("6.0.0", AppConstants.DefaultVersionString);
    }

    [Fact]
    public void SafePathGuard_ProtectionArmor_RejectsProtectedRootsAndSystemFiles()
    {
        // Core OS and Root Protection Invariants
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Windows"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Windows\System32"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Windows\System32\drivers"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Program Files"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Program Files (x86)"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Users"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Users\Public"));
    }

    [Fact]
    public void InputSanitizer_Security_DetectsDangerousShellCharacters()
    {
        Assert.True(InputSanitizer.ContainsDangerousShellCharacters("cmd.exe & calc.exe"));
        Assert.True(InputSanitizer.ContainsDangerousShellCharacters("app.exe | whoami"));
        Assert.True(InputSanitizer.ContainsDangerousShellCharacters("app.exe ; del C:\\"));
        Assert.True(InputSanitizer.ContainsDangerousShellCharacters("app.exe > out.txt"));
        Assert.True(InputSanitizer.ContainsDangerousShellCharacters("app.exe `whoami`"));
        Assert.False(InputSanitizer.ContainsDangerousShellCharacters("normal_filename.exe"));
        Assert.False(InputSanitizer.ContainsDangerousShellCharacters("/silent /norestart"));
    }

    [Fact]
    public void InputSanitizer_UriSafety_AllowsOnlyApprovedSchemes()
    {
        Assert.True(InputSanitizer.IsSafeUri("https://github.com/Nguyen-Trung-Tien/WinCarePro"));
        Assert.True(InputSanitizer.IsSafeUri("http://localhost:8080"));
        Assert.True(InputSanitizer.IsSafeUri("windowsdefender:"));
        Assert.True(InputSanitizer.IsSafeUri("ms-settings:privacy"));
        Assert.False(InputSanitizer.IsSafeUri("file:///C:/Windows/System32/cmd.exe"));
        Assert.False(InputSanitizer.IsSafeUri("javascript:alert(1)"));
        Assert.False(InputSanitizer.IsSafeUri("powershell.exe -enc ..."));
    }

    [Fact]
    public void IconCacheService_LruAndSafety_BordersWithinLimits()
    {
        var iconService = new IconCacheService();

        // Safe fallback tests
        Assert.Equal("", iconService.GetIconForExecutable(""));
        Assert.Equal("", iconService.GetIconForExecutable("System Process"));
        Assert.Equal("", iconService.GetIconForExecutable(null!));
        Assert.Equal("", iconService.GetIconForExecutable(@"C:\NonExistent_FakeApp_12345.exe"));
    }
}


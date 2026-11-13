using System;
using System.Collections.Generic;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class SecurityAndSanitizationTests
{
    [Theory]
    [InlineData("cmd.exe & calc.exe", true)]
    [InlineData("test | dir", true)]
    [InlineData("arg; whoami", true)]
    [InlineData("normal_argument_value", false)]
    [InlineData("C:\\Windows\\System32\\notepad.exe", false)]
    public void InputSanitizer_ContainsDangerousShellCharacters_ValidatesCorrectly(string input, bool expectedDangerous)
    {
        bool isDangerous = Infrastructure.Security.InputSanitizer.ContainsDangerousShellCharacters(input);
        Assert.Equal(expectedDangerous, isDangerous);
    }

    [Theory]
    [InlineData("C:\\Users\\Admin\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\Login Data", false)]
    [InlineData("C:\\Users\\Admin\\.ssh\\id_rsa", false)]
    [InlineData("C:\\Windows\\System32\\config\\SAM", false)]
    [InlineData("C:\\Users\\Admin\\AppData\\Local\\Temp\\test_junk_file.tmp", true)]
    public void SafePathGuard_IsPathSafeForDeletion_ProtectsSensitiveFiles(string path, bool expectedSafe)
    {
        bool actualSafe = Core.Helpers.SafePathGuard.IsPathSafeForDeletion(path);
        Assert.Equal(expectedSafe, actualSafe);
    }

    [Fact]
    public void WmiHelper_CacheInvalidate_OperatesCleanly()
    {
        Core.Helpers.WmiHelper.InvalidateCache();
        // Should not throw
        Assert.True(true);
    }
}


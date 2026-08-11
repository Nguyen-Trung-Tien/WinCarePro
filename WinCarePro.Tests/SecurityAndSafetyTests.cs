using System;
using System.IO;
using WinCarePro.Core.Helpers;
using WinCarePro.Engines;
using Xunit;

namespace WinCarePro.Tests;

public class SecurityAndSafetyTests
{
    [Theory]
    [InlineData(@"C:\", false)]
    [InlineData(@"C:\Windows", false)]
    [InlineData(@"C:\Windows\System32", false)]
    [InlineData(@"C:\Program Files", false)]
    [InlineData(@"C:\Program Files (x86)", false)]
    [InlineData(@"", false)]
    [InlineData(null, false)]
    public void IsPathSafeToClean_RejectsForbiddenSystemPaths(string? path, bool expected)
    {
        bool isSafe = JunkCleanerEngine.IsPathSafeToClean(path);
        Assert.Equal(expected, isSafe);
    }

    [Fact]
    public void IsPathSafeToClean_AcceptsValidTempPath()
    {
        string validTempPath = Path.Combine(Path.GetTempPath(), "WinCareTestFolder");
        bool isSafe = JunkCleanerEngine.IsPathSafeToClean(validTempPath);
        Assert.True(isSafe);
    }

    [Theory]
    [InlineData("test; calc.exe", "\"test calc.exe\"")]
    [InlineData("file & whoami", "\"file  whoami\"")]
    [InlineData("echo | dir", "\"echo  dir\"")]
    public void SanitizeArgument_StripsDangerousInjectionCharacters(string input, string expected)
    {
        string result = ProcessRunner.SanitizeArgument(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RestoreRegistryBackup_RejectsFileWithInvalidHeader()
    {
        // Arrange
        string tempRegFile = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.reg");
        File.WriteAllText(tempRegFile, "This is not a valid registry file header.\n[HKCU\\Test]");

        try
        {
            var engine = new RegistryBackupEngine();

            // Act
            bool result = engine.RestoreRegistryBackup(tempRegFile);

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempRegFile))
            {
                File.Delete(tempRegFile);
            }
        }
    }
}

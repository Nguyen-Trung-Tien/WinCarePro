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

    [Theory]
    [InlineData(@"C:\Users\Admin\.ssh\id_rsa")]
    [InlineData(@"C:\Users\Admin\.ssh\id_ed25519")]
    [InlineData(@"C:\Users\Admin\Projects\App\.env")]
    [InlineData(@"C:\Windows\System32\config\SAM")]
    [InlineData(@"C:\Windows\System32\config\SYSTEM")]
    [InlineData(@"C:\Windows\System32\config\SECURITY")]
    [InlineData(@"C:\Users\Admin\AppData\Local\Microsoft\Edge\User Data\Default\Login Data")]
    [InlineData(@"C:\Users\Admin\Documents\token.json")]
    [InlineData(@"C:\Users\Admin\Secrets\client_secret.json")]
    [InlineData(@"C:\Users\Admin\Certificates\private_key.pem")]
    [InlineData(@"C:\Users\Admin\keepass.kdbx")]
    public void IsPathSafeToClean_RejectsSensitiveCredentialAndKeyFiles(string sensitivePath)
    {
        // SafePathGuard and JunkCleanerEngine must reject sensitive files regardless of folder context
        bool safeGuardResult = SafePathGuard.IsPathSafeForDeletion(sensitivePath);
        bool junkCleanerResult = JunkCleanerEngine.IsPathSafeToClean(sensitivePath);

        Assert.False(safeGuardResult, $"SafePathGuard should reject sensitive path: {sensitivePath}");
        Assert.False(junkCleanerResult, $"JunkCleanerEngine should reject sensitive path: {sensitivePath}");
    }

    [Fact]
    public void BoundaryAwarePathComparison_DistinguishesSimilarDirectoryNames()
    {
        // Directories with names that start with a blocked prefix as a substring,
        // but are NOT genuine subdirectories of that prefix, must NOT be blocked.
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        string system32BackupFile = Path.Combine(winDir, @"System32_backup\cleanme.tmp");
        string system32ExtraFile = Path.Combine(winDir, @"System32Extra\test.tmp");
        string bootcampFile = Path.Combine(winDir, @"Bootcamp\data.tmp");
        string defenderExtraFile = Path.Combine(progFiles, @"Windows Defender Extra\sample.tmp");

        // Genuine subdirectories of forbidden prefixes MUST be blocked
        string realSystem32File = Path.Combine(winDir, @"System32\drivers\etc\hosts");
        string realSysWow64File = Path.Combine(winDir, @"SysWOW64\kernel32.dll");
        string realDefenderFile = Path.Combine(progFiles, @"Windows Defender\MpCmdRun.exe");

        // Allowed exception paths (maintenance zones)
        string validWinTempFile = Path.Combine(winDir, @"Temp\legit_junk.tmp");
        string validWinLogFile = Path.Combine(winDir, @"Logs\CBS\cbs.log");
        string validWinUpdateFile = Path.Combine(winDir, @"SoftwareDistribution\Download\update.cab");

        // Fake allowed prefix (must NOT be treated as allowed)
        string fakeTempFile = Path.Combine(winDir, @"TempFake\bad.dll");

        Assert.True(SafePathGuard.IsSameOrChildPath(Path.Combine(winDir, "System32"), realSystem32File));
        Assert.False(SafePathGuard.IsSameOrChildPath(Path.Combine(winDir, "System32"), system32BackupFile));
        Assert.False(SafePathGuard.IsSameOrChildPath(Path.Combine(winDir, "System32"), system32ExtraFile));

        Assert.False(SafePathGuard.IsPathSafeForDeletion(realSystem32File));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(realSysWow64File));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(realDefenderFile));

        // Allowed maintenance zones should pass boundary check
        Assert.True(SafePathGuard.IsAllowedExceptionPath(validWinTempFile));
        Assert.True(SafePathGuard.IsAllowedExceptionPath(validWinLogFile));
        Assert.True(SafePathGuard.IsAllowedExceptionPath(validWinUpdateFile));
        Assert.False(SafePathGuard.IsAllowedExceptionPath(fakeTempFile));
    }

    [Fact]
    public void ReparsePoint_RejectsJunctionAndSymlink()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"WinCare_ReparseTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        string targetDir = Path.Combine(tempRoot, "TargetDir");
        string junctionDir = Path.Combine(tempRoot, "JunctionDir");
        string targetFile = Path.Combine(tempRoot, "TargetFile.txt");
        string symlinkFile = Path.Combine(tempRoot, "SymlinkFile.txt");

        Directory.CreateDirectory(targetDir);
        File.WriteAllText(targetFile, "Confidential target file content");

        bool junctionCreated = false;
        bool fileSymlinkCreated = false;

        try
        {
            // 1. Try creating directory junction via cmd /c mklink /J (standard user supported on NTFS)
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /J \"{junctionDir}\" \"{targetDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit(5000);
                junctionCreated = Directory.Exists(junctionDir);
            }
            catch
            {
                // Fallback to Directory.CreateSymbolicLink if supported
                try
                {
                    Directory.CreateSymbolicLink(junctionDir, targetDir);
                    junctionCreated = Directory.Exists(junctionDir);
                }
                catch { }
            }

            // 2. Try creating file symbolic link (requires SeCreateSymbolicLinkPrivilege or Windows Developer Mode)
            try
            {
                File.CreateSymbolicLink(symlinkFile, targetFile);
                fileSymlinkCreated = File.Exists(symlinkFile);
            }
            catch
            {
                // Unprivileged environment without Developer Mode will throw IOException / UnauthorizedAccessException;
                // this is expected on standard non-developer Windows configurations.
            }

            // Assertions
            if (junctionCreated)
            {
                var di = new DirectoryInfo(junctionDir);
                Assert.True((di.Attributes & FileAttributes.ReparsePoint) != 0, "Created directory must be a reparse point");

                bool safeDir = SafePathGuard.IsPathSafeForDeletion(junctionDir);
                bool safeJunk = JunkCleanerEngine.IsPathSafeToClean(junctionDir);

                Assert.False(safeDir, "SafePathGuard must reject directory junction/reparse points.");
                Assert.False(safeJunk, "JunkCleanerEngine must reject directory junction/reparse points.");
            }

            if (fileSymlinkCreated)
            {
                var fi = new FileInfo(symlinkFile);
                Assert.True((fi.Attributes & FileAttributes.ReparsePoint) != 0, "Created file must be a reparse point");

                bool safeFile = SafePathGuard.IsPathSafeForDeletion(symlinkFile);
                bool safeJunk = JunkCleanerEngine.IsPathSafeToClean(symlinkFile);

                Assert.False(safeFile, "SafePathGuard must reject file symbolic link/reparse points.");
                Assert.False(safeJunk, "JunkCleanerEngine must reject file symbolic link/reparse points.");
            }
        }
        finally
        {
            // Clean up links first then target directory
            if (junctionCreated && Directory.Exists(junctionDir))
            {
                try { Directory.Delete(junctionDir, false); } catch { }
            }
            if (fileSymlinkCreated && File.Exists(symlinkFile))
            {
                try { File.Delete(symlinkFile); } catch { }
            }
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }
}

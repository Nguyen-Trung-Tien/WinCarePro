using System;
using System.IO;
using WinCarePro.Infrastructure.Security;
using Xunit;

namespace WinCarePro.Tests;

public class UpdateSecurityValidatorTests
{
    // ============================================================
    // 1. Download URL Security Validation Tests
    // ============================================================

    [Theory]
    [InlineData("https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v4.9.1/WinCareProSetup.exe", true)]
    [InlineData("https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v4.9.0/WinCareProSetup.exe", true)]
    [InlineData("https://objects.githubusercontent.com/github-production-release-asset-2e65be/12345/setup.exe", true)]
    [InlineData("https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json", true)]
    [InlineData("http://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v4.9.1/WinCareProSetup.exe", false)] // Insecure HTTP
    [InlineData("ftp://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v4.9.1/setup.exe", false)] // Insecure FTP
    [InlineData("https://evil-attacker.com/WinCareProSetup.exe", false)] // Untrusted domain
    [InlineData("https://github.com/malicious-repo/evil/releases/download/v1.0/malware.exe", false)] // Wrong GitHub repo
    [InlineData("", false)] // Empty
    [InlineData(null, false)] // Null
    [InlineData("not-a-valid-url", false)] // Malformed
    public void IsTrustedDownloadUrl_EnforcesHttpsAndTrustedReleaseDomains(string? url, bool expectedResult)
    {
        bool isTrusted = UpdateSecurityValidator.IsTrustedDownloadUrl(url, out string? failureReason);
        Assert.Equal(expectedResult, isTrusted);

        if (!expectedResult)
        {
            Assert.False(string.IsNullOrWhiteSpace(failureReason));
        }
    }

    // ============================================================
    // 2. Missing SHA-256 Digest Tests
    // ============================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePackage_RejectsMissingSha256_AndDeletesFile(string? missingHash)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"WinCare_Test_MissingHash_{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A, 0x90, 0x00 }); // Dummy MZ

        try
        {
            Assert.True(File.Exists(tempFile));

            var result = UpdateSecurityValidator.ValidatePackageForInstallation(
                tempFile,
                missingHash,
                "Nguyen Trung Tien",
                customVerifier: (string file, string? publisher, out string? reason) => { reason = null; return true; });

            Assert.False(result.IsSuccess);
            Assert.Contains("SHA-256", result.Message, StringComparison.OrdinalIgnoreCase);
            // File must be deleted on security failure
            Assert.False(File.Exists(tempFile), "Untrusted package must be deleted immediately.");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    // ============================================================
    // 3. SHA-256 Mismatch Tests
    // ============================================================

    [Fact]
    public void ValidatePackage_RejectsHashMismatch_AndDeletesFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"WinCare_Test_HashMismatch_{Guid.NewGuid():N}.exe");
        File.WriteAllText(tempFile, "This is tampered installer content.");

        try
        {
            string wrongHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // Empty string hash

            var result = UpdateSecurityValidator.ValidatePackageForInstallation(
                tempFile,
                wrongHash,
                "Nguyen Trung Tien",
                customVerifier: (string file, string? publisher, out string? reason) => { reason = null; return true; });

            Assert.False(result.IsSuccess);
            Assert.Contains("SHA-256 checksum mismatch", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(tempFile), "Compromised file must be securely deleted.");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    // ============================================================
    // 4. Unsigned File Tests
    // ============================================================

    [Fact]
    public void ValidatePackage_RejectsUnsignedFile_AndDeletesFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"WinCare_Test_Unsigned_{Guid.NewGuid():N}.exe");
        byte[] content = new byte[] { 0x4D, 0x5A, 0x01, 0x02, 0x03 }; // Dummy unsigned PE
        File.WriteAllBytes(tempFile, content);

        try
        {
            string correctHash = CryptoHelper.ComputeFileHash(tempFile);

            // Using real WinVerifyTrust on unsigned file
            var result = UpdateSecurityValidator.ValidatePackageForInstallation(
                tempFile,
                correctHash,
                "Nguyen Trung Tien");

            Assert.False(result.IsSuccess);
            Assert.Contains("Authenticode", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(tempFile), "Unsigned installer must be securely deleted.");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    // ============================================================
    // 5. Publisher Mismatch Tests
    // ============================================================

    [Fact]
    public void ValidatePackage_RejectsPublisherMismatch_AndDeletesFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"WinCare_Test_PublisherMismatch_{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 });

        try
        {
            string correctHash = CryptoHelper.ComputeFileHash(tempFile);

            // Verifier simulates valid signature but signed by attacker/foreign publisher
            UpdateSecurityValidator.AuthenticodeVerifierFunc wrongPublisherVerifier = 
                (string file, string? publisher, out string? reason) =>
                {
                    reason = $"Publisher certificate mismatch. Expected: '{publisher}', Found: 'Untrusted Attacker LLC'.";
                    return false;
                };

            var result = UpdateSecurityValidator.ValidatePackageForInstallation(
                tempFile,
                correctHash,
                "Nguyen Trung Tien",
                customVerifier: wrongPublisherVerifier);

            Assert.False(result.IsSuccess);
            Assert.Contains("Publisher certificate mismatch", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(tempFile), "Binary with publisher mismatch must be deleted.");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    // ============================================================
    // 6. Valid Update Package Tests
    // ============================================================

    [Fact]
    public void ValidatePackage_AcceptsValidPackage_AndPreservesFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"WinCare_Test_Valid_{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 });

        try
        {
            string correctHash = CryptoHelper.ComputeFileHash(tempFile);

            // Verifier verifies signature and matching expected publisher
            UpdateSecurityValidator.AuthenticodeVerifierFunc validVerifier = 
                (string file, string? publisher, out string? reason) =>
                {
                    reason = null;
                    return true;
                };

            var result = UpdateSecurityValidator.ValidatePackageForInstallation(
                tempFile,
                correctHash,
                "Nguyen Trung Tien",
                customVerifier: validVerifier);

            Assert.True(result.IsSuccess);
            Assert.Contains("verified successfully", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(tempFile), "Valid verified package must be preserved for execution.");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    // ============================================================
    // 7. Real Windows Binary Authenticode Integration Test
    // ============================================================

    [Fact]
    public void VerifyAuthenticodeSignature_AgainstRealSignedSystemBinary()
    {
        string? dotnetPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(dotnetPath) || !File.Exists(dotnetPath))
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            dotnetPath = Path.Combine(programFiles, "dotnet", "dotnet.exe");
        }

        if (File.Exists(dotnetPath))
        {
            // dotnet.exe has a real embedded Authenticode signature signed by Microsoft Corporation
            bool signedByMs = UpdateSecurityValidator.VerifyAuthenticodeSignature(dotnetPath, "Microsoft", out string? msReason);
            Assert.True(signedByMs, $"{dotnetPath} should verify successfully under Microsoft. Failure: {msReason}");

            // dotnet.exe is NOT signed by Nguyen Trung Tien
            bool signedByWrong = UpdateSecurityValidator.VerifyAuthenticodeSignature(dotnetPath, "Nguyen Trung Tien", out string? wrongReason);
            Assert.False(signedByWrong);
            Assert.Contains("Publisher certificate mismatch", wrongReason ?? "");
        }
    }
}

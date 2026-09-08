using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WinCarePro.Core.Models;
using WinCarePro.Database;

namespace WinCarePro.Infrastructure.Security;

/// <summary>
/// Hardened security validator for application updates.
/// Enforces trusted HTTPS sources, strict SHA-256 integrity, WinVerifyTrust Authenticode
/// signature validation, and publisher identity checks prior to executing update packages.
/// </summary>
public static class UpdateSecurityValidator
{
    public const string DefaultExpectedPublisher = "Nguyen Trung Tien";

    public delegate bool AuthenticodeVerifierFunc(string filePath, string? expectedPublisher, out string? failureReason);

    private static readonly HashSet<string> AllowedReleaseHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com",
        "objects.githubusercontent.com",
        "raw.githubusercontent.com"
    };

    private const string ExpectedRepoPathPrefix = "/nguyen-trung-tien/wincarepro/";

    /// <summary>
    /// Validates whether a download URL originates from an explicitly trusted HTTPS distribution domain.
    /// </summary>
    public static bool IsTrustedDownloadUrl(string? url, out string? failureReason)
    {
        failureReason = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            failureReason = "Download URL is empty or null.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            failureReason = "Download URL has an invalid URI format.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = $"Insecure scheme '{uri.Scheme}'. Only HTTPS is permitted for application updates.";
            return false;
        }

        if (!AllowedReleaseHosts.Contains(uri.Host))
        {
            failureReason = $"Untrusted host domain '{uri.Host}'. Only official release distribution endpoints are allowed.";
            return false;
        }

        // For github.com, ensure the URL points to the authorized repository releases
        if (string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            string path = uri.AbsolutePath.ToLowerInvariant();
            if (!path.StartsWith(ExpectedRepoPathPrefix, StringComparison.Ordinal))
            {
                failureReason = $"Untrusted repository path '{uri.AbsolutePath}'. Updates must originate from '{ExpectedRepoPathPrefix}'.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates an update package binary before installation.
    /// Enforces:
    /// 1. Package existence and non-zero size.
    /// 2. Mandatory SHA-256 presence and matching.
    /// 3. Authenticode digital signature verification via WinVerifyTrust.
    /// 4. Expected publisher certificate validation.
    /// If any check fails, the file is securely deleted, an audit entry is logged,
    /// and a failed OperationResult is returned (no installer execution allowed).
    /// </summary>
    public static OperationResult ValidatePackageForInstallation(
        string? filePath,
        string? expectedSha256,
        string? expectedPublisher = DefaultExpectedPublisher,
        AuthenticodeVerifierFunc? customVerifier = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            DbManager.LogAction("Update rejected: Installer package does not exist.", "Software Updater", "Failed");
            return OperationResult.Fail("Update installer file not found.");
        }

        // 1. Mandatory SHA-256 Digest Check
        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            SecureDeleteFile(filePath);
            DbManager.LogAction($"Update rejected: Missing mandatory SHA-256 checksum for '{Path.GetFileName(filePath)}'.", "Software Updater", "Failed");
            return OperationResult.Fail("Security validation failed: Update manifest did not supply a required SHA-256 hash digest. Installation rejected.");
        }

        string actualHash = CryptoHelper.ComputeFileHash(filePath);
        if (!string.Equals(actualHash, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            SecureDeleteFile(filePath);
            DbManager.LogAction(
                $"Update rejected: SHA-256 mismatch for '{Path.GetFileName(filePath)}'. Expected: {expectedSha256.Trim()}, Actual: {actualHash}. File securely deleted.",
                "Software Updater", "Failed");
            return OperationResult.Fail($"Security validation failed: SHA-256 checksum mismatch (Expected: {expectedSha256.Trim()}, Actual: {actualHash}). File deleted.");
        }

        // 2. Authenticode WinVerifyTrust & Publisher Validation (NO PE-header-only fallback)
        var verifier = customVerifier ?? VerifyAuthenticodeSignature;
        bool isSignatureValid = verifier(filePath, expectedPublisher, out string? sigFailureReason);

        if (!isSignatureValid)
        {
            SecureDeleteFile(filePath);
            DbManager.LogAction(
                $"Update rejected: Authenticode verification failed for '{Path.GetFileName(filePath)}'. Reason: {sigFailureReason}. File securely deleted.",
                "Software Updater", "Failed");
            return OperationResult.Fail($"Security validation failed: {sigFailureReason}");
        }

        DbManager.LogAction($"Update package '{Path.GetFileName(filePath)}' passed all security integrity checks.", "Software Updater", "Success");
        return OperationResult.Ok("Update package verified successfully.");
    }

    /// <summary>
    /// Performs Authenticode signature verification using WinVerifyTrust Win32 API
    /// and validates the certificate publisher.
    /// </summary>
    public static bool VerifyAuthenticodeSignature(string filePath, string? expectedPublisher, out string? failureReason)
    {
        failureReason = null;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            failureReason = "File does not exist for Authenticode verification.";
            return false;
        }

        var fileInfo = new WINTRUST_FILE_INFO
        {
            cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        IntPtr pFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
        IntPtr pWVTData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA)));

        try
        {
            Marshal.StructureToPtr(fileInfo, pFileInfo, false);

            var trustData = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA)),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = pFileInfo,
                dwStateAction = WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = WTD_REVOCATION_CHECK_NONE | WTD_SAFER_FLAG,
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            Marshal.StructureToPtr(trustData, pWVTData, false);

            int result = WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION_GENERIC_VERIFY_V2, pWVTData);
            if (result != 0)
            {
                failureReason = $"Authenticode signature is invalid or untrusted (WinVerifyTrust error code: 0x{result:X8}).";
                return false;
            }

            // Verify certificate publisher if specified
            if (!string.IsNullOrWhiteSpace(expectedPublisher))
            {
                try
                {
#pragma warning disable SYSLIB0057
                    using var rawCert = X509Certificate.CreateFromSignedFile(filePath);
                    if (rawCert == null)
                    {
                        failureReason = "No Authenticode signing certificate found on binary.";
                        return false;
                    }

                    using var cert = new X509Certificate2(rawCert);
                    string subject = cert.Subject ?? "";
                    string simpleName = cert.GetNameInfo(X509NameType.SimpleName, false) ?? "";

                    if (!subject.Contains(expectedPublisher, StringComparison.OrdinalIgnoreCase) &&
                        !simpleName.Contains(expectedPublisher, StringComparison.OrdinalIgnoreCase))
                    {
                        failureReason = $"Publisher certificate mismatch. Expected: '{expectedPublisher}', Found: '{subject}'.";
                        return false;
                    }
#pragma warning restore SYSLIB0057
                }
                catch (Exception ex)
                {
                    failureReason = $"Failed to extract or read signing certificate: {ex.Message}";
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            failureReason = $"Exception during WinVerifyTrust execution: {ex.Message}";
            return false;
        }
        finally
        {
            if (pFileInfo != IntPtr.Zero) Marshal.FreeHGlobal(pFileInfo);
            if (pWVTData != IntPtr.Zero) Marshal.FreeHGlobal(pWVTData);
        }
    }

    /// <summary>
    /// Safely and securely deletes a rejected or suspicious update binary.
    /// </summary>
    public static void SecureDeleteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
                    if (fs.Length > 0 && fs.Length < 100 * 1024 * 1024)
                    {
                        byte[] zeros = new byte[Math.Min(4096, (int)fs.Length)];
                        fs.Write(zeros, 0, zeros.Length);
                    }
                }
                catch { }

                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateSecurity] Failed to delete file '{filePath}': {ex.Message}");
        }
    }

    #region Win32 WinVerifyTrust P/Invoke Definitions

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    private const uint WTD_UI_NONE = 2;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_STATEACTION_IGNORE = 0;
    private const uint WTD_REVOCATION_CHECK_NONE = 0x00000010;
    private const uint WTD_SAFER_FLAG = 0x00000100;

    private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(
        IntPtr hwnd,
        [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
        IntPtr pWVTData
    );

    #endregion
}

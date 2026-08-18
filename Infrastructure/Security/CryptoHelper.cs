using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WinCarePro.Infrastructure.Security;

/// <summary>
/// Provides secure encryption and decryption helpers using Windows Data Protection API (DPAPI).
/// Protects sensitive user data such as API tokens, credentials, and custom configurations.
/// </summary>
public static class CryptoHelper
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WinCarePro_Secure_Entropy_v4");

    /// <summary>
    /// Encrypts a plain-text string using DPAPI for the current logged-in Windows user.
    /// </summary>
    public static string ProtectString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            // Fallback: return raw string if DPAPI is unavailable (e.g. specialized sandbox)
            return plainText;
        }
    }

    /// <summary>
    /// Decrypts a DPAPI-encrypted base64 string for the current logged-in Windows user.
    /// </summary>
    public static string UnprotectString(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            return string.Empty;

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Fallback: if not valid base64 or not encrypted with DPAPI, return as-is
            return encryptedBase64;
        }
    }

    /// <summary>
    /// Computes a fast SHA-256 hash formatted as a lowercase hexadecimal string.
    /// </summary>
    public static string ComputeSha256(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

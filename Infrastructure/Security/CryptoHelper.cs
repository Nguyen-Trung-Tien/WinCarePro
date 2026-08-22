using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WinCarePro.Infrastructure.Security;

/// <summary>
/// Provides secure encryption and decryption helpers using Windows Data Protection API (DPAPI)
/// with AES-256-GCM backup encryption. Protects sensitive user data such as API tokens,
/// credentials, and custom configurations.
/// 
/// Security v4.2: Removed plaintext fallback, machine-derived entropy, hash verification.
/// </summary>
public static class CryptoHelper
{
    // Derive entropy from machine-specific data rather than hardcoded string
    private static readonly byte[] Entropy = DeriveEntropy();

    /// <summary>
    /// Derives entropy bytes from machine-specific identifiers for DPAPI additional entropy.
    /// This ensures encrypted data is bound to the specific machine installation.
    /// </summary>
    private static byte[] DeriveEntropy()
    {
        try
        {
            // Combine machine name + OS install date + WinCare version salt for uniqueness
            string machineSeed = $"WinCarePro_v4_{Environment.MachineName}_{Environment.OSVersion.VersionString}";
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(machineSeed));
        }
        catch
        {
            // Ultimate fallback: static entropy (still better than plaintext)
            return Encoding.UTF8.GetBytes("WinCarePro_Secure_Entropy_v4_Fallback");
        }
    }

    /// <summary>
    /// Encrypts a plain-text string using DPAPI for the current logged-in Windows user.
    /// Falls back to AES-256-GCM if DPAPI is unavailable (sandboxed environments).
    /// NEVER returns plaintext on failure — throws CryptographicException instead.
    /// </summary>
    public static string ProtectString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            try
            {
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }
        catch (CryptographicException)
        {
            // DPAPI unavailable: Use AES-256-GCM backup encryption with machine-derived key
            return AesGcmEncrypt(plainText);
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Failed to protect sensitive data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts a DPAPI-encrypted base64 string for the current logged-in Windows user.
    /// Attempts DPAPI (with machine entropy), then legacy DPAPI (without entropy), then AES-GCM fallback.
    /// NEVER returns raw ciphertext on decryption failure — throws CryptographicException or returns string.Empty.
    /// </summary>
    public static string UnprotectString(string encryptedBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedBase64))
            return string.Empty;

        // 1. Try AES-GCM fallback first if prefix matches
        if (encryptedBase64.StartsWith("AES:", StringComparison.Ordinal))
        {
            try
            {
                return AesGcmDecrypt(encryptedBase64);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Failed to decrypt AES-GCM payload: {ex.Message}", ex);
            }
        }

        byte[]? encryptedBytes = null;
        try
        {
            encryptedBytes = Convert.FromBase64String(encryptedBase64);
        }
        catch
        {
            // If not valid base64, return empty instead of leaking corrupted ciphertext
            return string.Empty;
        }

        // 2. Try DPAPI with machine-derived entropy (primary v4.2 scheme)
        try
        {
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            try
            {
                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }
        catch
        {
            // 3. Try Legacy DPAPI without entropy (backwards compatibility with older snapshots/settings)
            try
            {
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                try
                {
                    return Encoding.UTF8.GetString(plainBytes);
                }
                finally
                {
                    Array.Clear(plainBytes, 0, plainBytes.Length);
                }
            }
            catch
            {
                // Decryption genuinely failed. Never return raw ciphertext.
                return string.Empty;
            }
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

    /// <summary>
    /// Computes SHA-256 hash of a file for update integrity verification.
    /// Reads file in streaming fashion to handle large files efficiently.
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return string.Empty;

        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies that a file's SHA-256 hash matches the expected hash.
    /// Used for update installer integrity verification before execution.
    /// </summary>
    public static bool VerifyFileIntegrity(string filePath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash)) return false;

        string actualHash = ComputeFileHash(filePath);
        return !string.IsNullOrEmpty(actualHash) &&
               string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    // ========== AES-256-GCM Backup Encryption ==========
    // Used when DPAPI is unavailable (containerized/sandboxed environments).
    // Key derived from machine entropy using PBKDF2.

    private const int AesKeySize = 32;  // 256-bit
    private const int AesNonceSize = 12; // 96-bit nonce for GCM
    private const int AesTagSize = 16;   // 128-bit auth tag
    private const int Pbkdf2Iterations = 100_000;

    private static byte[] DeriveAesKey()
    {
        byte[] salt = Encoding.UTF8.GetBytes($"WinCarePro_AES_Salt_{Environment.MachineName}");
        return Rfc2898DeriveBytes.Pbkdf2(Entropy, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, AesKeySize);
    }

    private static string AesGcmEncrypt(string plainText)
    {
        byte[] key = DeriveAesKey();
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherText = new byte[plainBytes.Length];
        byte[] tag = new byte[AesTagSize];

        using var aes = new AesGcm(key, AesTagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        // Format: "AES:" + Base64(nonce + tag + ciphertext)
        byte[] combined = new byte[AesNonceSize + AesTagSize + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, AesNonceSize);
        Buffer.BlockCopy(tag, 0, combined, AesNonceSize, AesTagSize);
        Buffer.BlockCopy(cipherText, 0, combined, AesNonceSize + AesTagSize, cipherText.Length);

        return "AES:" + Convert.ToBase64String(combined);
    }

    private static string AesGcmDecrypt(string encryptedText)
    {
        if (!encryptedText.StartsWith("AES:"))
            throw new CryptographicException("Not an AES-GCM encrypted string.");

        byte[] combined = Convert.FromBase64String(encryptedText.Substring(4));
        if (combined.Length < AesNonceSize + AesTagSize)
            throw new CryptographicException("Invalid AES-GCM ciphertext length.");

        byte[] key = DeriveAesKey();
        byte[] nonce = new byte[AesNonceSize];
        byte[] tag = new byte[AesTagSize];
        int cipherLength = combined.Length - AesNonceSize - AesTagSize;
        byte[] cipherText = new byte[cipherLength];
        byte[] plainBytes = new byte[cipherLength];

        Buffer.BlockCopy(combined, 0, nonce, 0, AesNonceSize);
        Buffer.BlockCopy(combined, AesNonceSize, tag, 0, AesTagSize);
        Buffer.BlockCopy(combined, AesNonceSize + AesTagSize, cipherText, 0, cipherLength);

        using var aes = new AesGcm(key, AesTagSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}

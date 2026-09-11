using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Enterprise-grade security guard for filesystem operations.
/// Prevents Path Traversal vulnerabilities, symlink/reparse-point exploits,
/// and accidental deletion of Windows critical system files and directories.
/// </summary>
public static class SafePathGuard
{
    private static readonly HashSet<string> BlacklistedExactPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> BlacklistedPathPrefixes = new();
    private static readonly List<string> AllowedExceptionPrefixes = new();
    private static readonly HashSet<string> ProtectedSensitiveFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "pagefile.sys", "hiberfil.sys", "swapfile.sys", "NTUSER.DAT",
        "SAM", "SYSTEM", "SECURITY", "SOFTWARE", "DEFAULT",
        "BCD", "BOOTSECT.BAK", "BitLocker.tpm",
        "Login Data", "Login Data For Account", "Web Data", "Local State",
        "Cookies", "Cookies-journal", "key4.db", "logins.json", "cert9.db",
        "Vault.dat", "credentials", "id_rsa", "id_ed25519", "id_ecdsa", "id_dsa", "known_hosts",
        "token.json", "client_secret.json", "azureProfile.json", ".env", "private_key.pem", "keepass.kdbx"
    };

    static SafePathGuard()
    {
        InitializeBlacklist();
    }

    /// <summary>
    /// Canonicalizes a path and strips redundant trailing directory separators, preserving root drive format.
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string fullPath = Path.GetFullPath(path);
        string? root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrEmpty(root) && string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Checks whether candidatePath is equal to or a genuine subdirectory/subfile of basePath.
    /// Uses boundary-aware comparison so "C:\Windows\System32_backup" is NOT considered part of "C:\Windows\System32".
    /// </summary>
    public static bool IsSameOrChildPath(string basePath, string candidatePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(candidatePath))
            return false;

        string normBase = NormalizePath(basePath);
        string normCandidate = NormalizePath(candidatePath);

        if (string.Equals(normBase, normCandidate, StringComparison.OrdinalIgnoreCase))
            return true;

        string baseWithSep = normBase.EndsWith(Path.DirectorySeparatorChar)
            ? normBase
            : normBase + Path.DirectorySeparatorChar;

        return normCandidate.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether the specified path is inside an allowed maintenance zone (Windows Temp, Windows Logs, SoftwareDistribution\Download).
    /// </summary>
    public static bool IsAllowedExceptionPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string norm = NormalizePath(path);
        foreach (var allowed in AllowedExceptionPrefixes)
        {
            if (IsSameOrChildPath(allowed, norm))
                return true;
        }
        return false;
    }

    private static void InitializeBlacklist()
    {
        try
        {
            // System root & core drives
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            BlacklistedExactPaths.Add(NormalizePath(systemDrive));

            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(winDir))
            {
                BlacklistedExactPaths.Add(NormalizePath(winDir));
                BlacklistedExactPaths.Add(NormalizePath(Path.Combine(winDir, "System32")));
                BlacklistedExactPaths.Add(NormalizePath(Path.Combine(winDir, "SysWOW64")));
                BlacklistedExactPaths.Add(NormalizePath(Path.Combine(winDir, "WinSxS")));
                BlacklistedExactPaths.Add(NormalizePath(Path.Combine(winDir, "system.ini")));
                BlacklistedExactPaths.Add(NormalizePath(Path.Combine(winDir, "win.ini")));
                
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(winDir, "System32")));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(winDir, "SysWOW64")));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(winDir, "WinSxS")));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(winDir, "Boot")));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(winDir, @"system32\config")));

                AllowedExceptionPrefixes.Add(NormalizePath(Path.Combine(winDir, "Temp")));
                AllowedExceptionPrefixes.Add(NormalizePath(Path.Combine(winDir, "Logs")));
                AllowedExceptionPrefixes.Add(NormalizePath(Path.Combine(winDir, @"SoftwareDistribution\Download")));
            }

            // System Drive root critical boot components
            BlacklistedExactPaths.Add(NormalizePath(Path.Combine(systemDrive, "bootmgr")));
            BlacklistedExactPaths.Add(NormalizePath(Path.Combine(systemDrive, "BOOTNXT")));
            BlacklistedExactPaths.Add(NormalizePath(Path.Combine(systemDrive, "autoexec.bat")));
            BlacklistedExactPaths.Add(NormalizePath(Path.Combine(systemDrive, "config.sys")));
            BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(systemDrive, "Boot")));
            BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(systemDrive, "Recovery")));
            BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(systemDrive, "System Volume Information")));

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                BlacklistedExactPaths.Add(NormalizePath(programFiles));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(programFiles, "Windows Defender")));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(programFiles, "Windows Defender Advanced Threat Protection")));
            }

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                BlacklistedExactPaths.Add(NormalizePath(programFilesX86));
                BlacklistedPathPrefixes.Add(NormalizePath(Path.Combine(programFilesX86, "Windows Defender")));
            }

            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!string.IsNullOrEmpty(programData))
            {
                BlacklistedExactPaths.Add(NormalizePath(programData));
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                // Protect user profile root itself (e.g. C:\Users\Admin)
                BlacklistedExactPaths.Add(NormalizePath(userProfile));
                var usersDir = Directory.GetParent(userProfile)?.FullName;
                if (!string.IsNullOrEmpty(usersDir))
                {
                    BlacklistedExactPaths.Add(NormalizePath(usersDir));
                }
            }
        }
        catch
        {
            // Fallback safety defaults
            BlacklistedExactPaths.Add(NormalizePath("C:\\"));
            BlacklistedExactPaths.Add(NormalizePath("C:\\Windows"));
            BlacklistedExactPaths.Add(NormalizePath("C:\\Windows\\System32"));
        }
    }

    /// <summary>
    /// Validates if a file or directory path is safe to delete or modify.
    /// Returns false if the path contains traversal tricks, points to Windows core files,
    /// or targets a reparse point (junction/symlink) that could cause unintended deletions.
    /// </summary>
    public static bool IsPathSafeForDeletion(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return false;

        // Check for null bytes or illegal characters
        if (rawPath.IndexOf('\0') >= 0 || rawPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        // Check for basic traversal sequence
        if (rawPath.Contains(".."))
            return false;

        string trimmedRaw = rawPath.Trim();
        // Disallow bare drive letter references (e.g. "C:", "D:")
        if (trimmedRaw.Length == 2 && char.IsLetter(trimmedRaw[0]) && trimmedRaw[1] == ':')
            return false;

        try
        {
            // Normalize to full canonical path
            string fullPath = NormalizePath(rawPath);
            if (string.IsNullOrEmpty(fullPath))
                return false;

            // Never allow root drive deletion (e.g., "C:\", "D:\", "\\server\share")
            string? root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrEmpty(root))
            {
                string rootTrimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullTrimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(rootTrimmed, fullTrimmed, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Check exact blacklisted paths
            if (BlacklistedExactPaths.Contains(fullPath))
                return false;

            // Check critical protected system prefixes using boundary-aware check
            foreach (var prefix in BlacklistedPathPrefixes)
            {
                if (IsSameOrChildPath(prefix, fullPath))
                {
                    // Exceptions: Specific maintenance subfolders (Temp, Logs, SoftwareDistribution\Download)
                    if (IsAllowedExceptionPath(fullPath))
                    {
                        break; // Allowed to continue to sensitive name and reparse checks
                    }

                    return false;
                }
            }

            // Check if file is critical system or credential store file
            string fileName = Path.GetFileName(fullPath);
            if (!string.IsNullOrEmpty(fileName) && ProtectedSensitiveFileNames.Contains(fileName))
            {
                return false;
            }

            // Check for symlinks / junction reparse points to prevent following links to sensitive folders.
            // Both files and directories possessing FileAttributes.ReparsePoint MUST be rejected.
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }
            }
            else if (Directory.Exists(fullPath))
            {
                var dirInfo = new DirectoryInfo(fullPath);
                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }
            }
            else if (Path.Exists(fullPath))
            {
                // Handles broken symlinks / dangling reparse points where target doesn't exist
                var attr = File.GetAttributes(fullPath);
                if ((attr & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Standard alias for IsPathSafeForDeletion adhering to Rules 02, 03, 07 and system specifications.
    /// Returns true if the path is safe to delete.
    /// </summary>
    public static bool IsSafeToDelete(string path) => IsPathSafeForDeletion(path);

    /// <summary>
    /// Safely deletes a file after verifying security constraints.
    /// </summary>
    public static bool TrySafeDeleteFile(string filePath)
    {
        if (!IsPathSafeForDeletion(filePath))
            return false;

        try
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Safely cleans files and subdirectories in a directory with full boundary checks,
    /// level-by-level traversal, reparse point rejection, and accurate accounting.
    /// </summary>
    public static (long deletedBytes, int filesDeleted) SafeCleanDirectoryWithStats(string dirPath, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
            return (0, 0);

        long deletedBytes = 0;
        int filesDeleted = 0;

        try
        {
            var dirInfo = new DirectoryInfo(dirPath);
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                return (0, 0); // Never enter reparse points

            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (IsPathSafeForDeletion(file.FullName))
                    {
                        long size = file.Length;
                        if (file.IsReadOnly)
                        {
                            file.Attributes = FileAttributes.Normal;
                        }
                        file.Delete();
                        deletedBytes += size;
                        filesDeleted++;
                    }
                }
                catch { }
            }

            if (recursive)
            {
                foreach (var subDir in dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        if ((subDir.Attributes & FileAttributes.ReparsePoint) == 0 && IsPathSafeForDeletion(subDir.FullName))
                        {
                            var (subBytes, subFiles) = SafeCleanDirectoryWithStats(subDir.FullName, true);
                            deletedBytes += subBytes;
                            filesDeleted += subFiles;
                            try
                            {
                                subDir.Delete(false); // Only delete if empty
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return (deletedBytes, filesDeleted);
    }

    /// <summary>
    /// Safely cleans files in a directory without deleting the directory itself or violating safety rules.
    /// </summary>
    public static long SafeCleanDirectoryContents(string dirPath, bool recursive = true)
    {
        return SafeCleanDirectoryWithStats(dirPath, recursive).deletedBytes;
    }
}

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

    static SafePathGuard()
    {
        InitializeBlacklist();
    }

    private static void InitializeBlacklist()
    {
        try
        {
            // System root & core drives
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            BlacklistedExactPaths.Add(systemDrive);

            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(winDir))
            {
                BlacklistedExactPaths.Add(winDir);
                BlacklistedExactPaths.Add(Path.Combine(winDir, "System32"));
                BlacklistedExactPaths.Add(Path.Combine(winDir, "SysWOW64"));
                BlacklistedExactPaths.Add(Path.Combine(winDir, "WinSxS"));
                BlacklistedExactPaths.Add(Path.Combine(winDir, "system.ini"));
                BlacklistedExactPaths.Add(Path.Combine(winDir, "win.ini"));
                
                BlacklistedPathPrefixes.Add(Path.Combine(winDir, "System32"));
                BlacklistedPathPrefixes.Add(Path.Combine(winDir, "SysWOW64"));
                BlacklistedPathPrefixes.Add(Path.Combine(winDir, "WinSxS"));
                BlacklistedPathPrefixes.Add(Path.Combine(winDir, "Boot"));
                BlacklistedPathPrefixes.Add(Path.Combine(winDir, "system32\\config"));
            }

            // System Drive root critical boot components
            BlacklistedExactPaths.Add(Path.Combine(systemDrive, "bootmgr"));
            BlacklistedExactPaths.Add(Path.Combine(systemDrive, "BOOTNXT"));
            BlacklistedExactPaths.Add(Path.Combine(systemDrive, "autoexec.bat"));
            BlacklistedExactPaths.Add(Path.Combine(systemDrive, "config.sys"));
            BlacklistedPathPrefixes.Add(Path.Combine(systemDrive, "Boot"));
            BlacklistedPathPrefixes.Add(Path.Combine(systemDrive, "Recovery"));
            BlacklistedPathPrefixes.Add(Path.Combine(systemDrive, "System Volume Information"));

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                BlacklistedExactPaths.Add(programFiles);
                BlacklistedPathPrefixes.Add(Path.Combine(programFiles, "Windows Defender"));
                BlacklistedPathPrefixes.Add(Path.Combine(programFiles, "Windows Defender Advanced Threat Protection"));
            }

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                BlacklistedExactPaths.Add(programFilesX86);
                BlacklistedPathPrefixes.Add(Path.Combine(programFilesX86, "Windows Defender"));
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                // Protect user profile root itself (e.g. C:\Users\Admin)
                BlacklistedExactPaths.Add(userProfile);
                var usersDir = Directory.GetParent(userProfile)?.FullName;
                if (!string.IsNullOrEmpty(usersDir))
                {
                    BlacklistedExactPaths.Add(usersDir);
                }
            }
        }
        catch
        {
            // Fallback safety defaults
            BlacklistedExactPaths.Add("C:\\");
            BlacklistedExactPaths.Add("C:\\Windows");
            BlacklistedExactPaths.Add("C:\\Windows\\System32");
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

        try
        {
            // Normalize to full canonical path
            string fullPath = Path.GetFullPath(rawPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Never allow root drive deletion (e.g., "C:", "C:\", "D:")
            if (Path.GetPathRoot(fullPath)?.TrimEnd('\\', '/') == fullPath)
                return false;

            // Check exact blacklisted paths
            if (BlacklistedExactPaths.Contains(fullPath))
                return false;

            // Check critical protected system prefixes
            foreach (var prefix in BlacklistedPathPrefixes)
            {
                if (fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Exception: Temp subfolders inside Windows (e.g., C:\Windows\Temp, C:\Windows\Logs\CBS) are allowed
                    if (fullPath.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), StringComparison.OrdinalIgnoreCase) ||
                        fullPath.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"), StringComparison.OrdinalIgnoreCase) ||
                        fullPath.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution\\Download"), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                }
            }

            // Check if file is critical system file
            string fileName = Path.GetFileName(fullPath);
            if (fileName.Equals("pagefile.sys", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("hiberfil.sys", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("swapfile.sys", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("NTUSER.DAT", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("SAM", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("SECURITY", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("SOFTWARE", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("BCD", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("BOOTSECT.BAK", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check for symlinks / junction reparse points to prevent following links to sensitive folders
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    // Reparse points should be unlinked rather than traversed recursively
                    return true; 
                }
            }
            else if (Directory.Exists(fullPath))
            {
                var dirInfo = new DirectoryInfo(fullPath);
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    // Junction points must not be wiped recursively
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
    /// Safely cleans files in a directory without deleting the directory itself or violating safety rules.
    /// </summary>
    public static long SafeCleanDirectoryContents(string dirPath, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
            return 0;

        long deletedBytes = 0;

        try
        {
            var dirInfo = new DirectoryInfo(dirPath);
            if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                return 0; // Skip junction folders

            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (IsPathSafeForDeletion(file.FullName))
                    {
                        long size = file.Length;
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                        deletedBytes += size;
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
                        if (IsPathSafeForDeletion(subDir.FullName) && !subDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            deletedBytes += SafeCleanDirectoryContents(subDir.FullName, true);
                            try
                            {
                                subDir.Delete(true);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return deletedBytes;
    }
}

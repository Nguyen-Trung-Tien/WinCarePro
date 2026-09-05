using System;
using System.Collections.Generic;
using System.Linq;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Security guard for Windows Registry operations.
/// Prevents accidental deletion or corruption of critical Windows system keys,
/// root hives, and core operating system configurations.
/// </summary>
public static class SafeRegistryGuard
{
    private static readonly HashSet<string> ProtectedRootKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "",
        "\\",
        "/",
        "HKCU",
        "HKLM",
        "HKCR",
        "HKU",
        "HKCC",
        "HKEY_CURRENT_USER",
        "HKEY_LOCAL_MACHINE",
        "HKEY_CLASSES_ROOT",
        "HKEY_USERS",
        "HKEY_CURRENT_CONFIG",
        @"HKCU\Software",
        @"HKCU\Software\Microsoft",
        @"HKCU\Software\Microsoft\Windows",
        @"HKCU\Software\Microsoft\Windows\CurrentVersion",
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
        @"HKLM\SOFTWARE",
        @"HKLM\SOFTWARE\Classes",
        @"HKLM\SOFTWARE\Microsoft",
        @"HKLM\SOFTWARE\Microsoft\Windows",
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion",
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKLM\SOFTWARE\Microsoft\Windows NT",
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
        @"HKLM\SOFTWARE\WOW6432Node",
        @"HKLM\SOFTWARE\WOW6432Node\Microsoft",
        @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows",
        @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKLM\SYSTEM",
        @"HKLM\SYSTEM\CurrentControlSet",
        @"HKLM\SYSTEM\CurrentControlSet\Control",
        @"HKLM\SYSTEM\CurrentControlSet\Services",
        @"HKLM\SAM",
        @"HKLM\SECURITY",
        @"HKLM\BCD00000000",
        @"HKLM\HARDWARE",
        @"HKLM\COMPONENTS"
    };

    private static readonly string[] ProtectedPrefixes = new[]
    {
        @"HKLM\SAM",
        @"HKLM\SECURITY",
        @"HKLM\BCD00000000",
        @"HKLM\HARDWARE",
        @"HKLM\COMPONENTS",
        @"HKLM\SYSTEM\CurrentControlSet\Control\Lsa",
        @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager",
        @"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot",
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon",
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList",
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"
    };

    /// <summary>
    /// Validates if a registry key is safe to delete or wipe.
    /// Returns false if the path is a root hive, core system configuration, or lacks sufficient depth.
    /// </summary>
    public static bool IsSafeToDeleteKey(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return false;

        string normalized = NormalizePath(rawPath);

        // 1. Direct match on blacklisted root or critical keys
        if (ProtectedRootKeys.Contains(normalized))
        {
            return false;
        }

        // 2. Check protected critical prefixes
        foreach (var prefix in ProtectedPrefixes)
        {
            if (normalized.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(prefix + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // 3. Prevent deleting top-level or second-level hive keys (e.g. HKLM\SOFTWARE, HKCU\Control Panel)
        var parts = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            // At least Hive + Category + SpecificItem required to delete a key tree
            return false;
        }

        // 4. Specifically prevent deletion of top-level vendor keys (e.g. HKCU\Software\Microsoft)
        if (parts.Length == 3 && parts[1].Equals("Software", StringComparison.OrdinalIgnoreCase) &&
            (parts[2].Equals("Microsoft", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("Classes", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates if a registry value can be deleted safely without breaking critical OS boot parameters.
    /// </summary>
    private static readonly HashSet<string> CriticalValueNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell",
        "Userinit",
        "AppInit_DLLs",
        "BootExecute"
    };

    public static bool IsSafeToDeleteValue(string keyPath, string valueName)
    {
        if (string.IsNullOrWhiteSpace(keyPath)) return false;
        if (string.IsNullOrWhiteSpace(valueName)) return false;

        string normalized = NormalizePath(keyPath);

        if (CriticalValueNames.Contains(valueName.Trim()))
        {
            return false;
        }

        // Core system startup or boot keys: allow deleting specific app value entries, but not default/critical values
        if (string.IsNullOrEmpty(valueName) && ProtectedRootKeys.Contains(normalized))
        {
            return false;
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        string p = path.Trim().TrimEnd('\\', '/');
        p = p.Replace('/', '\\');

        if (p.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            p = "HKCU" + p.Substring("HKEY_CURRENT_USER".Length);
        else if (p.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
            p = "HKLM" + p.Substring("HKEY_LOCAL_MACHINE".Length);
        else if (p.StartsWith("HKEY_CLASSES_ROOT", StringComparison.OrdinalIgnoreCase))
            p = "HKCR" + p.Substring("HKEY_CLASSES_ROOT".Length);
        else if (p.StartsWith("HKEY_USERS", StringComparison.OrdinalIgnoreCase))
            p = "HKU" + p.Substring("HKEY_USERS".Length);
        else if (p.StartsWith("HKEY_CURRENT_CONFIG", StringComparison.OrdinalIgnoreCase))
            p = "HKCC" + p.Substring("HKEY_CURRENT_CONFIG".Length);

        return p;
    }
}

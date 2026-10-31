using System;
using WinCarePro.Core;

namespace WinCarePro.Examples.Versioning;

/// <summary>
/// Reference example demonstrating how ViewModels, Pages, and services consume
/// version information exclusively from AppConstants (Single Source of Truth).
/// Invariants:
/// - NEVER hardcode version literals (e.g., "5.1.1") in XAML or code-behind.
/// - ALWAYS bind or reference AppConstants.DisplayVersion, AppConstants.TitleWithVersion, or AppConstants.SystemBadgeText.
/// </summary>
public static class AppVersionSyncExample
{
    public static string GetWindowTitle()
    {
        // Result: "WinCare Pro v5.1.1"
        return AppConstants.TitleWithVersion;
    }

    public static string GetSystemBadge()
    {
        // Result: "Version 5.1.1 (Codename: Polaris) • 64-bit Native System Suite"
        return AppConstants.SystemBadgeText;
    }

    public static bool IsNewerVersion(string remoteVersionString)
    {
        if (Version.TryParse(remoteVersionString, out var remoteVersion))
        {
            return remoteVersion > AppConstants.CurrentVersion;
        }

        return false;
    }
}


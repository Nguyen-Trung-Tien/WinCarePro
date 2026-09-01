using System;
using System.Reflection;

namespace WinCarePro.Core;

/// <summary>
/// Centralized Single Source of Truth (SSOT) for application metadata, versioning, and brand constants.
/// Dynamically extracts assembly metadata at runtime with standardized fallback values.
/// </summary>
public static class AppConstants
{
    public const string AppName = "WinCare Pro";
    public const string Publisher = "Nguyen Trung Tien";
    public const string Codename = "Nova";
    public const string DefaultVersionString = "4.6.0";
    public const string DefaultAssemblyVersionString = "4.6.0.0";

    /// <summary>
    /// The runtime assembly version.
    /// </summary>
    public static readonly Version CurrentVersion = 
        typeof(AppConstants).Assembly.GetName().Version ?? new Version(4, 6, 0, 0);

    /// <summary>
    /// Standard semantic version string (e.g., "4.6.0").
    /// </summary>
    public static readonly string VersionString = 
        $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    /// <summary>
    /// Compact display version string (e.g., "v4.6").
    /// </summary>
    public static readonly string DisplayVersion = 
        $"v{CurrentVersion.Major}.{CurrentVersion.Minor}";

    /// <summary>
    /// Full display version string (e.g., "v4.6.0").
    /// </summary>
    public static readonly string DisplayVersionFull = 
        $"v{VersionString}";

    /// <summary>
    /// Formatted title with version (e.g., "WinCare Pro v4.6").
    /// </summary>
    public static readonly string TitleWithVersion = 
        $"{AppName} {DisplayVersion}";

    /// <summary>
    /// Standard system badge text used in About and Diagnostic views.
    /// </summary>
    public static readonly string SystemBadgeText = 
        $"Version {VersionString} (Codename: {Codename}) • 64-bit Native System Suite";
}

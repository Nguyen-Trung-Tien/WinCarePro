namespace WinCarePro.Core.Constants;

/// <summary>
/// Reference example demonstrating Single Source of Truth (SSOT) versioning.
/// Invariants:
/// - Version numbers and release metadata MUST be defined exclusively in AppConstants.
/// - Never hardcode version strings in Pages, ViewModels, or Installer scripts.
/// </summary>
public static class AppConstantsVersionExample
{
    public const string AppName = "WinCare Pro";
    public const string AppVersion = "5.1.1";
    public const string AppCodename = "Polaris";
    public const string FullDisplayName = $"{AppName} v{AppVersion} ({AppCodename})";
}


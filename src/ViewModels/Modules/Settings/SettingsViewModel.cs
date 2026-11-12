using System;
using System.Reflection;
using WinCarePro.Core;

namespace WinCarePro.ViewModels;

/// <summary>
/// ViewModel representing application settings, diagnostics, and environment metadata.
/// Provides dynamic version resolution adhering to zero-hardcode directives.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    /// <summary>
    /// Dynamically resolved application version using Assembly or Package identity.
    /// Strictly eliminates static hardcoded version strings.
    /// </summary>
    public string AppVersion
    {
        get
        {
            try
            {
                var packageVer = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"v{packageVer.Major}.{packageVer.Minor}.{packageVer.Build}";
            }
            catch
            {
                var asmVer = Assembly.GetExecutingAssembly().GetName().Version;
                return asmVer != null ? $"v{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}" : AppConstants.DisplayVersion;
            }
        }
    }

    /// <summary>
    /// Full badge text including application codename and architecture.
    /// </summary>
    public string SystemBadgeText => AppConstants.SystemBadgeText;

    /// <summary>
    /// Formatted version with codename.
    /// </summary>
    public string VersionWithCodename => $"{AppVersion} {AppConstants.Codename}";
}


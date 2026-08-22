using System;
using Windows.UI.ViewManagement;

namespace WinCarePro.Shared.Animations;

/// <summary>
/// Provides global accessibility checks for Reduced Motion / Windows animation settings.
/// When animations are disabled by the user or system accessibility options, transitions
/// execute instantaneously or with gentle fades to maintain smooth usability.
/// </summary>
public static class ReducedMotionHelper
{
    private static UISettings? _uiSettings;
    private static bool _animationsEnabled = true;

    static ReducedMotionHelper()
    {
        try
        {
            _uiSettings = new UISettings();
            _animationsEnabled = _uiSettings.AnimationsEnabled;

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                _uiSettings.AnimationsEnabledChanged += (s, e) =>
                {
                    _animationsEnabled = s.AnimationsEnabled;
                };
            }
        }
        catch
        {
            _animationsEnabled = true;
        }
    }

    /// <summary>
    /// Gets whether system animations are enabled.
    /// </summary>
    public static bool AreAnimationsEnabled => _animationsEnabled;
}

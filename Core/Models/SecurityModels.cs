using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCarePro.Services;

namespace WinCarePro.Models;

public enum SecurityCategory
{
    Safeguards,
    Privacy,
    TraceEradication,
    ThreatHunter
}

public partial class SecuritySafeguardItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Safeguards";
    public string IconGlyph { get; set; } = "\uE727";
    public string RegistryPath { get; set; } = string.Empty;
    public string RecommendedValue { get; set; } = "1";
    public string FixKey { get; set; } = string.Empty;

    [ObservableProperty]
    private string _currentValue = string.Empty;

    [ObservableProperty]
    private bool _isProtected;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _comparisonText = string.Empty;

    public string StatusFormatted => IsProtected ? "Protected".T() : "Vulnerable".T();

    private static SolidColorBrush? _goodBg;
    private static SolidColorBrush? _goodBorder;
    private static SolidColorBrush? _goodFg;
    private static SolidColorBrush? _warnBg;
    private static SolidColorBrush? _warnBorder;
    private static SolidColorBrush? _warnFg;

    public Brush? StatusBgColor
    {
        get
        {
            try
            {
                return IsProtected
                    ? (_goodBg ??= new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)))
                    : (_warnBg ??= new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)));
            }
            catch { return null; }
        }
    }

    public Brush? StatusBorderColor
    {
        get
        {
            try
            {
                return IsProtected
                    ? (_goodBorder ??= new SolidColorBrush(Color.FromArgb(60, 16, 185, 129)))
                    : (_warnBorder ??= new SolidColorBrush(Color.FromArgb(60, 239, 68, 68)));
            }
            catch { return null; }
        }
    }

    public Brush? StatusForegroundColor
    {
        get
        {
            try
            {
                return IsProtected
                    ? (_goodFg ??= new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)))
                    : (_warnFg ??= new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)));
            }
            catch { return null; }
        }
    }

    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(StatusFormatted));
        OnPropertyChanged(nameof(StatusBgColor));
        OnPropertyChanged(nameof(StatusBorderColor));
        OnPropertyChanged(nameof(StatusForegroundColor));
        OnPropertyChanged(nameof(ComparisonText));
    }
}

public partial class SecurityComponentCard : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Glyph { get; set; } = "\uE727";
    public string ActionKey { get; set; } = string.Empty;
    public string ActionButtonText { get; set; } = "Open \u2192";

    [ObservableProperty]
    private string _statusText = "Checking...".T();

    [ObservableProperty]
    private bool _isSecure = true;

    [ObservableProperty]
    private string _badgeText = "SECURE";

    [ObservableProperty]
    private bool _canAutoFix;

    private SolidColorBrush? _badgeBg;
    public SolidColorBrush? BadgeBg
    {
        get => _badgeBg;
        set => SetProperty(ref _badgeBg, value);
    }

    private SolidColorBrush? _badgeFg;
    public SolidColorBrush? BadgeFg
    {
        get => _badgeFg;
        set => SetProperty(ref _badgeFg, value);
    }

    private SolidColorBrush? _iconBg;
    public SolidColorBrush? IconBg
    {
        get => _iconBg;
        set => SetProperty(ref _iconBg, value);
    }

    private SolidColorBrush? _iconFg;
    public SolidColorBrush? IconFg
    {
        get => _iconFg;
        set => SetProperty(ref _iconFg, value);
    }
}

public partial class SecurityAlertItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "System";
    public string Severity { get; set; } = "Warning";
    public string FixActionKey { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isFixed;

    [ObservableProperty]
    private string _statusText = "Open";

    private SolidColorBrush? _severityBrush;
    public SolidColorBrush? SeverityBrush
    {
        get => _severityBrush;
        set => SetProperty(ref _severityBrush, value);
    }

    public bool CanFix => !string.IsNullOrEmpty(FixActionKey) && !IsFixed;
}

public partial class PrivacyTuningItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Glyph { get; set; } = "\uE72E";

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private bool _isLoading;
}

public partial class TraceCleanItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Glyph { get; set; } = "\uE74D";

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _statusText = "Ready".T();
}

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCarePro.Services;

namespace WinCarePro.Models;

public partial class SecurityStatusItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uE727";
    public string ActionText { get; set; } = string.Empty;
    public string ActionTarget { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSecure = true;

    [ObservableProperty]
    private string _statusText = string.Empty;
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

    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(StatusFormatted));
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

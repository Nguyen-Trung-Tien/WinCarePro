using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCarePro.Services;

namespace WinCarePro.Models;

public enum DashboardScanMode
{
    DiskAndCache,
    StartupAndPerformance,
    SecurityCheck,
    RegistryAndDiskHealth
}

public enum ScanFindingSeverity
{
    Healthy, // Green
    Warning, // Yellow / Orange
    Danger   // Red
}

public partial class DashboardSubCategoryItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string NameKey { get; set; } = string.Empty;
    public string DescriptionKey { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uE71D";

    public string DisplayName => NameKey.T();
    public string DisplayDescription => DescriptionKey.T();

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _badgeText = string.Empty;
}

public partial class DashboardScanFinding : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ScanFindingSeverity _severity = ScanFindingSeverity.Healthy;

    [ObservableProperty]
    private string _category = string.Empty;

    public string SeverityBadgeText => Severity switch
    {
        ScanFindingSeverity.Healthy => "Safe".T(),
        ScanFindingSeverity.Warning => "Notice".T(),
        ScanFindingSeverity.Danger => "Risk".T(),
        _ => "Info".T()
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush BadgeBg => Severity switch
    {
        ScanFindingSeverity.Healthy => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 16, 185, 129)),
        ScanFindingSeverity.Warning => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 245, 158, 11)),
        ScanFindingSeverity.Danger => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 239, 68, 68)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 148, 163, 184))
    };

    public Microsoft.UI.Xaml.Media.SolidColorBrush BadgeFg => Severity switch
    {
        ScanFindingSeverity.Healthy => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
        ScanFindingSeverity.Warning => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        ScanFindingSeverity.Danger => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184))
    };
}

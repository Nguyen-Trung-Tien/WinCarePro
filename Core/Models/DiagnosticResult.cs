using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public class DiagnosticResult
{
    public string CheckName { get; set; } = "";
    public string Category { get; set; } = ""; // Performance, Storage, Network, Security, Software
    public bool IsHealthy { get; set; } = true;
    public string Description { get; set; } = "";
    public string Recommendation { get; set; } = "";

    public string StatusLabel => IsHealthy ? "PASSED" : "ATTENTION";
    public string StatusIcon => IsHealthy ? "\uE73E" : "\uE783";
    public Visibility StatusBadgeVisibility => IsHealthy ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FixButtonVisibility => IsHealthy ? Visibility.Collapsed : Visibility.Visible;

    private static readonly SolidColorBrush GreenBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129));
    private static readonly SolidColorBrush AmberBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush GreenBgBrush = new(Windows.UI.Color.FromArgb(32, 16, 185, 129));
    private static readonly SolidColorBrush AmberBgBrush = new(Windows.UI.Color.FromArgb(32, 245, 158, 11));

    public Brush StatusColor => IsHealthy ? GreenBrush : AmberBrush;
    public Brush StatusBadgeBg => IsHealthy ? GreenBgBrush : AmberBgBrush;
}

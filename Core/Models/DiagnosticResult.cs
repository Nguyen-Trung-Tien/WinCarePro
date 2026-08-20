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

    public string DisplayCheckName => Services.TranslationManager.Instance.T(CheckName);
    public string DisplayCategory => Services.TranslationManager.Instance.T(Category);
    public string DisplayDescription => Services.TranslationManager.Instance.T(Description);
    public string DisplayStatusLabel => Services.TranslationManager.Instance.T(StatusLabel);

    [System.Text.Json.Serialization.JsonIgnore]
    public Visibility StatusBadgeVisibility => IsHealthy ? Visibility.Visible : Visibility.Collapsed;

    [System.Text.Json.Serialization.JsonIgnore]
    public Visibility FixButtonVisibility => IsHealthy ? Visibility.Collapsed : Visibility.Visible;

    private static SolidColorBrush? _greenBrush;
    private static SolidColorBrush? _amberBrush;
    private static SolidColorBrush? _greenBgBrush;
    private static SolidColorBrush? _amberBgBrush;

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusColor
    {
        get
        {
            try
            {
                return IsHealthy
                    ? (_greenBrush ??= new(Windows.UI.Color.FromArgb(255, 16, 185, 129)))
                    : (_amberBrush ??= new(Windows.UI.Color.FromArgb(255, 245, 158, 11)));
            }
            catch
            {
                return null;
            }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusBadgeBg
    {
        get
        {
            try
            {
                return IsHealthy
                    ? (_greenBgBrush ??= new(Windows.UI.Color.FromArgb(32, 16, 185, 129)))
                    : (_amberBgBrush ??= new(Windows.UI.Color.FromArgb(32, 245, 158, 11)));
            }
            catch
            {
                return null;
            }
        }
    }
}

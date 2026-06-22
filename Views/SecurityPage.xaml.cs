using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityViewModel ViewModel { get; }

    public SecurityPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = new SecurityViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanSecurityAsync();
    }

    private async void OnAdvertisingIdToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("advertisingid", ts.IsOn);
        }
    }

    private async void OnTelemetryToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("telemetry", ts.IsOn);
        }
    }

    private async void OnClipboardToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("clipboardhistory", ts.IsOn);
        }
    }

    private async void OnInputTrackingToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("tracking", ts.IsOn);
        }
    }

    private async void OnClearClipboardClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearClipboardAsync();
    }

    private async void OnClearRecentClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearRecentFilesAsync();
    }

    internal bool IsNot(bool val) => !val;

    internal Visibility GetVisibility(int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Brush GetScoreBrush(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Emerald Green
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber Orange
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Crimson Red
    }

    internal Brush GetScoreBadgeBackground(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129));
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11));
        return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68));
    }

    internal string GetStatusText(int score)
    {
        if (score >= 90) return "PROTECTED - PC defense levels are high.";
        if (score >= 75) return "MODERATE - Consider enabling all features for peak defense.";
        return "RISK DETECTED - Crucial features disabled. Check alerts.";
    }

    internal Brush GetAvBadgeBg(string status)
    {
        bool ok = status.Contains("Enabled") || status.Contains("Running");
        var color = ok ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    internal Brush GetAvColor(string status)
    {
        bool ok = status.Contains("Enabled") || status.Contains("Running");
        var color = ok ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    internal string GetAvLabel(string status)
    {
        return (status.Contains("Enabled") || status.Contains("Running")) ? "Secure" : "Action Needed";
    }

    internal Brush GetFirewallBadgeBg(bool active)
    {
        var color = active ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    internal Brush GetFirewallColor(bool active)
    {
        var color = active ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 239, 68, 68);
        return new SolidColorBrush(color);
    }

    internal string GetFirewallLabel(bool active)
    {
        return active ? "Enabled" : "Disabled";
    }
}

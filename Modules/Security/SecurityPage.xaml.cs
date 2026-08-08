using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityViewModel ViewModel { get; }

    public SecurityPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = new SecurityViewModel();
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateLoadingOverlayState();
            }
        };

        this.Loaded += (s, e) =>
        {
            DataContext = ViewModel;
            UpdateLoadingOverlayState();
        };
    }

    // --- Security Center Tab: Shortcut buttons to Windows built-in tools ---

    private void OnOpenWindowsSecurityClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("windowsdefender:") { UseShellExecute = true }); }
        catch { Process.Start(new ProcessStartInfo("ms-settings:windowsdefender") { UseShellExecute = true }); }
    }

    private void OnOpenMsinfoClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("msinfo32.exe") { UseShellExecute = true });
    }

    private void OnOpenWindowsUpdateClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
    }

    private void OnOpenTaskManagerClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
    }

    // --- Privacy Tuning Tab handlers ---

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
        var btn = WipeClipboardBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, WipeClipboardRing, WipeClipboardText, null,
            "Wiping Clipboard...", "Wipe Clipboard Cache",
            async () =>
            {
                await ViewModel.ClearClipboardAsync();
            },
            minDurationMs: 1000);
    }

    private async void OnClearRecentClick(object sender, RoutedEventArgs e)
    {
        var btn = ClearRecentBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ClearRecentRing, ClearRecentText, null,
            "Clearing Recent Files...", "Clear Recent Files & Run History",
            async () =>
            {
                await ViewModel.ClearRecentFilesAsync();
            },
            minDurationMs: 1000);
    }

    public bool IsNot(bool val) => !val;

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsBusy)
        {
            LoadingOverlayGrid.Visibility = Visibility.Visible;
            FadeInLoading.Begin();
        }
        else
        {
            FadeOutLoading.Begin();
        }
    }

    private void FadeOutLoading_Completed(object? sender, object e)
    {
        if (!ViewModel.IsBusy && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}

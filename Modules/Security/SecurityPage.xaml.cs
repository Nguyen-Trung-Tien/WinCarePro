using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityViewModel ViewModel { get; }

    public SecurityPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Services?.GetService<SecurityViewModel>() ?? new SecurityViewModel();
        this.DataContext = ViewModel;
        
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
            TranslationManager.Instance.Translate(this);
        };

        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        this.ActualThemeChanged += (s, e) =>
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                Bindings.Update();
            });
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            TranslationManager.Instance.Translate(this);
            Bindings.Update();
        });
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.DataContext = ViewModel;
        ViewModel.LoadPrivacySettings();
        _ = ViewModel.ScanSecurityAsync();
        TranslationManager.Instance.Translate(this);
        Bindings.Update();
    }

    // ==================== SECURITY CENTER: SHORTCUTS & LAUNCHERS ====================

    private void OnOpenWindowsSecurityClick(object sender, RoutedEventArgs e)
    {
        try 
        { 
            Process.Start(new ProcessStartInfo("windowsdefender:") { UseShellExecute = true }); 
        }
        catch 
        { 
            try 
            { 
                Process.Start(new ProcessStartInfo("ms-settings:windowsdefender") { UseShellExecute = true }); 
            } 
            catch { }
        }
    }

    private void OnOpenMsinfoClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("msinfo32.exe") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenWindowsUpdateClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenTaskManagerClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenFirewallClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("wf.msc") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenEventViewerClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("eventvwr.msc") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenServicesClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("services.msc") { UseShellExecute = true }); } catch { }
    }

    private void OnOpenRegistryEditorClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true }); } catch { }
    }

    private async void OnScanSecurityClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanSecurityAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Security Scan Complete".T(),
                string.Format("System Health Score: {0}/100".T(), ViewModel.SecurityScore), "Success");
        }
    }

    // ==================== PRIVACY TUNING TOGGLES ====================

    private async void OnAdvertisingIdToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("advertisingid", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("advertisingid", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnTelemetryToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("telemetry", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("telemetry", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnClipboardToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("clipboardhistory", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("clipboardhistory", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnInputTrackingToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("tracking", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("tracking", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    // ==================== TRACE ERADICATION ====================

    private async void OnClearClipboardClick(object sender, RoutedEventArgs e)
    {
        var btn = WipeClipboardBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, WipeClipboardRing, WipeClipboardText, null,
            "Wiping Clipboard...".T(), "Wipe Clipboard Cache".T(),
            async () =>
            {
                await ViewModel.ClearClipboardAsync();
            },
            minDurationMs: 800);

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Clipboard Cache".T(),
                "Clipboard memory successfully cleared.".T(), "Success");
        }
    }

    private async void OnClearRecentClick(object sender, RoutedEventArgs e)
    {
        var btn = ClearRecentBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ClearRecentRing, ClearRecentText, null,
            "Clearing Recent Files...".T(), "Clear Recent Files & Run History".T(),
            async () =>
            {
                await ViewModel.ClearRecentFilesAsync();
            },
            minDurationMs: 800);

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Activity Traces".T(),
                "Recent items and Explorer Run history successfully cleared.".T(), "Success");
        }
    }

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

    public bool IsNot(bool val) => !val;
}

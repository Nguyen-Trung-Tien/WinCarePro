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
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;

    public SecurityPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Services?.GetService<SecurityViewModel>() ?? new SecurityViewModel();
        this.DataContext = ViewModel;
        
        _vmPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateLoadingOverlayState();
            }
        };
        ViewModel.PropertyChanged += _vmPropertyChangedHandler;

        this.Loaded += (s, e) =>
        {
            DataContext = ViewModel;
            UpdateLoadingOverlayState();
            TranslationManager.Instance.Translate(this);
        };

        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        this.Unloaded += (s, e) =>
        {
            if (ViewModel != null) ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        };

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
        ViewModel?.Initialize();
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            ViewModel.PropertyChanged += _vmPropertyChangedHandler;
        }
        this.DataContext = ViewModel;
        ViewModel?.LoadPrivacySettings();
        _ = ViewModel?.ScanSecurityAsync();
        _ = ViewModel?.LoadSafeguardsAsync();
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        TranslationManager.Instance.Translate(this);
        Bindings.Update();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            ViewModel.Cleanup();
        }
    }

    // ==================== SECURITY CENTER: SHORTCUTS & LAUNCHERS ====================

    private void SafeLaunchProcess(string target)
    {
        try
        {
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri(target))
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
        }
        catch { }
    }

    private void OnOpenWindowsSecurityClick(object sender, RoutedEventArgs e)
    {
        try 
        { 
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri("windowsdefender:"))
                Process.Start(new ProcessStartInfo("windowsdefender:") { UseShellExecute = true }); 
            else
                SafeLaunchProcess("ms-settings:windowsdefender");
        }
        catch 
        { 
            SafeLaunchProcess("ms-settings:windowsdefender");
        }
    }

    private void OnOpenMsinfoClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("msinfo32.exe");

    private void OnOpenWindowsUpdateClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("ms-settings:windowsupdate");

    private void OnOpenTaskManagerClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("taskmgr.exe");

    private void OnOpenFirewallClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("wf.msc");

    private void OnOpenEventViewerClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("eventvwr.msc");

    private void OnOpenServicesClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("services.msc");

    private void OnOpenRegistryEditorClick(object sender, RoutedEventArgs e) => SafeLaunchProcess("regedit.exe");

    private void OnSecurityMatrixActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string target && !string.IsNullOrWhiteSpace(target))
        {
            SafeLaunchProcess(target);
        }
    }

    public static Microsoft.UI.Xaml.Media.Brush GetStatusPillBg(bool isSecure) =>
        isSecure 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68));

    public static Microsoft.UI.Xaml.Media.Brush GetStatusPillBorder(bool isSecure) =>
        isSecure 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 239, 68, 68));

    public static Microsoft.UI.Xaml.Media.Brush GetStatusPillFg(bool isSecure) =>
        isSecure 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));

    public static string GetStatusIconGlyph(bool isSecure) =>
        isSecure ? "\uE73E" : "\uE783";

    private async void OnScanSecurityClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanSecurityAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Security Scan Complete".T(),
                string.Format("System Health Score: {0}/100".T(), ViewModel.SecurityScore), "Success");
        }
    }

    private async void OnFixAlertClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WinCarePro.Models.SecurityAlertItem alert)
        {
            await ViewModel.FixSecurityAlertAsync(alert);
            if (App.MainWindowInstance is MainWindow mw)
            {
                mw.ShowToastFromDb("Security Issue Resolved".T(),
                    string.Format("Resolved: {0}".T(), alert.Title), "Success");
            }
        }
    }

    private async void OnExportReportClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportSecurityReportAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Security Audit Report".T(),
                "Audit report copied to clipboard successfully.".T(), "Success");
        }
    }

    private async void OnToggleSafeguardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WinCarePro.Models.SecuritySafeguardItem item)
        {
            await ViewModel.ToggleSafeguardAsync(item);
        }
    }

    private async void OnQuickHardenClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.QuickHardenAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Security Hardened".T(),
                string.Format("System Health Score: {0}/100".T(), ViewModel.SecurityScore), "Success");
        }
    }

    private async void OnDefenseProfileGamerClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyDefenseProfileAsync("Gamer");
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Defense Profile".T(), "Gamer Stealth Profile activated.".T(), "Success");
        }
    }

    private async void OnDefenseProfileBalancedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyDefenseProfileAsync("Balanced");
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Defense Profile".T(), "Balanced Armor Profile activated.".T(), "Success");
        }
    }

    private async void OnDefenseProfileHardenedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyDefenseProfileAsync("Hardened");
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Defense Profile".T(), "Zero-Trust Hardened Profile activated.".T(), "Success");
        }
    }

    private async void OnEnableFirewallClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.EnableFirewallAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Firewall Active".T(), "Windows Firewall has been successfully enabled.".T(), "Success");
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

    private async void OnCortanaBingToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("cortanabing", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("cortanabing", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnLocationToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("location", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("location", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnFeedbackToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("feedback", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("feedback", lts.IsOn);
            }
            finally
            {
                lts.IsLoading = false;
            }
        }
    }

    private async void OnAppDiagnosticsToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            await ViewModel.TogglePrivacySettingAsync("appdiagnostics", ts.IsOn);
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await ViewModel.TogglePrivacySettingAsync("appdiagnostics", lts.IsOn);
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
        await ViewModel.ClearClipboardAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Clipboard Cache".T(),
                "Clipboard memory successfully cleared.".T(), "Success");
        }
    }

    private async void OnClearRecentClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearRecentFilesAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Activity Traces".T(),
                "Recent items and Explorer Run history successfully cleared.".T(), "Success");
        }
    }

    private async void OnClearJumpListsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearJumpListsAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Jump Lists".T(),
                "Explorer Jump Lists cleared successfully.".T(), "Success");
        }
    }

    private async void OnClearRunHistoryClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearRunHistoryAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Run & Typed History".T(),
                "Explorer Run dialog and address bar history cleared.".T(), "Success");
        }
    }

    private async void OnClearAllTracesClick(object sender, RoutedEventArgs e)
    {
        int count = await ViewModel.ClearAllActivityTracesAsync();
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Trace Eradication Complete".T(),
                string.Format("Purged {0} cached trace entries from system.".T(), count), "Success");
        }
    }

    private void UpdateLoadingOverlayState()
    {
        // Minimal Fluent UX: overlay removed to avoid UI freeze and visual obstruction
    }

    public bool IsNot(bool val) => !val;
}

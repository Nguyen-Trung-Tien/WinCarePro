using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class RepairPage : Page
{
    public RepairViewModel ViewModel { get; }

    public RepairPage()
    {
        ViewModel = App.Services?.GetService<RepairViewModel>() ?? new RepairViewModel();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy) || e.PropertyName == nameof(ViewModel.IsScanningDiagnostics))
            {
                DispatcherQueue?.TryEnqueue(UpdateProgressOverlayState);
            }
        };

        this.Loaded += (s, e) =>
        {
            UpdateProgressOverlayState();
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
        this.Bindings.Update();
        SetActiveTab(ViewModel?.ActiveTab ?? "diagnostics");
        TranslationManager.Instance.Translate(this);
    }

    public void OnTabDiagnosticsClick(object sender, RoutedEventArgs e) => SetActiveTab("diagnostics");
    public void OnTabSfcDismClick(object sender, RoutedEventArgs e) => SetActiveTab("sfcdism");
    public void OnTabRegistryClick(object sender, RoutedEventArgs e) => SetActiveTab("registry");
    public void OnTabNetworkClick(object sender, RoutedEventArgs e) => SetActiveTab("network");

    private Style? _accentStyle;
    private Style? _defaultStyle;

    private Style? GetButtonStyle(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var styleObj) && styleObj is Style style)
        {
            return style;
        }
        return null;
    }

    private void SetActiveTab(string tabName)
    {
        if (ViewModel != null)
        {
            ViewModel.ActiveTab = tabName;
        }

        _accentStyle ??= GetButtonStyle("VibrantPrimaryButtonStyle") ?? GetButtonStyle("AccentButtonStyle");
        _defaultStyle ??= GetButtonStyle("DefaultButtonStyle");

        if (BtnTabDiagnostics != null) BtnTabDiagnostics.Style = tabName == "diagnostics" ? _accentStyle : _defaultStyle;
        if (BtnTabSfcDism != null) BtnTabSfcDism.Style = tabName == "sfcdism" ? _accentStyle : _defaultStyle;
        if (BtnTabRegistry != null) BtnTabRegistry.Style = tabName == "registry" ? _accentStyle : _defaultStyle;
        if (BtnTabNetwork != null) BtnTabNetwork.Style = tabName == "network" ? _accentStyle : _defaultStyle;

        if (SectionDiagnostics != null) SectionDiagnostics.Visibility = tabName == "diagnostics" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionSfcDism != null) SectionSfcDism.Visibility = tabName == "sfcdism" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionRegistry != null) SectionRegistry.Visibility = tabName == "registry" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionNetwork != null) SectionNetwork.Visibility = tabName == "network" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateProgressOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        bool isLoading = ViewModel?.IsBusy == true || ViewModel?.IsScanningDiagnostics == true;
        if (isLoading)
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
        bool isLoading = ViewModel?.IsBusy == true || ViewModel?.IsScanningDiagnostics == true;
        if (!isLoading && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnScanDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = ScanDiagnosticsBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanDiagRing, ScanDiagText, ScanDiagIcon,
            "Scanning Diagnostics...", "Scan Diagnostics",
            async () =>
            {
                await ViewModel.RunDiagnosticsScanAsync();
            },
            minDurationMs: 1000);
    }

    private async void OnFixSelectedClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        bool isAutoRepair = (sender as Button) == AutoRepairBtn;
        var btn = (sender as Button) ?? FixSelectedBtn ?? AutoRepairBtn;
        var ring = isAutoRepair ? AutoRepairRing : FixSelectedRing;
        var text = isAutoRepair ? AutoRepairText : FixSelectedText;
        var icon = isAutoRepair ? null : FixSelectedIcon;
        string origText = isAutoRepair ? "Auto-Repair Selected Issues" : "Fix Selected";

        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ring, text, icon,
            "Repairing Issues...", origText,
            async () =>
            {
                await ViewModel.FixAllSelectedIssuesAsync();
            },
            minDurationMs: 1000);
    }

    private async void OnFixSingleIssueClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && (sender as Button)?.DataContext is DiagnosticIssueItem item)
        {
            await ViewModel.FixSingleIssueAsync(item);
        }
    }

    private void OnCancelOperationClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.CancelCurrentOperation();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.SelectAllIssues();
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.DeselectAllIssues();
    }

    private void OnFilterCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.SelectedFilterCategory = tag;
        }
    }

    private async void OnRepairRegistryPoliciesClick(object sender, RoutedEventArgs e)
    {
        try { if (ViewModel != null) await ViewModel.RepairRegistryPoliciesAsync(); } catch { }
    }

    private async void OnCreateRestorePointClick(object sender, RoutedEventArgs e)
    {
        try { if (ViewModel != null) await ViewModel.CreateRestorePointAsync(); } catch { }
    }

    private async void OnRepairNetworkStackClick(object sender, RoutedEventArgs e)
    {
        try { if (ViewModel != null) await ViewModel.RepairNetworkStackAsync(); } catch { }
    }

    private async void OnSfcScanClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = SfcScanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, SfcScanRing, SfcScanText, null,
            "Running SFC Verify...", "SFC Verify",
            async () =>
            {
                await ViewModel.RunSfcScanAsync(false);
            },
            minDurationMs: 1000);
    }

    private async void OnSfcRepairClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = SfcRepairBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, SfcRepairRing, SfcRepairText, null,
            "Repairing System Files...", "SFC Repair",
            async () =>
            {
                await ViewModel.RunSfcScanAsync(true);
            },
            minDurationMs: 1000);
    }

    private async void OnDismCheckClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = DismCheckBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismCheckRing, DismCheckText, null,
            "Checking DISM Health...", "DISM Check",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("checkhealth");
            },
            minDurationMs: 1000);
    }

    private async void OnDismScanClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = DismScanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismScanRing, DismScanText, null,
            "Scanning DISM Health...", "DISM Scan",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("scanhealth");
            },
            minDurationMs: 1000);
    }

    private async void OnDismRestoreClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = DismRestoreBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismRestoreRing, DismRestoreText, null,
            "Restoring DISM Health...", "DISM Restore",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("restorehealth");
            },
            minDurationMs: 1000);
    }

    private async void OnDismCleanClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var btn = DismCleanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismCleanRing, DismCleanText, null,
            "Cleaning Component Store...", "Clean WinSxS",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("startcomponentcleanup");
            },
            minDurationMs: 1000);
    }

    private async void OnResetUpdateClick(object sender, RoutedEventArgs e)
    {
        try { if (ViewModel != null) await ViewModel.RepairWindowsUpdateAsync(); } catch { }
    }

    private async void OnRestoreServicesClick(object sender, RoutedEventArgs e)
    {
        try { if (ViewModel != null) await ViewModel.RepairServicesConfigAsync(); } catch { }
    }

    public bool IsNot(bool val) => !val;

    public bool CanFixSelected(int count, bool isBusy) => count > 0 && !isBusy;

    public string GetProgressText(int percent) => $"{percent}%";

    public Microsoft.UI.Xaml.Media.Brush GetRestrictionColor(int count)
    {
        return count > 0 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)) // Red
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
    }

    public Microsoft.UI.Xaml.Media.Brush GetScoreColor(int score)
    {
        if (score >= 90) return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
        if (score >= 70) return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
    }

    public string GetScoreText(int score) => $"{score} / 100";
}



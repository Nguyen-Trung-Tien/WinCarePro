using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Views;

public sealed partial class RepairPage : Page
{
    public RepairViewModel ViewModel { get; }

    public RepairPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RepairViewModel>();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy) || e.PropertyName == nameof(ViewModel.IsScanningDiagnostics))
            {
                UpdateProgressOverlayState();
            }
        };

        this.Loaded += (s, e) => UpdateProgressOverlayState();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.DataContext = ViewModel;
        this.Bindings.Update();
    }

    private void UpdateProgressOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        bool isLoading = ViewModel.IsBusy || ViewModel.IsScanningDiagnostics;
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
        bool isLoading = ViewModel.IsBusy || ViewModel.IsScanningDiagnostics;
        if (!isLoading && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnScanDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanDiagnosticsBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanDiagRing, ScanDiagText, ScanDiagIcon,
            "Scanning Diagnostics...", "Scan Diagnostics",
            async () =>
            {
                await ViewModel.RunDiagnosticsScanAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnFixSelectedClick(object sender, RoutedEventArgs e)
    {
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
            minDurationMs: 1200);
    }

    private async void OnRepairRegistryPoliciesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairRegistryPoliciesAsync();
    }

    private async void OnCreateRestorePointClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateRestorePointAsync();
    }

    private async void OnRepairNetworkStackClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairNetworkStackAsync();
    }

    private async void OnSfcScanClick(object sender, RoutedEventArgs e)
    {
        var btn = SfcScanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, SfcScanRing, SfcScanText, null,
            "Running SFC Scan...", "SFC Scan",
            async () =>
            {
                await ViewModel.RunSfcScanAsync(false);
            },
            minDurationMs: 1200);
    }

    private async void OnSfcRepairClick(object sender, RoutedEventArgs e)
    {
        var btn = SfcRepairBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, SfcRepairRing, SfcRepairText, null,
            "Repairing System Files...", "SFC Repair",
            async () =>
            {
                await ViewModel.RunSfcScanAsync(true);
            },
            minDurationMs: 1200);
    }

    private async void OnDismCheckClick(object sender, RoutedEventArgs e)
    {
        var btn = DismCheckBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismCheckRing, DismCheckText, null,
            "Checking DISM Health...", "DISM Check",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("check");
            },
            minDurationMs: 1200);
    }

    private async void OnDismRestoreClick(object sender, RoutedEventArgs e)
    {
        var btn = DismRestoreBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, DismRestoreRing, DismRestoreText, null,
            "Restoring DISM Health...", "DISM Restore",
            async () =>
            {
                await ViewModel.RunDismOperationAsync("restore");
            },
            minDurationMs: 1200);
    }

    private async void OnResetUpdateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairWindowsUpdateAsync();
    }

    private async void OnRestoreServicesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairServicesConfigAsync();
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

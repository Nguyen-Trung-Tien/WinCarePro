using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

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

        // Auto-scroll terminal console to the bottom when logs are appended
        ConsoleLogTextBox.TextChanged += (s, e) =>
        {
            ConsoleLogTextBox.Select(ConsoleLogTextBox.Text.Length, 0);
        };
    }

    private async void OnScanDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsScanAsync();
    }

    private async void OnFixSelectedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.FixAllSelectedIssuesAsync();
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
        await ViewModel.RunSfcScanAsync(false);
    }

    private async void OnSfcRepairClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunSfcScanAsync(true);
    }

    private async void OnDismCheckClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDismOperationAsync("check");
    }

    private async void OnDismRestoreClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDismOperationAsync("restore");
    }

    private async void OnResetUpdateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairWindowsUpdateAsync();
    }

    private async void OnRestoreServicesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairServicesConfigAsync();
    }

    internal bool IsNot(bool val) => !val;

    internal bool CanFixSelected(int count, bool isBusy) => count > 0 && !isBusy;

    internal string GetProgressText(int percent) => $"{percent}%";

    internal Microsoft.UI.Xaml.Media.Brush GetRestrictionColor(int count)
    {
        return count > 0 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)) // Red
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
    }

    internal Microsoft.UI.Xaml.Media.Brush GetScoreColor(int score)
    {
        if (score >= 90) return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
        if (score >= 70) return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
    }

    internal string GetScoreText(int score) => $"{score} / 100";

}

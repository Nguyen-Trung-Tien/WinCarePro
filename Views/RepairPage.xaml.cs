using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class RepairPage : Page
{
    public RepairViewModel ViewModel { get; }

    public Page Progress { get; set; } = null!; // placeholder for template compatibility if needed

    public RepairPage()
    {
        InitializeComponent();
        ViewModel = new RepairViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
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
        await ViewModel.RunDismOperationAsync("checkhealth");
    }

    private async void OnDismScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDismOperationAsync("scanhealth");
    }

    private async void OnDismRestoreClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDismOperationAsync("restorehealth");
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

    internal string FormatPercent(int val) => $"{val}%";
}

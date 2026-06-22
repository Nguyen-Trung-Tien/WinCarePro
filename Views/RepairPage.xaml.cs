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
        this.DataContext = ViewModel;
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
}

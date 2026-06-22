using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class RegistryPage : Page
{
    public RegistryViewModel ViewModel { get; }

    public RegistryPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RegistryViewModel>();
        this.DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanRegistryAsync();
    }

    private async void OnFixClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairSelectedAsync();
    }

    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BackupRegistryAsync();
    }

    internal bool IsNot(bool b) => !b;
}

using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class RegistryBackupPage : Page
{
    public RegistryBackupViewModel ViewModel { get; }

    public RegistryBackupPage()
    {
        InitializeComponent();
        ViewModel = new RegistryBackupViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnScanRegistryClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanRegistryAsync();
    }

    private async void OnFixRegistryClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.FixRegistryAsync();
    }

    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateRegistryBackupAsync();
    }

    private async void OnRestoreBackupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            await ViewModel.RestoreRegistryBackupAsync(path);
        }
    }

    private async void OnCreateRestorePointClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateRestorePointAsync();
    }

    private void OnLaunchWizardClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LaunchRestoreWizard();
    }

    internal bool IsNot(bool val) => !val;

    internal bool CanFix(int count, bool isBusy)
    {
        return count > 0 && !isBusy;
    }

    internal string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }
}

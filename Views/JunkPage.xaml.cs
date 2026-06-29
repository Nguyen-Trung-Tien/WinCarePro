using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Views;

public sealed partial class JunkPage : Page
{
    public JunkViewModel ViewModel { get; }

    public JunkPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<JunkViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
        var dialogService = App.Services.GetService<IDialogService>();
        dialogService?.SetXamlRoot(this.XamlRoot);
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private async void OnScanJunkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private async void OnCleanJunkClick(object sender, RoutedEventArgs e)
    {
        var lockingAppService = App.Services.GetService<ILockingAppService>();
        var dialogService = App.Services.GetService<IDialogService>();
        if (lockingAppService != null && dialogService != null)
        {
            var apps = await lockingAppService.GetLockingAppsAsync();
            if (apps.Count > 0)
            {
                dialogService.SetXamlRoot(this.XamlRoot);
                var action = await dialogService.ShowLockingAppsDialogAsync(apps);
                if (action == CleaningAction.CloseAndClean)
                {
                    await ViewModel.CloseAppsOnlyAsync();
                    await ViewModel.CleanAsync();
                }
                else if (action == CleaningAction.CleanAnyway)
                {
                    await ViewModel.CleanAsync();
                }
                else if (action == CleaningAction.ScheduleAfterRestart)
                {
                    await ViewModel.ScheduleCleanupAfterRestartAsync();
                }
                return;
            }
        }
        await ViewModel.CleanAsync();
    }

    private void OnJunkSelectionChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTotalSize();
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSelectedFolder();
    }

    internal bool IsNot(bool val) => !val;

    internal bool CanClean(bool isCleaning, int count)
    {
        return !isCleaning && count > 0;
    }

    internal bool GetProgressRingActive(bool scanning, bool cleaning)
    {
        return scanning || cleaning;
    }

    internal Visibility GetDetailVisibility(JunkCategory? selectedItem, bool scanning, bool cleaning)
    {
        return (selectedItem != null && !scanning && !cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetProgressVisibility(bool scanning, bool cleaning)
    {
        return (scanning || cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetEmptyVisibility(JunkCategory? selectedItem, bool scanning, bool cleaning)
    {
        return (selectedItem == null && !scanning && !cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetWarningVisibility(bool hasLockingApps)
    {
        return hasLockingApps ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetLockedSizeVisibility(long lockedBytes)
    {
        return lockedBytes > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnCloseAppsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.CloseLockingAppsAsync();
    }
}

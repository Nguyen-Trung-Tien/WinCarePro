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

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(JunkPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public JunkPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<JunkViewModel>();
        this.DataContext = ViewModel;

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 800;
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;

            if (LeftColumn != null && RightColumn != null)
            {
                if (isWide)
                {
                    LeftColumn.Width = new GridLength(1.2, GridUnitType.Star);
                    RightColumn.Width = new GridLength(0.8, GridUnitType.Star);
                }
                else
                {
                    LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightColumn.Width = new GridLength(0, GridUnitType.Pixel);
                }
            }
        };
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

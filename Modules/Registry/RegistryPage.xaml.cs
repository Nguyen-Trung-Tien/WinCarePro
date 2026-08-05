using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class RegistryPage : Page
{
    public RegistryViewModel ViewModel { get; }

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(RegistryPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public RegistryPage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RegistryViewModel>();
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateLoadingOverlayState();
            }
        };

        this.Loaded += (s, e) => UpdateLoadingOverlayState();

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 800;
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;

            if (LeftCol != null && RightCol != null)
            {
                if (isWide)
                {
                    LeftCol.Width = new GridLength(1, GridUnitType.Star);
                    RightCol.Width = new GridLength(380, GridUnitType.Pixel);
                }
                else
                {
                    LeftCol.Width = new GridLength(1, GridUnitType.Star);
                    RightCol.Width = new GridLength(0, GridUnitType.Pixel);
                }
            }
        };
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanRegistryAsync();
    }

    private async void OnRepairClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RepairSelectedAsync();
    }

    // Registry Editor shortcut — safe alternative to registry cleaner
    private void OnOpenRegeditClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true });
    }

    // System Restore shortcut
    private void OnOpenSystemRestoreClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("rstrui.exe") { UseShellExecute = true });
    }

    // Registry Backup — still valuable, kept
    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BackupRegistryAsync();
    }

    public bool IsNot(bool b) => !b;

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsBusy)
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
        if (!ViewModel.IsBusy && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}

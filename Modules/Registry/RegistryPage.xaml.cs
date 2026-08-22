using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;

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

        this.Loaded += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            UpdateLoadingOverlayState();
        };
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        await ViewModel.ScanRegistryAsync();
    }

    private async void OnRepairClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);
        await ViewModel.RepairSelectedAsync();
    }

    // Registry Editor shortcut — safe alternative to registry cleaner
    private void OnOpenRegeditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri("regedit.exe"))
                Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true });
        }
        catch { }
    }

    // System Restore shortcut
    private void OnOpenSystemRestoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
        try
        {
            if (WinCarePro.Infrastructure.Security.InputSanitizer.IsSafeUri("rstrui.exe"))
                Process.Start(new ProcessStartInfo("rstrui.exe") { UseShellExecute = true });
        }
        catch { }
    }

    // Registry Backup — safe backup
    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
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

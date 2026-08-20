using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;

using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Animations;

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
        ViewModel = App.Services.GetRequiredService<JunkViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsScanning) || e.PropertyName == nameof(ViewModel.IsCleaning))
            {
                this.DispatcherQueue?.TryEnqueue(() => UpdateProgressOverlayState());
            }
        };

        this.Loaded += (s, e) =>
        {
            UpdateProgressOverlayState();
            TranslationManager.Instance.Translate(this);
        };

        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) => TranslationManager.Instance.LanguageChanged -= langHandler;

        this.SizeChanged += (s, e) =>
        {
            if (LeftColumn != null && RightColumn != null)
            {
                if (e.NewSize.Width >= 900)
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
        var btn = ScanJunkBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 300);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanJunkRing, ScanJunkText, ScanJunkIcon,
            "Scanning Debris...", "Scan Directories",
            async () =>
            {
                await ViewModel.ScanAsync();
            },
            minDurationMs: 400);

        ViewModel.FinalizeScan();
    }

    private async void OnCleanJunkClick(object sender, RoutedEventArgs e)
    {
        var btn = CleanJunkBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.07f, 350);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, CleanJunkRing, CleanJunkText, CleanJunkIcon,
            "Cleaning Debris...", "Clean Now",
            async () =>
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
            },
            minDurationMs: 400);

        ViewModel.FinalizeClean();
    }

    private void OnJunkSelectionChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTotalSize();
    }

    private void OnSelectAllCategoriesClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllCategories();
    }

    private void OnDeselectAllCategoriesClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAllCategories();
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSelectedFolder();
    }

    public bool IsNot(bool val) => !val;

    public bool CanClean(bool isCleaning, int count)
    {
        return !isCleaning && count > 0;
    }

    public Visibility GetDeckVisibility(bool isCleaning, int count)
    {
        return (!isCleaning && count > 0) ? Visibility.Visible : Visibility.Collapsed;
    }

    public bool GetProgressRingActive(bool scanning, bool cleaning)
    {
        return scanning || cleaning;
    }

    public Visibility GetDetailVisibility(JunkCategory? selectedItem, bool scanning, bool cleaning)
    {
        return (selectedItem != null && !scanning && !cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetProgressVisibility(bool scanning, bool cleaning)
    {
        return (scanning || cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetEmptyVisibility(JunkCategory? selectedItem, bool scanning, bool cleaning)
    {
        return (selectedItem == null && !scanning && !cleaning) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetWarningVisibility(bool hasLockingApps)
    {
        return hasLockingApps ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetLockedSizeVisibility(long lockedBytes)
    {
        return lockedBytes > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnCloseAppsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.CloseLockingAppsAsync();
    }

    private void UpdateProgressOverlayState()
    {
        if (ProgressOverlayGrid == null || FadeInProgress == null || FadeOutProgress == null) return;

        bool active = ViewModel.IsScanning || ViewModel.IsCleaning;
        if (active)
        {
            ProgressOverlayGrid.Visibility = Visibility.Visible;
            FadeInProgress.Begin();
        }
        else
        {
            FadeOutProgress.Begin();
        }
    }

    private void FadeOutProgress_Completed(object? sender, object e)
    {
        bool active = ViewModel.IsScanning || ViewModel.IsCleaning;
        if (!active && ProgressOverlayGrid != null)
        {
            ProgressOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }
}

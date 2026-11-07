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
            UpdateResponsiveLayout(this.ActualWidth);
        };

        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) => TranslationManager.Instance.LanguageChanged -= langHandler;

        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        };
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        // 1. Dual-Pane Master-Detail Area (Side-by-side on >= 950px, collapse details on < 950px)
        if (LeftColumn != null && RightColumn != null)
        {
            if (width >= 950)
            {
                LeftColumn.Width = new GridLength(1.15, GridUnitType.Star);
                RightColumn.Width = new GridLength(0.85, GridUnitType.Star);
                WideLayoutVisibility = Visibility.Visible;
            }
            else
            {
                LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                RightColumn.Width = new GridLength(0, GridUnitType.Pixel);
                WideLayoutVisibility = Visibility.Collapsed;
            }
        }

        // 2. Header Action Buttons Panel
        if (HeaderCol0 != null && HeaderCol1 != null && HeaderActionsPanel != null)
        {
            if (width >= 800)
            {
                HeaderCol0.Width = new GridLength(1, GridUnitType.Star);
                HeaderCol1.Width = GridLength.Auto;
                Grid.SetRow(HeaderActionsPanel, 0);
                Grid.SetColumn(HeaderActionsPanel, 1);
                Grid.SetColumnSpan(HeaderActionsPanel, 1);
                HeaderActionsPanel.Margin = new Thickness(0);
                HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                HeaderCol0.Width = new GridLength(1, GridUnitType.Star);
                HeaderCol1.Width = new GridLength(0, GridUnitType.Pixel);
                Grid.SetRow(HeaderActionsPanel, 1);
                Grid.SetColumn(HeaderActionsPanel, 0);
                Grid.SetColumnSpan(HeaderActionsPanel, 2);
                HeaderActionsPanel.Margin = new Thickness(0, 12, 0, 0);
                HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        // 3. Locking Apps Banner Actions
        if (LockingCol2 != null && LockingActionsPanel != null)
        {
            if (width >= 800)
            {
                LockingCol2.Width = GridLength.Auto;
                Grid.SetRow(LockingActionsPanel, 0);
                Grid.SetColumn(LockingActionsPanel, 2);
                Grid.SetColumnSpan(LockingActionsPanel, 1);
                LockingActionsPanel.Margin = new Thickness(0);
            }
            else
            {
                LockingCol2.Width = new GridLength(0, GridUnitType.Pixel);
                Grid.SetRow(LockingActionsPanel, 1);
                Grid.SetColumn(LockingActionsPanel, 0);
                Grid.SetColumnSpan(LockingActionsPanel, 3);
                LockingActionsPanel.Margin = new Thickness(0, 8, 0, 0);
            }
        }
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
        try
        {
            WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(this);
        }
        catch { }
        ViewModel.Cleanup();
    }

    private async void OnScanJunkClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanJunkBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 300);

        try
        {
            WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(this, Windows.UI.Color.FromArgb(220, 167, 139, 250));
            await UiLoadingHelper.ExecuteWithLoadingAsync(
                btn, ScanJunkRing, ScanJunkText, ScanJunkIcon,
                "Scanning Debris...", "Scan Directories",
                async () =>
                {
                    await ViewModel.ScanAsync();
                },
                minDurationMs: 1200);
        }
        finally
        {
            try
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(this);
                if (btn != null) WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(btn, Windows.UI.Color.FromArgb(255, 167, 139, 250));
            }
            catch { }
        }

        ViewModel.FinalizeScan();
    }

    private async void OnCleanJunkClick(object sender, RoutedEventArgs e)
    {
        var btn = CleanJunkBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.07f, 350);

        try
        {
            WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(this, Windows.UI.Color.FromArgb(220, 16, 185, 129));
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
                minDurationMs: 1200,
                restoreIsEnabled: true);
        }
        finally
        {
            try
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(this);
                if (btn != null)
                {
                    WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(this, Windows.UI.Color.FromArgb(255, 16, 185, 129));
                    WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btn, 1.10f, 400);
                }
            }
            catch { }
        }

        ViewModel.FinalizeClean();

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("System Cleanup Complete".T(), "Cleaned files and reclaimed storage successfully.".T(), "Success");
        }
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

    private void OnPresetSafeClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("Safe");
    }

    private void OnPresetAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("All");
    }

    private void OnDismissLockingAppsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DismissLockingAlert();
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSelectedFolder();
    }

    public bool IsNot(bool val) => !val;

    public bool CanClean(bool isCleaning, int count, long totalCleanableBytes)
    {
        return !isCleaning && count > 0 && totalCleanableBytes > 0;
    }

    public Style GetScanButtonStyle(bool hasCleanableJunk)
    {
        string key = hasCleanableJunk ? "StandardSecondaryButtonStyle" : "CleanerHeroScanButtonStyle";
        return (Application.Current?.Resources[key] as Style) ?? (Application.Current?.Resources["CleanerHeroScanButtonStyle"] as Style)!;
    }

    public Style GetCleanButtonStyle(bool hasCleanableJunk)
    {
        return (Application.Current?.Resources["CleanerHeroGreenButtonStyle"] as Style)!;
    }

    public Visibility GetCleanButtonVisibility(bool hasCleanableJunk)
    {
        return hasCleanableJunk ? Visibility.Visible : Visibility.Collapsed;
    }

    public string GetScanButtonText(bool hasCleanableJunk)
    {
        return hasCleanableJunk ? "Re-Scan System".T() : "Scan Directories".T();
    }

    public string GetCleanButtonText(bool hasCleanableJunk)
    {
        return hasCleanableJunk ? "Clean Now".T() : "Clean Now".T();
    }

    public string GetSummaryHintText(bool hasCleanableJunk)
    {
        return hasCleanableJunk ? "Ready to Clean — Click 'Clean Now'".T() : "Click 'Scan Directories' to Begin".T();
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

    public Visibility GetCelebrationVisibility(bool isCelebrationActive)
    {
        return isCelebrationActive ? Visibility.Visible : Visibility.Collapsed;
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

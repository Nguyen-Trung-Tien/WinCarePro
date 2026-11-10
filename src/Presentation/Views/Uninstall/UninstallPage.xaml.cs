using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Components;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Views;

public sealed partial class UninstallPage : Page
{
    public UninstallViewModel ViewModel { get; }

    public static readonly DependencyProperty BadgesColumnWidthProperty =
        DependencyProperty.Register(nameof(BadgesColumnWidth), typeof(GridLength), typeof(UninstallPage), new PropertyMetadata(GridLength.Auto));

    public GridLength BadgesColumnWidth
    {
        get => (GridLength)GetValue(BadgesColumnWidthProperty);
        set => SetValue(BadgesColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SizeColumnWidthProperty =
        DependencyProperty.Register(nameof(SizeColumnWidth), typeof(GridLength), typeof(UninstallPage), new PropertyMetadata(new GridLength(100)));

    public GridLength SizeColumnWidth
    {
        get => (GridLength)GetValue(SizeColumnWidthProperty);
        set => SetValue(SizeColumnWidthProperty, value);
    }

    public static readonly DependencyProperty WideLayoutVisibilityProperty =
        DependencyProperty.Register(nameof(WideLayoutVisibility), typeof(Visibility), typeof(UninstallPage), new PropertyMetadata(Visibility.Visible));

    public Visibility WideLayoutVisibility
    {
        get => (Visibility)GetValue(WideLayoutVisibilityProperty);
        set => SetValue(WideLayoutVisibilityProperty, value);
    }

    public static readonly DependencyProperty DetailsPaneVisibilityProperty =
        DependencyProperty.Register(nameof(DetailsPaneVisibility), typeof(Visibility), typeof(UninstallPage), new PropertyMetadata(Visibility.Collapsed));

    public Visibility DetailsPaneVisibility
    {
        get => (Visibility)GetValue(DetailsPaneVisibilityProperty);
        set => SetValue(DetailsPaneVisibilityProperty, value);
    }

    public static readonly DependencyProperty NarrowBackBtnVisibilityProperty =
        DependencyProperty.Register(nameof(NarrowBackBtnVisibility), typeof(Visibility), typeof(UninstallPage), new PropertyMetadata(Visibility.Collapsed));

    public Visibility NarrowBackBtnVisibility
    {
        get => (Visibility)GetValue(NarrowBackBtnVisibilityProperty);
        set => SetValue(NarrowBackBtnVisibilityProperty, value);
    }

    private void UpdateDetailsPaneVisibility()
    {
        bool isWide = this.ActualWidth >= 850;
        DetailsPaneVisibility = isWide ? Visibility.Visible : (ViewModel.SelectedApp != null ? Visibility.Visible : Visibility.Collapsed);
        NarrowBackBtnVisibility = (!isWide && ViewModel.SelectedApp != null) ? Visibility.Visible : Visibility.Collapsed;
        
        if (ListCol != null && DetailCol != null)
        {
            if (isWide)
            {
                ListCol.Width = new GridLength(3, GridUnitType.Star);
                DetailCol.Width = new GridLength(2, GridUnitType.Star);
            }
            else
            {
                if (ViewModel.SelectedApp != null)
                {
                    ListCol.Width = new GridLength(0, GridUnitType.Pixel);
                    DetailCol.Width = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    ListCol.Width = new GridLength(1, GridUnitType.Star);
                    DetailCol.Width = new GridLength(0, GridUnitType.Pixel);
                }
            }
        }
    }

    public UninstallPage()
    {
        ViewModel = App.Services.GetRequiredService<UninstallViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ThemeManager.Instance.RegisterPage(this);
        TranslationManager.Instance.RegisterPage(this);

        this.Loaded += (s, e) =>
        {
            var dialogService = App.Services.GetRequiredService<IDialogService>();
            dialogService.SetXamlRoot(this.XamlRoot);
            TranslationManager.Instance.Translate(this);
            UpdateDetailsPaneVisibility();
        };

        this.Unloaded += (s, e) =>
        {
            ThemeManager.Instance.UnregisterPage(this);
            TranslationManager.Instance.UnregisterPage(this);
        };

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.SelectedApp))
            {
                UpdateDetailsPaneVisibility();
            }
            else if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateSkeletonShimmerState();
            }
        };

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 850;
            BadgesColumnWidth = isWide ? GridLength.Auto : new GridLength(0);
            SizeColumnWidth = isWide ? new GridLength(100) : new GridLength(0);
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;
            UpdateDetailsPaneVisibility();
        };
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel?.Initialize();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel?.Cleanup();
    }

    private void OnBackToListClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedApp = null;
    }

    private async void OnReloadAppsClick(object sender, RoutedEventArgs e)
    {
        var btn = ReloadBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanAppsRing, ScanAppsText, ScanAppsIcon,
            "Scanning Applications...", "Scan Registry Apps",
            async () =>
            {
                await ViewModel.ScanAppsAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnSingleUninstallClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is InstalledAppInfo app)
        {
            var msgFormat = "Uninstall Single Confirm Message".T();
            var titleFormat = "Uninstall {0}".T();
            var confirmed = await ResultDialogHelper.ShowConfirmAsync(
                XamlRoot,
                string.Format(msgFormat, app.DisplayName),
                string.Format(titleFormat, app.DisplayName),
                "Uninstall Now".T(),
                "Keep Application".T());

            if (confirmed)
            {
                await ViewModel.UninstallAppAsync(app);
            }
        }
    }

    private void OnCancelLeftoversClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelLeftovers();
    }

    private async void OnDeleteLeftoversClick(object sender, RoutedEventArgs e)
    {
        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            "Wipe Leftovers Confirm Message".T(),
            "Wipe Remnant Leftovers".T(),
            "Wipe Leftovers Now".T(),
            "Keep Leftovers".T());

        if (!confirmed) return;

        var btn = WipeLeftoversBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, WipeLeftoversRing, WipeLeftoversText, null,
            "Wiping Leftovers...".T(), "Wipe Leftovers".T(),
            async () =>
            {
                await ViewModel.DeleteLeftoversAsync();
            },
            minDurationMs: 1200);
    }

    // Detail Panel Actions
    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSelectedAppFolder();
    }

    private void OnOpenRegistryClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSelectedAppRegistry();
    }

    private void OnSearchOnlineClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SearchSelectedAppOnline();
    }

    private async void OnDetailsUninstallClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedApp != null)
        {
            var app = ViewModel.SelectedApp;
            var msgFormat = "Uninstall Single Confirm Message".T();
            var titleFormat = "Uninstall {0}".T();
            var confirmed = await ResultDialogHelper.ShowConfirmAsync(
                XamlRoot,
                string.Format(msgFormat, app.DisplayName),
                string.Format(titleFormat, app.DisplayName),
                "Uninstall Now".T(),
                "Keep Application".T());

            if (confirmed)
            {
                await ViewModel.UninstallAppAsync(app);
            }
        }
    }

    private async void OnDetailsForceUninstallClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedApp != null)
        {
            var app = ViewModel.SelectedApp;
            var msgFormat = "Force Remove Confirm Message".T();
            var titleFormat = "Force Remove {0}".T();
            var confirmed = await ResultDialogHelper.ShowConfirmAsync(
                XamlRoot,
                string.Format(msgFormat, app.DisplayName),
                string.Format(titleFormat, app.DisplayName),
                "Force Remove Now".T(),
                "Cancel".T());

            if (!confirmed) return;

            // Clear other selections, check only the selected one, and force uninstall
            foreach (var a in ViewModel.FilteredApps)
            {
                a.IsSelected = false;
            }
            app.IsSelected = true;
            await ViewModel.UninstallSelectedAppsAsync(forceUninstall: true);
        }
    }

    private async void OnBatchUninstallClick(object sender, RoutedEventArgs e)
    {
        var count = ViewModel.SelectedAppsCount;
        if (count == 0) return;

        var msgFormat = "Batch Uninstall Confirm Message".T();
        var titleFormat = "Batch Uninstall ({0} Apps)".T();
        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            string.Format(msgFormat, count),
            string.Format(titleFormat, count),
            "Uninstall Selected".T(),
            "Cancel".T());

        if (confirmed)
        {
            await ViewModel.UninstallSelectedAppsAsync(forceUninstall: false);
            if (sender is Button btn) WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btn, 1.10f, 400);
        }
    }

    private async void OnBatchForceUninstallClick(object sender, RoutedEventArgs e)
    {
        var count = ViewModel.SelectedAppsCount;
        if (count == 0) return;

        var msgFormat = "Batch Force Remove Confirm Message".T();
        var titleFormat = "Force Remove ({0} Apps)".T();
        var confirmed = await ResultDialogHelper.ShowConfirmAsync(
            XamlRoot,
            string.Format(msgFormat, count),
            string.Format(titleFormat, count),
            "Force Remove All Selected".T(),
            "Cancel".T());

        if (confirmed)
        {
            await ViewModel.UninstallSelectedAppsAsync(forceUninstall: true);
        }
    }

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetFilters();
        AppSearch?.Focus(FocusState.Programmatic);
    }

    private void OnExportAppsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string csv = ViewModel.ExportAppsListToCsv();
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(csv);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            var notif = App.Services.GetService<INotificationService>();
            string msg = string.Format("Exported {0} applications to CSV report.".T(), ViewModel.TotalAppsCount);
            notif?.ShowSuccess(msg);
            ViewModel.ProgressMessage = msg;
            if (sender is Button btn) WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btn, 1.10f, 400);
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("UninstallPage.ExportApps", ex);
        }
    }

    private void OnCopyAppDetailsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string details = ViewModel.GetSelectedAppDetailsText();
            if (string.IsNullOrEmpty(details)) return;
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(details);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            var notif = App.Services.GetService<INotificationService>();
            notif?.ShowInfo("App details copied to clipboard.".T());
            ViewModel.ProgressMessage = "App details copied to clipboard.".T();
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("UninstallPage.CopyDetails", ex);
        }
    }

    private void OnInvertSelectionClick(object sender, RoutedEventArgs e)
    {
        ViewModel.InvertSelection();
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAllApps();
    }

    private void OnRefreshAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!ViewModel.IsBusy)
        {
            OnReloadAppsClick(ReloadBtn, null!);
        }
    }

    private void OnFindAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        AppSearch?.Focus(FocusState.Programmatic);
        AppSearch?.SelectAll();
    }

    private void OnContextMenuUninstallClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            OnSingleUninstallClick(new Button { DataContext = app }, e);
        }
    }

    private void OnContextMenuForceWipeClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            ViewModel.SelectedApp = app;
            OnDetailsForceUninstallClick(sender, e);
        }
    }

    private void OnContextMenuOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            ViewModel.SelectedApp = app;
            ViewModel.OpenSelectedAppFolder();
        }
    }

    private void OnContextMenuOpenRegistryClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            ViewModel.SelectedApp = app;
            ViewModel.OpenSelectedAppRegistry();
        }
    }

    private void OnContextMenuSearchOnlineClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            ViewModel.SelectedApp = app;
            ViewModel.SearchSelectedAppOnline();
        }
    }

    private void OnContextMenuCopyDetailsClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is InstalledAppInfo app)
        {
            ViewModel.SelectedApp = app;
            OnCopyAppDetailsClick(sender, e);
        }
    }

    private void OnFilterPillClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int filterIdx))
        {
            ViewModel.SelectedFilterIndex = filterIdx;
        }
    }

    private void OnCleanBloatwareClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllBloatware();
    }

    // UI Helpers
    public bool IsFilterActive(int currentFilter, int target) => currentFilter == target;

    public Visibility GetBloatwareNoticeVisibility(bool detected) => detected ? Visibility.Visible : Visibility.Collapsed;

    public bool IsStep0Active(int step) => step == 0;
    
    public Visibility GetOverlayVisibility(int step) => step != 0 ? Visibility.Visible : Visibility.Collapsed;

    public bool IsNot(bool val) => !val;

    public Visibility GetListViewVisibility(bool isBusy)
    {
        return isBusy ? Visibility.Collapsed : Visibility.Visible;
    }

    public Visibility GetSkeletonVisibility(bool isBusy)
    {
        return isBusy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSkeletonShimmerState()
    {
        if (SkeletonLoadingPanel == null) return;
        try
        {
            if (ViewModel.IsBusy)
            {
                FluidAnimationHelper.ApplyShimmerEffect(SkeletonLoadingPanel);
            }
            else
            {
                FluidAnimationHelper.StopShimmerEffect(SkeletonLoadingPanel);
            }
        }
        catch
        {
            // Gracefully ignore if composition compositor is unavailable
        }
    }

    public Visibility GetDetailsVisibility(InstalledAppInfo? app)
    {
        return app != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetNoDetailsVisibility(InstalledAppInfo? app)
    {
        return app == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetBatchBarVisibility(bool hasSelected)
    {
        return hasSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetStepListVisibility(int step)
    {
        return step == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetStepProgressVisibility(int step)
    {
        return step == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetStepLeftoversVisibility(int step)
    {
        return step == 2 ? Visibility.Visible : Visibility.Collapsed;
    }
}

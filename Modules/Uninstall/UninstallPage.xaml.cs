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
            await ViewModel.UninstallAppAsync(app);
        }
    }

    private void OnCancelLeftoversClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelLeftovers();
    }

    private async void OnDeleteLeftoversClick(object sender, RoutedEventArgs e)
    {
        var btn = WipeLeftoversBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, WipeLeftoversRing, WipeLeftoversText, null,
            "Wiping Leftovers...", "Wipe Leftovers",
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
            await ViewModel.UninstallAppAsync(ViewModel.SelectedApp);
        }
    }

    private async void OnDetailsForceUninstallClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedApp != null)
        {
            // Clear other selections, check only the selected one, and force uninstall
            foreach (var app in ViewModel.FilteredApps)
            {
                app.IsSelected = false;
            }
            ViewModel.SelectedApp.IsSelected = true;
            await ViewModel.UninstallSelectedAppsAsync(forceUninstall: true);
        }
    }

    // Batch Actions
    private async void OnBatchUninstallClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UninstallSelectedAppsAsync(forceUninstall: false);
    }

    private async void OnBatchForceUninstallClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UninstallSelectedAppsAsync(forceUninstall: true);
    }

    // UI Helpers
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

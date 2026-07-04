using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;

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

    private void UpdateDetailsPaneVisibility()
    {
        bool isWide = this.ActualWidth >= 800;
        DetailsPaneVisibility = (isWide && ViewModel.SelectedApp != null) ? Visibility.Visible : Visibility.Collapsed;
        
        if (ListCol != null && DetailCol != null)
        {
            if (isWide)
            {
                ListCol.Width = new GridLength(3, GridUnitType.Star);
                DetailCol.Width = new GridLength(2, GridUnitType.Star);
            }
            else
            {
                ListCol.Width = new GridLength(1, GridUnitType.Star);
                DetailCol.Width = new GridLength(0, GridUnitType.Pixel);
            }
        }
    }

    public UninstallPage()
    {
        ViewModel = App.Services.GetRequiredService<UninstallViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.SelectedApp))
            {
                UpdateDetailsPaneVisibility();
            }
        };

        this.SizeChanged += (s, e) =>
        {
            bool isWide = e.NewSize.Width >= 800;
            BadgesColumnWidth = isWide ? GridLength.Auto : new GridLength(0);
            SizeColumnWidth = isWide ? new GridLength(100) : new GridLength(0);
            WideLayoutVisibility = isWide ? Visibility.Visible : Visibility.Collapsed;
            UpdateDetailsPaneVisibility();

            // Adjust stats grid layout
            if (StatsGrid != null && StatCard1 != null && StatCard2 != null && StatCard3 != null && StatCard4 != null)
            {
                if (isWide)
                {
                    StatsCol2.Width = new GridLength(1, GridUnitType.Star);
                    StatsCol3.Width = new GridLength(1, GridUnitType.Star);
                    Grid.SetColumn(StatCard1, 0); Grid.SetRow(StatCard1, 0);
                    Grid.SetColumn(StatCard2, 1); Grid.SetRow(StatCard2, 0);
                    Grid.SetColumn(StatCard3, 2); Grid.SetRow(StatCard3, 0);
                    Grid.SetColumn(StatCard4, 3); Grid.SetRow(StatCard4, 0);
                }
                else
                {
                    StatsCol2.Width = new GridLength(0, GridUnitType.Pixel);
                    StatsCol3.Width = new GridLength(0, GridUnitType.Pixel);
                    Grid.SetColumn(StatCard1, 0); Grid.SetRow(StatCard1, 0);
                    Grid.SetColumn(StatCard2, 1); Grid.SetRow(StatCard2, 0);
                    Grid.SetColumn(StatCard3, 0); Grid.SetRow(StatCard3, 1);
                    Grid.SetColumn(StatCard4, 1); Grid.SetRow(StatCard4, 1);
                }
            }
        };
    }

    private async void OnReloadAppsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAppsAsync();
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
        await ViewModel.DeleteLeftoversAsync();
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
    internal bool IsNot(bool val) => !val;

    internal Visibility GetListViewVisibility(bool isBusy)
    {
        return isBusy ? Visibility.Collapsed : Visibility.Visible;
    }

    internal Visibility GetSkeletonVisibility(bool isBusy)
    {
        return isBusy ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetDetailsVisibility(InstalledAppInfo? app)
    {
        return app != null ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetNoDetailsVisibility(InstalledAppInfo? app)
    {
        return app == null ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetBatchBarVisibility(bool hasSelected)
    {
        return hasSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetStepListVisibility(int step)
    {
        return step == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetStepProgressVisibility(int step)
    {
        return step == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetStepLeftoversVisibility(int step)
    {
        return step == 2 ? Visibility.Visible : Visibility.Collapsed;
    }
}

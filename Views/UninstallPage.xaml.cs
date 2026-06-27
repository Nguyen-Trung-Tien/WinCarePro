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

    public UninstallPage()
    {
        ViewModel = App.Services.GetRequiredService<UninstallViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;
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

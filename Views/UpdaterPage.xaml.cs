using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Models;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class UpdaterPage : Page
{
    public UpdaterViewModel ViewModel { get; }

    public UpdaterPage()
    {
        InitializeComponent();
        ViewModel = new UpdaterViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanUpdatesAsync();
    }

    private async void OnUpdateSelectedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateSelectedAppsAsync();
    }

    private async void OnUpdateAllClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateAllAppsAsync();
    }

    private async void OnUpdateSingleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SoftwareUpdateInfo app)
        {
            await ViewModel.UpdateSingleAppAsync(app);
        }
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllApps();
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAllApps();
    }

    internal bool IsNot(bool val) => !val;

    internal bool CanUpdate(bool isUpdating, int count)
    {
        return !isUpdating && count > 0;
    }

    internal static string GetStatusIcon(string status)
    {
        return status switch
        {
            "Completed" => "\uE73E", // Check
            "Updating..." => "\uE128", // Sync
            "Failed" => "\uE7A6", // Error
            _ => "\uE7BA" // Warning/Info
        };
    }

    internal static Brush GetStatusColor(string status)
    {
        var color = status switch
        {
            "Completed" => Colors.LightGreen,
            "Updating..." => Colors.DeepSkyBlue,
            "Failed" => Colors.Red,
            _ => Colors.Orange
        };
        return new SolidColorBrush(color);
    }
}

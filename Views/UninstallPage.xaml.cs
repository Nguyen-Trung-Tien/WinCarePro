using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Models;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class UninstallPage : Page
{
    public UninstallViewModel ViewModel { get; }

    public UninstallPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = new UninstallViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAppsAsync();
    }

    private async void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InstalledAppInfo app)
        {
            await ViewModel.UninstallAppAsync(app);
        }
    }

    private async void OnUninstallSelectedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UninstallSelectedAppsAsync();
    }

    private void OnSelectAllThirdPartyApps(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllApps(true, false);
    }

    private void OnDeselectAllThirdPartyApps(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllApps(false, false);
    }

    private void OnSelectAllSystemApps(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllApps(true, true);
    }

    private void OnDeselectAllSystemApps(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllApps(false, true);
    }

    private async void OnDeleteLeftoversClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteLeftoversAsync();
    }

    private void OnCancelLeftoversClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelLeftovers();
    }

    private void OnSelectAllLeftovers(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllLeftovers(true);
    }

    private void OnDeselectAllLeftovers(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllLeftovers(false);
    }

    internal bool IsNot(bool val) => !val;

    public static Visibility StepToVisibility(int currentStep, int targetStep)
    {
        return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
    }

    internal static string GetLeftoverIcon(LeftoverType type)
    {
        return type switch
        {
            LeftoverType.Directory => "\uE8B7", // Folder icon
            LeftoverType.File => "\uE7C3", // File icon
            LeftoverType.RegistryKey => "\uE945", // Registry/Tuning icon
            LeftoverType.RegistryValue => "\uE946", // Registry value icon
            _ => "\uE8B7"
        };
    }

    internal static Microsoft.UI.Xaml.Media.Brush GetLeftoverColor(LeftoverType type)
    {
        var color = type switch
        {
            LeftoverType.Directory => Windows.UI.Color.FromArgb(255, 230, 126, 34), // Orange/Yellow
            LeftoverType.File => Windows.UI.Color.FromArgb(255, 52, 152, 219), // Blue
            LeftoverType.RegistryKey => Windows.UI.Color.FromArgb(255, 155, 89, 182), // Purple
            LeftoverType.RegistryValue => Windows.UI.Color.FromArgb(255, 142, 68, 173), // Violet
            _ => Microsoft.UI.Colors.DodgerBlue
        };
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
    }
}

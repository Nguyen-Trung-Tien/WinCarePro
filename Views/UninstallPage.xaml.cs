using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    private async void OnCleanLeftoversClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteLeftoversAsync();
    }

    private void OnCancelLeftoversClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelLeftovers();
    }

    private async void OnDeleteLeftoversClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteLeftoversAsync();
    }

    internal bool IsNot(bool val) => !val;

    internal Visibility GetLeftoversVisibility(int step)
    {
        return step == 2 ? Visibility.Visible : Visibility.Collapsed;
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

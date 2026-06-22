using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;

namespace WinCarePro.Views;

public sealed partial class StartupPage : Page
{
    public StartupViewModel ViewModel { get; }

    public StartupPage()
    {
        ViewModel = App.Services.GetRequiredService<StartupViewModel>();
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    private async void OnReloadStartupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAllDataAsync();
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading) return;

        if (sender is ToggleSwitch ts && ts.DataContext is StartupEntry entry)
        {
            if (entry.IsEnabled != ts.IsOn)
            {
                await ViewModel.ToggleStartupAppAsync(entry, ts.IsOn);
            }
        }
    }

    private async void OnServiceControlClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ServiceEntry entry && btn.Tag is string action)
        {
            await ViewModel.ControlServiceAsync(entry, action);
        }
    }

    internal bool IsNot(bool val) => !val;
}

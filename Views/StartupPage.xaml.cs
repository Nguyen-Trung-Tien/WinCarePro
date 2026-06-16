using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Models;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class StartupPage : Page
{
    public StartupViewModel ViewModel { get; }

    public StartupPage()
    {
        InitializeComponent();
        ViewModel = new StartupViewModel();
        DataContext = ViewModel;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAllDataAsync();
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ts.DataContext is StartupEntry entry)
        {
            await ViewModel.ToggleStartupAppAsync(entry, ts.IsOn);
        }
    }

    private async void OnDeleteStartupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StartupEntry entry)
        {
            await ViewModel.RemoveStartupAppAsync(entry);
        }
    }

    private async void OnTaskToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ts.DataContext is ScheduledTaskEntry entry)
        {
            await ViewModel.ToggleTaskAsync(entry, ts.IsOn);
        }
    }

    private async void OnDeleteTaskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ScheduledTaskEntry entry)
        {
            await ViewModel.DeleteTaskAsync(entry);
        }
    }

    private async void OnServiceStartupChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.DataContext is ServiceEntry entry && cb.SelectedItem is ComboBoxItem item)
        {
            string mode = item.Content.ToString() switch
            {
                "Automatic" => "auto",
                "Manual" => "manual",
                "Disabled" => "disabled",
                _ => "manual"
            };
            // Prevent recursive loops by calling engine only if changed
            if (entry.StartupType != item.Content.ToString())
            {
                await ViewModel.ToggleServiceAsync(entry, mode);
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

    internal static int GetStartupIndex(string startupType)
    {
        return startupType switch
        {
            "Automatic" => 0,
            "Manual" => 1,
            "Disabled" => 2,
            _ => 1
        };
    }

    internal static Brush GetServiceColor(string status)
    {
        var color = status == "Running" ? Colors.LightGreen : Colors.Gray;
        return new SolidColorBrush(color);
    }

    internal static bool CanStartService(string status)
    {
        return status != "Running";
    }

    internal static bool CanStopService(string status, bool canStop)
    {
        return status == "Running" && canStop;
    }
}

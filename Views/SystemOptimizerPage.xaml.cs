using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class SystemOptimizerPage : Page
{
    public SystemOptimizerViewModel ViewModel { get; }

    public SystemOptimizerPage()
    {
        InitializeComponent();
        ViewModel = new SystemOptimizerViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnScanTweaksClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplySelectedTweaksAsync();
    }

    private async void OnRevertTweaksClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RevertSelectedTweaksAsync();
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BoostRamAsync();
    }

    private async void OnCleanDoCacheClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanDoCacheAsync();
    }

    internal bool IsNot(bool val) => !val;
    internal string FormatPercent(double val) => $"{val:F1}%";

    public static string GetTweakStatusIcon(bool optimized)
    {
        return optimized ? "\uE73E" : "\uE7BA"; // Checkmark or Warning glyph
    }

    public static string GetTweakStatusLabel(bool optimized)
    {
        return optimized ? "Optimized" : "Needs Tuning";
    }

    public static Brush GetTweakStatusColor(bool optimized)
    {
        var color = optimized ? Colors.LightGreen : Colors.Orange;
        return new SolidColorBrush(color);
    }
}

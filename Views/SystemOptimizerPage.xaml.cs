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
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
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
        var color = optimized ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 245, 158, 11);
        return new SolidColorBrush(color);
    }

    public static Brush GetTweakStatusBadgeBg(bool optimized)
    {
        var color = optimized ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 245, 158, 11);
        return new SolidColorBrush(color);
    }
}

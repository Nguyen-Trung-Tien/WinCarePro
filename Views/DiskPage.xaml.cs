using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class DiskPage : Page
{
    public DiskToolsViewModel ViewModel { get; }

    public DiskPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = new DiskToolsViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnRefreshDrivesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDrivesAsync();
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.AnalyzeStorageAsync();
    }

    private async void OnFindDuplicatesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.FindDuplicatesAsync();
    }

    private async void OnCleanEmptyClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanEmptyFoldersAsync();
    }

    private async void OnRunChkdskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string drive)
        {
            await ViewModel.RunChkdskAsync(drive);
        }
    }

    internal bool IsNot(bool val) => !val;

    internal static Brush GetHealthColor(string status)
    {
        var color = status == "Healthy" ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 245, 158, 11);
        return new SolidColorBrush(color);
    }

    internal static Brush GetHealthBadgeBg(string status)
    {
        var color = status == "Healthy" ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 245, 158, 11);
        return new SolidColorBrush(color);
    }

    internal static Brush GetTempColor(double temp)
    {
        var color = temp > 45.0 ? Windows.UI.Color.FromArgb(255, 245, 158, 11) : Windows.UI.Color.FromArgb(255, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    internal static Brush GetTempBadgeBg(double temp)
    {
        var color = temp > 45.0 ? Windows.UI.Color.FromArgb(30, 245, 158, 11) : Windows.UI.Color.FromArgb(30, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    internal static string GetTypeIcon(bool isDirectory)
    {
        return isDirectory ? "\uE8B7" : "\uE7C3"; // Folder or File glyph
    }

    internal static string FormatTemp(double temp) => $"{temp:F0}°C";
    internal static string FormatDuplicateGroupSize(string size) => $"Duplicate Group - Size: {size}";
}

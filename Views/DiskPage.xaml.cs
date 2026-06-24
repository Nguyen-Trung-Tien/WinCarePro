using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class DiskPage : Page
{
    public DiskViewModel ViewModel { get; }

    public DiskPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<DiskViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SubscribeEvents();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.UnsubscribeEvents();
    }

    private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.AnalyzeStorageAsync();
    }

    private async void OnScanDuplicatesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.FindDuplicatesAsync();
    }

    private async void OnDeleteDuplicatesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanSelectedDuplicatesAsync();
    }

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Optional tracking or cleanup
    }

    private async void StorageItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is WinCarePro.Engines.StorageItem item)
        {
            if (item.IsDirectory)
            {
                ViewModel.StorageScanPath = item.Path;
                await ViewModel.AnalyzeStorageAsync();
            }
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

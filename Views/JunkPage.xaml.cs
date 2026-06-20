using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class JunkPage : Page
{
    public JunkCleanerViewModel ViewModel { get; }

    public JunkPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = new JunkCleanerViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private async void OnCleanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanAsync();
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTotalSize();
    }

    internal bool IsNot(bool val) => !val;
    
    internal static string FormatFilesCount(int count) => $"{count} files";
    
    internal bool CanClean(bool isCleaning, int categoriesCount)
    {
        return !isCleaning && categoriesCount > 0;
    }

    internal static string GetCategoryIcon(string name)
    {
        return name.ToLower() switch
        {
            var n when n.Contains("user temporary") => "\uE708", // Temporary files/brush
            var n when n.Contains("system temporary") => "\uF158", // System file
            var n when n.Contains("update cache") => "\uE895", // Sync/download
            var n when n.Contains("recycle bin") => "\uE74D", // Recycle Bin/Trash
            var n when n.Contains("web browser") => "\uE774", // Web/Globe
            var n when n.Contains("directx") => "\uE9F5", // Graphic card
            var n when n.Contains("system log") => "\uE7C1", // Document/logs
            _ => "\uEA99" // Default brush
        };
    }

    internal static Microsoft.UI.Xaml.Media.Brush GetCategoryIconColor(string name)
    {
        var color = name.ToLower() switch
        {
            var n when n.Contains("user temporary") => Windows.UI.Color.FromArgb(255, 230, 126, 34), // Orange
            var n when n.Contains("system temporary") => Windows.UI.Color.FromArgb(255, 142, 68, 173), // Purple
            var n when n.Contains("update cache") => Windows.UI.Color.FromArgb(255, 46, 204, 113), // Green
            var n when n.Contains("recycle bin") => Windows.UI.Color.FromArgb(255, 231, 76, 60), // Red
            var n when n.Contains("web browser") => Windows.UI.Color.FromArgb(255, 52, 152, 219), // Blue
            var n when n.Contains("directx") => Windows.UI.Color.FromArgb(255, 241, 196, 15), // Yellow
            var n when n.Contains("system log") => Windows.UI.Color.FromArgb(255, 127, 140, 141), // Slate
            _ => Microsoft.UI.Colors.DodgerBlue
        };
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
    }
}

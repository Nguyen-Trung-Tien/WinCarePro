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
}

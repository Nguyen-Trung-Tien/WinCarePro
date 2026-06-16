using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class SecurityPrivacyPage : Page
{
    public SecurityPrivacyViewModel ViewModel { get; }

    public SecurityPrivacyPage()
    {
        InitializeComponent();
        ViewModel = new SecurityPrivacyViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadSecurityAndPrivacyStateAsync();
    }

    private void OnClearClipboardClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearClipboard();
    }

    private void OnClearRecentClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearRecentFiles();
    }

    internal bool IsNot(bool val) => !val;

    internal Visibility GetVisibility(int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}

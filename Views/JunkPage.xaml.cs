using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class JunkPage : Page
{
    public JunkViewModel ViewModel { get; }

    public JunkPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<JunkViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private async void OnScanJunkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private async void OnCleanJunkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanAsync();
    }

    private void OnJunkSelectionChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTotalSize();
    }

    internal bool IsNot(bool val) => !val;

    internal bool CanClean(bool isCleaning, int count)
    {
        return !isCleaning && count > 0;
    }

    internal bool GetProgressRingActive(bool scanning, bool cleaning)
    {
        return scanning || cleaning;
    }
}

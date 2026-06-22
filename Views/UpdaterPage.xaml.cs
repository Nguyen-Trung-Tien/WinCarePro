using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class UpdaterPage : Page
{
    public UpdaterViewModel ViewModel { get; }

    public UpdaterPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<UpdaterViewModel>();
        this.DataContext = ViewModel;
    }

    private async void OnScanUpdatesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanUpdatesAsync();
    }

    private async void OnUpdateAllClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateAllAppsAsync();
    }

    internal bool IsNot(bool val) => !val;
}

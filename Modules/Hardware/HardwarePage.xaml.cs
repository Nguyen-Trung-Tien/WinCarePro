using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class HardwarePage : Page
{
    public HardwareViewModel ViewModel { get; }

    public HardwarePage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<HardwareViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.StartMonitoring();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }
}

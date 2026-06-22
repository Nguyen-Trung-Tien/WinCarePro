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
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<HardwareViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }
}

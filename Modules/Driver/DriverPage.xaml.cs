using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using Windows.UI;
using Windows.UI.Text;

namespace WinCarePro.Views;

public sealed partial class DriverPage : Page
{
    public DriverViewModel ViewModel { get; }

    public DriverPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DriverViewModel>();
        this.DataContext = ViewModel;
    }

    private async void OnScanDriversClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanDriversAsync();
    }

    private async void OnRunDriverWizardClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartDriverUpdateWizardAsync();
    }

    internal bool IsNot(bool val) => !val;

    // Driver Wizard Stepper styling
    internal Brush GetStepBg(int currentStep, int stepNum)
    {
        if (currentStep >= stepNum)
        {
            return new SolidColorBrush(Color.FromArgb(255, 127, 86, 217)); // Brand purple
        }
        return new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)); // Muted slate gray
    }

    internal FontWeight GetStepWeight(int currentStep, int stepNum)
    {
        return currentStep == stepNum ? FontWeights.Bold : FontWeights.Normal;
    }
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; }

    public NetworkPage()
    {
        InitializeComponent();
        ViewModel = new NetworkViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsAsync();
    }

    private async void OnPingClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunPingTestAsync();
    }

    private async void OnTraceClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunTracerouteAsync();
    }

    private async void OnDnsLookupClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDnsLookupAsync();
    }

    private async void OnScanPortsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunPortScanAsync();
    }

    private async void OnSpeedTestClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunSpeedTestAsync();
    }

    private async void OnRepairClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string op)
        {
            await ViewModel.RunRepairOperationAsync(op);
        }
    }

    internal bool IsNot(bool val) => !val;

    internal string FormatMs(double val) => $"{val:F0} ms";
    internal string FormatPercent(double val) => $"{val:F1}%";
    internal string FormatMbps(double val) => $"{val:F1} Mbps";
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; }

    public NetworkPage()
    {
        ViewModel = App.Services?.GetService<NetworkViewModel>() ?? new NetworkViewModel();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.Loaded += (s, e) => DataContext = ViewModel;
        this.Unloaded += (s, e) => ViewModel.Cleanup();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
        this.Bindings.Update();
        SetActiveTab("quality");
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tabName)
        {
            SetActiveTab(tabName);
        }
    }

    private void SetActiveTab(string tabName)
    {
        // Toggle tab button active styles
        BtnTabQuality.Style = tabName == "quality" ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["DefaultButtonStyle"];
        BtnTabDns.Style = tabName == "dns" ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["DefaultButtonStyle"];
        BtnTabPorts.Style = tabName == "ports" ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["DefaultButtonStyle"];
        BtnTabRepairs.Style = tabName == "repairs" ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["DefaultButtonStyle"];

        // Toggle content section visibility
        SectionQuality.Visibility = tabName == "quality" ? Visibility.Visible : Visibility.Collapsed;
        SectionDns.Visibility = tabName == "dns" ? Visibility.Visible : Visibility.Collapsed;
        SectionPorts.Visibility = tabName == "ports" ? Visibility.Visible : Visibility.Collapsed;
        SectionRepairs.Visibility = tabName == "repairs" ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsAsync();
        ViewModel.LoadAdapters();
        await ViewModel.LoadActiveConnectionsAsync();
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

    private async void OnDnsBenchmarkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartDnsBenchmarkAsync();
    }

    private async void OnApplyDnsClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DnsServerInfo dns)
        {
            await ViewModel.ApplyDnsAsync(dns);
        }
    }

    private async void OnRefreshConnectionsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadActiveConnectionsAsync();
    }

    internal bool IsNot(bool val) => !val;

    internal string FormatMs(double val) => $"{val:F0} ms";
    internal string FormatPercent(double val) => $"{val:F1}%";
    internal string FormatMbps(double val) => $"{val:F1} Mbps";

    internal string GetEstablishedCount(System.Collections.ObjectModel.ObservableCollection<ActiveConnectionInfo> connections)
    {
        if (connections == null) return "0";
        return connections.Count(c => c.State != null && c.State.ToUpper() == "ESTABLISHED").ToString();
    }

    internal string GetListeningCount(System.Collections.ObjectModel.ObservableCollection<ActiveConnectionInfo> connections)
    {
        if (connections == null) return "0";
        return connections.Count(c => c.State != null && (c.State.ToUpper() == "LISTENING" || c.State.ToUpper() == "LISTEN")).ToString();
    }
}

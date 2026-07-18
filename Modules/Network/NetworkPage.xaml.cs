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

    public static readonly DependencyProperty ShowTerminalProperty =
        DependencyProperty.Register(
            nameof(ShowTerminal),
            typeof(bool),
            typeof(NetworkPage),
            new PropertyMetadata(false, OnShowTerminalChanged));

    private static void OnShowTerminalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NetworkPage page)
        {
            page.UpdateTerminalLayout();
        }
    }

    public bool ShowTerminal
    {
        get => (bool)GetValue(ShowTerminalProperty);
        set => SetValue(ShowTerminalProperty, value);
    }

    public NetworkPage()
    {
        ViewModel = App.Services?.GetService<NetworkViewModel>() ?? new NetworkViewModel();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        this.Loaded += (s, e) => 
        {
            DataContext = ViewModel;
            UpdateTerminalLayout();
        };

        this.SizeChanged += (s, e) => UpdateTerminalLayout();
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

    private Style? GetButtonStyle(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var styleObj) && styleObj is Style style)
        {
            return style;
        }
        return null;
    }

    private void SetActiveTab(string tabName)
    {
        ViewModel.ActiveTab = tabName;

        var accentStyle = GetButtonStyle("AccentButtonStyle");
        var defaultStyle = GetButtonStyle("DefaultButtonStyle");

        // Toggle tab button active styles
        BtnTabQuality.Style = tabName == "quality" ? accentStyle : defaultStyle;
        BtnTabDns.Style = tabName == "dns" ? accentStyle : defaultStyle;
        BtnTabPorts.Style = tabName == "ports" ? accentStyle : defaultStyle;
        BtnTabRepairs.Style = tabName == "repairs" ? accentStyle : defaultStyle;

        // Toggle content section visibility
        SectionQuality.Visibility = tabName == "quality" ? Visibility.Visible : Visibility.Collapsed;
        SectionDns.Visibility = tabName == "dns" ? Visibility.Visible : Visibility.Collapsed;
        SectionPorts.Visibility = tabName == "ports" ? Visibility.Visible : Visibility.Collapsed;
        SectionRepairs.Visibility = tabName == "repairs" ? Visibility.Visible : Visibility.Collapsed;

        // Immediate load when clicking ports tab
        if (tabName == "ports")
        {
            _ = ViewModel.LoadActiveConnectionsAsync();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsAsync();
        ViewModel.LoadAdapters();
        await ViewModel.LoadActiveConnectionsAsync();
    }

    private async void OnPingClick(object sender, RoutedEventArgs e)
    {
        ShowTerminal = true;
        await ViewModel.RunPingTestAsync();
    }

    private async void OnTraceClick(object sender, RoutedEventArgs e)
    {
        ShowTerminal = true;
        await ViewModel.RunTracerouteAsync();
    }

    private async void OnDnsLookupClick(object sender, RoutedEventArgs e)
    {
        ShowTerminal = true;
        await ViewModel.RunDnsLookupAsync();
    }

    private async void OnScanPortsClick(object sender, RoutedEventArgs e)
    {
        ShowTerminal = true;
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
            ShowTerminal = true;
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

    private async void OnApplyDohClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyDohSettingsAsync();
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

    private void UpdateTerminalLayout()
    {
        if (RepTerminalBorder == null || SecRepCol0 == null || SecRepCol1 == null || RepToolkitBorder == null)
            return;

        bool isNarrow = this.ActualWidth < 850;

        if (ShowTerminal)
        {
            RepTerminalBorder.Visibility = Visibility.Visible;
            
            if (isNarrow)
            {
                SecRepCol0.Width = new GridLength(1, GridUnitType.Star);
                SecRepCol1.Width = new GridLength(0);
                
                Grid.SetColumn(RepTerminalBorder, 0);
                Grid.SetColumnSpan(RepTerminalBorder, 2);
                Grid.SetRow(RepTerminalBorder, 0);
                
                Grid.SetColumn(RepToolkitBorder, 0);
                Grid.SetColumnSpan(RepToolkitBorder, 2);
                Grid.SetRow(RepToolkitBorder, 1);
                RepToolkitBorder.Margin = new Thickness(0, 16, 0, 0);
            }
            else
            {
                SecRepCol0.Width = new GridLength(1.2, GridUnitType.Star);
                SecRepCol1.Width = new GridLength(0.8, GridUnitType.Star);
                
                Grid.SetColumn(RepTerminalBorder, 0);
                Grid.SetColumnSpan(RepTerminalBorder, 1);
                Grid.SetRow(RepTerminalBorder, 0);
                
                Grid.SetColumn(RepToolkitBorder, 1);
                Grid.SetColumnSpan(RepToolkitBorder, 1);
                Grid.SetRow(RepToolkitBorder, 0);
                RepToolkitBorder.Margin = new Thickness(0);
            }
        }
        else
        {
            RepTerminalBorder.Visibility = Visibility.Collapsed;
            
            if (isNarrow)
            {
                SecRepCol0.Width = new GridLength(1, GridUnitType.Star);
                SecRepCol1.Width = new GridLength(0);
                
                Grid.SetColumn(RepToolkitBorder, 0);
                Grid.SetColumnSpan(RepToolkitBorder, 2);
                Grid.SetRow(RepToolkitBorder, 0);
                RepToolkitBorder.Margin = new Thickness(0);
            }
            else
            {
                SecRepCol0.Width = new GridLength(0);
                SecRepCol1.Width = new GridLength(1, GridUnitType.Star);
                
                Grid.SetColumn(RepToolkitBorder, 0);
                Grid.SetColumnSpan(RepToolkitBorder, 2);
                Grid.SetRow(RepToolkitBorder, 0);
                RepToolkitBorder.Margin = new Thickness(0);
            }
        }
    }
}

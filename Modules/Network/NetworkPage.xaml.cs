using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.DataTransfer;

namespace WinCarePro.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; }
    public string[] DohProviders { get; } = new[] { "Cloudflare", "Google", "AdGuard", "NextDNS" };

    public NetworkPage()
    {
        ViewModel = App.Services?.GetService<NetworkViewModel>() ?? new NetworkViewModel();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        this.Loaded += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            SetActiveTab(ViewModel.ActiveTab ?? "quality");
            UpdateFilterCategoryButtons(ViewModel.ConnectionFilterCategory);
            InitDohComboBox();
        };

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.ConsoleOutput))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        TerminalScrollViewer?.ChangeView(null, TerminalScrollViewer.ScrollableHeight, null);
                    }
                    catch { }
                });
            }
        };
    }

    private void InitDohComboBox()
    {
        if (CmbDohProvider != null)
        {
            if (CmbDohProvider.ItemsSource == null)
            {
                CmbDohProvider.ItemsSource = DohProviders;
            }
            if (!string.IsNullOrEmpty(ViewModel.SelectedDohProvider))
            {
                CmbDohProvider.SelectedItem = ViewModel.SelectedDohProvider;
            }
            if (CmbDohProvider.SelectedIndex < 0)
            {
                CmbDohProvider.SelectedIndex = 0;
            }
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
        this.Bindings.Update();
        SetActiveTab(ViewModel.ActiveTab ?? "quality");
        UpdateFilterCategoryButtons(ViewModel.ConnectionFilterCategory);
        InitDohComboBox();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    public void OnTabQualityClick(object sender, RoutedEventArgs e) => SetActiveTab("quality");
    public void OnTabDnsClick(object sender, RoutedEventArgs e) => SetActiveTab("dns");
    public void OnTabPortsClick(object sender, RoutedEventArgs e) => SetActiveTab("ports");
    public void OnTabRepairsClick(object sender, RoutedEventArgs e) => SetActiveTab("repairs");

    private Style? _accentStyle;
    private Style? _defaultStyle;

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

        _accentStyle ??= GetButtonStyle("VibrantPrimaryButtonStyle") ?? GetButtonStyle("AccentButtonStyle");
        _defaultStyle ??= GetButtonStyle("DefaultButtonStyle");

        if (BtnTabQuality != null) BtnTabQuality.Style = tabName == "quality" ? _accentStyle : _defaultStyle;
        if (BtnTabDns != null) BtnTabDns.Style = tabName == "dns" ? _accentStyle : _defaultStyle;
        if (BtnTabPorts != null) BtnTabPorts.Style = tabName == "ports" ? _accentStyle : _defaultStyle;
        if (BtnTabRepairs != null) BtnTabRepairs.Style = tabName == "repairs" ? _accentStyle : _defaultStyle;

        if (SectionQuality != null) SectionQuality.Visibility = tabName == "quality" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionDns != null) SectionDns.Visibility = tabName == "dns" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionPorts != null) SectionPorts.Visibility = tabName == "ports" ? Visibility.Visible : Visibility.Collapsed;
        if (SectionRepairs != null) SectionRepairs.Visibility = tabName == "repairs" ? Visibility.Visible : Visibility.Collapsed;

        if (tabName == "ports" && ViewModel.Connections.Count == 0)
        {
            _ = Task.Run(async () => await ViewModel.LoadActiveConnectionsAsync());
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsAsync(userTriggered: true);
        ViewModel.LoadAdapters();
        await ViewModel.LoadActiveConnectionsAsync(forceRefresh: true);
    }

    private async void OnSpeedTestClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunSpeedTestAsync();
    }

    private async void OnDnsBenchmarkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartDnsBenchmarkAsync();
    }

    private async void OnRestoreDefaultDnsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RestoreDefaultDnsAsync();
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

    private void OnDohProviderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbDohProvider?.SelectedItem is string provider)
        {
            ViewModel.SelectedDohProvider = provider;
        }
    }

    private async void OnRepairClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string op)
        {
            await ViewModel.RunRepairOperationAsync(op);
        }
    }

    private async void OnRefreshConnectionsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadActiveConnectionsAsync(forceRefresh: true);
    }

    private void OnFilterSocketCategoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string category)
        {
            ViewModel.ConnectionFilterCategory = category;
            UpdateFilterCategoryButtons(category);
        }
    }

    private void UpdateFilterCategoryButtons(string activeCategory)
    {
        _accentStyle ??= GetButtonStyle("VibrantPrimaryButtonStyle") ?? GetButtonStyle("AccentButtonStyle");
        _defaultStyle ??= GetButtonStyle("DefaultButtonStyle");

        if (BtnFilterAll != null) BtnFilterAll.Style = activeCategory.Equals("All", StringComparison.OrdinalIgnoreCase) ? _accentStyle : _defaultStyle;
        if (BtnFilterEstablished != null) BtnFilterEstablished.Style = activeCategory.Equals("Established", StringComparison.OrdinalIgnoreCase) ? _accentStyle : _defaultStyle;
        if (BtnFilterListening != null) BtnFilterListening.Style = activeCategory.Equals("Listening", StringComparison.OrdinalIgnoreCase) ? _accentStyle : _defaultStyle;
        if (BtnFilterTcp != null) BtnFilterTcp.Style = activeCategory.Equals("TCP", StringComparison.OrdinalIgnoreCase) ? _accentStyle : _defaultStyle;
        if (BtnFilterUdp != null) BtnFilterUdp.Style = activeCategory.Equals("UDP", StringComparison.OrdinalIgnoreCase) ? _accentStyle : _defaultStyle;
    }

    private void OnHostKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _ = ViewModel.RunPingTestAsync();
        }
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

    private async void OnPortScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunPortScanAsync();
    }

    private void OnClearConsoleClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ConsoleOutput = "";
    }

    private void OnCopyConsoleClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.ConsoleOutput))
        {
            try
            {
                var package = new DataPackage();
                package.SetText(ViewModel.ConsoleOutput);
                Clipboard.SetContent(package);
            }
            catch { }
        }
    }

    private void OnCopySocketClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ActiveConnectionInfo conn)
        {
            try
            {
                var package = new DataPackage();
                package.SetText($"Process: {conn.ProcessName} (PID: {conn.Pid})\nProtocol: {conn.Protocol}\nLocal: {conn.LocalAddress}\nRemote: {conn.RemoteAddress}\nState: {conn.State}");
                Clipboard.SetContent(package);
            }
            catch { }
        }
    }

    private void OnOpenProcessLocationClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ActiveConnectionInfo conn)
        {
            ViewModel.OpenProcessLocation(conn.Pid, conn.ProcessName);
        }
    }

    private async void OnKillProcessClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ActiveConnectionInfo conn)
        {
            await ViewModel.TerminateProcessAsync(conn.Pid, conn.ProcessName);
        }
    }

    private void OnCopyPublicIpClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.PublicIpAddress))
        {
            try
            {
                var package = new DataPackage();
                package.SetText(ViewModel.PublicIpAddress);
                Clipboard.SetContent(package);
            }
            catch { }
        }
    }

    public bool IsNot(bool val) => !val;

    public string FormatMs(double val) => val >= 1000 ? $"{val / 1000.0:F2} s" : $"{val:F0} ms";
    public string FormatPercent(double val) => $"{val:F1}%";
    public string FormatMbps(double val) => val >= 1000 ? $"{val / 1000.0:F2} Gbps" : $"{val:F1} Mbps";

    public string FormatSpeedValue(double val)
    {
        if (val >= 1000) return (val / 1000.0).ToString("F2");
        return val.ToString("F1");
    }

    public string FormatSpeedUnit(double val)
    {
        if (val >= 1000) return "Gbps";
        return "Mbps";
    }

    public string FormatConnectionCount(int count) => $"{count} Sockets Active";
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.DataTransfer;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; }
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;
    public string[] DohProviders { get; } = new[] { "Cloudflare", "Google", "AdGuard", "NextDNS" };

    public NetworkPage()
    {
        ViewModel = App.Services?.GetService<NetworkViewModel>() ?? new NetworkViewModel();
        InitializeComponent();
        this.NavigationCacheMode = NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        _vmPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.ConsoleOutput))
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        TerminalScrollViewer?.ChangeView(null, TerminalScrollViewer.ScrollableHeight, null);
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogException("NetworkPage.ConsoleOutputScroll", ex);
                    }
                });
            }
        };
        ViewModel.PropertyChanged += _vmPropertyChangedHandler;

        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            ViewModel.Initialize();
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            ViewModel.PropertyChanged += _vmPropertyChangedHandler;

            if (MainPivot != null)
            {
                MainPivot.SelectedIndex = ViewModel.ActiveTab switch
                {
                    "quality" => 0,
                    "dns" => 1,
                    "ports" => 2,
                    "repairs" => 3,
                    _ => 0
                };
            }

            InitDohComboBox();
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
            TranslationManager.Instance.Translate(this);
            this.Bindings.Update();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkPage.OnNavigatedTo", ex);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        try
        {
            ViewModel.Cleanup();
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkPage.OnNavigatedFrom", ex);
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            TranslationManager.Instance.Translate(this);
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
            InitDohComboBox();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkPage.OnPageLoaded", ex);
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkPage.OnPageUnloaded", ex);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                TranslationManager.Instance.Translate(this);
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("NetworkPage.OnLanguageChanged", ex);
            }
        });
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

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Pivot pivot)
        {
            ViewModel.ActiveTab = pivot.SelectedIndex switch
            {
                0 => "quality",
                1 => "dns",
                2 => "ports",
                3 => "repairs",
                _ => "quality"
            };

            if (pivot.SelectedIndex == 2)
            {
                _ = ViewModel.LoadActiveConnectionsAsync(true);
            }
        }
    }

    // ==================== HEADER ACTIONS ====================
    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshActiveAdapter();
        ViewModel.RefreshTelemetryOnDemand();
    }

    private async void OnExportReportClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportNetworkReportAsync();
    }

    // ==================== TAB 1: SPEED TEST ====================
    private async void OnStartSpeedTestClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunSpeedTestAsync();
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

    // ==================== TAB 2: DNS BENCHMARK ====================
    private async void OnStartDnsBenchmarkClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartDnsBenchmarkAsync();
    }

    private async void OnSwitchToFastestDnsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SwitchToFastestDnsAsync();
    }

    private async void OnApplyDnsServerClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is DnsServerInfo server)
        {
            await ViewModel.ApplyDnsAsync(server);
        }
    }

    private async void OnApplyDohClick(object sender, RoutedEventArgs e)
    {
        if (CmbDohProvider?.SelectedItem is string provider)
        {
            ViewModel.SelectedDohProvider = provider;
        }
        await ViewModel.ApplyDohSettingsAsync();
    }

    // ==================== TAB 3: SOCKETS & TRAFFIC ====================
    private void OnFilterSocketCategoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string category)
        {
            ViewModel.ConnectionFilterCategory = category;
        }
    }

    private void OnRefreshSocketsClick(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.LoadActiveConnectionsAsync(true);
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

    // ==================== TAB 4: REPAIRS & DIAGNOSTICS ====================
    private async void OnRepairTileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string operation)
        {
            await ViewModel.RunRepairOperationAsync(operation);
        }
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
            catch (Exception ex)
            {
                CrashLogger.LogException("NetworkPage.OnCopyConsoleClick", ex);
            }
        }
    }

    // ==================== FORMATTING & BINDING HELPERS ====================
    public bool IsNot(bool val) => !val;
    public Visibility EmptyStateVisibility(int count, bool isBusy) =>
        (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;
    public string FormatMs(double val) => val >= 1000 ? $"{val / 1000.0:F2} s" : $"{val:F0} ms";
    public string FormatPercent(double val) => $"{val:F1}%";
    public string FormatMbps(double val) => val >= 1000 ? $"{val / 1000.0:F2} Gbps" : $"{val:F1} Mbps";

    public string FormatSpeedValue(double val)
    {
        if (ViewModel.DisplaySpeedLabel == "PING" || ViewModel.DisplaySpeedLabel == "PING".T())
        {
            return val.ToString("F0");
        }
        if (val >= 1000) return (val / 1000.0).ToString("F2");
        return val.ToString("F1");
    }

    public string FormatSpeedUnit(double val)
    {
        if (ViewModel.DisplaySpeedLabel == "PING" || ViewModel.DisplaySpeedLabel == "PING".T())
        {
            return "ms";
        }
        if (val >= 1000) return "Gbps";
        return "Mbps";
    }

    public string FormatConnectionCount(int count) => $"{count} Sockets";
}

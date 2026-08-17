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

        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        this.ActualThemeChanged += (s, e) =>
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                SetActiveTab(ViewModel.ActiveTab ?? "quality");
                UpdateFilterCategoryButtons(ViewModel.ConnectionFilterCategory);
            });
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            TranslationManager.Instance.Translate(this);
            UpdateFilterCategoryButtons(ViewModel.ConnectionFilterCategory);
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

    private async void OnViewSpeedHistoryClick(object sender, RoutedEventArgs e)
    {
        var history = ViewModel.SpeedTestHistory.ToList();
        
        var stack = new StackPanel { Spacing = 12, MinWidth = 480 };

        // 1. Summary Analytics Header
        double maxDl = history.Count > 0 ? history.Max(h => h.DownloadMbps) : 0;
        double maxUl = history.Count > 0 ? history.Max(h => h.UploadMbps) : 0;
        double avgPing = history.Count > 0 ? history.Average(h => h.PingMs) : 0;

        var statsGrid = new Grid { ColumnSpacing = 8 };
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border CreateStatChip(string title, string value, Windows.UI.Color color)
        {
            var b = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(20, color.R, color.G, color.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(50, color.R, color.G, color.B)),
                BorderThickness = new Thickness(1)
            };
            var sp = new StackPanel { Spacing = 2 };
            sp.Children.Add(new TextBlock { Text = title, FontSize = 9, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Opacity = 0.7 });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13.5,
                FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color)
            });
            b.Child = sp;
            return b;
        }

        var tm = TranslationManager.Instance;
        var dlChip = CreateStatChip(tm.T("PEAK DOWNLOAD"), $"{maxDl:F1} Mbps", Windows.UI.Color.FromArgb(255, 16, 185, 129));
        Grid.SetColumn(dlChip, 0);
        statsGrid.Children.Add(dlChip);

        var ulChip = CreateStatChip(tm.T("PEAK UPLOAD"), $"{maxUl:F1} Mbps", Windows.UI.Color.FromArgb(255, 139, 92, 246));
        Grid.SetColumn(ulChip, 1);
        statsGrid.Children.Add(ulChip);

        var pingChip = CreateStatChip(tm.T("AVG LATENCY"), $"{avgPing:F0} ms", Windows.UI.Color.FromArgb(255, 6, 182, 212));
        Grid.SetColumn(pingChip, 2);
        statsGrid.Children.Add(pingChip);

        var countChip = CreateStatChip(tm.T("TOTAL TESTS"), $"{history.Count}", Windows.UI.Color.FromArgb(255, 245, 158, 11));
        Grid.SetColumn(countChip, 3);
        statsGrid.Children.Add(countChip);

        stack.Children.Add(statsGrid);

        // 2. Table Column Headers
        var headerGrid = new Grid { Margin = new Thickness(4, 4, 4, 0) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        headerGrid.Children.Add(new TextBlock { Text = tm.T("TIMESTAMP"), FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Opacity = 0.6 });
        var hDl = new TextBlock { Text = tm.T("DOWNLOAD"), FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Opacity = 0.6 };
        Grid.SetColumn(hDl, 1);
        headerGrid.Children.Add(hDl);
        var hUl = new TextBlock { Text = tm.T("UPLOAD"), FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Opacity = 0.6 };
        Grid.SetColumn(hUl, 2);
        headerGrid.Children.Add(hUl);
        var hPing = new TextBlock { Text = tm.T("PING"), FontSize = 9.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Opacity = 0.6 };
        Grid.SetColumn(hPing, 3);
        headerGrid.Children.Add(hPing);

        stack.Children.Add(headerGrid);

        // 3. Scrollable List of History items
        var listScrollViewer = new ScrollViewer { MaxHeight = 260, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var listStack = new StackPanel { Spacing = 4 };

        if (history.Count == 0)
        {
            listStack.Children.Add(new TextBlock
            {
                Text = tm.T("No speed test records found."),
                FontSize = 11.5,
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            });
        }
        else
        {
            foreach (var item in history)
            {
                var row = new Grid { Padding = new Thickness(6, 4, 6, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var tBlock = new TextBlock { Text = item.TimeFormatted, FontSize = 11, Opacity = 0.8, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(tBlock);

                var dlBadge = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 16, 185, 129)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = item.DownloadFormatted, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) }
                };
                Grid.SetColumn(dlBadge, 1);
                row.Children.Add(dlBadge);

                var ulBadge = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 139, 92, 246)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = item.UploadFormatted, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)) }
                };
                Grid.SetColumn(ulBadge, 2);
                row.Children.Add(ulBadge);

                var pingBadge = new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 6, 182, 212)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = item.PingFormatted, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 6, 182, 212)) }
                };
                Grid.SetColumn(pingBadge, 3);
                row.Children.Add(pingBadge);

                listStack.Children.Add(row);
            }
        }

        listScrollViewer.Content = listStack;
        stack.Children.Add(listScrollViewer);

        var dialog = new ContentDialog
        {
            Title = tm.T("Speed Test Telemetry History"),
            Content = stack,
            PrimaryButtonText = tm.T("Close"),
            SecondaryButtonText = history.Count > 0 ? tm.T("Clear All History") : null,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };

        var res = await dialog.ShowAsync();
        if (res == ContentDialogResult.Secondary)
        {
            await ViewModel.ClearSpeedTestHistoryAsync();
        }
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

    public string FormatConnectionCount(int count) => $"{count} Sockets Active";
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Http;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Services;
using WinCarePro.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel : ViewModelBase, IDisposable
{
    private DispatcherQueue? _dispatcherQueue;
    private readonly INetworkService _engine;
    private readonly INetworkHistoryService _historyService;
    private readonly INotificationService _notificationService;
    private System.Threading.CancellationTokenSource? _dnsCts;
    private System.Threading.CancellationTokenSource? _cts;

    private string _internetStatus = "Checking...";
    private string _gatewayAddress = "Loading...";
    private string _gatewayReachability = "Checking...";
    private string _dnsStatus = "Checking...";
    private string _ipStatus = "Checking...";

    private string _testHost = "8.8.8.8";
    private string _consoleOutput = "";
    private string _portScannerHost = "localhost";
    private string _portScannerPorts = "80,443,3389";

    private bool _isBusy;
    private double _latencyMs;
    private double _packetLossPercent;
    private double _downloadSpeedMbps;
    private double _uploadSpeedMbps;
    private double _jitterMs;
    private string _publicIpAddress = "Checking...";
    private string _connectionQuality = "Good";
    private string _currentDnsText = "Checking...";

    private double _downloadSpeed;
    private double _uploadSpeed;

    private List<ActiveConnectionInfo> _rawConnections = new();
    private string _connectionSearchQuery = "";
    private DnsServerInfo? _fastestDns;
    private string _fastestDnsText = "Not Tested";
    private double _speedProgress = 0;

    public ObservableCollection<double> DownloadSpeedHistory { get; } = new();
    public ObservableCollection<double> UploadSpeedHistory { get; } = new();
    public ObservableCollection<double> PingHistory { get; } = new();

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = new();
    public ObservableCollection<DnsServerInfo> DnsServers { get; } = new();
    public ObservableCollection<ActiveConnectionInfo> Connections { get; } = new();
    public ObservableCollection<ProcessGroupInfo> TopProcesses { get; } = new();

    private string _connectionFilterCategory = "All";
    public string ConnectionFilterCategory
    {
        get => _connectionFilterCategory;
        set
        {
            SetPropertyOnUI(() => _connectionFilterCategory, v => _connectionFilterCategory = v, value);
            ApplyConnectionFilter();
        }
    }

    public string ConnectionSearchQuery
    {
        get => _connectionSearchQuery;
        set
        {
            SetPropertyOnUI(() => _connectionSearchQuery, v => _connectionSearchQuery = v, value);
            ApplyConnectionFilter();
        }
    }

    public string FastestDnsText
    {
        get
        {
            if (_fastestDns != null)
            {
                return $"{_fastestDns.Name.T()} ({_fastestDns.AverageQueryMs:F0} ms)";
            }
            return _fastestDnsText.T();
        }
        set
        {
            _fastestDns = null;
            SetPropertyOnUI(() => _fastestDnsText, v => _fastestDnsText = v, value);
        }
    }

    private string _activeTab = "quality";
    public string ActiveTab
    {
        get => _activeTab;
        set => SetPropertyOnUI(() => _activeTab, v => _activeTab = v, value);
    }

    public double SpeedProgress
    {
        get => _speedProgress;
        set => SetPropertyOnUI(() => _speedProgress, v => _speedProgress = v, value);
    }

    public string InternetStatus
    {
        get => _internetStatus.T();
        set
        {
            SetPropertyOnUI(() => _internetStatus, v =>
            {
                _internetStatus = v;
                RecalculateHealthScore();
            }, value);
        }
    }

    public string GatewayAddress
    {
        get => _gatewayAddress.T();
        set => SetPropertyOnUI(() => _gatewayAddress, v => _gatewayAddress = v, value);
    }

    public string GatewayReachability
    {
        get => _gatewayReachability.T();
        set => SetPropertyOnUI(() => _gatewayReachability, v => _gatewayReachability = v, value);
    }

    public string DnsStatus
    {
        get => _dnsStatus.T();
        set => SetPropertyOnUI(() => _dnsStatus, v => _dnsStatus = v, value);
    }

    public string IpStatus
    {
        get => _ipStatus.T();
        set => SetPropertyOnUI(() => _ipStatus, v => _ipStatus = v, value);
    }

    public string TestHost
    {
        get => _testHost;
        set => SetPropertyOnUI(() => _testHost, v => _testHost = v, value);
    }

    public string ConsoleOutput
    {
        get => _consoleOutput;
        set => SetPropertyOnUI(() => _consoleOutput, v => _consoleOutput = v, value);
    }

    public string PortScannerHost
    {
        get => _portScannerHost;
        set => SetPropertyOnUI(() => _portScannerHost, v => _portScannerHost = v, value);
    }

    public string PortScannerPorts
    {
        get => _portScannerPorts;
        set => SetPropertyOnUI(() => _portScannerPorts, v => _portScannerPorts = v, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetPropertyOnUI(() => _isBusy, v => _isBusy = v, value);
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !_isBusy;

    public double LatencyMs
    {
        get => _latencyMs;
        set
        {
            SetPropertyOnUI(() => _latencyMs, v =>
            {
                _latencyMs = v;
                RecalculateHealthScore();
            }, value);
        }
    }

    public double PacketLossPercent
    {
        get => _packetLossPercent;
        set
        {
            SetPropertyOnUI(() => _packetLossPercent, v =>
            {
                _packetLossPercent = v;
                RecalculateHealthScore();
            }, value);
        }
    }

    public double JitterMs
    {
        get => _jitterMs;
        set
        {
            SetPropertyOnUI(() => _jitterMs, v =>
            {
                _jitterMs = v;
                RecalculateHealthScore();
            }, value);
        }
    }

    private int _networkHealthScore = 96;
    public int NetworkHealthScore
    {
        get => _networkHealthScore;
        private set => SetPropertyOnUI(() => _networkHealthScore, v => _networkHealthScore = v, value);
    }

    private string _networkHealthSummary = "Optimal for Gaming & Streaming";
    public string NetworkHealthSummary
    {
        get => _networkHealthSummary.T();
        private set => SetPropertyOnUI(() => _networkHealthSummary, v => _networkHealthSummary = v, value);
    }

    private string _networkHealthRecommendation = "Ideal bandwidth and latency standard. 0% packet loss.";
    public string NetworkHealthRecommendation
    {
        get => _networkHealthRecommendation.T();
        private set => SetPropertyOnUI(() => _networkHealthRecommendation, v => _networkHealthRecommendation = v, value);
    }

    public bool IsConnectionsEmpty => Connections.Count == 0 && !_isLoadingConnections;
    public bool IsDnsServersEmpty => DnsServers.Count == 0 && !IsBusy;
    public bool IsAdaptersEmpty => Adapters.Count == 0 && !IsBusy;
    public bool IsSpeedTesting => IsBusy && SpeedTestPhase != "Ready" && SpeedTestPhase != "Completed";
    public bool IsBenchmarkingDns => IsBusy && _dnsCts != null && !_dnsCts.IsCancellationRequested;
    public int ActiveConnectionsCount => Connections.Count;

    public void RecalculateHealthScore()
    {
        if (_internetStatus?.Contains("Disconn", StringComparison.OrdinalIgnoreCase) == true ||
            _internetStatus?.Contains("No", StringComparison.OrdinalIgnoreCase) == true)
        {
            NetworkHealthScore = 0;
            NetworkHealthSummary = "No Internet Connection";
            NetworkHealthRecommendation = "Please check network cables, router, or Wi-Fi connection.";
            return;
        }

        int score = 100;
        if (_latencyMs > 120) score -= 35;
        else if (_latencyMs > 60) score -= 20;
        else if (_latencyMs > 25) score -= 8;

        if (_packetLossPercent > 5) score -= 40;
        else if (_packetLossPercent > 0) score -= 15;

        if (_jitterMs > 15) score -= 20;
        else if (_jitterMs > 5) score -= 8;

        score = Math.Clamp(score, 20, 100);
        NetworkHealthScore = score;

        if (score >= 90)
        {
            NetworkHealthSummary = "Optimal for Gaming & 4K Streaming";
            NetworkHealthRecommendation = "Bandwidth is stable, 0% packet loss, ultra-low latency.";
        }
        else if (score >= 70)
        {
            NetworkHealthSummary = "Connection Stable";
            NetworkHealthRecommendation = "Latency is moderate. Run DNS Benchmark to optimize further.";
        }
        else
        {
            NetworkHealthSummary = "Degraded Network Quality";
            NetworkHealthRecommendation = "High latency or jitter. Use 'Smart Auto-Heal' to refresh network.";
        }
    }

    public string PublicIpAddress
    {
        get => _publicIpAddress.T();
        set => SetPropertyOnUI(() => _publicIpAddress, v => _publicIpAddress = v, value);
    }

    public string ConnectionQuality
    {
        get => _connectionQuality.T();
        set => SetPropertyOnUI(() => _connectionQuality, v => _connectionQuality = v, value);
    }

    public string CurrentDnsText
    {
        get => _currentDnsText.T();
        set => SetPropertyOnUI(() => _currentDnsText, v => _currentDnsText = v, value);
    }

    public double DownloadSpeedMbps
    {
        get => _downloadSpeedMbps;
        set
        {
            SetPropertyOnUI(() => _downloadSpeedMbps, v =>
            {
                _downloadSpeedMbps = v;
                OnPropertyChanged(nameof(DownloadSpeed));
                OnPropertyChanged(nameof(DisplaySpeed));
                OnPropertyChanged(nameof(DisplaySpeedLabel));
            }, value);
        }
    }

    public double UploadSpeedMbps
    {
        get => _uploadSpeedMbps;
        set
        {
            SetPropertyOnUI(() => _uploadSpeedMbps, v =>
            {
                _uploadSpeedMbps = v;
                OnPropertyChanged(nameof(UploadSpeed));
                OnPropertyChanged(nameof(DisplaySpeed));
                OnPropertyChanged(nameof(DisplaySpeedLabel));
            }, value);
        }
    }

    public double DownloadSpeed
    {
        get => _downloadSpeedMbps > 0 ? _downloadSpeedMbps : _downloadSpeed;
        set
        {
            _downloadSpeed = value;
            OnPropertyChanged(nameof(DownloadSpeed));
            OnPropertyChanged(nameof(DisplaySpeed));
        }
    }

    public double UploadSpeed
    {
        get => _uploadSpeedMbps > 0 ? _uploadSpeedMbps : _uploadSpeed;
        set
        {
            _uploadSpeed = value;
            OnPropertyChanged(nameof(UploadSpeed));
            OnPropertyChanged(nameof(DisplaySpeed));
        }
    }

    private string _speedTestPhase = "Ready";
    public string SpeedTestPhase
    {
        get => _speedTestPhase.T();
        set => SetPropertyOnUI(() => _speedTestPhase, v => _speedTestPhase = v, value);
    }

    public double DisplaySpeed
    {
        get
        {
            if (IsBusy)
            {
                if (SpeedProgress > 52) return UploadSpeed;
                if (SpeedProgress > 8) return DownloadSpeed;
                return LatencyMs;
            }
            return DownloadSpeed > 0 ? DownloadSpeed : 0.0;
        }
    }

    public string DisplaySpeedLabel
    {
        get
        {
            if (IsBusy)
            {
                if (SpeedProgress > 52) return "UPLOAD".T();
                if (SpeedProgress > 8) return "DOWNLOAD".T();
                return "PING".T();
            }
            return DownloadSpeed > 0 ? "PEAK DOWNLOAD".T() : "READY".T();
        }
    }

    private static Microsoft.UI.Xaml.Media.Brush? _purpleBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _greenBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _cyanBrush;

    private static Microsoft.UI.Xaml.Media.Brush? _purpleBadgeBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _greenBadgeBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _cyanBadgeBrush;

    public Microsoft.UI.Xaml.Media.Brush? SpeedPhaseAccentBrush
    {
        get
        {
            try
            {
                _purpleBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246));
                _greenBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
                _cyanBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 6, 182, 212));

                if (IsBusy)
                {
                    if (SpeedProgress > 52) return _purpleBrush;   // UPLOAD -> Electric Purple
                    if (SpeedProgress > 8) return _greenBrush;    // DOWNLOAD -> Emerald Green
                    return _cyanBrush;                            // PING -> Neon Cyan
                }
                return DownloadSpeed > 0 ? _greenBrush : _purpleBrush;
            }
            catch
            {
                return null;
            }
        }
    }

    public Microsoft.UI.Xaml.Media.Brush? SpeedPhaseBadgeBgBrush
    {
        get
        {
            try
            {
                _purpleBadgeBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 139, 92, 246));
                _greenBadgeBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 16, 185, 129));
                _cyanBadgeBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 6, 182, 212));

                if (IsBusy)
                {
                    if (SpeedProgress > 52) return _purpleBadgeBrush;
                    if (SpeedProgress > 8) return _greenBadgeBrush;
                    return _cyanBadgeBrush;
                }
                return DownloadSpeed > 0 ? _greenBadgeBrush : _purpleBadgeBrush;
            }
            catch
            {
                return null;
            }
        }
    }

    public bool HasSpeedTestHistory => SpeedTestHistory.Count > 0;
    public bool HasNoSpeedTestHistory => SpeedTestHistory.Count == 0;

    public ObservableCollection<SpeedTestResult> SpeedTestHistory { get; } = new();

    public NetworkViewModel(INetworkService engine, INetworkHistoryService historyService, INotificationService notificationService)
    {
        _engine = engine;
        _historyService = historyService;
        _notificationService = notificationService;
        try
        {
            _dispatcherQueue = App.MainDispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        }
        catch
        {
            _dispatcherQueue = null;
        }
        DispatcherQueueInstance = _dispatcherQueue;
        _cts = new System.Threading.CancellationTokenSource();

        InitializeFluentCore();
    }

    public NetworkViewModel() : this(
        App.Services?.GetService<INetworkService>() ?? new NetworkService(),
        App.Services?.GetService<INetworkHistoryService>() ?? new NetworkHistoryService(),
        App.Services?.GetService<INotificationService>() ?? new NotificationService())
    {
    }

    public void Initialize()
    {
        try
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue ?? _dispatcherQueue;
        }
        catch
        {
            _dispatcherQueue = App.MainDispatcherQueue ?? _dispatcherQueue;
        }
        DispatcherQueueInstance = _dispatcherQueue;
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.Initialize.Cancel", ex);
        }
        _cts = new System.Threading.CancellationTokenSource();

        // Unsubscribe first to prevent double-registration when page is re-navigated
        _engine.OutputReceived -= OnOutputReceived;
        _engine.OutputReceived += OnOutputReceived;

        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        LoadAdapters();
        InitializeFluentCore();
        _ = LoadActiveConnectionsAsync();
        _ = RunDiagnosticsAsync();
        _ = LoadHistoryAsync();
        _ = InitializeDohAsync();

        IsBusy = false;
        if (SpeedTestPhase != "Completed")
        {
            SpeedTestPhase = "Ready";
        }
        OnPropertyChanged(nameof(DisplaySpeed));
        OnPropertyChanged(nameof(DisplaySpeedLabel));
        OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
        OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));

        // Zero-Bloat On-Demand: Background polling loops eliminated in favor of strictly on-demand telemetry.
    }

    public void Cleanup()
    {
        _engine.OutputReceived -= OnOutputReceived;
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.Cleanup.Cancel", ex);
        }
        finally
        {
            _cts = null;
        }
        CancelDnsBenchmark();
        IsBusy = false;
        if (SpeedTestPhase != "Completed")
        {
            SpeedTestPhase = "Ready";
        }
        OnPropertyChanged(nameof(DisplaySpeed));
        OnPropertyChanged(nameof(DisplaySpeedLabel));
        OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
        OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
    }

    public void Dispose() => Cleanup();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(InternetStatus));
        OnPropertyChanged(nameof(GatewayAddress));
        OnPropertyChanged(nameof(GatewayReachability));
        OnPropertyChanged(nameof(DnsStatus));
        OnPropertyChanged(nameof(IpStatus));
        OnPropertyChanged(nameof(ConsoleOutput));
        OnPropertyChanged(nameof(PublicIpAddress));
        OnPropertyChanged(nameof(ConnectionQuality));
        OnPropertyChanged(nameof(CurrentDnsText));
        OnPropertyChanged(nameof(FastestDnsText));
        OnPropertyChanged(nameof(NetworkHealthSummary));
        OnPropertyChanged(nameof(NetworkHealthRecommendation));

        // Refresh collections to update their list view items
        OnPropertyChanged(nameof(Adapters));
        OnPropertyChanged(nameof(DnsServers));
        OnPropertyChanged(nameof(Connections));
    }

    public async Task<string?> ExportNetworkReportAsync()
    {
        try
        {
            string reportContent = _engine.ExportDiagnosticReport(
                InternetStatus,
                PublicIpAddress,
                GatewayAddress,
                DnsStatus,
                IpStatus,
                NetworkHealthScore,
                NetworkHealthSummary,
                NetworkHealthRecommendation,
                LatencyMs,
                JitterMs,
                PacketLossPercent,
                DownloadSpeed,
                UploadSpeed,
                Adapters.ToList(),
                DnsServers.ToList(),
                _rawConnections);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string filePath = System.IO.Path.Combine(desktopPath, $"WinCarePro_Network_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await System.IO.File.WriteAllTextAsync(filePath, reportContent);

            LogText(string.Format("Network diagnostics report saved to: {0}".T(), filePath));
            _notificationService?.ShowSuccess("Network Report Exported".T(), "Saved to Desktop and copied to clipboard.".T());
            Database.DbManager.LogAction("Export Network Report", "Network Diagnostics", "Success");
            return reportContent;
        }
        catch (Exception ex)
        {
            LogText($"Failed to export network diagnostics report: {ex.Message}");
            _notificationService?.ShowError("Action Failed".T(), ex.Message);
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.ExportNetworkReport", ex);
            return null;
        }
    }

    private void CancelDnsBenchmark()
    {
        try
        {
            _dnsCts?.Cancel();
            _dnsCts?.Dispose();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.CancelDnsBenchmark", ex);
        }
        finally
        {
            _dnsCts = null;
        }
    }

    private void OnOutputReceived(string msg)
    {
        OnOutputReceived(msg, true);
    }

    private void OnOutputReceived(string msg, bool unusedPlaceholder)
    {
        LogText(msg);
    }

    private void LogText(string msg)
    {
        try
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    ConsoleOutput += msg + "\n";
                }
                catch (Exception ex)
                {
                    CrashLogger.LogException("NetworkViewModel.LogText.Enqueue", ex);
                }
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.LogText", ex);
        }
    }

    /// <summary>
    /// Zero-Bloat On-Demand Telemetry:
    /// Bandwidth, adapter, port, and diagnostic checks are executed strictly on-demand
    /// when the user opens the page or explicitly clicks refresh, eliminating all background loops.
    /// </summary>
    public void RefreshTelemetryOnDemand()
    {
        try
        {
            LoadAdapters();
            if (ActiveTab == "ports")
            {
                _ = LoadActiveConnectionsAsync(forceRefresh: true);
            }
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.RefreshTelemetryOnDemand", ex);
        }
    }

    private void AddHistoryPoint(ObservableCollection<double> collection, double val)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            collection.Add(val);
            if (collection.Count > 120)
            {
                collection.RemoveAt(0);
            }
        });
    }

    private long GetTotalBytesReceived()
    {
        long total = 0;
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    try
                    {
                        var stats = ni.GetIPStatistics();
                        total += stats.BytesReceived;
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogException("NetworkViewModel.GetTotalBytesReceived.Stats", ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.GetTotalBytesReceived", ex);
        }
        return total;
    }

    private long GetTotalBytesSent()
    {
        long total = 0;
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    try
                    {
                        var stats = ni.GetIPStatistics();
                        total += stats.BytesSent;
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogException("NetworkViewModel.GetTotalBytesSent.Stats", ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.GetTotalBytesSent", ex);
        }
        return total;
    }

    // Shared HttpClient singleton to prevent socket exhaustion
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    private async Task<string> FetchPublicIpAddressAsync()
    {
        try
        {
            return (await _httpClient.GetStringAsync("https://api.ipify.org")).Trim();
        }
        catch
        {
            return "N/A";
        }
    }
}

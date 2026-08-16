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

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel : ViewModelBase
{
    private DispatcherQueue _dispatcherQueue;
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
        set => SetPropertyOnUI(() => _internetStatus, v => _internetStatus = v, value);
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
        set => SetPropertyOnUI(() => _latencyMs, v => _latencyMs = v, value);
    }

    public double PacketLossPercent
    {
        get => _packetLossPercent;
        set => SetPropertyOnUI(() => _packetLossPercent, v => _packetLossPercent = v, value);
    }

    public double DownloadSpeedMbps
    {
        get => _downloadSpeedMbps;
        set
        {
            SetPropertyOnUI(() => _downloadSpeedMbps, v => _downloadSpeedMbps = v, value);
            OnPropertyChanged(nameof(DownloadSpeed));
        }
    }

    public double UploadSpeedMbps
    {
        get => _uploadSpeedMbps;
        set
        {
            SetPropertyOnUI(() => _uploadSpeedMbps, v => _uploadSpeedMbps = v, value);
            OnPropertyChanged(nameof(UploadSpeed));
        }
    }

    public double JitterMs
    {
        get => _jitterMs;
        set => SetPropertyOnUI(() => _jitterMs, v => _jitterMs = v, value);
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

    public double DownloadSpeed
    {
        get => _downloadSpeedMbps > 0 ? _downloadSpeedMbps : _downloadSpeed;
        set
        {
            _downloadSpeed = value;
            _downloadSpeedMbps = value;
            OnPropertyChanged(nameof(DownloadSpeed));
            OnPropertyChanged(nameof(DownloadSpeedMbps));
        }
    }

    public double UploadSpeed
    {
        get => _uploadSpeedMbps > 0 ? _uploadSpeedMbps : _uploadSpeed;
        set
        {
            _uploadSpeed = value;
            _uploadSpeedMbps = value;
            OnPropertyChanged(nameof(UploadSpeed));
            OnPropertyChanged(nameof(UploadSpeedMbps));
        }
    }

    public ObservableCollection<SpeedTestResult> SpeedTestHistory { get; } = new();

    public NetworkViewModel(INetworkService engine, INetworkHistoryService historyService, INotificationService notificationService)
    {
        _engine = engine;
        _historyService = historyService;
        _notificationService = notificationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DispatcherQueueInstance = _dispatcherQueue;
        _cts = new System.Threading.CancellationTokenSource();
    }

    public NetworkViewModel() : this(
        App.Services?.GetService<INetworkService>() ?? new NetworkService(),
        App.Services?.GetService<INetworkHistoryService>() ?? new NetworkHistoryService(),
        App.Services?.GetService<INotificationService>() ?? new NotificationService())
    {
    }

    public void Initialize()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue ?? _dispatcherQueue;
        DispatcherQueueInstance = _dispatcherQueue;
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch { }
        _cts = new System.Threading.CancellationTokenSource();

        // Unsubscribe first to prevent double-registration when page is re-navigated
        _engine.OutputReceived -= OnOutputReceived;
        _engine.OutputReceived += OnOutputReceived;
        
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        LoadAdapters();
        _ = LoadActiveConnectionsAsync();
        _ = RunDiagnosticsAsync();
        _ = LoadHistoryAsync();
        _ = InitializeDohAsync();
        
        StartMonitoringLoops(_cts.Token);
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
        catch { }
        finally
        {
            _cts = null;
        }
        CancelDnsBenchmark();
    }

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

        // Refresh collections to update their list view items
        OnPropertyChanged(nameof(Adapters));
        OnPropertyChanged(nameof(DnsServers));
        OnPropertyChanged(nameof(Connections));
    }

    private void CancelDnsBenchmark()
    {
        try
        {
            _dnsCts?.Cancel();
            _dnsCts?.Dispose();
        }
        catch { }
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
                catch { }
            });
        }
        catch { }
    }

    private void StartMonitoringLoops(System.Threading.CancellationToken token)
    {
        // 1. Download/Upload Bandwidth Utilization Polling (1 sec)
        _ = Task.Run(async () =>
        {
            long lastRx = GetTotalBytesReceived();
            long lastTx = GetTotalBytesSent();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);
                    long currentRx = GetTotalBytesReceived();
                    long currentTx = GetTotalBytesSent();

                    long rxDiff = currentRx - lastRx;
                    long txDiff = currentTx - lastTx;

                    lastRx = currentRx;
                    lastTx = currentTx;

                    double dlSpeed = (rxDiff * 8.0) / 1_000_000.0;
                    double ulSpeed = (txDiff * 8.0) / 1_000_000.0;

                    if (!IsBusy)
                    {
                        DownloadSpeed = Math.Round(dlSpeed, 2);
                        UploadSpeed = Math.Round(ulSpeed, 2);
                        
                        AddHistoryPoint(DownloadSpeedHistory, DownloadSpeed);
                        AddHistoryPoint(UploadSpeedHistory, UploadSpeed);
                    }
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 2. Active Connections Polling (5 sec, only when 'ports' tab is active)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, token);
                    if (ActiveTab == "ports")
                    {
                        await LoadActiveConnectionsAsync();
                    }
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 3. Adapter Statistics Polling (10 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000, token);
                    LoadAdapters();
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 4. Public IP Polling (120 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string ip = await FetchPublicIpAddressAsync();
                    PublicIpAddress = ip;
                    await Task.Delay(120000, token);
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 5. DNS / Diagnostics Polling (120 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RunDiagnosticsAsync();
                    await Task.Delay(120000, token);
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);
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
                    catch { }
                }
            }
        }
        catch { }
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
                    catch { }
                }
            }
        }
        catch { }
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

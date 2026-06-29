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

public class NetworkViewModel : ViewModelBase
{
    private DispatcherQueue _dispatcherQueue;
    private readonly INetworkService _engine;
    private readonly INetworkHistoryService _historyService;
    private readonly INotificationService _notificationService;
    private System.Threading.CancellationTokenSource? _dnsCts;
    private System.Threading.CancellationTokenSource? _cts;

    private string _internetStatus = "Checking...".T();
    private string _gatewayAddress = "Loading...".T();
    private string _gatewayReachability = "Checking...".T();
    private string _dnsStatus = "Checking...".T();
    private string _ipStatus = "Checking...".T();
    
    private string _testHost = "8.8.8.8";
    private string _consoleOutput = "Network Console Ready.\n".T();
    private string _portScannerHost = "localhost";
    private string _portScannerPorts = "80,443,3389";

    private bool _isBusy;
    private double _latencyMs;
    private double _packetLossPercent;
    private double _downloadSpeedMbps;
    private double _uploadSpeedMbps;
    private double _jitterMs;
    private string _publicIpAddress = "Checking...".T();
    private string _connectionQuality = "Good".T();
    private string _currentDnsText = "Checking...".T();

    private double _downloadSpeed;
    private double _uploadSpeed;

    private ObservableCollection<NetworkAdapterInfo> _adapters = new();
    private ObservableCollection<DnsServerInfo> _dnsServers = new();
    private ObservableCollection<ActiveConnectionInfo> _connections = new();
    private List<ActiveConnectionInfo> _rawConnections = new();
    private string _connectionSearchQuery = "";
    private string _fastestDnsText = "Not Tested".T();
    private double _speedProgress = 0;
    private ObservableCollection<SpeedTestResult> _speedTestHistory = new();

    public ObservableCollection<double> DownloadSpeedHistory { get; } = new();
    public ObservableCollection<double> UploadSpeedHistory { get; } = new();
    public ObservableCollection<double> PingHistory { get; } = new();

    public ObservableCollection<NetworkAdapterInfo> Adapters
    {
        get => _adapters;
        set => SetPropertyOnUI(() => _adapters, v => _adapters = v, value);
    }

    public ObservableCollection<DnsServerInfo> DnsServers
    {
        get => _dnsServers;
        set => SetPropertyOnUI(() => _dnsServers, v => _dnsServers = v, value);
    }

    public ObservableCollection<ActiveConnectionInfo> Connections
    {
        get => _connections;
        set => SetPropertyOnUI(() => _connections, v => _connections = v, value);
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
        get => _fastestDnsText;
        set => SetPropertyOnUI(() => _fastestDnsText, v => _fastestDnsText = v, value);
    }

    public double SpeedProgress
    {
        get => _speedProgress;
        set => SetPropertyOnUI(() => _speedProgress, v => _speedProgress = v, value);
    }

    public string InternetStatus
    {
        get => _internetStatus;
        set => SetPropertyOnUI(() => _internetStatus, v => _internetStatus = v, value);
    }

    public string GatewayAddress
    {
        get => _gatewayAddress;
        set => SetPropertyOnUI(() => _gatewayAddress, v => _gatewayAddress = v, value);
    }

    public string GatewayReachability
    {
        get => _gatewayReachability;
        set => SetPropertyOnUI(() => _gatewayReachability, v => _gatewayReachability = v, value);
    }

    public string DnsStatus
    {
        get => _dnsStatus;
        set => SetPropertyOnUI(() => _dnsStatus, v => _dnsStatus = v, value);
    }

    public string IpStatus
    {
        get => _ipStatus;
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
        set => SetPropertyOnUI(() => _downloadSpeedMbps, v => _downloadSpeedMbps = v, value);
    }

    public double UploadSpeedMbps
    {
        get => _uploadSpeedMbps;
        set => SetPropertyOnUI(() => _uploadSpeedMbps, v => _uploadSpeedMbps = v, value);
    }

    public double JitterMs
    {
        get => _jitterMs;
        set => SetPropertyOnUI(() => _jitterMs, v => _jitterMs = v, value);
    }

    public string PublicIpAddress
    {
        get => _publicIpAddress;
        set => SetPropertyOnUI(() => _publicIpAddress, v => _publicIpAddress = v, value);
    }

    public string ConnectionQuality
    {
        get => _connectionQuality;
        set => SetPropertyOnUI(() => _connectionQuality, v => _connectionQuality = v, value);
    }

    public string CurrentDnsText
    {
        get => _currentDnsText;
        set => SetPropertyOnUI(() => _currentDnsText, v => _currentDnsText = v, value);
    }

    public double DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetPropertyOnUI(() => _downloadSpeed, v => _downloadSpeed = v, value);
    }

    public double UploadSpeed
    {
        get => _uploadSpeed;
        set => SetPropertyOnUI(() => _uploadSpeed, v => _uploadSpeed = v, value);
    }

    public ObservableCollection<SpeedTestResult> SpeedTestHistory
    {
        get => _speedTestHistory;
        set => SetPropertyOnUI(() => _speedTestHistory, v => _speedTestHistory = v, value);
    }

    public NetworkViewModel(INetworkService engine, INetworkHistoryService historyService, INotificationService notificationService)
    {
        _engine = engine;
        _historyService = historyService;
        _notificationService = notificationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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
        
        LoadAdapters();
        _ = LoadActiveConnectionsAsync();
        _ = RunDiagnosticsAsync();
        _ = LoadHistoryAsync();
        
        StartMonitoringLoops(_cts.Token);
    }

    public void Cleanup()
    {
        _engine.OutputReceived -= OnOutputReceived;
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
        LogText(msg);
    }

    private void SetPropertyOnUI<T>(Func<T> getter, Action<T> setter, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (Equals(getter(), value)) return;

        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            T localValue = value;
            try
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (!Equals(getter(), localValue))
                        {
                            setter(localValue);
                            OnPropertyChanged(propertyName);
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }
        else
        {
            try
            {
                setter(value);
                OnPropertyChanged(propertyName);
            }
            catch { }
        }
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

    public async Task RunDiagnosticsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Starting connectivity diagnosis...".T());

        try
        {
            bool hasInternet = await Task.Run(() => _engine.CheckInternetConnection());
            if (_cts == null || _cts.IsCancellationRequested) return;
            InternetStatus = hasInternet ? "Connected".T() : "No Internet".T();

            string gw = await Task.Run(() => _engine.GetGatewayAddress());
            if (_cts == null || _cts.IsCancellationRequested) return;
            GatewayAddress = gw;

            bool gatewayOk = await Task.Run(() => _engine.CheckGatewayReachability());
            if (_cts == null || _cts.IsCancellationRequested) return;
            GatewayReachability = gatewayOk ? "Reachable".T() : "Unreachable".T();

            bool dnsOk = await Task.Run(() => _engine.CheckDnsResolution());
            if (_cts == null || _cts.IsCancellationRequested) return;
            DnsStatus = dnsOk ? "Resolving".T() : "Failed".T();

            var (v4, v6) = await Task.Run(() => _engine.CheckIpStatus());
            if (_cts == null || _cts.IsCancellationRequested) return;
            IpStatus = $"IPv4: {(v4 ? "Active" : "Inactive")}, IPv6: {(v6 ? "Active" : "Inactive")}".T();

            LogText("Estimating packet loss, latency, and jitter quality...".T());
            var (loss, latency, jitter) = await _engine.AnalyzePingQualityAsync();
            if (_cts == null || _cts.IsCancellationRequested) return;
            LatencyMs = Math.Round(latency, 1);
            PacketLossPercent = Math.Round(loss, 1);
            JitterMs = Math.Round(jitter, 1);

            // Connection quality mapping
            if (loss > 10.0 || latency > 150.0)
                ConnectionQuality = "Poor".T();
            else if (loss > 2.0 || latency > 60.0 || jitter > 15.0)
                ConnectionQuality = "Moderate".T();
            else
                ConnectionQuality = "Good".T();
            
            // Add point to history charts
            AddHistoryPoint(PingHistory, LatencyMs);

            LogText(string.Format("Diagnostics complete. Latency: {0}ms, Jitter: {1}ms, Packet Loss: {2}%.".T(), LatencyMs, JitterMs, PacketLossPercent));
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Diagnostics error: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunPingTestAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunPingTestAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Ping test failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunTracerouteAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunTracerouteAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Traceroute failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunDnsLookupAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunDnsLookupAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("DNS Lookup failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunPortScanAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(PortScannerHost)) return;
        IsBusy = true;
        try
        {
            var ports = PortScannerPorts.Split(',')
                .Select(p => int.TryParse(p.Trim(), out int val) ? val : -1)
                .Where(v => v > 0)
                .ToArray();

            await _engine.RunPortScanAsync(PortScannerHost, ports);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Port scan failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunSpeedTestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        SpeedProgress = 0;
        DownloadSpeedMbps = 0;
        UploadSpeedMbps = 0;
        LogText("Starting speed test...".T());
        try
        {
            LogText("Running download speed benchmark...".T());
            double dl = await _engine.RunSpeedTestAsync((speed, progress) =>
            {
                DownloadSpeedMbps = Math.Round(speed, 1);
                SpeedProgress = Math.Round(progress / 2.0, 1);
            });

            if (_cts == null || _cts.IsCancellationRequested) return;

            LogText("Running upload speed benchmark...".T());
            double ul = await _engine.RunUploadSpeedTestAsync((speed, progress) =>
            {
                UploadSpeedMbps = Math.Round(speed, 1);
                SpeedProgress = Math.Round(50.0 + (progress / 2.0), 1);
            });

            if (_cts == null || _cts.IsCancellationRequested) return;
            SpeedProgress = 100;

            var result = new SpeedTestResult
            {
                DownloadMbps = DownloadSpeedMbps,
                UploadMbps = UploadSpeedMbps,
                PingMs = LatencyMs,
                JitterMs = JitterMs,
                ServerName = "Tele2 & Httpbin CDN",
                TestDuration = 16.0,
                Timestamp = DateTime.Now
            };

            await _historyService.SaveSpeedTestResultAsync(result);
            await LoadHistoryAsync();

            LogText(string.Format("Speed test complete. Download: {0} Mbps, Upload: {1} Mbps, Latency: {2} ms, Jitter: {3} ms.".T(), DownloadSpeedMbps, UploadSpeedMbps, LatencyMs, JitterMs));
            _notificationService?.ShowSuccess("Speed Test Completed".T(), string.Format("Download: {0} Mbps, Upload: {1} Mbps.".T(), DownloadSpeedMbps, UploadSpeedMbps));
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Speed test failed: {0}".T(), ex.Message));
                _notificationService?.ShowError("Speed Test Failed".T(), ex.Message);
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public void LoadAdapters()
    {
        try
        {
            var list = _engine.GetNetworkAdapters();
            // Get DNS for first interface
            string dnsText = "Unknown";
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var props = ni.GetIPProperties();
                    var dnsServers = props.DnsAddresses.Select(d => d.ToString()).ToList();
                    if (dnsServers.Count > 0)
                    {
                        dnsText = string.Join(", ", dnsServers);
                        break;
                    }
                }
            }
            
            try
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        Adapters = new ObservableCollection<NetworkAdapterInfo>(list);
                        CurrentDnsText = dnsText;
                    }
                    catch { }
                });
            }
            catch { }
        }
        catch (Exception ex)
        {
            LogText($"Failed to load adapters: {ex.Message}");
        }
    }

    public async Task LoadActiveConnectionsAsync()
    {
        try
        {
            var list = await Task.Run(() => _engine.GetActiveConnections());
            try
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        _rawConnections = list;
                        ApplyConnectionFilter();
                    }
                    catch { }
                });
            }
            catch { }
        }
        catch (Exception ex)
        {
            LogText($"Failed to load active connections: {ex.Message}");
        }
    }

    private void ApplyConnectionFilter()
    {
        try
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    var query = _connectionSearchQuery?.Trim().ToLower() ?? "";
                    if (string.IsNullOrEmpty(query))
                    {
                        Connections = new ObservableCollection<ActiveConnectionInfo>(_rawConnections);
                    }
                    else
                    {
                        var filtered = _rawConnections.Where(c =>
                            c.ProcessName.ToLower().Contains(query) ||
                            c.Protocol.ToLower().Contains(query) ||
                            c.LocalAddress.ToLower().Contains(query) ||
                            c.ForeignAddress.ToLower().Contains(query) ||
                            c.State.ToLower().Contains(query) ||
                            c.Pid.ToString().Contains(query)
                        ).ToList();
                        Connections = new ObservableCollection<ActiveConnectionInfo>(filtered);
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    public async Task StartDnsBenchmarkAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Initiating DNS query resolution benchmark...".T());
        CancelDnsBenchmark();
        _dnsCts = new System.Threading.CancellationTokenSource();
        var token = _dnsCts.Token;
        try
        {
            var result = await _engine.RunDnsBenchmarkAsync(token);
            if (token.IsCancellationRequested || (_cts != null && _cts.IsCancellationRequested)) return;
            try
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        if (token.IsCancellationRequested || (_cts != null && _cts.IsCancellationRequested)) return;
                        DnsServers = new ObservableCollection<DnsServerInfo>(result);
                        var fastest = result.FirstOrDefault(d => d.IsFastest);
                        FastestDnsText = fastest != null ? $"{fastest.Name} ({fastest.AverageQueryMs:F0} ms)" : "Failed".T();
                    }
                    catch { }
                });
            }
            catch { }
            
            await _historyService.SaveDnsBenchmarkResultAsync(result);
            _notificationService?.ShowSuccess("DNS Benchmark Completed".T(), string.Format("Fastest server: {0}".T(), FastestDnsText));
        }
        catch (OperationCanceledException)
        {
            // Do not update UI or log if cancelled
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested && (_cts == null || !_cts.IsCancellationRequested))
            {
                LogText($"DNS benchmark error: {ex.Message}");
            }
        }
        finally
        {
            if (!token.IsCancellationRequested && (_cts == null || !_cts.IsCancellationRequested))
            {
                IsBusy = false;
            }
        }
    }

    public async Task ApplyDnsAsync(DnsServerInfo server)
    {
        if (IsBusy || server == null) return;
        IsBusy = true;
        try
        {
            bool ok = await _engine.ApplyDnsSettingsAsync(server.Name, server.PrimaryIp, server.SecondaryIp);
            if (_cts == null || _cts.IsCancellationRequested) return;
            if (ok)
            {
                LogText(string.Format("Successfully applied DNS: {0}".T(), server.Name));
                _notificationService?.ShowSuccess("DNS Server Updated".T(), string.Format("Active interface configured to use {0}.".T(), server.Name));
            }
            else
            {
                LogText("Failed to apply DNS settings (Requires administrator privilege).".T());
                _notificationService?.ShowError("DNS Setup Failed".T(), "Administrative privileges required.");
            }
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText($"Error applying DNS settings: {ex.Message}");
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunRepairOperationAsync(string operation)
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText(string.Format("Initiating repair action: {0}...".T(), operation));
        
        try
        {
            bool ok = operation.ToLower() switch
            {
                "dns" => await _engine.FlushDnsAsync(),
                "winsock" => await _engine.ResetWinsockAsync(),
                "tcpip" => await _engine.ResetTcpIpAsync(),
                "iprenew" => await _engine.ReleaseRenewIpAsync(),
                "adapter" => await _engine.RestartNetworkAdapterAsync(),
                "firewall" => await _engine.ResetFirewallAsync(),
                "proxy" => await _engine.ResetProxyAsync(),
                "hosts" => await _engine.ResetHostsFileAsync(),
                "optimize" => await _engine.OptimizeTcpAutoTuningAsync(),
                "green" => await _engine.DisableEnergyEfficientEthernetAsync(),
                _ => false
            };

            if (_cts == null || _cts.IsCancellationRequested) return;
            
            if (ok)
            {
                LogText("Repair operation succeeded.".T());
                _notificationService?.ShowSuccess("Network Repair".T(), string.Format("Operation '{0}' completed successfully.", operation).T());
            }
            else
            {
                LogText("Repair operation encountered errors.".T());
                _notificationService?.ShowWarning("Network Repair".T(), string.Format("Operation '{0}' failed or requires Administrator elevation.", operation).T());
            }
            await RunDiagnosticsAsync(); // refresh connectivity status
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Repair failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var list = await _historyService.GetSpeedTestHistoryAsync();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                SpeedTestHistory = new ObservableCollection<SpeedTestResult>(list);
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

        // 2. Active Connections Polling (2 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token);
                    await LoadActiveConnectionsAsync();
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 3. Adapter Statistics Polling (3 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3000, token);
                    LoadAdapters();
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 4. Public IP Polling (30 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string ip = await FetchPublicIpAddressAsync();
                    PublicIpAddress = ip;
                    await Task.Delay(30000, token);
                }
                catch (TaskCanceledException) { break; }
                catch { }
            }
        }, token);

        // 5. DNS / Diagnostics Polling (60 sec)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RunDiagnosticsAsync();
                    await Task.Delay(60000, token);
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

    private async Task<string> FetchPublicIpAddressAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            return (await client.GetStringAsync("https://api.ipify.org")).Trim();
        }
        catch
        {
            return "N/A";
        }
    }
}

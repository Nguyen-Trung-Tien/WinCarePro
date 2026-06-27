using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Dispatching;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Services;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class NetworkViewModel : ViewModelBase
{
    private DispatcherQueue _dispatcherQueue;
    private readonly INetworkService _engine;

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

    private ObservableCollection<NetworkAdapterInfo> _adapters = new();
    private ObservableCollection<DnsServerInfo> _dnsServers = new();
    private ObservableCollection<ActiveConnectionInfo> _connections = new();
    private List<ActiveConnectionInfo> _rawConnections = new();
    private string _connectionSearchQuery = "";
    private string _fastestDnsText = "Not Tested".T();
    private double _speedProgress = 0;

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
        set => SetPropertyOnUI(() => _isBusy, v => _isBusy = v, value);
    }

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

    public NetworkViewModel(INetworkService engine)
    {
        _engine = engine;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = RunDiagnosticsAsync();
    }

    public NetworkViewModel() : this(new NetworkService())
    {
    }

    public void Initialize()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue ?? _dispatcherQueue;

        // Unsubscribe first to prevent double-registration when page is re-navigated (NavigationCacheMode.Required)
        _engine.OutputReceived -= OnOutputReceived;
        _engine.OutputReceived += OnOutputReceived;
        LoadAdapters();
        _ = LoadActiveConnectionsAsync();
        _ = RunDiagnosticsAsync();
    }

    public void Cleanup()
    {
        _engine.OutputReceived -= OnOutputReceived;
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
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!Equals(getter(), localValue))
                {
                    setter(localValue);
                    OnPropertyChanged(propertyName);
                }
            });
        }
        else
        {
            setter(value);
            OnPropertyChanged(propertyName);
        }
    }

    private void LogText(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ConsoleOutput += msg + "\n";
        });
    }

    public async Task RunDiagnosticsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Starting connectivity diagnosis...".T());

        try
        {
            bool hasInternet = await Task.Run(() => _engine.CheckInternetConnection());
            InternetStatus = hasInternet ? "Connected".T() : "No Internet".T();

            GatewayAddress = await Task.Run(() => _engine.GetGatewayAddress());

            bool gatewayOk = await Task.Run(() => _engine.CheckGatewayReachability());
            GatewayReachability = gatewayOk ? "Reachable".T() : "Unreachable".T();

            bool dnsOk = await Task.Run(() => _engine.CheckDnsResolution());
            DnsStatus = dnsOk ? "Resolving".T() : "Failed".T();

            var (v4, v6) = await Task.Run(() => _engine.CheckIpStatus());
            IpStatus = $"IPv4: {(v4 ? "Active" : "Inactive")}, IPv6: {(v6 ? "Active" : "Inactive")}".T();

            LogText("Estimating packet loss and latency quality...".T());
            var (loss, latency) = await _engine.AnalyzePingQualityAsync();
            LatencyMs = Math.Round(latency, 1);
            PacketLossPercent = Math.Round(loss, 1);
            
            LogText(string.Format("Diagnostics complete. Latency: {0}ms, Packet Loss: {1}%.".T(), LatencyMs, PacketLossPercent));
        }
        catch (Exception ex)
        {
            LogText(string.Format("Diagnostics error: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
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
            LogText(string.Format("Ping test failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
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
            LogText(string.Format("Traceroute failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
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
            LogText(string.Format("DNS Lookup failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
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
            LogText(string.Format("Port scan failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunSpeedTestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        SpeedProgress = 0;
        LogText("Starting speed test...".T());
        try
        {
            var progressTask = Task.Run(async () =>
            {
                for (int i = 1; i <= 95; i += 5)
                {
                    if (!IsBusy) break;
                    SpeedProgress = i;
                    await Task.Delay(100);
                }
            });

            double speed = await _engine.RunSpeedTestAsync();
            DownloadSpeedMbps = Math.Round(speed, 1);
            SpeedProgress = 100;
            LogText(string.Format("Speed test complete: {0} Mbps.".T(), DownloadSpeedMbps));
        }
        catch (Exception ex)
        {
            LogText(string.Format("Speed test failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LoadAdapters()
    {
        try
        {
            var list = _engine.GetNetworkAdapters();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                Adapters = new ObservableCollection<NetworkAdapterInfo>(list);
            });
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
            _dispatcherQueue?.TryEnqueue(() =>
            {
                _rawConnections = list;
                ApplyConnectionFilter();
            });
        }
        catch (Exception ex)
        {
            LogText($"Failed to load active connections: {ex.Message}");
        }
    }

    private void ApplyConnectionFilter()
    {
        _dispatcherQueue?.TryEnqueue(() =>
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
        });
    }

    public async Task StartDnsBenchmarkAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Initiating DNS latency benchmark...".T());
        try
        {
            var result = await _engine.RunDnsBenchmarkAsync();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                DnsServers = new ObservableCollection<DnsServerInfo>(result);
                var fastest = result.FirstOrDefault(d => d.IsFastest);
                FastestDnsText = fastest != null ? $"{fastest.Name} ({fastest.PingMs:F0} ms)" : "Failed".T();
            });
        }
        catch (Exception ex)
        {
            LogText($"DNS benchmark error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ApplyDnsAsync(DnsServerInfo server)
    {
        if (IsBusy || server == null) return;
        IsBusy = true;
        try
        {
            bool ok = await _engine.ApplyDnsSettingsAsync(server.Name, server.PrimaryIp, server.SecondaryIp);
            if (ok)
            {
                LogText(string.Format("Successfully applied DNS: {0}".T(), server.Name));
            }
            else
            {
                LogText("Failed to apply DNS settings (Requires administrator privilege).".T());
            }
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            LogText($"Error applying DNS settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
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
                _ => false
            };

            LogText(ok ? "Repair operation succeeded.".T() : "Repair operation encountered errors.".T());
            await RunDiagnosticsAsync(); // refresh connectivity status
        }
        catch (Exception ex)
        {
            LogText(string.Format("Repair failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

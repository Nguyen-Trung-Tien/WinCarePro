using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class NetworkViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
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
        _engine.OutputReceived += OnOutputReceived;
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
        try
        {
            double speed = await _engine.RunSpeedTestAsync();
            DownloadSpeedMbps = Math.Round(speed, 1);
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

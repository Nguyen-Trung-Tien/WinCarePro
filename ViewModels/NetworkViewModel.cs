using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public class NetworkViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly NetworkEngine _engine = new();

    private string _internetStatus = "Checking...";
    private string _gatewayAddress = "Loading...";
    private string _gatewayReachability = "Checking...";
    private string _dnsStatus = "Checking...";
    private string _ipStatus = "Checking...";
    
    private string _testHost = "8.8.8.8";
    private string _consoleOutput = "Network Console Ready.\n";
    private string _portScannerHost = "localhost";
    private string _portScannerPorts = "80,443,3389";

    private bool _isBusy;
    private double _latencyMs;
    private double _packetLossPercent;
    private double _downloadSpeedMbps;

    public string InternetStatus
    {
        get => _internetStatus;
        set => SetProperty(ref _internetStatus, value);
    }

    public string GatewayAddress
    {
        get => _gatewayAddress;
        set => SetProperty(ref _gatewayAddress, value);
    }

    public string GatewayReachability
    {
        get => _gatewayReachability;
        set => SetProperty(ref _gatewayReachability, value);
    }

    public string DnsStatus
    {
        get => _dnsStatus;
        set => SetProperty(ref _dnsStatus, value);
    }

    public string IpStatus
    {
        get => _ipStatus;
        set => SetProperty(ref _ipStatus, value);
    }

    public string TestHost
    {
        get => _testHost;
        set => SetProperty(ref _testHost, value);
    }

    public string ConsoleOutput
    {
        get => _consoleOutput;
        set => SetProperty(ref _consoleOutput, value);
    }

    public string PortScannerHost
    {
        get => _portScannerHost;
        set => SetProperty(ref _portScannerHost, value);
    }

    public string PortScannerPorts
    {
        get => _portScannerPorts;
        set => SetProperty(ref _portScannerPorts, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public double LatencyMs
    {
        get => _latencyMs;
        set => SetProperty(ref _latencyMs, value);
    }

    public double PacketLossPercent
    {
        get => _packetLossPercent;
        set => SetProperty(ref _packetLossPercent, value);
    }

    public double DownloadSpeedMbps
    {
        get => _downloadSpeedMbps;
        set => SetProperty(ref _downloadSpeedMbps, value);
    }

    public NetworkViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += LogText;
        _ = RunDiagnosticsAsync();
    }

    private void LogText(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ConsoleOutput += msg + "\n";
        });
    }

    public async Task RunDiagnosticsAsync()
    {
        IsBusy = true;
        LogText("Starting connectivity diagnosis...");

        try
        {
            bool hasInternet = await Task.Run(() => _engine.CheckInternetConnection());
            InternetStatus = hasInternet ? "Connected" : "No Internet";

            GatewayAddress = await Task.Run(() => _engine.GetGatewayAddress());

            bool gatewayOk = await Task.Run(() => _engine.CheckGatewayReachability());
            GatewayReachability = gatewayOk ? "Reachable" : "Unreachable";

            bool dnsOk = await Task.Run(() => _engine.CheckDnsResolution());
            DnsStatus = dnsOk ? "Resolving" : "Failed";

            var (v4, v6) = await Task.Run(() => _engine.CheckIpStatus());
            IpStatus = $"IPv4: {(v4 ? "Active" : "Inactive")}, IPv6: {(v6 ? "Active" : "Inactive")}";

            LogText("Estimating packet loss and latency quality...");
            var (loss, latency) = await _engine.AnalyzePingQualityAsync();
            LatencyMs = Math.Round(latency, 1);
            PacketLossPercent = Math.Round(loss, 1);
            
            LogText($"Diagnostics complete. Latency: {LatencyMs}ms, Packet Loss: {PacketLossPercent}%.");
        }
        catch (Exception ex)
        {
            LogText($"Diagnostics error: {ex.Message}");
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
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunRepairOperationAsync(string operation)
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText($"Initiating repair action: {operation}...");
        
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

            LogText(ok ? "Repair operation succeeded." : "Repair operation encountered errors.");
            await RunDiagnosticsAsync(); // refresh connectivity status
        }
        catch (Exception ex)
        {
            LogText($"Repair failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

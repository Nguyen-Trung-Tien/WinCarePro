using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Services.Contracts;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Services.Implementations;

public class NetworkService : INetworkService
{
    private readonly NetworkEngine _engine;

    public event Action<string>? OutputReceived;

    public NetworkService()
    {
        _engine = new NetworkEngine();
        _engine.OutputReceived += msg => OutputReceived?.Invoke(msg);
    }

    public bool CheckInternetConnection() => _engine.CheckInternetConnection();

    public string GetGatewayAddress() => _engine.GetGatewayAddress();

    public bool CheckGatewayReachability() => _engine.CheckGatewayReachability();

    public bool CheckDnsResolution() => _engine.CheckDnsResolution();

    public (bool ipv4, bool ipv6) CheckIpStatus() => _engine.CheckIpStatus();

    public Task<(double packetLossPercent, double avgLatencyMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5)
    {
        return _engine.AnalyzePingQualityAsync(target, count);
    }

    public Task RunPingTestAsync(string host, int count = 4)
    {
        return _engine.RunPingTestAsync(host, count);
    }

    public Task RunTracerouteAsync(string host, int maxHops = 30)
    {
        return _engine.RunTracerouteAsync(host, maxHops);
    }

    public Task RunDnsLookupAsync(string host)
    {
        return _engine.RunDnsLookupAsync(host);
    }

    public Task RunPortScanAsync(string host, int[] ports)
    {
        return _engine.RunPortScanAsync(host, ports);
    }

    public Task<double> RunSpeedTestAsync()
    {
        return _engine.RunSpeedTestAsync();
    }

    public Task<bool> FlushDnsAsync() => _engine.FlushDnsAsync();

    public Task<bool> ResetWinsockAsync() => _engine.ResetWinsockAsync();

    public Task<bool> ResetTcpIpAsync() => _engine.ResetTcpIpAsync();

    public Task<bool> ReleaseRenewIpAsync() => _engine.ReleaseRenewIpAsync();

    public Task<bool> ResetFirewallAsync() => _engine.ResetFirewallAsync();

    public Task<bool> ResetProxyAsync() => _engine.ResetProxyAsync();

    public Task<bool> RestartNetworkAdapterAsync() => _engine.RestartNetworkAdapterAsync();

    public List<NetworkAdapterInfo> GetNetworkAdapters() => _engine.GetNetworkAdapters();

    public Task<List<DnsServerInfo>> RunDnsBenchmarkAsync() => _engine.RunDnsBenchmarkAsync();

    public Task<bool> ApplyDnsSettingsAsync(string dnsName, string primaryIp, string secondaryIp)
    {
        return _engine.ApplyDnsSettingsAsync(dnsName, primaryIp, secondaryIp);
    }

    public List<ActiveConnectionInfo> GetActiveConnections() => _engine.GetActiveConnections();
}

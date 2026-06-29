using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public interface INetworkService
{
    event Action<string>? OutputReceived;
    bool CheckInternetConnection();
    string GetGatewayAddress();
    bool CheckGatewayReachability();
    bool CheckDnsResolution();
    (bool ipv4, bool ipv6) CheckIpStatus();
    Task<(double packetLossPercent, double avgLatencyMs, double jitterMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5);
    Task RunPingTestAsync(string host, int count = 4);
    Task RunTracerouteAsync(string host, int maxHops = 30);
    Task RunDnsLookupAsync(string host);
    Task RunPortScanAsync(string host, int[] ports);
    Task<double> RunSpeedTestAsync(Action<double, double>? progressCallback = null);
    Task<double> RunUploadSpeedTestAsync(Action<double, double>? progressCallback = null);
    Task<bool> FlushDnsAsync();
    Task<bool> ResetWinsockAsync();
    Task<bool> ResetTcpIpAsync();
    Task<bool> ReleaseRenewIpAsync();
    Task<bool> ResetFirewallAsync();
    Task<bool> ResetProxyAsync();
    Task<bool> RestartNetworkAdapterAsync();
    Task<bool> ResetHostsFileAsync();
    Task<bool> OptimizeTcpAutoTuningAsync();
    Task<bool> DisableEnergyEfficientEthernetAsync();
    List<NetworkAdapterInfo> GetNetworkAdapters();
    Task<List<DnsServerInfo>> RunDnsBenchmarkAsync(System.Threading.CancellationToken cancellationToken = default);
    Task<bool> ApplyDnsSettingsAsync(string dnsName, string primaryIp, string secondaryIp);
    List<ActiveConnectionInfo> GetActiveConnections();
}

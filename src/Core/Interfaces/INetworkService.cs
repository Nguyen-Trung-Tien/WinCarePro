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
    Task<bool> CheckDnsResolutionAsync(System.Threading.CancellationToken cancellationToken = default);
    (bool ipv4, bool ipv6) CheckIpStatus();
    Task<(double packetLossPercent, double avgLatencyMs, double jitterMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5);
    Task RunPingTestAsync(string host, int count = 4, System.Threading.CancellationToken cancellationToken = default);
    Task RunTracerouteAsync(string host, int maxHops = 30, System.Threading.CancellationToken cancellationToken = default);
    Task RunDnsLookupAsync(string host, System.Threading.CancellationToken cancellationToken = default);
    Task RunPortScanAsync(string host, int[] ports, System.Threading.CancellationToken cancellationToken = default);
    Task<double> RunSpeedTestAsync(Action<double, double>? progressCallback = null, CancellationToken cancellationToken = default);
    Task<double> RunUploadSpeedTestAsync(Action<double, double>? progressCallback = null, CancellationToken cancellationToken = default);
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
    Task<bool> ApplyDnsSettingsAsync(string dnsName, string primaryIp, string secondaryIp, System.Threading.CancellationToken cancellationToken = default);
    List<ActiveConnectionInfo> GetActiveConnections();
    Task<bool> IsDohEnabledAsync();
    Task<bool> SetDohSettingsAsync(bool enable, string primaryDns, string secondaryDns, string dohTemplate);
    string ExportSocketsToCsv(IEnumerable<ActiveConnectionInfo> connections);
    string ExportDiagnosticReport(
        string internetStatus,
        string publicIp,
        string gateway,
        string dnsStatus,
        string ipStatus,
        int healthScore,
        string healthSummary,
        string healthRecommendation,
        double latencyMs,
        double jitterMs,
        double packetLoss,
        double downloadSpeed,
        double uploadSpeed,
        IEnumerable<NetworkAdapterInfo> adapters,
        IEnumerable<DnsServerInfo> dnsServers,
        IEnumerable<ActiveConnectionInfo> connections);
}

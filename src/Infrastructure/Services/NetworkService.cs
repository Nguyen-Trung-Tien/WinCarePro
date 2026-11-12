using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCarePro.Services.Contracts;
using WinCarePro.Engines;
using WinCarePro.Models;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Services.Implementations;

public class NetworkService : INetworkService
{
    private readonly NetworkEngine _engine;

    public event Action<string>? OutputReceived;

    public NetworkService() : this(App.Services?.GetService<NetworkEngine>() ?? new NetworkEngine())
    {
    }

    public NetworkService(NetworkEngine engine)
    {
        _engine = engine;
        _engine.OutputReceived += msg => OutputReceived?.Invoke(msg);
    }

    public bool CheckInternetConnection() => _engine.CheckInternetConnection();

    public string GetGatewayAddress() => _engine.GetGatewayAddress();

    public bool CheckGatewayReachability() => _engine.CheckGatewayReachability();

    public bool CheckDnsResolution() => _engine.CheckDnsResolution();
    
    public Task<bool> CheckDnsResolutionAsync(System.Threading.CancellationToken cancellationToken = default) => 
        _engine.CheckDnsResolutionAsync(cancellationToken);

    public (bool ipv4, bool ipv6) CheckIpStatus() => _engine.CheckIpStatus();

    public Task<(double packetLossPercent, double avgLatencyMs, double jitterMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5)
    {
        return _engine.AnalyzePingQualityAsync(target, count);
    }

    public Task RunPingTestAsync(string host, int count = 4, System.Threading.CancellationToken cancellationToken = default)
    {
        return _engine.RunPingTestAsync(host, count, cancellationToken);
    }

    public Task RunTracerouteAsync(string host, int maxHops = 30, System.Threading.CancellationToken cancellationToken = default)
    {
        return _engine.RunTracerouteAsync(host, maxHops, cancellationToken);
    }

    public Task RunDnsLookupAsync(string host, System.Threading.CancellationToken cancellationToken = default)
    {
        return _engine.RunDnsLookupAsync(host, cancellationToken);
    }

    public Task RunPortScanAsync(string host, int[] ports, System.Threading.CancellationToken cancellationToken = default)
    {
        return _engine.RunPortScanAsync(host, ports, cancellationToken);
    }

    public Task<double> RunSpeedTestAsync(Action<double, double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return _engine.RunSpeedTestAsync(progressCallback, cancellationToken);
    }

    public Task<double> RunUploadSpeedTestAsync(Action<double, double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        return _engine.RunUploadSpeedTestAsync(progressCallback, cancellationToken);
    }

    public Task<bool> FlushDnsAsync() => _engine.FlushDnsAsync();

    public Task<bool> ResetWinsockAsync() => _engine.ResetWinsockAsync();

    public Task<bool> ResetTcpIpAsync() => _engine.ResetTcpIpAsync();

    public Task<bool> ReleaseRenewIpAsync() => _engine.ReleaseRenewIpAsync();

    public Task<bool> ResetFirewallAsync() => _engine.ResetFirewallAsync();

    public Task<bool> ResetProxyAsync() => _engine.ResetProxyAsync();

    public Task<bool> RestartNetworkAdapterAsync() => _engine.RestartNetworkAdapterAsync();

    public Task<bool> ResetHostsFileAsync() => _engine.ResetHostsFileAsync();

    public Task<bool> OptimizeTcpAutoTuningAsync() => _engine.OptimizeTcpAutoTuningAsync();

    public Task<bool> DisableEnergyEfficientEthernetAsync() => _engine.DisableEnergyEfficientEthernetAsync();

    public List<NetworkAdapterInfo> GetNetworkAdapters() => _engine.GetNetworkAdapters();

    public Task<List<DnsServerInfo>> RunDnsBenchmarkAsync(System.Threading.CancellationToken cancellationToken = default) => _engine.RunDnsBenchmarkAsync(cancellationToken);

    public Task<bool> ApplyDnsSettingsAsync(string dnsName, string primaryIp, string secondaryIp, System.Threading.CancellationToken cancellationToken = default)
    {
        return _engine.ApplyDnsSettingsAsync(dnsName, primaryIp, secondaryIp, cancellationToken);
    }

    public List<ActiveConnectionInfo> GetActiveConnections() => _engine.GetActiveConnections();

    public Task<bool> IsDohEnabledAsync() => _engine.IsDohEnabledAsync();

    public Task<bool> SetDohSettingsAsync(bool enable, string primaryDns, string secondaryDns, string dohTemplate)
    {
        return _engine.SetDohSettingsAsync(enable, primaryDns, secondaryDns, dohTemplate);
    }

    public string ExportSocketsToCsv(IEnumerable<ActiveConnectionInfo> connections) => _engine.ExportSocketsToCsv(connections);

    public string ExportDiagnosticReport(
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
        IEnumerable<ActiveConnectionInfo> connections)
    {
        return _engine.ExportDiagnosticReport(
            internetStatus,
            publicIp,
            gateway,
            dnsStatus,
            ipStatus,
            healthScore,
            healthSummary,
            healthRecommendation,
            latencyMs,
            jitterMs,
            packetLoss,
            downloadSpeed,
            uploadSpeed,
            adapters,
            dnsServers,
            connections);
    }

    public NetworkAdapterInfo? GetActiveAdapterSummary() => _engine.GetActiveAdapterSummary();

    public (string? AdapterName, List<string> DnsServers, bool IsDhcp) BackupCurrentDns() => _engine.BackupCurrentDns();

    public bool HasDnsBackup => _engine.HasDnsBackup;

    public IReadOnlyList<DnsProfileOption> GetPresetDnsProfiles() => NetworkEngine.GetPresetDnsProfiles();

    public Task<bool> ApplyDnsProfileAsync(string profileName, string primaryDns, string secondaryDns, System.Threading.CancellationToken cancellationToken = default) =>
        _engine.ApplyDnsProfileAsync(profileName, primaryDns, secondaryDns, cancellationToken);

    public Task<bool> RestoreDnsBackupAsync(System.Threading.CancellationToken cancellationToken = default) =>
        _engine.RestoreDnsBackupAsync(cancellationToken);

    public Task<NetworkDiagnosticsResult> RunOnDemandDiagnosticsAsync(System.Threading.CancellationToken cancellationToken = default) =>
        _engine.RunOnDemandDiagnosticsAsync(cancellationToken);

    public Task<bool> ResetWinsockAndTcpIpAsync(System.Threading.CancellationToken cancellationToken = default) =>
        _engine.ResetWinsockAndTcpIpAsync(cancellationToken);
}

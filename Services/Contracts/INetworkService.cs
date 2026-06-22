using System;
using System.Threading.Tasks;

namespace WinCarePro.Services.Contracts;

public interface INetworkService
{
    event Action<string>? OutputReceived;
    bool CheckInternetConnection();
    string GetGatewayAddress();
    bool CheckGatewayReachability();
    bool CheckDnsResolution();
    (bool ipv4, bool ipv6) CheckIpStatus();
    Task<(double packetLossPercent, double avgLatencyMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5);
    Task RunPingTestAsync(string host, int count = 4);
    Task RunTracerouteAsync(string host, int maxHops = 30);
    Task RunDnsLookupAsync(string host);
    Task RunPortScanAsync(string host, int[] ports);
    Task<double> RunSpeedTestAsync();
    Task<bool> FlushDnsAsync();
    Task<bool> ResetWinsockAsync();
    Task<bool> ResetTcpIpAsync();
    Task<bool> ReleaseRenewIpAsync();
    Task<bool> ResetFirewallAsync();
    Task<bool> ResetProxyAsync();
    Task<bool> RestartNetworkAdapterAsync();
}

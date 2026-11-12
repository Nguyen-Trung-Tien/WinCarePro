using System;

namespace WinCarePro.Models;

public class NetworkDiagnosticsResult
{
    public long GatewayLatencyMs { get; set; } = -1;
    public long CloudflareLatencyMs { get; set; } = -1;
    public long GoogleLatencyMs { get; set; } = -1;
    public bool IsInternetAvailable { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string GatewayLatencyText => GatewayLatencyMs >= 0 ? $"{GatewayLatencyMs} ms" : "Unreachable";
    public string CloudflareLatencyText => CloudflareLatencyMs >= 0 ? $"{CloudflareLatencyMs} ms" : "Timeout";
    public string GoogleLatencyText => GoogleLatencyMs >= 0 ? $"{GoogleLatencyMs} ms" : "Timeout";
}


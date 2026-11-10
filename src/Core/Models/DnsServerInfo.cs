using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class DnsServerInfo
{
    public string Name { get; set; } = "";
    public string DisplayName => Name.T();
    public string PrimaryIp { get; set; } = "";
    public string SecondaryIp { get; set; } = "";
    public double PingMs { get; set; } = -1;
    public bool IsFastest { get; set; }
    public string PingFormatted => PingMs < 0 ? "Timeout".T() : $"{PingMs:F0} ms";
    public string LatencyFormatted => PingFormatted;
    public string Provider => Name;

    // New optimized DNS benchmark fields
    public double AverageQueryMs { get; set; } = -1;
    public double MinQueryMs { get; set; } = -1;
    public double MaxQueryMs { get; set; } = -1;
    public double PacketLossPercent { get; set; } = 0;
    public double ReliabilityScore { get; set; } = 100;
    public DateTime LastBenchmarkTime { get; set; } = DateTime.Now;
}

using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class NetworkAdapterInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string Type { get; set; } = "";
    public string Speed { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string IpAddresses { get; set; } = "";
    public string StatusColor => Status == "Up" ? "MediumSeaGreen" : "Tomato";
    public string StatusGlyph => Status == "Up" ? "\uE73E" : "\uF140";
    public string DisplayStatus => Status.T();
    public string DisplaySpeed => Speed.T();

    // New optimized telemetry fields
    public string CurrentDnsServers { get; set; } = "";
    public double LatencyMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }
    public string GatewayAddress { get; set; } = "";
    public string AdapterSpeed { get; set; } = "";
    public string IPv6Address { get; set; } = "";
    public string PublicIPAddress { get; set; } = "";
}

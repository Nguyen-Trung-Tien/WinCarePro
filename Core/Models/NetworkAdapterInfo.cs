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
    public string IpAddress => IpAddresses;
    public string StatusColor => Status == "Up" ? "MediumSeaGreen" : "Tomato";
    public string StatusGlyph => Status == "Up" ? "\uE73E" : "\uF140";
    public string DisplayStatus => Status.T();
    public string DisplaySpeed => Speed.T();

    private static Microsoft.UI.Xaml.Media.Brush? _upBgBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _downBgBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _upFgBrush;
    private static Microsoft.UI.Xaml.Media.Brush? _downFgBrush;

    public Microsoft.UI.Xaml.Media.Brush? StatusBadgeBg
    {
        get
        {
            try
            {
                return Status == "Up"
                    ? (_upBgBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 16, 185, 129)))
                    : (_downBgBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 239, 68, 68)));
            }
            catch
            {
                return null;
            }
        }
    }

    public Microsoft.UI.Xaml.Media.Brush? StatusBadgeFg
    {
        get
        {
            try
            {
                return Status == "Up"
                    ? (_upFgBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)))
                    : (_downFgBrush ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)));
            }
            catch
            {
                return null;
            }
        }
    }

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

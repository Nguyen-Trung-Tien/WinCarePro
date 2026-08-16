using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class ActiveConnectionInfo
{
    public string Protocol { get; set; } = "";
    public string LocalAddress { get; set; } = "";
    public string ForeignAddress { get; set; } = "";
    public string RemoteAddress => ForeignAddress;
    public string State { get; set; } = "";
    public string DisplayState => State.T();
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
    public string ProcessId => $"PID: {Pid}";

    public Microsoft.UI.Xaml.Media.Brush StateBadgeBg => (State?.ToUpper() == "ESTABLISHED" || State?.ToUpper() == "LISTEN" || State?.ToUpper() == "LISTENING")
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(35, 139, 92, 246));

    public Microsoft.UI.Xaml.Media.Brush StateBadgeFg => (State?.ToUpper() == "ESTABLISHED" || State?.ToUpper() == "LISTEN" || State?.ToUpper() == "LISTENING")
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246));
}

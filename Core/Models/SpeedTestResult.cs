using System;

namespace WinCarePro.Models;

public class SpeedTestResult
{
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public double PingMs { get; set; }
    public double JitterMs { get; set; }
    public string ServerName { get; set; } = "";
    public double TestDuration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

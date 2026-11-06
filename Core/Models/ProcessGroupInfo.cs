using System;

namespace WinCarePro.Models;

/// <summary>
/// Aggregated network connection telemetry per application process.
/// </summary>
public class ProcessGroupInfo
{
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
    public int ConnectionCount { get; set; }
    public string StateSummary { get; set; } = "ESTABLISHED";
    public string IconGlyph { get; set; } = "\uE71D";
    public string Description { get; set; } = "";
    public string DisplayCount => $"{ConnectionCount} sockets";
}


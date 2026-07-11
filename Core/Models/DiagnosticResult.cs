using System;

namespace WinCarePro.Models;

public class DiagnosticResult
{
    public string CheckName { get; set; } = "";
    public string Category { get; set; } = ""; // Performance, Storage, Network, Security, Software
    public bool IsHealthy { get; set; } = true;
    public string Description { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

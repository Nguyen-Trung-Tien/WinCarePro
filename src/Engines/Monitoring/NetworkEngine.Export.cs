using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    public string ExportSocketsToCsv(IEnumerable<ActiveConnectionInfo> connections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Process Name,PID,Protocol,Local Address,Remote Address,State");
        if (connections != null)
        {
            foreach (var conn in connections)
            {
                string pName = EscapeCsv(conn.ProcessName);
                string proto = EscapeCsv(conn.Protocol);
                string local = EscapeCsv(conn.LocalAddress);
                string remote = EscapeCsv(conn.ForeignAddress);
                string state = EscapeCsv(conn.State);
                sb.AppendLine($"{pName},{conn.Pid},{proto},{local},{remote},{state}");
            }
        }
        return sb.ToString();
    }

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
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("           WINCARE PRO SUITE — NETWORK DIAGNOSTICS & TELEMETRY REPORT");
        sb.AppendLine($"           Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | OS: Windows 10/11 x64");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine("--- 1. CONNECTIVITY & HEALTH ASSESSMENT ---");
        sb.AppendLine($"Internet Status:         {internetStatus}");
        sb.AppendLine($"Public IP Address:       {publicIp}");
        sb.AppendLine($"Default Gateway:         {gateway}");
        sb.AppendLine($"DNS Resolution:          {dnsStatus}");
        sb.AppendLine($"IP Stack Status:         {ipStatus}");
        sb.AppendLine($"AI Health Score:         {healthScore}/100");
        sb.AppendLine($"Health Summary:          {healthSummary}");
        sb.AppendLine($"AI Recommendation:       {healthRecommendation}");
        sb.AppendLine();
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        sb.AppendLine("--- 2. REAL-TIME TELEMETRY & SPEED ---");
        sb.AppendLine($"Round-Trip Latency:      {latencyMs.ToString("F1", inv)} ms");
        sb.AppendLine($"Jitter Variation:        {jitterMs.ToString("F1", inv)} ms");
        sb.AppendLine($"Packet Loss Rate:        {packetLoss.ToString("F1", inv)} %");
        sb.AppendLine($"Download Throughput:     {downloadSpeed.ToString("F2", inv)} Mbps");
        sb.AppendLine($"Upload Throughput:       {uploadSpeed.ToString("F2", inv)} Mbps");
        sb.AppendLine();
        sb.AppendLine("--- 3. NETWORK ADAPTERS ---");
        if (adapters != null)
        {
            foreach (var a in adapters)
            {
                sb.AppendLine($"• [{a.Status}] {a.Name} ({a.Type})");
                sb.AppendLine($"  IPv4: {a.IpAddress} | MAC: {a.MacAddress} | Speed: {a.Speed}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("--- 4. DNS BENCHMARK SERVERS ---");
        if (dnsServers != null)
        {
            foreach (var d in dnsServers)
            {
                string tag = d.IsFastest ? "[FASTEST]" : "";
                sb.AppendLine($"• {d.Name} ({d.PrimaryIp}) - Avg: {d.AverageQueryMs.ToString("F1", inv)} ms {tag}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("--- 5. ACTIVE NETWORK SOCKETS SUMMARY ---");
        if (connections != null)
        {
            var list = connections.ToList();
            sb.AppendLine($"Total Active Sockets: {list.Count}");
            var topApps = list.Where(c => !string.IsNullOrWhiteSpace(c.ProcessName) && c.Pid > 4)
                              .GroupBy(c => c.ProcessName)
                              .OrderByDescending(g => g.Count())
                              .Take(10);
            foreach (var g in topApps)
            {
                sb.AppendLine($"  • {g.Key}: {g.Count()} sockets");
            }
        }
        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine("End of WinCare Pro Network Diagnostics Report");
        sb.AppendLine("================================================================================");
        return sb.ToString();
    }

    public static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return $"\"{value}\"";
    }
}

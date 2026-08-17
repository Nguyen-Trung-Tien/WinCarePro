using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    public async Task<List<DnsServerInfo>> RunDnsBenchmarkAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        var dnsList = new List<DnsServerInfo>
        {
            new() { Name = "Cloudflare DNS", PrimaryIp = "1.1.1.1", SecondaryIp = "1.0.0.1" },
            new() { Name = "Google Public DNS", PrimaryIp = "8.8.8.8", SecondaryIp = "8.8.4.4" },
            new() { Name = "Quad9 DNS", PrimaryIp = "9.9.9.9", SecondaryIp = "149.112.112.112" },
            new() { Name = "OpenDNS", PrimaryIp = "208.67.222.222", SecondaryIp = "208.67.220.220" },
            new() { Name = "AdGuard DNS", PrimaryIp = "94.140.14.14", SecondaryIp = "94.140.15.15" }
        };

        if (cancellationToken.IsCancellationRequested) return dnsList;

        Log("Starting true DNS resolution latency benchmark (resolving domains)...");
        var testDomains = new[] { "google.com", "cloudflare.com", "microsoft.com" };
        var tasks = new List<Task>();

        foreach (var dns in dnsList)
        {
            tasks.Add(Task.Run(async () =>
            {
                int runs = 5;
                int successfulQueries = 0;
                double totalMs = 0;
                double minMs = double.MaxValue;
                double maxMs = double.MinValue;

                for (int run = 0; run < runs; run++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    string domain = testDomains[run % testDomains.Length];
                    double time = await MeasureDnsResolutionTimeAsync(dns.PrimaryIp, domain, cancellationToken);
                    
                    if (time >= 0)
                    {
                        successfulQueries++;
                        totalMs += time;
                        if (time < minMs) minMs = time;
                        if (time > maxMs) maxMs = time;
                    }
                    
                    try { await Task.Delay(50, cancellationToken); } catch { return; }
                }

                if (successfulQueries > 0)
                {
                    dns.AverageQueryMs = totalMs / successfulQueries;
                    dns.MinQueryMs = minMs;
                    dns.MaxQueryMs = maxMs;
                    dns.PingMs = dns.AverageQueryMs; // Backwards compatibility mapping
                }
                else
                {
                    dns.AverageQueryMs = -1;
                    dns.MinQueryMs = -1;
                    dns.MaxQueryMs = -1;
                    dns.PingMs = -1;
                }

                dns.PacketLossPercent = ((double)(runs - successfulQueries) / runs) * 100.0;
                dns.ReliabilityScore = ((double)successfulQueries / runs) * 100.0;
                dns.LastBenchmarkTime = DateTime.Now;

            }, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exception to return partial results
        }

        if (cancellationToken.IsCancellationRequested) return dnsList;

        double minAvg = double.MaxValue;
        DnsServerInfo? fastest = null;
        foreach (var dns in dnsList)
        {
            if (dns.AverageQueryMs >= 0 && dns.AverageQueryMs < minAvg)
            {
                minAvg = dns.AverageQueryMs;
                fastest = dns;
            }
        }

        if (fastest != null)
        {
            fastest.IsFastest = true;
            if (!cancellationToken.IsCancellationRequested)
            {
                Log($"DNS Benchmark complete. Fastest: {fastest.Name} (Avg: {fastest.AverageQueryMs:F0}ms, Reliability: {fastest.ReliabilityScore}%)");
            }
        }
        else
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log("DNS Benchmark complete. No DNS servers responded to resolution queries.");
            }
        }

        return dnsList;
    }

    public async Task<bool> ApplyDnsSettingsAsync(string dnsName, string primaryIp, string secondaryIp)
    {
        Log($"Applying DNS settings for {dnsName} ({primaryIp}, {secondaryIp})...");
        try
        {
            string script;
            bool isDhcp = string.IsNullOrWhiteSpace(primaryIp) || 
                          dnsName.Contains("DHCP", StringComparison.OrdinalIgnoreCase) || 
                          dnsName.Contains("Automatic", StringComparison.OrdinalIgnoreCase);

            if (isDhcp)
            {
                script = "$route = Get-NetRoute -DestinationPrefix '0.0.0.0/0' | Sort-Object RouteMetric | Select-Object -First 1; " +
                         "if ($route) { " +
                         "  Set-DnsClientServerAddress -InterfaceIndex $route.InterfaceIndex -ResetServerAddresses; " +
                         "} else { " +
                         "  $adapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' }; " +
                         "  foreach ($adapter in $adapters) { " +
                         "    Set-DnsClientServerAddress -InterfaceAlias $adapter.Name -ResetServerAddresses; " +
                         "  } " +
                         "}";
            }
            else
            {
                script = "$route = Get-NetRoute -DestinationPrefix '0.0.0.0/0' | Sort-Object RouteMetric | Select-Object -First 1; " +
                         "if ($route) { " +
                         "  Set-DnsClientServerAddress -InterfaceIndex $route.InterfaceIndex -ServerAddresses ('" + primaryIp + "', '" + secondaryIp + "'); " +
                         "} else { " +
                         "  $adapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' }; " +
                         "  foreach ($adapter in $adapters) { " +
                         "    Set-DnsClientServerAddress -InterfaceAlias $adapter.Name -ServerAddresses ('" + primaryIp + "', '" + secondaryIp + "'); " +
                         "  } " +
                         "}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0)
                {
                    Log($"DNS configured successfully to {dnsName}.");
                    return true;
                }
                else
                {
                    Log($"Failed to configure DNS. PowerShell exited with code {proc.ExitCode}.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to configure DNS: {ex.Message}");
        }
        return false;
    }

    private static byte[] CreateDnsQueryPacket(string domain)
    {
        var header = new byte[] {
            0x12, 0x34, // ID
            0x01, 0x00, // Flags (standard query)
            0x00, 0x01, // Questions = 1
            0x00, 0x00, // Answers = 0
            0x00, 0x00, // Authority = 0
            0x00, 0x00  // Additional = 0
        };

        var nameBytes = new List<byte>();
        var parts = domain.Split('.');
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                nameBytes.Add((byte)part.Length);
                nameBytes.AddRange(System.Text.Encoding.ASCII.GetBytes(part));
            }
        }
        nameBytes.Add(0x00); // Terminating byte

        var typeAndClass = new byte[] {
            0x00, 0x01, // Type A
            0x00, 0x01  // Class IN
        };

        var packet = new byte[header.Length + nameBytes.Count + typeAndClass.Length];
        Buffer.BlockCopy(header, 0, packet, 0, header.Length);
        Buffer.BlockCopy(nameBytes.ToArray(), 0, packet, header.Length, nameBytes.Count);
        Buffer.BlockCopy(typeAndClass, 0, packet, header.Length + nameBytes.Count, typeAndClass.Length);

        return packet;
    }

    private static async Task<double> MeasureDnsResolutionTimeAsync(string dnsServerIp, string domain, System.Threading.CancellationToken cancellationToken, int timeoutMs = 1500)
    {
        var packet = CreateDnsQueryPacket(domain);
        using var udpClient = new UdpClient();
        
        try
        {
            udpClient.Client.SendTimeout = timeoutMs;
            udpClient.Client.ReceiveTimeout = timeoutMs;
            var ipEndpoint = new IPEndPoint(IPAddress.Parse(dnsServerIp), 53);
            
            var stopwatch = Stopwatch.StartNew();
            await udpClient.SendAsync(packet, packet.Length, ipEndpoint);
            
            var receiveTask = udpClient.ReceiveAsync(cancellationToken).AsTask();
            var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (completedTask == receiveTask)
            {
                stopwatch.Stop();
                var result = await receiveTask;
                if (result.Buffer.Length > 12 && result.Buffer[0] == 0x12 && result.Buffer[1] == 0x34)
                {
                    return stopwatch.Elapsed.TotalMilliseconds;
                }
            }
        }
        catch
        {
            // Ignored
        }
        return -1;
    }
}

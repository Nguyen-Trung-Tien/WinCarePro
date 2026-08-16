using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    public event Action<string>? OutputReceived;
    private void Log(string msg) => OutputReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");

    // Connectivity checks
    public bool CheckInternetConnection()
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send("8.8.8.8", 2000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public string GetGatewayAddress()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var props = ni.GetIPProperties();
                    foreach (var gateway in props.GatewayAddresses)
                    {
                        return gateway.Address.ToString();
                    }
                }
            }
        }
        catch { }
        return "Unknown";
    }

    public bool CheckGatewayReachability()
    {
        string gw = GetGatewayAddress();
        if (gw == "Unknown") return false;
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(gw, 2000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public bool CheckDnsResolution()
    {
        try
        {
            var ips = Dns.GetHostAddresses("google.com");
            return ips.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public (bool ipv4, bool ipv6) CheckIpStatus()
    {
        bool ipv4 = false;
        bool ipv6 = false;
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var props = ni.GetIPProperties();
                    foreach (var unicast in props.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork) ipv4 = true;
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetworkV6) ipv6 = true;
                    }
                }
            }
        }
        catch { }
        return (ipv4, ipv6);
    }

    public async Task<(double packetLossPercent, double avgLatencyMs, double jitterMs)> AnalyzePingQualityAsync(string target = "8.8.8.8", int count = 5)
    {
        int packetsSent = 0;
        int packetsReceived = 0;
        double totalRoundtripTime = 0;
        var rttList = new List<double>();

        using var ping = new Ping();
        for (int i = 0; i < count; i++)
        {
            try
            {
                packetsSent++;
                var reply = await ping.SendPingAsync(target, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    packetsReceived++;
                    double rtt = reply.RoundtripTime;
                    totalRoundtripTime += rtt;
                    rttList.Add(rtt);
                }
            }
            catch { }
            await Task.Delay(100);
        }

        if (packetsSent == 0) return (100.0, 0.0, 0.0);
        double packetLoss = ((double)(packetsSent - packetsReceived) / packetsSent) * 100.0;
        double avgLatency = packetsReceived > 0 ? totalRoundtripTime / packetsReceived : 0.0;

        double jitter = 0.0;
        if (packetsReceived > 1)
        {
            double sumOfSquares = 0;
            foreach (var rtt in rttList)
            {
                sumOfSquares += Math.Pow(rtt - avgLatency, 2);
            }
            jitter = Math.Sqrt(sumOfSquares / (packetsReceived - 1));
        }

        return (packetLoss, avgLatency, jitter);
    }

    // Diagnostics Tools
    public async Task RunPingTestAsync(string host, int count = 4)
    {
        Log($"Ping test to {host} ({count} packets):");
        using var ping = new Ping();
        for (int i = 0; i < count; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(host, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    Log($"Reply from {reply.Address}: bytes=32 time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
                }
                else
                {
                    Log($"Ping failed: {reply.Status}");
                }
            }
            catch (Exception ex)
            {
                Log($"Ping error: {ex.Message}");
            }
            await Task.Delay(500);
        }
    }

    public async Task RunTracerouteAsync(string host, int maxHops = 30)
    {
        Log($"Traceroute to {host} (max {maxHops} hops):");
        using var ping = new Ping();
        var options = new PingOptions(1, true);

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            options.Ttl = ttl;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var reply = await ping.SendPingAsync(host, 3000, new byte[32], options);
                stopwatch.Stop();

                if (reply.Status == IPStatus.Success)
                {
                    Log($"{ttl}\t{stopwatch.ElapsedMilliseconds} ms\t{reply.Address} [Reached Target]");
                    break;
                }
                else if (reply.Status == IPStatus.TtlExpired)
                {
                    Log($"{ttl}\t{stopwatch.ElapsedMilliseconds} ms\t{reply.Address}");
                }
                else
                {
                    Log($"{ttl}\t*\tRequest timed out.");
                }
            }
            catch (Exception ex)
            {
                Log($"{ttl}\t*\tError: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        Log("Traceroute complete.");
    }

    public async Task RunDnsLookupAsync(string host)
    {
        Log($"DNS Lookup for: {host}...");
        try
        {
            var ips = await Dns.GetHostAddressesAsync(host);
            foreach (var ip in ips)
            {
                Log($"Found IP Address: {ip} (Family: {ip.AddressFamily})");
            }
        }
        catch (Exception ex)
        {
            Log($"DNS Lookup error: {ex.Message}");
        }
    }

    public async Task RunPortScanAsync(string host, int[] ports)
    {
        Log($"Starting Port Scan on {host} for {ports.Length} ports...");
        foreach (var port in ports)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(1000);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == connectTask)
                {
                    Log($"Port {port}: OPEN");
                }
                else
                {
                    Log($"Port {port}: CLOSED (Timeout)");
                }

            }
            catch
            {
                Log($"Port {port}: CLOSED");
            }
        }
        Log("Port scan finished.");
    }

    public List<NetworkAdapterInfo> GetNetworkAdapters()
    {
        var list = new List<NetworkAdapterInfo>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                // Filter out virtual Windows miniports, QoS packet schedulers, and WFP lightweight filters
                string desc = ni.Description ?? "";
                string name = ni.Name ?? "";
                if (desc.Contains("Filter", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Scheduler", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("LightWeight", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("QoS", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Kernel Debug", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Filter", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Scheduler", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Virtual Adapter", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Kernel Debug", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Local Area Connection*", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var ipList = new List<string>();
                try
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipList.Add(addr.Address.ToString());
                        }
                    }
                }
                catch { }

                string speedStr = "Unknown";
                if (ni.Speed > 0)
                {
                    double speedGbps = ni.Speed / 1_000_000_000.0;
                    if (speedGbps >= 1.0)
                        speedStr = $"{speedGbps:F1} Gbps";
                    else
                        speedStr = $"{ni.Speed / 1_000_000.0:F0} Mbps";
                }

                string mac = ni.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
                {
                    mac = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                }

                list.Add(new NetworkAdapterInfo
                {
                    Name = ni.Name ?? "",
                    Description = ni.Description ?? "",
                    Status = ni.OperationalStatus.ToString(),
                    Type = ni.NetworkInterfaceType.ToString(),
                    Speed = speedStr,
                    MacAddress = string.IsNullOrEmpty(mac) ? "N/A" : mac,
                    IpAddresses = ipList.Count > 0 ? string.Join(", ", ipList) : "No IPv4"
                });
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to retrieve adapters: {ex.Message}");
        }
        return list;
    }

    public List<ActiveConnectionInfo> GetActiveConnections()
    {
        var list = new List<ActiveConnectionInfo>();
        try
        {
            var procDict = Process.GetProcesses().ToDictionary(p => p.Id, p => p.ProcessName);

            var psi = new ProcessStartInfo
            {
                FileName = "netstat.exe",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string? line;
                while ((line = proc.StandardOutput.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("Proto") || line.StartsWith("Active")) continue;

                    var parts = Regex.Split(line, @"\s+");
                    if (parts.Length >= 4)
                    {
                        string proto = parts[0];
                        string local = parts[1];
                        string foreign = parts[2];
                        string state = "";
                        string pidStr = "";

                        if (proto.ToUpper() == "TCP")
                        {
                            if (parts.Length >= 5)
                            {
                                state = parts[3];
                                pidStr = parts[4];
                            }
                        }
                        else
                        {
                            state = "-";
                            pidStr = parts[3];
                        }

                        if (int.TryParse(pidStr, out int pid))
                        {
                            procDict.TryGetValue(pid, out string? processName);
                            processName ??= "System / Unknown";

                            list.Add(new ActiveConnectionInfo
                            {
                                Protocol = proto,
                                LocalAddress = local,
                                ForeignAddress = foreign,
                                State = state,
                                ProcessName = processName,
                                Pid = pid
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to retrieve active connections: {ex.Message}");
        }
        return list;
    }
}

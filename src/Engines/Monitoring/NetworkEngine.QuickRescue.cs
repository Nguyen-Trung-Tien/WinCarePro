using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    private (string? AdapterName, List<string> DnsServers, bool IsDhcp)? _lastDnsBackup;

    public bool HasDnsBackup => _lastDnsBackup.HasValue;

    public static IReadOnlyList<DnsProfileOption> GetPresetDnsProfiles()
    {
        return new List<DnsProfileOption>
        {
            new()
            {
                Id = "cloudflare",
                Name = "Cloudflare DNS",
                Provider = "Cloudflare",
                PrimaryDns = "1.1.1.1",
                SecondaryDns = "1.0.0.1",
                Description = "Fastest privacy-first DNS with 1.1.1.1 & 1.0.0.1."
            },
            new()
            {
                Id = "google",
                Name = "Google Public DNS",
                Provider = "Google",
                PrimaryDns = "8.8.8.8",
                SecondaryDns = "8.8.4.4",
                Description = "Global robust DNS resolution from Google."
            },
            new()
            {
                Id = "adguard",
                Name = "AdGuard DNS",
                Provider = "AdGuard",
                PrimaryDns = "94.140.14.14",
                SecondaryDns = "94.140.15.15",
                Description = "Blocks intrusive ads, trackers and phishing domains."
            },
            new()
            {
                Id = "quad9",
                Name = "Quad9 DNS",
                Provider = "Quad9",
                PrimaryDns = "9.9.9.9",
                SecondaryDns = "149.112.112.112",
                Description = "Security-focused DNS blocking malicious domains."
            },
            new()
            {
                Id = "dhcp",
                Name = "Automatic (DHCP)",
                Provider = "Router / ISP",
                PrimaryDns = "",
                SecondaryDns = "",
                Description = "Restores automatic DNS assigned by local router DHCP."
            }
        };
    }

    public NetworkAdapterInfo? GetActiveAdapterSummary()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            if (interfaces.Count == 0) return null;

            NetworkInterface? active = null;
            // Prefer interface with a default gateway
            foreach (var ni in interfaces)
            {
                var props = ni.GetIPProperties();
                if (props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                {
                    active = ni;
                    break;
                }
            }

            active ??= interfaces.FirstOrDefault();
            if (active == null) return null;

            var ipProperties = active.GetIPProperties();
            string ipv4 = "N/A";
            string subnet = "N/A";
            foreach (var unicast in ipProperties.UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipv4 = unicast.Address.ToString();
                    subnet = unicast.IPv4Mask?.ToString() ?? "255.255.255.0";
                    break;
                }
            }

            string gateway = ipProperties.GatewayAddresses
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "N/A";

            var dnsList = ipProperties.DnsAddresses
                .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                .Select(d => d.ToString())
                .ToList();

            string dnsText = dnsList.Count > 0 ? string.Join(", ", dnsList) : "DHCP (Automatic)";

            string mac = active.GetPhysicalAddress().ToString();
            if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
            {
                mac = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
            }

            string speedStr = "Unknown";
            if (active.Speed > 0)
            {
                double speedGbps = active.Speed / 1_000_000_000.0;
                if (speedGbps >= 1.0)
                    speedStr = $"{speedGbps:F1} Gbps";
                else
                    speedStr = $"{active.Speed / 1_000_000.0:F0} Mbps";
            }

            return new NetworkAdapterInfo
            {
                Name = active.Name ?? "Ethernet",
                Description = active.Description ?? "",
                Status = active.OperationalStatus.ToString(),
                Type = active.NetworkInterfaceType.ToString(),
                Speed = speedStr,
                IpAddresses = ipv4,
                SubnetMask = subnet,
                GatewayAddress = gateway,
                CurrentDnsServers = dnsText,
                MacAddress = string.IsNullOrEmpty(mac) ? "N/A" : mac
            };
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkEngine.GetActiveAdapterSummary", ex);
            Log($"Failed to retrieve active adapter: {ex.Message}");
            return null;
        }
    }

    public (string? AdapterName, List<string> DnsServers, bool IsDhcp) BackupCurrentDns()
    {
        try
        {
            var summary = GetActiveAdapterSummary();
            string adapterName = summary?.Name ?? "Active Adapter";
            var currentDnsList = new List<string>();

            if (summary != null && !string.IsNullOrWhiteSpace(summary.CurrentDnsServers) &&
                !summary.CurrentDnsServers.Contains("DHCP", StringComparison.OrdinalIgnoreCase))
            {
                currentDnsList = summary.CurrentDnsServers
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => IPAddress.TryParse(s, out _))
                    .ToList();
            }

            bool isDhcp = currentDnsList.Count == 0;
            _lastDnsBackup = (adapterName, currentDnsList, isDhcp);

            Database.DbManager.LogAction(
                "Backup DNS",
                "Network Core",
                isDhcp ? $"Backed up DHCP configuration for {adapterName}" : $"Backed up DNS: {string.Join(", ", currentDnsList)} for {adapterName}");

            return _lastDnsBackup.Value;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkEngine.BackupCurrentDns", ex);
            return (null, new List<string>(), true);
        }
    }

    public async Task<bool> ApplyDnsProfileAsync(
        string profileName,
        string primaryDns,
        string secondaryDns,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Mandatory backup of current DNS before switching
        if (!_lastDnsBackup.HasValue)
        {
            BackupCurrentDns();
        }

        // 2. Apply DNS settings safely
        bool ok = await ApplyDnsSettingsAsync(profileName, primaryDns, secondaryDns, cancellationToken);
        Database.DbManager.LogAction(
            "DNS Switcher",
            "Network Core",
            ok ? $"Switched DNS to {profileName} ({primaryDns}/{secondaryDns})" : $"Failed to switch DNS to {profileName}");

        return ok;
    }

    public async Task<bool> RestoreDnsBackupAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_lastDnsBackup.HasValue)
        {
            Log("No DNS backup exists. Restoring DHCP default instead.");
            return await ApplyDnsSettingsAsync("DHCP (Automatic)", "", "", cancellationToken);
        }

        var backup = _lastDnsBackup.Value;
        if (backup.IsDhcp || backup.DnsServers.Count == 0)
        {
            Log($"Restoring DHCP DNS configuration for {backup.AdapterName}...");
            return await ApplyDnsSettingsAsync("DHCP (Automatic)", "", "", cancellationToken);
        }

        string primary = backup.DnsServers[0];
        string secondary = backup.DnsServers.Count > 1 ? backup.DnsServers[1] : "";
        Log($"Restoring previous DNS ({primary}, {secondary}) for {backup.AdapterName}...");
        bool ok = await ApplyDnsSettingsAsync("Restored DNS", primary, secondary, cancellationToken);

        Database.DbManager.LogAction(
            "Restore DNS Backup",
            "Network Core",
            ok ? $"Restored previous DNS: {primary}, {secondary}" : "Failed to restore previous DNS");

        return ok;
    }

    public async Task<NetworkDiagnosticsResult> RunOnDemandDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var result = new NetworkDiagnosticsResult();
        string gateway = GetGatewayAddress();

        using var ping = new Ping();

        // 1. Gateway Ping (local router)
        if (!string.IsNullOrWhiteSpace(gateway) && gateway != "Unknown")
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var gwReply = await ping.SendPingAsync(gateway, 1500);
                if (gwReply.Status == IPStatus.Success)
                {
                    result.GatewayLatencyMs = gwReply.RoundtripTime;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                CrashLogger.LogException("NetworkEngine.RunOnDemandDiagnosticsAsync.Gateway", ex);
            }
        }

        if (cancellationToken.IsCancellationRequested) return result;

        // 2. Cloudflare Ping (1.1.1.1)
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cfReply = await ping.SendPingAsync("1.1.1.1", 1500);
            if (cfReply.Status == IPStatus.Success)
            {
                result.CloudflareLatencyMs = cfReply.RoundtripTime;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkEngine.RunOnDemandDiagnosticsAsync.Cloudflare", ex);
        }

        if (cancellationToken.IsCancellationRequested) return result;

        // 3. Google Ping (8.8.8.8)
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var googleReply = await ping.SendPingAsync("8.8.8.8", 1500);
            if (googleReply.Status == IPStatus.Success)
            {
                result.GoogleLatencyMs = googleReply.RoundtripTime;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkEngine.RunOnDemandDiagnosticsAsync.Google", ex);
        }

        result.IsInternetAvailable = result.CloudflareLatencyMs >= 0 || result.GoogleLatencyMs >= 0;
        return result;
    }

    public async Task<bool> ResetWinsockAndTcpIpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Log("Initiating Winsock and TCP/IP stack complete reset...");

        bool okWinsock = await ResetWinsockAsync();
        cancellationToken.ThrowIfCancellationRequested();

        bool okTcp = await ResetTcpIpAsync();

        bool overall = okWinsock && okTcp;
        Database.DbManager.LogAction(
            "Reset Winsock & TCP/IP",
            "Network Repair",
            overall ? "Success (Requires Reboot)" : "Partial/Failed");

        return overall;
    }
}


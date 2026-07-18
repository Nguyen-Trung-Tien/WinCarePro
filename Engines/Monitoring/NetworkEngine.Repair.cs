using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Engines;

public partial class NetworkEngine
{
    public async Task<bool> FlushDnsAsync()
    {
        Log("Flushing DNS cache...");
        bool ok = await RunProcessAsync("ipconfig.exe", "/flushdns");
        Database.DbManager.LogAction("Flush DNS", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> ResetWinsockAsync()
    {
        Log("Resetting Winsock Catalog (requires restart)...");
        bool ok = await RunProcessAsync("netsh.exe", "winsock reset");
        Database.DbManager.LogAction("Reset Winsock", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> ResetTcpIpAsync()
    {
        Log("Resetting TCP/IP stack...");
        bool ok = await RunProcessAsync("netsh.exe", "int ip reset");
        Database.DbManager.LogAction("Reset TCP/IP", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> ReleaseRenewIpAsync()
    {
        Log("Releasing IP Address...");
        await RunProcessAsync("ipconfig.exe", "/release");
        await Task.Delay(1000);
        Log("Renewing IP Address...");
        bool ok = await RunProcessAsync("ipconfig.exe", "/renew");
        Database.DbManager.LogAction("Release/Renew IP", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> ResetFirewallAsync()
    {
        Log("Resetting Windows Firewall to defaults...");
        bool ok = await RunProcessAsync("netsh.exe", "advfirewall reset");
        Database.DbManager.LogAction("Reset Firewall", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> ResetProxyAsync()
    {
        Log("Resetting proxy settings...");
        try
        {
            // Reset WinHTTP proxy
            await RunProcessAsync("netsh.exe", "winhttp reset proxy");

            // Disable internet settings proxy
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);
            if (key != null)
            {
                key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                key.DeleteValue("ProxyServer", false);
                key.DeleteValue("ProxyOverride", false);
            }
            Log("Proxy settings cleared in Registry.");
            Database.DbManager.LogAction("Reset Proxy", "Network Repair", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to reset proxy: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestartNetworkAdapterAsync()
    {
        Log("Attempting network adapters restart...");
        try
        {
            // We run a powershell command to restart enabled network adapters
            var result = await ProcessRunner.RunAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Restart-NetAdapter -Confirm:$false\"",
                TimeSpan.FromSeconds(45),
                onOutput: Log,
                onError: Log
            );
            if (result.ExitCode == 0)
            {
                Log("Restart adapter command triggered via PowerShell.");
                Database.DbManager.LogAction("Restart Adapter", "Network Repair", "Success");
                return true;
            }
            else
            {
                Log($"Failed to restart adapter. PowerShell exited with code {result.ExitCode}.");
                Database.DbManager.LogAction("Restart Adapter", "Network Repair", "Failed");
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to restart adapter: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> RunProcessAsync(string filename, string arguments)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(
                filename,
                arguments,
                TimeSpan.FromSeconds(30),
                onOutput: Log,
                onError: Log
            );
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log($"Process error running {filename}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResetHostsFileAsync()
    {
        Log("Resetting Hosts file to system defaults...");
        try
        {
            string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc\\hosts");
            if (File.Exists(hostsPath))
            {
                string backupPath = hostsPath + ".bak";
                File.Copy(hostsPath, backupPath, true);
                Log($"Hosts file backup created at: {backupPath}");
            }

            string defaultHosts = "# Created by WinCare Pro Network Repair Tools\r\n" +
                                 "127.0.0.1       localhost\r\n" +
                                 "::1             localhost\r\n";
            await File.WriteAllTextAsync(hostsPath, defaultHosts);
            Log("Hosts file successfully reset to defaults.");
            Database.DbManager.LogAction("Reset Hosts File", "Network Repair", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to reset Hosts file (Requires Administrator privileges): {ex.Message}");
            Database.DbManager.LogAction("Reset Hosts File", "Network Repair", "Failed");
            return false;
        }
    }

    public async Task<bool> OptimizeTcpAutoTuningAsync()
    {
        Log("Optimizing TCP Window Auto-Tuning level...");
        bool ok = await RunProcessAsync("netsh.exe", "int tcp set global autotuninglevel=normal");
        Database.DbManager.LogAction("Optimize TCP AutoTuning", "Network Repair", ok ? "Success" : "Failed");
        return ok;
    }

    public async Task<bool> DisableEnergyEfficientEthernetAsync()
    {
        Log("Disabling network adapter energy saving features (Green/EEE)...");
        try
        {
            string script = "Get-NetAdapterAdvancedProperty | Where-Object { $_.DisplayName -like '*Energy*' -or $_.DisplayName -like '*Green*' -or $_.DisplayName -like '*Power Saving*' } | " +
                            "foreach { Set-NetAdapterAdvancedProperty -Name $_.InterfaceAlias -RegistryKeyword $_.RegistryKeyword -RegistryValue '0' -NoRestart; }";
            var result = await ProcessRunner.RunAsync(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                TimeSpan.FromSeconds(45),
                onOutput: Log,
                onError: Log
            );
            if (result.ExitCode == 0)
            {
                Log("Energy saving Ethernet properties set to Disabled via PowerShell.");
                Database.DbManager.LogAction("Disable Green Ethernet", "Network Repair", "Success");
                return true;
            }
            else
            {
                Log($"Failed to disable energy saving features. PowerShell exited with code {result.ExitCode}.");
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to disable energy saving features (Requires Administrator privileges): {ex.Message}");
        }
        Database.DbManager.LogAction("Disable Green Ethernet", "Network Repair", "Failed");
        return false;
    }

    public async Task<bool> IsDohEnabledAsync()
    {
        Log("Checking DNS over HTTPS (DoH) status...");
        try
        {
            var result = await ProcessRunner.RunAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"if (Get-Command Get-DnsClientDohServerAddress -ErrorAction SilentlyContinue) { Get-DnsClientDohServerAddress | Where-Object { $_.DohState -eq 'Enabled' -or $_.DohState -eq 'Required' } | ConvertTo-Json } else { write-output '' }\"",
                TimeSpan.FromSeconds(15)
            );
            return !string.IsNullOrWhiteSpace(result.Output) && result.Output.Contains("Enabled");
        }
        catch (Exception ex)
        {
            Log($"Failed to check DoH status: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetDohSettingsAsync(bool enable, string primaryDns, string secondaryDns, string dohTemplate)
    {
        string actionName = enable ? "Enable DoH" : "Disable DoH";
        Log(enable ? $"Enabling DNS over HTTPS with primary: {primaryDns}, template: {dohTemplate}..." : "Disabling DNS over HTTPS and resetting DNS to automatic (DHCP)...");
        
        try
        {
            string script;
            if (enable)
            {
                script = "$adapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' }; " +
                         $"foreach ($adapter in $adapters) {{ Set-DnsClientServerAddress -InterfaceIndex $adapter.InterfaceIndex -ServerAddresses ('{primaryDns}', '{secondaryDns}') }}; " +
                         "if (Get-Command Set-DnsClientDohServerAddress -ErrorAction SilentlyContinue) { " +
                         $"  Set-DnsClientDohServerAddress -ServerAddress '{primaryDns}' -DohTemplate '{dohTemplate}' -AllowFallbackToUdp $true -AutoUpgrade $true; " +
                         $"  Set-DnsClientDohServerAddress -ServerAddress '{secondaryDns}' -DohTemplate '{dohTemplate}' -AllowFallbackToUdp $true -AutoUpgrade $true; " +
                         "}";
            }
            else
            {
                script = "$adapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' }; " +
                         "foreach ($adapter in $adapters) { Set-DnsClientServerAddress -InterfaceIndex $adapter.InterfaceIndex -ResetServerAddresses }; " +
                         "if (Get-Command Clear-DnsClientDohServerAddress -ErrorAction SilentlyContinue) { Clear-DnsClientDohServerAddress }";
            }

            var result = await ProcessRunner.RunAsync(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                TimeSpan.FromSeconds(30),
                onOutput: Log,
                onError: Log
            );

            bool ok = result.ExitCode == 0;
            Database.DbManager.LogAction(actionName, "Network Center", ok ? "Success" : "Failed");
            return ok;
        }
        catch (Exception ex)
        {
            Log($"Failed to set DoH settings: {ex.Message}");
            Database.DbManager.LogAction(actionName, "Network Center", "Failed");
            return false;
        }
    }
}


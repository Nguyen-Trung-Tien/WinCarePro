using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

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
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Restart-NetAdapter -Confirm:$false\"",
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
                    Log("Restart adapter command triggered via PowerShell.");
                    Database.DbManager.LogAction("Restart Adapter", "Network Repair", "Success");
                    return true;
                }
                else
                {
                    Log($"Failed to restart adapter. PowerShell exited with code {proc.ExitCode}.");
                    Database.DbManager.LogAction("Restart Adapter", "Network Repair", "Failed");
                }
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
            var psi = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0;
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
                    Log("Energy saving Ethernet properties set to Disabled via PowerShell.");
                    Database.DbManager.LogAction("Disable Green Ethernet", "Network Repair", "Success");
                    return true;
                }
                else
                {
                    Log($"Failed to disable energy saving features. PowerShell exited with code {proc.ExitCode}.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to disable energy saving features (Requires Administrator privileges): {ex.Message}");
        }
        Database.DbManager.LogAction("Disable Green Ethernet", "Network Repair", "Failed");
        return false;
    }
}

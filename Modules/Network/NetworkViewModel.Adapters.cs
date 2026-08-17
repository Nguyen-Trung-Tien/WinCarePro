using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel
{
    public void LoadAdapters()
    {
        _ = Task.Run(() =>
        {
            try
            {
                var list = _engine.GetNetworkAdapters();
                // Get DNS for first interface — wrapped in separate try-catch
                // because System.Net.NetworkInformation assembly may not resolve
                // on some deployment configurations (FileNotFoundException crash fix)
                string dnsText = "Unknown";
                try
                {
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        {
                            var props = ni.GetIPProperties();
                            var dnsServers = props.DnsAddresses.Select(d => d.ToString()).ToList();
                            if (dnsServers.Count > 0)
                            {
                                dnsText = string.Join(", ", dnsServers);
                                break;
                            }
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    // System.Net.NetworkInformation assembly not found — graceful fallback
                    dnsText = "Unavailable";
                }
                catch (Exception dnsEx)
                {
                    dnsText = "Error";
                    LogText($"DNS detection failed: {dnsEx.Message}");
                }
                
                try
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        try
                        {
                            SyncAdapters(list);
                            CurrentDnsText = dnsText;
                        }
                        catch { }
                    });
                }
                catch { }
            }
            catch (Exception ex)
            {
                LogText($"Failed to load adapters: {ex.Message}");
            }
        });
    }

    private void SyncAdapters(List<NetworkAdapterInfo> newList)
    {
        Adapters.Clear();
        var sorted = newList
            .OrderByDescending(a => a.Status == "Up")
            .ThenByDescending(a => !a.Name.Contains("Filter") && !a.Name.Contains("Scheduler") && !a.Name.Contains("LightWeight"))
            .ToList();
        foreach (var item in sorted)
        {
            Adapters.Add(item);
        }
    }

    private bool _isLoadingConnections;
    public async Task LoadActiveConnectionsAsync(bool forceRefresh = false)
    {
        if (_rawConnections.Count > 0 && !forceRefresh) return;
        if (_isLoadingConnections) return;
        _isLoadingConnections = true;

        try
        {
            var list = await Task.Run(() => _engine.GetActiveConnections());
            if (_cts == null || _cts.IsCancellationRequested) return;
            _rawConnections = list;
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ApplyConnectionFilter();
            });
        }
        catch (Exception ex)
        {
            LogText($"Failed to load active connections: {ex.Message}");
        }
        finally
        {
            _isLoadingConnections = false;
        }
    }

    private void ApplyConnectionFilter()
    {
        try
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    var query = _connectionSearchQuery?.Trim().ToLower() ?? "";
                    var category = _connectionFilterCategory?.Trim().ToLower() ?? "all";

                    var filtered = _rawConnections.AsEnumerable();

                    // Apply category filter
                    if (category == "established")
                    {
                        filtered = filtered.Where(c => c.State.Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (category == "listening" || category == "listen")
                    {
                        filtered = filtered.Where(c => c.State.StartsWith("LISTEN", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (category == "tcp")
                    {
                        filtered = filtered.Where(c => c.Protocol.Equals("TCP", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (category == "udp")
                    {
                        filtered = filtered.Where(c => c.Protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase));
                    }

                    // Apply text search query
                    if (!string.IsNullOrEmpty(query))
                    {
                        filtered = filtered.Where(c =>
                            c.ProcessName.ToLower().Contains(query) ||
                            c.Protocol.ToLower().Contains(query) ||
                            c.LocalAddress.ToLower().Contains(query) ||
                            c.ForeignAddress.ToLower().Contains(query) ||
                            c.State.ToLower().Contains(query) ||
                            c.Pid.ToString().Contains(query)
                        );
                    }

                    var resultList = filtered.ToList();
                    Connections.Clear();
                    foreach (var item in resultList)
                    {
                        Connections.Add(item);
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    public async Task<bool> TerminateProcessAsync(int pid, string processName)
    {
        if (pid <= 4) return false; // Protected system processes
        try
        {
            await Task.Run(() =>
            {
                var proc = System.Diagnostics.Process.GetProcessById(pid);
                proc.Kill(true);
            });

            LogText(string.Format("Terminated process '{0}' (PID {1}).".T(), processName, pid));
            _notificationService?.ShowSuccess("Process Terminated".T(), string.Format("Process {0} (PID {1}) was stopped.".T(), processName, pid));
            await LoadActiveConnectionsAsync(forceRefresh: true);
            return true;
        }
        catch (Exception ex)
        {
            LogText($"Failed to terminate process {processName} (PID {pid}): {ex.Message}");
            _notificationService?.ShowError("Action Failed".T(), ex.Message);
            return false;
        }
    }

    public void OpenProcessLocation(int pid, string processName)
    {
        if (pid <= 4) return;
        try
        {
            var proc = System.Diagnostics.Process.GetProcessById(pid);
            string? exePath = proc.MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{exePath}\"");
            }
        }
        catch (Exception ex)
        {
            LogText($"Could not open process directory for {processName}: {ex.Message}");
        }
    }
}

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
                        catch (Exception ex)
                        {
                            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.SyncAdaptersUI", ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.LoadAdaptersEnqueue", ex);
                }
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
        OnPropertyChanged(nameof(IsAdaptersEmpty));
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

    private string _connectionSortBy = "ProcessName";
    public string ConnectionSortBy
    {
        get => _connectionSortBy;
        set
        {
            SetPropertyOnUI(() => _connectionSortBy, v => _connectionSortBy = v, value);
            ApplyConnectionFilter();
        }
    }

    private bool _connectionSortAscending = true;
    public bool ConnectionSortAscending
    {
        get => _connectionSortAscending;
        set
        {
            SetPropertyOnUI(() => _connectionSortAscending, v => _connectionSortAscending = v, value);
            ApplyConnectionFilter();
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

                    // Apply sorting
                    filtered = (_connectionSortBy.ToLowerInvariant(), _connectionSortAscending) switch
                    {
                        ("pid", true) => filtered.OrderBy(c => c.Pid),
                        ("pid", false) => filtered.OrderByDescending(c => c.Pid),
                        ("protocol", true) => filtered.OrderBy(c => c.Protocol).ThenBy(c => c.ProcessName),
                        ("protocol", false) => filtered.OrderByDescending(c => c.Protocol).ThenBy(c => c.ProcessName),
                        ("port", true) or ("localport", true) => filtered.OrderBy(c => GetLocalPort(c.LocalAddress)),
                        ("port", false) or ("localport", false) => filtered.OrderByDescending(c => GetLocalPort(c.LocalAddress)),
                        ("state", true) => filtered.OrderBy(c => c.State).ThenBy(c => c.ProcessName),
                        ("state", false) => filtered.OrderByDescending(c => c.State).ThenBy(c => c.ProcessName),
                        (_, false) => filtered.OrderByDescending(c => c.ProcessName),
                        _ => filtered.OrderBy(c => c.ProcessName)
                    };

                    var resultList = filtered.ToList();
                    Connections.Clear();
                    foreach (var item in resultList)
                    {
                        Connections.Add(item);
                    }
                    OnPropertyChanged(nameof(IsConnectionsEmpty));
                    OnPropertyChanged(nameof(ActiveConnectionsCount));

                    // Populate TopProcesses from raw connections (grouped by application)
                    var topGrouped = _rawConnections
                        .Where(c => !string.IsNullOrWhiteSpace(c.ProcessName) && c.Pid > 4)
                        .GroupBy(c => c.ProcessName)
                        .OrderByDescending(g => g.Count())
                        .Take(6)
                        .Select(g =>
                        {
                            var first = g.First();
                            var pName = g.Key.ToLowerInvariant();
                            string glyph = "\uE71D";
                            if (pName.Contains("chrome") || pName.Contains("edge") || pName.Contains("firefox") || pName.Contains("brave")) glyph = "\uE774";
                            else if (pName.Contains("discord") || pName.Contains("telegram") || pName.Contains("teams") || pName.Contains("zalo")) glyph = "\uE915";
                            else if (pName.Contains("steam") || pName.Contains("epic") || pName.Contains("riot") || pName.Contains("game")) glyph = "\uE7FC";
                            else if (pName.Contains("spotify") || pName.Contains("music")) glyph = "\uE8D6";
                            else if (pName.Contains("svchost") || pName.Contains("system")) glyph = "\uE770";

                            return new ProcessGroupInfo
                            {
                                ProcessName = g.Key,
                                Pid = first.Pid,
                                ConnectionCount = g.Count(),
                                StateSummary = g.Any(x => x.State.Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase)) ? "ESTABLISHED" : first.State,
                                IconGlyph = glyph,
                                Description = $"{g.Count()} sockets • PID: {first.Pid}"
                            };
                        }).ToList();

                    TopProcesses.Clear();
                    foreach (var p in topGrouped)
                    {
                        TopProcesses.Add(p);
                    }
                }
                catch (Exception ex)
                {
                    WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.ApplyConnectionFilterInner", ex);
                }
            });
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.ApplyConnectionFilterEnqueue", ex);
        }
    }

    private static int GetLocalPort(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return 0;
        int lastColon = address.LastIndexOf(':');
        if (lastColon >= 0 && lastColon < address.Length - 1 && int.TryParse(address[(lastColon + 1)..], out int port))
        {
            return port;
        }
        return 0;
    }

    private static readonly HashSet<string> CriticalSystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "system idle process", "smss", "smss.exe", "csrss", "csrss.exe", 
        "wininit", "wininit.exe", "services", "services.exe", "lsass", "lsass.exe", 
        "lsm", "lsm.exe", "winlogon", "winlogon.exe", "dwm", "dwm.exe", 
        "fontdrvhost", "fontdrvhost.exe", "sihost", "sihost.exe"
    };

    public static bool IsProtectedProcess(int pid, string processName)
    {
        if (pid <= 4 || pid == Environment.ProcessId) return true;
        if (string.IsNullOrWhiteSpace(processName)) return false;
        string cleanName = processName.Trim();
        if (cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) 
            cleanName = cleanName[..^4];
        return CriticalSystemProcesses.Contains(cleanName) || CriticalSystemProcesses.Contains(processName.Trim());
    }

    public async Task<bool> TerminateProcessAsync(int pid, string processName)
    {
        if (IsProtectedProcess(pid, processName))
        {
            LogText(string.Format("Cannot terminate protected system process '{0}' (PID {1}).".T(), processName, pid));
            _notificationService?.ShowWarning("Action Blocked".T(), string.Format("Process '{0}' is protected by Windows OS.".T(), processName));
            return false;
        }

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
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.TerminateProcess", ex);
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
            if (!string.IsNullOrEmpty(exePath))
            {
                WinCarePro.Core.Helpers.ProcessRunner.OpenInExplorer(exePath, selectFile: true);
            }
        }
        catch (Exception ex)
        {
            LogText($"Could not open process directory for {processName}: {ex.Message}");
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.OpenProcessLocation", ex);
        }
    }

    public string ExportSocketsListToCsv()
    {
        var targetList = Connections.Count > 0 ? Connections : (IEnumerable<ActiveConnectionInfo>)_rawConnections;
        string csv = _engine.ExportSocketsToCsv(targetList);
        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string filePath = System.IO.Path.Combine(desktopPath, $"WinCarePro_Network_Sockets_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            System.IO.File.WriteAllText(filePath, csv);

            LogText(string.Format("Exported {0} sockets to: {1}".T(), targetList.Count(), filePath));
            Database.DbManager.LogAction("Export Sockets CSV", "Network Center", "Success");
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("NetworkViewModel.ExportSocketsCsv", ex);
        }
        return csv;
    }

    public void ResetSocketFilters()
    {
        _connectionSearchQuery = "";
        _connectionFilterCategory = "All";
        _connectionSortBy = "ProcessName";
        _connectionSortAscending = true;
        OnPropertyChanged(nameof(ConnectionSearchQuery));
        OnPropertyChanged(nameof(ConnectionFilterCategory));
        OnPropertyChanged(nameof(ConnectionSortBy));
        OnPropertyChanged(nameof(ConnectionSortAscending));
        ApplyConnectionFilter();
    }
}

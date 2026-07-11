using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using WinCarePro.Models;

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
        Adapters = new ObservableCollection<NetworkAdapterInfo>(newList);
    }

    public async Task LoadActiveConnectionsAsync()
    {
        try
        {
            var list = await Task.Run(() => _engine.GetActiveConnections());
            try
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        _rawConnections = list;
                        ApplyConnectionFilter();
                    }
                    catch { }
                });
            }
            catch { }
        }
        catch (Exception ex)
        {
            LogText($"Failed to load active connections: {ex.Message}");
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
                    List<ActiveConnectionInfo> filtered;
                    if (string.IsNullOrEmpty(query))
                    {
                        filtered = _rawConnections;
                    }
                    else
                    {
                        filtered = _rawConnections.Where(c =>
                            c.ProcessName.ToLower().Contains(query) ||
                            c.Protocol.ToLower().Contains(query) ||
                            c.LocalAddress.ToLower().Contains(query) ||
                            c.ForeignAddress.ToLower().Contains(query) ||
                            c.State.ToLower().Contains(query) ||
                            c.Pid.ToString().Contains(query)
                        ).ToList();
                    }

                    Connections = new ObservableCollection<ActiveConnectionInfo>(filtered);
                }
                catch { }
            });
        }
        catch { }
    }
}

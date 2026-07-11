using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel
{
    public async Task StartDnsBenchmarkAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Initiating DNS query resolution benchmark...".T());
        CancelDnsBenchmark();
        _dnsCts = new System.Threading.CancellationTokenSource();
        var token = _dnsCts.Token;
        try
        {
            var result = await _engine.RunDnsBenchmarkAsync(token);
            if (token.IsCancellationRequested || (_cts != null && _cts.IsCancellationRequested)) return;
            try
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        if (token.IsCancellationRequested || (_cts != null && _cts.IsCancellationRequested)) return;
                        DnsServers = new ObservableCollection<DnsServerInfo>(result);
                        var fastest = result.FirstOrDefault(d => d.IsFastest);
                        _fastestDns = fastest;
                        if (fastest == null)
                        {
                            FastestDnsText = "Failed";
                        }
                        else
                        {
                            OnPropertyChanged(nameof(FastestDnsText));
                        }
                    }
                    catch { }
                });
            }
            catch { }
            
            await _historyService.SaveDnsBenchmarkResultAsync(result);
            _notificationService?.ShowSuccess("DNS Benchmark Completed".T(), string.Format("Fastest server: {0}".T(), FastestDnsText));
        }
        catch (OperationCanceledException)
        {
            // Do not update UI or log if cancelled
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested && (_cts == null || !_cts.IsCancellationRequested))
            {
                LogText($"DNS benchmark error: {ex.Message}");
            }
        }
        finally
        {
            if (!token.IsCancellationRequested && (_cts == null || !_cts.IsCancellationRequested))
            {
                IsBusy = false;
            }
        }
    }

    public async Task ApplyDnsAsync(DnsServerInfo server)
    {
        if (IsBusy || server == null) return;
        IsBusy = true;
        try
        {
            bool ok = await _engine.ApplyDnsSettingsAsync(server.Name, server.PrimaryIp, server.SecondaryIp);
            if (_cts == null || _cts.IsCancellationRequested) return;
            if (ok)
            {
                LogText(string.Format("Successfully applied DNS: {0}".T(), server.Name));
                _notificationService?.ShowSuccess("DNS Server Updated".T(), string.Format("Active interface configured to use {0}.".T(), server.Name));
            }
            else
            {
                LogText("Failed to apply DNS settings (Requires administrator privilege).".T());
                _notificationService?.ShowError("DNS Setup Failed".T(), "Administrative privileges required.");
            }
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText($"Error applying DNS settings: {ex.Message}");
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunRepairOperationAsync(string operation)
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText(string.Format("Initiating repair action: {0}...".T(), operation));
        
        try
        {
            bool ok = operation.ToLower() switch
            {
                "dns" => await _engine.FlushDnsAsync(),
                "winsock" => await _engine.ResetWinsockAsync(),
                "tcpip" => await _engine.ResetTcpIpAsync(),
                "iprenew" => await _engine.ReleaseRenewIpAsync(),
                "adapter" => await _engine.RestartNetworkAdapterAsync(),
                "firewall" => await _engine.ResetFirewallAsync(),
                "proxy" => await _engine.ResetProxyAsync(),
                "hosts" => await _engine.ResetHostsFileAsync(),
                "optimize" => await _engine.OptimizeTcpAutoTuningAsync(),
                "green" => await _engine.DisableEnergyEfficientEthernetAsync(),
                _ => false
            };

            if (_cts == null || _cts.IsCancellationRequested) return;
            
            if (ok)
            {
                LogText("Repair operation succeeded.".T());
                _notificationService?.ShowSuccess("Network Repair".T(), string.Format("Operation '{0}' completed successfully.", operation).T());
            }
            else
            {
                LogText("Repair operation encountered errors.".T());
                _notificationService?.ShowWarning("Network Repair".T(), string.Format("Operation '{0}' failed or requires Administrator elevation.", operation).T());
            }
            await RunDiagnosticsAsync(); // refresh connectivity status
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Repair failed: {0}".T(), ex.Message));
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }
}

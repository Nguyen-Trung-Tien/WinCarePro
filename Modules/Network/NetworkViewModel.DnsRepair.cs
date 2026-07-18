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
                        DnsServers.Clear();
                        foreach (var server in result)
                        {
                            DnsServers.Add(server);
                        }
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

    private bool _isDohEnabled;
    public bool IsDohEnabled
    {
        get => _isDohEnabled;
        set => SetPropertyOnUI(() => _isDohEnabled, v => _isDohEnabled = v, value);
    }

    private string _selectedDohProvider = "Cloudflare";
    public string SelectedDohProvider
    {
        get => _selectedDohProvider;
        set => SetPropertyOnUI(() => _selectedDohProvider, v => _selectedDohProvider = v, value);
    }

    public async Task InitializeDohAsync()
    {
        try
        {
            bool isEnabled = await _engine.IsDohEnabledAsync();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                _isDohEnabled = isEnabled;
                OnPropertyChanged(nameof(IsDohEnabled));
            });
        }
        catch (Exception ex)
        {
            LogText($"Failed to detect DoH state: {ex.Message}");
        }
    }

    public async Task ApplyDohSettingsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        
        try
        {
            bool enable = IsDohEnabled;
            string primaryIp = "";
            string secondaryIp = "";
            string template = "";

            if (enable)
            {
                switch (SelectedDohProvider)
                {
                    case "Google":
                        primaryIp = "8.8.8.8";
                        secondaryIp = "8.8.4.4";
                        template = "https://dns.google/dns-query";
                        break;
                    case "AdGuard":
                        primaryIp = "94.140.14.14";
                        secondaryIp = "94.140.15.15";
                        template = "https://dns.adguard-dns.com/dns-query";
                        break;
                    case "NextDNS":
                        primaryIp = "45.90.28.0";
                        secondaryIp = "45.90.30.0";
                        template = "https://dns.nextdns.io";
                        break;
                    case "Cloudflare":
                    default:
                        primaryIp = "1.1.1.1";
                        secondaryIp = "1.0.0.1";
                        template = "https://cloudflare-dns.com/dns-query";
                        break;
                }
            }

            bool success = await _engine.SetDohSettingsAsync(enable, primaryIp, secondaryIp, template);
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (success)
                {
                    LogText(enable ? "DNS over HTTPS successfully enabled.".T() : "DNS over HTTPS successfully disabled.".T());
                    _notificationService?.ShowSuccess("Secure DNS Updated".T(), enable ? "DNS over HTTPS configured successfully." : "DNS config reset to automatic.");
                }
                else
                {
                    LogText("Failed to update DNS over HTTPS configuration. Admin rights required.".T());
                    _notificationService?.ShowError("Secure DNS Failed".T(), "Administrative privileges required.");
                    // Revert state
                    _isDohEnabled = !enable;
                    OnPropertyChanged(nameof(IsDohEnabled));
                }
            });

            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            LogText($"Error updating DoH: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}


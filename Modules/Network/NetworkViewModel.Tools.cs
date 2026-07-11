using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel
{
    public async Task RunDiagnosticsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText("Starting connectivity diagnosis...".T());

        try
        {
            bool hasInternet = await Task.Run(() => _engine.CheckInternetConnection());
            if (_cts == null || _cts.IsCancellationRequested) return;
            InternetStatus = hasInternet ? "Connected" : "No Internet";

            string gw = await Task.Run(() => _engine.GetGatewayAddress());
            if (_cts == null || _cts.IsCancellationRequested) return;
            GatewayAddress = gw;

            bool gatewayOk = await Task.Run(() => _engine.CheckGatewayReachability());
            if (_cts == null || _cts.IsCancellationRequested) return;
            GatewayReachability = gatewayOk ? "Reachable" : "Unreachable";

            bool dnsOk = await Task.Run(() => _engine.CheckDnsResolution());
            if (_cts == null || _cts.IsCancellationRequested) return;
            DnsStatus = dnsOk ? "Resolving" : "Failed";

            var (v4, v6) = await Task.Run(() => _engine.CheckIpStatus());
            if (_cts == null || _cts.IsCancellationRequested) return;
            IpStatus = $"IPv4: {(v4 ? "Active" : "Inactive")}, IPv6: {(v6 ? "Active" : "Inactive")}";

            LogText("Estimating packet loss, latency, and jitter quality...".T());
            var (loss, latency, jitter) = await _engine.AnalyzePingQualityAsync();
            if (_cts == null || _cts.IsCancellationRequested) return;
            LatencyMs = Math.Round(latency, 1);
            PacketLossPercent = Math.Round(loss, 1);
            JitterMs = Math.Round(jitter, 1);

            // Connection quality mapping
            if (loss > 10.0 || latency > 150.0)
                ConnectionQuality = "Poor";
            else if (loss > 2.0 || latency > 60.0 || jitter > 15.0)
                ConnectionQuality = "Moderate";
            else
                ConnectionQuality = "Good";
            
            // Add point to history charts
            AddHistoryPoint(PingHistory, LatencyMs);

            LogText(string.Format("Diagnostics complete. Latency: {0}ms, Jitter: {1}ms, Packet Loss: {2}%.".T(), LatencyMs, JitterMs, PacketLossPercent));
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Diagnostics error: {0}".T(), ex.Message));
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

    public async Task RunPingTestAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunPingTestAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Ping test failed: {0}".T(), ex.Message));
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

    public async Task RunTracerouteAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunTracerouteAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Traceroute failed: {0}".T(), ex.Message));
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

    public async Task RunDnsLookupAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(TestHost)) return;
        IsBusy = true;
        try
        {
            await _engine.RunDnsLookupAsync(TestHost);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("DNS Lookup failed: {0}".T(), ex.Message));
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

    public async Task RunPortScanAsync()
    {
        if (IsBusy || string.IsNullOrEmpty(PortScannerHost)) return;
        IsBusy = true;
        try
        {
            var ports = PortScannerPorts.Split(',')
                .Select(p => int.TryParse(p.Trim(), out int val) ? val : -1)
                .Where(v => v > 0)
                .ToArray();

            await _engine.RunPortScanAsync(PortScannerHost, ports);
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Port scan failed: {0}".T(), ex.Message));
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

    public async Task RunSpeedTestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        SpeedProgress = 0;
        DownloadSpeedMbps = 0;
        UploadSpeedMbps = 0;
        LogText("Starting speed test...".T());
        try
        {
            LogText("Running download speed benchmark...".T());
            double dl = await _engine.RunSpeedTestAsync((speed, progress) =>
            {
                DownloadSpeedMbps = Math.Round(speed, 1);
                SpeedProgress = Math.Round(progress / 2.0, 1);
            });

            if (_cts == null || _cts.IsCancellationRequested) return;

            LogText("Running upload speed benchmark...".T());
            double ul = await _engine.RunUploadSpeedTestAsync((speed, progress) =>
            {
                UploadSpeedMbps = Math.Round(speed, 1);
                SpeedProgress = Math.Round(50.0 + (progress / 2.0), 1);
            });

            if (_cts == null || _cts.IsCancellationRequested) return;
            SpeedProgress = 100;

            var result = new SpeedTestResult
            {
                DownloadMbps = DownloadSpeedMbps,
                UploadMbps = UploadSpeedMbps,
                PingMs = LatencyMs,
                JitterMs = JitterMs,
                ServerName = "Tele2 & Httpbin CDN",
                TestDuration = 16.0,
                Timestamp = DateTime.Now
            };

            await _historyService.SaveSpeedTestResultAsync(result);
            await LoadHistoryAsync();

            LogText(string.Format("Speed test complete. Download: {0} Mbps, Upload: {1} Mbps, Latency: {2} ms, Jitter: {3} ms.".T(), DownloadSpeedMbps, UploadSpeedMbps, LatencyMs, JitterMs));
            _notificationService?.ShowSuccess("Speed Test Completed".T(), string.Format("Download: {0} Mbps, Upload: {1} Mbps.".T(), DownloadSpeedMbps, UploadSpeedMbps));
        }
        catch (Exception ex)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText(string.Format("Speed test failed: {0}".T(), ex.Message));
                _notificationService?.ShowError("Speed Test Failed".T(), ex.Message);
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

    private async Task LoadHistoryAsync()
    {
        try
        {
            var list = await _historyService.GetSpeedTestHistoryAsync();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                SpeedTestHistory = new ObservableCollection<SpeedTestResult>(list);
            });
        }
        catch { }
    }
}

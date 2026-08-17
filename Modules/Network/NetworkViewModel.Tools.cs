using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel
{
    public async Task RunDiagnosticsAsync(bool userTriggered = false)
    {
        if (userTriggered && IsBusy) return;
        if (userTriggered) IsBusy = true;
        
        LogText("Starting connectivity diagnosis...".T());

        try
        {
            var internetTask = Task.Run(() => _engine.CheckInternetConnection());
            var gwTask = Task.Run(() => _engine.GetGatewayAddress());
            var gwReachTask = Task.Run(() => _engine.CheckGatewayReachability());
            var dnsTask = Task.Run(() => _engine.CheckDnsResolution());
            var ipTask = Task.Run(() => _engine.CheckIpStatus());
            var pingTask = _engine.AnalyzePingQualityAsync("1.1.1.1", 3);

            await Task.WhenAll(internetTask, gwTask, gwReachTask, dnsTask, ipTask, pingTask);

            if (_cts == null || _cts.IsCancellationRequested) return;

            InternetStatus = internetTask.Result ? "Connected" : "No Internet";
            GatewayAddress = gwTask.Result;
            GatewayReachability = gwReachTask.Result ? "Reachable" : "Unreachable";
            DnsStatus = dnsTask.Result ? "Resolving" : "Failed";

            var (v4, v6) = ipTask.Result;
            IpStatus = $"IPv4: {(v4 ? "Active" : "Inactive")}, IPv6: {(v6 ? "Active" : "Inactive")}";

            var (loss, latency, jitter) = pingTask.Result;
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
            if (userTriggered && _cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }
    }

    public async Task RunPingTestAsync()
    {
        string host = string.IsNullOrWhiteSpace(TestHost) ? "8.8.8.8" : TestHost.Trim();
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _engine.RunPingTestAsync(host);
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
        string host = string.IsNullOrWhiteSpace(TestHost) ? "8.8.8.8" : TestHost.Trim();
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _engine.RunTracerouteAsync(host);
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
        string host = string.IsNullOrWhiteSpace(TestHost) ? "google.com" : TestHost.Trim();
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _engine.RunDnsLookupAsync(host);
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
        string host = string.IsNullOrWhiteSpace(TestHost) ? (string.IsNullOrWhiteSpace(PortScannerHost) ? "127.0.0.1" : PortScannerHost.Trim()) : TestHost.Trim();
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var ports = PortScannerPorts.Split(',')
                .Select(p => int.TryParse(p.Trim(), out int val) ? val : -1)
                .Where(v => v > 0)
                .ToArray();

            if (ports.Length == 0)
            {
                ports = new[] { 80, 443, 3389, 8080, 22 };
            }

            await _engine.RunPortScanAsync(host, ports);
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
        SpeedTestPhase = "Testing Ping...";
        SpeedProgress = 5;
        DownloadSpeedMbps = 0;
        UploadSpeedMbps = 0;
        OnPropertyChanged(nameof(DisplaySpeed));
        OnPropertyChanged(nameof(DisplaySpeedLabel));
        OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
        OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
        LogText("Starting speed test...".T());
        try
        {
            LogText("Measuring network latency & ping quality...".T());
            var pingQuality = await _engine.AnalyzePingQualityAsync("1.1.1.1", 3);
            if (pingQuality.avgLatencyMs > 0)
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    LatencyMs = Math.Round(pingQuality.avgLatencyMs, 1);
                    JitterMs = Math.Round(pingQuality.jitterMs, 1);
                    OnPropertyChanged(nameof(DisplaySpeed));
                    OnPropertyChanged(nameof(DisplaySpeedLabel));
                });
            }

            if (_cts == null || _cts.IsCancellationRequested) return;

            SpeedTestPhase = "Testing Download...";
            SpeedProgress = 10;
            OnPropertyChanged(nameof(DisplaySpeedLabel));
            OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
            OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
            LogText("Running download speed benchmark...".T());
            double dl = await _engine.RunSpeedTestAsync((speed, progress) =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    DownloadSpeedMbps = Math.Round(speed, 1);
                    SpeedProgress = Math.Round(10.0 + (progress * 0.42), 1);
                    OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
                    OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
                });
            });

            if (_cts == null || _cts.IsCancellationRequested) return;

            SpeedTestPhase = "Testing Upload...";
            SpeedProgress = 55;
            OnPropertyChanged(nameof(DisplaySpeedLabel));
            OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
            OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
            LogText("Running upload speed benchmark...".T());
            double ul = await _engine.RunUploadSpeedTestAsync((speed, progress) =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    UploadSpeedMbps = Math.Round(speed, 1);
                    SpeedProgress = Math.Round(55.0 + (progress * 0.44), 1);
                    OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
                    OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));
                });
            });

            if (_cts == null || _cts.IsCancellationRequested) return;
            SpeedProgress = 100;
            SpeedTestPhase = "Completed";
            OnPropertyChanged(nameof(DisplaySpeed));
            OnPropertyChanged(nameof(DisplaySpeedLabel));
            OnPropertyChanged(nameof(SpeedPhaseAccentBrush));
            OnPropertyChanged(nameof(SpeedPhaseBadgeBgBrush));

            var result = new SpeedTestResult
            {
                DownloadMbps = DownloadSpeedMbps,
                UploadMbps = UploadSpeedMbps,
                PingMs = LatencyMs,
                JitterMs = JitterMs,
                ServerName = "Cloudflare Edge CDN (Low Latency)",
                TestDuration = 14.0,
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
                SpeedTestPhase = "Failed";
                LogText(string.Format("Speed test failed: {0}".T(), ex.Message));
                _notificationService?.ShowError("Speed Test Failed".T(), ex.Message);
            }
        }
        finally
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsBusy = false;
                if (SpeedTestPhase != "Completed")
                {
                    SpeedTestPhase = "Ready";
                }
                OnPropertyChanged(nameof(DisplaySpeed));
                OnPropertyChanged(nameof(DisplaySpeedLabel));
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
                SpeedTestHistory.Clear();
                foreach (var item in list)
                {
                    SpeedTestHistory.Add(item);
                }
                OnPropertyChanged(nameof(HasSpeedTestHistory));
                OnPropertyChanged(nameof(HasNoSpeedTestHistory));
            });
        }
        catch (Exception ex)
        {
            LogText($"Failed to load speed test history: {ex.Message}");
        }
    }

    public async Task ClearSpeedTestHistoryAsync()
    {
        try
        {
            await _historyService.ClearHistoryAsync();
            _dispatcherQueue?.TryEnqueue(() =>
            {
                SpeedTestHistory.Clear();
                OnPropertyChanged(nameof(HasSpeedTestHistory));
                OnPropertyChanged(nameof(HasNoSpeedTestHistory));
            });
            _notificationService?.ShowSuccess("History Cleared".T(), "Speed test telemetry history has been wiped.".T());
        }
        catch (Exception ex)
        {
            LogText($"Failed to clear history: {ex.Message}");
        }
    }
}

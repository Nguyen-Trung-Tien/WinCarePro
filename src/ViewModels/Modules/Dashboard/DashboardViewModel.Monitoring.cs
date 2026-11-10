using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using LiveChartsCore.Defaults;
using WinCarePro.Services;
using WinCarePro.Engines;
using WinCarePro.Core.Interop;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.ViewModels;

public partial class DashboardViewModel
{
    private NativeApi.FILETIME _prevIdleTime;
    private NativeApi.FILETIME _prevKernelTime;
    private NativeApi.FILETIME _prevUserTime;
    private bool _hasPrevTimes = false;

    private (double cpu, double ramPercent) GetSystemResourceUsage()
    {
        double cpu = 0;
        double ramPercent = 45.0;
        bool cpuReadSuccess = false;
        bool ramReadSuccess = false;

        try
        {
            if (NativeApi.GetSystemTimes(out NativeApi.FILETIME idleTime, out NativeApi.FILETIME kernelTime, out NativeApi.FILETIME userTime))
            {
                if (_hasPrevTimes)
                {
                    ulong prevIdle = _prevIdleTime.ToUInt64();
                    ulong prevKernel = _prevKernelTime.ToUInt64();
                    ulong prevUser = _prevUserTime.ToUInt64();

                    ulong currIdle = idleTime.ToUInt64();
                    ulong currKernel = kernelTime.ToUInt64();
                    ulong currUser = userTime.ToUInt64();

                    ulong idleDiff = currIdle - prevIdle;
                    ulong kernelDiff = currKernel - prevKernel;
                    ulong userDiff = currUser - prevUser;

                    ulong totalDiff = kernelDiff + userDiff;
                    if (totalDiff > 0)
                    {
                        cpu = ((double)(totalDiff - idleDiff) / totalDiff) * 100.0;
                        cpu = Math.Clamp(cpu, 0.0, 100.0);
                        cpuReadSuccess = true;
                    }
                }
                else
                {
                    cpu = 2.0;
                    cpuReadSuccess = true;
                }

                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;
                _hasPrevTimes = true;
            }

            var memStatus = NativeApi.MEMORYSTATUSEX.Create();
            if (NativeApi.GlobalMemoryStatusEx(ref memStatus))
            {
                ramPercent = memStatus.dwMemoryLoad;
                ramReadSuccess = true;
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.GetSystemResourceUsage", ex);
        }

        if (!cpuReadSuccess)
        {
            cpu = 0.0;
        }
        if (!ramReadSuccess)
        {
            ramPercent = 0.0;
        }

        return (cpu, ramPercent);
    }

    private void StartResourceMonitor()
    {
        if (Interlocked.CompareExchange(ref _monitorRunning, 1, 0) != 0) return;

        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        // Zero-Bloat On-Demand Snapshot: Query Win32 metrics once on view activation without background polling loops
        Task.Run(() =>
        {
            try
            {
                if (token.IsCancellationRequested) return;

                var currentSettings = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
                bool sensorsEnabled = currentSettings.EnableSensorsThread;

                var (cpu, ram) = GetSystemResourceUsage();
                double gpu = sensorsEnabled ? GetGpuUsageMetric() : 0.0;
                double disk = sensorsEnabled ? GetDiskUsageMetric() : 0.0;
                double cpuTemp = sensorsEnabled ? _hardwareEngine.GetCpuTemperature(cpu) : double.NaN;

                if (token.IsCancellationRequested) return;

                _dispatcherQueue?.TryEnqueue(() =>
                {
                    if (token.IsCancellationRequested) return;

                    CpuUsage = Math.Round(cpu, 1);
                    RamUsage = Math.Round(ram, 1);
                    GpuUsage = Math.Round(gpu, 1);
                    DiskUsage = Math.Round(disk, 1);
                    CpuTemperature = double.IsNaN(cpuTemp) ? 0 : cpuTemp;
                    CpuTempFormatted = !sensorsEnabled ? "Disabled".T() : (double.IsNaN(cpuTemp) ? "N/A" : $"{cpuTemp:F0}°C");

                    int count = CpuSeriesValues.Count;
                    if (count > 0)
                    {
                        CpuSeriesValues[count - 1].Value = CpuUsage;
                        RamSeriesValues[count - 1].Value = RamUsage;
                        GpuSeriesValues[count - 1].Value = GpuUsage;
                        DiskSeriesValues[count - 1].Value = DiskUsage;
                    }

                    var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                    SystemUptime = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";

                    DetectBottlenecks();
                    UpdateHealthScoreBreakdown();
                });
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("DashboardViewModel.StartResourceMonitor", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _monitorRunning, 0);
            }
        }, token);
    }

    public void StartMonitoring()
    {
        _isDisposed = false;
        if (_languageChangedHandler != null)
        {
            TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
            TranslationManager.Instance.LanguageChanged += _languageChangedHandler;
        }
        if (_diskTimeCounter == null)
        {
            InitializeCounters();
        }

        if (_monitorCts == null || _monitorCts.IsCancellationRequested)
        {
            _monitorCts?.Dispose();
            _monitorCts = null;
        }
        StartResourceMonitor();
    }

    public void StopMonitoring()
    {
        _monitorCts?.Cancel();
        CancelScanIfRunning();
        if (_languageChangedHandler != null)
        {
            TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        }
    }

    /// <summary>Cancel scan đang chạy (gọi khi navigate away hoặc dispose).</summary>
    public void CancelScanIfRunning()
    {
        if (_scanCts != null && !_scanCts.IsCancellationRequested)
        {
            SetOperationState(OperationState.Cancelling);
            try { _scanCts.Cancel(); } catch { }
            SetOperationState(OperationState.Idle);
        }
    }

    private double GetGpuUsageMetric()
    {
        double realGpu = _hardwareEngine.GetActualGpuUsage();
        return realGpu >= 0 ? Math.Clamp(realGpu, 0, 100) : 0.0;
    }

    private double GetDiskUsageMetric()
    {
        try
        {
            if (_diskTimeCounter != null)
            {
                double val = _diskTimeCounter.NextValue();
                return Math.Clamp(val, 0, 100);
            }
        }
        catch { }

        // Fallback using P/Invoke GetDiskFreeSpaceEx
        try
        {
            if (NativeApi.GetDiskFreeSpaceEx("C:\\", out ulong freeBytes, out ulong totalBytes, out _))
            {
                if (totalBytes > 0)
                {
                    double usedPercent = ((double)(totalBytes - freeBytes) / totalBytes) * 100.0;
                    return Math.Clamp(usedPercent, 0, 100);
                }
            }
        }
        catch { }

        return 0.0;
    }

    public void ToggleChartSeries(string seriesName, bool isVisible)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            var target = seriesName.ToUpperInvariant() switch
            {
                "CPU" => _cpuLineSeries,
                "RAM" => _ramLineSeries,
                "GPU" => _gpuLineSeries,
                "DISK" => _diskLineSeries,
                _ => null
            };

            if (target == null) return;

            if (isVisible)
            {
                if (!PerformanceSeries.Contains(target))
                {
                    // Keep elements sorted: CPU -> RAM -> GPU -> Disk
                    int targetIndex = 0;
                    if (target == _ramLineSeries)
                    {
                        if (PerformanceSeries.Contains(_cpuLineSeries!)) targetIndex = 1;
                    }
                    else if (target == _gpuLineSeries)
                    {
                        targetIndex = PerformanceSeries.Count;
                        if (PerformanceSeries.Contains(_diskLineSeries!)) targetIndex = PerformanceSeries.IndexOf(_diskLineSeries!);
                    }
                    else if (target == _diskLineSeries)
                    {
                        targetIndex = PerformanceSeries.Count;
                    }

                    if (targetIndex >= 0 && targetIndex <= PerformanceSeries.Count)
                    {
                        PerformanceSeries.Insert(targetIndex, target);
                    }
                    else
                    {
                        PerformanceSeries.Add(target);
                    }
                }
            }
            else
            {
                if (PerformanceSeries.Contains(target))
                {
                    PerformanceSeries.Remove(target);
                }
            }
        });
    }
}

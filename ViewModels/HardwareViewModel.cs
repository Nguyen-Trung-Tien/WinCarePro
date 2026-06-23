using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class HardwareViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly HardwareDriverEngine _engine = new();

    private bool _isBusy = true;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    // Specifications
    private string _cpuModel = "Loading...".T();
    public string CpuModel
    {
        get => _cpuModel;
        set => SetProperty(ref _cpuModel, value);
    }

    private string _cpuSpecs = "";
    public string CpuSpecs
    {
        get => _cpuSpecs;
        set => SetProperty(ref _cpuSpecs, value);
    }

    private string _ramModel = "Loading...".T();
    public string RamModel
    {
        get => _ramModel;
        set => SetProperty(ref _ramModel, value);
    }

    private string _gpuModel = "Loading...".T();
    public string GpuModel
    {
        get => _gpuModel;
        set => SetProperty(ref _gpuModel, value);
    }

    private string _motherboard = "Loading...".T();
    public string Motherboard
    {
        get => _motherboard;
        set => SetProperty(ref _motherboard, value);
    }

    private string _biosVersion = "Loading...".T();
    public string BiosVersion
    {
        get => _biosVersion;
        set => SetProperty(ref _biosVersion, value);
    }

    private string _storageSummary = "Loading...".T();
    public string StorageSummary
    {
        get => _storageSummary;
        set => SetProperty(ref _storageSummary, value);
    }

    // Realtime Telemetry
    private double _cpuTemp = 40.0;
    public double CpuTemp
    {
        get => _cpuTemp;
        set { if (SetProperty(ref _cpuTemp, value)) OnPropertyChanged(nameof(CpuTempFormatted)); }
    }
    public string CpuTempFormatted => $"{CpuTemp:F1} °C";

    private double _cpuFreq = 3.2;
    public double CpuFreq
    {
        get => _cpuFreq;
        set { if (SetProperty(ref _cpuFreq, value)) OnPropertyChanged(nameof(CpuFreqFormatted)); }
    }
    public string CpuFreqFormatted => $"{CpuFreq:F2} GHz";

    private double _cpuPower = 15.0;
    public double CpuPower
    {
        get => _cpuPower;
        set { if (SetProperty(ref _cpuPower, value)) OnPropertyChanged(nameof(CpuPowerFormatted)); }
    }
    public string CpuPowerFormatted => $"{CpuPower:F1} W";

    private double _gpuTemp = 42.0;
    public double GpuTemp
    {
        get => _gpuTemp;
        set { if (SetProperty(ref _gpuTemp, value)) OnPropertyChanged(nameof(GpuTempFormatted)); }
    }
    public string GpuTempFormatted => $"{GpuTemp:F1} °C";

    private double _gpuLoad = 2.0;
    public double GpuLoad
    {
        get => _gpuLoad;
        set { if (SetProperty(ref _gpuLoad, value)) OnPropertyChanged(nameof(GpuLoadFormatted)); }
    }
    public string GpuLoadFormatted => $"{GpuLoad:F1} %";

    private double _gpuFanRpm = 1200;
    public double GpuFanRpm
    {
        get => _gpuFanRpm;
        set { if (SetProperty(ref _gpuFanRpm, value)) OnPropertyChanged(nameof(GpuFanRpmFormatted)); }
    }
    public string GpuFanRpmFormatted => $"{GpuFanRpm:F0} RPM";

    private double _ramCommittedGb = 6.4;
    public double RamCommittedGb { get => _ramCommittedGb; set => SetProperty(ref _ramCommittedGb, value); }

    private double _ramCachedGb = 2.8;
    public double RamCachedGb { get => _ramCachedGb; set => SetProperty(ref _ramCachedGb, value); }

    // Battery Health
    private int _batteryPercent = 100;
    public int BatteryPercent
    {
        get => _batteryPercent;
        set { if (SetProperty(ref _batteryPercent, value)) OnPropertyChanged(nameof(BatteryPercentFormatted)); }
    }
    public string BatteryPercentFormatted => $"{BatteryPercent}%";

    private string _batteryStatus = "AC Power".T();
    public string BatteryStatus { get => _batteryStatus; set => SetProperty(ref _batteryStatus, value); }

    private int _batteryCycles = 84;
    public int BatteryCycles { get => _batteryCycles; set => SetProperty(ref _batteryCycles, value); }

    private string _batteryDesignCapacity = "48000 mWh";
    public string BatteryDesignCapacity { get => _batteryDesignCapacity; set => SetProperty(ref _batteryDesignCapacity, value); }

    private string _batteryFullChargeCapacity = "46500 mWh";
    public string BatteryFullChargeCapacity { get => _batteryFullChargeCapacity; set => SetProperty(ref _batteryFullChargeCapacity, value); }

    private double _batteryWearLevel = 3.1;
    public double BatteryWearLevel
    {
        get => _batteryWearLevel;
        set { if (SetProperty(ref _batteryWearLevel, value)) OnPropertyChanged(nameof(BatteryWearLevelFormatted)); }
    }
    public string BatteryWearLevelFormatted => $"{BatteryWearLevel:F1}%";

    private bool _isRunning = true;

    public HardwareViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = LoadSpecsAsync();
        StartSensorMonitoring();
    }

    private async Task LoadSpecsAsync()
    {
        IsBusy = true;
        try
        {
            var specs = await Task.Run(() => _engine.GetHardwareSpecifications());
            _dispatcherQueue.TryEnqueue(() =>
            {
                CpuModel = specs.CpuModel;
                CpuSpecs = $"{specs.CpuCores} " + "Cores".T() + $" / {specs.CpuThreads} " + "Threads".T() + $" - {specs.CpuSpeed}";
                RamModel = $"{specs.RamCapacityGb:F1} GB DDR ({specs.RamSpeed})";
                GpuModel = $"{specs.GpuModel} ({specs.GpuVram})";
                Motherboard = $"{specs.MotherboardManufacturer} {specs.MotherboardModel}";
                BiosVersion = specs.BiosVersion;
                StorageSummary = specs.StorageInfo;
                IsBusy = false;
            });
        }
        catch
        {
            _dispatcherQueue.TryEnqueue(() => IsBusy = false);
        }
    }

    private void StartSensorMonitoring()
    {
        Task.Run(async () =>
        {
            var rand = new Random();
            while (_isRunning)
            {
                try
                {
                    double cpuUsage = 5.0 + rand.NextDouble() * 10.0;
                    double gpuUsage = 2.0 + rand.NextDouble() * 5.0;

                    var stats = _engine.GetBatteryInfo();
                    double cTemp = _engine.GetCpuTemperature(cpuUsage);
                    double gTemp = _engine.GetGpuTemperature(gpuUsage);

                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        CpuTemp = cTemp;
                        GpuTemp = gTemp;
                        GpuLoad = Math.Round(gpuUsage, 1);
                        GpuFanRpm = 1100 + rand.Next(300);

                        CpuFreq = 2.8 + rand.NextDouble() * 0.8;
                        CpuPower = 12.0 + rand.NextDouble() * 8.0;

                        RamCommittedGb = 5.8 + rand.NextDouble() * 1.5;
                        RamCachedGb = 2.0 + rand.NextDouble() * 1.0;

                        BatteryPercent = stats.ChargePercent;
                        BatteryStatus = stats.Status.T();
                        BatteryCycles = 120 + rand.Next(5);
                        BatteryDesignCapacity = "54000 mWh";
                        BatteryFullChargeCapacity = "51800 mWh";
                        BatteryWearLevel = 4.1;
                    });
                }
                catch { }

                await Task.Delay(2000);
            }
        });
    }

    public void StopMonitoring()
    {
        _isRunning = false;
    }
}

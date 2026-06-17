using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public class RepairViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemEngine _engine = new();

    private bool _isBusy;
    private string _consoleLog = "Windows Repair Center Console Ready.\n";
    private int _progressPercent;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string ConsoleLog
    {
        get => _consoleLog;
        set => SetProperty(ref _consoleLog, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    private ObservableCollection<RepairServiceItem> _services = new();
    public ObservableCollection<RepairServiceItem> Services
    {
        get => _services;
        set => SetProperty(ref _services, value);
    }

    public RepairViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += LogText;
        _engine.ProgressChanged += Pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = Pct);
        LoadServices();
    }

    public void LoadServices()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Services.Clear();
            var targetServices = new[]
            {
                new { Name = "wuauserv", Display = "Windows Update (wuauserv)" },
                new { Name = "bits", Display = "Background Intelligent Transfer (bits)" },
                new { Name = "cryptsvc", Display = "Cryptographic Services (cryptsvc)" },
                new { Name = "winmgmt", Display = "Windows Management Instrumentation (winmgmt)" },
                new { Name = "mpssvc", Display = "Windows Defender Firewall (mpssvc)" }
            };

            foreach (var ts in targetServices)
            {
                string status = "Not Found";
                string startupType = "Unknown";
                try
                {
                    using var svc = new System.ServiceProcess.ServiceController(ts.Name);
                    status = svc.Status.ToString();
                    startupType = svc.StartType.ToString();
                }
                catch {}

                Services.Add(new RepairServiceItem
                {
                    Name = ts.Name,
                    DisplayName = ts.Display,
                    Status = status,
                    StartupType = startupType,
                    IsSelected = false
                });
            }
        });
    }

    private void LogText(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ConsoleLog += msg + "\n";
        });
    }

    public async Task RunSfcScanAsync(bool repair)
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            await _engine.RunSfcScanAsync(repair);
        }
        catch (Exception ex)
        {
            LogText($"SFC command execution failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunDismOperationAsync(string mode)
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            await _engine.RunDismAsync(mode);
        }
        catch (Exception ex)
        {
            LogText($"DISM execution failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairWindowsUpdateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            await _engine.RepairWindowsUpdateAsync();
        }
        catch (Exception ex)
        {
            LogText($"Windows Update repair execution failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairServicesConfigAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            var selected = new List<string>();
            foreach (var s in Services)
            {
                if (s.IsSelected)
                {
                    selected.Add(s.Name);
                }
            }
            await _engine.RepairServicesConfigAsync(selected);
            LoadServices();
        }
        catch (Exception ex)
        {
            LogText($"Services restoration failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class RepairServiceItem : ViewModelBase
{
    private string _name = "";
    private string _displayName = "";
    private string _status = "";
    private string _startupType = "";
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string StartupType
    {
        get => _startupType;
        set => SetProperty(ref _startupType, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

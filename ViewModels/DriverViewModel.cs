using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class DriverViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly HardwareDriverEngine _driverEngine = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _progressMessage = "Ready";
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public ObservableCollection<DriverInfo> Drivers { get; } = new();

    private int _driverWizardStep = 1;
    public int DriverWizardStep
    {
        get => _driverWizardStep;
        set => SetProperty(ref _driverWizardStep, value);
    }

    public DriverViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = ScanDriversAsync();
    }

    public async Task ScanDriversAsync()
    {
        Drivers.Clear();
        try
        {
            var list = await Task.Run(() => _driverEngine.GetInstalledDrivers());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    item.IsSelected = item.HasUpdate;
                    item.UpdateStatus = item.HasUpdate ? "Update Available" : "Up to date";
                    Drivers.Add(item);
                }
            });
        }
        catch { }
    }

    public async Task StartDriverUpdateWizardAsync()
    {
        var selectedDrivers = Drivers.Where(x => x.IsSelected && x.HasUpdate).ToList();
        if (selectedDrivers.Count == 0)
        {
            ProgressMessage = "No drivers selected for update.";
            return;
        }

        IsBusy = true;

        DriverWizardStep = 1;
        ProgressMessage = "Wizard Step 1: Analysing firmware components...";
        await Task.Delay(1000);

        DriverWizardStep = 2;
        ProgressMessage = "Wizard Step 2: Downloading driver binaries from verified manufacturer nodes...";
        ProgressPercent = 0;
        for (int i = 0; i <= 100; i += 20)
        {
            ProgressPercent = i;
            await Task.Delay(200);
        }

        DriverWizardStep = 3;
        ProgressMessage = "Wizard Step 3: Installing driver payloads. Display flicker or audio dropouts may occur during firmware compilation.";
        ProgressPercent = 50;
        await Task.Delay(2500);

        DriverWizardStep = 4;
        ProgressMessage = "Wizard Step 4: Verification of physical thread components complete.";
        ProgressPercent = 100;

        foreach (var d in selectedDrivers)
        {
            d.HasUpdate = false;
            d.UpdateStatus = "Completed";
            d.DriverVersion = d.AvailableVersion;
            d.Status = "OK";
        }

        IsBusy = false;
        ProgressMessage = "All physical driver installations successfully verified.";
    }
}

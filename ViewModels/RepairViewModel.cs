using System;
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

    public RepairViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += LogText;
        _engine.ProgressChanged += Pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = Pct);
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
            await _engine.RepairServicesConfigAsync();
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

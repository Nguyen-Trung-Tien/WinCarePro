using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class UpdaterViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SoftwareUpdaterEngine _updaterEngine = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _progressMessage = "Ready".T();
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

    public ObservableCollection<SoftwareUpdateInfo> Updates { get; } = new();

    public UpdaterViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _updaterEngine.OutputReceived += msg => _dispatcherQueue.TryEnqueue(() => ProgressMessage = msg);
        _ = ScanUpdatesAsync();
    }

    public async Task ScanUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Updates.Clear();
        ProgressMessage = "Auditing winget packages database...".T();

        try
        {
            var list = await _updaterEngine.ScanUpdatesAsync("winget");
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    Updates.Add(item);
                }
                ProgressMessage = string.Format("Updates scan completed. {0} packages available.".T(), Updates.Count);
                IsBusy = false;
            });
        }
        catch
        {
            IsBusy = false;
        }
    }

    public async Task UpdateAllAppsAsync()
    {
        if (Updates.Count == 0 || IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;

        try
        {
            double step = 100.0 / Updates.Count;
            double current = 0;

            for (int i = 0; i < Updates.Count; i++)
            {
                var app = Updates[i];
                app.UpdateStatus = "Updating...".T();
                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, Updates.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, "winget");
                _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? "Completed".T() : "Failed".T(); });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "All background installations complete.".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Updates failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

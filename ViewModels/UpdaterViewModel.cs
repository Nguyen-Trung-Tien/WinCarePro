using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class UpdaterViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SoftwareUpdaterEngine _engine = new();

    private bool _isScanning;
    private bool _isUpdating;
    private string _progressMessage = "Ready to scan for application updates";
    private int _progressPercent;

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        set => SetProperty(ref _isUpdating, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public ObservableCollection<SoftwareUpdateInfo> Updates { get; } = new();

    public UpdaterViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += LogText;
    }

    private void LogText(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ProgressMessage = msg;
        });
    }

    public async Task ScanUpdatesAsync()
    {
        if (IsScanning || IsUpdating) return;
        IsScanning = true;
        ProgressPercent = 10;
        Updates.Clear();

        try
        {
            var list = await _engine.ScanUpdatesAsync();
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    Updates.Add(item);
                }
                ProgressPercent = 100;
                ProgressMessage = $"Scan complete. {Updates.Count} updates found.";
                IsScanning = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Scan failed: {ex.Message}";
                IsScanning = false;
            });
        }
    }

    public async Task UpdateSelectedAppsAsync()
    {
        if (IsScanning || IsUpdating || Updates.Count == 0) return;
        IsUpdating = true;
        ProgressPercent = 0;

        var selected = Updates.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ProgressMessage = "No applications selected.";
            IsUpdating = false;
            return;
        }

        try
        {
            double step = 100.0 / selected.Count;
            double current = 0;

            foreach (var app in selected)
            {
                app.UpdateStatus = "Updating...";
                ProgressMessage = $"Updating {app.Name}...";
                
                bool ok = await _engine.UpdateApplicationAsync(app.Id);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    app.UpdateStatus = ok ? "Completed" : "Failed";
                    // Refresh bindings in view
                    var idx = Updates.IndexOf(app);
                    if (idx >= 0)
                    {
                        Updates[idx] = app;
                    }
                });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "Selected applications successfully updated.";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    public async Task UpdateAllAppsAsync()
    {
        foreach (var app in Updates)
        {
            app.IsSelected = true;
        }
        await UpdateSelectedAppsAsync();
    }
}

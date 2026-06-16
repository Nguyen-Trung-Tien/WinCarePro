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
    private int _selectedCount;

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

    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
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

    public void RefreshSelectedCount()
    {
        SelectedCount = Updates.Count(x => x.IsSelected);
    }

    public void SelectAllApps()
    {
        foreach (var app in Updates)
        {
            app.IsSelected = true;
        }
        RefreshSelectedCount();
    }

    public void DeselectAllApps()
    {
        foreach (var app in Updates)
        {
            app.IsSelected = false;
        }
        RefreshSelectedCount();
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
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SoftwareUpdateInfo.IsSelected))
                        {
                            RefreshSelectedCount();
                        }
                    };
                    Updates.Add(item);
                }
                ProgressPercent = 100;
                RefreshSelectedCount();
                ProgressMessage = $"Scan complete. {Updates.Count} updates available.";
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

    public async Task UpdateSingleAppAsync(SoftwareUpdateInfo app)
    {
        if (IsScanning || IsUpdating) return;
        IsUpdating = true;
        ProgressPercent = 0;

        try
        {
            app.UpdateStatus = "Updating...";
            ProgressMessage = $"Updating {app.Name}...";
            ProgressPercent = 30;

            bool ok = await _engine.UpdateApplicationAsync(app.Id);

            _dispatcherQueue.TryEnqueue(() =>
            {
                app.UpdateStatus = ok ? "Completed" : "Failed";
                ProgressPercent = 100;
                ProgressMessage = ok
                    ? $"{app.Name} updated successfully."
                    : $"Failed to update {app.Name}.";
            });
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

    public async Task UpdateSelectedAppsAsync()
    {
        if (IsScanning || IsUpdating || Updates.Count == 0) return;
        IsUpdating = true;
        ProgressPercent = 0;

        var selected = Updates.Where(x => x.IsSelected && x.UpdateStatus != "Completed").ToList();
        if (selected.Count == 0)
        {
            ProgressMessage = "No applications selected for update.";
            IsUpdating = false;
            return;
        }

        try
        {
            double step = 100.0 / selected.Count;
            double current = 0;
            int successCount = 0;
            int failCount = 0;

            foreach (var app in selected)
            {
                app.UpdateStatus = "Updating...";
                ProgressMessage = $"Updating {app.Name} ({successCount + failCount + 1}/{selected.Count})...";
                
                bool ok = await _engine.UpdateApplicationAsync(app.Id);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    app.UpdateStatus = ok ? "Completed" : "Failed";
                });

                if (ok) successCount++; else failCount++;

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = $"Update complete. {successCount} succeeded, {failCount} failed.";
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
        SelectAllApps();
        await UpdateSelectedAppsAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly List<SoftwareUpdateInfo> _allUpdates = new();

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

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _updateEngine = "winget";
    public string UpdateEngine
    {
        get => _updateEngine;
        set
        {
            if (SetProperty(ref _updateEngine, value))
            {
                _ = ScanUpdatesAsync();
            }
        }
    }

    private string _terminalLog = "";
    public string TerminalLog
    {
        get => _terminalLog;
        set => SetProperty(ref _terminalLog, value);
    }

    private bool _showLogPanel = false;
    public bool ShowLogPanel
    {
        get => _showLogPanel;
        set => SetProperty(ref _showLogPanel, value);
    }

    // Statistics properties
    private int _updatesCount;
    public int UpdatesCount
    {
        get => _updatesCount;
        set => SetProperty(ref _updatesCount, value);
    }

    private string _lastScanTime = "Never".T();
    public string LastScanTime
    {
        get => _lastScanTime;
        set => SetProperty(ref _lastScanTime, value);
    }

    private string _activeEngineName = "Windows Package Manager";
    public string ActiveEngineName
    {
        get => _activeEngineName;
        set => SetProperty(ref _activeEngineName, value);
    }

    private string _systemHealthStatus = "Unknown".T();
    public string SystemHealthStatus
    {
        get => _systemHealthStatus;
        set => SetProperty(ref _systemHealthStatus, value);
    }

    private string _systemHealthColor = "#FF3B82F6"; // Default Blue
    public string SystemHealthColor
    {
        get => _systemHealthColor;
        set => SetProperty(ref _systemHealthColor, value);
    }

    public bool HasSelectedUpdates => _allUpdates.Any(x => x.IsSelected);

    public ObservableCollection<SoftwareUpdateInfo> Updates { get; } = new();

    public UpdaterViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _updaterEngine.OutputReceived += msg => _dispatcherQueue.TryEnqueue(() => 
        {
            ProgressMessage = msg;
            TerminalLog += msg + "\n";
        });
        _ = ScanUpdatesAsync();
    }

    public async Task ScanUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        _allUpdates.Clear();
        Updates.Clear();
        ProgressMessage = "Auditing winget packages database...".T();
        TerminalLog += string.Format("[WinCare] Scanning updates using {0} engine...\n".T(), UpdateEngine);

        try
        {
            var list = await _updaterEngine.ScanUpdatesAsync(UpdateEngine);
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    _allUpdates.Add(item);
                }
                LastScanTime = DateTime.Now.ToString("HH:mm:ss");
                ApplyFilters();
                ProgressMessage = string.Format("Updates scan completed. {0} packages available.".T(), UpdatesCount);
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Scan failed: {0}".T(), ex.Message);
            TerminalLog += string.Format("[Error] {0}\n", ex.Message);
            IsBusy = false;
        }
    }

    public void ApplyFilters()
    {
        Updates.Clear();
        var query = SearchText.Trim();
        var list = _allUpdates.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            list = list.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                   x.Id.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in list)
        {
            item.PropertyChanged -= OnAppPropertyChanged;
            item.PropertyChanged += OnAppPropertyChanged;
            Updates.Add(item);
        }

        UpdateStatistics();
    }

    private void OnAppPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoftwareUpdateInfo.IsSelected))
        {
            OnPropertyChanged(nameof(HasSelectedUpdates));
        }
    }

    private void UpdateStatistics()
    {
        UpdatesCount = _allUpdates.Count;
        int completedCount = _allUpdates.Count(x => x.UpdateStatus == "Completed");
        int remainingUpdatesCount = UpdatesCount - completedCount;

        if (remainingUpdatesCount == 0)
        {
            SystemHealthStatus = "System Up-to-Date".T();
            SystemHealthColor = "#FF10B981"; // Green
        }
        else
        {
            SystemHealthStatus = string.Format("Action Required ({0} Updates)".T(), remainingUpdatesCount);
            SystemHealthColor = "#FFF59E0B"; // Amber
        }

        ActiveEngineName = UpdateEngine == "winget" ? "Windows Package Manager" : "WinCare Direct Downloader";
        OnPropertyChanged(nameof(HasSelectedUpdates));
    }

    public void SetAllSelection(bool isSelected)
    {
        foreach (var app in Updates)
        {
            app.IsSelected = isSelected;
        }
        OnPropertyChanged(nameof(HasSelectedUpdates));
    }

    public async Task UpdateSelectedAppsAsync()
    {
        var selected = Updates.Where(x => x.IsSelected && x.UpdateStatus != "Completed").ToList();
        if (selected.Count == 0 || IsBusy) return;

        IsBusy = true;
        ProgressPercent = 0;
        TerminalLog += string.Format("[WinCare] Starting installation for {0} selected packages...\n".T(), selected.Count);

        try
        {
            double step = 100.0 / selected.Count;
            double current = 0;

            for (int i = 0; i < selected.Count; i++)
            {
                var app = selected[i];
                app.UpdateStatus = "Updating...".T();
                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, selected.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
                _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? "Completed".T() : "Failed".T(); });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "Selected package installations complete.".T();
            TerminalLog += "[WinCare] Selected package installations complete.\n".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Updates failed: {0}".T(), ex.Message);
            TerminalLog += string.Format("[Error] {0}\n", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateStatistics();
        }
    }

    public async Task UpdateSingleAppAsync(SoftwareUpdateInfo app)
    {
        if (IsBusy || app == null || app.UpdateStatus == "Completed" || app.UpdateStatus == "Updating...") return;

        IsBusy = true;
        app.UpdateStatus = "Updating...".T();
        ProgressMessage = string.Format("Silent updating {0}...".T(), app.Name);
        TerminalLog += string.Format("[WinCare] Starting single update for {0}...\n".T(), app.Name);

        try
        {
            bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
            _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? "Completed".T() : "Failed".T(); });
            
            ProgressMessage = ok ? string.Format("Successfully updated {0}".T(), app.Name) : string.Format("Failed to update {0}".T(), app.Name);
            TerminalLog += string.Format("[WinCare] Single update finished. Status: {0}\n".T(), ok ? "Success" : "Failed");
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Update failed: {0}".T(), ex.Message);
            TerminalLog += string.Format("[Error] {0}\n", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateStatistics();
        }
    }

    public async Task UpdateAllAppsAsync()
    {
        if (Updates.Count == 0 || IsBusy) return;
        IsBusy = true;
        ProgressPercent = 0;
        TerminalLog += "[WinCare] Starting update for all packages...\n".T();

        try
        {
            double step = 100.0 / Updates.Count;
            double current = 0;

            for (int i = 0; i < Updates.Count; i++)
            {
                var app = Updates[i];
                app.UpdateStatus = "Updating...".T();
                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, Updates.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
                _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? "Completed".T() : "Failed".T(); });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "All background installations complete.".T();
            TerminalLog += "[WinCare] All background installations complete.\n".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Updates failed: {0}".T(), ex.Message);
            TerminalLog += string.Format("[Error] {0}\n", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateStatistics();
        }
    }
}

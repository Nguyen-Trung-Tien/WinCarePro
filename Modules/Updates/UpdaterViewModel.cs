using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class UpdaterViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SoftwareUpdaterEngine _updaterEngine = App.Services?.GetService<SoftwareUpdaterEngine>() ?? new();
    private readonly List<SoftwareUpdateInfo> _allUpdates = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetPropertyOnUI(() => _isBusy, v => _isBusy = v, value);
    }

    private string _progressMessage = "Ready".T();
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetPropertyOnUI(() => _progressMessage, v => _progressMessage = v, value);
    }

    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetPropertyOnUI(() => _progressPercent, v => _progressPercent = v, value);
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetPropertyOnUI(() => _searchText, v => 
            {
                _searchText = v;
                ApplyFilters();
            }, value);
        }
    }

    private string _updateEngine = "winget";
    public string UpdateEngine
    {
        get => _updateEngine;
        set
        {
            SetPropertyOnUI(() => _updateEngine, v => 
            {
                _updateEngine = v;
                _ = ScanUpdatesAsync();
            }, value);
        }
    }

    private string _terminalLog = "";
    public string TerminalLog
    {
        get => _terminalLog;
        set => SetPropertyOnUI(() => _terminalLog, v => _terminalLog = v, value);
    }

    private bool _showLogPanel = false;
    public bool ShowLogPanel
    {
        get => _showLogPanel;
        set => SetPropertyOnUI(() => _showLogPanel, v => _showLogPanel = v, value);
    }

    // Filter Tab Properties
    private string _selectedStatusFilter = "All";
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            SetPropertyOnUI(() => _selectedStatusFilter, v =>
            {
                _selectedStatusFilter = v;
                ApplyFilters();
            }, value);
        }
    }

    private int _pendingUpdatesCount;
    public int PendingUpdatesCount
    {
        get => _pendingUpdatesCount;
        set => SetPropertyOnUI(() => _pendingUpdatesCount, v => _pendingUpdatesCount = v, value);
    }

    private int _updatingCount;
    public int UpdatingCount
    {
        get => _updatingCount;
        set => SetPropertyOnUI(() => _updatingCount, v => _updatingCount = v, value);
    }

    private int _completedCount;
    public int CompletedCount
    {
        get => _completedCount;
        set => SetPropertyOnUI(() => _completedCount, v => _completedCount = v, value);
    }

    // Statistics properties
    private int _updatesCount;
    public int UpdatesCount
    {
        get => _updatesCount;
        set => SetPropertyOnUI(() => _updatesCount, v => _updatesCount = v, value);
    }

    private string _lastScanTime = "Never".T();
    public string LastScanTime
    {
        get => _lastScanTime;
        set => SetPropertyOnUI(() => _lastScanTime, v => _lastScanTime = v, value);
    }

    private string _activeEngineName = "Windows Package Manager";
    public string ActiveEngineName
    {
        get => _activeEngineName;
        set => SetPropertyOnUI(() => _activeEngineName, v => _activeEngineName = v, value);
    }

    private string _systemHealthStatus = "Unknown".T();
    public string SystemHealthStatus
    {
        get => _systemHealthStatus;
        set => SetPropertyOnUI(() => _systemHealthStatus, v => _systemHealthStatus = v, value);
    }

    private string _systemHealthColor = "#FF3B82F6"; // Default Blue
    public string SystemHealthColor
    {
        get => _systemHealthColor;
        set => SetPropertyOnUI(() => _systemHealthColor, v => _systemHealthColor = v, value);
    }

    public bool HasSelectedUpdates => _allUpdates.Any(x => x.IsSelected && x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted);

    public ObservableCollection<SoftwareUpdateInfo> Updates { get; } = new();

    public UpdaterViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private void OnOutputReceived(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() => 
        {
            ProgressMessage = msg;
            TerminalLog += msg + "\n";
        });
    }

    private void OnItemProgressChanged(string appId, int percent, string statusText)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            var app = Updates.FirstOrDefault(x => x.Id.Equals(appId, StringComparison.OrdinalIgnoreCase));
            if (app != null)
            {
                app.DownloadProgress = percent;
                app.IsIndeterminate = (percent <= 0);
                app.ProgressText = statusText;
            }
            ProgressPercent = percent;
            ProgressMessage = statusText;
        });
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            UpdateStatistics();
            ApplyFilters();
        });
    }

    public void Initialize()
    {
        // Unsubscribe first to avoid double registration
        _updaterEngine.OutputReceived -= OnOutputReceived;
        _updaterEngine.OutputReceived += OnOutputReceived;
        _updaterEngine.ItemProgressChanged -= OnItemProgressChanged;
        _updaterEngine.ItemProgressChanged += OnItemProgressChanged;
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        _ = ScanUpdatesAsync();
    }

    public void Cleanup()
    {
        _updaterEngine.OutputReceived -= OnOutputReceived;
        _updaterEngine.ItemProgressChanged -= OnItemProgressChanged;
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
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
            var list = await Task.Run(() => _updaterEngine.ScanUpdatesAsync(UpdateEngine));
            _dispatcherQueue?.TryEnqueue(() =>
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

        if (SelectedStatusFilter == "Pending")
        {
            list = list.Where(x => x.UpdateStatus == SoftwareUpdateInfo.StatusAvailable || x.UpdateStatus == SoftwareUpdateInfo.StatusFailed);
        }
        else if (SelectedStatusFilter == "Updating")
        {
            list = list.Where(x => x.UpdateStatus == SoftwareUpdateInfo.StatusUpdating);
        }
        else if (SelectedStatusFilter == "Completed")
        {
            list = list.Where(x => x.UpdateStatus == SoftwareUpdateInfo.StatusCompleted);
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
        if (e.PropertyName == nameof(SoftwareUpdateInfo.IsSelected) ||
            e.PropertyName == nameof(SoftwareUpdateInfo.UpdateStatus))
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                UpdateStatistics();
            });
        }
    }

    private void UpdateStatistics()
    {
        PendingUpdatesCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusAvailable || x.UpdateStatus == SoftwareUpdateInfo.StatusFailed);
        UpdatingCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusUpdating);
        CompletedCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusCompleted);
        UpdatesCount = PendingUpdatesCount;

        if (PendingUpdatesCount == 0)
        {
            SystemHealthStatus = "System Up-to-Date".T();
            SystemHealthColor = "#FF10B981"; // Green
        }
        else
        {
            SystemHealthStatus = string.Format("Action Required ({0} Updates)".T(), PendingUpdatesCount);
            SystemHealthColor = "#FFF59E0B"; // Amber
        }

        ActiveEngineName = UpdateEngine == "winget" ? "Windows Package Manager" : "WinCare Direct Downloader";
        OnPropertyChanged(nameof(HasSelectedUpdates));
        OnPropertyChanged(nameof(SelectedStatusFilter));
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
        var selected = Updates.Where(x => x.IsSelected && x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted).ToList();
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
                app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
                app.DownloadProgress = 0;
                app.IsIndeterminate = true;
                app.ProgressText = "Preparing...".T();

                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, selected.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
                _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; });

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
        if (IsBusy || app == null || app.UpdateStatus == SoftwareUpdateInfo.StatusCompleted || app.UpdateStatus == SoftwareUpdateInfo.StatusUpdating) return;

        IsBusy = true;
        app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
        app.DownloadProgress = 0;
        app.IsIndeterminate = true;
        app.ProgressText = "Preparing...".T();

        ProgressMessage = string.Format("Silent updating {0}...".T(), app.Name);
        TerminalLog += string.Format("[WinCare] Starting single update for {0}...\n".T(), app.Name);

        try
        {
            bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
            _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; });
            
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
                app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
                app.DownloadProgress = 0;
                app.IsIndeterminate = true;
                app.ProgressText = "Preparing...".T();

                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, Updates.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine);
                _dispatcherQueue.TryEnqueue(() => { app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; });

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

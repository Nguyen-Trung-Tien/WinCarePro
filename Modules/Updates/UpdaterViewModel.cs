using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class UpdaterViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly SoftwareUpdaterEngine _updaterEngine = App.Services?.GetService<SoftwareUpdaterEngine>() ?? new();
    private readonly List<SoftwareUpdateInfo> _allUpdates = new();
    private readonly DispatcherQueueTimer? _searchDebounceTimer;
    private CancellationTokenSource? _operationCts;
    private bool _isDisposed;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetPropertyOnUI(() => _isBusy, v => 
            {
                _isBusy = v;
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(IsNotBusy));
            }, value);
        }
    }

    public bool IsNotBusy => !_isBusy;
    public bool CanCancel => _isBusy && _operationCts != null && !_operationCts.IsCancellationRequested;

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

    // Live Download & Telemetry HUD Properties
    private string _activeUpdatingAppName = "Software Package".T();
    public string ActiveUpdatingAppName
    {
        get => _activeUpdatingAppName;
        set => SetPropertyOnUI(() => _activeUpdatingAppName, v => _activeUpdatingAppName = v, value);
    }

    private string _currentDownloadUrl = "";
    public string CurrentDownloadUrl
    {
        get => _currentDownloadUrl;
        set => SetPropertyOnUI(() => _currentDownloadUrl, v => 
        {
            _currentDownloadUrl = v;
            OnPropertyChanged(nameof(HasDownloadUrl));
        }, value);
    }

    public bool HasDownloadUrl => !string.IsNullOrWhiteSpace(_currentDownloadUrl);

    private string _currentBytesProgress = "";
    public string CurrentBytesProgress
    {
        get => _currentBytesProgress;
        set => SetPropertyOnUI(() => _currentBytesProgress, v => 
        {
            _currentBytesProgress = v;
            OnPropertyChanged(nameof(HasBytesProgress));
        }, value);
    }

    public bool HasBytesProgress => !string.IsNullOrWhiteSpace(_currentBytesProgress);

    private string _currentSpeedText = "";
    public string CurrentSpeedText
    {
        get => _currentSpeedText;
        set => SetPropertyOnUI(() => _currentSpeedText, v => _currentSpeedText = v, value);
    }

    private string _currentPhase = "Updating".T();
    public string CurrentPhase
    {
        get => _currentPhase;
        set => SetPropertyOnUI(() => _currentPhase, v => _currentPhase = v, value);
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
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Start();
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
                ActiveEngineName = v == "winget" ? "Windows Package Manager" : "WinCare Direct Downloader";
                _ = ScanUpdatesAsync();
            }, value);
        }
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

    private int _totalFoundCount;
    public int TotalFoundCount
    {
        get => _totalFoundCount;
        set => SetPropertyOnUI(() => _totalFoundCount, v => _totalFoundCount = v, value);
    }

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

    private double _systemHealthScore = 100;
    public double SystemHealthScore
    {
        get => _systemHealthScore;
        set => SetPropertyOnUI(() => _systemHealthScore, v => _systemHealthScore = v, value);
    }

    private string _systemHealthStatus = "System Up-to-Date".T();
    public string SystemHealthStatus
    {
        get => _systemHealthStatus;
        set => SetPropertyOnUI(() => _systemHealthStatus, v => _systemHealthStatus = v, value);
    }

    private string _systemHealthColor = "#FF10B981"; // Emerald Green
    public string SystemHealthColor
    {
        get => _systemHealthColor;
        set => SetPropertyOnUI(() => _systemHealthColor, v => _systemHealthColor = v, value);
    }

    public bool HasSelectedUpdates => _allUpdates.Any(x => x.IsSelected && x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted);
    public int SelectedCount => _allUpdates.Count(x => x.IsSelected && x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted);

    public ObservableCollection<SoftwareUpdateInfo> Updates { get; } = new();

    public UpdaterViewModel()
    {
        _dispatcherQueue = App.MainDispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        if (_dispatcherQueue != null)
        {
            _searchDebounceTimer = _dispatcherQueue.CreateTimer();
            _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(150);
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ApplyFilters();
            };
        }
    }

    private void OnOutputReceived(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ProgressMessage = msg;
        });
    }

    private void OnItemProgressChanged(string appId, int percent, string statusText)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            var app = Updates.FirstOrDefault(x => x.Id.Equals(appId, StringComparison.OrdinalIgnoreCase)) ??
                      _allUpdates.FirstOrDefault(x => x.Id.Equals(appId, StringComparison.OrdinalIgnoreCase));
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

    private void OnUpdateProgressReported(SoftwareUpdateProgressReport report)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            var app = Updates.FirstOrDefault(x => x.Id.Equals(report.AppId, StringComparison.OrdinalIgnoreCase)) ??
                      _allUpdates.FirstOrDefault(x => x.Id.Equals(report.AppId, StringComparison.OrdinalIgnoreCase));
            
            if (app != null)
            {
                app.DownloadProgress = report.Percent;
                app.IsIndeterminate = (report.Percent <= 0);
                app.ProgressText = report.StatusText;
                if (!string.IsNullOrEmpty(report.DownloadUrl)) app.DownloadUrl = report.DownloadUrl;
                if (!string.IsNullOrEmpty(report.BytesProgress)) app.BytesProgress = report.BytesProgress;
                if (!string.IsNullOrEmpty(report.SpeedText)) app.SpeedText = report.SpeedText;
                if (!string.IsNullOrEmpty(report.Phase)) app.CurrentPhase = report.Phase;
                ActiveUpdatingAppName = app.Name;
            }
            else
            {
                ActiveUpdatingAppName = report.AppId;
            }

            if (!string.IsNullOrEmpty(report.DownloadUrl)) CurrentDownloadUrl = report.DownloadUrl;
            if (!string.IsNullOrEmpty(report.BytesProgress)) CurrentBytesProgress = report.BytesProgress;
            if (!string.IsNullOrEmpty(report.SpeedText)) CurrentSpeedText = report.SpeedText;
            if (!string.IsNullOrEmpty(report.Phase)) CurrentPhase = report.Phase.T();

            ProgressPercent = report.Percent;
            ProgressMessage = report.StatusText;
        });
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (_isDisposed) return;
            UpdateStatistics();
            ApplyFilters();
        });
    }

    public void Initialize()
    {
        _isDisposed = false;
        _updaterEngine.OutputReceived -= OnOutputReceived;
        _updaterEngine.OutputReceived += OnOutputReceived;
        _updaterEngine.ItemProgressChanged -= OnItemProgressChanged;
        _updaterEngine.ItemProgressChanged += OnItemProgressChanged;
        _updaterEngine.UpdateProgressReported -= OnUpdateProgressReported;
        _updaterEngine.UpdateProgressReported += OnUpdateProgressReported;
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        if (_allUpdates.Count == 0 && !IsBusy)
        {
            _ = ScanUpdatesAsync();
        }
    }

    public void Cleanup()
    {
        CancelOperations();
        try { _operationCts?.Dispose(); } catch { }
        _operationCts = null;
        _searchDebounceTimer?.Stop();
        _updaterEngine.OutputReceived -= OnOutputReceived;
        _updaterEngine.ItemProgressChanged -= OnItemProgressChanged;
        _updaterEngine.UpdateProgressReported -= OnUpdateProgressReported;
        IsBusy = false;
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    public void CancelOperations()
    {
        if (_operationCts != null && !_operationCts.IsCancellationRequested)
        {
            try { _operationCts.Cancel(); } catch { }
            ProgressMessage = "Operation cancelled by user.".T();
        }
    }

    public async Task ScanUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { _operationCts?.Dispose(); } catch { }
        _operationCts = new CancellationTokenSource();
        var ct = _operationCts.Token;

        _allUpdates.Clear();
        Updates.Clear();
        ProgressMessage = "Auditing software packages and repositories...".T();
        ProgressPercent = 0;

        try
        {
            var list = await Task.Run(async () => await _updaterEngine.ScanUpdatesAsync(UpdateEngine, ct), ct);
            
            _dispatcherQueue?.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    _allUpdates.Add(item);
                }
                LastScanTime = DateTime.Now.ToString("HH:mm:ss");
                ApplyFilters();
                ProgressMessage = string.Format("Updates scan completed. {0} packages found.".T(), list.Count);
                ProgressPercent = 100;
                IsBusy = false;
            });
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Scan cancelled.".T();
            IsBusy = false;
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Scan failed: {0}".T(), ex.Message);
            IsBusy = false;
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(UpdateStatistics);
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
            _dispatcherQueue?.TryEnqueue(UpdateStatistics);
        }
    }

    private void UpdateStatistics()
    {
        PendingUpdatesCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusAvailable || x.UpdateStatus == SoftwareUpdateInfo.StatusFailed);
        UpdatingCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusUpdating);
        CompletedCount = _allUpdates.Count(x => x.UpdateStatus == SoftwareUpdateInfo.StatusCompleted);
        TotalFoundCount = _allUpdates.Count;
        UpdatesCount = PendingUpdatesCount;

        if (PendingUpdatesCount == 0)
        {
            SystemHealthStatus = "All Packages Up-to-Date".T();
            SystemHealthScore = 100.0;
            SystemHealthColor = "#FF10B981"; // Emerald Green
        }
        else
        {
            SystemHealthStatus = string.Format("Action Required ({0} Updates)".T(), PendingUpdatesCount);
            SystemHealthScore = Math.Max(100.0 - (PendingUpdatesCount * 15.0), 25.0);
            SystemHealthColor = PendingUpdatesCount > 3 ? "#FFEF4444" : "#FFF59E0B"; // Red or Amber
        }

        ActiveEngineName = UpdateEngine == "winget" ? "Windows Package Manager" : "WinCare Direct Downloader";
        OnPropertyChanged(nameof(HasSelectedUpdates));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedStatusFilter));
    }

    public void SetAllSelection(bool isSelected)
    {
        foreach (var app in Updates)
        {
            if (app.CanUpdate)
            {
                app.IsSelected = isSelected;
            }
        }
        OnPropertyChanged(nameof(HasSelectedUpdates));
        OnPropertyChanged(nameof(SelectedCount));
    }

    public async Task UpdateSelectedAppsAsync()
    {
        var selected = Updates.Where(x => x.IsSelected && x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted).ToList();
        if (selected.Count == 0 || IsBusy) return;

        IsBusy = true;
        try { _operationCts?.Dispose(); } catch { }
        _operationCts = new CancellationTokenSource();
        var ct = _operationCts.Token;
        ProgressPercent = 0;

        try
        {
            double step = 100.0 / selected.Count;
            double current = 0;

            for (int i = 0; i < selected.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var app = selected[i];
                app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
                app.DownloadProgress = 0;
                app.IsIndeterminate = true;
                app.ProgressText = "Preparing...".T();

                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, selected.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine, ct);
                _dispatcherQueue?.TryEnqueue(() => 
                { 
                    app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; 
                });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "Selected package installations complete.".T();
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Batch update cancelled.".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Updates failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsBusy = false;
            _dispatcherQueue?.TryEnqueue(UpdateStatistics);
        }
    }

    public async Task UpdateSingleAppAsync(SoftwareUpdateInfo app)
    {
        if (IsBusy || app == null || app.UpdateStatus == SoftwareUpdateInfo.StatusCompleted || app.UpdateStatus == SoftwareUpdateInfo.StatusUpdating) return;

        IsBusy = true;
        try { _operationCts?.Dispose(); } catch { }
        _operationCts = new CancellationTokenSource();
        var ct = _operationCts.Token;

        app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
        app.DownloadProgress = 0;
        app.IsIndeterminate = true;
        app.ProgressText = "Preparing...".T();

        ProgressMessage = string.Format("Silent updating {0}...".T(), app.Name);

        try
        {
            bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine, ct);
            _dispatcherQueue?.TryEnqueue(() => 
            { 
                app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; 
            });
            
            ProgressMessage = ok ? string.Format("Successfully updated {0}".T(), app.Name) : string.Format("Failed to update {0}".T(), app.Name);
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Update cancelled.".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Update failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsBusy = false;
            _dispatcherQueue?.TryEnqueue(UpdateStatistics);
        }
    }

    public async Task UpdateAllAppsAsync()
    {
        if (Updates.Count == 0 || IsBusy) return;
        var pending = Updates.Where(x => x.UpdateStatus != SoftwareUpdateInfo.StatusCompleted).ToList();
        if (pending.Count == 0) return;

        IsBusy = true;
        try { _operationCts?.Dispose(); } catch { }
        _operationCts = new CancellationTokenSource();
        var ct = _operationCts.Token;
        ProgressPercent = 0;

        try
        {
            double step = 100.0 / pending.Count;
            double current = 0;

            for (int i = 0; i < pending.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var app = pending[i];
                app.UpdateStatus = SoftwareUpdateInfo.StatusUpdating;
                app.DownloadProgress = 0;
                app.IsIndeterminate = true;
                app.ProgressText = "Preparing...".T();

                ProgressMessage = string.Format("Silent updating {0} ({1}/{2})...".T(), app.Name, i + 1, pending.Count);

                bool ok = await _updaterEngine.UpdateApplicationAsync(app.Id, app.AvailableVersion, UpdateEngine, ct);
                _dispatcherQueue?.TryEnqueue(() => 
                { 
                    app.UpdateStatus = ok ? SoftwareUpdateInfo.StatusCompleted : SoftwareUpdateInfo.StatusFailed; 
                });

                current += step;
                ProgressPercent = (int)current;
            }

            ProgressPercent = 100;
            ProgressMessage = "All background installations complete.".T();
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Update all cancelled.".T();
        }
        catch (Exception ex)
        {
            ProgressMessage = string.Format("Updates failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsBusy = false;
            _dispatcherQueue?.TryEnqueue(UpdateStatistics);
        }
    }

    private readonly HardwareDriverEngine _driverEngine = App.Services?.GetService<HardwareDriverEngine>() ?? new();

    public async Task<DriverBackupResult> BackupDriversAsync(string? customPath = null)
    {
        if (IsBusy) return new DriverBackupResult { Success = false, Message = "Engine is currently busy." };
        IsBusy = true;
        ProgressMessage = "Initializing OEM hardware driver backup (pnputil)...".T();
        ProgressPercent = 5;

        try
        {
            var progress = new Progress<int>(p => ProgressPercent = p);
            var result = await _driverEngine.BackupThirdPartyDriversAsync(customPath, progress);

            ProgressPercent = 100;
            ProgressMessage = result.Message;
            return result;
        }
        catch (Exception ex)
        {
            ProgressMessage = "Driver backup error: " + ex.Message;
            return new DriverBackupResult { Success = false, Message = ex.Message };
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<DriverBackupResult> RestoreDriversAsync(string sourcePath)
    {
        if (IsBusy) return new DriverBackupResult { Success = false, Message = "Engine is currently busy." };
        IsBusy = true;
        ProgressMessage = "Restoring hardware drivers from backup folder...".T();
        ProgressPercent = 5;

        try
        {
            var progress = new Progress<int>(p => ProgressPercent = p);
            var result = await _driverEngine.RestoreDriversFromBackupAsync(sourcePath, progress);

            ProgressPercent = 100;
            ProgressMessage = result.Message;
            return result;
        }
        catch (Exception ex)
        {
            ProgressMessage = "Driver restore error: " + ex.Message;
            return new DriverBackupResult { Success = false, Message = ex.Message };
        }
        finally
        {
            IsBusy = false;
        }
    }
}

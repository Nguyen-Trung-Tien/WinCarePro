using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class RegistryViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly RegistryBackupEngine _engine;
    private CancellationTokenSource? _scanCts;
    private bool _isDisposed;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _statusText = "Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _scanProgress;
    public int ScanProgress
    {
        get => _scanProgress;
        set => SetProperty(ref _scanProgress, value);
    }

    public ObservableCollection<RegistryIssue> Issues { get; } = new();
    public ObservableCollection<RegistryBackupItem> Backups { get; } = new();

    private RegistryIssue? _selectedIssue;
    public RegistryIssue? SelectedIssue
    {
        get => _selectedIssue;
        set => SetProperty(ref _selectedIssue, value);
    }

    private int _safeCount;
    public int SafeCount
    {
        get => _safeCount;
        set => SetProperty(ref _safeCount, value);
    }

    private int _cautionCount;
    public int CautionCount
    {
        get => _cautionCount;
        set => SetProperty(ref _cautionCount, value);
    }

    public void UpdateCounts()
    {
        SafeCount = Issues.Count(i => i.RiskLevel == RegistryRiskLevel.Safe);
        CautionCount = Issues.Count(i => i.RiskLevel == RegistryRiskLevel.Caution);
    }

    public void SelectAll(bool select)
    {
        foreach (var issue in Issues)
        {
            issue.IsSelected = select;
        }
    }

    public void SelectSafeOnly()
    {
        foreach (var issue in Issues)
        {
            issue.IsSelected = (issue.RiskLevel == RegistryRiskLevel.Safe);
        }
    }

    public async Task RestoreBackupAsync(RegistryBackupItem item)
    {
        if (IsBusy || item == null || string.IsNullOrEmpty(item.FilePath) || _isDisposed) return;
        IsBusy = true;
        SetOperationState(OperationState.Preparing);
        StatusText = "Restoring registry snapshot...".T();

        try
        {
            bool ok = await _engine.RestoreRegistryBackupAsync(item.FilePath);
            if (ok)
            {
                StatusText = "Registry restored successfully from snapshot.".T();
                SetOperationState(OperationState.Completed);
                await ScanRegistryAsync();
            }
            else
            {
                StatusText = "Registry snapshot restore failed.".T();
                SetOperationState(OperationState.Failed);
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("RegistryViewModel.RestoreBackupAsync", ex);
            StatusText = "Restore failed: ".T() + ex.Message;
            SetOperationState(OperationState.Failed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairSingleIssueAsync(RegistryIssue issue)
    {
        if (IsBusy || issue == null || _isDisposed) return;
        IsBusy = true;
        SetOperationState(OperationState.Preparing);
        StatusText = $"Repairing issue '{issue.Title}'...".T();

        try
        {
            await _engine.CreateRegistryBackupAsync("AutoBeforeSingleFix");
            var singleList = new System.Collections.Generic.List<RegistryIssue> { issue };
            await _engine.FixRegistryIssuesAsync(singleList);
            Issues.Remove(issue);
            if (SelectedIssue == issue)
            {
                SelectedIssue = Issues.FirstOrDefault();
            }
            UpdateCounts();
            StatusText = "Issue repaired successfully.".T();
            SetOperationState(OperationState.Completed);
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("RegistryViewModel.RepairSingleIssueAsync", ex);
            StatusText = "Repair failed: ".T() + ex.Message;
            SetOperationState(OperationState.Failed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public RegistryViewModel() : this(null, null)
    {
    }

    public RegistryViewModel(RegistryBackupEngine? engine = null, DispatcherQueue? dispatcherQueue = null)
    {
        _engine = engine ?? App.Services?.GetService<RegistryBackupEngine>() ?? new();
        _dispatcherQueue = dispatcherQueue ?? SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;
        LoadBackups();
    }

    public void LoadBackups()
    {
        Backups.Clear();
        var list = _engine.GetRegistryBackupsList();
        foreach (var b in list)
        {
            Backups.Add(b);
        }
    }

    public void Initialize()
    {
        _isDisposed = false;
    }

    public void Cleanup()
    {
        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
        }
        catch { }
        IsBusy = false;
        SetOperationState(OperationState.Idle);
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
    }

    public void CancelScan()
    {
        try
        {
            _scanCts?.Cancel();
        }
        catch { }
        IsBusy = false;
        SetOperationState(OperationState.Idle);
    }

    public async Task ScanRegistryAsync()
    {
        if (IsBusy || _isDisposed) return;
        IsBusy = true;
        SetOperationState(OperationState.Running);
        ScanProgress = 0;
        StatusText = "Scanning registry for broken paths...".T();
        Issues.Clear();

        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
        }
        catch { }
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        try
        {
            var list = await Task.Run(() => _engine.ScanRegistryIssues(token), token);
            RunOnUI(() =>
            {
                if (token.IsCancellationRequested || _isDisposed) return;
                foreach (var issue in list)
                {
                    Issues.Add(issue);
                }
                UpdateCounts();
                SelectedIssue = Issues.FirstOrDefault();
                ScanProgress = 100;
                StatusText = string.Format("Scan complete. Found {0} issues.".T(), Issues.Count);
                SetOperationState(OperationState.Completed);
            });
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() =>
            {
                StatusText = "Scan cancelled.".T();
                ScanProgress = 0;
                SetOperationState(OperationState.Idle);
            });
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("RegistryViewModel.ScanRegistryAsync", ex);
            RunOnUI(() =>
            {
                if (_isDisposed) return;
                StatusText = "Scan failed: ".T() + ex.Message;
                SetOperationState(OperationState.Failed);
            });
        }
        finally
        {
            RunOnUI(() =>
            {
                IsBusy = false;
                if (token.IsCancellationRequested || _isDisposed)
                {
                    SetOperationState(OperationState.Idle);
                }
            });
        }
    }

    public async Task RepairSelectedAsync()
    {
        if (IsBusy || Issues.Count == 0 || _isDisposed) return;
        IsBusy = true;
        SetOperationState(OperationState.Preparing);
        StatusText = "Repairing selected registry issues...".T();

        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
        }
        catch { }
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        try
        {
            var selected = Issues.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                SetOperationState(OperationState.Idle);
                return;
            }

            StatusText = "Creating safety backup before repair...".T();
            await _engine.CreateRegistryBackupAsync("AutoBeforeRepair");
            token.ThrowIfCancellationRequested();

            SetOperationState(OperationState.Running);
            StatusText = "Repairing selected registry issues...".T();
            await _engine.FixRegistryIssuesAsync(selected, token);
            StatusText = string.Format("Repaired {0} registry issues.".T(), selected.Count);
            SetOperationState(OperationState.Completed);
            IsBusy = false;
            await ScanRegistryAsync();
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() =>
            {
                StatusText = "Repair cancelled.".T();
                SetOperationState(OperationState.Idle);
            });
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("RegistryViewModel.RepairSelectedAsync", ex);
            RunOnUI(() =>
            {
                StatusText = "Repair failed:".T() + " " + ex.Message;
                SetOperationState(OperationState.Failed);
            });
        }
        finally
        {
            RunOnUI(() =>
            {
                IsBusy = false;
                if (token.IsCancellationRequested || _isDisposed)
                {
                    SetOperationState(OperationState.Idle);
                }
            });
        }
    }

    public async Task BackupRegistryAsync()
    {
        if (IsBusy || _isDisposed) return;
        IsBusy = true;
        SetOperationState(OperationState.Running);
        StatusText = "Creating registry backup...".T();

        try
        {
            await _engine.CreateRegistryBackupAsync("UserBackup");
            StatusText = "Registry backup created successfully.".T();
            LoadBackups();
            SetOperationState(OperationState.Completed);
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("RegistryViewModel.BackupRegistryAsync", ex);
            StatusText = "Backup failed:".T() + " " + ex.Message;
            SetOperationState(OperationState.Failed);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

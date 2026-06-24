using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class ProcessViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ProcessService _processService = new();

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusText = "Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilterAndSort();
            }
        }
    }

    private string _sortColumn = "CpuUsage";
    private bool _isAscending;

    private ObservableCollection<ProcessInfo> _allProcesses = new();
    public ObservableCollection<ProcessInfo> Processes { get; } = new();

    private bool _isRunning = true; // kept for legacy compatibility; CancellationToken is the primary stop mechanism
    private CancellationTokenSource? _monitorCts;

    public ProcessViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = RefreshProcessesAsync();
        StartRunningProcessesMonitor();
    }

    private void StartRunningProcessesMonitor()
    {
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var list = await Task.Run(() => _processService.GetRunningProcessesAsync(), token);
                    if (token.IsCancellationRequested) break;
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        _allProcesses = new ObservableCollection<ProcessInfo>(list);
                        ApplyFilterAndSort();
                    });
                }
                catch { }

                try
                {
                    await Task.Delay(3000, token);
                }
                catch (TaskCanceledException) { break; }
            }
        });
    }

    public void StopMonitoring()
    {
        _isRunning = false;
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    public async Task RefreshProcessesAsync()
    {
        IsLoading = true;
        StatusText = "Refreshing process tree...".T();
        try
        {
            var list = await Task.Run(() => _processService.GetRunningProcessesAsync());
            _dispatcherQueue.TryEnqueue(() =>
            {
                _allProcesses = new ObservableCollection<ProcessInfo>(list);
                ApplyFilterAndSort();
                StatusText = string.Format("Monitoring {0} active processes.".T(), _allProcesses.Count);
            });
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Refresh failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task EndProcessAsync(int pid)
    {
        IsLoading = true;
        StatusText = string.Format("Terminating process PID {0}...".T(), pid);

        bool ok = await Task.Run(() => _processService.TerminateProcess(pid));
        if (ok)
        {
            StatusText = "Process terminated successfully.".T();
        }
        else
        {
            StatusText = "Failed to terminate process (Access Denied).".T();
        }
        await RefreshProcessesAsync();
    }

    public async Task EndProcessTreeAsync(int pid)
    {
        IsLoading = true;
        StatusText = string.Format("Terminating process tree for PID {0}...".T(), pid);

        bool ok = await _processService.TerminateProcessTreeAsync(pid);
        if (ok)
        {
            StatusText = "Process tree terminated successfully.".T();
        }
        else
        {
            StatusText = "Failed to terminate process tree.".T();
        }
        await RefreshProcessesAsync();
    }

    public void ChangeSort(string column)
    {
        if (_sortColumn == column)
        {
            _isAscending = !_isAscending;
        }
        else
        {
            _sortColumn = column;
            _isAscending = false;
        }
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var filtered = _allProcesses.AsEnumerable();

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            string query = SearchQuery.ToLower();
            filtered = filtered.Where(x => x.Name.ToLower().Contains(query) || x.Id.ToString().Contains(query) || x.Publisher.ToLower().Contains(query));
        }

        filtered = _sortColumn switch
        {
            "Id" => _isAscending ? filtered.OrderBy(x => x.Id) : filtered.OrderByDescending(x => x.Id),
            "Name" => _isAscending ? filtered.OrderBy(x => x.Name) : filtered.OrderByDescending(x => x.Name),
            "RamUsageBytes" => _isAscending ? filtered.OrderBy(x => x.RamUsageBytes) : filtered.OrderByDescending(x => x.RamUsageBytes),
            "DiskUsageMb" => _isAscending ? filtered.OrderBy(x => x.DiskUsageMb) : filtered.OrderByDescending(x => x.DiskUsageMb),
            _ => _isAscending ? filtered.OrderBy(x => x.CpuUsage) : filtered.OrderByDescending(x => x.CpuUsage),
        };

        Processes.Clear();
        foreach (var item in filtered)
        {
            Processes.Add(item);
        }
    }
}

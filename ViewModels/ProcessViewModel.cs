using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

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

    private string _statusText = "Ready";
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

    public ProcessViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = RefreshProcessesAsync();
        _ = StartRunningProcessesMonitor();
    }

    private async Task StartRunningProcessesMonitor()
    {
        while (true)
        {
            try
            {
                var list = await Task.Run(() => _processService.GetRunningProcessesAsync());
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _allProcesses = new ObservableCollection<ProcessInfo>(list);
                    ApplyFilterAndSort();
                });
            }
            catch { }

            await Task.Delay(3000);
        }
    }

    public async Task RefreshProcessesAsync()
    {
        IsLoading = true;
        StatusText = "Refreshing process tree...";
        try
        {
            var list = await Task.Run(() => _processService.GetRunningProcessesAsync());
            _dispatcherQueue.TryEnqueue(() =>
            {
                _allProcesses = new ObservableCollection<ProcessInfo>(list);
                ApplyFilterAndSort();
                StatusText = $"Monitoring {_allProcesses.Count} active processes.";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task EndProcessAsync(int pid)
    {
        IsLoading = true;
        StatusText = $"Terminating process PID {pid}...";

        bool ok = await Task.Run(() => _processService.TerminateProcess(pid));
        if (ok)
        {
            StatusText = "Process terminated successfully.";
        }
        else
        {
            StatusText = "Failed to terminate process (Access Denied).";
        }
        await RefreshProcessesAsync();
    }

    public async Task EndProcessTreeAsync(int pid)
    {
        IsLoading = true;
        StatusText = $"Terminating process tree for PID {pid}...";

        bool ok = await _processService.TerminateProcessTreeAsync(pid);
        if (ok)
        {
            StatusText = "Process tree terminated successfully.";
        }
        else
        {
            StatusText = "Failed to terminate process tree.";
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

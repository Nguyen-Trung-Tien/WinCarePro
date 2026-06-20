using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class ProcessViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ProcessService _service = new();

    private string _searchQuery = "";
    private bool _isBusy;
    private string _statusText = "Ready";
    private string _sortColumn = "CpuUsage";
    private bool _isAscending;

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

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private ObservableCollection<ProcessInfo> _allProcesses = new();
    public ObservableCollection<ProcessInfo> Processes { get; } = new();

    public ProcessViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = RefreshProcessesAsync();
    }

    public async Task RefreshProcessesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Refreshing process tree...";

        try
        {
            var list = await Task.Run(() => _service.GetRunningProcessesAsync());
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
            IsBusy = false;
        }
    }

    public async Task EndProcessAsync(int pid)
    {
        IsBusy = true;
        StatusText = $"Terminating process PID {pid}...";

        bool ok = await Task.Run(() => _service.TerminateProcess(pid));
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
        IsBusy = true;
        StatusText = $"Terminating process tree for PID {pid}...";

        bool ok = await _service.TerminateProcessTreeAsync(pid);
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

        // 1. Filter
        if (!string.IsNullOrEmpty(SearchQuery))
        {
            string query = SearchQuery.ToLower();
            filtered = filtered.Where(x => x.Name.ToLower().Contains(query) || x.Id.ToString().Contains(query) || x.Publisher.ToLower().Contains(query));
        }

        // 2. Sort
        filtered = _sortColumn switch
        {
            "Id" => _isAscending ? filtered.OrderBy(x => x.Id) : filtered.OrderByDescending(x => x.Id),
            "Name" => _isAscending ? filtered.OrderBy(x => x.Name) : filtered.OrderByDescending(x => x.Name),
            "RamUsageBytes" => _isAscending ? filtered.OrderBy(x => x.RamUsageBytes) : filtered.OrderByDescending(x => x.RamUsageBytes),
            "DiskUsageMb" => _isAscending ? filtered.OrderBy(x => x.DiskUsageMb) : filtered.OrderByDescending(x => x.DiskUsageMb),
            "NetworkUsageKb" => _isAscending ? filtered.OrderBy(x => x.NetworkUsageKb) : filtered.OrderByDescending(x => x.NetworkUsageKb),
            _ => _isAscending ? filtered.OrderBy(x => x.CpuUsage) : filtered.OrderByDescending(x => x.CpuUsage),
        };

        Processes.Clear();
        foreach (var item in filtered)
        {
            Processes.Add(item);
        }
    }
}

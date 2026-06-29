using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

    private bool _hideSystemProcesses;
    public bool HideSystemProcesses
    {
        get => _hideSystemProcesses;
        set
        {
            if (SetProperty(ref _hideSystemProcesses, value))
            {
                ApplyFilterAndSort();
            }
        }
    }

    private bool _highResourceOnly;
    public bool HighResourceOnly
    {
        get => _highResourceOnly;
        set
        {
            if (SetProperty(ref _highResourceOnly, value))
            {
                ApplyFilterAndSort();
            }
        }
    }

    private double _cpuUsageSummary;
    public double CpuUsageSummary
    {
        get => _cpuUsageSummary;
        set => SetProperty(ref _cpuUsageSummary, value);
    }

    private double _ramUsageSummary;
    public double RamUsageSummary
    {
        get => _ramUsageSummary;
        set => SetProperty(ref _ramUsageSummary, value);
    }

    private int _totalProcessCount;
    public int TotalProcessCount
    {
        get => _totalProcessCount;
        set => SetProperty(ref _totalProcessCount, value);
    }

    private string _sortColumn = "CpuUsage";
    private bool _isAscending;

    private List<ProcessInfo> _allProcesses = new();
    public ObservableCollection<ProcessInfo> Processes { get; } = new();

    private ProcessInfo? _selectedProcess;
    public ProcessInfo? SelectedProcess
    {
        get => _selectedProcess;
        set
        {
            if (SetProperty(ref _selectedProcess, value))
            {
                OnPropertyChanged(nameof(IsDetailsVisible));
                if (value != null)
                {
                    _ = LoadDetailedInfoAsync(value);
                }
            }
        }
    }

    public bool IsDetailsVisible => SelectedProcess != null;

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
            int tickCount = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var list = await _processService.GetRunningProcessesAsync();
                    if (token.IsCancellationRequested) break;

                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        _allProcesses = list;
                        
                        // Tier 1: Every 1.5s (each tick) - Update metrics in-place
                        SyncProcessMetrics(list);

                        // Tier 2: Every 4.5s (every 3 ticks) - Synchronize process additions/removals and re-sort
                        if (tickCount % 3 == 0)
                        {
                            SyncProcessStructure(list);
                            ApplyFilterAndSort();
                        }

                        UpdateStatsSummary(list);
                        tickCount++;
                    });
                }
                catch { }

                try
                {
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException) { break; }
            }
        });
    }

    private void SyncProcessMetrics(List<ProcessInfo> freshList)
    {
        var freshMap = freshList.ToDictionary(p => p.Id);

        foreach (var existing in Processes)
        {
            if (freshMap.TryGetValue(existing.Id, out var fresh))
            {
                existing.CpuUsage = fresh.CpuUsage;
                existing.RamUsageBytes = fresh.RamUsageBytes;
                existing.DiskUsageMb = fresh.DiskUsageMb;
                existing.NetworkUsageKb = fresh.NetworkUsageKb;
                existing.IconPath = fresh.IconPath;
            }
        }

        // Refresh selected process on-demand detailed info
        if (SelectedProcess != null)
        {
            _ = LoadDetailedInfoAsync(SelectedProcess);
        }
    }

    private void SyncProcessStructure(List<ProcessInfo> freshList)
    {
        var freshIds = new HashSet<int>(freshList.Select(x => x.Id));
        var currentIds = new HashSet<int>(Processes.Select(x => x.Id));

        // Remove processes that exited
        for (int i = Processes.Count - 1; i >= 0; i--)
        {
            if (!freshIds.Contains(Processes[i].Id))
            {
                if (SelectedProcess != null && SelectedProcess.Id == Processes[i].Id)
                {
                    SelectedProcess = null;
                }
                Processes.RemoveAt(i);
            }
        }

        // Add new processes
        foreach (var fresh in freshList)
        {
            if (!currentIds.Contains(fresh.Id))
            {
                Processes.Add(fresh);
            }
        }
    }

    private void UpdateStatsSummary(List<ProcessInfo> list)
    {
        double totalCpu = list.Sum(p => p.CpuUsage);
        CpuUsageSummary = Math.Min(100.0, totalCpu);
        TotalProcessCount = list.Count;

        try
        {
            var optEngine = new SystemOptimizerEngine();
            var ramStatus = optEngine.GetRamStatus();
            RamUsageSummary = ramStatus.percentage;
        }
        catch
        {
            RamUsageSummary = 0.0;
        }
    }

    private async Task LoadDetailedInfoAsync(ProcessInfo process)
    {
        try
        {
            var details = await Task.Run(() => _processService.GetDetailedProcessInfo(process.Id, process.Name));
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (SelectedProcess != null && SelectedProcess.Id == process.Id)
                {
                    SelectedProcess.ThreadCount = details.ThreadCount;
                    SelectedProcess.HandleCount = details.HandleCount;
                    SelectedProcess.StartTime = details.StartTime;
                    SelectedProcess.CommandLine = details.CommandLine;
                    SelectedProcess.PriorityClass = details.PriorityClass;
                    SelectedProcess.ParentPid = details.ParentPid;
                }
            });
        }
        catch { }
    }

    public void StopMonitoring()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    public async Task RefreshProcessesAsync()
    {
        IsLoading = true;
        StatusText = "Refreshing process list...".T();
        try
        {
            var list = await Task.Run(() => _processService.GetRunningProcessesAsync());
            _dispatcherQueue.TryEnqueue(() =>
            {
                _allProcesses = list;
                SyncProcessStructure(list);
                SyncProcessMetrics(list);
                UpdateStatsSummary(list);
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

    public async Task OptimizeMemoryAsync()
    {
        IsLoading = true;
        StatusText = "Optimizing system memory...".T();
        try
        {
            var optEngine = new SystemOptimizerEngine();
            var result = await optEngine.OptimizeRamAsync();
            double freedMb = result.memoryReclaimedBytes / 1024.0 / 1024.0;
            StatusText = string.Format("RAM Boost Complete: Freed {0:F1} MB by trimming working sets on {1} apps.".T(), freedMb, result.processesOptimized);
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Memory optimization failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
            await RefreshProcessesAsync();
        }
    }

    public async Task<bool> UpdateSelectedProcessPriorityAsync(string priorityStr)
    {
        if (SelectedProcess == null) return false;

        if (!Enum.TryParse<ProcessPriorityClass>(priorityStr, out var priority))
        {
            return false;
        }

        IsLoading = true;
        StatusText = string.Format("Updating priority for PID {0} to {1}...".T(), SelectedProcess.Id, priorityStr);

        bool ok = await Task.Run(() => _processService.SetProcessPriority(SelectedProcess.Id, SelectedProcess.Name, priority));
        if (ok)
        {
            SelectedProcess.PriorityClass = priorityStr;
            StatusText = "Process priority updated successfully.".T();
        }
        else
        {
            StatusText = "Failed to update process priority (Access Denied or Protected).".T();
        }
        IsLoading = false;
        return ok;
    }

    public async Task<bool> SuspendSelectedProcessAsync()
    {
        if (SelectedProcess == null) return false;

        IsLoading = true;
        StatusText = string.Format("Suspending process PID {0}...".T(), SelectedProcess.Id);

        bool ok = await Task.Run(() => _processService.SuspendProcess(SelectedProcess.Id, SelectedProcess.Name));
        if (ok)
        {
            SelectedProcess.Status = "Suspended";
            StatusText = "Process suspended successfully.".T();
        }
        else
        {
            StatusText = "Failed to suspend process (Access Denied or Protected).".T();
        }
        IsLoading = false;
        return ok;
    }

    public async Task<bool> ResumeSelectedProcessAsync()
    {
        if (SelectedProcess == null) return false;

        IsLoading = true;
        StatusText = string.Format("Resuming process PID {0}...".T(), SelectedProcess.Id);

        bool ok = await Task.Run(() => _processService.ResumeProcess(SelectedProcess.Id, SelectedProcess.Name));
        if (ok)
        {
            SelectedProcess.Status = "Running";
            StatusText = "Process resumed successfully.".T();
        }
        else
        {
            StatusText = "Failed to resume process (Access Denied or Protected).".T();
        }
        IsLoading = false;
        return ok;
    }

    public async Task EndProcessAsync(int pid, string name)
    {
        IsLoading = true;
        StatusText = string.Format("Terminating process {0} (PID {1})...".T(), name, pid);

        bool ok = await Task.Run(() => _processService.TerminateProcess(pid, name));
        if (ok)
        {
            StatusText = string.Format("Process {0} terminated successfully.".T(), name);
            if (SelectedProcess != null && SelectedProcess.Id == pid)
            {
                SelectedProcess = null;
            }
        }
        else
        {
            StatusText = string.Format("Failed to terminate process {0} (Access Denied or Protected).".T(), name);
        }
        await RefreshProcessesAsync();
    }

    public async Task EndProcessTreeAsync(int pid, string name)
    {
        IsLoading = true;
        StatusText = string.Format("Terminating process tree for {0} (PID {1})...".T(), name, pid);

        bool ok = await _processService.TerminateProcessTreeAsync(pid, name);
        if (ok)
        {
            StatusText = string.Format("Process tree for {0} terminated successfully.".T(), name);
            if (SelectedProcess != null && SelectedProcess.Id == pid)
            {
                SelectedProcess = null;
            }
        }
        else
        {
            StatusText = string.Format("Failed to terminate process tree for {0}.".T(), name);
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

    private bool IsSystemProcess(ProcessInfo p)
    {
        if (p.Id <= 4) return true;
        if (string.Equals(p.Publisher, "Microsoft Corporation", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.FilePath.ToLower().Contains("system32") || p.FilePath.ToLower().Contains("c:\\windows")) return true;
        return false;
    }

    private void ApplyFilterAndSort()
    {
        var filtered = _allProcesses.AsEnumerable();

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            string query = SearchQuery.ToLower();
            filtered = filtered.Where(x => x.Name.ToLower().Contains(query) || x.Id.ToString().Contains(query) || x.Publisher.ToLower().Contains(query));
        }

        if (HideSystemProcesses)
        {
            filtered = filtered.Where(x => !IsSystemProcess(x));
        }

        if (HighResourceOnly)
        {
            filtered = filtered.Where(x => x.CpuUsage > 1.0 || x.RamUsageBytes > 100 * 1024 * 1024);
        }

        filtered = _sortColumn switch
        {
            "Id" => _isAscending ? filtered.OrderBy(x => x.Id) : filtered.OrderByDescending(x => x.Id),
            "Name" => _isAscending ? filtered.OrderBy(x => x.Name) : filtered.OrderByDescending(x => x.Name),
            "RamUsageBytes" => _isAscending ? filtered.OrderBy(x => x.RamUsageBytes) : filtered.OrderByDescending(x => x.RamUsageBytes),
            "DiskUsageMb" => _isAscending ? filtered.OrderBy(x => x.DiskUsageMb) : filtered.OrderByDescending(x => x.DiskUsageMb),
            "NetworkUsageKb" => _isAscending ? filtered.OrderBy(x => x.NetworkUsageKb) : filtered.OrderByDescending(x => x.NetworkUsageKb),
            _ => _isAscending ? filtered.OrderBy(x => x.CpuUsage) : filtered.OrderByDescending(x => x.CpuUsage),
        };

        var targetList = filtered.ToList();

        // In-place sync ObservableCollection
        var targetIds = new HashSet<int>(targetList.Select(x => x.Id));
        for (int i = Processes.Count - 1; i >= 0; i--)
        {
            if (!targetIds.Contains(Processes[i].Id))
            {
                Processes.RemoveAt(i);
            }
        }

        for (int i = 0; i < targetList.Count; i++)
        {
            var targetItem = targetList[i];
            int existingIndex = -1;
            for (int j = 0; j < Processes.Count; j++)
            {
                if (Processes[j].Id == targetItem.Id)
                {
                    existingIndex = j;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                var existingItem = Processes[existingIndex];
                existingItem.CpuUsage = targetItem.CpuUsage;
                existingItem.RamUsageBytes = targetItem.RamUsageBytes;
                existingItem.DiskUsageMb = targetItem.DiskUsageMb;
                existingItem.NetworkUsageKb = targetItem.NetworkUsageKb;

                if (existingIndex != i)
                {
                    Processes.Move(existingIndex, i);
                }
            }
            else
            {
                Processes.Insert(i, targetItem);
            }
        }
    }
}

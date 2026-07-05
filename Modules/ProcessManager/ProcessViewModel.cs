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
        set => SetPropertyOnUI(() => _isLoading, v => _isLoading = v, value);
    }

    private string _statusText = "Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetPropertyOnUI(() => _statusText, v => _statusText = v, value);
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
        set => SetPropertyOnUI(() => _cpuUsageSummary, v => _cpuUsageSummary = v, value);
    }

    private double _ramUsageSummary;
    public double RamUsageSummary
    {
        get => _ramUsageSummary;
        set => SetPropertyOnUI(() => _ramUsageSummary, v => _ramUsageSummary = v, value);
    }

    private int _totalProcessCount;
    public int TotalProcessCount
    {
        get => _totalProcessCount;
        set => SetPropertyOnUI(() => _totalProcessCount, v => _totalProcessCount = v, value);
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
        DispatcherQueueInstance = _dispatcherQueue;
        StartRunningProcessesMonitor();
    }

    private void StartRunningProcessesMonitor()
    {
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;
        Task.Run(async () =>
        {
            // Perform the initial load sequentially to avoid race conditions.
            await RefreshProcessesAsync();
            if (token.IsCancellationRequested) return;

            int tickCount = 1; // Start from 1 because the initial load (acting as tick 0) is complete
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException) { break; }

                if (token.IsCancellationRequested) break;

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
                            ApplyFilterAndSort();
                        }

                        UpdateStatsSummary(list);
                        tickCount++;
                    });
                }
                catch { }
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
                ApplyFilterAndSort();
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

        // Save selected process ID to restore it later
        var selectedId = SelectedProcess?.Id;

        // Reset the collection if it's currently empty, or if the number of changes is large.
        // This avoids UI lag when filtering/typing.
        // We do not clear if it's a minor change (like periodic updates) to preserve scroll position.
        bool needsReset = Processes.Count == 0 || Math.Abs(Processes.Count - targetList.Count) > 10;

        if (needsReset)
        {
            Processes.Clear();
            foreach (var item in targetList)
            {
                Processes.Add(item);
            }
        }
        else
        {
            // In-place sync ObservableCollection (minimizes layout updates)
            var targetIds = new HashSet<int>(targetList.Select(x => x.Id));
            
            // Remove exited processes
            for (int i = Processes.Count - 1; i >= 0; i--)
            {
                if (!targetIds.Contains(Processes[i].Id))
                {
                    Processes.RemoveAt(i);
                }
            }

            // Sync order and insert new processes
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

        // Restore Selection
        if (selectedId.HasValue)
        {
            SelectedProcess = Processes.FirstOrDefault(x => x.Id == selectedId.Value);
        }
    }
}

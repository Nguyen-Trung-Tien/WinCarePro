using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Implementations;

namespace WinCarePro.ViewModels;

public class StartupViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly StartupEngine _startupEngine;
    private readonly IconCacheService _iconCache;
    private readonly ServiceSafetyService _safety;
    private readonly AuditLogService _audit;

    // Undo Stack Definition
    private class UndoAction
    {
        public string Type { get; set; } = ""; // Startup, Service, Task
        public string Target { get; set; } = ""; // Target Identifier (e.g. name or path)
        public object PreviousState { get; set; } = null!; // Previous configuration value
        public string Description { get; set; } = "";
    }

    private readonly Stack<UndoAction> _undoStack = new();

    // Data lists for filtering
    private readonly List<StartupEntry> _allStartupApps = new();
    private readonly List<ServiceEntry> _allServices = new();
    private readonly List<ScheduledTaskEntry> _allScheduledTasks = new();

    // Observable Collections bound to UI
    public ObservableCollection<StartupEntry> StartupApps { get; } = new();
    public ObservableCollection<ServiceEntry> Services { get; } = new();
    public ObservableCollection<ScheduledTaskEntry> ScheduledTasks { get; } = new();

    // Properties
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

    private string _loadingStatus = "";
    public string LoadingStatus
    {
        get => _loadingStatus;
        set => SetProperty(ref _loadingStatus, value);
    }

    // Dashboard Info
    private string _bootTimeFormatted = "N/A";
    public string BootTimeFormatted
    {
        get => _bootTimeFormatted;
        set => SetProperty(ref _bootTimeFormatted, value);
    }

    private double _optimizationScore = 100;
    public double OptimizationScore
    {
        get => _optimizationScore;
        set => SetProperty(ref _optimizationScore, value);
    }

    private int _totalStartupApps;
    public int TotalStartupApps
    {
        get => _totalStartupApps;
        set => SetProperty(ref _totalStartupApps, value);
    }

    private int _totalRunningServices;
    public int TotalRunningServices
    {
        get => _totalRunningServices;
        set => SetProperty(ref _totalRunningServices, value);
    }

    private int _totalScheduledTasks;
    public int TotalScheduledTasks
    {
        get => _totalScheduledTasks;
        set => SetProperty(ref _totalScheduledTasks, value);
    }

    private string _optimizationSuggestions = "";
    public string OptimizationSuggestions
    {
        get => _optimizationSuggestions;
        set => SetProperty(ref _optimizationSuggestions, value);
    }

    // Search and Filters
    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilters();
            }
        }
    }

    private bool _filterMicrosoft = true;
    public bool FilterMicrosoft
    {
        get => _filterMicrosoft;
        set
        {
            if (SetProperty(ref _filterMicrosoft, value))
            {
                ApplyFilters();
            }
        }
    }

    private bool _filterThirdParty = true;
    public bool FilterThirdParty
    {
        get => _filterThirdParty;
        set
        {
            if (SetProperty(ref _filterThirdParty, value))
            {
                ApplyFilters();
            }
        }
    }

    private bool _filterHighImpact = false;
    public bool FilterHighImpact
    {
        get => _filterHighImpact;
        set
        {
            if (SetProperty(ref _filterHighImpact, value))
            {
                ApplyFilters();
            }
        }
    }

    private bool _canUndo;
    public bool CanUndo
    {
        get => _canUndo;
        set => SetProperty(ref _canUndo, value);
    }

    // Selection properties for details panel
    private StartupEntry? _selectedStartupItem;
    public StartupEntry? SelectedStartupItem
    {
        get => _selectedStartupItem;
        set => SetProperty(ref _selectedStartupItem, value);
    }

    private ServiceEntry? _selectedService;
    public ServiceEntry? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    private ScheduledTaskEntry? _selectedTask;
    public ScheduledTaskEntry? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public StartupViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        try
        {
            _startupEngine = App.Services?.GetService<StartupEngine>() ?? new StartupEngine();
            _iconCache = App.Services?.GetService<IconCacheService>() ?? new IconCacheService();
            _safety = App.Services?.GetService<ServiceSafetyService>() ?? new ServiceSafetyService();
            _audit = App.Services?.GetService<AuditLogService>() ?? new AuditLogService();
        }
        catch
        {
            _startupEngine = new StartupEngine();
            _iconCache = new IconCacheService();
            _safety = new ServiceSafetyService();
            _audit = new AuditLogService();
        }

        _ = LoadAllDataAsync();
    }

    public async Task LoadAllDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusText = "Scanning startup configuration...".T();

        try
        {
            // 1. Boot Performance Analytics
            LoadingStatus = "Analyzing last system boot performance...".T();
            double bootSec = await Task.Run(() => _startupEngine.GetLastBootTimeSeconds());

            // 2. Load Startup Apps
            LoadingStatus = "Reading registry and folder startup applications...".T();
            var apps = await Task.Run(() => _startupEngine.GetStartupEntries());
            _allStartupApps.Clear();
            _allStartupApps.AddRange(apps);

            // 3. Load Background Services
            LoadingStatus = "Scanning Windows background services...".T();
            var svcs = await Task.Run(() => _startupEngine.GetServices());
            _allServices.Clear();
            _allServices.AddRange(svcs);

            // 4. Load Scheduled Tasks
            LoadingStatus = "Reading active scheduled maintenance tasks...".T();
            var tasks = await Task.Run(() => _startupEngine.GetScheduledTasks());
            _allScheduledTasks.Clear();
            _allScheduledTasks.AddRange(tasks);

            // Set dashboard stats
            int activeApps = _allStartupApps.Count(x => x.IsEnabled);
            int activeSvcs = _allServices.Count(x => x.Status == "Running");
            int activeTasks = _allScheduledTasks.Count(x => x.IsEnabled);

            TotalStartupApps = activeApps;
            TotalRunningServices = activeSvcs;
            TotalScheduledTasks = activeTasks;

            if (bootSec > 0)
            {
                BootTimeFormatted = string.Format("{0:F1}s".T(), bootSec);
            }
            else
            {
                // Fallback logical boot estimate
                double estimated = 5.2 + (activeApps * 0.7) + (_allServices.Count(x => !x.IsMicrosoftService && x.Status == "Running") * 0.4);
                BootTimeFormatted = string.Format("{0:F1}s (Est.)".T(), estimated);
            }

            // Calculate Health / Optimization Score
            CalculateOptimizationScore();

            // Apply filter selection to observable collections
            ApplyFilters();

            StatusText = "Startup data loaded successfully.".T();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Scan failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingStatus = "";
        }
    }

    private void CalculateOptimizationScore()
    {
        double score = 100.0;
        var suggestions = new List<string>();

        // Deduct startup items
        int highApps = 0;
        int critApps = 0;
        int medApps = 0;

        foreach (var app in _allStartupApps.Where(x => x.IsEnabled))
        {
            if (app.StartupImpact == "Critical") { score -= 15.0; critApps++; }
            else if (app.StartupImpact == "High") { score -= 10.0; highApps++; }
            else if (app.StartupImpact == "Medium") { score -= 5.0; medApps++; }
        }

        // Deduct running third-party services
        int activeThirdPartySvcs = _allServices.Count(x => !x.IsMicrosoftService && x.Status == "Running");
        score -= (activeThirdPartySvcs * 2.0);

        // Deduct running third-party tasks
        int activeThirdPartyTasks = _allScheduledTasks.Count(x => !x.IsMicrosoftTask && x.IsEnabled);
        score -= (activeThirdPartyTasks * 3.0);

        score = Math.Max(35.0, score);
        OptimizationScore = Math.Round(score);

        // Build suggestions
        if (critApps > 0 || highApps > 0)
        {
            suggestions.Add(string.Format("Disable {0} high/critical impact startup applications to boost boot speeds by up to 30%.".T(), critApps + highApps));
        }
        if (activeThirdPartySvcs > 4)
        {
            suggestions.Add(string.Format("There are {0} running non-Microsoft background services. Consider disabling those you don't use regularly.".T(), activeThirdPartySvcs));
        }
        if (activeThirdPartyTasks > 2)
        {
            suggestions.Add(string.Format("Found {0} active third-party scheduled tasks running in the background. Disabling them saves system overhead.".T(), activeThirdPartyTasks));
        }

        if (suggestions.Count == 0)
        {
            OptimizationSuggestions = "Excellent! Your Windows startup and background services are fully optimized.".T();
        }
        else
        {
            OptimizationSuggestions = string.Join("\n• ", suggestions.Prepend("Optimization recommendations:".T()));
        }
    }

    private void ApplyFilters()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // 1. Filter Startup Apps
            StartupApps.Clear();
            var filteredApps = _allStartupApps.Where(app =>
            {
                if (!FilterMicrosoft && app.IsMicrosoft) return false;
                if (!FilterThirdParty && !app.IsMicrosoft) return false;
                if (FilterHighImpact && app.StartupImpact != "High" && app.StartupImpact != "Critical") return false;

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    bool matchName = app.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchPublisher = app.Publisher.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchPath = app.Path.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    return matchName || matchPublisher || matchPath;
                }
                return true;
            });
            foreach (var app in filteredApps) StartupApps.Add(app);

            // 2. Filter Services
            Services.Clear();
            var filteredSvcs = _allServices.Where(svc =>
            {
                if (!FilterMicrosoft && svc.IsMicrosoftService) return false;
                if (!FilterThirdParty && !svc.IsMicrosoftService) return false;
                
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    bool matchName = svc.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchDisplay = svc.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchComp = svc.CompanyName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    return matchName || matchDisplay || matchComp;
                }
                return true;
            });
            foreach (var svc in filteredSvcs) Services.Add(svc);

            // 3. Filter Scheduled Tasks
            ScheduledTasks.Clear();
            var filteredTasks = _allScheduledTasks.Where(task =>
            {
                if (!FilterMicrosoft && task.IsMicrosoftTask) return false;
                if (!FilterThirdParty && !task.IsMicrosoftTask) return false;

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    bool matchName = task.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchAuthor = task.Author.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchAction = task.Action.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
                    return matchName || matchAuthor || matchAction;
                }
                return true;
            });
            foreach (var task in filteredTasks) ScheduledTasks.Add(task);
        });
    }

    // Toggle Startup Entry
    public async Task ToggleStartupAppAsync(StartupEntry entry, bool enable)
    {
        IsLoading = true;
        StatusText = string.Format("Updating startup setting for {0}...".T(), entry.Name);

        // Push to undo stack
        _undoStack.Push(new UndoAction
        {
            Type = "Startup",
            Target = entry.Name,
            PreviousState = !enable,
            Description = $"Reverted startup application '{entry.Name}' to {(enable ? "Disabled" : "Enabled")}"
        });
        CanUndo = true;

        bool ok = await Task.Run(() => _startupEngine.ToggleStartupEntry(entry, enable));
        if (ok)
        {
            _audit.LogAction("Startup", "Toggle", entry.Name, "Success", $"Toggled to {(enable ? "Enabled" : "Disabled")}");
            StatusText = string.Format("{0} startup setting updated.".T(), entry.Name);
            
            entry.IsEnabled = enable;
            TotalStartupApps = _allStartupApps.Count(x => x.IsEnabled);
            CalculateOptimizationScore();
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Startup", "Toggle", entry.Name, "Failed");
            StatusText = string.Format("Failed to modify startup for {0}.".T(), entry.Name);
            _undoStack.Pop(); // remove failed action
            if (_undoStack.Count == 0) CanUndo = false;
        }

        IsLoading = false;
    }

    // Permanently Delete Startup Application Registry/Folder Shortcut
    public async Task RemoveStartupAppAsync(StartupEntry entry)
    {
        IsLoading = true;
        StatusText = string.Format("Deleting startup entry {0}...".T(), entry.Name);

        bool ok = await Task.Run(() => _startupEngine.RemoveStartupEntry(entry));
        if (ok)
        {
            _audit.LogAction("Startup", "Remove", entry.Name, "Success");
            StatusText = string.Format("Startup entry {0} permanently removed.".T(), entry.Name);
            
            _allStartupApps.Remove(entry);
            TotalStartupApps = _allStartupApps.Count(x => x.IsEnabled);
            CalculateOptimizationScore();
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Startup", "Remove", entry.Name, "Failed");
            StatusText = string.Format("Failed to delete startup entry {0}.".T(), entry.Name);
        }

        IsLoading = false;
    }

    // Service Controls (Start, Stop, Restart)
    public async Task ControlServiceAsync(ServiceEntry entry, string action)
    {
        string warn = _safety.GetSafetyWarning(entry.Name, action);
        if (!string.IsNullOrEmpty(warn))
        {
            StatusText = warn.T();
            return;
        }

        IsLoading = true;
        StatusText = string.Format("Sending {0} command to service {1}...".T(), action, entry.DisplayName);

        bool ok = await Task.Run(() => _startupEngine.ControlService(entry.Name, action));
        if (ok)
        {
            _audit.LogAction("Service", action, entry.Name, "Success");
            StatusText = string.Format("Service {0} command completed successfully.".T(), entry.DisplayName);
            
            entry.Status = action == "Start" ? "Running" : (action == "Stop" ? "Stopped" : "Running");
            TotalRunningServices = _allServices.Count(x => x.Status == "Running");
            CalculateOptimizationScore();
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Service", action, entry.Name, "Failed");
            StatusText = string.Format("Failed to complete action on service {0}.".T(), entry.DisplayName);
        }

        IsLoading = false;
    }

    // Modify Service Registry Startup Type
    public async Task ChangeServiceStartupTypeAsync(ServiceEntry entry, string startupType)
    {
        if (entry.StartupType == startupType) return;

        // Check safety
        if (_safety.IsCriticalService(entry.Name) && startupType == "Disabled")
        {
            StatusText = "Action Blocked: Disabling a system-critical service is restricted.".T();
            return;
        }

        IsLoading = true;
        StatusText = string.Format("Changing service {0} startup to {1}...".T(), entry.DisplayName, startupType);

        ServiceStartMode mode = startupType switch
        {
            "Automatic" => ServiceStartMode.Automatic,
            "Manual" => ServiceStartMode.Manual,
            "Disabled" => ServiceStartMode.Disabled,
            _ => ServiceStartMode.Manual
        };

        // Push to undo stack
        _undoStack.Push(new UndoAction
        {
            Type = "Service",
            Target = entry.Name,
            PreviousState = entry.StartupType,
            Description = $"Reverted service '{entry.DisplayName}' startup type to {entry.StartupType}"
        });
        CanUndo = true;

        bool ok = await Task.Run(() => _startupEngine.SetServiceStartupType(entry.Name, mode));
        if (ok)
        {
            _audit.LogAction("Service", "ChangeStartupType", entry.Name, "Success", $"Changed startup type to {startupType}");
            StatusText = string.Format("Service {0} startup type changed to {1}.".T(), entry.DisplayName, startupType);
            
            entry.StartupType = startupType;
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Service", "ChangeStartupType", entry.Name, "Failed");
            StatusText = string.Format("Failed to change service {0} startup type.".T(), entry.DisplayName);
            _undoStack.Pop();
            if (_undoStack.Count == 0) CanUndo = false;
        }

        IsLoading = false;
    }

    // Toggle Scheduled Task Active State
    public async Task ToggleScheduledTaskAsync(ScheduledTaskEntry entry, bool enable)
    {
        IsLoading = true;
        StatusText = string.Format("Toggling task {0} state...".T(), entry.Name);

        // Push to undo stack
        _undoStack.Push(new UndoAction
        {
            Type = "Task",
            Target = entry.Path,
            PreviousState = !enable,
            Description = $"Reverted task '{entry.Name}' active state to {(enable ? "Disabled" : "Enabled")}"
        });
        CanUndo = true;

        bool ok = await Task.Run(() => _startupEngine.ToggleScheduledTask(entry.Path, enable));
        if (ok)
        {
            _audit.LogAction("Task", "Toggle", entry.Name, "Success", $"Toggled to {(enable ? "Enabled" : "Disabled")}");
            StatusText = string.Format("Scheduled task {0} updated.".T(), entry.Name);
            
            entry.IsEnabled = enable;
            TotalScheduledTasks = _allScheduledTasks.Count(x => x.IsEnabled);
            CalculateOptimizationScore();
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Task", "Toggle", entry.Name, "Failed");
            StatusText = string.Format("Failed to toggle scheduled task {0}.".T(), entry.Name);
            _undoStack.Pop();
            if (_undoStack.Count == 0) CanUndo = false;
        }

        IsLoading = false;
    }

    // Permanently Delete Scheduled Task
    public async Task DeleteScheduledTaskAsync(ScheduledTaskEntry entry)
    {
        IsLoading = true;
        StatusText = string.Format("Deleting scheduled task {0}...".T(), entry.Name);

        bool ok = await Task.Run(() => _startupEngine.DeleteScheduledTask(entry.Path));
        if (ok)
        {
            _audit.LogAction("Task", "Delete", entry.Name, "Success", $"Deleted path {entry.Path}");
            StatusText = string.Format("Scheduled task {0} deleted successfully.".T(), entry.Name);
            
            _allScheduledTasks.Remove(entry);
            TotalScheduledTasks = _allScheduledTasks.Count(x => x.IsEnabled);
            CalculateOptimizationScore();
            ApplyFilters();
        }
        else
        {
            _audit.LogAction("Task", "Delete", entry.Name, "Failed");
            StatusText = string.Format("Failed to delete scheduled task {0}.".T(), entry.Name);
        }

        IsLoading = false;
    }

    // One-Click Smart Optimization (Disables High/Critical third party items)
    public async Task OptimizeStartupAppsAsync()
    {
        var targetApps = _allStartupApps.Where(x => x.IsEnabled && !x.IsMicrosoft && (x.StartupImpact == "High" || x.StartupImpact == "Critical")).ToList();
        if (targetApps.Count == 0)
        {
            StatusText = "Startup is already optimized! No high-impact third party apps found.".T();
            return;
        }

        IsLoading = true;
        int count = 0;
        foreach (var app in targetApps)
        {
            StatusText = string.Format("Optimizing startup: Disabling {0}...".T(), app.Name);
            bool ok = await Task.Run(() => _startupEngine.ToggleStartupEntry(app, false));
            if (ok)
            {
                _audit.LogAction("Startup", "OptimizeDisable", app.Name, "Success");
                app.IsEnabled = false;
                count++;
            }
        }

        StatusText = string.Format("Successfully disabled {0} startup impact programs.".T(), count);
        TotalStartupApps = _allStartupApps.Count(x => x.IsEnabled);
        CalculateOptimizationScore();
        ApplyFilters();
        IsLoading = false;
    }

    // Revert Last User Configuration Action
    public async Task UndoLastChangeAsync()
    {
        if (_undoStack.Count == 0) return;

        var action = _undoStack.Pop();
        if (_undoStack.Count == 0) CanUndo = false;

        IsLoading = true;
        StatusText = string.Format("Undoing action: Reverting {0}...".T(), action.Target);

        try
        {
            bool ok = false;
            if (action.Type == "Startup")
            {
                var entry = _allStartupApps.FirstOrDefault(x => x.Name == action.Target);
                if (entry != null)
                {
                    bool prevValue = (bool)action.PreviousState;
                    ok = await Task.Run(() => _startupEngine.ToggleStartupEntry(entry, prevValue));
                    if (ok)
                    {
                        entry.IsEnabled = prevValue;
                        TotalStartupApps = _allStartupApps.Count(x => x.IsEnabled);
                        CalculateOptimizationScore();
                        ApplyFilters();
                    }
                }
            }
            else if (action.Type == "Service")
            {
                string prevType = (string)action.PreviousState;
                ServiceStartMode mode = prevType switch
                {
                    "Automatic" => ServiceStartMode.Automatic,
                    "Manual" => ServiceStartMode.Manual,
                    "Disabled" => ServiceStartMode.Disabled,
                    _ => ServiceStartMode.Manual
                };
                ok = await Task.Run(() => _startupEngine.SetServiceStartupType(action.Target, mode));
                if (ok)
                {
                    var entry = _allServices.FirstOrDefault(x => x.Name == action.Target);
                    if (entry != null)
                    {
                        entry.StartupType = prevType;
                        ApplyFilters();
                    }
                }
            }
            else if (action.Type == "Task")
            {
                bool prevValue = (bool)action.PreviousState;
                ok = await Task.Run(() => _startupEngine.ToggleScheduledTask(action.Target, prevValue));
                if (ok)
                {
                    var entry = _allScheduledTasks.FirstOrDefault(x => x.Path == action.Target);
                    if (entry != null)
                    {
                        entry.IsEnabled = prevValue;
                        TotalScheduledTasks = _allScheduledTasks.Count(x => x.IsEnabled);
                        CalculateOptimizationScore();
                        ApplyFilters();
                    }
                }
            }

            if (ok)
            {
                _audit.LogAction("Undo", "Revert", action.Target, "Success", action.Description);
                StatusText = action.Description.T();
            }
            else
            {
                _audit.LogAction("Undo", "Revert", action.Target, "Failed");
                StatusText = string.Format("Failed to undo configuration change for {0}.".T(), action.Target);
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Undo action error: {0}".T(), ex.Message);
        }

        IsLoading = false;
    }
}

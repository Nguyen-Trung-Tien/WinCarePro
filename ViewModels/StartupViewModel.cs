using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class StartupViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly StartupEngine _engine = new();

    private bool _isLoading;
    private string _statusText = "Ready";

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<StartupEntry> StartupApps { get; } = new();
    public ObservableCollection<ServiceEntry> Services { get; } = new();
    public ObservableCollection<ScheduledTaskEntry> ScheduledTasks { get; } = new();

    public StartupViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = LoadAllDataAsync();
    }

    public async Task LoadAllDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusText = "Loading startup configurations...";

        StartupApps.Clear();
        Services.Clear();
        ScheduledTasks.Clear();

        try
        {
            // Load Startup Apps
            var apps = await Task.Run(() => _engine.GetStartupEntries());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var app in apps) StartupApps.Add(app);
            });

            // Load Services
            var svcs = await Task.Run(() => _engine.GetServices());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var svc in svcs) Services.Add(svc);
            });

            // Load Scheduled Tasks
            var tasks = await Task.Run(() => _engine.GetScheduledTasks());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var task in tasks) ScheduledTasks.Add(task);
            });

            StatusText = "Data loaded successfully.";
        }
        catch (Exception ex)
        {
            StatusText = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleStartupAppAsync(StartupEntry entry, bool enable)
    {
        IsLoading = true;
        StatusText = $"Toggling {entry.Name}...";

        bool ok = await Task.Run(() => _engine.ToggleStartupEntry(entry, enable));
        if (ok)
        {
            StatusText = $"{entry.Name} toggled to {(enable ? "Enabled" : "Disabled")}.";
        }
        else
        {
            StatusText = $"Failed to toggle startup app {entry.Name}.";
        }
        await LoadAllDataAsync();
    }

    public async Task RemoveStartupAppAsync(StartupEntry entry)
    {
        IsLoading = true;
        StatusText = $"Removing {entry.Name}...";

        bool ok = await Task.Run(() => _engine.RemoveStartupEntry(entry));
        if (ok)
        {
            StatusText = $"{entry.Name} removed from startup.";
        }
        else
        {
            StatusText = $"Failed to remove startup app {entry.Name}.";
        }
        await LoadAllDataAsync();
    }

    public async Task ToggleServiceAsync(ServiceEntry entry, string mode)
    {
        IsLoading = true;
        StatusText = $"Configuring service {entry.Name}...";

        var startMode = mode.ToLower() switch
        {
            "auto" => ServiceStartMode.Automatic,
            "manual" => ServiceStartMode.Manual,
            "disabled" => ServiceStartMode.Disabled,
            _ => ServiceStartMode.Manual
        };

        bool ok = await Task.Run(() => _engine.SetServiceStartupType(entry.Name, startMode));
        if (ok)
        {
            StatusText = $"Service {entry.Name} startup set to {startMode}.";
        }
        else
        {
            StatusText = $"Failed to set startup type for {entry.Name}.";
        }
        await LoadAllDataAsync();
    }

    public async Task ControlServiceAsync(ServiceEntry entry, string action)
    {
        IsLoading = true;
        StatusText = $"{action}ing service {entry.Name}...";

        bool ok = await Task.Run(() => _engine.ControlService(entry.Name, action));
        if (ok)
        {
            StatusText = $"Service {entry.Name} {action}ed successfully.";
        }
        else
        {
            StatusText = $"Failed to {action} service {entry.Name}.";
        }
        await LoadAllDataAsync();
    }

    public async Task ToggleTaskAsync(ScheduledTaskEntry entry, bool enable)
    {
        IsLoading = true;
        StatusText = $"Toggling task {entry.Name}...";

        bool ok = await Task.Run(() => _engine.ToggleScheduledTask(entry.Path, enable));
        if (ok)
        {
            StatusText = $"Task {entry.Name} toggled to {(enable ? "Enabled" : "Disabled")}.";
        }
        else
        {
            StatusText = $"Failed to toggle task {entry.Name}.";
        }
        await LoadAllDataAsync();
    }

    public async Task DeleteTaskAsync(ScheduledTaskEntry entry)
    {
        IsLoading = true;
        StatusText = $"Deleting task {entry.Name}...";

        bool ok = await Task.Run(() => _engine.DeleteScheduledTask(entry.Path));
        if (ok)
        {
            StatusText = $"Task {entry.Name} deleted.";
        }
        else
        {
            StatusText = $"Failed to delete task {entry.Name}.";
        }
        await LoadAllDataAsync();
    }
}

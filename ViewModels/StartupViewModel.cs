using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class StartupViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly StartupEngine _startupEngine = new();

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

    public ObservableCollection<StartupEntry> StartupApps { get; } = new();
    public ObservableCollection<ServiceEntry> Services { get; } = new();

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

        try
        {
            var apps = await Task.Run(() => _startupEngine.GetStartupEntries());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var app in apps) StartupApps.Add(app);
            });

            var svcs = await Task.Run(() => _startupEngine.GetServices());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var svc in svcs) Services.Add(svc);
            });

            StatusText = "Startup data loaded successfully.";
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

        bool ok = await Task.Run(() => _startupEngine.ToggleStartupEntry(entry, enable));
        if (ok)
        {
            StatusText = $"{entry.Name} toggled to {(enable ? "Enabled" : "Disabled")}.";
        }
        else
        {
            StatusText = $"Failed to toggle startup app {entry.Name}.";
        }
        IsLoading = false;
        await LoadAllDataAsync();
    }

    public async Task ControlServiceAsync(ServiceEntry entry, string action)
    {
        IsLoading = true;
        StatusText = $"Executing {action} on service {entry.Name}...";
        bool ok = await Task.Run(() => _startupEngine.ControlService(entry.Name, action));
        if (ok)
        {
            StatusText = $"Successfully sent {action} command to {entry.Name}.";
        }
        else
        {
            StatusText = $"Failed to {action} service {entry.Name}.";
        }
        IsLoading = false;
        await LoadAllDataAsync();
    }
}

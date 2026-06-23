using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

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

    private string _statusText = "Ready".T();
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
        StatusText = "Loading startup configurations...".T();

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

            StatusText = "Startup data loaded successfully.".T();
        }
        catch (Exception ex)
        {
            StatusText = string.Format("Load failed: {0}".T(), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleStartupAppAsync(StartupEntry entry, bool enable)
    {
        IsLoading = true;
        StatusText = string.Format("Toggling {0}...".T(), entry.Name);

        bool ok = await Task.Run(() => _startupEngine.ToggleStartupEntry(entry, enable));
        if (ok)
        {
            StatusText = string.Format("{0} toggled to {1}.".T(), entry.Name, (enable ? "Enabled" : "Disabled").T());
        }
        else
        {
            StatusText = string.Format("Failed to toggle startup app {0}.".T(), entry.Name);
        }
        IsLoading = false;
        await LoadAllDataAsync();
    }

    public async Task ControlServiceAsync(ServiceEntry entry, string action)
    {
        IsLoading = true;
        StatusText = string.Format("Executing {0} on service {1}...".T(), action, entry.Name);
        bool ok = await Task.Run(() => _startupEngine.ControlService(entry.Name, action));
        if (ok)
        {
            StatusText = string.Format("Successfully sent {0} command to {1}.".T(), action, entry.Name);
        }
        else
        {
            StatusText = string.Format("Failed to {0} service {1}.".T(), action, entry.Name);
        }
        IsLoading = false;
        await LoadAllDataAsync();
    }
}

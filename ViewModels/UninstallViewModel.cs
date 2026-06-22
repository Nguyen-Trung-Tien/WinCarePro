using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class UninstallViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UninstallEngine _uninstallEngine = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _progressMessage = "Ready";
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    private List<InstalledAppInfo> _allApps = new();
    public ObservableCollection<InstalledAppInfo> FilteredApps { get; } = new();
    public ObservableCollection<LeftoverItem> Leftovers { get; } = new();

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyAppFilter();
            }
        }
    }

    private int _uninstallStep = 0;
    public int UninstallStep
    {
        get => _uninstallStep;
        set => SetProperty(ref _uninstallStep, value);
    }

    private string _leftoversSizeFormatted = "0 B";
    public string LeftoversSizeFormatted
    {
        get => _leftoversSizeFormatted;
        set => SetProperty(ref _leftoversSizeFormatted, value);
    }

    public UninstallViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _uninstallEngine.OutputReceived += msg => _dispatcherQueue.TryEnqueue(() => ProgressMessage = msg);
        _uninstallEngine.ProgressChanged += pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = pct);

        _ = ScanAppsAsync();
    }

    public async Task ScanAppsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 10;
        ProgressMessage = "Scanning registry for installed applications...";

        FilteredApps.Clear();
        _allApps.Clear();

        try
        {
            var apps = await Task.Run(() => _uninstallEngine.ScanInstalledApps());
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                _allApps = apps;
                ApplyAppFilter();
                ProgressPercent = 100;
                ProgressMessage = $"Loaded {_allApps.Count} applications.";
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Scan failed: {ex.Message}";
                IsBusy = false;
            });
        }
    }

    private void ApplyAppFilter()
    {
        FilteredApps.Clear();
        var query = SearchText.Trim();
        var list = _allApps.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            list = list.Where(x => x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var app in list)
        {
            FilteredApps.Add(app);
        }
    }

    public async Task UninstallAppAsync(InstalledAppInfo app)
    {
        UninstallStep = 1;
        IsBusy = true;

        try
        {
            ProgressPercent = 10;
            ProgressMessage = $"Uninstalling {app.DisplayName}...";

            bool uninstalled = await _uninstallEngine.RunStandardUninstallerAsync(app);

            ProgressPercent = 60;
            ProgressMessage = $"Scanning leftovers for {app.DisplayName}...";

            var leftoverList = await Task.Run(() => _uninstallEngine.ScanLeftovers(app));

            IsBusy = false;

            _dispatcherQueue.TryEnqueue(() =>
            {
                Leftovers.Clear();
                foreach (var item in leftoverList)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(LeftoverItem.IsSelected))
                        {
                            UpdateLeftoversSize();
                        }
                    };
                    Leftovers.Add(item);
                }

                UpdateLeftoversSize();
                ProgressPercent = 100;

                if (Leftovers.Count > 0)
                {
                    UninstallStep = 2;
                    ProgressMessage = $"Scanned {Leftovers.Count} leftover items.";
                }
                else
                {
                    UninstallStep = 0;
                    ProgressMessage = $"Successfully uninstalled. No leftovers found.";
                    _ = ScanAppsAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Uninstallation failed: {ex.Message}";
                IsBusy = false;
                UninstallStep = 0;
            });
        }
    }

    public async Task DeleteLeftoversAsync()
    {
        if (Leftovers.Count == 0) return;
        IsBusy = true;
        ProgressMessage = "Deleting leftover components...";

        try
        {
            var selectedItems = Leftovers.Where(x => x.IsSelected).ToList();
            int deleted = await _uninstallEngine.DeleteLeftoversAsync(selectedItems);
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressPercent = 100;
                ProgressMessage = $"Cleaned leftover files and registry entries.";
                IsBusy = false;
                UninstallStep = 0;
                _ = ScanAppsAsync();
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Error deleting leftovers: {ex.Message}";
                IsBusy = false;
            });
        }
    }

    public void CancelLeftovers()
    {
        UninstallStep = 0;
        ProgressMessage = "Leftover deletion cancelled.";
        _ = ScanAppsAsync();
    }

    private void UpdateLeftoversSize()
    {
        long bytes = Leftovers.Where(x => x.IsSelected && x.Type != LeftoverType.RegistryKey && x.Type != LeftoverType.RegistryValue).Sum(x => x.SizeBytes);
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        LeftoversSizeFormatted = $"{doubleBytes:F1} {suffix[i]}";
    }
}

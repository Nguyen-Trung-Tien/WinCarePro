using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCarePro.ViewModels;

public partial class UninstallViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UninstallEngine _engine = new();
    private List<InstalledAppInfo> _allApps = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isUninstalling;

    [ObservableProperty]
    private bool _isScanningLeftovers;

    [ObservableProperty]
    private bool _isCleaningLeftovers;

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string _progressMessage = "Ready";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private int _currentStep = 0; // 0 = List of apps, 1 = Uninstalling/Scanning, 2 = Leftovers review

    [ObservableProperty]
    private InstalledAppInfo? _selectedApp;

    [ObservableProperty]
    private string _leftoversSizeFormatted = "0 B";

    [ObservableProperty]
    private string _uninstallSelectedText = "Uninstall Selected";

    [ObservableProperty]
    private bool _canUninstallSelected = false;

    public ObservableCollection<InstalledAppInfo> FilteredApps { get; } = new();
    public ObservableCollection<InstalledAppInfo> FilteredThirdPartyApps { get; } = new();
    public ObservableCollection<InstalledAppInfo> FilteredSystemApps { get; } = new();
    public ObservableCollection<LeftoverItem> Leftovers { get; } = new();

    public UninstallViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += msg => _dispatcherQueue.TryEnqueue(() => ProgressMessage = msg);
        _engine.ProgressChanged += pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = pct);
        
        // Scan apps on initialization
        _ = ScanAppsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    public async Task ScanAppsAsync()
    {
        if (IsScanning || IsUninstalling || IsCleaningLeftovers) return;

        IsScanning = true;
        ProgressPercent = 20;
        ProgressMessage = "Scanning registry for installed applications...";
        
        foreach (var app in _allApps)
        {
            app.PropertyChanged -= OnAppPropertyChanged;
        }
        
        FilteredApps.Clear();
        FilteredThirdPartyApps.Clear();
        FilteredSystemApps.Clear();
        _allApps.Clear();
        UpdateSelectionProperties();

        try
        {
            var apps = await Task.Run(() => _engine.ScanInstalledApps());
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                _allApps = apps;
                foreach (var app in _allApps)
                {
                    app.PropertyChanged -= OnAppPropertyChanged;
                    app.PropertyChanged += OnAppPropertyChanged;
                }
                ApplyFilter();
                ProgressPercent = 100;
                ProgressMessage = $"Loaded {_allApps.Count} applications.";
                IsScanning = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Scan failed: {ex.Message}";
                IsScanning = false;
            });
        }
    }

    private void ApplyFilter()
    {
        FilteredApps.Clear();
        FilteredThirdPartyApps.Clear();
        FilteredSystemApps.Clear();
        var query = SearchText.Trim();
        var list = _allApps.AsEnumerable();
        
        if (!string.IsNullOrEmpty(query))
        {
            list = list.Where(x => 
                x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)
            );
        }

        foreach (var app in list)
        {
            FilteredApps.Add(app);
            if (app.IsStoreApp)
            {
                FilteredSystemApps.Add(app);
            }
            else
            {
                FilteredThirdPartyApps.Add(app);
            }
        }

        UpdateSelectionProperties();
    }

    private void OnAppPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstalledAppInfo.IsSelected))
        {
            _dispatcherQueue.TryEnqueue(UpdateSelectionProperties);
        }
    }

    private void UpdateSelectionProperties()
    {
        int count = _allApps.Count(x => x.IsSelected);
        UninstallSelectedText = count > 0 ? $"Uninstall Selected ({count})" : "Uninstall Selected";
        CanUninstallSelected = count > 0 && !IsScanning && !IsUninstalling && !IsCleaningLeftovers;
    }

    public void SelectAllApps(bool select, bool system)
    {
        var apps = _allApps.Where(x => x.IsStoreApp == system);
        var query = SearchText.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            apps = apps.Where(x => 
                x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase)
            );
        }

        foreach (var app in apps)
        {
            app.IsSelected = select;
        }
        UpdateSelectionProperties();
    }

    public async Task UninstallAppAsync(InstalledAppInfo app)
    {
        if (IsScanning || IsUninstalling || IsCleaningLeftovers) return;

        foreach (var a in _allApps)
        {
            a.IsSelected = (a == app);
        }
        await UninstallSelectedAppsAsync();
    }

    public async Task UninstallSelectedAppsAsync()
    {
        if (IsScanning || IsUninstalling || IsCleaningLeftovers) return;

        var appsToUninstall = _allApps.Where(x => x.IsSelected).ToList();
        if (appsToUninstall.Count == 0) return;

        CurrentStep = 1;
        IsUninstalling = true;
        UpdateSelectionProperties();

        try
        {
            var allLeftovers = new List<LeftoverItem>();
            int totalApps = appsToUninstall.Count;

            for (int i = 0; i < totalApps; i++)
            {
                var app = appsToUninstall[i];
                SelectedApp = app;

                int appStepBase = (int)((double)i / totalApps * 100);
                ProgressPercent = appStepBase + 5;
                ProgressMessage = $"[{i + 1}/{totalApps}] Uninstalling {app.DisplayName}...";

                // Step 1: Run standard uninstaller
                bool uninstalled = await _engine.RunStandardUninstallerAsync(app);
                if (!uninstalled)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        ProgressMessage = $"Standard uninstaller for {app.DisplayName} could not be launched or failed.";
                    });
                }

                // Step 2: Scan for leftovers
                ProgressPercent = appStepBase + (int)(50.0 / totalApps);
                ProgressMessage = $"[{i + 1}/{totalApps}] Scanning leftovers for {app.DisplayName}...";

                var leftoverList = await Task.Run(() => _engine.ScanLeftovers(app));
                allLeftovers.AddRange(leftoverList);
            }

            IsUninstalling = false;
            IsScanningLeftovers = false;

            _dispatcherQueue.TryEnqueue(() =>
            {
                Leftovers.Clear();
                foreach (var item in allLeftovers)
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
                    CurrentStep = 2; // Move to Leftovers view
                    ProgressMessage = $"Scanned {Leftovers.Count} leftover items from {totalApps} uninstalled applications.";
                }
                else
                {
                    CurrentStep = 0; // Go back to app list
                    ProgressMessage = $"Successfully uninstalled {totalApps} applications. No leftovers found.";
                    _ = ScanAppsAsync(); // Refresh list
                }
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Batch uninstallation process error: {ex.Message}";
                IsUninstalling = false;
                IsScanningLeftovers = false;
                CurrentStep = 0;
            });
        }
    }

    public async Task DeleteLeftoversAsync()
    {
        if (IsCleaningLeftovers || Leftovers.Count == 0) return;

        IsCleaningLeftovers = true;
        ProgressPercent = 20;
        ProgressMessage = "Deleting leftover components...";

        try
        {
            var selectedItems = Leftovers.Where(x => x.IsSelected).ToList();
            int deleted = await _engine.DeleteLeftoversAsync(selectedItems);
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressPercent = 100;
                ProgressMessage = $"Successfully cleaned up residual files and registry entries.";
                IsCleaningLeftovers = false;
                CurrentStep = 0;
                _ = ScanAppsAsync(); // Refresh installed apps list
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Error deleting leftovers: {ex.Message}";
                IsCleaningLeftovers = false;
            });
        }
    }

    public void CancelLeftovers()
    {
        CurrentStep = 0;
        ProgressMessage = "Leftover deletion cancelled.";
        _ = ScanAppsAsync();
    }

    public void SelectAllLeftovers(bool select)
    {
        foreach (var item in Leftovers)
        {
            item.IsSelected = select;
        }
        UpdateLeftoversSize();
    }

    public void UpdateLeftoversSize()
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

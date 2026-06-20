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

    public ObservableCollection<InstalledAppInfo> FilteredApps { get; } = new();
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
        FilteredApps.Clear();
        _allApps.Clear();

        try
        {
            var apps = await Task.Run(() => _engine.ScanInstalledApps());
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                _allApps = apps;
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
        }
    }

    public async Task UninstallAppAsync(InstalledAppInfo app)
    {
        if (IsScanning || IsUninstalling || IsCleaningLeftovers) return;

        SelectedApp = app;
        CurrentStep = 1;
        IsUninstalling = true;
        ProgressPercent = 10;
        ProgressMessage = $"Preparing to uninstall {app.DisplayName}...";

        try
        {
            // Step 1: Run standard uninstaller
            ProgressPercent = 30;
            bool uninstalled = await _engine.RunStandardUninstallerAsync(app);
            
            if (!uninstalled)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ProgressMessage = $"Standard uninstaller for {app.DisplayName} could not be launched or failed.";
                });
            }

            // Step 2: Scan for leftovers
            IsUninstalling = false;
            IsScanningLeftovers = true;
            ProgressPercent = 60;
            ProgressMessage = "Scanning for residual files, directories, and registry keys...";

            var leftoverList = await Task.Run(() => _engine.ScanLeftovers(app));
            ProgressPercent = 90;

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
                IsScanningLeftovers = false;
                ProgressPercent = 100;
                
                if (Leftovers.Count > 0)
                {
                    CurrentStep = 2; // Move to Leftovers view
                    ProgressMessage = $"Scanned {Leftovers.Count} leftover items.";
                }
                else
                {
                    CurrentStep = 0; // Go back to app list
                    ProgressMessage = "Application successfully uninstalled. No leftovers found.";
                    _ = ScanAppsAsync(); // Refresh list
                }
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Uninstallation process error: {ex.Message}";
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

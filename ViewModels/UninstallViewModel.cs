using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

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

    private string _progressMessage = "Ready".T();
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

    // New Sorting, Filtering, Selection and Statistics properties
    private int _selectedSortIndex = 0;
    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                ApplyAppFilter();
            }
        }
    }

    private int _selectedFilterIndex = 0;
    public int SelectedFilterIndex
    {
        get => _selectedFilterIndex;
        set
        {
            if (SetProperty(ref _selectedFilterIndex, value))
            {
                ApplyAppFilter();
            }
        }
    }

    private InstalledAppInfo? _selectedApp;
    public InstalledAppInfo? SelectedApp
    {
        get => _selectedApp;
        set
        {
            if (SetProperty(ref _selectedApp, value))
            {
                IsSelectedAppNotNull = value != null;
            }
        }
    }

    private bool _isSelectedAppNotNull;
    public bool IsSelectedAppNotNull
    {
        get => _isSelectedAppNotNull;
        set => SetProperty(ref _isSelectedAppNotNull, value);
    }

    private int _totalAppsCount;
    public int TotalAppsCount
    {
        get => _totalAppsCount;
        set => SetProperty(ref _totalAppsCount, value);
    }

    private string _totalAppsSizeFormatted = "0 B";
    public string TotalAppsSizeFormatted
    {
        get => _totalAppsSizeFormatted;
        set => SetProperty(ref _totalAppsSizeFormatted, value);
    }

    private int _desktopAppsCount;
    public int DesktopAppsCount
    {
        get => _desktopAppsCount;
        set => SetProperty(ref _desktopAppsCount, value);
    }

    private int _storeAppsCount;
    public int StoreAppsCount
    {
        get => _storeAppsCount;
        set => SetProperty(ref _storeAppsCount, value);
    }

    private bool _hasSelectedApps;
    public bool HasSelectedApps
    {
        get => _hasSelectedApps;
        set => SetProperty(ref _hasSelectedApps, value);
    }

    public UninstallViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _uninstallEngine.OutputReceived += msg => _dispatcherQueue.TryEnqueue(() => ProgressMessage = msg.T());
        _uninstallEngine.ProgressChanged += pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = pct);

        _ = ScanAppsAsync();
    }

    public async Task ScanAppsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ProgressPercent = 10;
        ProgressMessage = "Scanning registry for installed applications...".T();

        FilteredApps.Clear();
        _allApps.Clear();

        try
        {
            var apps = await Task.Run(() => _uninstallEngine.ScanInstalledApps());
            ProgressPercent = 80;

            _dispatcherQueue.TryEnqueue(() =>
            {
                _allApps = apps;
                UpdateStatistics();
                ApplyAppFilter();
                ProgressPercent = 100;
                ProgressMessage = string.Format("Loaded {0} applications.".T(), _allApps.Count);
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Scan failed:".T() + " " + ex.Message;
                IsBusy = false;
            });
        }
    }

    private void UpdateStatistics()
    {
        TotalAppsCount = _allApps.Count;
        DesktopAppsCount = _allApps.Count(x => !x.IsStoreApp);
        StoreAppsCount = _allApps.Count(x => x.IsStoreApp);
        
        long totalBytes = _allApps.Sum(x => x.SizeBytes);
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = totalBytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        TotalAppsSizeFormatted = $"{doubleBytes:F1} {suffix[i]}";
    }

    private void ApplyAppFilter()
    {
        FilteredApps.Clear();
        var query = SearchText.Trim();
        var list = _allApps.AsEnumerable();

        // 1. Search filter
        if (!string.IsNullOrEmpty(query))
        {
            list = list.Where(x => x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                   x.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Type filter
        // 0 = All, 1 = Desktop, 2 = Store
        if (SelectedFilterIndex == 1)
        {
            list = list.Where(x => !x.IsStoreApp);
        }
        else if (SelectedFilterIndex == 2)
        {
            list = list.Where(x => x.IsStoreApp);
        }

        // 3. Sorting
        // 0 = Name A-Z, 1 = Name Z-A, 2 = Size Max-Min, 3 = Size Min-Max, 4 = Install Date Newest, 5 = Install Date Oldest
        list = SelectedSortIndex switch
        {
            1 => list.OrderByDescending(x => x.DisplayName),
            2 => list.OrderByDescending(x => x.SizeBytes),
            3 => list.OrderBy(x => x.SizeBytes),
            4 => list.OrderByDescending(x => string.IsNullOrEmpty(x.InstallDate) ? "0000-00-00" : x.InstallDate),
            5 => list.OrderBy(x => string.IsNullOrEmpty(x.InstallDate) ? "9999-99-99" : x.InstallDate),
            _ => list.OrderBy(x => x.DisplayName)
        };

        foreach (var app in list)
        {
            app.PropertyChanged -= OnAppPropertyChanged;
            app.PropertyChanged += OnAppPropertyChanged;
            FilteredApps.Add(app);
        }

        HasSelectedApps = _allApps.Any(x => x.IsSelected);

        if (FilteredApps.Count > 0)
        {
            if (SelectedApp == null || !FilteredApps.Contains(SelectedApp))
            {
                SelectedApp = FilteredApps[0];
            }
        }
        else
        {
            SelectedApp = null;
        }
    }

    private void OnAppPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstalledAppInfo.IsSelected))
        {
            HasSelectedApps = _allApps.Any(x => x.IsSelected);
        }
    }

    public async Task UninstallAppAsync(InstalledAppInfo app)
    {
        UninstallStep = 1;
        IsBusy = true;

        try
        {
            ProgressPercent = 10;
            ProgressMessage = string.Format("Uninstalling {0}...".T(), app.DisplayName);

            bool uninstalled = await _uninstallEngine.RunStandardUninstallerAsync(app);

            ProgressPercent = 60;
            ProgressMessage = string.Format("Scanning leftovers for {0}...".T(), app.DisplayName);

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
                    ProgressMessage = string.Format("Scanned {0} leftover items.".T(), Leftovers.Count);
                }
                else
                {
                    UninstallStep = 0;
                    ProgressMessage = "Successfully uninstalled. No leftovers found.".T();
                    _ = ScanAppsAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Uninstallation failed:".T() + " " + ex.Message;
                IsBusy = false;
                UninstallStep = 0;
            });
        }
    }

    // New Queue/Batch Uninstall (supporting both Standard and Force Mode)
    public async Task UninstallSelectedAppsAsync(bool forceUninstall = false)
    {
        var selected = _allApps.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0) return;

        UninstallStep = 1;
        IsBusy = true;
        
        var allLeftovers = new List<LeftoverItem>();
        int count = 0;

        try
        {
            foreach (var app in selected)
            {
                count++;
                ProgressPercent = (int)((double)(count - 1) / selected.Count * 100);

                if (forceUninstall)
                {
                    ProgressMessage = string.Format("Force uninstalling {0} ({1}/{2})...".T(), app.DisplayName, count, selected.Count);
                    await Task.Delay(500); // UI delay
                }
                else
                {
                    ProgressMessage = string.Format("Uninstalling {0} ({1}/{2})...".T(), app.DisplayName, count, selected.Count);
                    await _uninstallEngine.RunStandardUninstallerAsync(app);
                }

                ProgressMessage = string.Format("Scanning leftovers for {0}...".T(), app.DisplayName);
                var leftoverList = await Task.Run(() => _uninstallEngine.ScanLeftovers(app));
                allLeftovers.AddRange(leftoverList);
            }

            IsBusy = false;

            _dispatcherQueue.TryEnqueue(() =>
            {
                Leftovers.Clear();
                // Filter unique path leftovers to prevent duplicate deletions
                var uniqueLeftovers = allLeftovers.GroupBy(x => x.Path.ToLower()).Select(g => g.First()).ToList();

                foreach (var item in uniqueLeftovers)
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
                    ProgressMessage = string.Format("Scanned {0} leftover items.".T(), Leftovers.Count);
                }
                else
                {
                    UninstallStep = 0;
                    ProgressMessage = "Batch uninstall completed successfully. No leftovers found.".T();
                    _ = ScanAppsAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Batch uninstallation encountered an error:".T() + " " + ex.Message;
                IsBusy = false;
                UninstallStep = 0;
            });
        }
    }

    public async Task DeleteLeftoversAsync()
    {
        if (Leftovers.Count == 0) return;
        IsBusy = true;
        ProgressMessage = "Deleting leftover components...".T();

        try
        {
            var selectedItems = Leftovers.Where(x => x.IsSelected).ToList();
            int deleted = await _uninstallEngine.DeleteLeftoversAsync(selectedItems);
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressPercent = 100;
                ProgressMessage = string.Format("Cleaned {0} leftover files and registry entries.".T(), deleted);
                IsBusy = false;
                UninstallStep = 0;
                _ = ScanAppsAsync();
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Error deleting leftovers:".T() + " " + ex.Message;
                IsBusy = false;
            });
        }
    }

    public void CancelLeftovers()
    {
        UninstallStep = 0;
        ProgressMessage = "Leftover deletion cancelled.".T();
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

    // UI Action Link Helpers
    public void OpenSelectedAppFolder()
    {
        if (SelectedApp == null) return;
        string path = SelectedApp.InstallLocation;
        if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path)) return;
        
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ProgressMessage = "Error opening folder:".T() + " " + ex.Message;
        }
    }

    public void OpenSelectedAppRegistry()
    {
        if (SelectedApp == null) return;
        string hive = SelectedApp.Hive;
        string path = SelectedApp.RegistryPath;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string fullPath = System.IO.Path.Combine(hive, path);
            fullPath = fullPath.Replace("HKLM", "HKEY_LOCAL_MACHINE")
                               .Replace("HKCU", "HKEY_CURRENT_USER");
                               
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", true);
            if (key != null)
            {
                key.SetValue("LastKey", fullPath);
            }
            
            System.Diagnostics.Process.Start("regedit.exe");
        }
        catch (Exception ex)
        {
            ProgressMessage = "Error opening registry:".T() + " " + ex.Message;
        }
    }

    public void SearchSelectedAppOnline()
    {
        if (SelectedApp == null) return;
        try
        {
            string query = Uri.EscapeDataString($"{SelectedApp.DisplayName} {SelectedApp.Publisher}");
            string url = $"https://www.google.com/search?q={query}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ProgressMessage = "Error searching online:".T() + " " + ex.Message;
        }
    }
}

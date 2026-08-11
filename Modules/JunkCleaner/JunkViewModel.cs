using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Core.Helpers;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class JunkViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IJunkCleanerService _junkEngine;
    private readonly ILockingAppService _lockingAppService;
    private readonly IDialogService _dialogService;
    private System.Threading.CancellationTokenSource? _scanCts;

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set => SetPropertyOnUI(() => _isScanning, v => _isScanning = v, value);
    }

    private bool _isCleaning;
    public bool IsCleaning
    {
        get => _isCleaning;
        set => SetPropertyOnUI(() => _isCleaning, v => _isCleaning = v, value);
    }

    private string _progressMessage = "Ready to scan junk files".T();
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetPropertyOnUI(() => _progressMessage, v => _progressMessage = v, value);
    }

    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetPropertyOnUI(() => _progressPercent, v => _progressPercent = v, value);
    }

    private string _totalJunkSize = "0.0 B";
    public string TotalJunkSize
    {
        get => _totalJunkSize;
        set => SetPropertyOnUI(() => _totalJunkSize, v => _totalJunkSize = v, value);
    }

    private string _totalLockedSize = "0.0 B";
    public string TotalLockedSize
    {
        get => _totalLockedSize;
        set => SetPropertyOnUI(() => _totalLockedSize, v => _totalLockedSize = v, value);
    }

    private bool _hasLockingApps;
    public bool HasLockingApps
    {
        get => _hasLockingApps;
        set => SetPropertyOnUI(() => _hasLockingApps, v => _hasLockingApps = v, value);
    }

    private string _lockingAppsText = "";
    public string LockingAppsText
    {
        get => _lockingAppsText;
        set => SetPropertyOnUI(() => _lockingAppsText, v => _lockingAppsText = v, value);
    }

    private string _liveLogs = "";
    public string LiveLogs
    {
        get => _liveLogs;
        set => SetPropertyOnUI(() => _liveLogs, v => _liveLogs = v, value);
    }

    private JunkCategory? _selectedCategory;
    public JunkCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetPropertyOnUI(() => _selectedCategory, v => _selectedCategory = v, value);
    }

    public ObservableCollection<JunkCategory> Categories { get; } = new();
    public ObservableCollection<LockingAppInfo> ActiveLockingApps { get; } = new();

    public JunkViewModel(IJunkCleanerService junkEngine, ILockingAppService lockingAppService, IDialogService dialogService)
    {
        _junkEngine = junkEngine;
        _lockingAppService = lockingAppService;
        _dialogService = dialogService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DispatcherQueueInstance = _dispatcherQueue;
    }

    public JunkViewModel() : this(
        App.Services?.GetService<IJunkCleanerService>() ?? new JunkCleanerService(),
        App.Services?.GetService<ILockingAppService>() ?? new LockingAppService(),
        App.Services?.GetService<IDialogService>() ?? new DialogService())
    {
    }

    public void Initialize()
    {
        // Unsubscribe first to prevent double-registration on re-navigation
        _junkEngine.ProgressMessage -= OnProgressMessage;
        _junkEngine.ProgressChanged -= OnProgressChanged;
        _junkEngine.ProgressMessage += OnProgressMessage;
        _junkEngine.ProgressChanged += OnProgressChanged;
    }

    public void Cleanup()
    {
        _junkEngine.ProgressMessage -= OnProgressMessage;
        _junkEngine.ProgressChanged -= OnProgressChanged;

        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
        }
        catch { }
    }

    private void OnProgressMessage(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ProgressMessage = msg.T();
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LiveLogs += (string.IsNullOrEmpty(LiveLogs) ? "" : "\r\n") + $"[{timestamp}] {msg.T()}";
        });
    }

    private void OnProgressChanged(int pct)
    {
        _dispatcherQueue?.TryEnqueue(() => ProgressPercent = pct);
    }

    public async Task ScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
        }
        catch { }

        _scanCts = new System.Threading.CancellationTokenSource();
        var token = _scanCts.Token;

        IsScanning = true;
        ProgressPercent = 0;
        LiveLogs = "";
        Categories.Clear();
        SelectedCategory = null;
        ActiveLockingApps.Clear();

        try
        {
            var results = await TaskSchedulerService.Instance.RunTaskAsync("junk", t => _junkEngine.ScanJunkAsync(t), token);
            var lockingApps = await _lockingAppService.GetLockingAppsAsync();
            
            token.ThrowIfCancellationRequested();
            
            await RunOnUIActionAsync(() =>
            {
                foreach (var cat in results)
                {
                    Categories.Add(cat);
                }
                UpdateTotalSize();
                SelectedCategory = Categories.FirstOrDefault();
                
                ActiveLockingApps.Clear();
                foreach (var app in lockingApps)
                {
                    ActiveLockingApps.Add(app);
                }
                
                if (lockingApps.Count > 0)
                {
                    HasLockingApps = true;
                    var names = lockingApps.Select(a => a.Name);
                    LockingAppsText = string.Format("Applications in use: {0}. These apps lock cache files and prevent them from being cleaned.".T(), string.Join(", ", names));
                }
                else
                {
                    HasLockingApps = false;
                    LockingAppsText = "";
                }
            });
        }
        catch (OperationCanceledException)
        {
            await RunOnUIActionAsync(() =>
            {
                ProgressMessage = "Scan cancelled.".T();
                IsScanning = false;
            });
        }
        catch (Exception ex)
        {
            await RunOnUIActionAsync(() =>
            {
                ProgressMessage = "Scan failed:".T() + " " + ex.Message;
                IsScanning = false;
            });
        }
    }

    public void FinalizeScan()
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (Categories.Count > 0 && IsScanning)
            {
                ProgressMessage = "Scan completed. Select items to clean.".T();
            }
            IsScanning = false;
        });
    }

    public async Task CleanAsync()
    {
        if (IsScanning || IsCleaning || Categories.Count == 0) return;

        IsCleaning = true;
        ProgressPercent = 0;
        LiveLogs = "";

        try
        {
            long cleanedBytes = await _junkEngine.CleanJunkAsync(Categories.ToList());
            
            long lockedBytes = Categories.Where(x => x.IsSelected).Sum(x => x.LockedBytes);

            // Capture values before dispatching to avoid async void lambda in TryEnqueue
            string cleanedFormatted = FormatHelper.FormatBytes(cleanedBytes);
            string lockedFormatted = FormatHelper.FormatBytes(lockedBytes);
            bool hasLockedFiles = lockedBytes > 0;

            await RunOnUIActionAsync(() =>
            {
                if (!hasLockedFiles)
                {
                    ProgressMessage = string.Format("Cleanup complete. Reclaimed {0} MB.".T(), (cleanedBytes / 1024.0 / 1024.0).ToString("F2"));
                }

                Categories.Clear();
                SelectedCategory = null;
                TotalJunkSize = "0.0 B";
                TotalLockedSize = "0.0 B";
                HasLockingApps = false;
                LockingAppsText = "";
                ActiveLockingApps.Clear();
                IsCleaning = false;
            });

            // Show dialog on UI thread separately to avoid async void lambda
            if (hasLockedFiles)
            {
                await RunOnUIAsync(async () =>
                    await _dialogService.ShowMessageAsync(
                        "Cleanup Result".T(),
                        string.Format("Cleaning completed.\n\n✓ Cleaned: {0}\n⚠ Could not clean: {1} (files in use)".T(), cleanedFormatted, lockedFormatted)
                    )
                );
            }
        }
        catch (Exception ex)
        {
            await RunOnUIActionAsync(() =>
            {
                ProgressMessage = "Cleanup failed:".T() + " " + ex.Message;
                IsCleaning = false;
            });
        }
    }

    public void FinalizeClean()
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsCleaning = false;
        });
    }

    // FormatBytes centralized to FormatHelper.FormatBytes

    public void UpdateTotalSize()
    {
        long cleanable = Categories.Where(x => x.IsSelected).Sum(x => x.CleanableBytes);
        long locked = Categories.Where(x => x.IsSelected).Sum(x => x.LockedBytes);
        TotalJunkSize = FormatHelper.FormatBytes(cleanable);
        TotalLockedSize = FormatHelper.FormatBytes(locked);
    }

    public void CheckLockingApps()
    {
        var running = new List<string>();
        try
        {
            if (System.Diagnostics.Process.GetProcessesByName("chrome").Length > 0) running.Add("Google Chrome");
            if (System.Diagnostics.Process.GetProcessesByName("msedge").Length > 0) running.Add("Microsoft Edge");
            if (System.Diagnostics.Process.GetProcessesByName("brave").Length > 0) running.Add("Brave Browser");
            if (System.Diagnostics.Process.GetProcessesByName("firefox").Length > 0) running.Add("Mozilla Firefox");
            if (System.Diagnostics.Process.GetProcessesByName("opera").Length > 0) running.Add("Opera Browser");
        }
        catch { }

        if (running.Count > 0)
        {
            HasLockingApps = true;
            LockingAppsText = string.Format("Applications in use: {0}. These apps lock cache files and prevent them from being cleaned.".T(), string.Join(", ", running));
        }
        else
        {
            HasLockingApps = false;
            LockingAppsText = "";
        }
    }

    public async Task CloseAppsOnlyAsync()
    {
        IsCleaning = true;
        ProgressPercent = 0;
        LiveLogs = $"[{DateTime.Now:HH:mm:ss}] " + "Stopping locking applications...".T();

        var apps = await _lockingAppService.GetLockingAppsAsync();
        if (apps.Count > 0)
        {
            var userDecisions = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            await _lockingAppService.CloseAppsAsync(apps, async (appName) =>
            {
                if (userDecisions.TryGetValue(appName, out bool force))
                {
                    return force;
                }

                try
                {
                    bool decision = await RunOnUIAsync(async () => 
                        await _dialogService.ShowForceClosePromptAsync(appName));
                    userDecisions[appName] = decision;
                    return decision;
                }
                catch
                {
                    return false;
                }
            });
        }
        IsCleaning = false;
    }

    public async Task CloseLockingAppsAsync()
    {
        if (IsScanning || IsCleaning) return;
        await CloseAppsOnlyAsync();
        await ScanAsync();
    }

    public async Task ScheduleCleanupAfterRestartAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true);
                if (exePath.EndsWith(".dll"))
                {
                    string dotnet = "dotnet.exe";
                    string args = $"\"{exePath}\" /background";
                    key?.SetValue("WinCareProCleanup", $"\"{dotnet}\" {args}");
                }
                else
                {
                    key?.SetValue("WinCareProCleanup", $"\"{exePath}\" /background");
                }
            });

            // Use RunOnUIAsync instead of async void lambda in TryEnqueue to prevent unobserved exceptions
            await RunOnUIAsync(async () =>
                await _dialogService.ShowMessageAsync(
                    "Clean After Restart".T(),
                    "Cleanup scheduled successfully for next startup.".T()
                )
            );
        }
        catch (Exception ex)
        {
            await RunOnUIAsync(async () =>
                await _dialogService.ShowMessageAsync("Error".T(), ex.Message)
            );
        }
    }

    public void OpenSelectedFolder()
    {
        if (SelectedCategory == null) return;
        string path = SelectedCategory.FolderPath;
        if (string.IsNullOrEmpty(path))
        {
            if (SelectedCategory.Type == JunkType.RecycleBin)
            {
                path = "shell:RecycleBinFolder";
            }
            else
            {
                return;
            }
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        catch { }
    }
}

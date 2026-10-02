using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Implementations;
using WinCarePro.Core.Helpers;

namespace WinCarePro.ViewModels;

public class DiskViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly DiskEngine _engine;
    private readonly EventHandler _languageChangedHandler;
    private System.Threading.CancellationTokenSource? _diskCts;
    private bool _isDisposed;

    public void Initialize()
    {
        _isDisposed = false;
        SubscribeEvents();
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;
    }

    public void CancelOperations()
    {
        SetOperationState(OperationState.Cancelling);
        try
        {
            _diskCts?.Cancel();
            _diskCts?.Dispose();
            _diskCts = null;
        }
        catch { }
        IsBusy = false;
        SetOperationState(OperationState.Idle);
    }

    public void Cleanup()
    {
        CancelOperations();
        UnsubscribeEvents();
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
    }

    private string _storageScanPath = "";
    public string StorageScanPath
    {
        get => _storageScanPath;
        set
        {
            if (SetProperty(ref _storageScanPath, value))
            {
                OnPropertyChanged(nameof(CurrentAnalysisPath));
                OnPropertyChanged(nameof(CanGoUp));
            }
        }
    }

    public string CurrentAnalysisPath => StorageScanPath;
    public bool CanGoUp => !string.IsNullOrEmpty(StorageScanPath) && Directory.GetParent(StorageScanPath) != null;

    private string _consoleOutput = "Disk Tools ready.\n".T();
    public string ConsoleOutput
    {
        get => _consoleOutput;
        set => SetProperty(ref _consoleOutput, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !_isBusy;

    public ObservableCollection<DriveHealthInfo> Drives { get; } = new();
    public ObservableCollection<StorageItem> StorageItems { get; } = new();
    public ObservableCollection<StorageDuplicateGroup> DuplicateGroups { get; } = new();
    public ObservableCollection<LargeFileItem> LargeFiles { get; } = new();
    private readonly System.Collections.Generic.List<LargeFileItem> _allLargeFiles = new();

    private bool _sendToRecycleBin = true;
    public bool SendToRecycleBin
    {
        get => _sendToRecycleBin;
        set => SetProperty(ref _sendToRecycleBin, value);
    }

    private string _selectedCategoryFilter = "All";
    public string SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetProperty(ref _selectedCategoryFilter, value))
            {
                ApplyLargeFilesFilter();
            }
        }
    }

    private int _minSizeMb = 100;
    public int MinSizeMb
    {
        get => _minSizeMb;
        set => SetProperty(ref _minSizeMb, value);
    }

    private string _largeFilesTotalSizeFormatted = "0.0 B";
    public string LargeFilesTotalSizeFormatted
    {
        get => _largeFilesTotalSizeFormatted;
        set => SetProperty(ref _largeFilesTotalSizeFormatted, value);
    }

    public DiskViewModel() : this(null, null)
    {
    }

    public DiskViewModel(DiskEngine? engine = null, DispatcherQueue? dispatcherQueue = null)
    {
        _engine = engine ?? App.Services?.GetService<DiskEngine>() ?? new();
        _dispatcherQueue = dispatcherQueue ?? SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;
        // Don't subscribe events in constructor; use SubscribeEvents/UnsubscribeEvents
        // called from DiskPage.OnNavigatedTo/From to avoid double-subscription

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        StorageScanPath = Path.Combine(userProfile, "Downloads");

        _languageChangedHandler = (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                ConsoleOutput = "Disk Tools ready.\n".T();
            });
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;

        _ = LoadDrivesAsync();
    }

    public void SubscribeEvents()
    {
        // Unsubscribe first to prevent double-registration (NavigationCacheMode.Required re-fires OnNavigatedTo)
        _engine.OutputReceived -= LogText;
        _engine.OutputReceived += LogText;
    }

    public void UnsubscribeEvents()
    {
        _engine.OutputReceived -= LogText;
    }

    private void LogText(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ConsoleOutput += msg + "\n";
        });
    }

    public async Task LoadDrivesAsync()
    {
        IsBusy = true;
        try
        {
            var list = await Task.Run(() => _engine.GetDiskHealthStatus());
            RunOnUI(() =>
            {
                Drives.Clear();
                foreach (var d in list)
                {
                    Drives.Add(d);
                }
            });
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("DiskViewModel.LoadDrivesAsync", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AnalyzeStorageAsync()
    {
        if (IsBusy || !Directory.Exists(StorageScanPath))
        {
            LogText(string.Format("Directory does not exist: {0}".T(), StorageScanPath));
            return;
        }

        try
        {
            _diskCts?.Cancel();
            _diskCts?.Dispose();
        }
        catch { }

        _diskCts = new System.Threading.CancellationTokenSource();
        var token = _diskCts.Token;

        IsBusy = true;
        SetOperationState(OperationState.Running);
        StorageItems.Clear();
        LogText(string.Format("Starting disk usage analysis for: {0}...".T(), StorageScanPath));

        try
        {
            var list = await TaskSchedulerService.Instance.RunTaskAsync("disk", t => _engine.AnalyzeStorageAsync(StorageScanPath, t), token);
            token.ThrowIfCancellationRequested();
            RunOnUI(() =>
            {
                foreach (var item in list)
                {
                    StorageItems.Add(item);
                }
            });
            LogText(string.Format("Analysis complete. Found {0} items.".T(), list.Count));
            SetOperationState(OperationState.Completed);
        }
        catch (OperationCanceledException)
        {
            LogText("Storage analysis cancelled.".T());
            SetOperationState(OperationState.Idle);
        }
        catch (Exception ex)
        {
            LogText("Storage analysis error:".T() + " " + ex.Message);
            SetOperationState(OperationState.Failed);
        }
        finally
        {
            IsBusy = false;
            if (CurrentOperationState == OperationState.Running)
            {
                SetOperationState(OperationState.Completed);
            }
        }
    }

    public async Task FindDuplicatesAsync()
    {
        if (IsBusy || !Directory.Exists(StorageScanPath)) return;

        try
        {
            _diskCts?.Cancel();
            _diskCts?.Dispose();
        }
        catch { }

        _diskCts = new System.Threading.CancellationTokenSource();
        var token = _diskCts.Token;

        IsBusy = true;
        RunOnUI(() => DuplicateGroups.Clear());
        LogText(string.Format("Searching duplicate files in: {0}...".T(), StorageScanPath));

        try
        {
            var list = await TaskSchedulerService.Instance.RunTaskAsync("disk_dup", t => _engine.FindDuplicateFilesAsync(StorageScanPath, t), token);
            token.ThrowIfCancellationRequested();

            var mappedGroups = new List<StorageDuplicateGroup>();
            foreach (var group in list)
            {
                var uiGroup = new StorageDuplicateGroup { SizeFormatted = group.SizeFormatted };
                
                var sortedPaths = group.FilePaths
                    .Select(p => {
                        var fi = new FileInfo(p);
                        return new StorageDuplicateItem
                        {
                            Path = p,
                            SizeBytes = group.FileSize,
                            SizeFormatted = group.SizeFormatted,
                            LastModified = fi.Exists ? fi.LastWriteTime : DateTime.Now
                        };
                    })
                    .OrderBy(item => item.LastModified)
                    .ToList();

                for (int i = 0; i < sortedPaths.Count - 1; i++)
                {
                    sortedPaths[i].IsSelectedForDeletion = true;
                }

                foreach (var item in sortedPaths)
                {
                    uiGroup.Items.Add(item);
                }

                mappedGroups.Add(uiGroup);
            }

            RunOnUI(() =>
            {
                foreach (var g in mappedGroups)
                {
                    DuplicateGroups.Add(g);
                }
            });

            LogText(string.Format("Scan complete. Found {0} duplicate groups.".T(), mappedGroups.Count));
        }
        catch (OperationCanceledException)
        {
            LogText("Duplicate file search cancelled.".T());
        }
        catch (Exception ex)
        {
            LogText("Duplicate finder error:".T() + " " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectAllDuplicates()
    {
        foreach (var group in DuplicateGroups)
        {
            // Safety guard: Always keep 1 original copy unselected to prevent total data loss
            for (int i = 0; i < group.Items.Count; i++)
            {
                group.Items[i].IsSelectedForDeletion = (i > 0);
            }
        }
    }

    public void DeselectAllDuplicates()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var item in group.Items)
            {
                item.IsSelectedForDeletion = false;
            }
        }
    }

    public void SelectKeepNewest()
    {
        foreach (var group in DuplicateGroups)
        {
            var sorted = group.Items.OrderBy(x => x.LastModified).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].IsSelectedForDeletion = (i < sorted.Count - 1);
            }
        }
    }

    public void SelectKeepOldest()
    {
        foreach (var group in DuplicateGroups)
        {
            var sorted = group.Items.OrderByDescending(x => x.LastModified).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].IsSelectedForDeletion = (i < sorted.Count - 1);
            }
        }
    }

    public async Task CleanSelectedDuplicatesAsync()
    {
        if (IsBusy || DuplicateGroups.Count == 0) return;

        IsBusy = true;
        LogText(string.Format("Starting duplicate files cleanup (Recycle Bin: {0})...".T(), SendToRecycleBin));
        int count = 0;
        int failedCount = 0;
        long bytesSaved = 0;

        try
        {
            await Task.Run(() =>
            {
                foreach (var group in DuplicateGroups)
                {
                    // Double safety guard: Prevent wiping all copies of a file if all were checked
                    if (group.Items.Count > 0 && group.Items.All(x => x.IsSelectedForDeletion))
                    {
                        group.Items[0].IsSelectedForDeletion = false;
                    }

                    foreach (var item in group.Items)
                    {
                        if (item.IsSelectedForDeletion)
                        {
                            try
                            {
                                if (File.Exists(item.Path))
                                {
                                    if (!SafePathGuard.IsSafeToDelete(item.Path))
                                    {
                                        failedCount++;
                                        LogText(string.Format("Skipped protected file: {0}".T(), Path.GetFileName(item.Path)));
                                        continue;
                                    }

                                    var delRes = SafeFileRecycler.Delete(item.Path, SendToRecycleBin);
                                    if (delRes.IsSuccess)
                                    {
                                        count++;
                                        bytesSaved += item.SizeBytes;
                                    }
                                    else
                                    {
                                        failedCount++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                failedCount++;
                                LogText(string.Format("Failed to delete {0}: {1}".T(), Path.GetFileName(item.Path), ex.Message));
                            }
                        }
                    }
                }
            });

            LogText(string.Format("Cleaned {0} duplicate files, reclaiming {1} MB. (Failed: {2})".T(), 
                count, (bytesSaved / 1024.0 / 1024.0).ToString("F2"), failedCount));
            await FindDuplicatesAsync();
        }
        catch (Exception ex)
        {
            LogText("Cleanup error: ".T() + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ScanLargeFilesAsync()
    {
        if (IsBusy || !Directory.Exists(StorageScanPath)) return;

        try
        {
            _diskCts?.Cancel();
            _diskCts?.Dispose();
        }
        catch { }

        _diskCts = new System.Threading.CancellationTokenSource();
        var token = _diskCts.Token;

        IsBusy = true;
        SetOperationState(OperationState.Running);
        RunOnUI(() =>
        {
            LargeFiles.Clear();
            _allLargeFiles.Clear();
        });

        long minBytes = (long)MinSizeMb * 1024 * 1024;
        LogText(string.Format("Scanning for large files (>{0} MB) in: {1}...".T(), MinSizeMb, StorageScanPath));

        try
        {
            var list = await TaskSchedulerService.Instance.RunTaskAsync("disk_large", t => _engine.FindLargeFilesAsync(StorageScanPath, minBytes, t), token);
            token.ThrowIfCancellationRequested();

            RunOnUI(() =>
            {
                _allLargeFiles.Clear();
                _allLargeFiles.AddRange(list);
                ApplyLargeFilesFilter();
            });

            long totalBytes = list.Sum(x => x.SizeBytes);
            LogText(string.Format("Found {0} large files totalling {1}.".T(), list.Count, FormatHelper.FormatBytes(totalBytes)));
            SetOperationState(OperationState.Completed);
        }
        catch (OperationCanceledException)
        {
            LogText("Large files scan cancelled.".T());
            SetOperationState(OperationState.Idle);
        }
        catch (Exception ex)
        {
            LogText("Large files scan error:".T() + " " + ex.Message);
            SetOperationState(OperationState.Failed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyLargeFilesFilter()
    {
        RunOnUI(() =>
        {
            LargeFiles.Clear();
            var filtered = _selectedCategoryFilter == "All"
                ? _allLargeFiles
                : _allLargeFiles.Where(x => string.Equals(x.Category, _selectedCategoryFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var file in filtered)
            {
                LargeFiles.Add(file);
            }

            long totalBytes = LargeFiles.Sum(x => x.SizeBytes);
            LargeFilesTotalSizeFormatted = FormatHelper.FormatBytes(totalBytes);
        });
    }

    public void SelectAllLargeFiles()
    {
        foreach (var file in LargeFiles)
        {
            file.IsSelected = true;
        }
    }

    public void DeselectAllLargeFiles()
    {
        foreach (var file in LargeFiles)
        {
            file.IsSelected = false;
        }
    }

    public async Task DeleteSelectedLargeFilesAsync()
    {
        var selected = LargeFiles.Where(x => x.IsSelected).ToList();
        if (IsBusy || selected.Count == 0) return;

        IsBusy = true;
        LogText(string.Format("Deleting {0} selected large files (Recycle Bin: {1})...".T(), selected.Count, SendToRecycleBin));

        int deleted = 0;
        int failed = 0;
        long reclaimedBytes = 0;

        try
        {
            await Task.Run(() =>
            {
                foreach (var item in selected)
                {
                    if (!SafePathGuard.IsSafeToDelete(item.Path))
                    {
                        failed++;
                        LogText(string.Format("Skipped protected file: {0}".T(), Path.GetFileName(item.Path)));
                        continue;
                    }

                    var res = SafeFileRecycler.Delete(item.Path, SendToRecycleBin);
                    if (res.IsSuccess)
                    {
                        deleted++;
                        reclaimedBytes += item.SizeBytes;
                    }
                    else
                    {
                        failed++;
                    }
                }
            });

            RunOnUI(() =>
            {
                foreach (var item in selected.Where(x => !File.Exists(x.Path)))
                {
                    _allLargeFiles.Remove(item);
                    LargeFiles.Remove(item);
                }
                long totalBytes = LargeFiles.Sum(x => x.SizeBytes);
                LargeFilesTotalSizeFormatted = FormatHelper.FormatBytes(totalBytes);
            });

            LogText(string.Format("Cleaned {0} large files. Reclaimed {1}.".T(), deleted, FormatHelper.FormatBytes(reclaimedBytes)));
            Database.DbManager.LogAction($"Cleaned {deleted} large files, Reclaimed {FormatHelper.FormatBytes(reclaimedBytes)}", "Disk Tools", "Success");
        }
        catch (Exception ex)
        {
            LogText("Error deleting large files:".T() + " " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunChkdskAsync(string driveName)
    {
        if (IsBusy) return;
        IsBusy = true;
        LogText(string.Format("Initiating CHKDSK check for drive {0}...".T(), driveName));
        try
        {
            bool success = await _engine.RunChkdskAsync(driveName);
            LogText(success ? string.Format("CHKDSK completed successfully for {0}.".T(), driveName)
                            : string.Format("CHKDSK completed with warnings/errors for {0}.".T(), driveName));
        }
        catch (Exception ex)
        {
            LogText("CHKDSK error: ".T() + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ClearEmptyFoldersAsync()
    {
        if (IsBusy || !Directory.Exists(StorageScanPath)) return;
        IsBusy = true;
        LogText(string.Format("Scanning and removing empty directories in {0}...".T(), StorageScanPath));
        try
        {
            int deletedCount = await _engine.ClearEmptyFoldersAsync(StorageScanPath);
            LogText(string.Format("Empty directory cleanup finished. Removed {0} empty folders.".T(), deletedCount));
        }
        catch (Exception ex)
        {
            LogText("Empty directory cleanup error: ".T() + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}


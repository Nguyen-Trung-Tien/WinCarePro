using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class DiskViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DiskEngine _engine = new();

    private string _storageScanPath = "";
    public string StorageScanPath
    {
        get => _storageScanPath;
        set => SetProperty(ref _storageScanPath, value);
    }

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

    public DiskViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        // Don't subscribe events in constructor; use SubscribeEvents/UnsubscribeEvents
        // called from DiskPage.OnNavigatedTo/From to avoid double-subscription

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        StorageScanPath = Path.Combine(userProfile, "Downloads");

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
        _dispatcherQueue.TryEnqueue(() =>
        {
            ConsoleOutput += msg + "\n";
        });
    }

    public async Task LoadDrivesAsync()
    {
        IsBusy = true;
        Drives.Clear();
        try
        {
            var list = await Task.Run(() => _engine.GetDiskHealthStatus());
            foreach (var d in list)
            {
                Drives.Add(d);
            }
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

        IsBusy = true;
        StorageItems.Clear();
        LogText(string.Format("Starting disk usage analysis for: {0}...".T(), StorageScanPath));

        try
        {
            var list = await _engine.AnalyzeStorageAsync(StorageScanPath);
            foreach (var item in list)
            {
                StorageItems.Add(item);
            }
            LogText(string.Format("Analysis complete. Found {0} items.".T(), StorageItems.Count));
        }
        catch (Exception ex)
        {
            LogText("Storage analysis error:".T() + " " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task FindDuplicatesAsync()
    {
        if (IsBusy || !Directory.Exists(StorageScanPath)) return;

        IsBusy = true;
        DuplicateGroups.Clear();
        LogText(string.Format("Searching duplicate files in: {0}...".T(), StorageScanPath));

        try
        {
            var list = await _engine.FindDuplicateFilesAsync(StorageScanPath);
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

                DuplicateGroups.Add(uiGroup);
            }
            LogText(string.Format("Scan complete. Found {0} duplicate groups.".T(), DuplicateGroups.Count));
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

    public async Task CleanSelectedDuplicatesAsync()
    {
        if (IsBusy || DuplicateGroups.Count == 0) return;

        IsBusy = true;
        LogText("Starting duplicate files cleanup...".T());
        int count = 0;
        long bytesSaved = 0;

        try
        {
            await Task.Run(() =>
            {
                foreach (var group in DuplicateGroups)
                {
                    foreach (var item in group.Items)
                    {
                        if (item.IsSelectedForDeletion)
                        {
                            try
                            {
                                if (File.Exists(item.Path))
                                {
                                    File.Delete(item.Path);
                                    count++;
                                    bytesSaved += item.SizeBytes;
                                }
                            }
                            catch { }
                        }
                    }
                }
            });

            LogText(string.Format("Cleaned {0} duplicate files, reclaiming {1} MB.".T(), count, (bytesSaved / 1024.0 / 1024.0).ToString("F2")));
            _ = FindDuplicatesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}

// These helper classes were in StorageViewModel - kept for DiskPage
public class StorageDuplicateItem : ViewModelBase
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime LastModified { get; set; }

    private bool _isSelectedForDeletion;
    public bool IsSelectedForDeletion
    {
        get => _isSelectedForDeletion;
        set => SetProperty(ref _isSelectedForDeletion, value);
    }
}

public class StorageDuplicateGroup : ViewModelBase
{
    public string SizeFormatted { get; set; } = "";
    public ObservableCollection<StorageDuplicateItem> Items { get; } = new();
}

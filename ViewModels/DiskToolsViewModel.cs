using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public class DiskToolsViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DiskEngine _engine = new();

    private string _storageScanPath = "C:\\";
    private string _emptyFolderRootPath = "";
    private string _consoleOutput = "Disk Tools ready.\n";
    private bool _isBusy;

    public string StorageScanPath
    {
        get => _storageScanPath;
        set => SetProperty(ref _storageScanPath, value);
    }

    public string EmptyFolderRootPath
    {
        get => _emptyFolderRootPath;
        set => SetProperty(ref _emptyFolderRootPath, value);
    }

    public string ConsoleOutput
    {
        get => _consoleOutput;
        set => SetProperty(ref _consoleOutput, value);
    }

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
    public ObservableCollection<DuplicateFileGroup> Duplicates { get; } = new();

    public DiskToolsViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.OutputReceived += LogText;
        
        // Default root path
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        StorageScanPath = Path.Combine(userProfile, "Downloads");
        EmptyFolderRootPath = Path.Combine(userProfile, "AppData\\Local\\Temp");
        
        _ = LoadDrivesAsync();
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
            LogText($"Directory does not exist: {StorageScanPath}");
            return;
        }
        
        IsBusy = true;
        StorageItems.Clear();
        LogText($"Starting disk usage analysis for: {StorageScanPath}...");

        try
        {
            var list = await _engine.AnalyzeStorageAsync(StorageScanPath);
            foreach (var item in list)
            {
                StorageItems.Add(item);
            }
            LogText($"Analysis complete. Found {StorageItems.Count} items.");
        }
        catch (Exception ex)
        {
            LogText($"Storage analysis error: {ex.Message}");
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
        Duplicates.Clear();
        LogText($"Searching duplicate files in: {StorageScanPath}...");

        try
        {
            var list = await _engine.FindDuplicateFilesAsync(StorageScanPath);
            foreach (var group in list)
            {
                Duplicates.Add(group);
            }
            LogText($"Scan complete. Found {Duplicates.Count} duplicate groups.");
        }
        catch (Exception ex)
        {
            LogText($"Duplicate finder error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CleanEmptyFoldersAsync()
    {
        if (IsBusy || !Directory.Exists(EmptyFolderRootPath)) return;

        IsBusy = true;
        LogText($"Scanning and cleaning empty folders in: {EmptyFolderRootPath}...");

        try
        {
            int deleted = await _engine.ClearEmptyFoldersAsync(EmptyFolderRootPath);
            LogText($"Cleaned {deleted} empty directories successfully.");
        }
        catch (Exception ex)
        {
            LogText($"Empty folders cleanup error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunChkdskAsync(string driveLetter)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _engine.RunChkdskAsync(driveLetter);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

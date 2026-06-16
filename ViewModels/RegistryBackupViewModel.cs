using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class RegistryBackupViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly RegistryBackupEngine _engine = new();

    private bool _isScanningRegistry;
    private bool _isBusy;
    private string _statusMessage = "Ready";
    private string _backupName = "Manual_Backup";
    private string _restorePointName = "Before WinCare Pro Maintenance";

    public bool IsScanningRegistry
    {
        get => _isScanningRegistry;
        set => SetProperty(ref _isScanningRegistry, value);
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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string BackupName
    {
        get => _backupName;
        set => SetProperty(ref _backupName, value);
    }

    public string RestorePointName
    {
        get => _restorePointName;
        set => SetProperty(ref _restorePointName, value);
    }

    public ObservableCollection<RegistryIssue> RegistryIssues { get; } = new();
    public ObservableCollection<RegistryBackupItem> RegistryBackups { get; } = new();
    public ObservableCollection<RestorePointInfo> RestorePoints { get; } = new();

    public RegistryBackupViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = LoadSystemRestoreDataAsync();
        LoadRegistryBackupsList();
    }

    public async Task ScanRegistryAsync()
    {
        if (IsScanningRegistry) return;
        IsScanningRegistry = true;
        StatusMessage = "Scanning registry trees for broken links...";
        RegistryIssues.Clear();

        try
        {
            var list = await Task.Run(() => _engine.ScanRegistryIssues());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var issue in list)
                {
                    RegistryIssues.Add(issue);
                }
                StatusMessage = $"Scan complete. Identified {RegistryIssues.Count} registry references.";
                IsScanningRegistry = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = $"Scan failed: {ex.Message}";
                IsScanningRegistry = false;
            });
        }
    }

    public async Task FixRegistryAsync()
    {
        if (RegistryIssues.Count == 0 || IsScanningRegistry || IsBusy) return;
        IsBusy = true;
        StatusMessage = "Repairing selected registry records...";

        try
        {
            bool ok = await _engine.FixRegistryIssuesAsync(RegistryIssues.ToList());
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = ok ? "Selected registry database corrections applied." : "Some registry fixes could not be applied.";
                RegistryIssues.Clear();
                LoadRegistryBackupsList(); // refresh list since we backed up before fixing
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() => StatusMessage = $"Repair failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LoadRegistryBackupsList()
    {
        RegistryBackups.Clear();
        try
        {
            var backups = _engine.GetRegistryBackupsList();
            foreach (var b in backups)
            {
                RegistryBackups.Add(b);
            }
        }
        catch { }
    }

    public async Task CreateRegistryBackupAsync()
    {
        if (string.IsNullOrEmpty(BackupName) || IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Exporting User Hive to: {BackupName}...";

        try
        {
            bool ok = await Task.Run(() => _engine.CreateRegistryBackup(BackupName));
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = ok ? "Registry hive exported successfully." : "Failed to export registry.";
                LoadRegistryBackupsList();
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreRegistryBackupAsync(string path)
    {
        if (!File.Exists(path) || IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Restoring registry records from: {Path.GetFileName(path)}...";

        try
        {
            bool ok = await Task.Run(() => _engine.RestoreRegistryBackup(path));
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = ok ? "Registry records restored. Restart recommended." : "Import failed.";
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    // System Restore Points
    public async Task LoadSystemRestoreDataAsync()
    {
        RestorePoints.Clear();
        try
        {
            var points = await Task.Run(() => _engine.GetSystemRestorePoints());
            foreach (var p in points)
            {
                RestorePoints.Add(p);
            }
        }
        catch { }
    }

    public async Task CreateRestorePointAsync()
    {
        if (string.IsNullOrEmpty(RestorePointName) || IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating system snapshot restore point...";

        try
        {
            bool ok = await Task.Run(() => _engine.CreateSystemRestorePoint(RestorePointName));
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = ok ? "Windows System Restore point created successfully." : "Failed to create restore point (Verify System Protection is Enabled).";
                _ = LoadSystemRestoreDataAsync();
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LaunchRestoreWizard()
    {
        _engine.LaunchRestoreWizard();
        StatusMessage = "Windows System Restore Wizard launched.";
    }
}

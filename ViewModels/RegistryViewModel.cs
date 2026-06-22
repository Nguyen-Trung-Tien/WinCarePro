using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class RegistryViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly RegistryBackupEngine _engine = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _scanProgress;
    public int ScanProgress
    {
        get => _scanProgress;
        set => SetProperty(ref _scanProgress, value);
    }

    public ObservableCollection<RegistryIssue> Issues { get; } = new();
    public ObservableCollection<RegistryBackupItem> Backups { get; } = new();

    public RegistryViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        LoadBackups();
    }

    public void LoadBackups()
    {
        Backups.Clear();
        var list = _engine.GetRegistryBackupsList();
        foreach (var b in list)
        {
            Backups.Add(b);
        }
    }

    public async Task ScanRegistryAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ScanProgress = 0;
        StatusText = "Scanning registry for broken paths...";
        Issues.Clear();

        try
        {
            var list = await Task.Run(() => _engine.ScanRegistryIssues());
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var issue in list)
                {
                    Issues.Add(issue);
                }
                ScanProgress = 100;
                StatusText = $"Scan complete. Found {Issues.Count} issues.";
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairSelectedAsync()
    {
        if (IsBusy || Issues.Count == 0) return;
        IsBusy = true;
        StatusText = "Repairing selected registry issues...";

        try
        {
            var selected = Issues.Where(x => x.IsSelected).ToList();
            await _engine.FixRegistryIssuesAsync(selected);
            StatusText = $"Repaired {selected.Count} registry issues.";
            await ScanRegistryAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Repair failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BackupRegistryAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Creating registry backup...";

        try
        {
            await Task.Run(() => _engine.CreateRegistryBackup("UserBackup"));
            StatusText = "Registry backup created successfully.";
            LoadBackups();
        }
        catch (Exception ex)
        {
            StatusText = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

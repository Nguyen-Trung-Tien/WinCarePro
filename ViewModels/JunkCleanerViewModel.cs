using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.ViewModels;

public class JunkCleanerViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly JunkCleanerEngine _engine = new();

    private bool _isScanning;
    private bool _isCleaning;
    private string _progressMessage = "Ready to scan";
    private int _progressPercent;
    private string _totalJunkSize = "0.0 MB";

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public bool IsCleaning
    {
        get => _isCleaning;
        set => SetProperty(ref _isCleaning, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public string TotalJunkSize
    {
        get => _totalJunkSize;
        set => SetProperty(ref _totalJunkSize, value);
    }

    public ObservableCollection<JunkCategory> Categories { get; } = new();

    public JunkCleanerViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _engine.ProgressMessage += Msg => _dispatcherQueue.TryEnqueue(() => ProgressMessage = Msg);
        _engine.ProgressChanged += Pct => _dispatcherQueue.TryEnqueue(() => ProgressPercent = Pct);
    }

    public async Task ScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        IsScanning = true;
        ProgressPercent = 0;
        Categories.Clear();

        try
        {
            var results = await _engine.ScanJunkAsync();
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var cat in results)
                {
                    Categories.Add(cat);
                }
                UpdateTotalSize();
                IsScanning = false;
                ProgressMessage = "Scan completed. Select items to clean.";
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

    public async Task CleanAsync()
    {
        if (IsScanning || IsCleaning || Categories.Count == 0) return;

        IsCleaning = true;
        ProgressPercent = 0;

        try
        {
            long cleanedBytes = await _engine.CleanJunkAsync(Categories.ToList());
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsCleaning = false;
                ProgressMessage = $"Cleanup complete. Reclaimed {(cleanedBytes / 1024.0 / 1024.0):F2} MB.";
                Categories.Clear();
                TotalJunkSize = "0.0 MB";
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = $"Cleanup failed: {ex.Message}";
                IsCleaning = false;
            });
        }
    }

    public void UpdateTotalSize()
    {
        long bytes = Categories.Where(x => x.IsSelected).Sum(x => x.SizeBytes);
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        TotalJunkSize = $"{doubleBytes:F1} {suffix[i]}";
    }
}

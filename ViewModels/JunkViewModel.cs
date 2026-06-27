using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class JunkViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IJunkCleanerService _junkEngine;

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    private bool _isCleaning;
    public bool IsCleaning
    {
        get => _isCleaning;
        set => SetProperty(ref _isCleaning, value);
    }

    private string _progressMessage = "Ready to scan junk files".T();
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

    private string _totalJunkSize = "0.0 MB";
    public string TotalJunkSize
    {
        get => _totalJunkSize;
        set => SetProperty(ref _totalJunkSize, value);
    }

    private JunkCategory? _selectedCategory;
    public JunkCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public ObservableCollection<JunkCategory> Categories { get; } = new();

    public JunkViewModel(IJunkCleanerService junkEngine)
    {
        _junkEngine = junkEngine;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public JunkViewModel() : this(new JunkCleanerService())
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
    }

    private void OnProgressMessage(string msg)
    {
        _dispatcherQueue?.TryEnqueue(() => ProgressMessage = msg.T());
    }

    private void OnProgressChanged(int pct)
    {
        _dispatcherQueue?.TryEnqueue(() => ProgressPercent = pct);
    }

    public async Task ScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        IsScanning = true;
        ProgressPercent = 0;
        Categories.Clear();
        SelectedCategory = null;

        try
        {
            var results = await _junkEngine.ScanJunkAsync();
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var cat in results)
                {
                    Categories.Add(cat);
                }
                UpdateTotalSize();
                SelectedCategory = Categories.FirstOrDefault();
                IsScanning = false;
                ProgressMessage = "Scan completed. Select items to clean.".T();
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Scan failed:".T() + " " + ex.Message;
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
            long cleanedBytes = await _junkEngine.CleanJunkAsync(Categories.ToList());
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsCleaning = false;
                ProgressMessage = string.Format("Cleanup complete. Reclaimed {0} MB.".T(), (cleanedBytes / 1024.0 / 1024.0).ToString("F2"));
                Categories.Clear();
                SelectedCategory = null;
                TotalJunkSize = "0.0 MB";
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressMessage = "Cleanup failed:".T() + " " + ex.Message;
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

using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class ProcessInfo : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => $"{Name} ({Id})";

    private double _cpuUsage;
    public double CpuUsage
    {
        get => _cpuUsage;
        set
        {
            if (SetProperty(ref _cpuUsage, value))
            {
                OnPropertyChanged(nameof(CpuUsageFormatted));
            }
        }
    }
    public string CpuUsageFormatted => $"{CpuUsage:F1}%";

    private long _ramUsageBytes;
    public long RamUsageBytes
    {
        get => _ramUsageBytes;
        set
        {
            if (SetProperty(ref _ramUsageBytes, value))
            {
                OnPropertyChanged(nameof(RamUsageFormatted));
            }
        }
    }
    public string RamUsageFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(RamUsageBytes);

    private double _diskUsageMb;
    public double DiskUsageMb
    {
        get => _diskUsageMb;
        set
        {
            if (SetProperty(ref _diskUsageMb, value))
            {
                OnPropertyChanged(nameof(DiskUsageFormatted));
            }
        }
    }
    public string DisplayDiskUsage => DiskUsageMb > 0.1 ? $"{DiskUsageMb:F1} MB/s" : "0 MB/s";
    public string DiskUsageFormatted => DisplayDiskUsage;

    private double _networkUsageKb;
    public double NetworkUsageKb
    {
        get => _networkUsageKb;
        set
        {
            if (SetProperty(ref _networkUsageKb, value))
            {
                OnPropertyChanged(nameof(NetworkUsageFormatted));
            }
        }
    }
    public string DisplayNetworkUsage => NetworkUsageKb > 0.1 ? $"{NetworkUsageKb:F1} KB/s" : "0 KB/s";
    public string NetworkUsageFormatted => DisplayNetworkUsage;

    public string FilePath { get; set; } = "";
    public string Publisher { get; set; } = "Unknown Publisher";

    private string _iconPath = "";
    public string IconPath
    {
        get => _iconPath;
        set
        {
            if (SetProperty(ref _iconPath, value))
            {
                OnPropertyChanged(nameof(IconImageSource));
                OnPropertyChanged(nameof(HasIcon));
                OnPropertyChanged(nameof(FallbackVisibility));
                OnPropertyChanged(nameof(IconVisibility));
            }
        }
    }

    public Microsoft.UI.Xaml.Media.ImageSource? IconImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath) || !System.IO.File.Exists(IconPath)) return null;
            try
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(IconPath));
            }
            catch
            {
                return null;
            }
        }
    }

    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);

    public Microsoft.UI.Xaml.Visibility IconVisibility => HasIcon 
        ? Microsoft.UI.Xaml.Visibility.Visible 
        : Microsoft.UI.Xaml.Visibility.Collapsed;

    public Microsoft.UI.Xaml.Visibility FallbackVisibility => HasIcon 
        ? Microsoft.UI.Xaml.Visibility.Collapsed 
        : Microsoft.UI.Xaml.Visibility.Visible;

    // Detailed metadata properties (Lazy loaded on selection)
    private int _threadCount;
    public int ThreadCount
    {
        get => _threadCount;
        set => SetProperty(ref _threadCount, value);
    }

    private int _handleCount;
    public int HandleCount
    {
        get => _handleCount;
        set => SetProperty(ref _handleCount, value);
    }

    private string _startTime = "";
    public string StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    private string _commandLine = "";
    public string CommandLine
    {
        get => _commandLine;
        set => SetProperty(ref _commandLine, value);
    }

    private string _priorityClass = "Normal";
    public string PriorityClass
    {
        get => _priorityClass;
        set => SetProperty(ref _priorityClass, value);
    }

    private int _parentPid;
    public int ParentPid
    {
        get => _parentPid;
        set => SetProperty(ref _parentPid, value);
    }

    private string _status = "Running";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}

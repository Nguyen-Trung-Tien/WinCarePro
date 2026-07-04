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
    public string RamUsageFormatted => FormatSize(RamUsageBytes);

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
    public string DisplayDiskUsage => DiskUsageMb > 0.1 ? $"{DiskUsageMb:F1} MB/s" : "0 MB/s"; // Add backing variable for safety
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
    public string DisplayNetworkUsage => NetworkUsageKb > 0.1 ? $"{NetworkUsageKb:F1} KB/s" : "0 KB/s"; // Add backing variable for safety
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

    private static string FormatSize(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {suffix[i]}";
    }
}

public enum JunkType
{
    WindowsTemp,
    UserTemp,
    BrowserCache,
    SystemLog,
    RecycleBin,
    UpdateCache,
    ShaderCache,
    ThumbnailCache,
    DeliveryOptimization,
    Prefetch,
    CrashDumps
}

public class JunkFileItem
{
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeFormatted => FormatSize(SizeBytes);
    public string FileName => System.IO.Path.GetFileName(Path);
    
    // Status properties
    public bool IsLocked { get; set; } = false;
    public string IconGlyph => IsLocked ? "\uE72E" : "\uE7C3"; // Lock vs File icon
    public string IconColor => IsLocked ? "#FFEF4444" : "#FFF59E0B"; // Red if locked, Amber if ready
    public string StatusText => IsLocked ? "Locked / In Use".T() : "Ready to Clean".T();
    
    public Microsoft.UI.Xaml.Media.Brush StatusBgColor => IsLocked 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)) 
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129));

    public Microsoft.UI.Xaml.Media.Brush StatusForegroundColor => IsLocked 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)) 
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));

    private static string FormatSize(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {suffix[i]}";
    }
}

public class JunkCategory
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public JunkType Type { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted => FormatSize(SizeBytes);
    
    public long CleanableBytes { get; set; }
    public long LockedBytes { get; set; }
    public string CleanableSizeFormatted => FormatSize(CleanableBytes);
    public string LockedSizeFormatted => FormatSize(LockedBytes);
    public Microsoft.UI.Xaml.Visibility LockedSizeVisibility => LockedBytes > 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public bool IsSelected { get; set; } = true;
    public int FileCount { get; set; }
    public string FileCountFormatted => $"{FileCount} files";
    
    public string IconGlyph { get; set; } = "\uEA99";
    public string IconColor { get; set; } = "#FF7F56D9";
    public string FolderPath { get; set; } = "";
    public System.Collections.Generic.List<JunkFileItem> TopFiles { get; set; } = new();

    private static string FormatSize(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {suffix[i]}";
    }
}

public class SoftwareUpdateInfo : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string InstalledVersion { get; set; } = "";
    public string AvailableVersion { get; set; } = "";
    public string Source { get; set; } = "winget";

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    private string _updateStatus = "Available";
    public string UpdateStatus // Available, Updating, Completed, Failed
    {
        get => _updateStatus;
        set
        {
            if (_updateStatus != value)
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
                OnPropertyChanged(nameof(IsUpdating));
                OnPropertyChanged(nameof(IsNotUpdating));
                OnPropertyChanged(nameof(CanUpdate));
                OnPropertyChanged(nameof(IsUpdatingVisibility));
                OnPropertyChanged(nameof(IsNotUpdatingVisibility));
                OnPropertyChanged(nameof(StatusBgColor));
                OnPropertyChanged(nameof(StatusBorderColor));
                OnPropertyChanged(nameof(StatusForegroundColor));
            }
        }
    }

    public bool IsUpdating => UpdateStatus == "Updating...";
    public bool IsNotUpdating => UpdateStatus != "Updating...";
    public bool CanUpdate => UpdateStatus != "Completed" && UpdateStatus != "Updating...";

    public Microsoft.UI.Xaml.Visibility IsUpdatingVisibility => IsUpdating ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility IsNotUpdatingVisibility => IsNotUpdating ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public Microsoft.UI.Xaml.Media.Brush StatusBgColor => UpdateStatus switch
    {
        "Completed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129)),  // #1E10B981
        "Failed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)),    // #1EEF4444
        "Updating..." => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11)), // #1EF59E0B
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(20, 59, 130, 246))           // #143B82F6
    };

    public Microsoft.UI.Xaml.Media.Brush StatusBorderColor => UpdateStatus switch
    {
        "Completed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(48, 16, 185, 129)),
        "Failed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(48, 239, 68, 68)),
        "Updating..." => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(48, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(32, 59, 130, 246))
    };

    public Microsoft.UI.Xaml.Media.Brush StatusForegroundColor => UpdateStatus switch
    {
        "Completed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
        "Failed" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        "Updating..." => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246))
    };
}

public enum StartupSource
{
    RegistryRunHKCU,
    RegistryRunHKLM,
    RegistryRunWow64,
    StartupFolderUser,
    StartupFolderCommon,
    TaskScheduler
}

public class StartupEntry : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Path { get; set; } = "";
    public StartupSource Source { get; set; }
    public string SourceFormatted => Source.ToString();

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public int StartupDelayMs { get; set; }
    public string Impact => StartupDelayMs switch
    {
        < 150 => "Low",
        < 500 => "Medium",
        < 2000 => "High",
        _ => "Critical"
    };

    // New Properties
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);
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
    public string Publisher { get; set; } = "Unknown";

    private string _startupImpact = "Medium";
    public string StartupImpact
    {
        get => _startupImpact;
        set { if (_startupImpact != value) { _startupImpact = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImpactBgBrush)); OnPropertyChanged(nameof(ImpactFgBrush)); } }
    }

    public bool IsMicrosoft { get; set; }
    public bool IsSystemItem { get; set; }
    public int EstimatedLaunchTimeMs { get; set; }
    public bool IsRecommendedDisable { get; set; }

    // UI Helper properties
    public Microsoft.UI.Xaml.Media.Brush ImpactBgBrush => StartupImpact switch
    {
        "Critical" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)),
        "High" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68)),
        "Medium" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
    };

    public Microsoft.UI.Xaml.Media.Brush ImpactFgBrush => StartupImpact switch
    {
        "Critical" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        "High" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
        "Medium" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
    };
}

public class ServiceEntry : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";

    private string _status = "";
    public string Status
    {
        get => _status;
        set 
        { 
            if (_status != value) 
            { 
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StatusBgBrush)); 
                OnPropertyChanged(nameof(StatusFgBrush)); 
                OnPropertyChanged(nameof(IsRunning)); 
                OnPropertyChanged(nameof(IsNotRunning)); 
            } 
        }
    }

    private string _startupType = "";
    public string StartupType
    {
        get => _startupType;
        set { if (_startupType != value) { _startupType = value; OnPropertyChanged(); } }
    }

    public bool CanStop { get; set; }

    // New Properties
    public string ImagePath { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Publisher { get; set; } = "Unknown";
    public bool IsSystemService { get; set; }
    public bool IsCriticalService { get; set; }
    public bool IsMicrosoftService { get; set; }
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);
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
    public string ServiceDescription { get; set; } = "";
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

    // UI Helper properties
    public Microsoft.UI.Xaml.Media.Brush StatusBgBrush => Status.Equals("Running", StringComparison.OrdinalIgnoreCase)
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 107, 114, 128));

    public Microsoft.UI.Xaml.Media.Brush StatusFgBrush => Status.Equals("Running", StringComparison.OrdinalIgnoreCase)
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 107, 114, 128));

    public Microsoft.UI.Xaml.Media.Brush CategoryBgBrush => IsMicrosoftService
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 59, 130, 246))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 127, 86, 217));

    public Microsoft.UI.Xaml.Media.Brush CategoryFgBrush => IsMicrosoftService
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 86, 217));

    public string CategoryText => IsMicrosoftService ? "System" : "Third-Party";

    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase);
    public bool IsNotRunning => !IsRunning;
}

public class ScheduledTaskEntry : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Action { get; set; } = "";
    
    private string _status = "";
    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }

    // New Properties
    public string Author { get; set; } = "";
    public string Folder { get; set; } = "";
    public bool IsMicrosoftTask { get; set; }
    public bool IsCriticalTask { get; set; }
    public int LastResult { get; set; }
    public string TaskDescription { get; set; } = "";
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

    // UI Helper properties
    public string DisplayLastRunTime => LastRunTime.HasValue ? LastRunTime.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
    public string DisplayNextRunTime => NextRunTime.HasValue ? NextRunTime.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
}

public class DriverInfo : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public string Name { get; set; } = "";
    public string DeviceClass { get; set; } = "";
    public string Provider { get; set; } = "";
    public string DriverVersion { get; set; } = "";
    public string DriverDate { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasUpdate { get; set; }
    public string AvailableVersion { get; set; } = "";

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    private string _updateStatus = "Available";
    public string UpdateStatus
    {
        get => _updateStatus;
        set
        {
            if (_updateStatus != value)
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
            }
        }
    }
}

public class RegistryIssue
{
    public string Section { get; set; } = ""; // e.g. "Shared DLLs", "Startup Programs"
    public string KeyPath { get; set; } = "";
    public string ValueName { get; set; } = "";
    public string ValueData { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSelected { get; set; } = true;
}

public class HardwareSpecs
{
    public string CpuModel { get; set; } = "Loading...";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string CpuSpeed { get; set; } = "";
    
    public double RamCapacityGb { get; set; }
    public string RamSpeed { get; set; } = "";
    
    public string GpuModel { get; set; } = "Loading...";
    public string GpuVram { get; set; } = "";
    public string GpuDriverVersion { get; set; } = "";
    
    public string MotherboardManufacturer { get; set; } = "";
    public string MotherboardModel { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    
    public string StorageInfo { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string SystemUptime { get; set; } = "";
}

public class DiagnosticResult
{
    public string CheckName { get; set; } = "";
    public string Category { get; set; } = ""; // Performance, Storage, Network, Security, Software
    public bool IsHealthy { get; set; } = true;
    public string Description { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

public class RegistryBackupItem
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public class SystemTweak : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = ""; // Performance, Network, Disk, UI Responsiveness
    public string IconGlyph { get; set; } = "";
    public string RegistryPath { get; set; } = "";
    public string RecommendedValue { get; set; } = "";

    private string _currentValue = "";
    public string CurrentValue
    {
        get => _currentValue;
        set
        {
            if (_currentValue != value)
            {
                _currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
                OnPropertyChanged(nameof(ComparisonText));
            }
        }
    }

    private bool _isOptimized;
    public bool IsOptimized
    {
        get => _isOptimized;
        set
        {
            if (_isOptimized != value)
            {
                _isOptimized = value;
                OnPropertyChanged(nameof(IsOptimized));
                OnPropertyChanged(nameof(StatusFormatted));
                OnPropertyChanged(nameof(ComparisonText));
                OnPropertyChanged(nameof(StatusBgColor));
                OnPropertyChanged(nameof(StatusBorderColor));
                OnPropertyChanged(nameof(StatusForegroundColor));
            }
        }
    }

    public string StatusFormatted => IsOptimized ? "Optimized" : "Available";

    public Microsoft.UI.Xaml.Media.Brush StatusBgColor => IsOptimized 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129)) 
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(20, 59, 130, 246));

    public Microsoft.UI.Xaml.Media.Brush StatusBorderColor => IsOptimized 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(48, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(32, 59, 130, 246));

    public Microsoft.UI.Xaml.Media.Brush StatusForegroundColor => IsOptimized 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246));

    public string ComparisonText
    {
        get
        {
            string cur = CurrentValue;
            string rec = RecommendedValue;
            try
            {
                if (Id == "AllowAutoGameMode" || Id == "NtfsDisableLastAccessUpdate" || Id == "AllowTelemetry" || Id == "AllowCortana" || Id == "WerDisabled" || Id == "DisableBackoff")
                {
                    cur = cur == "1" ? "Enabled".T() : "Disabled".T();
                    rec = rec == "1" ? "Enabled".T() : "Disabled".T();
                }
                else if (Id == "HwSchMode")
                {
                    cur = cur == "2" ? "Enabled".T() : "Disabled".T();
                    rec = rec == "2" ? "Enabled".T() : "Disabled".T();
                }
                else if (Id == "MenuShowDelay")
                {
                    cur = $"{cur} ms";
                    rec = $"{rec} ms";
                }
                else if (Id == "WaitToKillAppTimeout")
                {
                    if (double.TryParse(cur, out double curMs))
                        cur = $"{curMs / 1000.0} s";
                    if (double.TryParse(rec, out double recMs))
                        rec = $"{recMs / 1000.0} s";
                }
                else if (Id == "NetworkThrottlingIndex")
                {
                    cur = (cur == "-1" || cur == "4294967295") ? "Disabled".T() : "Default (10)".T();
                    rec = "Disabled".T();
                }
                else if (Id == "SystemResponsiveness")
                {
                    cur = cur == "0" ? "High Priority (0)".T() : $"Normal (20)".T();
                    rec = "High Priority (0)".T();
                }
                else if (Id == "MinAnimate")
                {
                    cur = cur == "0" ? "Disabled".T() : "Enabled".T();
                    rec = "Disabled".T();
                }
            }
            catch { }
            
            return string.Format("Current: {0} | Recommended: {1}".T(), cur, rec);
        }
    }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}

public class OptimizationSummary
{
    public long JunkBytesCleaned { get; set; }
    public int RegistryIssuesFixed { get; set; }
    public long RamBytesReclaimed { get; set; }
    public int RamProcessesOptimized { get; set; }
    public long DoCacheBytesCleaned { get; set; }
    public bool DnsCacheFlushed { get; set; }
    public int TweaksApplied { get; set; }
}

public class InstalledAppInfo : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public string DisplayName { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Version { get; set; } = "";
    public string InstallDate { get; set; } = "";
    public string InstallLocation { get; set; } = "";
    public string UninstallString { get; set; } = "";
    public string RegistryKeyName { get; set; } = "";
    public string Hive { get; set; } = ""; // HKLM or HKCU
    public string RegistryPath { get; set; } = "";
    public string DisplayIcon { get; set; } = "";
    public bool IsStoreApp { get; set; } = false;
    public bool IsDesktopApp => !IsStoreApp;
    public string IconPath { get; set; } = "";
    
    public Microsoft.UI.Xaml.Media.ImageSource? IconImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath)) return null;
            try
            {
                if (Uri.TryCreate(IconPath, UriKind.Absolute, out var uri))
                {
                    return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
                }
                else
                {
                    return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(System.IO.Path.GetFullPath(IconPath)));
                }
            }
            catch
            {
                return null;
            }
        }
    }
    
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath);

    public Microsoft.UI.Xaml.Visibility IconVisibility => HasIcon 
        ? Microsoft.UI.Xaml.Visibility.Visible 
        : Microsoft.UI.Xaml.Visibility.Collapsed;

    public Microsoft.UI.Xaml.Visibility FallbackVisibility => HasIcon 
        ? Microsoft.UI.Xaml.Visibility.Collapsed 
        : Microsoft.UI.Xaml.Visibility.Visible;

    public Microsoft.UI.Xaml.Media.Brush IconBackground => IsStoreApp 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 0, 193, 238)) 
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 127, 86, 217));

    public Microsoft.UI.Xaml.Media.Brush IconForeground => IsStoreApp 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 193, 238)) 
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 86, 217));

    public string DefaultIconGlyph => IsStoreApp ? "\uE719" : "\uE736";

    public long SizeBytes { get; set; }
    public string SizeFormatted
    {
        get
        {
            if (SizeBytes <= 0) return "Unknown";
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double doubleBytes = SizeBytes;
            while (doubleBytes >= 1024 && i < suffix.Length - 1)
            {
                i++;
                doubleBytes /= 1024;
            }
            return $"{doubleBytes:F1} {suffix[i]}";
        }
    }
}

public enum LeftoverType
{
    File,
    Directory,
    RegistryKey,
    RegistryValue
}

public class LeftoverItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public LeftoverType Type { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted
    {
        get
        {
            if (Type == LeftoverType.RegistryKey || Type == LeftoverType.RegistryValue) return "N/A";
            if (SizeBytes <= 0) return "0 B";
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double doubleBytes = SizeBytes;
            while (doubleBytes >= 1024 && i < suffix.Length - 1)
            {
                i++;
                doubleBytes /= 1024;
            }
            return $"{doubleBytes:F1} {suffix[i]}";
        }
    }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public string IconGlyph => Type switch
    {
        LeftoverType.Directory => "\uE8B7", // Folder icon
        LeftoverType.File => "\uE7C3",      // File icon
        _ => "\uE945"                       // Registry Key icon
    };

    public Microsoft.UI.Xaml.Media.Brush IconBackground => Type switch
    {
        LeftoverType.Directory => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 245, 158, 11)), // Orange
        LeftoverType.File => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 59, 130, 246)),      // Blue
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 139, 92, 246))                      // Purple
    };

    public Microsoft.UI.Xaml.Media.Brush IconForeground => Type switch
    {
        LeftoverType.Directory => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
        LeftoverType.File => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246))
    };
}

public class SettingsProfile
{
    public string Theme { get; set; } = "Dark";
    public bool AutoScan { get; set; }
    public string ReportFormat { get; set; } = "TXT";
    
    // General & Update policy
    public int LanguageIndex { get; set; } = 0; // 0=English, 1=Tiếng Việt
    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = false; // Auto download & install in background
    public bool MinimizeToTray { get; set; } = true;
    public bool BetaUpdates { get; set; } = false; // Check for beta builds

    // Appearance
    public string AccentColor { get; set; } = "Default"; // Default, Green, Purple, Pink, Amber
    public double TransparencyLevel { get; set; } = 80.0;
    public bool EnableAnimations { get; set; } = true;

    // Auto Maintenance
    public double AutoCleanupTriggerSizeGB { get; set; } = 5.0;
    public bool TriggerSmartBoost { get; set; } = true;
    public int MaintenanceFrequencyIndex { get; set; } = 1; // 0=Daily, 1=Weekly, 2=Monthly

    // Telemetry Diagnostics
    public int TelemetryIntervalIndex { get; set; } = 1; // 0=0.5s, 1=1.0s, 2=2.0s, 3=5.0s
    public int PerformanceHistoryDurationIndex { get; set; } = 0; // 0=7 Days, 1=30 Days, 2=90 Days
    public bool EnableSensorsThread { get; set; } = true;

    // Safety & Data Security
    public bool CreateRestorePoint { get; set; } = true;
    public bool BackupRegistryHive { get; set; } = true;
    public double ConfirmationAlertsLevel { get; set; } = 2.0;

    // Advanced Developer
    public bool EnableVerboseLogs { get; set; } = false;
    public bool EnableExperimentalAi { get; set; } = false;

    // Notifications Settings
    public bool ShowNotifications { get; set; } = true;
    public double NotificationThreshold { get; set; } = 10.0; // Show notification if health score drops by more than this
    public bool NotifyOnLowHealth { get; set; } = true;
    public bool NotifyOnMaintenance { get; set; } = true;
    public bool ShowUpdateNotifications { get; set; } = true;
    public bool NotificationSound { get; set; } = true;
}

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Info"; // Info, Warning, Critical
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}

public class CpuTemperatureInfo
{
    public double TemperatureCelsius { get; set; }
    public string SensorName { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class NetworkAdapterInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string Type { get; set; } = "";
    public string Speed { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string IpAddresses { get; set; } = "";
    public string StatusColor => Status == "Up" ? "MediumSeaGreen" : "Tomato";
    public string StatusGlyph => Status == "Up" ? "\uE73E" : "\uF140";

    // New optimized telemetry fields
    public string CurrentDnsServers { get; set; } = "";
    public double LatencyMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }
    public string GatewayAddress { get; set; } = "";
    public string AdapterSpeed { get; set; } = "";
    public string IPv6Address { get; set; } = "";
    public string PublicIPAddress { get; set; } = "";
}

public class DnsServerInfo
{
    public string Name { get; set; } = "";
    public string PrimaryIp { get; set; } = "";
    public string SecondaryIp { get; set; } = "";
    public double PingMs { get; set; } = -1;
    public bool IsFastest { get; set; }
    public string PingFormatted => PingMs < 0 ? "Timeout".T() : $"{PingMs:F0} ms";

    // New optimized DNS benchmark fields
    public double AverageQueryMs { get; set; } = -1;
    public double MinQueryMs { get; set; } = -1;
    public double MaxQueryMs { get; set; } = -1;
    public double PacketLossPercent { get; set; } = 0;
    public double ReliabilityScore { get; set; } = 100;
    public DateTime LastBenchmarkTime { get; set; } = DateTime.Now;
}

public class SpeedTestResult
{
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public double PingMs { get; set; }
    public double JitterMs { get; set; }
    public string ServerName { get; set; } = "";
    public double TestDuration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ActiveConnectionInfo
{
    public string Protocol { get; set; } = "";
    public string LocalAddress { get; set; } = "";
    public string ForeignAddress { get; set; } = "";
    public string State { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public int Pid { get; set; }
}

public enum CleaningAction
{
    CloseAndClean,
    CleanAnyway,
    ScheduleAfterRestart,
    Cancel
}

public class LockingAppInfo
{
    public string Name { get; set; } = "";
    public int ProcessCount { get; set; }
    public long LockedSizeBytes { get; set; }
    public string LockedSizeFormatted => FormatSize(LockedSizeBytes);
    public System.Collections.Generic.List<int> ProcessIds { get; set; } = new();
    
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);
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

    private static string FormatSize(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {suffix[i]}";
    }
}






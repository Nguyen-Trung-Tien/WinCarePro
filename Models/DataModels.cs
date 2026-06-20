using System;

namespace WinCarePro.Models;

public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => $"{Name} ({Id})";
    public double CpuUsage { get; set; }
    public long RamUsageBytes { get; set; }
    public string RamUsageFormatted => FormatSize(RamUsageBytes);
    public double DiskUsageMb { get; set; }
    public string DiskUsageFormatted => DiskUsageMb > 0.1 ? $"{DiskUsageMb:F1} MB/s" : "0 MB/s";
    public double NetworkUsageKb { get; set; }
    public string NetworkUsageFormatted => NetworkUsageKb > 0.1 ? $"{NetworkUsageKb:F1} KB/s" : "0 KB/s";
    public string FilePath { get; set; } = "";
    public string Publisher { get; set; } = "Unknown Publisher";

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
    ThumbnailCache
}

public class JunkCategory
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public JunkType Type { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted => FormatSize(SizeBytes);
    public bool IsSelected { get; set; } = true;
    public int FileCount { get; set; }

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
            }
        }
    }
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

public class StartupEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Path { get; set; } = "";
    public StartupSource Source { get; set; }
    public string SourceFormatted => Source.ToString();
    public bool IsEnabled { get; set; } = true;
}

public class ServiceEntry
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Status { get; set; } = ""; // Running, Stopped, Paused, etc.
    public string StartupType { get; set; } = ""; // Automatic, Manual, Disabled
    public bool CanStop { get; set; }
}

public class ScheduledTaskEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Action { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
}

public class DriverInfo
{
    public string Name { get; set; } = "";
    public string DeviceClass { get; set; } = "";
    public string Provider { get; set; } = "";
    public string DriverVersion { get; set; } = "";
    public string DriverDate { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasUpdate { get; set; }
    public string AvailableVersion { get; set; } = "";
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
            }
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
}




using System;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => $"{Name} ({Id})";
    public double CpuUsage { get; set; }
    public string CpuUsageFormatted => $"{CpuUsage:F1}%";
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
    public string IconGlyph => "\uE7C3"; // Document/File icon
    public string IconColor => "#FFF59E0B"; // Amber color for files

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

public class StartupEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Path { get; set; } = "";
    public StartupSource Source { get; set; }
    public string SourceFormatted => Source.ToString();
    public bool IsEnabled { get; set; } = true;
    public int StartupDelayMs { get; set; }
    public string Impact => StartupDelayMs switch
    {
        < 150 => "Low",
        < 500 => "Medium",
        < 2000 => "High",
        _ => "Critical"
    };
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
                OnPropertyChanged(nameof(StatusFormatted));
            }
        }
    }

    public string StatusFormatted => IsOptimized ? "Optimized" : "Available";

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
}

public class DnsServerInfo
{
    public string Name { get; set; } = "";
    public string PrimaryIp { get; set; } = "";
    public string SecondaryIp { get; set; } = "";
    public double PingMs { get; set; } = -1;
    public bool IsFastest { get; set; }
    public string PingFormatted => PingMs < 0 ? "Timeout".T() : $"{PingMs:F0} ms";
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





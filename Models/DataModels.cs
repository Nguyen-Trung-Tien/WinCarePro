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

public class SoftwareUpdateInfo
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string InstalledVersion { get; set; } = "";
    public string AvailableVersion { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public string Source { get; set; } = "winget";
    public string UpdateStatus { get; set; } = "Available"; // Available, Updating, Completed, Failed
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

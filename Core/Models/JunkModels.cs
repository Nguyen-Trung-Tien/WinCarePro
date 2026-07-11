using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinCarePro.Services;


namespace WinCarePro.Models;

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
    
    public Brush StatusBgColor => IsLocked 
        ? new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)) 
        : new SolidColorBrush(Color.FromArgb(30, 16, 185, 129));

    public Brush StatusForegroundColor => IsLocked 
        ? new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)) 
        : new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));

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
    public Visibility LockedSizeVisibility => LockedBytes > 0 ? Visibility.Visible : Visibility.Collapsed;

    public bool IsSelected { get; set; } = true;
    public int FileCount { get; set; }
    public string FileCountFormatted => $"{FileCount} files";
    
    public string IconGlyph { get; set; } = "\uEA99";
    public string IconColor { get; set; } = "#FF7F56D9";
    public string FolderPath { get; set; } = "";
    public List<JunkFileItem> TopFiles { get; set; } = new();

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

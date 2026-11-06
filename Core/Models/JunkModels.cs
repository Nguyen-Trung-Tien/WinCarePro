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
    CrashDumps,
    DeveloperCache
}

public enum JunkSafetyTier
{
    Safe,
    Review,
    Developer
}

public class JunkFileItem
{
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(SizeBytes);
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
}

public class JunkCategory
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public JunkType Type { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(SizeBytes);
    
    public long CleanableBytes { get; set; }
    public long LockedBytes { get; set; }
    public string CleanableSizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(CleanableBytes);
    public string LockedSizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(LockedBytes);
    public Visibility LockedSizeVisibility => LockedBytes > 0 ? Visibility.Visible : Visibility.Collapsed;

    public bool IsSelected { get; set; } = true;
    public int FileCount { get; set; }
    public string FileCountFormatted => $"{FileCount} files";
    
    public JunkSafetyTier SafetyTier => Type switch
    {
        JunkType.WindowsTemp => JunkSafetyTier.Safe,
        JunkType.UserTemp => JunkSafetyTier.Safe,
        JunkType.ThumbnailCache => JunkSafetyTier.Safe,
        JunkType.Prefetch => JunkSafetyTier.Safe,
        JunkType.DeliveryOptimization => JunkSafetyTier.Safe,
        JunkType.UpdateCache => JunkSafetyTier.Safe,
        JunkType.BrowserCache => JunkSafetyTier.Review,
        JunkType.RecycleBin => JunkSafetyTier.Review,
        JunkType.CrashDumps => JunkSafetyTier.Review,
        JunkType.SystemLog => JunkSafetyTier.Review,
        JunkType.DeveloperCache => JunkSafetyTier.Developer,
        JunkType.ShaderCache => JunkSafetyTier.Developer,
        _ => JunkSafetyTier.Safe
    };

    public string SafetyTierText => SafetyTier switch
    {
        JunkSafetyTier.Safe => "100% Safe".T(),
        JunkSafetyTier.Review => "Review Recommended".T(),
        JunkSafetyTier.Developer => "Developer & Deep".T(),
        _ => "Safe".T()
    };

    public string SafetyTierColor => SafetyTier switch
    {
        JunkSafetyTier.Safe => "#FF10B981",
        JunkSafetyTier.Review => "#FFF59E0B",
        JunkSafetyTier.Developer => "#FFA78BFA",
        _ => "#FF10B981"
    };

    public string SafetyTierBgColor => SafetyTier switch
    {
        JunkSafetyTier.Safe => "#2010B981",
        JunkSafetyTier.Review => "#20F59E0B",
        JunkSafetyTier.Developer => "#208B5CF6",
        _ => "#2010B981"
    };

    public string SafetyTierGlyph => SafetyTier switch
    {
        JunkSafetyTier.Safe => "\uE73E",
        JunkSafetyTier.Review => "\uE7BA",
        JunkSafetyTier.Developer => "\uE943",
        _ => "\uE73E"
    };

    public string IconGlyph { get; set; } = "\uEA99";
    public string IconColor { get; set; } = "#FF7F56D9";
    public string FolderPath { get; set; } = "";
    public List<JunkFileItem> TopFiles { get; set; } = new();
}

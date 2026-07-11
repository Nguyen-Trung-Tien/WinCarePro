using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace WinCarePro.Models;

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
    public List<int> ProcessIds { get; set; } = new();
    
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);
    public ImageSource? IconImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath) || !System.IO.File.Exists(IconPath)) return null;
            try
            {
                return new BitmapImage(new Uri(IconPath));
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

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
    public string LockedSizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(LockedSizeBytes);
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
}

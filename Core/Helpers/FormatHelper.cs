namespace WinCarePro.Core.Helpers;

/// <summary>
/// Centralized formatting utilities shared across all modules.
/// Eliminates duplication of FormatBytes/FormatSize found in 8+ files.
/// </summary>
public static class FormatHelper
{
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>
    /// Formats a byte count into a human-readable string (e.g. "1.5 GB").
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";

        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < SizeSuffixes.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {SizeSuffixes[i]}";
    }

    /// <summary>
    /// Formats a duration in seconds into a human-readable string (e.g. "2d 5h 30m").
    /// </summary>
    public static string FormatDuration(double totalSeconds)
    {
        var span = System.TimeSpan.FromSeconds(totalSeconds);
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
        if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m";
        return $"{span.Minutes}m {span.Seconds}s";
    }
}

using System;

namespace WinCarePro.Database;

public class StateSnapshotEntry
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string KeyName { get; set; } = "";
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportEntry
{
    public int Id { get; set; }
    public string ReportName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class LogEntry
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public string DisplayAction => Services.TranslationManager.Instance.T(Action);
    public string DisplayModule => Services.TranslationManager.Instance.T(Module);
    public string DisplayStatus => Services.TranslationManager.Instance.T(Status);

    public Microsoft.UI.Xaml.Media.Brush StatusBrush => GetStatusBrush(Status);
    public Microsoft.UI.Xaml.Media.Brush StatusTintBrush => GetStatusTintBrush(Status);
    public string StatusGlyph => GetStatusGlyph(Status);

    public string RelativeTimeAgo
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "Just Now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return CreatedAt.ToString("MMM dd");
        }
    }

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SuccessBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush WarningBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush ErrorBrush = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush InfoBrush = new(Windows.UI.Color.FromArgb(255, 139, 92, 246));

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush SuccessTint = new(Windows.UI.Color.FromArgb(38, 16, 185, 129));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush WarningTint = new(Windows.UI.Color.FromArgb(38, 245, 158, 11));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush ErrorTint = new(Windows.UI.Color.FromArgb(38, 239, 68, 68));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush InfoTint = new(Windows.UI.Color.FromArgb(38, 139, 92, 246));

    public static Microsoft.UI.Xaml.Media.SolidColorBrush GetStatusBrush(string? status)
    {
        if (string.IsNullOrEmpty(status)) return InfoBrush;
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return ErrorBrush;
        if (lower.Contains("warn") || lower.Contains("skip"))
            return WarningBrush;
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return SuccessBrush;
        return InfoBrush;
    }

    public static Microsoft.UI.Xaml.Media.SolidColorBrush GetStatusTintBrush(string? status)
    {
        if (string.IsNullOrEmpty(status)) return InfoTint;
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return ErrorTint;
        if (lower.Contains("warn") || lower.Contains("skip"))
            return WarningTint;
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return SuccessTint;
        return InfoTint;
    }

    public static string GetStatusGlyph(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "\uE946";
        string lower = status.ToLower();
        if (lower.Contains("fail") || lower.Contains("err") || lower.Contains("crit"))
            return "\uEA39";
        if (lower.Contains("warn") || lower.Contains("skip"))
            return "\uE7BA";
        if (lower.Contains("succ") || lower.Contains("ok") || lower.Contains("done") || lower.Contains("clean"))
            return "\uE73E";
        return "\uE946";
    }
}

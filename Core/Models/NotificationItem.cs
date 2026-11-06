using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Models;

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Info"; // Info, Warning, Critical
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
    public bool ShowToast { get; set; } = true;
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public Visibility UnreadVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;

    public Brush LevelBrush => GetLevelBrush(Level);
    public Brush LevelTintBrush => GetLevelTintBrush(Level);
    public Brush LevelColor => LevelBrush;
    public Brush LevelBadgeBg => LevelTintBrush;
    public string LevelGlyph => GetLevelGlyph(Level);
    public string Source => ExtractSource(Title, Message);

    public string? QuickActionType => DetermineQuickActionType(Title, Message);
    public string? QuickActionText => DetermineQuickActionText(QuickActionType);
    public string QuickActionGlyph => DetermineQuickActionGlyph(QuickActionType);
    public bool HasQuickAction => !string.IsNullOrEmpty(QuickActionType);
    public Visibility QuickActionVisibility => HasQuickAction ? Visibility.Visible : Visibility.Collapsed;

    private static string ExtractSource(string title, string message)
    {
        string text = $"{title} {message}".ToLower();
        if (text.Contains("ram") || text.Contains("memory") || text.Contains("bộ nhớ")) return "Memory Sentinel";
        if (text.Contains("junk") || text.Contains("disk") || text.Contains("rác") || text.Contains("ổ đĩa")) return "Storage Sentinel";
        if (text.Contains("registry")) return "Registry Sentinel";
        if (text.Contains("startup") || text.Contains("khởi động")) return "Startup Sentinel";
        if (text.Contains("network") || text.Contains("dns") || text.Contains("mạng")) return "Network Center";
        if (text.Contains("security") || text.Contains("shield") || text.Contains("bảo mật") || text.Contains("virus")) return "Security Shield";
        if (text.Contains("update") || text.Contains("cập nhật")) return "Update Sentinel";
        if (text.Contains("repair") || text.Contains("sửa lỗi")) return "System Repair";
        return "System Core";
    }

    private static string? DetermineQuickActionType(string title, string message)
    {
        string text = $"{title} {message}".ToLower();
        if (text.Contains("ram") || text.Contains("memory") || text.Contains("bộ nhớ")) return "RAM";
        if (text.Contains("junk") || text.Contains("disk") || text.Contains("rác") || text.Contains("tệp thừa")) return "Junk";
        if (text.Contains("registry")) return "Registry";
        if (text.Contains("startup") || text.Contains("khởi động")) return "Startup";
        if (text.Contains("security") || text.Contains("shield") || text.Contains("bảo mật")) return "Security";
        if (text.Contains("network") || text.Contains("dns") || text.Contains("mạng")) return "Network";
        return null;
    }

    private static string? DetermineQuickActionText(string? actionType)
    {
        return actionType switch
        {
            "RAM" => "Optimize RAM",
            "Junk" => "Clean Junk",
            "Registry" => "Scan Registry",
            "Startup" => "Review Apps",
            "Security" => "Shield Check",
            "Network" => "Check Network",
            _ => null
        };
    }

    private static string DetermineQuickActionGlyph(string? actionType)
    {
        return actionType switch
        {
            "RAM" => "\uE7F1",
            "Junk" => "\uE74D",
            "Registry" => "\uE81E",
            "Startup" => "\uE7E8",
            "Security" => "\uEA1F",
            "Network" => "\uE839",
            _ => "\uE946"
        };
    }

    private static readonly SolidColorBrush GreenBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129));
    private static readonly SolidColorBrush AmberBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush RedBrush = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
    private static readonly SolidColorBrush PurpleBrush = new(Windows.UI.Color.FromArgb(255, 139, 92, 246));

    private static readonly SolidColorBrush GreenTint = new(Windows.UI.Color.FromArgb(38, 16, 185, 129));
    private static readonly SolidColorBrush AmberTint = new(Windows.UI.Color.FromArgb(38, 245, 158, 11));
    private static readonly SolidColorBrush RedTint = new(Windows.UI.Color.FromArgb(38, 239, 68, 68));
    private static readonly SolidColorBrush PurpleTint = new(Windows.UI.Color.FromArgb(38, 139, 92, 246));

    public static SolidColorBrush GetLevelBrush(string? level)
    {
        if (string.IsNullOrEmpty(level)) return PurpleBrush;
        string lower = level.ToLower();
        if (lower.Contains("crit") || lower.Contains("err") || lower.Contains("fail"))
            return RedBrush;
        if (lower.Contains("warn") || lower.Contains("cảnh báo"))
            return AmberBrush;
        if (lower.Contains("succ") || lower.Contains("thành công"))
            return GreenBrush;
        return PurpleBrush;
    }

    public static SolidColorBrush GetLevelTintBrush(string? level)
    {
        if (string.IsNullOrEmpty(level)) return PurpleTint;
        string lower = level.ToLower();
        if (lower.Contains("crit") || lower.Contains("err") || lower.Contains("fail"))
            return RedTint;
        if (lower.Contains("warn") || lower.Contains("cảnh báo"))
            return AmberTint;
        if (lower.Contains("succ") || lower.Contains("thành công"))
            return GreenTint;
        return PurpleTint;
    }

    public static string GetLevelGlyph(string? level)
    {
        if (string.IsNullOrEmpty(level)) return "\uE946";
        string lower = level.ToLower();
        if (lower.Contains("crit") || lower.Contains("err") || lower.Contains("fail"))
            return "\uEA39";
        if (lower.Contains("warn") || lower.Contains("cảnh báo"))
            return "\uE7BA";
        if (lower.Contains("succ") || lower.Contains("thành công"))
            return "\uE73E";
        return "\uE946";
    }

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

    public string TimeAgoGroup
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 60) return "Just Now";
            if (diff.TotalHours < 24) return "Today";
            if (diff.TotalDays < 7) return "This Week";
            return "Older";
        }
    }
}

public class NotificationGroup : List<NotificationItem>
{
    public string Name { get; }
    public NotificationGroup(string name, List<NotificationItem> items) : base(items)
    {
        Name = name;
    }
}

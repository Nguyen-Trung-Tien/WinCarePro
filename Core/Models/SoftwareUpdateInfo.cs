using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class SoftwareUpdateInfo : INotifyPropertyChanged
{
    // Status constants — always use these for logic comparison, never translated strings
    public const string StatusAvailable = "Available";
    public const string StatusUpdating = "Updating";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

    private double _downloadProgress = 0;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (Math.Abs(_downloadProgress - value) > 0.01)
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }
    }

    private bool _isIndeterminate = true;
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set
        {
            if (_isIndeterminate != value)
            {
                _isIndeterminate = value;
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }
    }

    private string _progressText = "Updating...";
    public string ProgressText
    {
        get => _progressText;
        set
        {
            if (_progressText != value)
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    private string _updateStatus = StatusAvailable;
    public string UpdateStatus // Always store English constants: Available, Updating, Completed, Failed
    {
        get => _updateStatus;
        set
        {
            if (_updateStatus != value)
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
                OnPropertyChanged(nameof(UpdateStatusDisplay));
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

    /// <summary>Translated display string for UI binding</summary>
    public string UpdateStatusDisplay => UpdateStatus.T();

    public bool IsUpdating => UpdateStatus == StatusUpdating;
    public bool IsNotUpdating => UpdateStatus != StatusUpdating;
    public bool CanUpdate => UpdateStatus != StatusCompleted && UpdateStatus != StatusUpdating;

    public Visibility IsUpdatingVisibility => IsUpdating ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsNotUpdatingVisibility => IsNotUpdating ? Visibility.Visible : Visibility.Collapsed;

    public Brush StatusBgColor => UpdateStatus switch
    {
        StatusCompleted => new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)),  // #1E10B981
        StatusFailed => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),    // #1EEF4444
        StatusUpdating => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)), // #1EF59E0B
        _ => new SolidColorBrush(Color.FromArgb(20, 59, 130, 246))           // #143B82F6
    };

    public Brush StatusBorderColor => UpdateStatus switch
    {
        StatusCompleted => new SolidColorBrush(Color.FromArgb(48, 16, 185, 129)),
        StatusFailed => new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)),
        StatusUpdating => new SolidColorBrush(Color.FromArgb(48, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(32, 59, 130, 246))
    };

    public Brush StatusForegroundColor => UpdateStatus switch
    {
        StatusCompleted => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
        StatusFailed => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
        StatusUpdating => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(255, 59, 130, 246))
    };

    // App Branding & Icon Properties
    public string IconGlyph
    {
        get
        {
            var id = Id.ToLowerInvariant();
            var name = Name.ToLowerInvariant();

            if (id.Contains("edge") || name.Contains("edge")) return "\uE774"; // Globe / Web
            if (id.Contains("chrome") || name.Contains("chrome")) return "\uE774";
            if (id.Contains("firefox") || name.Contains("firefox")) return "\uE774";
            if (id.Contains("code") || id.Contains("visualstudio") || name.Contains("visual studio")) return "\uE943"; // Code
            if (id.Contains("git") || name.Contains("git")) return "\uEAF3"; // Git / Source Control
            if (id.Contains("node") || name.Contains("node")) return "\uE756"; // Terminal / Script
            if (id.Contains("python") || name.Contains("python")) return "\uE756";
            if (id.Contains("vlc") || name.Contains("vlc")) return "\uE714"; // Media / Play
            if (id.Contains("7zip") || id.Contains("winrar") || name.Contains("zip") || name.Contains("rar")) return "\uE8B7"; // Zip / Archive
            if (id.Contains("notepad") || name.Contains("notepad")) return "\uE8C8"; // Document Editor
            if (id.Contains("discord") || id.Contains("slack") || id.Contains("teams")) return "\uE8BD"; // Chat

            return "\uE74C"; // Default App Cube / Package
        }
    }

    public string BrandColorHex
    {
        get
        {
            var id = Id.ToLowerInvariant();
            var name = Name.ToLowerInvariant();

            if (id.Contains("edge") || name.Contains("edge")) return "#0078D4";       // Microsoft Edge Blue
            if (id.Contains("chrome") || name.Contains("chrome")) return "#EA4335";   // Google Red
            if (id.Contains("firefox") || name.Contains("firefox")) return "#FF7139"; // Firefox Orange
            if (id.Contains("code") || id.Contains("visualstudio")) return "#007ACC"; // VS Code Blue
            if (id.Contains("git") || name.Contains("git")) return "#F05032";         // Git Orange
            if (id.Contains("node") || name.Contains("node")) return "#339933";       // Node Green
            if (id.Contains("python") || name.Contains("python")) return "#3776AB";   // Python Blue
            if (id.Contains("vlc") || name.Contains("vlc")) return "#FF8800";         // VLC Orange
            if (id.Contains("7zip") || id.Contains("winrar")) return "#10B981";       // Green Archive
            if (id.Contains("notepad") || name.Contains("notepad")) return "#90B44C"; // Notepad++ Green
            if (id.Contains("discord") || id.Contains("slack")) return "#5865F2";     // Discord Blurple

            return "#3B82F6"; // Default Accent Blue
        }
    }

    public Brush BrandColorBrush
    {
        get
        {
            var color = ColorFromHex(BrandColorHex);
            return new SolidColorBrush(color);
        }
    }

    public Brush BrandBgBrush
    {
        get
        {
            var color = ColorFromHex(BrandColorHex);
            return new SolidColorBrush(Color.FromArgb(38, color.R, color.G, color.B)); // 15% opacity tint
        }
    }

    public Brush BrandBorderBrush
    {
        get
        {
            var color = ColorFromHex(BrandColorHex);
            return new SolidColorBrush(Color.FromArgb(76, color.R, color.G, color.B)); // 30% opacity border
        }
    }

    public string AppInitials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "APP";
            var parts = Name.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpperInvariant() : parts[0].ToUpperInvariant();
            }
            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant();
        }
    }

    private static Color ColorFromHex(string hex)
    {
        try
        {
            hex = hex.Replace("#", "");
            if (hex.Length == 6)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch {}
        return Color.FromArgb(255, 59, 130, 246);
    }
}


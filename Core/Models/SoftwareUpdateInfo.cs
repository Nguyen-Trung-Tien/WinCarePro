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

    private string _downloadUrl = "";
    public string DownloadUrl
    {
        get => _downloadUrl;
        set
        {
            if (_downloadUrl != value)
            {
                _downloadUrl = value;
                OnPropertyChanged(nameof(DownloadUrl));
                OnPropertyChanged(nameof(HasDownloadUrl));
            }
        }
    }

    public bool HasDownloadUrl => !string.IsNullOrWhiteSpace(_downloadUrl);

    private string _bytesProgress = "";
    public string BytesProgress
    {
        get => _bytesProgress;
        set
        {
            if (_bytesProgress != value)
            {
                _bytesProgress = value;
                OnPropertyChanged(nameof(BytesProgress));
                OnPropertyChanged(nameof(HasBytesProgress));
            }
        }
    }

    public bool HasBytesProgress => !string.IsNullOrWhiteSpace(_bytesProgress);

    private string _speedText = "";
    public string SpeedText
    {
        get => _speedText;
        set
        {
            if (_speedText != value)
            {
                _speedText = value;
                OnPropertyChanged(nameof(SpeedText));
            }
        }
    }

    private string _currentPhase = "Preparing";
    public string CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase != value)
            {
                _currentPhase = value;
                OnPropertyChanged(nameof(CurrentPhase));
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
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(CanUpdate));
                OnPropertyChanged(nameof(CanRetry));
                OnPropertyChanged(nameof(IsUpdatingVisibility));
                OnPropertyChanged(nameof(IsNotUpdatingVisibility));
                OnPropertyChanged(nameof(StatusBgColor));
                OnPropertyChanged(nameof(StatusBorderColor));
                OnPropertyChanged(nameof(StatusForegroundColor));
                OnPropertyChanged(nameof(CardBackgroundBrush));
                OnPropertyChanged(nameof(CardBorderBrush));
                OnPropertyChanged(nameof(StatusIconGlyph));
                OnPropertyChanged(nameof(CurrentInstalledVersionDisplay));
                OnPropertyChanged(nameof(ActionBtnText));
                OnPropertyChanged(nameof(IsActionBtnEnabled));
            }
        }
    }

    /// <summary>Translated display string for UI binding</summary>
    public string UpdateStatusDisplay => UpdateStatus.T();

    public bool IsUpdating => UpdateStatus == StatusUpdating;
    public bool IsNotUpdating => UpdateStatus != StatusUpdating;
    public bool IsCompleted => UpdateStatus == StatusCompleted;
    public bool IsFailed => UpdateStatus == StatusFailed;
    public bool IsAvailable => UpdateStatus == StatusAvailable;
    public bool CanUpdate => UpdateStatus != StatusCompleted && UpdateStatus != StatusUpdating;
    public bool CanRetry => UpdateStatus == StatusFailed;
    public bool IsActionBtnEnabled => UpdateStatus == StatusAvailable || UpdateStatus == StatusFailed;

    public Visibility IsUpdatingVisibility => IsUpdating ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsNotUpdatingVisibility => IsNotUpdating ? Visibility.Visible : Visibility.Collapsed;

    public string CurrentInstalledVersionDisplay => IsCompleted ? AvailableVersion : InstalledVersion;

    public string ActionBtnText => UpdateStatus switch
    {
        StatusCompleted => "Up to Date ✓".T(),
        StatusUpdating => "Updating...".T(),
        StatusFailed => "Retry ↻".T(),
        _ => "Update".T()
    };

    public string StatusIconGlyph => UpdateStatus switch
    {
        StatusCompleted => "\uE73E", // Checkmark
        StatusUpdating => "\uE896",  // Progress Sync
        StatusFailed => "\uEA39",    // Error / Warning
        _ => "\uE895"                // Download
    };

    private static SolidColorBrush? _cardBgCompleted;
    private static SolidColorBrush? _cardBgUpdating;
    private static SolidColorBrush? _cardBgFailed;
    private static SolidColorBrush? _cardBgDefault;

    private static SolidColorBrush? _cardBorderCompleted;
    private static SolidColorBrush? _cardBorderUpdating;
    private static SolidColorBrush? _cardBorderFailed;
    private static SolidColorBrush? _cardBorderDefault;

    private static SolidColorBrush? _statusBgCompleted;
    private static SolidColorBrush? _statusBgFailed;
    private static SolidColorBrush? _statusBgUpdating;
    private static SolidColorBrush? _statusBgDefault;

    private static SolidColorBrush? _statusBorderCompleted;
    private static SolidColorBrush? _statusBorderFailed;
    private static SolidColorBrush? _statusBorderUpdating;
    private static SolidColorBrush? _statusBorderDefault;

    private static SolidColorBrush? _statusFgCompleted;
    private static SolidColorBrush? _statusFgFailed;
    private static SolidColorBrush? _statusFgUpdating;
    private static SolidColorBrush? _statusFgDefault;

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? CardBackgroundBrush
    {
        get
        {
            try
            {
                return UpdateStatus switch
                {
                    StatusCompleted => _cardBgCompleted ??= new SolidColorBrush(Color.FromArgb(18, 16, 185, 129)),
                    StatusUpdating => _cardBgUpdating ??= new SolidColorBrush(Color.FromArgb(22, 245, 158, 11)),
                    StatusFailed => _cardBgFailed ??= new SolidColorBrush(Color.FromArgb(22, 239, 68, 68)),
                    _ => _cardBgDefault ??= new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
                };
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? CardBorderBrush
    {
        get
        {
            try
            {
                return UpdateStatus switch
                {
                    StatusCompleted => _cardBorderCompleted ??= new SolidColorBrush(Color.FromArgb(70, 16, 185, 129)),
                    StatusUpdating => _cardBorderUpdating ??= new SolidColorBrush(Color.FromArgb(120, 245, 158, 11)),
                    StatusFailed => _cardBorderFailed ??= new SolidColorBrush(Color.FromArgb(100, 239, 68, 68)),
                    _ => _cardBorderDefault ??= new SolidColorBrush(Color.FromArgb(32, 128, 128, 128))
                };
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusBgColor
    {
        get
        {
            try
            {
                return UpdateStatus switch
                {
                    StatusCompleted => _statusBgCompleted ??= new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)),
                    StatusFailed => _statusBgFailed ??= new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
                    StatusUpdating => _statusBgUpdating ??= new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
                    _ => _statusBgDefault ??= new SolidColorBrush(Color.FromArgb(20, 59, 130, 246))
                };
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusBorderColor
    {
        get
        {
            try
            {
                return UpdateStatus switch
                {
                    StatusCompleted => _statusBorderCompleted ??= new SolidColorBrush(Color.FromArgb(48, 16, 185, 129)),
                    StatusFailed => _statusBorderFailed ??= new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)),
                    StatusUpdating => _statusBorderUpdating ??= new SolidColorBrush(Color.FromArgb(48, 245, 158, 11)),
                    _ => _statusBorderDefault ??= new SolidColorBrush(Color.FromArgb(32, 59, 130, 246))
                };
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusForegroundColor
    {
        get
        {
            try
            {
                return UpdateStatus switch
                {
                    StatusCompleted => _statusFgCompleted ??= new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
                    StatusFailed => _statusFgFailed ??= new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                    StatusUpdating => _statusFgUpdating ??= new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
                    _ => _statusFgDefault ??= new SolidColorBrush(Color.FromArgb(255, 59, 130, 246))
                };
            }
            catch { return null; }
        }
    }

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
            if (id.Contains("discord") || id.Contains("slack") || id.Contains("telegram")) return "\uE8BD"; // Chat
            if (id.Contains("spotify") || id.Contains("music") || id.Contains("vlc")) return "\uE8D6"; // Media
            if (id.Contains("git") || id.Contains("node") || id.Contains("python")) return "\uE756"; // Developer Tool
            if (id.Contains("7zip") || id.Contains("winrar") || id.Contains("zip")) return "\uF012"; // Archive

            return "\uE71D"; // Default Generic Tool
        }
    }

    public string BrandColorHex
    {
        get
        {
            var id = Id.ToLowerInvariant();
            var name = Name.ToLowerInvariant();

            if (id.Contains("edge")) return "#0078D7";                                // Edge Blue
            if (id.Contains("chrome") || name.Contains("chrome")) return "#EA4335";     // Chrome Red/Yellow
            if (id.Contains("firefox")) return "#FF7139";                             // Firefox Orange
            if (id.Contains("code") || id.Contains("vscode")) return "#007ACC";       // VS Code Blue
            if (id.Contains("visualstudio")) return "#5C2D91";                        // Visual Studio Purple
            if (id.Contains("spotify")) return "#1DB954";                             // Spotify Green
            if (id.Contains("discord") || id.Contains("slack")) return "#5865F2";     // Discord Blurple

            return "#3B82F6"; // Default Accent Blue
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? BrandColorBrush
    {
        get
        {
            try
            {
                var color = ColorFromHex(BrandColorHex);
                return new SolidColorBrush(color);
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? BrandBgBrush
    {
        get
        {
            try
            {
                var color = ColorFromHex(BrandColorHex);
                return new SolidColorBrush(Color.FromArgb(38, color.R, color.G, color.B)); // 15% opacity tint
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? BrandBorderBrush
    {
        get
        {
            try
            {
                var color = ColorFromHex(BrandColorHex);
                return new SolidColorBrush(Color.FromArgb(76, color.R, color.G, color.B)); // 30% opacity border
            }
            catch { return null; }
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


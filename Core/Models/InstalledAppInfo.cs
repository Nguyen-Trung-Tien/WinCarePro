using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace WinCarePro.Models;

public class InstalledAppInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool _isSelected;
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

    public string DisplayName { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Version { get; set; } = "";
    public string InstallDate { get; set; } = "";
    public string InstallLocation { get; set; } = "";
    public string UninstallString { get; set; } = "";
    public string RegistryKeyName { get; set; } = "";
    public string Hive { get; set; } = ""; // HKLM or HKCU
    public string RegistryPath { get; set; } = "";
    public string DisplayIcon { get; set; } = "";
    public bool IsStoreApp { get; set; } = false;
    public bool IsDesktopApp => !IsStoreApp;
    private string _iconPath = "";
    public string IconPath
    {
        get => _iconPath;
        set
        {
            if (_iconPath != value)
            {
                _iconPath = value;
                _cachedIconImageSource = null;
                OnPropertyChanged(nameof(IconPath));
                OnPropertyChanged(nameof(IconImageSource));
                OnPropertyChanged(nameof(HasIcon));
                OnPropertyChanged(nameof(IconVisibility));
                OnPropertyChanged(nameof(FallbackVisibility));
            }
        }
    }
    
    private ImageSource? _cachedIconImageSource;
    public ImageSource? IconImageSource
    {
        get
        {
            if (_cachedIconImageSource != null) return _cachedIconImageSource;
            if (string.IsNullOrWhiteSpace(_iconPath)) return null;
            try
            {
                if (Uri.TryCreate(_iconPath, UriKind.Absolute, out var uri))
                {
                    _cachedIconImageSource = new BitmapImage(uri);
                }
                else
                {
                    _cachedIconImageSource = new BitmapImage(new Uri(System.IO.Path.GetFullPath(_iconPath)));
                }
                return _cachedIconImageSource;
            }
            catch
            {
                return null;
            }
        }
    }
    
    public bool HasIcon => !string.IsNullOrWhiteSpace(_iconPath);

    public Visibility IconVisibility => HasIcon 
        ? Visibility.Visible 
        : Visibility.Collapsed;

    public Visibility FallbackVisibility => HasIcon 
        ? Visibility.Collapsed 
        : Visibility.Visible;

    private static SolidColorBrush? _storeBgBrush;
    private static SolidColorBrush? _desktopBgBrush;
    private static SolidColorBrush? _storeFgBrush;
    private static SolidColorBrush? _desktopFgBrush;

    public Brush IconBackground => IsStoreApp 
        ? (_storeBgBrush ??= new SolidColorBrush(Color.FromArgb(25, 0, 193, 238)))
        : (_desktopBgBrush ??= new SolidColorBrush(Color.FromArgb(25, 127, 86, 217)));

    public Brush IconForeground => IsStoreApp 
        ? (_storeFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 0, 193, 238)))
        : (_desktopFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 127, 86, 217)));

    public string DefaultIconGlyph => IsStoreApp ? "\uE719" : "\uE736";
    public string TypeBadgeText => IsStoreApp ? "Store App" : "Win32 Desktop";
    public string AppTypeDescription => IsStoreApp ? "Microsoft Store / UWP Package" : "Desktop Application (Win32 / Native)";

    private long _sizeBytes;
    public long SizeBytes
    {
        get => _sizeBytes;
        set
        {
            if (_sizeBytes != value)
            {
                _sizeBytes = value;
                OnPropertyChanged(nameof(SizeBytes));
                OnPropertyChanged(nameof(SizeFormatted));
                OnPropertyChanged(nameof(IsLargeApp));
            }
        }
    }

    public bool IsLargeApp => SizeBytes >= 500L * 1024 * 1024;

    public string SizeFormatted => SizeBytes <= 0 ? "Unknown" : WinCarePro.Core.Helpers.FormatHelper.FormatBytes(SizeBytes);
}

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
    public string IconPath { get; set; } = "";
    
    public ImageSource? IconImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IconPath)) return null;
            try
            {
                if (Uri.TryCreate(IconPath, UriKind.Absolute, out var uri))
                {
                    return new BitmapImage(uri);
                }
                else
                {
                    return new BitmapImage(new Uri(System.IO.Path.GetFullPath(IconPath)));
                }
            }
            catch
            {
                return null;
            }
        }
    }
    
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath);

    public Visibility IconVisibility => HasIcon 
        ? Visibility.Visible 
        : Visibility.Collapsed;

    public Visibility FallbackVisibility => HasIcon 
        ? Visibility.Collapsed 
        : Visibility.Visible;

    public Brush IconBackground => IsStoreApp 
        ? new SolidColorBrush(Color.FromArgb(25, 0, 193, 238)) 
        : new SolidColorBrush(Color.FromArgb(25, 127, 86, 217));

    public Brush IconForeground => IsStoreApp 
        ? new SolidColorBrush(Color.FromArgb(255, 0, 193, 238)) 
        : new SolidColorBrush(Color.FromArgb(255, 127, 86, 217));

    public string DefaultIconGlyph => IsStoreApp ? "\uE719" : "\uE736";

    public long SizeBytes { get; set; }
    public string SizeFormatted
    {
        get
        {
            if (SizeBytes <= 0) return "Unknown";
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double doubleBytes = SizeBytes;
            while (doubleBytes >= 1024 && i < suffix.Length - 1)
            {
                i++;
                doubleBytes /= 1024;
            }
            return $"{doubleBytes:F1} {suffix[i]}";
        }
    }
}

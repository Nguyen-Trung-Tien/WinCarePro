using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinCarePro.Models;

public enum LeftoverType
{
    File,
    Directory,
    RegistryKey,
    RegistryValue
}

public class LeftoverItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public LeftoverType Type { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted
    {
        get
        {
            if (Type == LeftoverType.RegistryKey || Type == LeftoverType.RegistryValue) return "N/A";
            if (SizeBytes <= 0) return "0 B";
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

    public string IconGlyph => Type switch
    {
        LeftoverType.Directory => "\uE8B7", // Folder icon
        LeftoverType.File => "\uE7C3",      // File icon
        _ => "\uE945"                       // Registry Key icon
    };

    public Brush IconBackground => Type switch
    {
        LeftoverType.Directory => new SolidColorBrush(Color.FromArgb(25, 245, 158, 11)), // Orange
        LeftoverType.File => new SolidColorBrush(Color.FromArgb(25, 59, 130, 246)),      // Blue
        _ => new SolidColorBrush(Color.FromArgb(25, 139, 92, 246))                      // Purple
    };

    public Brush IconForeground => Type switch
    {
        LeftoverType.Directory => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
        LeftoverType.File => new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
        _ => new SolidColorBrush(Color.FromArgb(255, 139, 92, 246))
    };
}

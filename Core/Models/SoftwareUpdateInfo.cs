using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinCarePro.Models;

public class SoftwareUpdateInfo : INotifyPropertyChanged
{
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

    private string _updateStatus = "Available";
    public string UpdateStatus // Available, Updating, Completed, Failed
    {
        get => _updateStatus;
        set
        {
            if (_updateStatus != value)
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
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

    public bool IsUpdating => UpdateStatus == "Updating...";
    public bool IsNotUpdating => UpdateStatus != "Updating...";
    public bool CanUpdate => UpdateStatus != "Completed" && UpdateStatus != "Updating...";

    public Visibility IsUpdatingVisibility => IsUpdating ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsNotUpdatingVisibility => IsNotUpdating ? Visibility.Visible : Visibility.Collapsed;

    public Brush StatusBgColor => UpdateStatus switch
    {
        "Completed" => new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)),  // #1E10B981
        "Failed" => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),    // #1EEF4444
        "Updating..." => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)), // #1EF59E0B
        _ => new SolidColorBrush(Color.FromArgb(20, 59, 130, 246))           // #143B82F6
    };

    public Brush StatusBorderColor => UpdateStatus switch
    {
        "Completed" => new SolidColorBrush(Color.FromArgb(48, 16, 185, 129)),
        "Failed" => new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)),
        "Updating..." => new SolidColorBrush(Color.FromArgb(48, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(32, 59, 130, 246))
    };

    public Brush StatusForegroundColor => UpdateStatus switch
    {
        "Completed" => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
        "Failed" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
        "Updating..." => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(255, 59, 130, 246))
    };
}

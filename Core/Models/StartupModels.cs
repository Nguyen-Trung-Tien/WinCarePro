using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace WinCarePro.Models;

public enum StartupSource
{
    RegistryRunHKCU,
    RegistryRunHKLM,
    RegistryRunWow64,
    StartupFolderUser,
    StartupFolderCommon,
    TaskScheduler
}

public class StartupEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Path { get; set; } = "";
    public StartupSource Source { get; set; }
    public string SourceFormatted => Source.ToString();

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public int StartupDelayMs { get; set; }
    public string Impact => StartupDelayMs switch
    {
        < 150 => "Low",
        < 500 => "Medium",
        < 2000 => "High",
        _ => "Critical"
    };

    // New Properties
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
    public string Publisher { get; set; } = "Unknown";

    private string _startupImpact = "Medium";
    public string StartupImpact
    {
        get => _startupImpact;
        set { if (_startupImpact != value) { _startupImpact = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImpactBgBrush)); OnPropertyChanged(nameof(ImpactFgBrush)); } }
    }

    public bool IsMicrosoft { get; set; }
    public bool IsSystemItem { get; set; }
    public int EstimatedLaunchTimeMs { get; set; }
    public bool IsRecommendedDisable { get; set; }

    // UI Helper properties
    public Brush ImpactBgBrush => StartupImpact switch
    {
        "Critical" => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
        "High" => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
        "Medium" => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(30, 16, 185, 129))
    };

    public Brush ImpactFgBrush => StartupImpact switch
    {
        "Critical" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
        "High" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
        "Medium" => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
        _ => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129))
    };
}

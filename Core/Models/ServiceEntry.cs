using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace WinCarePro.Models;

public class ServiceEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";

    private string _status = "";
    public string Status
    {
        get => _status;
        set 
        { 
            if (_status != value) 
            { 
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StatusBgBrush)); 
                OnPropertyChanged(nameof(StatusFgBrush)); 
                OnPropertyChanged(nameof(IsRunning)); 
                OnPropertyChanged(nameof(IsNotRunning)); 
            } 
        }
    }

    private string _startupType = "";
    public string StartupType
    {
        get => _startupType;
        set { if (_startupType != value) { _startupType = value; OnPropertyChanged(); } }
    }

    public bool CanStop { get; set; }

    // New Properties
    public string ImagePath { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Publisher { get; set; } = "Unknown";
    public bool IsSystemService { get; set; }
    public bool IsCriticalService { get; set; }
    public bool IsMicrosoftService { get; set; }
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath) && System.IO.File.Exists(IconPath);
    [System.Text.Json.Serialization.JsonIgnore]
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
    public string ServiceDescription { get; set; } = "";
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

    private static SolidColorBrush? _runBgBrush;
    private static SolidColorBrush? _stopBgBrush;
    private static SolidColorBrush? _runFgBrush;
    private static SolidColorBrush? _stopFgBrush;
    private static SolidColorBrush? _msBgBrush;
    private static SolidColorBrush? _tpBgBrush;
    private static SolidColorBrush? _msFgBrush;
    private static SolidColorBrush? _tpFgBrush;

    // UI Helper properties
    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusBgBrush
    {
        get
        {
            try
            {
                return Status.Equals("Running", StringComparison.OrdinalIgnoreCase)
                    ? (_runBgBrush ??= new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)))
                    : (_stopBgBrush ??= new SolidColorBrush(Color.FromArgb(30, 107, 114, 128)));
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusFgBrush
    {
        get
        {
            try
            {
                return Status.Equals("Running", StringComparison.OrdinalIgnoreCase)
                    ? (_runFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)))
                    : (_stopFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)));
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? CategoryBgBrush
    {
        get
        {
            try
            {
                return IsMicrosoftService
                    ? (_msBgBrush ??= new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)))
                    : (_tpBgBrush ??= new SolidColorBrush(Color.FromArgb(30, 127, 86, 217)));
            }
            catch { return null; }
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? CategoryFgBrush
    {
        get
        {
            try
            {
                return IsMicrosoftService
                    ? (_msFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)))
                    : (_tpFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 127, 86, 217)));
            }
            catch { return null; }
        }
    }

    public string CategoryText => IsMicrosoftService ? "System" : "Third-Party";

    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase);
    public bool IsNotRunning => !IsRunning;
}

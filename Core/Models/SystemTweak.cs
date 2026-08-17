using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinCarePro.Services;

namespace WinCarePro.Models;

public class SystemTweak : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = ""; // Performance, Network, Disk, UI Responsiveness
    public string IconGlyph { get; set; } = "";
    public string RegistryPath { get; set; } = "";
    public string RecommendedValue { get; set; } = "";

    private string _currentValue = "";
    public string CurrentValue
    {
        get => _currentValue;
        set
        {
            if (_currentValue != value)
            {
                _currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
                OnPropertyChanged(nameof(ComparisonText));
            }
        }
    }

    private bool _isOptimized;
    public bool IsOptimized
    {
        get => _isOptimized;
        set
        {
            if (_isOptimized != value)
            {
                _isOptimized = value;
                OnPropertyChanged(nameof(IsOptimized));
                OnPropertyChanged(nameof(StatusFormatted));
                OnPropertyChanged(nameof(ComparisonText));
                OnPropertyChanged(nameof(StatusBgColor));
                OnPropertyChanged(nameof(StatusBorderColor));
                OnPropertyChanged(nameof(StatusForegroundColor));
            }
        }
    }

    public string StatusFormatted => IsOptimized ? "Optimized" : "Available";

    private static SolidColorBrush? _optBgBrush;
    private static SolidColorBrush? _availBgBrush;
    private static SolidColorBrush? _optBorderBrush;
    private static SolidColorBrush? _availBorderBrush;
    private static SolidColorBrush? _optFgBrush;
    private static SolidColorBrush? _availFgBrush;

    [System.Text.Json.Serialization.JsonIgnore]
    public Brush? StatusBgColor
    {
        get
        {
            try
            {
                return IsOptimized 
                    ? (_optBgBrush ??= new SolidColorBrush(Color.FromArgb(30, 16, 185, 129)))
                    : (_availBgBrush ??= new SolidColorBrush(Color.FromArgb(20, 59, 130, 246)));
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
                return IsOptimized 
                    ? (_optBorderBrush ??= new SolidColorBrush(Color.FromArgb(48, 16, 185, 129)))
                    : (_availBorderBrush ??= new SolidColorBrush(Color.FromArgb(32, 59, 130, 246)));
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
                return IsOptimized 
                    ? (_optFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)))
                    : (_availFgBrush ??= new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)));
            }
            catch { return null; }
        }
    }

    public string ComparisonText
    {
        get
        {
            string cur = CurrentValue;
            string rec = RecommendedValue;
            try
            {
                if (Id == "AllowAutoGameMode" || Id == "NtfsDisableLastAccessUpdate" || Id == "AllowTelemetry" || Id == "AllowCortana" || Id == "WerDisabled" || Id == "DisableBackoff")
                {
                    cur = cur == "1" ? "Enabled".T() : "Disabled".T();
                    rec = rec == "1" ? "Enabled".T() : "Disabled".T();
                }
                else if (Id == "HwSchMode")
                {
                    cur = cur == "2" ? "Enabled".T() : "Disabled".T();
                    rec = rec == "2" ? "Enabled".T() : "Disabled".T();
                }
                else if (Id == "MenuShowDelay")
                {
                    cur = $"{cur} ms";
                    rec = $"{rec} ms";
                }
                else if (Id == "WaitToKillAppTimeout")
                {
                    if (double.TryParse(cur, out double curMs))
                        cur = $"{curMs / 1000.0} s";
                    if (double.TryParse(rec, out double recMs))
                        rec = $"{recMs / 1000.0} s";
                }
                else if (Id == "NetworkThrottlingIndex")
                {
                    cur = (cur == "-1" || cur == "4294967295") ? "Disabled".T() : "Default (10)".T();
                    rec = "Disabled".T();
                }
                else if (Id == "SystemResponsiveness")
                {
                    cur = cur == "0" ? "High Priority (0)".T() : $"Normal (20)".T();
                    rec = "High Priority (0)".T();
                }
                else if (Id == "MinAnimate")
                {
                    cur = cur == "0" ? "Disabled".T() : "Enabled".T();
                    rec = "Disabled".T();
                }
            }
            catch { }
            
            return string.Format("Current: {0} | Recommended: {1}".T(), cur, rec);
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
}

using System;
using System.ComponentModel;
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
    public string DefaultValue { get; set; } = "";
    public bool RequiresRestart { get; set; } = false;
    public bool RequiresAdmin { get; set; } = true;
    public bool Reversible { get; set; } = true;
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

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
            }
        }
    }

    public string StatusFormatted => IsOptimized ? "Optimized" : "Available";

    public string ComparisonText
    {
        get
        {
            string cur = CurrentValue;
            string rec = RecommendedValue;
            try
            {
                if (Id == "AutoEndTasks" || Id == "NtfsDisableLastAccessUpdate" || Id == "AllowTelemetry" || Id == "AllowCortana" || Id == "WerDisabled" || Id == "DisableLocation")
                {
                    cur = cur == "1" ? "Enabled".T() : "Disabled".T();
                    rec = rec == "1" ? "Enabled".T() : "Disabled".T();
                }
                else if (Id == "GameDVR_Enabled")
                {
                    cur = cur == "0" ? "Disabled".T() : "Enabled".T();
                    rec = "Disabled".T();
                }
                else if (Id == "HwSchMode")
                {
                    cur = cur == "2" ? "Enabled".T() : "Disabled".T();
                    rec = "Enabled".T();
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
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException("SystemTweak.ComparisonText", ex);
            }
            
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

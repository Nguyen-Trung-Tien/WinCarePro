using System;
using System.ComponentModel;

namespace WinCarePro.Models;

public class ScheduledTaskEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Action { get; set; } = "";
    
    private string _status = "";
    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }

    // New Properties
    public string Author { get; set; } = "";
    public string Folder { get; set; } = "";
    public bool IsMicrosoftTask { get; set; }
    public bool IsCriticalTask { get; set; }
    public int LastResult { get; set; }
    public string TaskDescription { get; set; } = "";
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High

    // UI Helper properties
    public string DisplayLastRunTime => LastRunTime.HasValue ? LastRunTime.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
    public string DisplayNextRunTime => NextRunTime.HasValue ? NextRunTime.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
}

using System;
using System.ComponentModel;

namespace WinCarePro.Models;

public class DriverInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Name { get; set; } = "";
    public string DeviceClass { get; set; } = "";
    public string Provider { get; set; } = "";
    public string DriverVersion { get; set; } = "";
    public string DriverDate { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasUpdate { get; set; }
    public string AvailableVersion { get; set; } = "";

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
    public string UpdateStatus
    {
        get => _updateStatus;
        set
        {
            if (_updateStatus != value)
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
            }
        }
    }
}

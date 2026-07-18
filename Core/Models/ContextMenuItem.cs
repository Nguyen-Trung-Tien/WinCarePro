using System;
using System.ComponentModel;

namespace WinCarePro.Models;

public class ContextMenuItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string RegistryPath { get; set; } = "";
    public string Type { get; set; } = ""; // "File", "Folder", "Desktop Background"

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public string ClassId { get; set; } = ""; // CLSID if applicable
}

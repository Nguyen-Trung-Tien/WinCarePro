using System;
using System.Collections.ObjectModel;
using WinCarePro.Core.Helpers;
using WinCarePro.ViewModels;

namespace WinCarePro.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public class DriveHealthInfo
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public string HealthStatus { get; set; } = "Unknown";
    public double Temperature { get; set; }
    public string TemperatureFormatted => $"{Temperature:F0}°C";
    public string Interface { get; set; } = "";
}

[Microsoft.UI.Xaml.Data.Bindable]
public class StorageItem
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public long SizeBytes { get; set; }
    public double Percentage { get; set; }
    public string PercentageFormatted => $"{Percentage:F1}%";
    public string SizeFormatted => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(SizeBytes);
    public bool IsDirectory { get; set; }
    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE7C3";
}

[Microsoft.UI.Xaml.Data.Bindable]
public class StorageDuplicateItem : ViewModelBase
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime LastModified { get; set; }
    public string LastModifiedFormatted => LastModified.ToString("yyyy-MM-dd HH:mm");

    private bool _isSelectedForDeletion;
    public bool IsSelectedForDeletion
    {
        get => _isSelectedForDeletion;
        set => SetProperty(ref _isSelectedForDeletion, value);
    }
}

[Microsoft.UI.Xaml.Data.Bindable]
public class StorageDuplicateGroup : ViewModelBase
{
    public string SizeFormatted { get; set; } = "";
    public ObservableCollection<StorageDuplicateItem> Items { get; } = new();
}

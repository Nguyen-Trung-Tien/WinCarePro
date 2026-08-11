using System;
using System.Collections.ObjectModel;
using WinCarePro.ViewModels;

namespace WinCarePro.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public class StorageDuplicateItem : ViewModelBase
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime LastModified { get; set; }

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

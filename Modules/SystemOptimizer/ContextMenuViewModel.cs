using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class ContextMenuViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ContextMenuEngine _engine = App.Services?.GetService<ContextMenuEngine>() ?? new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _statusText = "Ready".T();
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _activeCount;
    public int ActiveCount
    {
        get => _activeCount;
        set => SetProperty(ref _activeCount, value);
    }

    private int _disabledCount;
    public int DisabledCount
    {
        get => _disabledCount;
        set => SetProperty(ref _disabledCount, value);
    }

    private string _filterCategory = "All";
    public string FilterCategory
    {
        get => _filterCategory;
        set
        {
            if (SetProperty(ref _filterCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<ContextMenuItem> Items { get; } = new();
    public ObservableCollection<ContextMenuItem> FilteredItems { get; } = new();

    public ContextMenuViewModel()
    {
        _dispatcherQueue = SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;
        _engine.ProgressMessage += (msg) => Log(msg);
        _ = ScanAsync();
    }

    private void Log(string msg)
    {
        System.Diagnostics.Debug.WriteLine($"[ContextMenu] {msg}");
    }

    private void UpdateCounts()
    {
        TotalCount = Items.Count;
        ActiveCount = Items.Count(x => x.IsEnabled);
        DisabledCount = Items.Count(x => !x.IsEnabled);
    }

    public async Task ScanAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Scanning context menu handlers...".T();
        Items.Clear();
        FilteredItems.Clear();

        try
        {
            var result = await _engine.ScanContextMenuItemsAsync();
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in result)
                {
                    Items.Add(item);
                }
                UpdateCounts();
                ApplyFilter();
                StatusText = string.Format("Found {0} context menu handlers.".T(), Items.Count);
            });
        }
        catch (Exception ex)
        {
            StatusText = "Scan failed:".T() + " " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> ToggleItemAsync(ContextMenuItem item, bool enable)
    {
        if (IsBusy) return false;
        IsBusy = true;
        StatusText = string.Format(
            enable ? "Enabling {0}...".T() : "Disabling {0}...".T(), 
            item.Name
        );

        try
        {
            bool ok = await _engine.ToggleContextMenuItemAsync(item, enable);
            if (ok)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    item.IsEnabled = enable;
                    UpdateCounts();
                    StatusText = string.Format(
                        enable ? "{0} enabled successfully.".T() : "{0} disabled successfully.".T(),
                        item.Name
                    );
                    ApplyFilter();
                });
                return true;
            }
            else
            {
                StatusText = "Failed to modify setting. Administrator rights required.".T();
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusText = "Error: ".T() + ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyFilter()
    {
        FilteredItems.Clear();
        var query = Items.AsEnumerable();

        if (!string.Equals(FilterCategory, "All", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(FilterCategory, "All".T(), StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Type.Equals(FilterCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(x => x.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                     x.RegistryPath.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }
    }
}

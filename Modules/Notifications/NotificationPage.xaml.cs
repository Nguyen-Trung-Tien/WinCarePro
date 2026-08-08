using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinCarePro.Views;

public class NotificationGroup : List<NotificationItem>
{
    public string Name { get; }
    public NotificationGroup(string name, List<NotificationItem> items) : base(items)
    {
        Name = name;
    }
}

public sealed partial class NotificationPage : Page
{
    private DispatcherQueueTimer? _liveRefreshTimer;
    private DispatcherQueueTimer? _searchDebounceTimer;

    public NotificationPage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();

        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;

        DbManager.OnNotificationAdded += OnLiveNotificationAdded;
        DbManager.OnLogAdded += OnLiveLogAdded;

        this.Loaded += async (s, e) =>
        {
            ApplyLocalization();
            await LoadNotificationsAsync();
            await LoadLogsAsync();
            
            // Mark all notifications as read when viewing the page
            DbManager.MarkAllNotificationsAsRead();
        };

        this.Unloaded += (s, e) =>
        {
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            DbManager.OnNotificationAdded -= OnLiveNotificationAdded;
            DbManager.OnLogAdded -= OnLiveLogAdded;
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        App.MainDispatcherQueue?.TryEnqueue(async () =>
        {
            ApplyLocalization();
            await LoadNotificationsAsync();
            await LoadLogsAsync();
        });
    }

    private void TriggerThrottledRefresh()
    {
        App.MainDispatcherQueue?.TryEnqueue(() =>
        {
            if (_liveRefreshTimer == null)
            {
                _liveRefreshTimer = App.MainDispatcherQueue.CreateTimer();
                _liveRefreshTimer.Interval = TimeSpan.FromMilliseconds(300);
                _liveRefreshTimer.Tick += async (s, e) =>
                {
                    _liveRefreshTimer.Stop();
                    await LoadNotificationsAsync();
                    await LoadLogsAsync();
                };
            }
            _liveRefreshTimer.Stop();
            _liveRefreshTimer.Start();
        });
    }

    private void OnLiveNotificationAdded(NotificationItem item)
    {
        TriggerThrottledRefresh();
    }

    private void OnLiveLogAdded(LogEntry entry)
    {
        TriggerThrottledRefresh();
    }

    private void ApplyLocalization()
    {
        try
        {
            if (PageTitleTextBlock != null) PageTitleTextBlock.Text = "Notifications & Activity Log".T();
            if (PageSubtitleTextBlock != null) PageSubtitleTextBlock.Text = "Review system notifications, update alerts, and detailed operation history logs.".T();

            if (AlertsPivotItem != null) AlertsPivotItem.Header = "System Alerts".T();
            if (LogsPivotItem != null) LogsPivotItem.Header = "Activity Log".T();

            if (NotificationSearchBox != null) NotificationSearchBox.PlaceholderText = "Search notifications...".T();
            if (SearchBox != null) SearchBox.PlaceholderText = "Search logs by action or status...".T();

            if (LevelAllItem != null) LevelAllItem.Content = "All Levels".T();
            if (LevelInfoItem != null) LevelInfoItem.Content = "Info".T();
            if (LevelWarningItem != null) LevelWarningItem.Content = "Warning".T();
            if (LevelCriticalItem != null) LevelCriticalItem.Content = "Critical".T();

            if (ClearAlertsBtn != null) ClearAlertsBtn.Content = "Clear Alerts".T();
            if (NotificationsEmptyText != null) NotificationsEmptyText.Text = "All clear! No new notifications.".T();
            if (LogsEmptyText != null) LogsEmptyText.Text = "No activity logs found.".T();

            if (ModAllItem != null) ModAllItem.Content = "All Modules".T();
            if (ModDashItem != null) ModDashItem.Content = "Dashboard".T();
            if (ModJunkItem != null) ModJunkItem.Content = "Junk Cleaner".T();
            if (ModRegItem != null) ModRegItem.Content = "Registry Cleaner".T();
            if (ModStartItem != null) ModStartItem.Content = "Startup Manager".T();
            if (ModDiskItem != null) ModDiskItem.Content = "Disk Analyzer".T();
            if (ModNetItem != null) ModNetItem.Content = "Network Service".T();
            if (ModSecItem != null) ModSecItem.Content = "Security Shield".T();
            if (ModUninstItem != null) ModUninstItem.Content = "Uninstaller".T();
            if (ModDrvItem != null) ModDrvItem.Content = "Driver Manager".T();
            if (ModOptItem != null) ModOptItem.Content = "System Optimizer".T();
            if (ModRepItem != null) ModRepItem.Content = "Repair Tools".T();

            if (ColHeaderAction != null) ColHeaderAction.Text = "Action / Operation".T();
            if (ColHeaderModule != null) ColHeaderModule.Text = "Module".T();
            if (ColHeaderStatus != null) ColHeaderStatus.Text = "Status".T();
            if (ColHeaderTime != null) ColHeaderTime.Text = "Time Logged".T();

            if (RefreshLogsBtn != null) RefreshLogsBtn.Content = "Refresh".T();
            if (ClearLogsBtn != null) ClearLogsBtn.Content = "Clear Logs".T();
            if (ExportLogsBtn != null) ExportLogsBtn.Content = "Export Logs".T();
        }
        catch { }
    }

    private async System.Threading.Tasks.Task LoadNotificationsAsync()
    {
        if (NotificationSearchBox == null || NotificationLevelFilter == null || 
            NotificationsEmptyState == null || NotificationsListView == null || 
            GroupedNotificationsCVS == null)
        {
            return;
        }

        string search = NotificationSearchBox.Text?.Trim() ?? "";
        string tag = (NotificationLevelFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        try
        {
            var (groups, count) = await System.Threading.Tasks.Task.Run(() =>
            {
                var notifications = DbManager.GetRecentNotifications();
                if (notifications == null) notifications = new List<NotificationItem>();

                if (!string.IsNullOrEmpty(search))
                {
                    notifications = notifications.Where(n => 
                        n.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                        n.Message.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (tag != "All")
                {
                    notifications = notifications.Where(n => n.Level.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var grouped = notifications
                    .GroupBy(n => GetGroupHeader(n.CreatedAt))
                    .Select(g => new NotificationGroup(g.Key, g.ToList()))
                    .ToList();

                return (grouped, notifications.Count);
            });

            if (count == 0)
            {
                NotificationsEmptyState.Visibility = Visibility.Visible;
                NotificationsListView.Visibility = Visibility.Collapsed;
                GroupedNotificationsCVS.Source = null;
            }
            else
            {
                NotificationsEmptyState.Visibility = Visibility.Collapsed;
                NotificationsListView.Visibility = Visibility.Visible;
                GroupedNotificationsCVS.Source = groups;
            }
        }
        catch
        {
            NotificationsEmptyState.Visibility = Visibility.Visible;
            NotificationsListView.Visibility = Visibility.Collapsed;
            GroupedNotificationsCVS.Source = null;
        }
    }

    private string GetGroupHeader(DateTime dt)
    {
        var today = DateTime.Today;
        if (dt.Date == today) return "Today".T();
        if (dt.Date == today.AddDays(-1)) return "Yesterday".T();
        return "Older Notifications".T();
    }

    private async void OnDeleteSingleNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem item)
        {
            DbManager.DeleteNotification(item.Id);
            await LoadNotificationsAsync();
        }
    }

    private void TriggerDebouncedSearch()
    {
        if (_searchDebounceTimer == null)
        {
            _searchDebounceTimer = App.MainDispatcherQueue?.CreateTimer();
            if (_searchDebounceTimer != null)
            {
                _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(200);
                _searchDebounceTimer.Tick += async (s, e) =>
                {
                    _searchDebounceTimer.Stop();
                    await LoadNotificationsAsync();
                    await LoadLogsAsync();
                };
            }
        }
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Start();
    }

    private void OnNotificationSearchChanged(object sender, TextChangedEventArgs e)
    {
        TriggerDebouncedSearch();
    }

    private async void OnNotificationFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadNotificationsAsync();
    }

    private async System.Threading.Tasks.Task LoadLogsAsync()
    {
        if (ModuleFilter == null || SearchBox == null || LogsListView == null)
        {
            return;
        }

        string? module = null;
        if (ModuleFilter.SelectedItem is ComboBoxItem item)
        {
            string tag = item.Tag?.ToString() ?? "All";
            if (tag != "All")
            {
                module = tag;
            }
        }

        string? search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim();

        try
        {
            var logs = await System.Threading.Tasks.Task.Run(() => DbManager.GetLogs(module, search));
            LogsListView.ItemsSource = logs;

            bool isEmpty = logs == null || logs.Count == 0;
            if (LogsEmptyState != null) LogsEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            if (LogsListView != null) LogsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
        catch { }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        TriggerDebouncedSearch();
    }

    private async void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
        await LoadNotificationsAsync();
    }

    private async void OnClearNotificationsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Clear All Notifications?".T(),
                Content = "Are you sure you want to delete all notifications? This action cannot be undone.".T(),
                PrimaryButtonText = "Clear Alerts".T(),
                CloseButtonText = "Cancel".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DbManager.ClearAllNotifications();
                await LoadNotificationsAsync();
            }
        }
        catch
        {
            // Direct fallback if XamlRoot or dialog fails
            DbManager.ClearAllNotifications();
            await LoadNotificationsAsync();
        }
    }

    private async void OnClearOldLogsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Clear All Activity Logs?".T(),
                Content = "Are you sure you want to delete all activity log entries? This action cannot be undone.".T(),
                PrimaryButtonText = "Clear Logs".T(),
                CloseButtonText = "Cancel".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DbManager.CleanupOldLogs(0); // Clear all logs
                await LoadLogsAsync();
            }
        }
        catch
        {
            // Direct fallback if XamlRoot or dialog fails
            DbManager.CleanupOldLogs(0);
            await LoadLogsAsync();
        }
    }

    private async void OnExportLogsClick(object sender, RoutedEventArgs e)
    {
        var logs = LogsListView.ItemsSource as List<LogEntry>;
        if (logs == null || logs.Count == 0) return;

        var savePicker = new FileSavePicker();
        var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        InitializeWithWindow.Initialize(savePicker, hwnd);

        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
        savePicker.SuggestedFileName = $"WinCarePro_Logs_{DateTime.Now:yyyyMMdd_HHmmss}";

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            var sb = new StringBuilder();
            sb.Append("\uFEFF"); // Add UTF-8 Byte Order Mark (BOM) for Excel Vietnamese character compatibility
            sb.AppendLine("ID,Timestamp,Module,Action,Status");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{log.Module}\",\"{log.Action.Replace("\"", "\"\"")}\",\"{log.Status}\"");
            }
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8);
        }
    }

    // Static helper method used by XAML binding
    public static Visibility IsUnreadVisibility(bool isRead)
    {
        return !isRead ? Visibility.Visible : Visibility.Collapsed;
    }
}

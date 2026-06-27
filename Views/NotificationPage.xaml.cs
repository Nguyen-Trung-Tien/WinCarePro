using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public NotificationPage()
    {
        InitializeComponent();
        this.Loaded += (s, e) =>
        {
            LoadNotifications();
            LoadLogs();
            
            // Mark all notifications as read when viewing the page
            DbManager.MarkAllNotificationsAsRead();
        };
    }

    private void LoadNotifications()
    {
        if (NotificationSearchBox == null || NotificationLevelFilter == null || 
            NotificationsEmptyState == null || NotificationsListView == null || 
            GroupedNotificationsCVS == null)
        {
            return;
        }

        try
        {
            var notifications = DbManager.GetRecentNotifications();
            if (notifications == null) notifications = new List<NotificationItem>();

            // Apply search filter
            string search = NotificationSearchBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                notifications = notifications.Where(n => 
                    n.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                    n.Message.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply level filter
            if (NotificationLevelFilter.SelectedItem is ComboBoxItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString() ?? "All";
                if (tag != "All")
                {
                    notifications = notifications.Where(n => n.Level.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            if (notifications.Count == 0)
            {
                NotificationsEmptyState.Visibility = Visibility.Visible;
                NotificationsListView.Visibility = Visibility.Collapsed;
                GroupedNotificationsCVS.Source = null;
            }
            else
            {
                NotificationsEmptyState.Visibility = Visibility.Collapsed;
                NotificationsListView.Visibility = Visibility.Visible;

                // Group notifications
                var groups = notifications
                    .GroupBy(n => GetGroupHeader(n.CreatedAt))
                    .Select(g => new NotificationGroup(g.Key, g.ToList()))
                    .ToList();

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

    private void OnDeleteSingleNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem item)
        {
            DbManager.DeleteNotification(item.Id);
            LoadNotifications();
        }
    }

    private void OnNotificationSearchChanged(object sender, TextChangedEventArgs e)
    {
        LoadNotifications();
    }

    private void OnNotificationFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadNotifications();
    }

    private void LoadLogs()
    {
        if (ModuleFilter == null || SearchBox == null || LogsListView == null)
        {
            return;
        }

        try
        {
            string? module = null;
            if (ModuleFilter.SelectedItem is ComboBoxItem item && item.Content.ToString() != "All Modules")
            {
                module = item.Content.ToString();
            }

            string? search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim();

            var logs = DbManager.GetLogs(module, search);
            LogsListView.ItemsSource = logs;
        }
        catch { }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        LoadLogs();
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadLogs();
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        LoadLogs();
        LoadNotifications();
    }

    private void OnClearNotificationsClick(object sender, RoutedEventArgs e)
    {
        DbManager.ClearAllNotifications();
        LoadNotifications();
    }

    private void OnClearOldLogsClick(object sender, RoutedEventArgs e)
    {
        DbManager.CleanupOldLogs(0); // Clear all logs
        LoadLogs();
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

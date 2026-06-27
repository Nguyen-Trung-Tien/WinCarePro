using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Database;
using WinCarePro.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinCarePro.Views;

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
        try
        {
            var notifications = DbManager.GetRecentNotifications();
            NotificationsListView.ItemsSource = notifications;
            
            if (notifications == null || notifications.Count == 0)
            {
                NotificationsEmptyState.Visibility = Visibility.Visible;
                NotificationsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                NotificationsEmptyState.Visibility = Visibility.Collapsed;
                NotificationsListView.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            NotificationsEmptyState.Visibility = Visibility.Visible;
            NotificationsListView.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadLogs()
    {
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

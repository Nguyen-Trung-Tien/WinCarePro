using System;
using System.Collections.Generic;
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

public sealed partial class NotificationPage : Page
{
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _searchDebounceTimer;
    private bool _isAlertsTabActive = true;
    private EventHandler? _languageChangedHandler;

    public NotificationPage()
    {
        this.InitializeComponent();
        this.Loaded += NotificationPage_Loaded;
        this.Unloaded += NotificationPage_Unloaded;

        _languageChangedHandler = (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TranslationManager.Instance.Translate(this);
                ApplyLocalization();
            });
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;
    }

    private async void NotificationPage_Loaded(object sender, RoutedEventArgs e)
    {
        TranslationManager.Instance.Translate(this);
        ApplyLocalization();
        SetActiveTab(true);
        await LoadNotificationsAsync();
        await LoadLogsAsync();
    }

    private void NotificationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _searchDebounceTimer?.Stop();
        if (_languageChangedHandler != null)
        {
            TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        }
    }

    private void ApplyLocalization()
    {
        if (PageTitleTextBlock != null) PageTitleTextBlock.Text = "Notifications & Activity Log".T();
        if (PageSubtitleTextBlock != null) PageSubtitleTextBlock.Text = "System telemetry alerts, diagnostic logs & operation records.".T();

        if (BtnTabAlerts != null && BtnTabAlerts.Content is not StackPanel) BtnTabAlerts.Content = "System Alerts".T();
        if (BtnTabLogs != null && BtnTabLogs.Content is not StackPanel) BtnTabLogs.Content = "Activity History".T();

        if (NotificationSearchBox != null) NotificationSearchBox.PlaceholderText = "Search alerts by title or description...".T();
        if (SearchBox != null) SearchBox.PlaceholderText = "Search logs by action or status...".T();

        if (ClearAlertsBtn != null) ClearAlertsBtn.Content = "Clear All Alerts".T();
        if (ClearLogsBtn != null) ClearLogsBtn.Content = "Clear Logs".T();
        if (RefreshLogsBtn != null) RefreshLogsBtn.Content = "Refresh".T();
        if (ExportLogsBtn != null) ExportLogsBtn.Content = "Export Logs".T();

        if (LevelAllItem != null) LevelAllItem.Content = "All Levels".T();
        if (LevelInfoItem != null) LevelInfoItem.Content = "Info".T();
        if (LevelWarningItem != null) LevelWarningItem.Content = "Warning".T();
        if (LevelCriticalItem != null) LevelCriticalItem.Content = "Critical".T();

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

        if (NotificationsEmptyText != null) NotificationsEmptyText.Text = "All clear! No active system alerts.".T();
        if (LogsEmptyText != null) LogsEmptyText.Text = "No activity logs found.".T();
    }

    private void OnTabAlertsClick(object sender, RoutedEventArgs e)
    {
        SetActiveTab(true);
    }

    private void OnTabLogsClick(object sender, RoutedEventArgs e)
    {
        SetActiveTab(false);
    }

    private void SetActiveTab(bool alertsActive)
    {
        _isAlertsTabActive = alertsActive;
        if (SectionAlerts != null) SectionAlerts.Visibility = alertsActive ? Visibility.Visible : Visibility.Collapsed;
        if (SectionLogs != null) SectionLogs.Visibility = alertsActive ? Visibility.Collapsed : Visibility.Visible;

        if (BtnTabAlerts != null && BtnTabLogs != null)
        {
            if (alertsActive)
            {
                BtnTabAlerts.Style = (Style)Application.Current.Resources["VibrantPrimaryButtonStyle"];
                BtnTabLogs.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];
            }
            else
            {
                BtnTabAlerts.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];
                BtnTabLogs.Style = (Style)Application.Current.Resources["VibrantPrimaryButtonStyle"];
            }
        }
    }

    private async System.Threading.Tasks.Task LoadNotificationsAsync()
    {
        if (NotificationSearchBox == null || NotificationLevelFilter == null || 
            NotificationsEmptyState == null || NotificationsListView == null || 
            GroupedNotificationsCVS == null)
            return;

        string search = NotificationSearchBox.Text.Trim();
        string tag = (NotificationLevelFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        try
        {
            var (groups, count, totalActive) = await System.Threading.Tasks.Task.Run(() =>
            {
                var allNotifications = DbManager.GetRecentNotifications();
                if (allNotifications == null) allNotifications = new List<NotificationItem>();

                int total = allNotifications.Count;
                var filtered = allNotifications;

                if (!string.IsNullOrEmpty(search))
                {
                    filtered = filtered.Where(n => 
                        n.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                        n.Message.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (tag != "All")
                {
                    filtered = filtered.Where(n => n.Level.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var grouped = filtered
                    .GroupBy(n => n.TimeAgoGroup)
                    .Select(g => new NotificationGroup(g.Key, g.ToList()))
                    .ToList();

                return (grouped, filtered.Count, total);
            });

            // Update Telemetry Deck Card 1
            if (DeckAlertsCountText != null) DeckAlertsCountText.Text = totalActive.ToString();
            if (DeckAlertsPill != null && DeckAlertsPillText != null)
            {
                if (totalActive == 0)
                {
                    DeckAlertsPill.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeGoodBg"];
                    DeckAlertsPill.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeGoodBorder"];
                    DeckAlertsPillText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeGoodFg"];
                    DeckAlertsPillText.Text = "All clear".T();
                }
                else
                {
                    DeckAlertsPill.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeWarningBg"];
                    DeckAlertsPill.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeWarningBorder"];
                    DeckAlertsPillText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBadgeWarningFg"];
                    DeckAlertsPillText.Text = $"{totalActive} Action(s)".T();
                }
            }

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

    private async System.Threading.Tasks.Task LoadLogsAsync()
    {
        if (ModuleFilter == null || SearchBox == null || LogsListView == null || LogsEmptyState == null)
            return;

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

            // Update Telemetry Deck Cards 2 & 3
            if (logs != null && logs.Count > 0)
            {
                int todayCount = logs.Count(l => l.CreatedAt.Date == DateTime.Today);
                if (DeckOperationsCountText != null) DeckOperationsCountText.Text = todayCount.ToString();

                var latest = logs.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                if (latest != null)
                {
                    if (DeckRecentPulseText != null) DeckRecentPulseText.Text = latest.RelativeTimeAgo;
                    if (DeckRecentModuleText != null) DeckRecentModuleText.Text = $"{latest.Module} • {latest.Status}";
                }
            }
            else
            {
                if (DeckOperationsCountText != null) DeckOperationsCountText.Text = "0";
                if (DeckRecentPulseText != null) DeckRecentPulseText.Text = "Just Now";
                if (DeckRecentModuleText != null) DeckRecentModuleText.Text = "System Idle";
            }

            bool isEmpty = logs == null || logs.Count == 0;
            LogsEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            LogsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
        catch { }
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
                    if (_isAlertsTabActive)
                        await LoadNotificationsAsync();
                    else
                        await LoadLogsAsync();
                };
            }
        }
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Start();
    }

    private void OnNotificationSearchChanged(object sender, TextChangedEventArgs e) => TriggerDebouncedSearch();
    private async void OnNotificationFilterChanged(object sender, SelectionChangedEventArgs e) => await LoadNotificationsAsync();
    private void OnSearchChanged(object sender, TextChangedEventArgs e) => TriggerDebouncedSearch();
    private async void OnFilterChanged(object sender, SelectionChangedEventArgs e) => await LoadLogsAsync();

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadNotificationsAsync();
        await LoadLogsAsync();
    }

    private async void OnDeleteSingleNotificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem item)
        {
            DbManager.DeleteNotification(item.Id);
            await LoadNotificationsAsync();
        }
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
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
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
                XamlRoot = this.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DbManager.CleanupOldLogs(0);
                await LoadLogsAsync();
            }
        }
        catch
        {
            DbManager.CleanupOldLogs(0);
            await LoadLogsAsync();
        }
    }

    private async void OnExportLogsClick(object sender, RoutedEventArgs e)
    {
        var logs = LogsListView?.ItemsSource as List<LogEntry>;
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
            sb.Append("\uFEFF");
            sb.AppendLine("ID,Timestamp,Module,Action,Status");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{log.Module}\",\"{log.Action.Replace("\"", "\"\"")}\",\"{log.Status}\"");
            }
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8);
        }
    }
}

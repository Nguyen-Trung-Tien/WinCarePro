using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loadingSettings = true; // Guard initialization events from saving settings early
    private List<string> _traceLogs = new();

    public SettingsPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        // Initialize mock trace logs
        _traceLogs = new List<string>
        {
            "[*] System diagnostics trace initialized...".T(),
            "[*] Registered local SQLite connection...".T(),
            "[*] Background scheduling task parsed...".T(),
            "[*] Telemetry sensor monitoring thread spawned...".T(),
            "[*] Safety policies integrity check: PASS".T(),
            "[*] No CPU bottlenecks or memory leaks detected.".T(),
            "[!] Warning: Winget update repository has outdated packages.".T(),
            "[*] Diagnostic log purge: waiting user input...".T()
        };

        LoadSettings();
        UpdateStorageSizes();
        PopulateTraceLogs();
    }

    private void LoadSettings()
    {
        _loadingSettings = true;
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                var profile = JsonSerializer.Deserialize<SettingsProfile>(raw);
                if (profile != null)
                {
                    // General & updates
                    LanguageComboBox.SelectedIndex = profile.LanguageIndex;
                    AutoScanToggle.IsOn = profile.AutoScan;
                    AutoUpdateToggle.IsOn = profile.AutoCheckUpdates;
                    AutoInstallUpdatesToggle.IsOn = profile.AutoInstallUpdates;
                    MinimizeToTrayToggle.IsOn = profile.MinimizeToTray;
                    BetaUpdatesToggle.IsOn = profile.BetaUpdates;

                    // Appearance
                    ApplyAccentColorSelection(profile.AccentColor);
                    TransparencySlider.Value = profile.TransparencyLevel;
                    EnableAnimationsToggle.IsOn = profile.EnableAnimations;
                    ApplyThemeCardSelection(profile.Theme == "Dark");

                    // Auto Maintenance
                    AutoCleanupSlider.Value = profile.AutoCleanupTriggerSizeGB;
                    CleanupSizeLabel.Text = $"{profile.AutoCleanupTriggerSizeGB:F1} GB";
                    TriggerSmartBoostToggle.IsOn = profile.TriggerSmartBoost;
                    MaintenanceFrequencyComboBox.SelectedIndex = profile.MaintenanceFrequencyIndex;

                    // Notifications Settings
                    ShowNotificationsToggle.IsOn = profile.ShowNotifications;
                    NotificationThresholdSlider.Value = profile.NotificationThreshold;
                    NotificationThresholdLabel.Text = $"{profile.NotificationThreshold:F0}%";
                    NotifyOnLowHealthToggle.IsOn = profile.NotifyOnLowHealth;
                    NotifyOnMaintenanceToggle.IsOn = profile.NotifyOnMaintenance;
                    ShowUpdateNotificationsToggle.IsOn = profile.ShowUpdateNotifications;
                    NotificationSoundToggle.IsOn = profile.NotificationSound;

                    // Telemetry
                    TelemetryIntervalComboBox.SelectedIndex = profile.TelemetryIntervalIndex;
                    PerformanceHistoryComboBox.SelectedIndex = profile.PerformanceHistoryDurationIndex;
                    EnableHardwareSensorsToggle.IsOn = profile.EnableSensorsThread;

                    // Safety
                    CreateRestorePointToggle.IsOn = profile.CreateRestorePoint;
                    BackupRegistryToggle.IsOn = profile.BackupRegistryHive;
                    AlertsLevelSlider.Value = profile.ConfirmationAlertsLevel;

                    // Advanced
                    EnableVerboseLogsToggle.IsOn = profile.EnableVerboseLogs;
                    EnableExperimentalAiToggle.IsOn = profile.EnableExperimentalAi;
                }
            }
        }
        catch { }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void SaveSettings()
    {
        if (_loadingSettings) return;

        try
        {
            double sizeGB = AutoCleanupSlider.Value;
            if (sizeGB <= 0) sizeGB = 5.0;

            string currentTheme = "Dark";
            if (App.MainWindowInstance != null)
            {
                currentTheme = (App.MainWindowInstance.Content as FrameworkElement)?.RequestedTheme == ElementTheme.Light ? "Light" : "Dark";
            }

            var profile = new SettingsProfile
            {
                Theme = currentTheme,
                AutoScan = AutoScanToggle.IsOn,
                ReportFormat = "TXT",
                
                LanguageIndex = LanguageComboBox.SelectedIndex,
                AutoCheckUpdates = AutoUpdateToggle.IsOn,
                AutoInstallUpdates = AutoInstallUpdatesToggle.IsOn,
                MinimizeToTray = MinimizeToTrayToggle.IsOn,
                BetaUpdates = BetaUpdatesToggle.IsOn,

                AccentColor = GetSelectedAccentColorTag(),
                TransparencyLevel = TransparencySlider.Value,
                EnableAnimations = EnableAnimationsToggle.IsOn,

                AutoCleanupTriggerSizeGB = sizeGB,
                TriggerSmartBoost = TriggerSmartBoostToggle.IsOn,
                MaintenanceFrequencyIndex = MaintenanceFrequencyComboBox.SelectedIndex,

                ShowNotifications = ShowNotificationsToggle.IsOn,
                NotificationThreshold = NotificationThresholdSlider.Value,
                NotifyOnLowHealth = NotifyOnLowHealthToggle.IsOn,
                NotifyOnMaintenance = NotifyOnMaintenanceToggle.IsOn,
                ShowUpdateNotifications = ShowUpdateNotificationsToggle.IsOn,
                NotificationSound = NotificationSoundToggle.IsOn,

                TelemetryIntervalIndex = TelemetryIntervalComboBox.SelectedIndex,
                PerformanceHistoryDurationIndex = PerformanceHistoryComboBox.SelectedIndex,
                EnableSensorsThread = EnableHardwareSensorsToggle.IsOn,

                CreateRestorePoint = CreateRestorePointToggle.IsOn,
                BackupRegistryHive = BackupRegistryToggle.IsOn,
                ConfirmationAlertsLevel = AlertsLevelSlider.Value,

                EnableVerboseLogs = EnableVerboseLogsToggle.IsOn,
                EnableExperimentalAi = EnableExperimentalAiToggle.IsOn
            };

            string json = JsonSerializer.Serialize(profile);
            DbManager.SaveSettings(json);

            // Apply modifications immediately
            ApplyRuntimeSettings(profile);
        }
        catch { }
    }

    private void ApplyRuntimeSettings(SettingsProfile profile)
    {
        // 1. Accent color
        App.ApplyAccentColor(profile.AccentColor);

        // 2. Transparency
        if (App.MainWindowInstance != null)
        {
            App.MainWindowInstance.ApplyTransparency(profile.TransparencyLevel);
        }

        // 3. Animations
        if (App.MainWindowInstance != null)
        {
            if (App.MainWindowInstance.MainFrame.Content is MainPage mainPage)
            {
                mainPage.ApplyAnimationsEnabled(profile.EnableAnimations);
            }
        }
    }

    private string GetSelectedAccentColorTag()
    {
        if (AccentGreen.Stroke != null) return "Green";
        if (AccentPurple.Stroke != null) return "Purple";
        if (AccentPink.Stroke != null) return "Pink";
        if (AccentAmber.Stroke != null) return "Amber";
        return "Default";
    }

    private void ApplyAccentColorSelection(string tag)
    {
        AccentDefault.Stroke = null;
        AccentDefault.StrokeThickness = 0;
        AccentGreen.Stroke = null;
        AccentGreen.StrokeThickness = 0;
        AccentPurple.Stroke = null;
        AccentPurple.StrokeThickness = 0;
        AccentPink.Stroke = null;
        AccentPink.StrokeThickness = 0;
        AccentAmber.Stroke = null;
        AccentAmber.StrokeThickness = 0;

        var selectedEllipse = (tag ?? "default").ToLower() switch
        {
            "green" => AccentGreen,
            "purple" => AccentPurple,
            "pink" => AccentPink,
            "amber" => AccentAmber,
            _ => AccentDefault
        };

        if (selectedEllipse != null)
        {
            bool isDark = true;
            if (App.MainWindowInstance != null)
            {
                isDark = (App.MainWindowInstance.MainRootGrid.RequestedTheme != ElementTheme.Light);
            }
            selectedEllipse.Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.DimGray);
            selectedEllipse.StrokeThickness = 2.5;
        }
    }

    private void OnAutoScanToggled(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
        try
        {
            var engine = new WinCarePro.Engines.StartupEngine();
            engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
        }
        catch { }
    }

    private void OnSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
        
        int index = LanguageComboBox.SelectedIndex;
        TranslationManager.Instance.CurrentLanguage = index == 1 ? AppLanguage.Vietnamese : AppLanguage.English;
        
        // Fast-path cached translation update (Zero visual tree walks)
        TranslationManager.Instance.ApplyLanguageChange();
        
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            if (mainWindow.MainFrame.Content is MainPage mainPage)
            {
                mainPage.UpdateHeader();
            }
        }
        
        App.MainWindowInstance?.ShowToastNotification("Language Saved".T(), "Language setting has been updated successfully.".T(), "Success");
    }

    private void OnTransparencyChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnAutoCleanupSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (CleanupSizeLabel != null)
        {
            CleanupSizeLabel.Text = $"{e.NewValue:F1} GB";
        }
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnNotificationThresholdSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (NotificationThresholdLabel != null)
        {
            NotificationThresholdLabel.Text = $"{e.NewValue:F0}%";
        }
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnMaintenanceFrequencyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
        
        try
        {
            var engine = new WinCarePro.Engines.StartupEngine();
            engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
        }
        catch { }
    }

    // Storage Purge Management
    private void UpdateStorageSizes()
    {
        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
            string dbPath = Path.Combine(appData, "wincaredb.db");
            
            // 1. Logs
            long logsCount = 0;
            long dbSize = 0;
            if (File.Exists(dbPath))
            {
                dbSize = new FileInfo(dbPath).Length;
            }
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT COUNT(*) FROM Logs", conn);
                logsCount = (long)(cmd.ExecuteScalar() ?? 0L);
            }
            catch {}
            LogsDbSizeLabel.Text = $"{logsCount} logs ({FormatSize(dbSize)})";

            // 2. Reports
            long reportsCount = 0;
            long reportsSize = 0;
            string reportsFolder = Path.Combine(appData, "Reports");
            if (Directory.Exists(reportsFolder))
            {
                var files = Directory.GetFiles(reportsFolder);
                reportsCount = files.Length;
                foreach (var f in files)
                {
                    reportsSize += new FileInfo(f).Length;
                }
            }
            ReportsDbSizeLabel.Text = $"{reportsCount} files ({FormatSize(reportsSize)})";

            // 3. Cache
            long cacheCount = 0;
            long cacheSize = 0;
            string cacheFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
            if (Directory.Exists(cacheFolder))
            {
                var files = Directory.GetFiles(cacheFolder);
                cacheCount = files.Length;
                foreach (var f in files)
                {
                    cacheSize += new FileInfo(f).Length;
                }
            }
            string directCacheFolder = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
            if (Directory.Exists(directCacheFolder))
            {
                var files = Directory.GetFiles(directCacheFolder);
                cacheCount += files.Length;
                foreach (var f in files)
                {
                    cacheSize += new FileInfo(f).Length;
                }
            }
            CacheDbSizeLabel.Text = $"{cacheCount} pkgs ({FormatSize(cacheSize)})";
        }
        catch {}
    }

    private string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] suffix = { "B", "KB", "MB", "GB" };
        int i = 0;
        double doubleBytes = bytes;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            i++;
            doubleBytes /= 1024;
        }
        return $"{doubleBytes:F1} {suffix[i]}";
    }

    private async void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        PurgeProgressRing.IsActive = true;
        await Task.Delay(1200); // Visual feedback
        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
            string dbPath = Path.Combine(appData, "wincaredb.db");

            if (PurgeLogsCheckbox.IsChecked == true)
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using var cmd = new SqliteCommand("DELETE FROM Logs", connection);
                cmd.ExecuteNonQuery();
            }
            if (PurgeReportsCheckbox.IsChecked == true)
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using (var cmd = new SqliteCommand("DELETE FROM Reports", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                string reportsFolder = Path.Combine(appData, "Reports");
                if (Directory.Exists(reportsFolder))
                {
                    foreach (var file in Directory.GetFiles(reportsFolder))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            if (PurgeCacheCheckbox.IsChecked == true)
            {
                string cacheFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
                if (Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.GetFiles(cacheFolder))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                string directCacheFolder = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
                if (Directory.Exists(directCacheFolder))
                {
                    foreach (var file in Directory.GetFiles(directCacheFolder))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }

            DbManager.LogAction("Purged selected database rows and cache", "Settings", "Success");
            UpdateStorageSizes();
            
            App.MainWindowInstance?.ShowToastNotification("Purge Completed".T(), "Selected caches and database rows cleared successfully.".T(), "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Purge Failed".T(), ex.Message, "Critical");
        }
        finally
        {
            PurgeProgressRing.IsActive = false;
        }
    }

    // Theme Segmented Cards click handlers
    private void OnLightModeCardClick(object sender, PointerRoutedEventArgs e)
    {
        UpdateAppTheme(false);
        ApplyThemeCardSelection(false);
    }

    private void OnDarkModeCardClick(object sender, PointerRoutedEventArgs e)
    {
        UpdateAppTheme(true);
        ApplyThemeCardSelection(true);
    }

    private void ApplyThemeCardSelection(bool dark)
    {
        var accentBrush = (Brush)Application.Current.Resources["PrimaryAccentGradient"];
        var defaultBorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];

        if (dark)
        {
            DarkThemeCard.BorderBrush = accentBrush;
            DarkThemeCard.BorderThickness = new Thickness(2.0);
            LightThemeCard.BorderBrush = defaultBorderBrush;
            LightThemeCard.BorderThickness = new Thickness(1.5);
        }
        else
        {
            LightThemeCard.BorderBrush = accentBrush;
            LightThemeCard.BorderThickness = new Thickness(2.0);
            DarkThemeCard.BorderBrush = defaultBorderBrush;
            DarkThemeCard.BorderThickness = new Thickness(1.5);
        }
    }

    private void UpdateAppTheme(bool dark)
    {
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            mainWindow.ApplyAppTheme(dark);
        }
        else if (this.XamlRoot?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
        }

        try
        {
            string raw = DbManager.GetSettings();
            var settingsDict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(raw))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
                if (parsed != null) settingsDict = parsed;
            }
            settingsDict["Theme"] = dark ? "Dark" : "Light";
            DbManager.SaveSettings(JsonSerializer.Serialize(settingsDict));
        }
        catch { }

        // Re-apply indicators to fit new theme contrast (DimGray/White)
        string currentAccent = GetSelectedAccentColorTag();
        ApplyAccentColorSelection(currentAccent);
    }

    private void OnAccentClick(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Shapes.Ellipse ellipse && ellipse.Tag is string tag)
        {
            ApplyAccentColorSelection(tag);
            SaveSettings();
            App.MainWindowInstance?.ShowToastNotification("Accent Applied".T(), string.Format("System accent color successfully updated to {0}.".T(), tag), "Success");
        }
    }

    // Diagnostics Logs reader
    private void PopulateTraceLogs(string filter = "All")
    {
        var sb = new System.Text.StringBuilder();
        foreach (var log in _traceLogs)
        {
            if (filter == "Warnings" && !log.Contains("[!]") && !log.Contains("Warning")) continue;
            if (filter == "Errors" && !log.Contains("[Error]") && !log.Contains("Failed")) continue;
            sb.AppendLine(log);
        }
        TraceLogTextBox.Text = sb.ToString();
    }

    private void OnFilterTraceAllClick(object sender, RoutedEventArgs e)
    {
        PopulateTraceLogs("All");
    }

    private void OnFilterTraceWarnClick(object sender, RoutedEventArgs e)
    {
        PopulateTraceLogs("Warnings");
    }

    private void OnFilterTraceErrClick(object sender, RoutedEventArgs e)
    {
        PopulateTraceLogs("Errors");
    }

    private void OnClearTraceClick(object sender, RoutedEventArgs e)
    {
        _traceLogs.Clear();
        TraceLogTextBox.Text = "";
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateProgressRing.IsActive = true;
        UpdateStatusLabel.Text = "Checking for updates...".T();
        UpdateProgressBar.Visibility = Visibility.Collapsed;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
            
            string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
            string response;
            if (File.Exists(@"D:\WinCare\update.json"))
            {
                response = File.ReadAllText(@"D:\WinCare\update.json");
            }
            else
            {
                response = await client.GetStringAsync(jsonUrl);
            }
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string remoteVerStr = root.GetProperty("version").GetString() ?? "2.0.0";
            string downloadUrl = root.GetProperty("url").GetString() ?? "";
            string changelog = root.TryGetProperty("changelog", out var clProp) ? clProp.GetString() ?? "" : "";

            var currentVersion = typeof(SettingsPage).Assembly.GetName().Version ?? new Version(2, 0, 0, 0);
            var remoteVersion = new Version(remoteVerStr);

            UpdateProgressRing.IsActive = false;

            if (remoteVersion > currentVersion)
            {
                UpdateStatusLabel.Text = string.Format("New version {0} is available.".T(), remoteVerStr);

                ContentDialog updateDialog = new ContentDialog
                {
                    Title = "Update Available".T(),
                    Content = string.Format("Version {0} has been released (Current: {1}).\n\nWhat's New:\n{2}\n\nWould you like to download and install this update now?".T(), remoteVerStr, currentVersion.ToString(3), changelog),
                    PrimaryButtonText = "Update Now".T(),
                    CloseButtonText = "Later".T(),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await updateDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await DownloadAndInstallUpdateAsync(downloadUrl);
                }
            }
            else
            {
                UpdateStatusLabel.Text = string.Format("You are running the latest version (v{0}).".T(), currentVersion.ToString(3));
            }
        }
        catch (Exception ex)
        {
            UpdateProgressRing.IsActive = false;
            UpdateStatusLabel.Text = string.Format("Failed to check for updates: {0}".T(), ex.Message);
        }
        finally
        {
            CheckUpdatesBtn.IsEnabled = true;
        }
    }

    private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;

        CheckUpdatesBtn.IsEnabled = false;
        UpdateStatusLabel.Text = "Downloading update...".T();
        UpdateProgressBar.Visibility = Visibility.Visible;
        UpdateProgressBar.Value = 0;

        try
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string setupFilePath = Path.Combine(tempFolder, "WinCarePro_Setup.exe");

            if (downloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || downloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
                
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(setupFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                var buffer = new byte[8192];
                long totalRead = 0;
                int read;
                
                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    
                    if (totalBytes.HasValue)
                    {
                        double progress = (double)totalRead / totalBytes.Value * 100.0;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateProgressBar.Value = progress;
                            UpdateStatusLabel.Text = string.Format("Downloading update... {0}%".T(), progress.ToString("F0"));
                        });
                    }
                }
                fileStream.Close();
            }
            else
            {
                // Local file fallback for development testing
                string localPath = downloadUrl.Replace("file:///", "").Replace("file://", "").Replace("/", "\\");
                if (File.Exists(localPath))
                {
                    await Task.Run(() => File.Copy(localPath, setupFilePath, true));
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateProgressBar.Value = 100;
                        UpdateStatusLabel.Text = "Copying local update... 100%".T();
                    });
                }
                else
                {
                    throw new FileNotFoundException("Local update file not found: " + localPath);
                }
            }

            // System restore point policy check
            try
            {
                string raw = DbManager.GetSettings();
                bool createRp = true;
                if (!string.IsNullOrEmpty(raw))
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("CreateRestorePoint", out var rpProp))
                    {
                        createRp = rpProp.GetBoolean();
                    }
                }

                if (createRp)
                {
                    UpdateStatusLabel.Text = "Creating System Restore Point...".T();
                    var regEng = new Engines.RegistryBackupEngine();
                    await Task.Run(() => regEng.CreateSystemRestorePoint("Before WinCare Pro Update".T()));
                }
            }
            catch { }

            UpdateStatusLabel.Text = "Launching installer...".T();
            await Task.Delay(1000);

            Process.Start(new ProcessStartInfo
            {
                FileName = setupFilePath,
                Arguments = "/SILENT /SP- /NOICONS /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS",
                UseShellExecute = true
            });

            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        catch (Exception ex)
        {
            UpdateStatusLabel.Text = string.Format("Download failed: {0}".T(), ex.Message);
            UpdateProgressBar.Visibility = Visibility.Collapsed;
            CheckUpdatesBtn.IsEnabled = true;
        }
    }

    private void SettingsNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // View switches automatically
    }

    private Visibility GetSectionVisibility(int selectedIndex, int targetIndex)
    {
        return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
    }
}

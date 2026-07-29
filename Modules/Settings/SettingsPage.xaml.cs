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
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loadingSettings = true; // Guard initialization events from saving settings early
    private Microsoft.UI.Xaml.DispatcherTimer? _saveSettingsDebounceTimer;

    // Shared HttpClient singleton to prevent socket exhaustion from per-call instantiation
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; WinCareProUpdater/1.0)" } }
    };

    public SettingsPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        LoadSettings();
        UpdateStorageSizes();

        this.Loaded += (s, e) =>
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChangedExternally;
            ThemeManager.Instance.ThemeChanged += OnThemeChangedExternally;

            // Sync with current theme on load
            bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
            ApplyThemeCardSelection(isDark);

            string currentAccent = GetSelectedAccentColorTag();
            ApplyAccentColorSelection(currentAccent);
        };

        this.Unloaded += (s, e) =>
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChangedExternally;
        };
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
            Task.Run(() => DbManager.SaveSettings(json));

            // Apply modifications immediately
            ApplyRuntimeSettings(profile);
        }
        catch { }
    }

    private void QueueSaveSettings()
    {
        if (_loadingSettings) return;

        if (_saveSettingsDebounceTimer == null)
        {
            _saveSettingsDebounceTimer = new Microsoft.UI.Xaml.DispatcherTimer();
            _saveSettingsDebounceTimer.Interval = TimeSpan.FromMilliseconds(400);
            _saveSettingsDebounceTimer.Tick += (s, e) =>
            {
                _saveSettingsDebounceTimer.Stop();
                SaveSettings();
            };
        }
        else
        {
            _saveSettingsDebounceTimer.Stop();
        }

        _saveSettingsDebounceTimer.Start();
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

        if (CheckDefault != null) CheckDefault.Visibility = Visibility.Collapsed;
        if (CheckGreen != null) CheckGreen.Visibility = Visibility.Collapsed;
        if (CheckPurple != null) CheckPurple.Visibility = Visibility.Collapsed;
        if (CheckPink != null) CheckPink.Visibility = Visibility.Collapsed;
        if (CheckAmber != null) CheckAmber.Visibility = Visibility.Collapsed;

        var selectedEllipse = (tag ?? "default").ToLower() switch
        {
            "green" => AccentGreen,
            "purple" => AccentPurple,
            "pink" => AccentPink,
            "amber" => AccentAmber,
            _ => AccentDefault
        };

        var selectedCheck = (tag ?? "default").ToLower() switch
        {
            "green" => CheckGreen,
            "purple" => CheckPurple,
            "pink" => CheckPink,
            "amber" => CheckAmber,
            _ => CheckDefault
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

        if (selectedCheck != null)
        {
            selectedCheck.Visibility = Visibility.Visible;
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
        if (TransparencyValueLabel != null)
        {
            TransparencyValueLabel.Text = $"{e.NewValue:F0}%";
        }
        if (_loadingSettings) return;

        // Apply transparency visually in real-time
        if (App.MainWindowInstance != null)
        {
            App.MainWindowInstance.ApplyTransparency(e.NewValue);
        }

        QueueSaveSettings();
    }

    private void OnAutoCleanupSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (CleanupSizeLabel != null)
        {
            CleanupSizeLabel.Text = $"{e.NewValue:F1} GB";
        }
        if (_loadingSettings) return;
        QueueSaveSettings();
    }

    private void OnNotificationThresholdSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (NotificationThresholdLabel != null)
        {
            NotificationThresholdLabel.Text = $"{e.NewValue:F0}%";
        }
        if (_loadingSettings) return;
        QueueSaveSettings();
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
        Task.Run(() =>
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
                string logsText = $"{logsCount} logs ({FormatHelper.FormatBytes(dbSize)})";

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
                string reportsText = $"{reportsCount} files ({FormatHelper.FormatBytes(reportsSize)})";

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
                string cacheText = $"{cacheCount} pkgs ({FormatHelper.FormatBytes(cacheSize)})";

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (LogsDbSizeLabel != null) LogsDbSizeLabel.Text = logsText;
                    if (ReportsDbSizeLabel != null) ReportsDbSizeLabel.Text = reportsText;
                    if (CacheDbSizeLabel != null) CacheDbSizeLabel.Text = cacheText;
                });
            }
            catch {}
        });
    }

    // FormatSize centralized to FormatHelper.FormatBytes

    private async void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        var purgeBtn = sender as Button;
        if (purgeBtn != null) purgeBtn.IsEnabled = false;

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
            if (purgeBtn != null) purgeBtn.IsEnabled = true;
        }
    }

    // Theme Segmented Cards click handlers
    private void OnLightModeCardClick(object sender, PointerRoutedEventArgs e)
    {
        UpdateAppTheme(false);
    }

    private void OnDarkModeCardClick(object sender, PointerRoutedEventArgs e)
    {
        UpdateAppTheme(true);
    }

    private void OnThemeChangedExternally(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
            ApplyThemeCardSelection(isDark);

            // Re-apply indicators to fit new theme contrast (DimGray/White)
            string currentAccent = GetSelectedAccentColorTag();
            ApplyAccentColorSelection(currentAccent);
        });
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
            string themeJson = JsonSerializer.Serialize(settingsDict);
            Task.Run(() => DbManager.SaveSettings(themeJson));
        }
        catch { }

        // Re-apply indicators to fit new theme contrast (DimGray/White)
        string currentAccent = GetSelectedAccentColorTag();
        ApplyAccentColorSelection(currentAccent);
    }

    private void OnAccentClick(object sender, PointerRoutedEventArgs e)
    {
        string? tag = null;
        if (sender is FrameworkElement element)
        {
            tag = element.Tag?.ToString();
        }

        if (!string.IsNullOrEmpty(tag))
        {
            ApplyAccentColorSelection(tag);
            SaveSettings();
            App.MainWindowInstance?.ShowToastNotification("Accent Applied".T(), string.Format("System accent color successfully updated to {0}.".T(), tag), "Success");
        }
    }



    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateProgressRing.IsActive = true;
        UpdateStatusLabel.Text = "Checking for updates...".T();
        UpdateProgressBar.Visibility = Visibility.Collapsed;

        try
        {
            await CheckForUpdatesInternalAsync();
        }
        catch (System.IO.FileNotFoundException fnfEx)
        {
            // System.Net.Http assembly not found — deployment/packaging issue
            UpdateProgressRing.IsActive = false;
            UpdateStatusLabel.Text = string.Format("Network library unavailable: {0}".T(), fnfEx.FileName ?? fnfEx.Message);
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

    private async Task CheckForUpdatesInternalAsync()
    {
        string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
        string response = await _httpClient.GetStringAsync(jsonUrl);
        
        bool betaEnabled = false;
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var docSettings = JsonDocument.Parse(raw);
                if (docSettings.RootElement.TryGetProperty("BetaUpdates", out var betaProp))
                {
                    betaEnabled = betaProp.GetBoolean();
                }
            }
        }
        catch { }

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        
        string remoteVerStr;
        string downloadUrl;
        string changelog;

        if (betaEnabled && root.TryGetProperty("beta_version", out var betaVerProp))
        {
            remoteVerStr = betaVerProp.GetString() ?? "2.0.0";
            downloadUrl = root.TryGetProperty("beta_url", out var betaUrlProp) ? betaUrlProp.GetString() ?? "" : "";
            changelog = root.TryGetProperty("beta_changelog", out var betaClProp) ? betaClProp.GetString() ?? "" : "";
        }
        else
        {
            remoteVerStr = root.GetProperty("version").GetString() ?? "2.0.0";
            downloadUrl = root.GetProperty("url").GetString() ?? "";
            changelog = root.TryGetProperty("changelog", out var clProp) ? clProp.GetString() ?? "" : "";
        }

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
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
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
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
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

            // Verify digital signature of the downloaded update installer
            UpdateStatusLabel.Text = "Verifying digital signature...".T();
            bool isSignatureValid = await Task.Run(() => VerifyDigitalSignature(setupFilePath));
            if (!isSignatureValid)
            {
#if DEBUG
                DbManager.LogAction("Update package lacks digital signature (Bypassed in DEBUG mode)", "Settings", "Warning");
#else
                try { if (File.Exists(setupFilePath)) File.Delete(setupFilePath); } catch {}
                throw new System.Security.SecurityException("The downloaded update package does not have a valid or trusted digital signature. Update aborted for safety.".T());
#endif
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

    private bool VerifyDigitalSignature(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
        try
        {
#pragma warning disable SYSLIB0057
            using (var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(filePath))
            {
                return cert.Verify();
            }
#pragma warning restore SYSLIB0057
        }
        catch
        {
            return false;
        }
    }

    private void OnBrowsePluginsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro/wiki/Plugins",
                UseShellExecute = true
            });
            DbManager.LogAction("Launched verified plugins browser URL", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
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

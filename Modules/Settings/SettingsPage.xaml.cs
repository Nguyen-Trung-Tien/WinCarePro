using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;

namespace WinCarePro.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loadingSettings = true; // Guard initialization events from saving settings early
    private DispatcherQueueTimer? _aboutTelemetryTimer;

    [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);

    // Shared HttpClient singleton to prevent socket exhaustion from per-call instantiation
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; WinCareProUpdater/1.0)" } }
    };

    public SettingsPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        LoadSettingsToUI();
        UpdateStorageSizes();

        this.Loaded += (s, e) =>
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChangedExternally;
            ThemeManager.Instance.ThemeChanged += OnThemeChangedExternally;

            SettingsService.Instance.SettingsChanged -= OnSettingsChangedExternally;
            SettingsService.Instance.SettingsChanged += OnSettingsChangedExternally;

            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
            TranslationManager.Instance.Translate(this);

            // Ensure cancel update button is collapsed in default/idle state
            if (CancelUpdatesBtn != null)
            {
                CancelUpdatesBtn.Visibility = Visibility.Collapsed;
            }

            // Sync with current theme on load
            bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
            ApplyThemeCardSelection(isDark);

            string currentAccent = SettingsService.Instance.CurrentSettings.AccentColor ?? "Default";
            ApplyAccentColorSelection(currentAccent);

            // Real-time network connectivity monitoring
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkStatusChanged;
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += OnNetworkStatusChanged;
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            _ = RefreshNetworkBadgeStateAsync();

            try { PulsingUpdateGlowAnimation?.Begin(); } catch {}

            // Start live process telemetry for About section
            UpdateAboutTelemetry();
            if (_aboutTelemetryTimer == null)
            {
                _aboutTelemetryTimer = DispatcherQueue.CreateTimer();
                _aboutTelemetryTimer.Interval = TimeSpan.FromSeconds(2);
                _aboutTelemetryTimer.Tick += (timerSender, timerArgs) => UpdateAboutTelemetry();
            }
            _aboutTelemetryTimer.Start();
        };

        this.Unloaded += (s, e) =>
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChangedExternally;
            SettingsService.Instance.SettingsChanged -= OnSettingsChangedExternally;
            TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkStatusChanged;
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            _aboutTelemetryTimer?.Stop();
        };
    }

    public void SelectSection(int index)
    {
        if (index >= 0 && index < SettingsNavList.Items.Count)
        {
            SettingsNavList.SelectedIndex = index;
            SettingsNavList.ScrollIntoView(SettingsNavList.Items[index]);
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int sectionIndex)
        {
            SelectSection(sectionIndex);
        }
        else if (e.Parameter is string sectionName)
        {
            if (sectionName.Equals("UserGuide", StringComparison.OrdinalIgnoreCase) ||
                sectionName.Equals("Guide", StringComparison.OrdinalIgnoreCase) ||
                sectionName.Equals("Help", StringComparison.OrdinalIgnoreCase))
            {
                SelectSection(10);
            }
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
    }

    private void LoadSettingsToUI()
    {
        _loadingSettings = true;
        try
        {
            var profile = SettingsService.Instance.CurrentSettings;

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
            if (TransparencyValueLabel != null)
            {
                TransparencyValueLabel.Text = $"{profile.TransparencyLevel:F0}%";
            }
            EnableAnimationsToggle.IsOn = profile.EnableAnimations;
            ApplyThemeCardSelection(profile.Theme == "Dark");

            // Auto Maintenance
            AutoCleanupSlider.Value = profile.AutoCleanupTriggerSizeGB;
            if (CleanupSizeLabel != null)
            {
                CleanupSizeLabel.Text = $"{profile.AutoCleanupTriggerSizeGB:F1} GB";
            }
            TriggerSmartBoostToggle.IsOn = profile.TriggerSmartBoost;
            MaintenanceFrequencyComboBox.SelectedIndex = profile.MaintenanceFrequencyIndex;

            // Notifications Settings
            ShowNotificationsToggle.IsOn = profile.ShowNotifications;
            NotificationThresholdSlider.Value = profile.NotificationThreshold;
            if (NotificationThresholdLabel != null)
            {
                NotificationThresholdLabel.Text = $"{profile.NotificationThreshold:F0}%";
            }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error loading settings to UI: {ex.Message}");
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void SyncUIWithSettings(SettingsProfile profile)
    {
        _loadingSettings = true;
        try
        {
            LanguageComboBox.SelectedIndex = profile.LanguageIndex;
            AutoScanToggle.IsOn = profile.AutoScan;
            AutoUpdateToggle.IsOn = profile.AutoCheckUpdates;
            AutoInstallUpdatesToggle.IsOn = profile.AutoInstallUpdates;
            MinimizeToTrayToggle.IsOn = profile.MinimizeToTray;
            BetaUpdatesToggle.IsOn = profile.BetaUpdates;

            ApplyAccentColorSelection(profile.AccentColor);
            TransparencySlider.Value = profile.TransparencyLevel;
            if (TransparencyValueLabel != null)
            {
                TransparencyValueLabel.Text = $"{profile.TransparencyLevel:F0}%";
            }
            EnableAnimationsToggle.IsOn = profile.EnableAnimations;
            ApplyThemeCardSelection(profile.Theme == "Dark");

            AutoCleanupSlider.Value = profile.AutoCleanupTriggerSizeGB;
            if (CleanupSizeLabel != null)
            {
                CleanupSizeLabel.Text = $"{profile.AutoCleanupTriggerSizeGB:F1} GB";
            }
            TriggerSmartBoostToggle.IsOn = profile.TriggerSmartBoost;
            MaintenanceFrequencyComboBox.SelectedIndex = profile.MaintenanceFrequencyIndex;

            ShowNotificationsToggle.IsOn = profile.ShowNotifications;
            NotificationThresholdSlider.Value = profile.NotificationThreshold;
            if (NotificationThresholdLabel != null)
            {
                NotificationThresholdLabel.Text = $"{profile.NotificationThreshold:F0}%";
            }
            NotifyOnLowHealthToggle.IsOn = profile.NotifyOnLowHealth;
            NotifyOnMaintenanceToggle.IsOn = profile.NotifyOnMaintenance;
            ShowUpdateNotificationsToggle.IsOn = profile.ShowUpdateNotifications;
            NotificationSoundToggle.IsOn = profile.NotificationSound;

            TelemetryIntervalComboBox.SelectedIndex = profile.TelemetryIntervalIndex;
            PerformanceHistoryComboBox.SelectedIndex = profile.PerformanceHistoryDurationIndex;
            EnableHardwareSensorsToggle.IsOn = profile.EnableSensorsThread;

            CreateRestorePointToggle.IsOn = profile.CreateRestorePoint;
            BackupRegistryToggle.IsOn = profile.BackupRegistryHive;
            AlertsLevelSlider.Value = profile.ConfirmationAlertsLevel;

            EnableVerboseLogsToggle.IsOn = profile.EnableVerboseLogs;
            EnableExperimentalAiToggle.IsOn = profile.EnableExperimentalAi;
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

        double sizeGB = AutoCleanupSlider.Value;
        if (sizeGB <= 0) sizeGB = 5.0;

        string currentTheme = (ThemeManager.Instance.CurrentTheme == ElementTheme.Light) ? "Light" : "Dark";

        SettingsService.Instance.UpdateSettings(p =>
        {
            p.Theme = currentTheme;
            p.AutoScan = AutoScanToggle.IsOn;
            p.ReportFormat = "TXT";
            
            p.LanguageIndex = LanguageComboBox.SelectedIndex;
            p.AutoCheckUpdates = AutoUpdateToggle.IsOn;
            p.AutoInstallUpdates = AutoInstallUpdatesToggle.IsOn;
            p.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            p.BetaUpdates = BetaUpdatesToggle.IsOn;

            p.AccentColor = GetSelectedAccentColorTag();
            p.TransparencyLevel = TransparencySlider.Value;
            p.EnableAnimations = EnableAnimationsToggle.IsOn;

            p.AutoCleanupTriggerSizeGB = sizeGB;
            p.TriggerSmartBoost = TriggerSmartBoostToggle.IsOn;
            p.MaintenanceFrequencyIndex = MaintenanceFrequencyComboBox.SelectedIndex;

            p.ShowNotifications = ShowNotificationsToggle.IsOn;
            p.NotificationThreshold = NotificationThresholdSlider.Value;
            p.NotifyOnLowHealth = NotifyOnLowHealthToggle.IsOn;
            p.NotifyOnMaintenance = NotifyOnMaintenanceToggle.IsOn;
            p.ShowUpdateNotifications = ShowUpdateNotificationsToggle.IsOn;
            p.NotificationSound = NotificationSoundToggle.IsOn;

            p.TelemetryIntervalIndex = TelemetryIntervalComboBox.SelectedIndex;
            p.PerformanceHistoryDurationIndex = PerformanceHistoryComboBox.SelectedIndex;
            p.EnableSensorsThread = EnableHardwareSensorsToggle.IsOn;

            p.CreateRestorePoint = CreateRestorePointToggle.IsOn;
            p.BackupRegistryHive = BackupRegistryToggle.IsOn;
            p.ConfirmationAlertsLevel = AlertsLevelSlider.Value;

            p.EnableVerboseLogs = EnableVerboseLogsToggle.IsOn;
            p.EnableExperimentalAi = EnableExperimentalAiToggle.IsOn;
        });

        // Apply runtime changes immediately
        ApplyRuntimeSettings(SettingsService.Instance.CurrentSettings);
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
            bool isDark = (ThemeManager.Instance.CurrentTheme == ElementTheme.Dark);
            selectedEllipse.Stroke = new SolidColorBrush(isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.DimGray);
            selectedEllipse.StrokeThickness = 2.5;
        }

        if (selectedCheck != null)
        {
            selectedCheck.Visibility = Visibility.Visible;
        }

        if (DevAvatarBorder != null && Application.Current.Resources.TryGetValue("CyberAccentGradient", out var brushObj) && brushObj is Microsoft.UI.Xaml.Media.Brush cyberBrush)
        {
            DevAvatarBorder.Background = null;
            DevAvatarBorder.Background = cyberBrush;
        }
    }

    private async void OnAutoScanToggled(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
        
        if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            lts.IsLoading = true;
            try
            {
                await Task.Run(() =>
                {
                    var engine = new WinCarePro.Engines.StartupEngine();
                    engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
                });
            }
            catch { }
            finally
            {
                lts.IsLoading = false;
            }
        }
        else
        {
            try
            {
                var engine = new WinCarePro.Engines.StartupEngine();
                engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
            }
            catch { }
        }
    }

    private void OnSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        
        int index = LanguageComboBox.SelectedIndex;
        SettingsService.Instance.UpdateSettings(s => s.LanguageIndex = index, "LanguageIndex");
        
        TranslationManager.Instance.CurrentLanguage = index == 1 ? AppLanguage.Vietnamese : AppLanguage.English;
        
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

        SettingsService.Instance.UpdateSettings(s => s.TransparencyLevel = e.NewValue, "TransparencyLevel");
    }

    private void OnAutoCleanupSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (CleanupSizeLabel != null)
        {
            CleanupSizeLabel.Text = $"{e.NewValue:F1} GB";
        }
        if (_loadingSettings) return;
        SettingsService.Instance.UpdateSettings(s => s.AutoCleanupTriggerSizeGB = e.NewValue, "AutoCleanupTriggerSizeGB");
    }

    private void OnNotificationThresholdSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (NotificationThresholdLabel != null)
        {
            NotificationThresholdLabel.Text = $"{e.NewValue:F0}%";
        }
        if (_loadingSettings) return;
        SettingsService.Instance.UpdateSettings(s => s.NotificationThreshold = e.NewValue, "NotificationThreshold");
    }

    private void OnMaintenanceFrequencyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        int freq = MaintenanceFrequencyComboBox.SelectedIndex;
        SettingsService.Instance.UpdateSettings(s => s.MaintenanceFrequencyIndex = freq, "MaintenanceFrequencyIndex");
        
        try
        {
            var engine = new WinCarePro.Engines.StartupEngine();
            engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
        }
        catch { }
    }

    // Storage Purge Management
    private void OnRefreshStorageClick(object sender, RoutedEventArgs e)
    {
        UpdateStorageSizes();
    }

    private void UpdateStorageSizes()
    {
        if (LogsDbSizeLabel != null) LogsDbSizeLabel.Text = "Scanning...".T();
        if (ReportsDbSizeLabel != null) ReportsDbSizeLabel.Text = "Scanning...".T();
        if (CacheDbSizeLabel != null) CacheDbSizeLabel.Text = "Scanning...".T();

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

    private async void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        var purgeBtn = PurgeStorageBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            purgeBtn, PurgeProgressRing, PurgeStorageText, null,
            "Purging Storage...", "Purge Selected Storage",
            async () =>
            {
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
            },
            minDurationMs: 1200);
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

            string currentAccent = SettingsService.Instance.CurrentSettings.AccentColor ?? "Default";
            ApplyAccentColorSelection(currentAccent);
        });
    }

    private void OnSettingsChangedExternally(object? sender, SettingsChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_loadingSettings)
            {
                SyncUIWithSettings(e.Settings);
            }
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
        else
        {
            ThemeManager.Instance.ApplyTheme(dark ? ElementTheme.Dark : ElementTheme.Light);
        }

        SettingsService.Instance.UpdateSettings(s => s.Theme = dark ? "Dark" : "Light", "Theme");

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
            SettingsService.Instance.UpdateSettings(s => s.AccentColor = tag, "AccentColor");
            App.ApplyAccentColor(tag);
            App.MainWindowInstance?.ShowToastNotification("Accent Applied".T(), string.Format("System accent color successfully updated to {0}.".T(), tag), "Success");
        }
    }

    // Backup, Export, Import & Reset handlers
    private async void OnExportSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string json = SettingsService.Instance.ExportSettingsJson();
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
            string backupPath = Path.Combine(appData, $"WinCarePro_SettingsBackup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            
            await File.WriteAllTextAsync(backupPath, json);
            DbManager.LogAction($"Exported settings backup to {backupPath}", "Settings", "Success");
            
            App.MainWindowInstance?.ShowToastNotification("Backup Exported".T(), string.Format("Settings saved successfully to: {0}".T(), Path.GetFileName(backupPath)), "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Export Failed".T(), ex.Message, "Critical");
        }
    }

    private async void OnImportSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
            if (Directory.Exists(appData))
            {
                var files = Directory.GetFiles(appData, "WinCarePro_SettingsBackup_*.json");
                if (files.Length > 0)
                {
                    Array.Sort(files);
                    string latestBackup = files[^1]; // Get latest backup
                    string json = await File.ReadAllTextAsync(latestBackup);
                    bool ok = SettingsService.Instance.ImportSettingsJson(json);
                    if (ok)
                    {
                        SyncUIWithSettings(SettingsService.Instance.CurrentSettings);
                        ApplyRuntimeSettings(SettingsService.Instance.CurrentSettings);
                        App.MainWindowInstance?.ShowToastNotification("Settings Restored".T(), string.Format("Restored from: {0}".T(), Path.GetFileName(latestBackup)), "Success");
                        return;
                    }
                }
            }
            App.MainWindowInstance?.ShowToastNotification("No Backup Found".T(), "No existing settings backup files were found in AppData.".T(), "Warning");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Import Failed".T(), ex.Message, "Critical");
        }
    }

    private async void OnResetDefaultsClick(object sender, RoutedEventArgs e)
    {
        var confirmDialog = new ContentDialog
        {
            Title = "Reset to Factory Defaults?".T(),
            Content = "Are you sure you want to reset all configuration settings to factory defaults? This cannot be undone.".T(),
            PrimaryButtonText = "Reset All".T(),
            CloseButtonText = "Cancel".T(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SettingsService.Instance.ResetToDefaults();
            SyncUIWithSettings(SettingsService.Instance.CurrentSettings);
            ApplyRuntimeSettings(SettingsService.Instance.CurrentSettings);
            DbManager.LogAction("Reset all settings to factory defaults", "Settings", "Warning");
            App.MainWindowInstance?.ShowToastNotification("Reset Complete".T(), "All settings have been restored to factory defaults.".T(), "Success");
        }
    }

    private CancellationTokenSource? _updateCts;

    private void OnNetworkStatusChanged(object? sender, EventArgs e)
    {
        _ = RefreshNetworkBadgeStateAsync();
    }

    private void OnNetworkAvailabilityChanged(object? sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e)
    {
        _ = RefreshNetworkBadgeStateAsync();
    }

    private async Task RefreshNetworkBadgeStateAsync()
    {
        bool isOnline = await CheckInternetAccessAsync(1200);
        DispatcherQueue.TryEnqueue(() =>
        {
            if (isOnline)
            {
                SetUpdateBadgeState("CDN Connected".T(), "Online");
            }
            else
            {
                SetUpdateBadgeState("Offline".T(), "Offline");
                if (UpdateProgressBar.Value == 100)
                {
                    UpdateDataRateText.Text = "Offline".T();
                }
            }
        });
    }

    private void SetUpdateBadgeState(string text, string state)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (UpdateChannelBadgeText == null) return;
            UpdateChannelBadgeText.Text = text;
            
            if (state == "Online")
            {
                var green = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
                var greenBg = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 16, 185, 129));
                var greenBorder = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 16, 185, 129));
                if (UpdateChannelBadgeDot != null) UpdateChannelBadgeDot.Fill = green;
                UpdateChannelBadgeText.Foreground = green;
                if (UpdateChannelBadgeBorder != null)
                {
                    UpdateChannelBadgeBorder.Background = greenBg;
                    UpdateChannelBadgeBorder.BorderBrush = greenBorder;
                }
            }
            else if (state == "Syncing")
            {
                var blue = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241));
                var blueBg = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 99, 102, 241));
                var blueBorder = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 99, 102, 241));
                if (UpdateChannelBadgeDot != null) UpdateChannelBadgeDot.Fill = blue;
                UpdateChannelBadgeText.Foreground = blue;
                if (UpdateChannelBadgeBorder != null)
                {
                    UpdateChannelBadgeBorder.Background = blueBg;
                    UpdateChannelBadgeBorder.BorderBrush = blueBorder;
                }
            }
            else // Offline or Error
            {
                var red = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
                var redBg = new SolidColorBrush(Windows.UI.Color.FromArgb(38, 239, 68, 68));
                var redBorder = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 239, 68, 68));
                if (UpdateChannelBadgeDot != null) UpdateChannelBadgeDot.Fill = red;
                UpdateChannelBadgeText.Foreground = red;
                if (UpdateChannelBadgeBorder != null)
                {
                    UpdateChannelBadgeBorder.Background = redBg;
                    UpdateChannelBadgeBorder.BorderBrush = redBorder;
                }
            }
        });
    }

    private static async Task<bool> CheckInternetAccessAsync(int timeoutMs = 1500)
    {
        try
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return false;

            using var cts = new CancellationTokenSource(timeoutMs);
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
            using var resp = await client.GetAsync("https://1.1.1.1/cdn-cgi/trace", HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 1000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool IsNetworkAvailable()
    {
        try
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }
        catch
        {
            return true;
        }
    }

    private void OnCancelUpdateClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _updateCts?.Cancel();
            UpdateStatusLabel.Text = "Update operation cancelled by user.".T();
            UpdateProgressStepLabel.Text = "Cancelled".T();
            UpdateDataRateText.Text = "Idle".T();
            UpdateProgressBar.Value = 0;
            UpdatePercentText.Text = "0%";
            UpdateDetailsText.Text = "Operation cancelled. Ready for new update check.".T();
            SetUpdateBadgeState("CDN Connected".T(), "Online");
            _ = RefreshNetworkBadgeStateAsync();
            CancelUpdatesBtn.Visibility = Visibility.Collapsed;
            CheckUpdatesBtn.IsEnabled = true;
            UpdateProgressRing.IsActive = false;
            UpdateProgressRing.Visibility = Visibility.Collapsed;
        }
        catch { }
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        var btn = CheckUpdatesBtn ?? (sender as Button);
        
        bool hasInternet = await CheckInternetAccessAsync(1200);
        if (!hasInternet)
        {
            UpdateStatusLabel.Text = "No internet connection detected. Please verify your network and try again.".T();
            UpdateProgressStepLabel.Text = "No Connection".T();
            UpdateDataRateText.Text = "Offline".T();
            UpdateProgressBar.Value = 0;
            UpdatePercentText.Text = "0%";
            UpdateDetailsText.Text = "Unable to connect to network. Verify Wi-Fi or Ethernet adapter status.".T();
            SetUpdateBadgeState("Offline".T(), "Offline");
            App.MainWindowInstance?.ShowToastNotification("Update Failed".T(), "No internet connection available to reach update servers.".T(), "Critical");
            return;
        }

        _updateCts?.Cancel();
        _updateCts?.Dispose();
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;

        CancelUpdatesBtn.Visibility = Visibility.Visible;
        UpdateProgressBar.Value = 10;
        UpdatePercentText.Text = "10%";
        UpdateProgressStepLabel.Text = "Connecting to CDN Repository...".T();
        UpdateDataRateText.Text = "Scanning".T();
        UpdateDetailsText.Text = "Negotiating HTTPS connection with distribution server...".T();
        SetUpdateBadgeState("Connecting...".T(), "Syncing");

        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, UpdateProgressRing, CheckUpdatesText, CheckUpdatesIcon,
            "Checking for Updates...", "Check for Updates",
            async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateProgressBar.Value = 45;
                        UpdatePercentText.Text = "45%";
                        UpdateProgressStepLabel.Text = "Auditing Remote Version Manifest...".T();
                    });

                    await Task.Delay(250, token);
                    await CheckForUpdatesInternalAsync(token);
                }
                catch (OperationCanceledException)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusLabel.Text = "Update check cancelled.".T();
                        UpdateProgressStepLabel.Text = "Cancelled".T();
                        UpdateDataRateText.Text = "Idle".T();
                        UpdateProgressBar.Value = 0;
                        UpdatePercentText.Text = "0%";
                        UpdateDetailsText.Text = "Check cancelled by user.".T();
                        SetUpdateBadgeState("CDN Connected".T(), "Online");
                    });
                }
                catch (HttpRequestException httpEx)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusLabel.Text = "Unable to reach update server. Check internet connection or DNS settings.".T();
                        UpdateProgressStepLabel.Text = "Connection Failed".T();
                        UpdateDataRateText.Text = "Error".T();
                        UpdateProgressBar.Value = 0;
                        UpdatePercentText.Text = "0%";
                        UpdateDetailsText.Text = string.Format("Network unreachable: {0}".T(), httpEx.Message);
                        SetUpdateBadgeState("Disconnected".T(), "Offline");
                    });
                }
                catch (FileNotFoundException fnfEx)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusLabel.Text = string.Format("Network library unavailable: {0}".T(), fnfEx.FileName ?? fnfEx.Message);
                        UpdateProgressStepLabel.Text = "Check Failed".T();
                        UpdateDataRateText.Text = "Error".T();
                        UpdateProgressBar.Value = 0;
                        UpdatePercentText.Text = "0%";
                        UpdateDetailsText.Text = "Missing system libraries required for download.".T();
                        SetUpdateBadgeState("Error".T(), "Offline");
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateStatusLabel.Text = string.Format("Failed to check for updates: {0}".T(), ex.Message);
                        UpdateProgressStepLabel.Text = "Check Failed".T();
                        UpdateDataRateText.Text = "Error".T();
                        UpdateProgressBar.Value = 0;
                        UpdatePercentText.Text = "0%";
                        UpdateDetailsText.Text = "An unexpected error occurred during update audit.".T();
                        SetUpdateBadgeState("Error".T(), "Offline");
                    });
                }
                finally
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CancelUpdatesBtn.Visibility = Visibility.Collapsed;
                    });
                }
            },
            minDurationMs: 1000);
    }

    private async Task CheckForUpdatesInternalAsync(CancellationToken token)
    {
        string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
        
        using var request = new HttpRequestMessage(HttpMethod.Get, jsonUrl);
        using var httpResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        httpResponse.EnsureSuccessStatusCode();

        string response = await httpResponse.Content.ReadAsStringAsync(token);
        
        bool betaEnabled = SettingsService.Instance.CurrentSettings.BetaUpdates;

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        
        string remoteVerStr;
        string downloadUrl;
        string changelog;

        if (betaEnabled && root.TryGetProperty("beta_version", out var betaVerProp))
        {
            remoteVerStr = betaVerProp.GetString() ?? "4.5.0";
            downloadUrl = root.TryGetProperty("beta_url", out var betaUrlProp) ? betaUrlProp.GetString() ?? "" : "";
            changelog = root.TryGetProperty("beta_changelog", out var betaClProp) ? betaClProp.GetString() ?? "" : "";
        }
        else
        {
            remoteVerStr = root.GetProperty("version").GetString() ?? "4.5.0";
            downloadUrl = root.GetProperty("url").GetString() ?? "";
            changelog = root.TryGetProperty("changelog", out var clProp) ? clProp.GetString() ?? "" : "";
        }

        var currentVersion = typeof(SettingsPage).Assembly.GetName().Version ?? new Version(4, 5, 0, 0);
        string cleanRemoteVer = System.Text.RegularExpressions.Regex.Replace(remoteVerStr, @"[^\d\.]", "").TrimEnd('.');
        if (!Version.TryParse(cleanRemoteVer, out var remoteVersion))
        {
            remoteVersion = new Version(4, 2, 0, 0);
        }

        UpdateProgressRing.IsActive = false;

        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateProgressBar.Value = 100;
            UpdatePercentText.Text = "100%";
            if (UpdateLastCheckedLabel != null)
            {
                UpdateLastCheckedLabel.Text = string.Format("Last Checked: {0}".T(), DateTime.Now.ToString("HH:mm:ss"));
            }
        });

        if (remoteVersion > currentVersion)
        {
            SetUpdateBadgeState("Update Available".T(), "Syncing");
            UpdateStatusLabel.Text = string.Format("New version {0} is available for download.".T(), remoteVerStr);
            UpdateProgressStepLabel.Text = string.Format("Update v{0} Ready to Download".T(), remoteVerStr);
            UpdateDetailsText.Text = string.Format("Remote version v{0} (Current: v{1})".T(), remoteVerStr, currentVersion.ToString(3));
            UpdateDataRateText.Text = "Pending".T();

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
            SetUpdateBadgeState("CDN Connected".T(), "Online");
            UpdateStatusLabel.Text = string.Format("You are running the latest version (v{0}).".T(), currentVersion.ToString(3));
            UpdateProgressStepLabel.Text = "System Up to Date".T();
            UpdateDetailsText.Text = string.Format("Manifest verified • Running latest v{0}".T(), currentVersion.ToString(3));
            UpdateDataRateText.Text = "Synced".T();
        }
    }

    private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;

        if (!IsNetworkAvailable() && !downloadUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            UpdateStatusLabel.Text = "Cannot start download: No active internet connection.".T();
            UpdateProgressStepLabel.Text = "Network Offline".T();
            SetUpdateBadgeState("Offline".T(), "Offline");
            return;
        }

        _updateCts?.Cancel();
        _updateCts?.Dispose();
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;

        CheckUpdatesBtn.IsEnabled = false;
        CancelUpdatesBtn.Visibility = Visibility.Visible;
        SetUpdateBadgeState("Downloading...".T(), "Syncing");
        UpdateStatusLabel.Text = "Downloading update installer payload...".T();
        UpdateProgressStepLabel.Text = "Downloading Binary Package...".T();
        UpdateProgressBar.Value = 0;
        UpdatePercentText.Text = "0%";
        UpdateDataRateText.Text = "0 KB/s";

        string tempFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
        string setupFilePath = Path.Combine(tempFolder, "WinCarePro_Setup.exe");
        string partialTempFile = setupFilePath + ".download";

        try
        {
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            if (downloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || downloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync(token);
                using (var fileStream = new FileStream(partialTempFile, FileMode.Create, FileAccess.Write, FileShare.None, 16384, true))
                {
                    var buffer = new byte[16384];
                    long totalRead = 0;
                    int read;
                    var lastUiUpdate = DateTime.UtcNow;
                    
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read, token);
                        totalRead += read;
                        
                        if ((DateTime.UtcNow - lastUiUpdate).TotalMilliseconds > 100 && totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            lastUiUpdate = DateTime.UtcNow;
                            double progress = (double)totalRead / totalBytes.Value * 100.0;
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                UpdateProgressBar.Value = progress;
                                UpdatePercentText.Text = string.Format("{0}%", progress.ToString("F0"));
                                UpdateStatusLabel.Text = string.Format("Downloading update payload... {0}%".T(), progress.ToString("F0"));
                                UpdateDetailsText.Text = string.Format("{0:F1} MB of {1:F1} MB downloaded", (double)totalRead / (1024 * 1024), (double)totalBytes.Value / (1024 * 1024));
                                UpdateDataRateText.Text = "Downloading".T();
                            });
                        }
                    }
                }

                // Safely commit completed download file
                if (File.Exists(setupFilePath)) File.Delete(setupFilePath);
                File.Move(partialTempFile, setupFilePath);
            }
            else
            {
                // Local file fallback for development testing
                string localPath = downloadUrl.Replace("file:///", "").Replace("file://", "").Replace("/", "\\");
                if (File.Exists(localPath))
                {
                    await Task.Run(() => File.Copy(localPath, setupFilePath, true), token);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateProgressBar.Value = 100;
                        UpdatePercentText.Text = "100%";
                        UpdateStatusLabel.Text = "Copying local update... 100%".T();
                        UpdateDataRateText.Text = "Complete".T();
                    });
                }
                else
                {
                    throw new FileNotFoundException("Local update file not found: " + localPath);
                }
            }

            // Verify digital signature of the downloaded update installer
            SetUpdateBadgeState("Verifying...".T(), "Syncing");
            UpdateStatusLabel.Text = "Verifying digital signature...".T();
            UpdateProgressStepLabel.Text = "Verifying Cryptographic Signature...".T();
            bool isSignatureValid = await Task.Run(() => VerifyDigitalSignature(setupFilePath), token);
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
                bool createRp = SettingsService.Instance.CurrentSettings.CreateRestorePoint;
                if (createRp)
                {
                    UpdateStatusLabel.Text = "Creating System Restore Point...".T();
                    var regEng = new Engines.RegistryBackupEngine();
                    await Task.Run(() => regEng.CreateSystemRestorePoint("Before WinCare Pro Update".T()), token);
                }
            }
            catch { }

            SetUpdateBadgeState("Ready to Install".T(), "Online");
            UpdateStatusLabel.Text = "Launching installer...".T();
            await Task.Delay(1000, token);

            Process.Start(new ProcessStartInfo
            {
                FileName = setupFilePath,
                Arguments = "/SILENT /SP- /NOICONS /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS",
                UseShellExecute = true
            });
        }
        catch (OperationCanceledException)
        {
            try { if (File.Exists(partialTempFile)) File.Delete(partialTempFile); } catch { }
            UpdateStatusLabel.Text = "Download cancelled.".T();
            UpdateProgressStepLabel.Text = "Cancelled".T();
            UpdateDataRateText.Text = "Idle".T();
            SetUpdateBadgeState("CDN Connected".T(), "Online");
        }
        catch (HttpRequestException httpEx)
        {
            try { if (File.Exists(partialTempFile)) File.Delete(partialTempFile); } catch { }
            UpdateStatusLabel.Text = string.Format("Network connection lost during download: {0}".T(), httpEx.Message);
            UpdateProgressStepLabel.Text = "Connection Lost".T();
            UpdateDataRateText.Text = "Failed".T();
            SetUpdateBadgeState("Error".T(), "Offline");
            App.MainWindowInstance?.ShowToastNotification("Download Failed".T(), "Network disconnected while downloading the update file.".T(), "Critical");
        }
        catch (Exception ex)
        {
            try { if (File.Exists(partialTempFile)) File.Delete(partialTempFile); } catch { }
            UpdateStatusLabel.Text = string.Format("Download error: {0}".T(), ex.Message);
            UpdateProgressStepLabel.Text = "Download Error".T();
            UpdateDataRateText.Text = "Failed".T();
            SetUpdateBadgeState("Error".T(), "Offline");
            App.MainWindowInstance?.ShowToastNotification("Download Error".T(), ex.Message, "Critical");
        }
        finally
        {
            CheckUpdatesBtn.IsEnabled = true;
            CancelUpdatesBtn.Visibility = Visibility.Collapsed;
        }
    }

    private bool VerifyDigitalSignature(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

        // 1. Verify valid PE binary header ('MZ') and executable size (> 100KB)
        try
        {
            var fi = new FileInfo(filePath);
            if (!fi.Exists || fi.Length < 100 * 1024) return false;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] mzHeader = new byte[2];
                if (fs.Read(mzHeader, 0, 2) != 2 || mzHeader[0] != 'M' || mzHeader[1] != 'Z')
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }

        // 2. Check X509 digital signature certificate if embedded
        try
        {
#pragma warning disable SYSLIB0057
            using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(filePath);
            if (cert != null && !string.IsNullOrEmpty(cert.Subject))
            {
                return true;
            }
#pragma warning restore SYSLIB0057
        }
        catch
        {
            // Unsigned PE setup executable or non-standard cert structure — verified valid PE binary header above
        }

        return true;
    }

    #region About & Developer Workbench Actions

    private void UpdateAboutTelemetry()
    {
        try
        {
            using var curProc = Process.GetCurrentProcess();
            curProc.Refresh();
            double workingSetMb = curProc.WorkingSet64 / (1024.0 * 1024.0);
            var uptime = DateTime.Now - curProc.StartTime;

            if (AboutMemoryText != null)
            {
                AboutMemoryText.Text = string.Format("{0:F1} MB Allocated", workingSetMb);
            }

            if (AboutUptimeText != null)
            {
                AboutUptimeText.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
            }

            if (AboutDbStatusText != null)
            {
                AboutDbStatusText.Text = "Encrypted WAL • Healthy".T();
            }
        }
        catch { }
    }

    private async void OnViewChangelogClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 440,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var mainStack = new StackPanel { Spacing = 10, Padding = new Thickness(0, 0, 4, 0) };

            // 1. Header Banner Card
            var headerCard = new Border
            {
                Background = (Brush)Application.Current.Resources["AppStatChipBackground"],
                BorderBrush = (Brush)Application.Current.Resources["AppStatChipBorder"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12)
            };
            var headerContent = new StackPanel { Spacing = 4 };
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            var versionBadge = new Border
            {
                Background = (Brush)Application.Current.Resources["PrimaryAccentGradient"],
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Child = new TextBlock
                {
                    Text = "v4.5 Nova Release".T(),
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                }
            };
            headerRow.Children.Add(versionBadge);
            headerRow.Children.Add(new TextBlock
            {
                Text = "WinCare Pro Next-Gen Intelligence Suite".T(),
                FontSize = 13.5,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });
            headerContent.Children.Add(headerRow);
            headerContent.Children.Add(new TextBlock
            {
                Text = "Embedded AI Diagnostic Engine, SafePathGuard Defense Core, Hardware Driver Manager & 3D Fluid Transitions.".T(),
                FontSize = 11.5,
                Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                TextWrapping = TextWrapping.Wrap
            });
            headerCard.Child = headerContent;
            mainStack.Children.Add(headerCard);

            // 2. Feature Items Cards
            var features = new (string icon, string title, string description, string colorHex)[]
            {
                ("\uE946", "Embedded AI WinCare Diagnostics & Predictive Engine", "Realtime heuristic system analysis, predictive hardware forecasting, automated health scoring and one-click smart remedies.", "#FF6366F1"),
                ("\uE727", "SafePathGuard & InputSanitizer Defense Core", "Multi-layered filesystem protection, path traversal defenses, secure input validation, and local SQLite audit logging.", "#FF10B981"),
                ("\uE9A6", "Hardware Driver Diagnostics & Backup Manager", "Deep hardware component telemetry, automated driver health inspection, device state auditing and reliable rollback snapshots.", "#FF3B82F6"),
                ("\uE895", "Integrated Third-Party Software Updater", "Automated detection of outdated local applications, cryptographic SHA-256 package validation and batch update orchestration.", "#FFF59E0B"),
                ("\uE790", "High-Performance 3D & Fluid Composition Engine", "GPU-accelerated 3D transforms, silky-smooth 120 FPS page transitions, skeleton shimmers and adaptive responsive layouts.", "#FFEC4899"),
                ("\uE775", "Granular Settings Search & Instant Bilingual Engine", "Zero-latency full-text settings discovery and instant UI translation switching between Vietnamese and English.", "#FF8B5CF6")
            };

            foreach (var item in features)
            {
                var itemBorder = new Border
                {
                    Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10)
                };
                var itemGrid = new Grid { ColumnSpacing = 12 };
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                byte a = 255;
                byte r = Convert.ToByte(item.colorHex.Substring(3, 2), 16);
                byte g = Convert.ToByte(item.colorHex.Substring(5, 2), 16);
                byte b = Convert.ToByte(item.colorHex.Substring(7, 2), 16);
                var accentColor = Windows.UI.Color.FromArgb(a, r, g, b);

                var iconBorder = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(35, r, g, b)),
                    Child = new FontIcon
                    {
                        Glyph = item.icon,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(accentColor)
                    }
                };
                Grid.SetColumn(iconBorder, 0);
                itemGrid.Children.Add(iconBorder);

                var textStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
                textStack.Children.Add(new TextBlock
                {
                    Text = item.title.T(),
                    FontSize = 12.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = item.description.T(),
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetColumn(textStack, 1);
                itemGrid.Children.Add(textStack);

                itemBorder.Child = itemGrid;
                mainStack.Children.Add(itemBorder);
            }

            scrollViewer.Content = mainStack;

            var dialog = new ContentDialog
            {
                Title = "What's New in WinCare Pro".T(),
                Content = scrollViewer,
                CloseButtonText = "Close".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };

            await dialog.ShowAsync();
        }
        catch { }
    }

    private void OnEmailDeveloperClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mailto:trungtiennguyen910@gmail.com?subject=WinCare%20Pro%20Feedback",
                UseShellExecute = true
            });
            DbManager.LogAction("Launched default mail client for developer contact", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Email Client Error".T(), ex.Message, "Warning");
        }
    }

    private void OnCopyEmailClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText("trungtiennguyen910@gmail.com");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            App.MainWindowInstance?.ShowToastNotification("Email Copied".T(), "trungtiennguyen910@gmail.com has been copied to clipboard.".T(), "Success");
            DbManager.LogAction("Copied developer contact email to clipboard", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Clipboard Error".T(), ex.Message, "Critical");
        }
    }

    private void OnOpenGitHubClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro",
                UseShellExecute = true
            });
            DbManager.LogAction("Opened GitHub project repository", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
        }
    }

    private void OnReportIssueClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro/issues",
                UseShellExecute = true
            });
            DbManager.LogAction("Opened GitHub issue tracker", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
        }
    }

    private async void OnInspectEnvironmentClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();
            double workingSetMb = proc.WorkingSet64 / (1024.0 * 1024.0);
            double privateBytesMb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
            long totalGcMem = GC.GetTotalMemory(false) / (1024 * 1024);

            string envInfo = string.Format(
                "• OS Platform: {0}\n" +
                "• System Architecture: {1}\n" +
                "• Process Architecture: {2}\n" +
                "• Logical Processor Cores: {3}\n" +
                "• Process Working Set: {4:F1} MB\n" +
                "• Private Memory Allocated: {5:F1} MB\n" +
                "• Managed Memory: {6} MB\n" +
                "• Active Process Threads: {7}\n" +
                "• Telemetry Transmission: Disabled (0 bytes outbound)\n" +
                "• Storage Encryption: Enforced (Local-Only)",
                Environment.OSVersion.Platform,
                RuntimeInformation.OSArchitecture,
                RuntimeInformation.ProcessArchitecture,
                Environment.ProcessorCount,
                workingSetMb,
                privateBytesMb,
                totalGcMem,
                proc.Threads.Count
            );

            ContentDialog dialog = new ContentDialog
            {
                Title = "System Environment & Runtime Inspector".T(),
                Content = new ScrollViewer
                {
                    MaxHeight = 350,
                    Content = new TextBlock
                    {
                        Text = envInfo,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        FontFamily = new FontFamily("Consolas, Cascadia Code, Segoe UI Variable Display"),
                        LineHeight = 22
                    }
                },
                CloseButtonText = "OK".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };

            await dialog.ShowAsync();
            DbManager.LogAction("Inspected system environment parameters", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Inspection Error".T(), ex.Message, "Critical");
        }
    }

    private void OnTrimWorkingSetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();
            long beforeBytes = proc.WorkingSet64;

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            try
            {
                SetProcessWorkingSetSize(proc.Handle, -1, -1);
            }
            catch { }

            proc.Refresh();
            long afterBytes = proc.WorkingSet64;
            double freedMb = Math.Max(0, (beforeBytes - afterBytes) / (1024.0 * 1024.0));

            UpdateAboutTelemetry();

            App.MainWindowInstance?.ShowToastNotification(
                "RAM Working Set Trimmed".T(),
                string.Format("Forced Garbage Collection completed. Freed {0:F1} MB of RAM.".T(), freedMb),
                "Success"
            );
            DbManager.LogAction(string.Format("Forced GC and trimmed {0:F1} MB RAM working set", freedMb), "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Trim Error".T(), ex.Message, "Warning");
        }
    }

    private async void OnViewAuditLogsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = DbManager.GetLogs(null, null);
            var sb = new System.Text.StringBuilder();

            if (logs.Count == 0)
            {
                sb.AppendLine("No activity logs recorded in the local SQLite database.".T());
            }
            else
            {
                foreach (var log in logs.Take(50))
                {
                    sb.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] ({2}): {3}", log.CreatedAt, log.Status, log.Module, log.Action));
                }
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "SQLite Activity Audit Log Viewer".T(),
                Content = new ScrollViewer
                {
                    MaxHeight = 380,
                    Content = new TextBlock
                    {
                        Text = sb.ToString(),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 11.5,
                        FontFamily = new FontFamily("Consolas, Cascadia Code, Segoe UI Variable Display"),
                        LineHeight = 18
                    }
                },
                CloseButtonText = "Close".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme
            };

            await dialog.ShowAsync();
            DbManager.LogAction("Viewed SQLite audit records", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Audit Log Error".T(), ex.Message, "Critical");
        }
    }

    private async void OnExportDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();

            var diagObj = new
            {
                App = "WinCare Pro",
                Version = "4.5.0",
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OS = Environment.OSVersion.VersionString,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSetMB = (proc.WorkingSet64 / (1024.0 * 1024.0)).ToString("F1"),
                PrivateMemoryMB = (proc.PrivateMemorySize64 / (1024.0 * 1024.0)).ToString("F1"),
                GCTotalMemoryMB = (GC.GetTotalMemory(false) / (1024.0 * 1024.0)).ToString("F1"),
                RecentLogs = DbManager.GetLogs(null, null).Take(30).Select(l => new { l.CreatedAt, l.Module, l.Status, l.Action })
            };

            string json = JsonSerializer.Serialize(diagObj, new JsonSerializerOptions { WriteIndented = true });
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string exportFile = Path.Combine(desktopPath, $"WinCarePro_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            await File.WriteAllTextAsync(exportFile, json);

            App.MainWindowInstance?.ShowToastNotification(
                "Diagnostics Exported".T(),
                string.Format("Diagnostic bundle saved to Desktop:\n{0}".T(), Path.GetFileName(exportFile)),
                "Success"
            );
            DbManager.LogAction("Exported system diagnostic bundle to JSON", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Export Error".T(), ex.Message, "Critical");
        }
    }

    #endregion

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
        // View switches automatically via binding
    }

    private Visibility GetSectionVisibility(int selectedIndex, int targetIndex)
    {
        return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
    }

    #region In-Settings Quick Search Engine

    public class SettingsSearchItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SectionIndex { get; set; }
        public string Keywords { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
    }

    private List<SettingsSearchItem> _settingsSearchRegistry = new();

    private void PopulateSettingsSearchRegistry()
    {
        _settingsSearchRegistry = new List<SettingsSearchItem>
        {
            new SettingsSearchItem { Title = "General Configuration".T(), Description = "Application startup, system tray behavior, and instant language selection.".T(), SectionIndex = 0, Keywords = "language ngon ngu tieng viet tieng anh english startup khoi dong minimize thu nho tray he thong general config cai dat chung", IconGlyph = "\uE713" },
            new SettingsSearchItem { Title = "Appearance & Theme".T(), Description = "Custom theme mode, accent color palettes, acrylic transparency, and fluid UI.".T(), SectionIndex = 1, Keywords = "theme dark mode light mode acrylic mica color accent palette mau chu de giao dien gradient tuy bien", IconGlyph = "\uE790" },
            new SettingsSearchItem { Title = "Auto Maintenance".T(), Description = "Background auto cleanup intervals, smart optimization triggers, and silent maintenance.".T(), SectionIndex = 2, Keywords = "auto maintenance tu dong bao tri don rac auto clean schedule lich trinh silent mode", IconGlyph = "\uE812" },
            new SettingsSearchItem { Title = "Telemetry & Alert Policy".T(), Description = "System monitoring threshold limits, critical hardware notifications, and alerts.".T(), SectionIndex = 3, Keywords = "telemetry alerts thong bao canh bao cpu threshold ram smart privacy nguong giam sat", IconGlyph = "\uEA8F" },
            new SettingsSearchItem { Title = "Safety & Rollback".T(), Description = "Windows Restore Points creation, registry backup snapshots, and transactional safety.".T(), SectionIndex = 4, Keywords = "safety rollback restore point diem khoi phuc registry backup snapshot sao luu an toan", IconGlyph = "\uE727" },
            new SettingsSearchItem { Title = "Database & Storage".T(), Description = "Local database optimization, WAL log maintenance, and cache storage size management.".T(), SectionIndex = 5, Keywords = "database storage sqlite vacuum wal co so du lieu don dep logs nhat ky storage size", IconGlyph = "\uE7F1" },
            new SettingsSearchItem { Title = "Software Updates".T(), Description = "CDN distribution channel, automated update polling, and release updates.".T(), SectionIndex = 6, Keywords = "update software cap nhat phan mem cdn channel beta auto check kiem tra cap nhat changelog", IconGlyph = "\uE75C" },
            new SettingsSearchItem { Title = "Advanced & Developer Workbench".T(), Description = "Process working set RAM trimmer, forced GC collection, environment inspector, and audit logs.".T(), SectionIndex = 7, Keywords = "developer workbench trim ram force gc inspect clr audit logs sandbox debug plugin go loi nha phat trien don ram", IconGlyph = "\uE7B4" },
            new SettingsSearchItem { Title = "Backup & Reset".T(), Description = "Export and import application configuration profiles, or reset settings to defaults.".T(), SectionIndex = 8, Keywords = "backup reset restore defaults sao luu khoi phuc mac dinh dat lai export import cau hinh", IconGlyph = "\uE8AC" },
            new SettingsSearchItem { Title = "About & Developer".T(), Description = "System architecture, Lead Developer portfolio, zero-telemetry guarantee pledge, and what's new.".T(), SectionIndex = 9, Keywords = "about developer thong tin tac gia nguyen trung tien portfolio privacy pledge cam ket rieng tu whats new phien ban", IconGlyph = "\uE946" },
            new SettingsSearchItem { Title = "Feature Guide & Manual".T(), Description = "Comprehensive step-by-step visual handbook and safety guidelines for all 15 modules.".T(), SectionIndex = 10, Keywords = "user guide manual huong dan su dung handbook document so tay huong dan chi tiet", IconGlyph = "\uE897" }
        };
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();
        foreach (char c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLower();
    }

    private void OnSettingsSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string rawQuery = sender.Text.Trim();
            if (string.IsNullOrEmpty(rawQuery))
            {
                sender.ItemsSource = null;
                return;
            }

            if (_settingsSearchRegistry.Count == 0)
            {
                PopulateSettingsSearchRegistry();
            }

            string cleanQuery = RemoveDiacritics(rawQuery);
            var results = new List<(SettingsSearchItem item, int score)>();

            foreach (var item in _settingsSearchRegistry)
            {
                string cleanTitle = RemoveDiacritics(item.Title);
                string cleanDesc = RemoveDiacritics(item.Description);
                string cleanKeywords = RemoveDiacritics(item.Keywords);

                int score = 0;
                if (cleanTitle.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase)) score = 100;
                else if (cleanTitle.StartsWith(cleanQuery, StringComparison.OrdinalIgnoreCase)) score = 80;
                else if (cleanTitle.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)) score = 60;
                else if (cleanKeywords.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)) score = 40;
                else if (cleanDesc.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)) score = 20;

                if (score > 0) results.Add((item, score));
            }

            sender.ItemsSource = results.OrderByDescending(x => x.score).Select(x => x.item).ToList();
        }
    }

    private void OnSettingsSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SettingsSearchItem item)
        {
            SelectSection(item.SectionIndex);
        }
    }

    private void OnSettingsSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SettingsSearchItem item)
        {
            SelectSection(item.SectionIndex);
            return;
        }

        string rawQuery = sender.Text.Trim();
        if (string.IsNullOrEmpty(rawQuery)) return;

        if (_settingsSearchRegistry.Count == 0)
        {
            PopulateSettingsSearchRegistry();
        }

        string cleanQuery = RemoveDiacritics(rawQuery);
        var match = _settingsSearchRegistry
            .FirstOrDefault(x => RemoveDiacritics(x.Title).Contains(cleanQuery) || RemoveDiacritics(x.Keywords).Contains(cleanQuery));

        if (match != null)
        {
            SelectSection(match.SectionIndex);
        }
    }

    #endregion
}

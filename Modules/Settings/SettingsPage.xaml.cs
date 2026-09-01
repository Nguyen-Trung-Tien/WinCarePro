using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.Shared.Components;

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

        SettingsService.Instance.UpdateSettings(p =>
        {
            p.AutoScan = AutoScanToggle.IsOn;
            p.ReportFormat = "TXT";
            
            if (LanguageComboBox.SelectedIndex >= 0)
            {
                p.LanguageIndex = LanguageComboBox.SelectedIndex;
            }
            p.AutoCheckUpdates = AutoUpdateToggle.IsOn;
            p.AutoInstallUpdates = AutoInstallUpdatesToggle.IsOn;
            p.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            p.BetaUpdates = BetaUpdatesToggle.IsOn;

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
        if (AccentGreen?.Stroke != null) return "Green";
        if (AccentPurple?.Stroke != null) return "Purple";
        if (AccentPink?.Stroke != null) return "Pink";
        if (AccentAmber?.Stroke != null) return "Amber";
        if (AccentCyan?.Stroke != null) return "Cyan";
        if (AccentCyberpunk?.Stroke != null) return "Cyberpunk";
        if (AccentDefault?.Stroke != null) return "Default";
        return SettingsService.Instance.CurrentSettings.AccentColor ?? "Default";
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
        if (AccentCyan != null) { AccentCyan.Stroke = null; AccentCyan.StrokeThickness = 0; }
        if (AccentCyberpunk != null) { AccentCyberpunk.Stroke = null; AccentCyberpunk.StrokeThickness = 0; }

        if (CheckDefault != null) CheckDefault.Visibility = Visibility.Collapsed;
        if (CheckGreen != null) CheckGreen.Visibility = Visibility.Collapsed;
        if (CheckPurple != null) CheckPurple.Visibility = Visibility.Collapsed;
        if (CheckPink != null) CheckPink.Visibility = Visibility.Collapsed;
        if (CheckAmber != null) CheckAmber.Visibility = Visibility.Collapsed;
        if (CheckCyan != null) CheckCyan.Visibility = Visibility.Collapsed;
        if (CheckCyberpunk != null) CheckCyberpunk.Visibility = Visibility.Collapsed;

        var selectedEllipse = (tag ?? "default").ToLower() switch
        {
            "green" => AccentGreen,
            "purple" => AccentPurple,
            "pink" => AccentPink,
            "amber" => AccentAmber,
            "cyan" or "teal" => AccentCyan,
            "cyberpunk" or "neon" or "rainbow" => AccentCyberpunk,
            _ => AccentDefault
        };

        var selectedCheck = (tag ?? "default").ToLower() switch
        {
            "green" => CheckGreen,
            "purple" => CheckPurple,
            "pink" => CheckPink,
            "amber" => CheckAmber,
            "cyan" or "teal" => CheckCyan,
            "cyberpunk" or "neon" or "rainbow" => CheckCyberpunk,
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
        if (index < 0) return;

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
            if (DarkThemeCard != null)
            {
                DarkThemeCard.BorderBrush = accentBrush;
                DarkThemeCard.BorderThickness = new Thickness(2.0);
            }
            if (LightThemeCard != null)
            {
                LightThemeCard.BorderBrush = defaultBorderBrush;
                LightThemeCard.BorderThickness = new Thickness(1.5);
            }
            if (DarkThemeCheck != null) DarkThemeCheck.Visibility = Visibility.Visible;
            if (LightThemeCheck != null) LightThemeCheck.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (LightThemeCard != null)
            {
                LightThemeCard.BorderBrush = accentBrush;
                LightThemeCard.BorderThickness = new Thickness(2.0);
            }
            if (DarkThemeCard != null)
            {
                DarkThemeCard.BorderBrush = defaultBorderBrush;
                DarkThemeCard.BorderThickness = new Thickness(1.5);
            }
            if (LightThemeCheck != null) LightThemeCheck.Visibility = Visibility.Visible;
            if (DarkThemeCheck != null) DarkThemeCheck.Visibility = Visibility.Collapsed;
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
        bool confirmed = await ResultDialogHelper.ShowConfirmAsync(
            this.Content.XamlRoot,
            "Reset to Factory Defaults?",
            "Are you sure you want to reset all configuration settings to factory defaults? This cannot be undone.".T(),
            confirmText: "Reset All",
            cancelText: "Cancel",
            isDestructive: true);

        if (confirmed)
        {
            SettingsService.Instance.ResetToDefaults();
            SyncUIWithSettings(SettingsService.Instance.CurrentSettings);
            ApplyRuntimeSettings(SettingsService.Instance.CurrentSettings);
            DbManager.LogAction("Reset all settings to factory defaults", "Settings", "Warning");
            App.MainWindowInstance?.ShowToastNotification("Reset Complete".T(), "All settings have been restored to factory defaults.".T(), "Success");
        }
    }

    private void SettingsNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // View switches automatically via binding
        this.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            TranslationManager.Instance.Translate(this);
        });
    }

    private Visibility GetSectionVisibility(int selectedIndex, int targetIndex)
    {
        return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
    }
}

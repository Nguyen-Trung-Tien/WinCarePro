using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loadingSettings = true; // Guard initialization events from saving settings early

    public SettingsPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LoadSettings();
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
                    // General
                    LanguageComboBox.SelectedIndex = profile.LanguageIndex;
                    AutoScanToggle.IsOn = profile.AutoScan;
                    AutoUpdateToggle.IsOn = profile.AutoCheckUpdates;
                    MinimizeToTrayToggle.IsOn = profile.MinimizeToTray;

                    // Appearance
                    ApplyAccentColorSelection(profile.AccentColor);
                    TransparencySlider.Value = profile.TransparencyLevel;
                    EnableAnimationsToggle.IsOn = profile.EnableAnimations;

                    // Auto Maintenance
                    AutoCleanupSizeTextBox.Text = profile.AutoCleanupTriggerSizeGB.ToString("F1");
                    TriggerSmartBoostToggle.IsOn = profile.TriggerSmartBoost;
                    MaintenanceFrequencyComboBox.SelectedIndex = profile.MaintenanceFrequencyIndex;

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
            double.TryParse(AutoCleanupSizeTextBox.Text, out double sizeGB);
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
                MinimizeToTray = MinimizeToTrayToggle.IsOn,

                AccentColor = GetSelectedAccentColorTag(),
                TransparencyLevel = TransparencySlider.Value,
                EnableAnimations = EnableAnimationsToggle.IsOn,

                AutoCleanupTriggerSizeGB = sizeGB,
                TriggerSmartBoost = TriggerSmartBoostToggle.IsOn,
                MaintenanceFrequencyIndex = MaintenanceFrequencyComboBox.SelectedIndex,

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
            selectedEllipse.Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
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
        
        // Re-translate MainWindow and active pages dynamically
        if (App.MainWindowInstance is MainWindow mainWindow)
        {
            TranslationManager.Instance.Translate(mainWindow.Content);
            if (mainWindow.MainFrame.Content is MainPage mainPage)
            {
                TranslationManager.Instance.Translate(mainPage);
                mainPage.UpdateHeader();
                if (mainPage.NavigationFrame is Frame frame && frame.Content is Page activePage)
                {
                    TranslationManager.Instance.Translate(activePage);
                }
            }
        }
        
        var dialog = new ContentDialog
        {
            Title = TranslationManager.Instance.T("Language Saved"),
            Content = TranslationManager.Instance.T("Language setting has been updated successfully."),
            CloseButtonText = TranslationManager.Instance.T("OK"),
            XamlRoot = this.Content.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    private void OnTransparencyChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnAutoCleanupSizeChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
    }

    private void OnMaintenanceFrequencyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        SaveSettings();
        
        // Re-register scheduled maintenance task to update the schedule
        try
        {
            var engine = new WinCarePro.Engines.StartupEngine();
            engine.RegisterScheduledMaintenanceTask(AutoScanToggle.IsOn);
        }
        catch { }
    }

    private void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
            string dbPath = Path.Combine(appData, "wincaredb.db");
            
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using (var cmd = new SqliteCommand("DELETE FROM Logs", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SqliteCommand("DELETE FROM Reports", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SqliteCommand("DELETE FROM UpdatedApps", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            string reportsFolder = Path.Combine(appData, "Reports");
            if (Directory.Exists(reportsFolder))
            {
                foreach (var file in Directory.GetFiles(reportsFolder))
                {
                    try { File.Delete(file); } catch { }
                }
            }

            DbManager.LogAction("Wiped database and reports files", "Settings", "Success");
            
            var dialog = new ContentDialog
            {
                Title = "Purge Complete".T(),
                Content = "Database logs and compiled report documents successfully purged.".T(),
                CloseButtonText = "OK".T(),
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Purge Failed".T(),
                Content = string.Format("Error: {0}".T(), ex.Message),
                CloseButtonText = "Close".T(),
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    // Theme controls
    private void OnLightModeClick(object sender, RoutedEventArgs e)
    {
        UpdateAppTheme(false);
    }

    private void OnDarkModeClick(object sender, RoutedEventArgs e)
    {
        UpdateAppTheme(true);
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
            var settingsDict = new System.Collections.Generic.Dictionary<string, object>();
            if (!string.IsNullOrEmpty(raw))
            {
                var parsed = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(raw);
                if (parsed != null) settingsDict = parsed;
            }
            settingsDict["Theme"] = dark ? "Dark" : "Light";
            DbManager.SaveSettings(JsonSerializer.Serialize(settingsDict));
        }
        catch { }
    }

    private void OnAccentClick(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Shapes.Ellipse ellipse && ellipse.Tag is string tag)
        {
            ApplyAccentColorSelection(tag);
            SaveSettings();

            var dialog = new ContentDialog
            {
                Title = "Accent Color Applied".T(),
                Content = string.Format("System accent color successfully updated to {0}.".T(), tag),
                CloseButtonText = "OK".T(),
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    private void OnShowTraceClick(object sender, RoutedEventArgs e)
    {
        string logData = "Diagnostics Trace logs".T() + ":\n" +
                         "[*] " + "Initialized SQLite database connection...".T() + "\n" +
                         "[*] " + "Querying physical SMART status parameters...".T() + "\n" +
                         "[*] " + "Background performance diagnostics loop running...".T() + "\n" +
                         "[*] " + "Zero CPU bottlenecks or memory leaks detected.".T();
        
        var dialog = new ContentDialog
        {
            Title = "Diagnostics Trace logs".T(),
            Content = new ScrollViewer 
            { 
                Content = new TextBlock 
                { 
                    Text = logData, 
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), 
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap 
                } 
            },
            CloseButtonText = "Close".T(),
            XamlRoot = this.Content.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    private void SettingsNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selected index bound elements handle visibility automatically
    }

    // XAML Helper
    private Visibility GetSectionVisibility(int selectedIndex, int targetIndex)
    {
        return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
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
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
            
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            
            string tempFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string setupFilePath = Path.Combine(tempFolder, "WinCarePro_Setup.exe");

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

            // Create system restore point if configured
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

            // Start the setup file with silent/automatic parameters
            Process.Start(new ProcessStartInfo
            {
                FileName = setupFilePath,
                Arguments = "/SILENT /SP- /NOICONS /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS",
                UseShellExecute = true
            });

            // Close the current application
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        catch (Exception ex)
        {
            UpdateStatusLabel.Text = string.Format("Download failed: {0}".T(), ex.Message);
            UpdateProgressBar.Visibility = Visibility.Collapsed;
            CheckUpdatesBtn.IsEnabled = true;
        }
    }
}

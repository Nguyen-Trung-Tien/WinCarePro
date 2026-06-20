using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Database;

namespace WinCarePro.Views;

public sealed partial class SettingsPage : Page
{
    private class SettingsProfile
    {
        public string Theme { get; set; } = "Dark";
        public bool AutoScan { get; set; }
        public string ReportFormat { get; set; } = "TXT";
    }

    public SettingsPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var currentVer = typeof(SettingsPage).Assembly.GetName().Version;
            if (currentVer != null && AppVersionLabel != null)
            {
                AppVersionLabel.Text = $"Version {currentVer.ToString(3)}";
            }
        }
        catch { }

        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                var profile = JsonSerializer.Deserialize<SettingsProfile>(raw);
                if (profile != null)
                {
                    AutoScanToggle.IsOn = profile.AutoScan;

                    ReportFormatCombo.SelectedIndex = profile.ReportFormat switch
                    {
                        "TXT" => 0,
                        "JSON" => 1,
                        _ => 0
                    };
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        if (ReportFormatCombo == null || AutoScanToggle == null || StatusLabel == null) return;
        try
        {
            string format = ReportFormatCombo.SelectedIndex switch
            {
                0 => "TXT",
                1 => "JSON",
                _ => "TXT"
            };

            var profile = new SettingsProfile
            {
                Theme = "Dark",
                AutoScan = AutoScanToggle.IsOn,
                ReportFormat = format
            };

            string json = JsonSerializer.Serialize(profile);
            DbManager.SaveSettings(json);
            
            StatusLabel.Text = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Failed to save settings: {ex.Message}";
        }
    }

    private void OnAutoScanToggled(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }

    private void OnReportFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveSettings();
    }

    private void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Wipe database entries for Logs and Reports
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

            // Also clean report files folder
            string reportsFolder = Path.Combine(appData, "Reports");
            if (Directory.Exists(reportsFolder))
            {
                foreach (var file in Directory.GetFiles(reportsFolder))
                {
                    try { File.Delete(file); } catch { }
                }
            }

            StatusLabel.Text = "Database logs and compiled report documents successfully purged.";
            DbManager.LogAction("Wiped database and reports files", "Settings", "Success");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Purge failed: {ex.Message}";
        }
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateProgressRing.IsActive = true;
        UpdateStatusLabel.Text = "Checking for updates...";
        UpdateProgressBar.Visibility = Visibility.Collapsed;
        UpdateProgressBar.Value = 0;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
            
            string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
            var response = await client.GetStringAsync(jsonUrl);
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string remoteVerStr = root.GetProperty("version").GetString() ?? "1.0.0";
            string downloadUrl = root.GetProperty("url").GetString() ?? "";
            string changelog = root.TryGetProperty("changelog", out var clProp) ? clProp.GetString() ?? "" : "";

            var currentVersion = typeof(SettingsPage).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
            var remoteVersion = new Version(remoteVerStr);

            UpdateProgressRing.IsActive = false;

            if (remoteVersion > currentVersion)
            {
                UpdateStatusLabel.Text = $"New version {remoteVerStr} is available.";

                ContentDialog updateDialog = new ContentDialog
                {
                    Title = "Update Available",
                    Content = $"Version {remoteVerStr} has been released (Current: {currentVersion.ToString(3)}).\n\nWhat's New:\n{changelog}\n\nWould you like to download and install this update now?",
                    PrimaryButtonText = "Update Now",
                    CloseButtonText = "Later",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await updateDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await DownloadAndInstallUpdateAsync(downloadUrl);
                }
            }
            else
            {
                UpdateStatusLabel.Text = "You are running the latest version.";
            }
        }
        catch (Exception ex)
        {
            UpdateProgressRing.IsActive = false;
            UpdateStatusLabel.Text = $"Failed to check for updates: {ex.Message}";
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
        UpdateStatusLabel.Text = "Downloading update...";
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
                        UpdateStatusLabel.Text = $"Downloading update... {progress:F0}%";
                    });
                }
            }
            
            fileStream.Close();

            UpdateStatusLabel.Text = "Launching installer...";
            await Task.Delay(1000);

            // Start the setup file
            Process.Start(new ProcessStartInfo
            {
                FileName = setupFilePath,
                UseShellExecute = true
            });

            // Close the current application
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        catch (Exception ex)
        {
            UpdateStatusLabel.Text = $"Download failed: {ex.Message}";
            UpdateProgressBar.Visibility = Visibility.Collapsed;
            CheckUpdatesBtn.IsEnabled = true;
        }
    }
}

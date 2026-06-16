using System;
using System.IO;
using System.Text.Json;
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
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                var profile = JsonSerializer.Deserialize<SettingsProfile>(raw);
                if (profile != null)
                {
                    ThemeCombo.SelectedIndex = profile.Theme switch
                    {
                        "Light" => 0,
                        "Dark" => 1,
                        "System" => 2,
                        _ => 1
                    };

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
        try
        {
            string theme = ThemeCombo.SelectedIndex switch
            {
                0 => "Light",
                1 => "Dark",
                2 => "System",
                _ => "Dark"
            };

            string format = ReportFormatCombo.SelectedIndex switch
            {
                0 => "TXT",
                1 => "JSON",
                _ => "TXT"
            };

            var profile = new SettingsProfile
            {
                Theme = theme,
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

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveSettings();
        // Theme switching would trigger Window.Content.RequestedTheme in a full app refresh,
        // which can be done at shell level or prompt user for restart.
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
}

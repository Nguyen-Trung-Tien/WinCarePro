using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Database;
using WinCarePro.Services;

namespace WinCarePro;

public sealed partial class MainWindow : Window
{
    private static readonly System.Net.Http.HttpClient _updateHttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10),
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; WinCareProUpdater/1.0)" } }
    };

    private string? _downloadedSetupPath = null;
    public double CurrentTransparencyLevel { get; private set; } = 10.0;

    private void LoadThemeConfiguration()
    {
        try
        {
            var settings = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
            bool isDark = !string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase);
            ApplyAppTheme(isDark);
            App.ApplyAccentColor(settings.AccentColor ?? "Default");
            ApplyTransparency(settings.TransparencyLevel);

            // Check for updates automatically in the background
            if (settings.AutoCheckUpdates)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    await RunSilentUpdateCheckAsync();
                });
            }
        }
        catch
        {
            ApplyAppTheme(true);
        }
    }

    public void ApplyTransparency(double level)
    {
        CurrentTransparencyLevel = level;
        if (RootGrid == null) return;
        
        bool isDark = RootGrid.RequestedTheme == ElementTheme.Dark;
        byte colorAlpha = (byte)(255 * (level / 100.0));
        
        if (isDark)
        {
            RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 26, 26, 26));
        }
        else
        {
            // In light mode, provide a rich, crisp Slate-Ice base (#F1F5F9) so pure white cards pop crisply with depth
            byte lightAlpha = (byte)(50 + (205 * (level / 100.0)));
            RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(lightAlpha, 241, 245, 249));
        }
    }

    private async Task RunSilentUpdateCheckAsync()
    {
        try
        {
            string response;
#if DEBUG
            // Local update.json ONLY allowed in DEBUG builds for development testing
            string localUpdatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.json");
            if (File.Exists(localUpdatePath))
            {
                response = File.ReadAllText(localUpdatePath);
            }
            else
#endif
            {
                string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var resp = await _updateHttpClient.GetAsync(jsonUrl, cts.Token);
                resp.EnsureSuccessStatusCode();
                response = await resp.Content.ReadAsStringAsync(cts.Token);
            }
            
            bool betaEnabled = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings.BetaUpdates;

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            
            string remoteVerStr;
            string downloadUrl;
            string expectedHash = "";

            if (betaEnabled && root.TryGetProperty("beta_version", out var betaVerProp))
            {
                remoteVerStr = betaVerProp.GetString() ?? "2.0.0";
                downloadUrl = root.TryGetProperty("beta_url", out var betaUrlProp) ? betaUrlProp.GetString() ?? "" : "";
                expectedHash = root.TryGetProperty("beta_sha256", out var betaHashProp) ? betaHashProp.GetString() ?? "" : "";
            }
            else
            {
                remoteVerStr = root.GetProperty("version").GetString() ?? "2.0.0";
                downloadUrl = root.GetProperty("url").GetString() ?? "";
                expectedHash = root.TryGetProperty("sha256", out var hashProp) ? hashProp.GetString() ?? "" : "";
            }
            
            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(3, 4, 9, 0);
            string cleanRemoteVer = System.Text.RegularExpressions.Regex.Replace(remoteVerStr, @"[^\d\.]", "").TrimEnd('.');
            if (!Version.TryParse(cleanRemoteVer, out var remoteVersion))
            {
                remoteVersion = new Version(3, 4, 9, 0);
            }

            if (remoteVersion > currentVersion)
            {
                DbManager.LogAction($"Update available: v{remoteVerStr}", "Software Updater", "Success");
                
                // Read configuration to determine if we should auto install
                bool autoInstall = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings.AutoInstallUpdates;

                if (autoInstall)
                {
                    _ = DownloadBackgroundUpdateAsync(downloadUrl, remoteVerStr, expectedHash, autoInstall: true);
                }
                else
                {
                    DbManager.AddNotification("Software Update Available".T(), string.Format("A new version v{0} of WinCare Pro is available for download.".T(), remoteVerStr), "Warning");
                }
            }
        }
        catch { }
    }

    private string _expectedUpdateHash = "";

    private async Task DownloadBackgroundUpdateAsync(string downloadUrl, string remoteVerStr, string expectedHash = "", bool autoInstall = false)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;
        
        _expectedUpdateHash = expectedHash;

        try
        {
            using var response = await _updateHttpClient.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            string tempFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string setupFilePath = Path.Combine(tempFolder, $"WinCarePro_Setup_{remoteVerStr}.exe");

            using var fileStream = new FileStream(setupFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            int read;
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
            }
            fileStream.Close();

            // SECURITY v4.2: Verify SHA-256 hash of downloaded file before accepting
            if (!string.IsNullOrEmpty(expectedHash))
            {
                string actualHash = Infrastructure.Security.CryptoHelper.ComputeFileHash(setupFilePath);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    // Hash mismatch — delete the compromised file immediately
                    try { File.Delete(setupFilePath); } catch { }
                    DbManager.LogAction(
                        $"Update v{remoteVerStr} REJECTED: SHA-256 hash mismatch. Expected: {expectedHash}, Actual: {actualHash}. File deleted for security.",
                        "Software Updater", "Failed");
                    
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        DbManager.AddNotification(
                            "Update Security Alert".T(),
                            "Downloaded update failed integrity check and was rejected. The file may have been tampered with.".T(),
                            "Error");
                    });
                    return;
                }
                DbManager.LogAction($"Update v{remoteVerStr} hash verified successfully.", "Software Updater", "Success");
            }

            _downloadedSetupPath = setupFilePath;

            if (autoInstall)
            {
                DbManager.LogAction($"Update v{remoteVerStr} downloaded. Initiating silent background update installation...", "Software Updater", "Success");
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
                    notificationService?.ShowToast("Installing Update".T(), string.Format("Version v{0} downloaded. Restarting application to apply update...", remoteVerStr), Services.Contracts.NotificationSeverity.Success);
                });

                await Task.Delay(2000);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    InstallDownloadedUpdate(silent: true);
                });
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    DbManager.AddNotification(
                        "Update Ready to Install".T(),
                        string.Format("Version {0} is successfully downloaded. Click here to restart and install now.".T(), remoteVerStr),
                        "Success"
                    );
                });
            }
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Background download failed: {ex.Message}", "Software Updater", "Failed");
        }
    }

    public void ApplyAppTheme(bool dark)
    {
        Services.ThemeManager.Instance.ApplyTheme(dark ? ElementTheme.Dark : ElementTheme.Light);
    }

    public void SetBackdropType(string type)
    {
        try
        {
            this.SystemBackdrop = type.ToLower() switch
            {
                "mica" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base },
                "micaalt" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                "acrylic" => new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop(),
                _ => new Microsoft.UI.Xaml.Media.MicaBackdrop()
            };
        }
        catch { }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDark = RootGrid.RequestedTheme == ElementTheme.Dark;
        bool nextIsDark = !isCurrentlyDark;
        ApplyAppTheme(nextIsDark);

        // Update stored settings reactively
        WinCarePro.Services.Implementations.SettingsService.Instance.UpdateSettings(s =>
        {
            s.Theme = nextIsDark ? "Dark" : "Light";
        }, "Theme");
    }

    public void InstallDownloadedUpdate(bool silent = false)
    {
        if (string.IsNullOrEmpty(_downloadedSetupPath) || !File.Exists(_downloadedSetupPath))
        {
            var service = App.Services.GetService<Services.Contracts.INotificationService>();
            service?.ShowError("Installer Not Found".T(), "The downloaded update installer could not be found. Please check again.");
            return;
        }

        // SECURITY v4.2: Re-verify file integrity before execution
        if (!string.IsNullOrEmpty(_expectedUpdateHash))
        {
            if (!Infrastructure.Security.CryptoHelper.VerifyFileIntegrity(_downloadedSetupPath, _expectedUpdateHash))
            {
                var service = App.Services.GetService<Services.Contracts.INotificationService>();
                service?.ShowError("Security Alert".T(), "Installer integrity verification failed. The file may have been modified after download.");
                try { File.Delete(_downloadedSetupPath); } catch { }
                DbManager.LogAction("Update installer rejected: hash mismatch on pre-execution verify.", "Software Updater", "Failed");
                return;
            }
        }

        try
        {
            string args = silent 
                ? "/VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS" 
                : "/CLOSEAPPLICATIONS /RESTARTAPPLICATIONS";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _downloadedSetupPath,
                Arguments = args,
                UseShellExecute = true
            };

            DbManager.LogAction($"Launching update installer {(silent ? "(Silent Auto-Install)" : "")}...", "Software Updater", "Success");
            
            CleanupTrayIcon();

            System.Diagnostics.Process.Start(psi);
            
            // Terminate running process immediately to free file locks for installer overwrite
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            var service = App.Services.GetService<Services.Contracts.INotificationService>();
            service?.ShowError("Installation Failed".T(), string.Format("Could not start installer: {0}".T(), ex.Message));
        }
    }

    private void CheckAndShowChangelog(Version currentVersion)
    {
        try
        {
            string raw = DbManager.GetSettings();
            string lastVersionStr = "";
            bool versionChanged = false;

            if (!string.IsNullOrEmpty(raw))
            {
                using (var doc = JsonDocument.Parse(raw))
                {
                    if (doc.RootElement.TryGetProperty("LastVersion", out var verProp))
                    {
                        lastVersionStr = verProp.GetString() ?? "";
                    }
                }
            }

            if (string.IsNullOrEmpty(lastVersionStr))
            {
                versionChanged = true;
            }
            else
            {
                var lastVersion = new Version(lastVersionStr);
                if (currentVersion > lastVersion)
                {
                    versionChanged = true;
                }
            }

            if (versionChanged)
            {
                WinCarePro.Services.Implementations.SettingsService.Instance.UpdateSettings(p => p.LastVersion = currentVersion.ToString());

                // Log to Activity Log
                string logMessage = string.Format("System updated to version {0}".T(), currentVersion.ToString());
                DbManager.LogAction(logMessage, "System", "Success");
            }
        }
        catch { }
    }
}

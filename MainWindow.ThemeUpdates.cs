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
    public string CurrentBackdropType { get; private set; } = "MicaAlt";

    private void LoadThemeConfiguration()
    {
        try
        {
            var settings = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
            bool isDark = !string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase);
            
            RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
            ThemeIcon.Glyph = isDark ? "\uE708" : "\uE706";
            
            ApplyAppTheme(isDark);
            ThemeManager.Instance.AccentChanged += (s, e) => DispatcherQueue?.TryEnqueue(() =>
            {
                UpdateAuraMesh();
                ApplyTransparency(CurrentTransparencyLevel);
            });
            App.ApplyAccentColor(settings.AccentColor ?? "Default");
            SetBackdropType(settings.BackdropType ?? (isDark ? "micaalt" : "mica"));
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
        
        bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
        
        // Transparency level ranges from 10% to 100%.
        // At 10% (minimum transparency): solid/crisp contrast (alpha = 175)
        // At 100% (maximum transparency): lush frosted glass (alpha = 25)
        double clampedLevel = Math.Clamp(level, 10.0, 100.0);
        double fraction = (clampedLevel - 10.0) / 90.0; // 0.0 to 1.0
        byte colorAlpha = (byte)Math.Clamp((int)(175 - (fraction * 150)), 25, 185);
        
        if (isDark)
        {
            var accent = ThemeManager.Instance.CurrentAccent?.ToLower();
            if (accent == "cyberpunk" || accent == "neon")
            {
                RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 14, 12, 24));
            }
            else
            {
                RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 22, 24, 29));
            }
        }
        else
        {
            // In light mode, apply crisp Slate-Ice base (#F1F5F9) with smooth opacity fraction
            RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 241, 245, 249));
        }

        // Synchronize Nav menu pane background in MainPage
        if (RootFrame?.Content is MainPage mainPage)
        {
            mainPage.UpdateNavTransparencyAndBackdrop(clampedLevel, CurrentBackdropType);
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
                remoteVerStr = betaVerProp.GetString() ?? WinCarePro.Core.AppConstants.DefaultVersionString;
                downloadUrl = root.TryGetProperty("beta_url", out var betaUrlProp) ? betaUrlProp.GetString() ?? "" : "";
                expectedHash = root.TryGetProperty("beta_sha256", out var betaHashProp) ? betaHashProp.GetString() ?? "" : "";
            }
            else
            {
                remoteVerStr = root.GetProperty("version").GetString() ?? WinCarePro.Core.AppConstants.DefaultVersionString;
                downloadUrl = root.GetProperty("url").GetString() ?? "";
                expectedHash = root.TryGetProperty("sha256", out var hashProp) ? hashProp.GetString() ?? "" : "";
            }
            
            var currentVersion = WinCarePro.Core.AppConstants.CurrentVersion;
            string cleanRemoteVer = System.Text.RegularExpressions.Regex.Replace(remoteVerStr, @"[^\d\.]", "").TrimEnd('.');
            if (!Version.TryParse(cleanRemoteVer, out var remoteVersion))
            {
                remoteVersion = WinCarePro.Core.AppConstants.CurrentVersion;
            }

            if (remoteVersion > currentVersion)
            {
                DbManager.LogAction($"Update available: v{remoteVerStr}", "Software Updater", "Success");
                
                if (string.IsNullOrWhiteSpace(expectedHash))
                {
                    DbManager.LogAction($"Update v{remoteVerStr} rejected: Missing required SHA-256 checksum in update manifest.", "Software Updater", "Failed");
                    return;
                }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SilentUpdateCheck] Error: {ex.Message}");
            DbManager.LogAction($"Silent update check failed: {ex.Message}", "Software Updater", "Failed");
        }
    }

    private string _expectedUpdateHash = "";

    private async Task DownloadBackgroundUpdateAsync(string downloadUrl, string remoteVerStr, string expectedHash = "", bool autoInstall = false)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;

        if (!Infrastructure.Security.UpdateSecurityValidator.IsTrustedDownloadUrl(downloadUrl, out string? urlError))
        {
            DbManager.LogAction($"Update download rejected: Insecure or untrusted URL '{downloadUrl}'. Reason: {urlError}", "Software Updater", "Failed");
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            DbManager.LogAction($"Update download rejected: Missing required SHA-256 checksum for v{remoteVerStr}.", "Software Updater", "Failed");
            return;
        }
        
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

            // Strict Security Validation via UpdateSecurityValidator (SHA-256 + Authenticode + Publisher)
            var validation = Infrastructure.Security.UpdateSecurityValidator.ValidatePackageForInstallation(
                setupFilePath,
                expectedHash,
                Infrastructure.Security.UpdateSecurityValidator.DefaultExpectedPublisher);

            if (!validation.IsSuccess)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    DbManager.AddNotification(
                        "Update Security Alert".T(),
                        validation.Message,
                        "Error");
                });
                _downloadedSetupPath = null;
                return;
            }

            _downloadedSetupPath = setupFilePath;

            if (autoInstall)
            {
                DbManager.LogAction($"Update v{remoteVerStr} downloaded and verified. Initiating silent background update installation...", "Software Updater", "Success");
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
            string normalized = type?.ToLowerInvariant() ?? "micaalt";
            CurrentBackdropType = normalized;

            // Avoid tearing down DWM swapchain surface if the underlying backdrop can be reused
            if (normalized == "acrylic")
            {
                if (this.SystemBackdrop is not Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop)
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                }
            }
            else
            {
                var targetKind = (normalized == "mica")
                    ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
                    : Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt;

                if (this.SystemBackdrop is Microsoft.UI.Xaml.Media.MicaBackdrop existingMica)
                {
                    if (existingMica.Kind != targetKind)
                    {
                        existingMica.Kind = targetKind; // In-place kind update: zero screen jitter/flash
                    }
                }
                else
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = targetKind };
                }
            }

            UpdateAuraMesh();
            ApplyTransparency(CurrentTransparencyLevel);
        }
        catch { }
    }

    public void UpdateAuraMesh()
    {
        try
        {
            if (AmbientAuraMesh == null) return;

            string normalized = CurrentBackdropType?.ToLowerInvariant() ?? "micaalt";
            bool isAura = normalized == "auraglow";

            AmbientAuraMesh.Opacity = isAura ? 0.95 : 0.65;

            var accent = ThemeManager.Instance.CurrentAccent?.ToLowerInvariant() ?? "default";
            Windows.UI.Color color1 = accent switch
            {
                "green" => Windows.UI.Color.FromArgb(255, 16, 185, 129),
                "purple" => Windows.UI.Color.FromArgb(255, 168, 85, 247),
                "pink" => Windows.UI.Color.FromArgb(255, 244, 63, 94),
                "amber" => Windows.UI.Color.FromArgb(255, 245, 158, 11),
                "cyberpunk" or "neon" => Windows.UI.Color.FromArgb(255, 6, 182, 212),
                _ => Windows.UI.Color.FromArgb(255, 59, 130, 246)
            };

            Windows.UI.Color color2 = accent switch
            {
                "green" => Windows.UI.Color.FromArgb(255, 6, 182, 212),
                "purple" => Windows.UI.Color.FromArgb(255, 236, 72, 153),
                "pink" => Windows.UI.Color.FromArgb(255, 139, 92, 246),
                "amber" => Windows.UI.Color.FromArgb(255, 239, 68, 68),
                "cyberpunk" or "neon" => Windows.UI.Color.FromArgb(255, 217, 70, 239),
                _ => Windows.UI.Color.FromArgb(255, 139, 92, 246)
            };

            if (AuraGlowStop1 != null) AuraGlowStop1.Color = color1;
            if (AuraGlowStop2 != null) AuraGlowStop2.Color = color2;
        }
        catch { }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        bool nextIsDark = (Services.ThemeManager.Instance.CurrentTheme != ElementTheme.Dark);
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

        // Strict Authenticode and SHA-256 pre-execution validation
        var validation = Infrastructure.Security.UpdateSecurityValidator.ValidatePackageForInstallation(
            _downloadedSetupPath,
            _expectedUpdateHash,
            Infrastructure.Security.UpdateSecurityValidator.DefaultExpectedPublisher);

        if (!validation.IsSuccess)
        {
            var service = App.Services.GetService<Services.Contracts.INotificationService>();
            service?.ShowError("Security Alert".T(), validation.Message);
            _downloadedSetupPath = null;
            return;
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

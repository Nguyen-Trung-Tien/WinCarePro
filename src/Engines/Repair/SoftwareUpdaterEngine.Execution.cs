using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Infrastructure.Security;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class SoftwareUpdaterEngine
{
    public async Task<bool> UpdateApplicationAsync(string appId, string version = "", string updateEngine = "winget", System.Threading.CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(version))
        {
            var appDef = SupportedApps.FirstOrDefault(x => x.Id == appId);
            version = appDef?.LatestVersion ?? "1.0.0";
        }

        if (updateEngine == "direct")
        {
            return await UpdateApplicationDirectAsync(appId, version, cancellationToken);
        }

        Log($"Upgrading application: {appId} via WinGet...");
        var initReport = new SoftwareUpdateProgressReport
        {
            AppId = appId,
            Percent = 5,
            Phase = "Connecting",
            StatusText = "Connecting to WinGet package source...",
            DownloadUrl = "",
            BytesProgress = ""
        };
        UpdateProgressReported?.Invoke(initReport);
        ItemProgressChanged?.Invoke(appId, 5, initReport.StatusText);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("upgrade");
            psi.ArgumentList.Add("--id");
            psi.ArgumentList.Add(appId);
            psi.ArgumentList.Add("--exact");
            psi.ArgumentList.Add("--accept-package-agreements");
            psi.ArgumentList.Add("--accept-source-agreements");
            psi.ArgumentList.Add("--disable-interactivity");

            using var process = new Process { StartInfo = psi };

            string currentUrl = "";
            string currentBytes = "";
            string currentPhase = "Preparing";
            int currentPercent = 10;
            string currentSpeed = "";

            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                string cleanLine = AnsiRegex.Replace(e.Data, "").Trim();
                if (string.IsNullOrWhiteSpace(cleanLine)) return;

                Log(cleanLine);

                // Check for URL
                var urlMatch = DownloadUrlRegex.Match(cleanLine);
                if (urlMatch.Success)
                {
                    currentUrl = urlMatch.Groups[1].Value.Trim();
                    currentPhase = "Downloading";
                }

                // Check for Bytes (e.g. 167 MB / 360 MB)
                var bytesMatch = BytesProgressRegex.Match(cleanLine);
                if (bytesMatch.Success)
                {
                    currentBytes = bytesMatch.Value.Trim();
                    currentPhase = "Downloading";
                }

                // Check for Percentage
                var percentMatch = PercentPatternRegex.Match(cleanLine);
                if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int p))
                {
                    currentPercent = Math.Clamp(p, 0, 100);
                }

                // Phase detection
                if (cleanLine.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
                {
                    currentPhase = "Downloading";
                }
                else if (cleanLine.Contains("verif", StringComparison.OrdinalIgnoreCase) || cleanLine.Contains("hash", StringComparison.OrdinalIgnoreCase))
                {
                    currentPhase = "Verifying Hash";
                    currentPercent = Math.Max(currentPercent, 90);
                }
                else if (cleanLine.Contains("Starting package install", StringComparison.OrdinalIgnoreCase) || cleanLine.Contains("install", StringComparison.OrdinalIgnoreCase))
                {
                    currentPhase = "Installing";
                    currentPercent = Math.Max(currentPercent, 95);
                }

                string statusText = !string.IsNullOrEmpty(currentBytes) 
                    ? $"Downloading {currentBytes} ({currentPercent}%)"
                    : $"{currentPhase}...";

                var rep = new SoftwareUpdateProgressReport
                {
                    AppId = appId,
                    Percent = currentPercent,
                    Phase = currentPhase,
                    StatusText = statusText,
                    DownloadUrl = currentUrl,
                    BytesProgress = currentBytes,
                    SpeedText = currentSpeed
                };
                UpdateProgressReported?.Invoke(rep);
                ItemProgressChanged?.Invoke(appId, currentPercent, statusText);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log($"[winget err] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var reg = cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            });

            var exitTask = process.WaitForExitAsync(cancellationToken);
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(180000, cancellationToken));
            if (completedTask != exitTask)
            {
                try { if (!process.HasExited) process.Kill(true); } catch {}
                throw new TimeoutException("Winget upgrade timed out.");
            }

            bool ok = process.ExitCode == 0;
            Log($"Upgrade of {appId} finished. Exit Code: {process.ExitCode}");
            Database.DbManager.LogAction($"Update Software {appId}", "Software Updater", ok ? "Success" : "Failed");
            
            if (!ok)
            {
                var failRep = new SoftwareUpdateProgressReport
                {
                    AppId = appId,
                    Percent = 0,
                    Phase = "Failed",
                    StatusText = "Update Failed",
                    DownloadUrl = currentUrl,
                    BytesProgress = currentBytes
                };
                UpdateProgressReported?.Invoke(failRep);
                ItemProgressChanged?.Invoke(appId, 0, "Failed");
                return false;
            }

            var successRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 100,
                Phase = "Completed",
                StatusText = "Successfully Updated",
                DownloadUrl = currentUrl,
                BytesProgress = currentBytes
            };
            UpdateProgressReported?.Invoke(successRep);
            ItemProgressChanged?.Invoke(appId, 100, "Completed");
            Database.DbManager.SaveUpdatedApp(appId, version);
            return true;
        }
        catch (OperationCanceledException)
        {
            var cancelRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 0,
                Phase = "Cancelled",
                StatusText = "Cancelled"
            };
            UpdateProgressReported?.Invoke(cancelRep);
            ItemProgressChanged?.Invoke(appId, 0, "Cancelled");
            Log($"Update cancelled for {appId}.");
            return false;
        }
        catch (Exception ex)
        {
            Log($"Failed to run winget upgrade for {appId}: {ex.Message}");
            var errRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 0,
                Phase = "Failed",
                StatusText = $"Failed: {ex.Message}"
            };
            UpdateProgressReported?.Invoke(errRep);
            ItemProgressChanged?.Invoke(appId, 0, "Failed");
            return false;
        }
    }

    public async Task<bool> UpdateApplicationDirectAsync(string appId, string version = "", System.Threading.CancellationToken cancellationToken = default)
    {
        Log($"Upgrading application {appId} via WinCare Custom Downloader...");
        var app = SupportedApps.FirstOrDefault(x => x.Id == appId);
        if (app == null)
        {
            Log($"Unknown application ID: {appId}");
            return false;
        }

        if (string.IsNullOrEmpty(version))
        {
            version = app.LatestVersion;
        }

        var startReport = new SoftwareUpdateProgressReport
        {
            AppId = appId,
            Percent = 0,
            Phase = "Connecting",
            StatusText = "Connecting...",
            DownloadUrl = app.DownloadUrl,
            BytesProgress = ""
        };
        UpdateProgressReported?.Invoke(startReport);
        ItemProgressChanged?.Invoke(appId, 0, "Connecting...");

        string filePath = "";
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string cleanAppId = InputSanitizer.SanitizeFileName(app.Id, "app_setup");
            string cleanExt = app.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase) ? ".msi" : ".exe";
            string fileName = $"{cleanAppId}_setup{cleanExt}";
            filePath = Path.Combine(tempDir, fileName);

            int maxAttempts = 3;
            bool downloaded = false;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Log($"Downloading installer from: {app.DownloadUrl} (Attempt {attempt}/{maxAttempts})");
                    using (var response = await _httpClient.GetAsync(app.DownloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();

                        long? totalBytes = response.Content.Headers.ContentLength;
                        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        int lastReportedPercent = -1;
                        var speedStopwatch = Stopwatch.StartNew();
                        long lastBytesCount = 0;
                        double lastSeconds = 0;
                        string currentSpeedText = "";

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                            totalRead += read;

                            double elapsed = speedStopwatch.Elapsed.TotalSeconds;
                            if (elapsed - lastSeconds >= 0.4)
                            {
                                double bytesDiff = totalRead - lastBytesCount;
                                double timeDiff = elapsed - lastSeconds;
                                double mbPerSec = (bytesDiff / (1024.0 * 1024.0)) / Math.Max(timeDiff, 0.01);
                                currentSpeedText = mbPerSec >= 1.0 ? $"{mbPerSec:F1} MB/s" : $"{(mbPerSec * 1024.0):F0} KB/s";
                                lastBytesCount = totalRead;
                                lastSeconds = elapsed;
                            }

                            if (totalBytes.HasValue && totalBytes.Value > 0)
                            {
                                int percent = (int)((double)totalRead / totalBytes.Value * 100);
                                if (percent - lastReportedPercent >= 1 || percent == 100)
                                {
                                    string detail = $"{totalRead / 1024 / 1024} MB / {totalBytes.Value / 1024 / 1024} MB";
                                    string statusText = $"Downloading {percent}% ({detail})";

                                    var rep = new SoftwareUpdateProgressReport
                                    {
                                        AppId = appId,
                                        Percent = percent,
                                        Phase = "Downloading",
                                        StatusText = statusText,
                                        DownloadUrl = app.DownloadUrl,
                                        BytesProgress = detail,
                                        SpeedText = currentSpeedText
                                    };
                                    UpdateProgressReported?.Invoke(rep);
                                    ItemProgressChanged?.Invoke(appId, percent, statusText);

                                    if (percent - lastReportedPercent >= 10 || percent == 100)
                                    {
                                        Log($"Downloading: {percent}% ({detail})");
                                    }
                                    lastReportedPercent = percent;
                                }
                            }
                        }
                    }
                    downloaded = true;
                    break;
                }
                catch (System.Net.Http.HttpRequestException ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
                {
                    Log($"Download attempt {attempt} failed ({ex.Message}). Retrying in {attempt * 2}s...");
                    await Task.Delay(attempt * 2000, cancellationToken);
                }
            }

            if (!downloaded)
            {
                throw new IOException($"Failed to download {app.Name} after {maxAttempts} attempts.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            Log($"Download completed. Saved to: {filePath}");
            var verifyRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 95,
                Phase = "Verifying Signature",
                StatusText = "Verifying Authenticode signature...",
                DownloadUrl = app.DownloadUrl,
                BytesProgress = "Download Complete"
            };
            UpdateProgressReported?.Invoke(verifyRep);
            ItemProgressChanged?.Invoke(appId, 95, "Verifying Signature...");

            Log("Verifying Authenticode digital signature of the downloaded installer...");
            if (!VerifyDigitalSignature(filePath, app.ExpectedPublisher))
            {
                try { if (SafePathGuard.IsSafeToDelete(filePath)) File.Delete(filePath); } catch (Exception ex) { Log($"Notice: Failed to remove unverified installer file: {ex.Message}"); }
                throw new System.Security.SecurityException("The installer does not have a valid, trusted Authenticode digital signature or publisher mismatch.");
            }
            Log("Authenticode verification successful. The installer is signed and trusted.");

            Log($"Launching installer silently: {app.Name}");
            var installRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 98,
                Phase = "Installing",
                StatusText = "Installing silently...",
                DownloadUrl = app.DownloadUrl,
                BytesProgress = "Installing"
            };
            UpdateProgressReported?.Invoke(installRep);
            ItemProgressChanged?.Invoke(appId, 98, "Installing...");

            string msiexecPath = Path.Combine(Environment.SystemDirectory, "msiexec.exe");
            var psi = new ProcessStartInfo
            {
                FileName = cleanExt.Equals(".msi", StringComparison.OrdinalIgnoreCase) ? msiexecPath : filePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? Environment.SystemDirectory
            };

            if (cleanExt.Equals(".msi", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("/i");
                psi.ArgumentList.Add(filePath);
                if (!string.IsNullOrWhiteSpace(app.SilentArguments))
                {
                    foreach (var arg in app.SilentArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        psi.ArgumentList.Add(arg);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(app.SilentArguments))
            {
                foreach (var arg in app.SilentArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    psi.ArgumentList.Add(arg);
                }
            }

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception("Failed to start installer process.");
            }

            using var procReg = cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); }
                catch (Exception killEx) { Infrastructure.Logging.CrashLogger.LogException("SoftwareUpdaterEngine.KillOnCancel", killEx); }
            });

            Log("Installer running in background, waiting for completion...");
            
            var exitTask = process.WaitForExitAsync(cancellationToken);
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(180000, cancellationToken));
            if (completedTask != exitTask)
            {
                try { if (!process.HasExited) process.Kill(true); }
                catch (Exception killEx) { Infrastructure.Logging.CrashLogger.LogException("SoftwareUpdaterEngine.KillOnTimeout", killEx); }
                throw new TimeoutException("Installer process timed out.");
            }

            bool ok = process.ExitCode == 0;
            Log($"Installation finished for {app.Name}. Exit code: {process.ExitCode}");
            Database.DbManager.LogAction($"Update Software {appId}", "Software Updater", ok ? "Success" : "Failed");
            
            try { if (SafePathGuard.IsSafeToDelete(filePath)) File.Delete(filePath); } catch (Exception ex) { Log($"Notice: Failed to clean up installer file: {ex.Message}"); }

            if (!ok)
            {
                var failRep = new SoftwareUpdateProgressReport
                {
                    AppId = appId,
                    Percent = 0,
                    Phase = "Failed",
                    StatusText = "Installation Failed"
                };
                UpdateProgressReported?.Invoke(failRep);
                ItemProgressChanged?.Invoke(appId, 0, "Failed");
                return false;
            }

            var compRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 100,
                Phase = "Completed",
                StatusText = "Completed"
            };
            UpdateProgressReported?.Invoke(compRep);
            ItemProgressChanged?.Invoke(appId, 100, "Completed");
            Database.DbManager.SaveUpdatedApp(appId, version);
            return true;
        }
        catch (OperationCanceledException)
        {
            var cancelRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 0,
                Phase = "Cancelled",
                StatusText = "Cancelled"
            };
            UpdateProgressReported?.Invoke(cancelRep);
            ItemProgressChanged?.Invoke(appId, 0, "Cancelled");
            Log($"Direct update cancelled for {app.Name}.");
            return false;
        }
        catch (Exception ex)
        {
            Log($"Failed to update {app.Name}: {ex.Message}");
            var errRep = new SoftwareUpdateProgressReport
            {
                AppId = appId,
                Percent = 0,
                Phase = "Failed",
                StatusText = $"Failed: {ex.Message}"
            };
            UpdateProgressReported?.Invoke(errRep);
            ItemProgressChanged?.Invoke(appId, 0, "Failed");
            return false;
        }
        finally
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && SafePathGuard.IsSafeToDelete(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch { }
        }
    }

    #region Authenticode Validation Delegation

    public static bool VerifyDigitalSignature(string filePath, string? expectedPublisher = null)
    {
        return WinCarePro.Infrastructure.Security.UpdateSecurityValidator.VerifyAuthenticodeSignature(filePath, expectedPublisher, out _);
    }

    #endregion

}

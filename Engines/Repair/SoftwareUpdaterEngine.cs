using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class SoftwareUpdateProgressReport
{
    public string AppId { get; set; } = "";
    public int Percent { get; set; }
    public string Phase { get; set; } = "Updating";
    public string StatusText { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string BytesProgress { get; set; } = "";
    public string SpeedText { get; set; } = "";
}

public class SoftwareUpdaterEngine
{
    private static readonly System.Net.Http.HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10),
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WinCarePro/4.2" } }
    };

    public event Action<string>? OutputReceived;
    public event Action<string, int, string>? ItemProgressChanged; // (appId, percent, statusText)
    public event Action<SoftwareUpdateProgressReport>? UpdateProgressReported;
    private void Log(string msg) => OutputReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");

    private class AppDefinition
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
        public string RegistryNameQuery { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string SilentArguments { get; set; } = "";
        public string FileExtension { get; set; } = ".exe";
        public string ExpectedPublisher { get; set; } = "";
    }

    private static readonly List<AppDefinition> SupportedApps = new()
    {
        new AppDefinition
        {
            Name = "Git for Windows",
            Id = "Git.Git",
            RegistryNameQuery = "Git",
            LatestVersion = "2.48.1",
            DownloadUrl = "https://github.com/git-for-windows/git/releases/download/v2.48.1.windows.1/Git-2.48.1-64-bit.exe",
            SilentArguments = "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            FileExtension = ".exe",
            ExpectedPublisher = "Git Development Community"
        },
        new AppDefinition
        {
            Name = "Visual Studio Code",
            Id = "Microsoft.VisualStudioCode",
            RegistryNameQuery = "Visual Studio Code",
            LatestVersion = "1.98.2",
            DownloadUrl = "https://update.code.visualstudio.com/latest/win32-x64-user/stable",
            SilentArguments = "/VERYSILENT /MERGETASKS=!runcode /NORESTART",
            FileExtension = ".exe",
            ExpectedPublisher = "Microsoft Corporation"
        },
        new AppDefinition
        {
            Name = "Node.js (LTS)",
            Id = "OpenJS.NodeJS.LTS",
            RegistryNameQuery = "Node.js",
            LatestVersion = "22.14.0",
            DownloadUrl = "https://nodejs.org/dist/v22.14.0/node-v22.14.0-x64.msi",
            SilentArguments = "/qn /norestart",
            FileExtension = ".msi",
            ExpectedPublisher = "OpenJS Foundation"
        },
        new AppDefinition
        {
            Name = "Mozilla Firefox",
            Id = "Mozilla.Firefox",
            RegistryNameQuery = "Mozilla Firefox",
            LatestVersion = "138.0.1",
            DownloadUrl = "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=en-US",
            SilentArguments = "/S",
            FileExtension = ".exe",
            ExpectedPublisher = "Mozilla Corporation"
        },
        new AppDefinition
        {
            Name = "Google Chrome",
            Id = "Google.Chrome",
            RegistryNameQuery = "Google Chrome",
            LatestVersion = "136.0.7103.93",
            DownloadUrl = "https://dl.google.com/tag/s/appguid%3D%7B8A91EB1D-223C-4C1B-87BD-78F4B7E1857A%7D%26iid%3D%7B%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dtrue%26ap%3Dx64-stable-statsdef_1/update2/installers/ChromeSetup.exe",
            SilentArguments = "/silent /install",
            FileExtension = ".exe",
            ExpectedPublisher = "Google LLC"
        },
        // v3.0 Nova — 5 new popular apps
        new AppDefinition
        {
            Name = "VLC Media Player",
            Id = "VideoLAN.VLC",
            RegistryNameQuery = "VLC media player",
            LatestVersion = "3.0.21",
            DownloadUrl = "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe",
            SilentArguments = "/S /L=1033",
            FileExtension = ".exe",
            ExpectedPublisher = "VideoLAN"
        },
        new AppDefinition
        {
            Name = "7-Zip",
            Id = "7zip.7zip",
            RegistryNameQuery = "7-Zip",
            LatestVersion = "24.09",
            DownloadUrl = "https://www.7-zip.org/a/7z2409-x64.exe",
            SilentArguments = "/S",
            FileExtension = ".exe",
            ExpectedPublisher = "Igor Pavlov"
        },
        new AppDefinition
        {
            Name = "Notepad++",
            Id = "Notepad++.Notepad++",
            RegistryNameQuery = "Notepad++",
            LatestVersion = "8.7.7",
            DownloadUrl = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.7/npp.8.7.7.Installer.x64.exe",
            SilentArguments = "/S",
            FileExtension = ".exe",
            ExpectedPublisher = "Don HO"
        },
        new AppDefinition
        {
            Name = "Python",
            Id = "Python.Python.3.13",
            RegistryNameQuery = "Python",
            LatestVersion = "3.13.3",
            DownloadUrl = "https://www.python.org/ftp/python/3.13.3/python-3.13.3-amd64.exe",
            SilentArguments = "/quiet InstallAllUsers=1 PrependPath=1",
            FileExtension = ".exe",
            ExpectedPublisher = "Python Software Foundation"
        },
        new AppDefinition
        {
            Name = "WinRAR",
            Id = "RARLab.WinRAR",
            RegistryNameQuery = "WinRAR",
            LatestVersion = "7.10",
            DownloadUrl = "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-710.exe",
            SilentArguments = "/S",
            FileExtension = ".exe",
            ExpectedPublisher = "win.rar GmbH"
        }
    };

    private static readonly Regex AnsiRegex = new(@"\x1B\[[^@-~]*[@-~]", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRegex = new(@"[^\d\.]", RegexOptions.Compiled);
    private static readonly Regex VersionNumRegex = new(@"^\d", RegexOptions.Compiled);
    private static readonly Regex DotNumberRegex = new(@"^\d+(\.\d+)+$", RegexOptions.Compiled);
    private static readonly Regex DownloadUrlRegex = new(@"Downloading\s+(https?://[^\s\r\n]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BytesProgressRegex = new(@"(\d+(?:\.\d+)?\s*(?:B|KB|MB|GB|TB))\s*/\s*(\d+(?:\.\d+)?\s*(?:B|KB|MB|GB|TB))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PercentPatternRegex = new(@"(\d{1,3})%", RegexOptions.Compiled);

    public async Task<List<SoftwareUpdateInfo>> ScanUpdatesAsync(string updateEngine = "winget", System.Threading.CancellationToken cancellationToken = default)
    {
        var list = new List<SoftwareUpdateInfo>();
        var updatedApps = Database.DbManager.GetUpdatedApps();

        if (updateEngine == "direct")
        {
            Log("Scanning local system registries for outdated third-party applications...");
            await Task.Delay(500, cancellationToken);

            try
            {
                foreach (var app in SupportedApps)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (updatedApps.TryGetValue(app.Id, out string? storedVer))
                    {
                        if (!IsVersionOlder(storedVer, app.LatestVersion))
                        {
                            continue; // Already updated to this version or newer
                        }
                    }

                    string? installedVer = GetInstalledVersionFromRegistry(app.RegistryNameQuery);
                    if (installedVer != null)
                    {
                        if (IsVersionOlder(installedVer, app.LatestVersion))
                        {
                            list.Add(new SoftwareUpdateInfo
                            {
                                Name = app.Name,
                                Id = app.Id,
                                InstalledVersion = installedVer,
                                AvailableVersion = app.LatestVersion,
                                Source = "direct"
                            });
                            Log($"Found outdated application: {app.Name} (Installed: {installedVer}, Available: {app.LatestVersion})");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log("Registry software scan was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Log($"Registry software scan failed: {ex.Message}");
            }

#if DEBUG
            if (list.Count == 0 && !cancellationToken.IsCancellationRequested)
            {
                Log("No installed outdated applications found in registry. Listing simulated updates for testing...");
                await Task.Delay(500, cancellationToken);
                AddSimulatedItem(list, updatedApps, "Git for Windows", "Git.Git", "2.40.1", "2.48.1", "direct");
                AddSimulatedItem(list, updatedApps, "Visual Studio Code", "Microsoft.VisualStudioCode", "1.85.0", "1.98.2", "direct");
                AddSimulatedItem(list, updatedApps, "Node.js (LTS)", "OpenJS.NodeJS.LTS", "20.10.0", "22.14.0", "direct");
                AddSimulatedItem(list, updatedApps, "Mozilla Firefox", "Mozilla.Firefox", "120.0", "138.0.1", "direct");
                AddSimulatedItem(list, updatedApps, "Google Chrome", "Google.Chrome", "121.0.6167.85", "136.0.7103.93", "direct");
            }
#endif
        }
        else
        {
            Log("Scanning for software updates via Windows Package Manager (winget)...");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = "upgrade --accept-source-agreements --disable-interactivity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                using var registration = cancellationToken.Register(() =>
                {
                    try { if (!process.HasExited) process.Kill(true); } catch { }
                });

                var readTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var exitTask = process.WaitForExitAsync(cancellationToken);
                var completedTask = await Task.WhenAny(exitTask, Task.Delay(12000, cancellationToken));
                
                if (completedTask != exitTask)
                {
                    try { if (!process.HasExited) process.Kill(true); } catch { }
                    Log("Winget scan response delayed (>12s), transitioning to fast registry audit...");
                }
                else
                {
                    string output = await readTask;
                    if (process.ExitCode == 0 || !string.IsNullOrEmpty(output))
                    {
                        var parsedList = ParseWingetUpgradeOutput(output);
                        foreach (var item in parsedList)
                        {
                            if (updatedApps.TryGetValue(item.Id, out string? storedVer))
                            {
                                if (!IsVersionOlder(storedVer, item.AvailableVersion))
                                {
                                    continue;
                                }
                            }
                            list.Add(item);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log("Winget scan cancelled by user.");
                throw;
            }
            catch (Exception ex)
            {
                Log($"Winget query failed: {ex.Message}. Using secondary application updater...");
            }

            if (list.Count == 0 && !cancellationToken.IsCancellationRequested)
            {
                Log("Performing system registries software scan...");
                await Task.Delay(300, cancellationToken);
                
                foreach (var app in SupportedApps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddSimulatedItem(list, updatedApps, app.Name, app.Id, "1.0.0", app.LatestVersion, "winget");
                }
            }
        }

        Log($"Found {list.Count} software updates available.");
        return list;
    }

    private void AddSimulatedItem(List<SoftwareUpdateInfo> list, Dictionary<string, string> updatedApps, string name, string id, string installedVersion, string availableVersion, string source)
    {
        var app = SupportedApps.FirstOrDefault(x => x.Id == id);
        string? actualInstalledVer = app != null ? GetInstalledVersionFromRegistry(app.RegistryNameQuery) : null;
        
        if (string.IsNullOrEmpty(actualInstalledVer))
        {
            return; // App is not installed on the system, do not list it
        }

        string realAvailableVersion = app?.LatestVersion ?? availableVersion;
        string currentVer = actualInstalledVer;
        
        if (!IsVersionOlder(actualInstalledVer, realAvailableVersion))
        {
            return; // Already up to date or newer on the system
        }

        if (updatedApps.TryGetValue(id, out string? storedVer))
        {
            if (!IsVersionOlder(storedVer, realAvailableVersion))
            {
                return; // Already updated to this or a newer version in DB
            }
        }
        list.Add(new SoftwareUpdateInfo { Name = name, Id = id, InstalledVersion = currentVer, AvailableVersion = realAvailableVersion, Source = source });
    }


    private string? GetInstalledVersionFromRegistry(string displayNameQuery)
    {
        string[] registryRoots = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };
        
        foreach (var rootPath in registryRoots)
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(rootPath);
            if (key != null)
            {
                var version = FindVersionInKey(key, displayNameQuery);
                if (version != null) return version;
            }
        }

        foreach (var rootPath in registryRoots)
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(rootPath);
            if (key != null)
            {
                var version = FindVersionInKey(key, displayNameQuery);
                if (version != null) return version;
            }
        }

        return null;
    }

    private string? FindVersionInKey(Microsoft.Win32.RegistryKey key, string displayNameQuery)
    {
        foreach (var subkeyName in key.GetSubKeyNames())
        {
            using var subkey = key.OpenSubKey(subkeyName);
            if (subkey != null)
            {
                var displayName = subkey.GetValue("DisplayName") as string;
                if (!string.IsNullOrEmpty(displayName) && displayName.Contains(displayNameQuery, StringComparison.OrdinalIgnoreCase))
                {
                    if (displayNameQuery.Equals("Git", StringComparison.OrdinalIgnoreCase))
                    {
                        if (displayName.Contains("GitHub", StringComparison.OrdinalIgnoreCase) || 
                            displayName.Contains("LFS", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    var displayVersion = subkey.GetValue("DisplayVersion") as string;
                    if (!string.IsNullOrEmpty(displayVersion))
                    {
                        return displayVersion;
                    }
                }
            }
        }
        return null;
    }

    private bool IsVersionOlder(string installed, string available)
    {
        try
        {
            var instClean = DigitsOnlyRegex.Replace(installed, "");
            var availClean = DigitsOnlyRegex.Replace(available, "");
            
            if (Version.TryParse(instClean, out Version? vInst) && Version.TryParse(availClean, out Version? vAvail))
            {
                return vInst < vAvail;
            }
        }
        catch {}
        return string.Compare(installed, available, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private List<SoftwareUpdateInfo> ParseWingetUpgradeOutput(string output)
    {
        var list = new List<SoftwareUpdateInfo>();
        if (string.IsNullOrWhiteSpace(output)) return list;

        // Clean ANSI control sequences (spinners, colors)
        string cleanOutput = AnsiRegex.Replace(output, "");
        var lines = cleanOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int nameStart = -1, idStart = -1, verStart = -1, availStart = -1, srcStart = -1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.Contains("------") || line.StartsWith("---"))
            {
                continue;
            }

            // Check if this is the Header line
            if (line.Contains("Name") && line.Contains("Id") && line.Contains("Version") && line.Contains("Available"))
            {
                nameStart = line.IndexOf("Name");
                idStart = line.IndexOf("Id");
                verStart = line.IndexOf("Version");
                availStart = line.IndexOf("Available");
                srcStart = line.IndexOf("Source");
                continue;
            }

            bool slicedSuccess = false;

            // Strategy 1: Fixed Column slicing based on Header indices
            if (nameStart >= 0 && idStart > nameStart && verStart > idStart && availStart > verStart && line.Length >= availStart)
            {
                try
                {
                    string name = line.Substring(nameStart, Math.Min(idStart - nameStart, line.Length - nameStart)).Trim();
                    string id = line.Substring(idStart, Math.Min(verStart - idStart, line.Length - idStart)).Trim();
                    string installedVer = line.Substring(verStart, Math.Min(availStart - verStart, line.Length - verStart)).Trim();
                    string availableVer = (srcStart > availStart && line.Length > availStart) 
                        ? line.Substring(availStart, Math.Min(srcStart - availStart, line.Length - availStart)).Trim()
                        : (line.Length > availStart ? line.Substring(availStart).Trim() : "");
                    
                    string source = (srcStart > 0 && line.Length > srcStart) ? line.Substring(srcStart).Trim() : "winget";

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(installedVer) && !string.IsNullOrEmpty(availableVer))
                    {
                        if (id.Contains(".") && !id.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new SoftwareUpdateInfo
                            {
                                Name = name,
                                Id = id,
                                InstalledVersion = installedVer,
                                AvailableVersion = availableVer,
                                Source = string.IsNullOrEmpty(source) ? "winget" : source
                            });
                            slicedSuccess = true;
                        }
                    }
                }
                catch
                {
                    slicedSuccess = false;
                }
            }

            if (slicedSuccess) continue;

            // Strategy 2: Token pattern matching fallback
            var tokens = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 4)
            {
                for (int i = 1; i <= tokens.Length - 3; i++)
                {
                    string candId = tokens[i];
                    string candVer = tokens[i + 1];
                    string candAvail = tokens[i + 2];

                    if (candId.Contains(".") && 
                        !DotNumberRegex.IsMatch(candId) &&
                        VersionNumRegex.IsMatch(candVer) &&
                        VersionNumRegex.IsMatch(candAvail))
                    {
                        string name = string.Join(" ", tokens.Take(i));
                        string source = (i + 3 < tokens.Length) ? tokens[i + 3] : "winget";

                        // Avoid duplicates if added by Strategy 1
                        if (!list.Any(x => x.Id.Equals(candId, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(new SoftwareUpdateInfo
                            {
                                Name = name,
                                Id = candId,
                                InstalledVersion = candVer,
                                AvailableVersion = candAvail,
                                Source = source
                            });
                        }
                        break;
                    }
                }
            }
        }

        return list;
    }

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
                Arguments = $"upgrade --id \"{appId}\" --exact --accept-package-agreements --accept-source-agreements --disable-interactivity",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                CreateNoWindow = true
            };

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

        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string fileName = $"{app.Id}_setup{app.FileExtension}";
            string filePath = Path.Combine(tempDir, fileName);

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
                try { File.Delete(filePath); } catch {}
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

            var psi = new ProcessStartInfo
            {
                FileName = app.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase) ? "msiexec.exe" : filePath,
                Arguments = app.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase) 
                    ? $"/i \"{filePath}\" {app.SilentArguments}" 
                    : app.SilentArguments,
                UseShellExecute = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception("Failed to start installer process.");
            }

            using var procReg = cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            });

            Log("Installer running in background, waiting for completion...");
            
            var exitTask = process.WaitForExitAsync(cancellationToken);
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(180000, cancellationToken));
            if (completedTask != exitTask)
            {
                try { if (!process.HasExited) process.Kill(true); } catch {}
                throw new TimeoutException("Installer process timed out.");
            }

            bool ok = process.ExitCode == 0;
            Log($"Installation finished for {app.Name}. Exit code: {process.ExitCode}");
            Database.DbManager.LogAction($"Update Software {appId}", "Software Updater", ok ? "Success" : "Failed");
            
            try { File.Delete(filePath); } catch {}

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
    }

    #region Win32 WinVerifyTrust Authenticode Validation

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
        public string? pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    private const uint WTD_UI_NONE = 2;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_STATEACTION_IGNORE = 0;
    private const uint WTD_REVOCATION_CHECK_NONE = 0x00000010;
    private const uint WTD_SAFER_FLAG = 0x00000100;

    private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

    [System.Runtime.InteropServices.DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int WinVerifyTrust(
        IntPtr hwnd,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStruct)] Guid pgActionID,
        IntPtr pWVTData
    );

    public static bool VerifyDigitalSignature(string filePath, string? expectedPublisher = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

        var fileInfo = new WINTRUST_FILE_INFO
        {
            cbStruct = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        IntPtr pFileInfo = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
        IntPtr pWVTData = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_DATA)));

        try
        {
            System.Runtime.InteropServices.Marshal.StructureToPtr(fileInfo, pFileInfo, false);

            var trustData = new WINTRUST_DATA
            {
                cbStruct = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_DATA)),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = pFileInfo,
                dwStateAction = WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = WTD_REVOCATION_CHECK_NONE | WTD_SAFER_FLAG,
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            System.Runtime.InteropServices.Marshal.StructureToPtr(trustData, pWVTData, false);

            int result = WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION_GENERIC_VERIFY_V2, pWVTData);
            bool isTrustValid = (result == 0); // 0 = ERROR_SUCCESS

            if (!isTrustValid)
            {
                return false;
            }

            // Verify expected publisher if specified
            if (!string.IsNullOrEmpty(expectedPublisher))
            {
                try
                {
#pragma warning disable SYSLIB0057
                    using var cert = new X509Certificate2(filePath);
                    if (cert == null || string.IsNullOrEmpty(cert.Subject))
                    {
                        return false;
                    }
                    if (!cert.Subject.Contains(expectedPublisher, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
#pragma warning restore SYSLIB0057
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (pFileInfo != IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(pFileInfo);
            if (pWVTData != IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(pWVTData);
        }
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class UninstallEngine
{
    public event Action<string>? OutputReceived;
    public event Action<int>? ProgressChanged;
    
    private void Log(string msg) => OutputReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");
    
    public List<InstalledAppInfo> ScanInstalledApps()
    {
        var appList = new List<InstalledAppInfo>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        string[] registryPaths = 
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };
        
        var hives = new[] 
        {
            (Registry.LocalMachine, "HKLM"),
            (Registry.CurrentUser, "HKCU")
        };
        
        foreach (var (baseKey, hiveName) in hives)
        {
            foreach (var path in registryPaths)
            {
                try
                {
                    using var uninstallKey = baseKey.OpenSubKey(path);
                    if (uninstallKey == null) continue;
                    
                    var subkeys = uninstallKey.GetSubKeyNames();
                    foreach (var subkeyName in subkeys)
                    {
                        try
                        {
                            using var subkey = uninstallKey.OpenSubKey(subkeyName);
                            if (subkey == null) continue;
                            
                            var displayName = subkey.GetValue("DisplayName")?.ToString();
                            if (string.IsNullOrWhiteSpace(displayName)) continue;
                            
                            var systemComponent = subkey.GetValue("SystemComponent");
                            if (systemComponent != null && Convert.ToInt32(systemComponent) == 1) continue;
                            
                            var parentKeyName = subkey.GetValue("ParentKeyName")?.ToString();
                            if (!string.IsNullOrEmpty(parentKeyName)) continue;
                            
                            var uninstallString = subkey.GetValue("UninstallString")?.ToString();
                            if (string.IsNullOrWhiteSpace(uninstallString)) continue;
                            
                            var publisher = subkey.GetValue("Publisher")?.ToString() ?? "Unknown Publisher";
                            var version = subkey.GetValue("DisplayVersion")?.ToString() ?? "Unknown";
                            var installDateRaw = subkey.GetValue("InstallDate")?.ToString() ?? "";
                            var installLocation = subkey.GetValue("InstallLocation")?.ToString() ?? "";
                            var displayIcon = subkey.GetValue("DisplayIcon")?.ToString() ?? "";
                            
                            long sizeBytes = 0;
                            var estimatedSizeVal = subkey.GetValue("EstimatedSize");
                            if (estimatedSizeVal != null)
                            {
                                if (long.TryParse(estimatedSizeVal.ToString(), out long sizeKb))
                                {
                                    sizeBytes = sizeKb * 1024;
                                }
                            }
                            
                            string installDate = "";
                            if (!string.IsNullOrEmpty(installDateRaw) && installDateRaw.Length == 8)
                            {
                                installDate = $"{installDateRaw.Substring(0, 4)}-{installDateRaw.Substring(4, 2)}-{installDateRaw.Substring(6, 2)}";
                            }
                            
                            string iconPath = "";
                            if (!string.IsNullOrWhiteSpace(displayIcon))
                            {
                                iconPath = ExtractIconFile(displayIcon, subkeyName);
                            }
                            
                            var appInfo = new InstalledAppInfo
                            {
                                DisplayName = displayName,
                                Publisher = publisher,
                                Version = version,
                                InstallDate = installDate,
                                InstallLocation = installLocation,
                                UninstallString = uninstallString,
                                RegistryKeyName = subkeyName,
                                Hive = hiveName,
                                RegistryPath = Path.Combine(path, subkeyName),
                                SizeBytes = sizeBytes,
                                DisplayIcon = displayIcon,
                                IsStoreApp = false,
                                IconPath = iconPath
                            };
                            
                            if (seenNames.Add(displayName + "_" + version))
                            {
                                appList.Add(appInfo);
                            }
                        }
                        catch {}
                    }
                }
                catch {}
            }
        }
        
        // 2. Scan Microsoft Store (packaged) applications
        try
        {
            var packageManager = new Windows.Management.Deployment.PackageManager();
            var packages = packageManager.FindPackagesForUser("");
            
            foreach (var package in packages)
            {
                try
                {
                    if (package.IsFramework || package.IsResourcePackage) 
                        continue;
                    
                    var appEntries = package.GetAppListEntries();
                    if (appEntries == null || appEntries.Count == 0) 
                        continue;
                    
                    string displayName = package.DisplayName;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = appEntries[0].DisplayInfo.DisplayName;
                    }
                    if (string.IsNullOrWhiteSpace(displayName)) continue;
                    
                    if (displayName.StartsWith("Microsoft.") || displayName.StartsWith("Windows."))
                    {
                        if (package.SignatureKind == Windows.ApplicationModel.PackageSignatureKind.System) continue;
                    }
                    
                    string publisher = package.PublisherDisplayName;
                    if (string.IsNullOrWhiteSpace(publisher))
                    {
                        publisher = "Microsoft Corporation";
                    }
                    
                    var v = package.Id.Version;
                    string version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                    
                    string installLocation = "";
                    try
                    {
                        installLocation = package.InstalledLocation?.Path ?? "";
                    }
                    catch {}
                    
                    string iconPath = "";
                    try
                    {
                        iconPath = package.Logo?.ToString() ?? "";
                    }
                    catch {}
                    
                    var appInfo = new InstalledAppInfo
                    {
                        DisplayName = displayName,
                        Publisher = publisher,
                        Version = version,
                        InstallLocation = installLocation,
                        UninstallString = package.Id.FullName, // Store FullName for uninstall
                        RegistryKeyName = package.Id.Name,
                        Hive = "Store",
                        RegistryPath = "",
                        SizeBytes = 0,
                        IsStoreApp = true,
                        IconPath = iconPath
                    };
                    
                    if (seenNames.Add("Store_" + displayName + "_" + version))
                    {
                        appList.Add(appInfo);
                    }
                }
                catch {}
            }
        }
        catch (Exception ex)
        {
            Log($"Error scanning Microsoft Store apps: {ex.Message}");
        }
        
        // Add simulated entries for development testing
        appList.Add(new InstalledAppInfo
        {
            DisplayName = "Mock Trash App",
            Publisher = "TrashySoft",
            Version = "4.2.1",
            InstallDate = "2026-05-10",
            InstallLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MockTrashApp"),
            UninstallString = "cmd.exe /c echo [Mock] Uninstalling Mock Trash App... & timeout /t 2",
            RegistryKeyName = "MockTrashApp",
            Hive = "HKCU",
            RegistryPath = @"SOFTWARE\MockTrashApp",
            SizeBytes = 128 * 1024 * 1024
        });

        appList.Add(new InstalledAppInfo
        {
            DisplayName = "WinCare Pro Helper Extension",
            Publisher = "Nguyen-Trung-Tien",
            Version = "1.0.0",
            InstallDate = "2026-06-15",
            InstallLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCareProHelper"),
            UninstallString = "cmd.exe /c echo [Mock] Uninstalling WinCare Pro Helper Extension... & timeout /t 2",
            RegistryKeyName = "WinCareProHelper",
            Hive = "HKCU",
            RegistryPath = @"SOFTWARE\WinCareProHelper",
            SizeBytes = 12 * 1024 * 1024
        });

        appList.Add(new InstalledAppInfo
        {
            DisplayName = "Mock Store Game",
            Publisher = "MockStorePublisher",
            Version = "1.0.4.0",
            InstallLocation = "",
            UninstallString = "MockStoreGame_1.0.4.0_x64__8wekyb3d8bbwe",
            RegistryKeyName = "MockStoreGame",
            Hive = "Store",
            RegistryPath = "",
            SizeBytes = 0,
            IsStoreApp = true,
            IconPath = ""
        });
        
        return appList.OrderBy(x => x.DisplayName).ToList();
    }
    
    public async Task<bool> RunStandardUninstallerAsync(InstalledAppInfo app)
    {
        if (app.IsStoreApp)
        {
            return await UninstallStoreAppAsync(app.UninstallString);
        }

        Log($"Launching standard uninstaller for: {app.DisplayName}");
        ProgressChanged?.Invoke(25);
        try
        {
            string cmd = app.UninstallString.Trim();
            string exe = "";
            string args = "";
            
            // Robust parsing of UninstallString
            if (cmd.StartsWith("\""))
            {
                int index = cmd.IndexOf("\"", 1);
                if (index > 0)
                {
                    exe = cmd.Substring(1, index - 1).Trim();
                    args = cmd.Substring(index + 1).Trim();
                }
                else
                {
                    exe = cmd.Replace("\"", "").Trim();
                }
            }
            else
            {
                // Look for typical executable extensions to split
                string[] extensions = { ".exe", ".msi", ".bat", ".cmd" };
                bool parsed = false;
                foreach (var ext in extensions)
                {
                    int extIndex = cmd.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                    if (extIndex > 0)
                    {
                        exe = cmd.Substring(0, extIndex + ext.Length).Trim();
                        args = cmd.Substring(extIndex + ext.Length).Trim();
                        parsed = true;
                        break;
                    }
                }
                
                if (!parsed)
                {
                    // Unquoted path with spaces: attempt to resolve by checking files
                    int lastSpace = cmd.Length;
                    while (lastSpace > 0)
                    {
                        string candidate = cmd.Substring(0, lastSpace).Trim();
                        if (File.Exists(candidate))
                        {
                            exe = candidate;
                            args = cmd.Substring(lastSpace).Trim();
                            parsed = true;
                            break;
                        }
                        lastSpace = candidate.LastIndexOf(' ');
                    }
                    
                    if (!parsed)
                    {
                        // Fallback: split on first space
                        int spaceIndex = cmd.IndexOf(" ");
                        if (spaceIndex > 0)
                        {
                            exe = cmd.Substring(0, spaceIndex).Trim();
                            args = cmd.Substring(spaceIndex + 1).Trim();
                        }
                        else
                        {
                            exe = cmd;
                        }
                    }
                }
            }
            
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas"
            };
            
            // Set working directory to installation folder if valid
            if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            {
                psi.WorkingDirectory = app.InstallLocation;
            }
            else if (!string.IsNullOrEmpty(exe))
            {
                try
                {
                    string dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        psi.WorkingDirectory = dir;
                    }
                }
                catch {}
            }
            
            ProgressChanged?.Invoke(50);
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return false;
            
            Log("Standard uninstaller launched. Please follow the prompt/UI instruction to finish uninstallation.");
            ProgressChanged?.Invoke(75);
            await process.WaitForExitAsync();
            Log($"Standard uninstaller exited. Exit Code: {process.ExitCode}");
            ProgressChanged?.Invoke(100);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error launching standard uninstaller: {ex.Message}");
            return false;
        }
    }
    
    public List<LeftoverItem> ScanLeftovers(InstalledAppInfo app)
    {
        ProgressChanged?.Invoke(15);
        var leftovers = new List<LeftoverItem>();
        string cleanName = CleanAppNameForMatching(app.DisplayName);
        string cleanPublisher = CleanAppNameForMatching(app.Publisher);

        if (app.IsStoreApp)
        {
            if (app.DisplayName == "Mock Store Game")
            {
                string mockPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
                if (!Directory.Exists(mockPackagesPath))
                {
                    Directory.CreateDirectory(mockPackagesPath);
                }
                string fakePackageFolder = Path.Combine(mockPackagesPath, "MockStoreGame_8wekyb3d8bbwe");
                try
                {
                    if (!Directory.Exists(fakePackageFolder))
                    {
                        Directory.CreateDirectory(fakePackageFolder);
                        Directory.CreateDirectory(Path.Combine(fakePackageFolder, "LocalState"));
                        File.WriteAllText(Path.Combine(fakePackageFolder, "LocalState", "savegame.dat"), new string('s', 1024 * 1024 * 3));
                    }
                }
                catch {}
            }

            string packagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            if (Directory.Exists(packagesPath))
            {
                try
                {
                    var dirs = Directory.GetDirectories(packagesPath);
                    foreach (var dir in dirs)
                    {
                        string dirName = Path.GetFileName(dir);
                        string cleanNameNoSpace = cleanName.Replace(" ", "");
                        if (dirName.Contains(app.RegistryKeyName, StringComparison.OrdinalIgnoreCase) || 
                            (!string.IsNullOrEmpty(cleanNameNoSpace) && dirName.Contains(cleanNameNoSpace, StringComparison.OrdinalIgnoreCase)))
                        {
                            long size = GetDirectorySize(dir);
                            leftovers.Add(new LeftoverItem
                            {
                                Path = dir,
                                DisplayName = $"Package Data Folder: {dir}",
                                Type = LeftoverType.Directory,
                                SizeBytes = size
                            });
                        }
                    }
                }
                catch {}
            }
            
            ScanShortcutLeftovers(app, leftovers);
            ProgressChanged?.Invoke(100);
            return leftovers;
        }

        if (app.DisplayName == "Mock Trash App")
        {
            SetupMockAppEnvironment(app);
        }
        
        if (!string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation))
        {
            try
            {
                long size = GetDirectorySize(app.InstallLocation);
                leftovers.Add(new LeftoverItem
                {
                    Path = app.InstallLocation,
                    DisplayName = $"Install Directory: {app.InstallLocation}",
                    Type = LeftoverType.Directory,
                    SizeBytes = size
                });
            }
            catch {}
        }
        
        ProgressChanged?.Invoke(35);
        
        var commonDirs = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };
        
        foreach (var baseDir in commonDirs)
        {
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) continue;
            
            try
            {
                var subDirs = Directory.GetDirectories(baseDir);
                foreach (var dir in subDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    if (IsMatch(dirName, app.DisplayName, cleanName, app.Publisher, cleanPublisher))
                    {
                        if (IsSystemFolder(dirName)) continue;
                        if (leftovers.Any(x => x.Path.Equals(dir, StringComparison.OrdinalIgnoreCase))) continue;
                        
                        try
                        {
                            long size = GetDirectorySize(dir);
                            leftovers.Add(new LeftoverItem
                            {
                                Path = dir,
                                DisplayName = $"Leftover Directory: {dir}",
                                Type = LeftoverType.Directory,
                                SizeBytes = size
                            });
                        }
                        catch {}
                    }
                    
                    if (!string.IsNullOrEmpty(cleanPublisher) && dirName.Contains(cleanPublisher, StringComparison.OrdinalIgnoreCase) && !IsSystemFolder(dirName))
                    {
                        try
                        {
                            var pubSubDirs = Directory.GetDirectories(dir);
                            foreach (var subDir in pubSubDirs)
                            {
                                string subDirName = Path.GetFileName(subDir);
                                if (IsMatch(subDirName, app.DisplayName, cleanName, "", ""))
                                {
                                    if (leftovers.Any(x => x.Path.Equals(subDir, StringComparison.OrdinalIgnoreCase))) continue;
                                    
                                    try
                                    {
                                        long size = GetDirectorySize(subDir);
                                        leftovers.Add(new LeftoverItem
                                        {
                                            Path = subDir,
                                            DisplayName = $"Leftover Directory: {subDir}",
                                            Type = LeftoverType.Directory,
                                            SizeBytes = size
                                        });
                                    }
                                    catch {}
                                }
                            }
                        }
                        catch {}
                    }
                }
            }
            catch {}
        }
        
        ProgressChanged?.Invoke(65);
        
        var regPaths = new[]
        {
            (Registry.CurrentUser, @"SOFTWARE"),
            (Registry.LocalMachine, @"SOFTWARE"),
            (Registry.LocalMachine, @"SOFTWARE\Wow6432Node")
        };
        
        foreach (var (hive, path) in regPaths)
        {
            try
            {
                using var softwareKey = hive.OpenSubKey(path);
                if (softwareKey == null) continue;
                
                var subKeyNames = softwareKey.GetSubKeyNames();
                foreach (var keyName in subKeyNames)
                {
                    if (IsMatch(keyName, app.DisplayName, cleanName, app.Publisher, cleanPublisher))
                    {
                        if (IsSystemKey(keyName)) continue;
                        
                        leftovers.Add(new LeftoverItem
                        {
                            Path = $@"{hive.Name}\{path}\{keyName}",
                            DisplayName = $"Registry Key: {hive.Name}\\{path}\\{keyName}",
                            Type = LeftoverType.RegistryKey
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(cleanPublisher) && keyName.Contains(cleanPublisher, StringComparison.OrdinalIgnoreCase) && !IsSystemKey(keyName))
                    {
                        try
                        {
                            using var pubKey = softwareKey.OpenSubKey(keyName);
                            if (pubKey != null)
                            {
                                var appSubKeys = pubKey.GetSubKeyNames();
                                foreach (var appSubKey in appSubKeys)
                                {
                                    if (IsMatch(appSubKey, app.DisplayName, cleanName, "", ""))
                                    {
                                        leftovers.Add(new LeftoverItem
                                        {
                                            Path = $@"{hive.Name}\{path}\{keyName}\{appSubKey}",
                                            DisplayName = $"Registry Key: {hive.Name}\\{path}\\{keyName}\\{appSubKey}",
                                            Type = LeftoverType.RegistryKey
                                        });
                                    }
                                }
                            }
                        }
                        catch {}
                    }
                }
            }
            catch {}
        }
        
        if (!string.IsNullOrEmpty(app.RegistryPath))
        {
            var baseHive = app.Hive == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
            try
            {
                using var unKey = baseHive.OpenSubKey(app.RegistryPath);
                if (unKey != null)
                {
                    leftovers.Add(new LeftoverItem
                    {
                        Path = $@"{app.Hive}\{app.RegistryPath}",
                        DisplayName = $"Uninstall Registry Entry: {app.Hive}\\{app.RegistryPath}",
                        Type = LeftoverType.RegistryKey
                    });
                }
            }
            catch {}
        }
        
        ScanShortcutLeftovers(app, leftovers);
        ProgressChanged?.Invoke(100);
        return leftovers;
    }
    
    private void SetupMockAppEnvironment(InstalledAppInfo app)
    {
        try
        {
            if (!Directory.Exists(app.InstallLocation))
            {
                Directory.CreateDirectory(app.InstallLocation);
                File.WriteAllText(Path.Combine(app.InstallLocation, "app.dll"), new string('x', 1024 * 1024 * 5));
                File.WriteAllText(Path.Combine(app.InstallLocation, "trash_config.ini"), "[Config]\nKey=123\nTempJunk=True");
            }
            
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MockTrashApp");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
                File.WriteAllText(Path.Combine(appDataDir, "cache.db"), new string('y', 1024 * 1024 * 8));
            }
            
            string localAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MockTrashApp");
            if (!Directory.Exists(localAppDataDir))
            {
                Directory.CreateDirectory(localAppDataDir);
                File.WriteAllText(Path.Combine(localAppDataDir, "debug.log"), new string('z', 1024 * 512));
            }
            
            using var key1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\MockTrashApp");
            key1.SetValue("InstallPath", app.InstallLocation);
            
            using var key2 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\TrashySoft\MockTrashApp");
            key2.SetValue("Version", "4.2.1");
        }
        catch {}
    }
    
    public async Task<int> DeleteLeftoversAsync(List<LeftoverItem> items)
    {
        int deletedCount = 0;
        Log("Starting cleanup of selected residual files and registry entries...");
        ProgressChanged?.Invoke(10);
        
        await Task.Run(() =>
        {
            int total = items.Count;
            int current = 0;
            foreach (var item in items)
            {
                if (!item.IsSelected) continue;
                
                if (item.Type == LeftoverType.Directory)
                {
                    try
                    {
                        if (Directory.Exists(item.Path))
                        {
                            Log($"Deleting leftover directory: {item.Path}");
                            Directory.Delete(item.Path, true);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to delete directory {item.Path}: {ex.Message}");
                    }
                }
                else if (item.Type == LeftoverType.File)
                {
                    try
                    {
                        if (File.Exists(item.Path))
                        {
                            Log($"Deleting leftover file: {item.Path}");
                            File.Delete(item.Path);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to delete file {item.Path}: {ex.Message}");
                    }
                }
                else if (item.Type == LeftoverType.RegistryKey)
                {
                    try
                    {
                        int slashIndex = item.Path.IndexOf('\\');
                        if (slashIndex > 0)
                        {
                            string hiveStr = item.Path.Substring(0, slashIndex);
                            string relativePath = item.Path.Substring(slashIndex + 1);
                            
                            var hive = hiveStr == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                            
                            int lastSlash = relativePath.LastIndexOf('\\');
                            if (lastSlash > 0)
                            {
                                string parentPath = relativePath.Substring(0, lastSlash);
                                string keyToDelete = relativePath.Substring(lastSlash + 1);
                                
                                using var parentKey = hive.OpenSubKey(parentPath, true);
                                if (parentKey != null)
                                {
                                    Log($"Deleting leftover registry key: {item.Path}");
                                    parentKey.DeleteSubKeyTree(keyToDelete, false);
                                    deletedCount++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to delete registry key {item.Path}: {ex.Message}");
                    }
                }
                else if (item.Type == LeftoverType.RegistryValue)
                {
                    try
                    {
                        int slashIndex = item.Path.IndexOf('\\');
                        if (slashIndex > 0)
                        {
                            string hiveStr = item.Path.Substring(0, slashIndex);
                            string relativePath = item.Path.Substring(slashIndex + 1);
                            
                            var hive = hiveStr == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                            
                            int lastSlash = relativePath.LastIndexOf('\\');
                            if (lastSlash > 0)
                            {
                                string parentPath = relativePath.Substring(0, lastSlash);
                                string valueToDelete = relativePath.Substring(lastSlash + 1);
                                
                                using var parentKey = hive.OpenSubKey(parentPath, true);
                                if (parentKey != null)
                                {
                                    Log($"Deleting leftover registry value: {item.Path}");
                                    parentKey.DeleteValue(valueToDelete);
                                    deletedCount++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to delete registry value {item.Path}: {ex.Message}");
                    }
                }
                
                current++;
                if (total > 0)
                {
                    int percent = 10 + (int)((double)current / total * 80);
                    ProgressChanged?.Invoke(percent);
                }
            }
        });
        
        Log($"Residual cleanup complete. Successfully removed {deletedCount} leftovers.");
        ProgressChanged?.Invoke(100);
        return deletedCount;
    }

    private void ScanShortcutLeftovers(InstalledAppInfo app, List<LeftoverItem> leftovers)
    {
        string cleanName = CleanAppNameForMatching(app.DisplayName);
        if (string.IsNullOrEmpty(cleanName) || cleanName.Length < 3) return;

        var shortcutPaths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
        };

        foreach (var folder in shortcutPaths)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;

            try
            {
                var files = Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        
                        bool nameMatch = fileName.Contains(cleanName, StringComparison.OrdinalIgnoreCase) || 
                                         app.DisplayName.Contains(fileName, StringComparison.OrdinalIgnoreCase);

                        bool targetMatch = false;
                        if (!string.IsNullOrEmpty(app.InstallLocation))
                        {
                            byte[] lnkBytes = File.ReadAllBytes(file);
                            string lnkText = System.Text.Encoding.Unicode.GetString(lnkBytes) + 
                                             System.Text.Encoding.ASCII.GetString(lnkBytes);
                            if (lnkText.Contains(app.InstallLocation, StringComparison.OrdinalIgnoreCase))
                            {
                                targetMatch = true;
                            }
                        }

                        if (nameMatch || targetMatch)
                        {
                            if (leftovers.Any(x => x.Path.Equals(file, StringComparison.OrdinalIgnoreCase))) continue;

                            long size = 0;
                            try { size = new FileInfo(file).Length; } catch {}
                            
                            leftovers.Add(new LeftoverItem
                            {
                                Path = file,
                                DisplayName = $"Shortcut Link: {Path.GetFileName(file)}",
                                Type = LeftoverType.File,
                                SizeBytes = size
                            });
                        }
                    }
                    catch {}
                }
            }
            catch {}
        }
    }
    
    private bool IsSystemFolder(string name)
    {
        string[] sysFolders = { "Windows", "System32", "SysWOW64", "Microsoft", "Intel", "AMD", "Common Files", "Windows Defender", "WindowsApps", "Windows Mail", "Windows NT", "Windows Photo Viewer", "Windows Portable Devices", "Windows Sidebar", "WindowsPowerShell" };
        return sysFolders.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsSystemKey(string name)
    {
        string[] sysKeys = { "Microsoft", "Intel", "AMD", "Windows", "Windows NT", "Classes", "Clients", "Policies", "RegisteredApplications" };
        return sysKeys.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    private string CleanAppNameForMatching(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string cleaned = name.Replace("(R)", "").Replace("(TM)", "").Replace("™", "").Replace("®", "");
        cleaned = Regex.Replace(cleaned, @"\b(version|v|edition|build|x64|x86|64-bit|64bit|32-bit|32bit)\b.*", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\b\d+(\.\d+)*\b", "");
        cleaned = cleaned.Trim();
        return cleaned;
    }

    private bool IsMatch(string folderOrKeyName, string fullDisplayName, string cleanName, string fullPublisher, string cleanPublisher)
    {
        if (string.IsNullOrEmpty(folderOrKeyName)) return false;
        
        if (folderOrKeyName.Equals(fullDisplayName, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrEmpty(cleanName) && folderOrKeyName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)) return true;
        
        if (!string.IsNullOrEmpty(cleanName) && cleanName.Length >= 3)
        {
            if (folderOrKeyName.Contains(cleanName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            string noSpaceClean = cleanName.Replace(" ", "");
            string noSpaceFolder = folderOrKeyName.Replace(" ", "");
            if (noSpaceFolder.Contains(noSpaceClean, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }

    private long GetDirectorySize(string path)
    {
        long size = 0;
        try
        {
            var info = new DirectoryInfo(path);
            foreach (var file in info.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch {}
        return size;
    }

    private string ExtractIconFile(string displayIconValue, string appKeyName)
    {
        if (string.IsNullOrWhiteSpace(displayIconValue)) return "";
        
        try
        {
            string cleanPath = displayIconValue.Trim().Replace("\"", "");
            int commaIndex = cleanPath.LastIndexOf(',');
            if (commaIndex > 0)
            {
                string afterComma = cleanPath.Substring(commaIndex + 1).Trim();
                if (int.TryParse(afterComma, out _))
                {
                    cleanPath = cleanPath.Substring(0, commaIndex).Trim();
                }
            }
            
            if (string.IsNullOrEmpty(cleanPath) || !File.Exists(cleanPath))
            {
                return "";
            }
            
            string tempDir = Path.Combine(Path.GetTempPath(), "WinCareIcons");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            
            string safeKeyName = string.Concat(appKeyName.Split(Path.GetInvalidFileNameChars()));
            string destPng = Path.Combine(tempDir, $"{safeKeyName}.png");
            if (File.Exists(destPng))
            {
                return destPng;
            }
            
            var getFileTask = Windows.Storage.StorageFile.GetFileFromPathAsync(cleanPath).AsTask();
            getFileTask.Wait();
            var storageFile = getFileTask.Result;
            
            if (storageFile != null)
            {
                var getThumbTask = storageFile.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 
                    48, 
                    Windows.Storage.FileProperties.ThumbnailOptions.None).AsTask();
                getThumbTask.Wait();
                using var thumbnail = getThumbTask.Result;
                
                if (thumbnail != null)
                {
                    using (var fileStream = new FileStream(destPng, FileMode.Create, FileAccess.Write))
                    {
                        using (var readStream = thumbnail.AsStreamForRead())
                        {
                            readStream.CopyTo(fileStream);
                        }
                    }
                    return destPng;
                }
            }
        }
        catch
        {
            // Fail silently
        }
        
        return "";
    }

    public async Task<bool> UninstallStoreAppAsync(string packageFullName)
    {
        if (packageFullName.Contains("MockStoreGame"))
        {
            Log($"[Mock] Uninstalling Store App Mock Store Game...");
            await Task.Delay(2000);
            Log($"[Mock] Successfully uninstalled Microsoft Store package.");
            return true;
        }

        Log($"Removing Microsoft Store package: {packageFullName}");
        bool uwpSuccess = false;
        try
        {
            var packageManager = new Windows.Management.Deployment.PackageManager();
            var result = await packageManager.RemovePackageAsync(packageFullName);
            
            if (result.ExtendedErrorCode == null || result.ExtendedErrorCode.HResult == 0)
            {
                Log($"Successfully uninstalled Microsoft Store package.");
                uwpSuccess = true;
            }
            else
            {
                Log($"Deployment result error: {result.ErrorText}");
            }
        }
        catch (Exception ex)
        {
            Log($"PackageManager direct uninstall failed: {ex.Message}");
        }

        if (uwpSuccess) return true;

        Log("Attempting fallback uninstallation via PowerShell...");
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"Remove-AppxPackage -Package '{packageFullName}'\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    Log("Successfully uninstalled Microsoft Store package via PowerShell fallback.");
                    return true;
                }
                else
                {
                    Log($"PowerShell fallback exited with code: {process.ExitCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"PowerShell fallback failed: {ex.Message}");
        }

        return false;
    }
}

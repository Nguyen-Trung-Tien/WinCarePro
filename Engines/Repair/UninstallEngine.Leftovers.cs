using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class UninstallEngine
{
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
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow"),
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
    
    public bool IsSystemFolder(string name)
    {
        string[] sysFolders = { "Windows", "System32", "SysWOW64", "Microsoft", "Intel", "AMD", "Common Files", "Windows Defender", "WindowsApps", "Windows Mail", "Windows NT", "Windows Photo Viewer", "Windows Portable Devices", "Windows Sidebar", "WindowsPowerShell" };
        return sysFolders.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsSystemKey(string name)
    {
        string[] sysKeys = { "Microsoft", "Intel", "AMD", "Windows", "Windows NT", "Classes", "Clients", "Policies", "RegisteredApplications" };
        return sysKeys.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public string CleanAppNameForMatching(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string cleaned = name.Replace("(R)", "").Replace("(TM)", "").Replace("™", "").Replace("®", "");
        cleaned = Regex.Replace(cleaned, @"\s*(?:\b(?:version|edition|build|x64|x86|64-bit|64bit|32-bit|32bit)\b|\bv?\d+(\.\d+)*).*", "", RegexOptions.IgnoreCase);
        cleaned = cleaned.Trim();
        return cleaned;
    }

    public bool IsMatch(string folderOrKeyName, string fullDisplayName, string cleanName, string fullPublisher, string cleanPublisher)
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
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            var info = new DirectoryInfo(path);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) return 0;

            var files = info.GetFiles();
            foreach (var file in files)
            {
                try
                {
                    size += file.Length;
                }
                catch {}
            }

            var dirs = info.GetDirectories();
            foreach (var dir in dirs)
            {
                try
                {
                    size += GetDirectorySize(dir.FullName);
                }
                catch {}
            }
        }
        catch {}
        return size;
    }

    private async Task<string> ExtractIconFileAsync(string cleanPath, string destPng)
    {
        try
        {
            var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(cleanPath);
            if (storageFile != null)
            {
                using var thumbnail = await storageFile.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 
                    48, 
                    Windows.Storage.FileProperties.ThumbnailOptions.None);
                
                if (thumbnail != null)
                {
                    using (var fileStream = new FileStream(destPng, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        using (var readStream = thumbnail.AsStreamForRead())
                        {
                            await readStream.CopyToAsync(fileStream);
                        }
                    }
                    return destPng;
                }
            }
        }
        catch { }
        return "";
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
            
            // Extract asynchronously in background to prevent blocking
            _ = Task.Run(async () => await ExtractIconFileAsync(cleanPath, destPng));
        }
        catch { }
        
        return "";
    }
}

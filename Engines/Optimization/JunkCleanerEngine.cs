using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class JunkCleanerEngine
{
    public event Action<string>? ProgressMessage;
    public event Action<int>? ProgressChanged;

    private void Log(string msg) => ProgressMessage?.Invoke(msg);

    // PInvoke to empty Recycle Bin or query it
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);

    private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

    private static string FormatSize(long bytes) => WinCarePro.Core.Helpers.FormatHelper.FormatBytes(bytes);

    private static bool IsFileLocked(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
        try
        {
            var info = new FileInfo(filePath);
            var access = info.IsReadOnly ? FileAccess.Read : FileAccess.ReadWrite;
            using (var stream = new FileStream(filePath, FileMode.Open, access, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
        catch
        {
            return true;
        }
        return false;
    }

    private (long bytes, long cleanableBytes, long lockedBytes, int count, List<JunkFileItem> files) GetDirectoryDetails(
        string path, string searchPattern = "*", bool recursive = true, CancellationToken token = default, int maxFiles = 5000)
    {
        token.ThrowIfCancellationRequested();
        long bytes = 0;
        long cleanableBytes = 0;
        long lockedBytes = 0;
        int count = 0;
        var fileItems = new List<JunkFileItem>();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return (0, 0, 0, 0, fileItems);

        var dirsToVisit = new Queue<string>();
        dirsToVisit.Enqueue(path);

        while (dirsToVisit.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            string currentDir = dirsToVisit.Dequeue();

            try
            {
                var dirInfo = new DirectoryInfo(currentDir);
                if (currentDir != path && (dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;

                // Enumerate files in current directory
                try
                {
                    foreach (var file in Directory.EnumerateFiles(currentDir, searchPattern))
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            var info = new FileInfo(file);
                            long size = info.Length;
                            bytes += size;
                            cleanableBytes += size;
                            count++;

                            if (fileItems.Count < maxFiles)
                            {
                                fileItems.Add(new JunkFileItem { Path = file, SizeBytes = size, IsLocked = false });
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch { }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { }

                // Enumerate sub-directories if recursive
                if (recursive)
                {
                    try
                    {
                        foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                        {
                            token.ThrowIfCancellationRequested();
                            dirsToVisit.Enqueue(subDir);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        return (bytes, cleanableBytes, lockedBytes, count, fileItems);
    }

    private void UpdateCategoryLockStatus(JunkCategory cat, CancellationToken token = default)
    {
        long actualLockedBytes = 0;
        foreach (var f in cat.TopFiles)
        {
            token.ThrowIfCancellationRequested();
            if (IsFileLocked(f.Path))
            {
                f.IsLocked = true;
                actualLockedBytes += f.SizeBytes;
            }
        }

        if (actualLockedBytes > 0)
        {
            cat.LockedBytes = actualLockedBytes;
            cat.CleanableBytes = Math.Max(0, cat.SizeBytes - cat.LockedBytes);
        }
        else
        {
            cat.LockedBytes = 0;
            cat.CleanableBytes = cat.SizeBytes;
        }
    }

    private JunkCategory ScanPaths(IEnumerable<string> paths, string name, string description, JunkType type, string iconGlyph, string iconColor, string primaryFolder, string searchPattern = "*", bool recursive = true, CancellationToken token = default)
    {
        long bytes = 0;
        long cleanableBytes = 0;
        long lockedBytes = 0;
        int count = 0;
        var allFiles = new List<JunkFileItem>();

        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();
            if (Directory.Exists(path))
            {
                var (totalBytes, totalCleanable, totalLocked, totalCount, files) = GetDirectoryDetails(path, searchPattern, recursive, token);
                bytes += totalBytes;
                cleanableBytes += totalCleanable;
                lockedBytes += totalLocked;
                count += totalCount;
                allFiles.AddRange(files);
            }
            else if (File.Exists(path))
            {
                try
                {
                    var info = new FileInfo(path);
                    long size = info.Length;
                    bytes += size;
                    cleanableBytes += size;
                    count++;
                    allFiles.Add(new JunkFileItem { Path = path, SizeBytes = size, IsLocked = false });
                }
                catch { }
            }
        }

        var cat = new JunkCategory
        {
            Name = Services.TranslationManager.Instance.T(name),
            Description = Services.TranslationManager.Instance.T(description),
            Type = type,
            SizeBytes = bytes,
            CleanableBytes = cleanableBytes,
            LockedBytes = lockedBytes,
            FileCount = count,
            IsSelected = true,
            IconGlyph = iconGlyph,
            IconColor = iconColor,
            FolderPath = primaryFolder,
            TopFiles = allFiles.OrderByDescending(f => f.SizeBytes).Take(50).ToList()
        };

        UpdateCategoryLockStatus(cat, token);
        return cat;
    }

    public async Task<List<JunkCategory>> ScanJunkAsync(CancellationToken token = default)
    {
        return await Task.Run(() =>
        {
            Log("Starting Junk scan...");
            ProgressChanged?.Invoke(5);
            var categories = new List<JunkCategory>();

            // 1. User Temp Files
            Log("Scanning User Temp files...");
            token.ThrowIfCancellationRequested();
            string userTemp = Path.GetTempPath();
            var userTempCat = ScanPaths(new[] { userTemp }, "User Temp Files", "Local application cache files and user account temp data.", JunkType.UserTemp, "\uE71B", "#FFF59E0B", userTemp, token: token);
            categories.Add(userTempCat);
            ProgressChanged?.Invoke(15);

            // 2. Windows Temp Files
            Log("Scanning Windows Temp directory...");
            token.ThrowIfCancellationRequested();
            string winTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp");
            var winTempCat = ScanPaths(new[] { winTemp }, "Windows Temp Files", "Temporary files generated by the Windows OS operating system.", JunkType.WindowsTemp, "\uE7F4", "#FFF97316", winTemp, token: token);
            categories.Add(winTempCat);
            ProgressChanged?.Invoke(25);

            // 3. Update Installer Cache
            Log("Scanning Windows Update Cache...");
            token.ThrowIfCancellationRequested();
            string winUpdate = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\Download");
            var winUpdateCat = ScanPaths(new[] { winUpdate }, "Update Installer Cache", "Leftover software update components and cached windows setup files.", JunkType.UpdateCache, "\uE777", "#FF10B981", winUpdate, token: token);
            categories.Add(winUpdateCat);
            ProgressChanged?.Invoke(35);

            // 4. Recycle Bin
            Log("Querying Recycle Bin status...");
            token.ThrowIfCancellationRequested();
            var rbCat = GetRecycleBinCategory();
            categories.Add(rbCat);
            ProgressChanged?.Invoke(45);

            // 5. Browser Cache (Chrome, Edge, Brave, Opera, Firefox)
            Log("Scanning Browser caches...");
            token.ThrowIfCancellationRequested();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var browserPaths = new List<string>
            {
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache\Cache_Data")
            };

            // Firefox profile discovery
            string firefoxProfiles = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
            if (Directory.Exists(firefoxProfiles))
            {
                try
                {
                    foreach (var profileDir in Directory.GetDirectories(firefoxProfiles))
                    {
                        string cache2 = Path.Combine(profileDir, "cache2");
                        if (Directory.Exists(cache2))
                        {
                            browserPaths.Add(cache2);
                        }
                    }
                }
                catch { }
            }

            string primaryBrowserFolder = browserPaths.FirstOrDefault(Directory.Exists) ?? localAppData;
            var browserCat = ScanPaths(browserPaths, "Browser Cache", "Cached webpages, images, and offline resources from Microsoft Edge.", JunkType.BrowserCache, "\uE774", "#FF3B82F6", primaryBrowserFolder, token: token);
            categories.Add(browserCat);
            ProgressChanged?.Invoke(60);

            // 6. DirectX Shader Cache
            Log("Scanning DirectX shader caches...");
            token.ThrowIfCancellationRequested();
            string shaderCachePath = Path.Combine(localAppData, @"D3DSCache");
            var shaderCat = ScanPaths(new[] { shaderCachePath }, "DirectX Shader Cache", "Graphics driver compiled shaders cache for speeding up UI renders.", JunkType.ShaderCache, "\uE7F6", "#FF8B5CF6", shaderCachePath, token: token);
            categories.Add(shaderCat);
            ProgressChanged?.Invoke(70);

            // 7. System Log Files
            Log("Scanning System Logs and WER reports...");
            token.ThrowIfCancellationRequested();
            var logPaths = new List<string>
            {
                Path.Combine(localAppData, @"Microsoft\Windows\WER"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles"),
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Logs")
            };
            var logCat = ScanPaths(logPaths, "System Log Files", "Diagnostic logging traces and Windows event log reports.", JunkType.SystemLog, "\uEA37", "#FF64748B", logPaths[2], token: token);
            categories.Add(logCat);
            ProgressChanged?.Invoke(80);

            // 8. Thumbnail Cache
            Log("Scanning Explorer thumbnail database caches...");
            token.ThrowIfCancellationRequested();
            string explorerFolder = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
            var thumbPaths = new[] { explorerFolder };
            var thumbCat = ScanPaths(thumbPaths, "Thumbnail Cache", "Cached preview image files of system explorer folders.", JunkType.ThumbnailCache, "\uEB9F", "#FFEC4899", explorerFolder, "thumbcache_*.db", false, token);
            if (Directory.Exists(explorerFolder))
            {
                try
                {
                    var (iconBytes, iconCleanable, iconLocked, iconCount, iconFiles) = GetDirectoryDetails(explorerFolder, "iconcache_*.db", false, token);
                    thumbCat.SizeBytes += iconBytes;
                    thumbCat.CleanableBytes += iconCleanable;
                    thumbCat.LockedBytes += iconLocked;
                    thumbCat.FileCount += iconCount;
                    thumbCat.TopFiles.AddRange(iconFiles);
                    thumbCat.TopFiles = thumbCat.TopFiles.OrderByDescending(f => f.SizeBytes).Take(50).ToList();
                    UpdateCategoryLockStatus(thumbCat, token);
                }
                catch { }
            }
            categories.Add(thumbCat);
            ProgressChanged?.Invoke(85);

            // 9. Delivery Optimization Files
            Log("Scanning Delivery Optimization cache...");
            token.ThrowIfCancellationRequested();
            string doPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\DeliveryOptimization");
            var doCat = ScanPaths(new[] { doPath }, "Delivery Optimization Files", "Cache files used for downloading and sharing Windows updates.", JunkType.DeliveryOptimization, "\uE72C", "#FF06B6D4", doPath, token: token);
            categories.Add(doCat);
            ProgressChanged?.Invoke(90);

            // 10. System Prefetch Files
            Log("Scanning system prefetch cache...");
            token.ThrowIfCancellationRequested();
            string prefetchPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Prefetch");
            var prefetchCat = ScanPaths(new[] { prefetchPath }, "System Prefetch Files", "System prefetch cache files created to speed up application startup.", JunkType.Prefetch, "\uE8A3", "#FFEAB308", prefetchPath, token: token);
            categories.Add(prefetchCat);
            ProgressChanged?.Invoke(95);

            // 11. System Crash Dumps
            Log("Scanning system crash memory dumps...");
            token.ThrowIfCancellationRequested();
            string minidumpPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Minidump");
            string memoryDmp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "MEMORY.DMP");
            var crashCat = ScanPaths(new[] { minidumpPath, memoryDmp }, "System Crash Dumps", "Memory dump files and log traces created when system crash or error occurs.", JunkType.CrashDumps, "\uE7BA", "#FFDC2626", minidumpPath, token: token);
            categories.Add(crashCat);

            // 12. Developer & IDE Caches (VS Code, npm, pip, NuGet)
            Log("Scanning Developer & IDE caches...");
            token.ThrowIfCancellationRequested();
            string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var devPaths = new List<string>
            {
                Path.Combine(appDataRoaming, @"Code\Cache"),
                Path.Combine(appDataRoaming, @"Code\CachedData"),
                Path.Combine(appDataRoaming, @"Code\CachedExtensionVSIXs"),
                Path.Combine(localAppData, @"npm-cache"),
                Path.Combine(userProfile, @".npm\_cacache"),
                Path.Combine(localAppData, @"pip\cache"),
                Path.Combine(localAppData, @"NuGet\v3-cache")
            };
            string primaryDevFolder = devPaths.FirstOrDefault(Directory.Exists) ?? localAppData;
            var devCat = ScanPaths(devPaths, "Developer & IDE Caches", "Cached build artifacts, package manager caches (npm, pip, NuGet, VS Code).", JunkType.DeveloperCache, "\uE943", "#FF8B5CF6", primaryDevFolder, token: token);
            categories.Add(devCat);

            ProgressChanged?.Invoke(100);
            Log("Junk scan completed.");
            return categories;
        });
    }

    public async Task<long> CleanJunkAsync(List<JunkCategory> categories)
    {
        return await Task.Run(async () =>
        {
            Log("Starting Junk cleaning process...");
            long totalCleanedBytes = 0;
            ProgressChanged?.Invoke(5);

            double increment = 95.0 / categories.Count;
            double currentProgress = 5.0;

            foreach (var cat in categories)
            {
                if (!cat.IsSelected) continue;

                Log($"Cleaning category: {cat.Name}...");
                long cleaned = 0;

                switch (cat.Type)
                {
                    case JunkType.UserTemp:
                        cleaned = await ClearDirectoryAsync(Path.GetTempPath());
                        break;
                    case JunkType.WindowsTemp:
                        string winTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp");
                        cleaned = await ClearDirectoryAsync(winTemp);
                        break;
                    case JunkType.UpdateCache:
                        string winUpdate = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\Download");
                        cleaned = await ClearDirectoryAsync(winUpdate);
                        break;
                    case JunkType.RecycleBin:
                        cleaned = cat.SizeBytes;
                        ClearRecycleBin();
                        Log("Recycle Bin emptied successfully.");
                        break;
                    case JunkType.BrowserCache:
                        cleaned = await ClearBrowserCachesAsync();
                        break;
                    case JunkType.ShaderCache:
                        string shaderCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"D3DSCache");
                        cleaned = await ClearDirectoryAsync(shaderCachePath);
                        break;
                    case JunkType.SystemLog:
                        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cleaned += await ClearDirectoryAsync(Path.Combine(localApp, @"Microsoft\Windows\WER"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Logs"));
                        break;
                    case JunkType.ThumbnailCache:
                        string explorerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
                        cleaned += await ClearFilesMatchingAsync(explorerFolder, "thumbcache_*.db");
                        cleaned += await ClearFilesMatchingAsync(explorerFolder, "iconcache_*.db");
                        break;
                    case JunkType.DeliveryOptimization:
                        string doPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\DeliveryOptimization");
                        cleaned = await ClearDirectoryAsync(doPath);
                        break;
                    case JunkType.Prefetch:
                        string prefetchPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Prefetch");
                        cleaned = await ClearDirectoryAsync(prefetchPath);
                        break;
                    case JunkType.CrashDumps:
                        string minidumpPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Minidump");
                        cleaned += await ClearDirectoryAsync(minidumpPath);
                        string memoryDmp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "MEMORY.DMP");
                        if (File.Exists(memoryDmp))
                        {
                            try
                            {
                                var info = new FileInfo(memoryDmp);
                                long size = info.Length;
                                File.Delete(memoryDmp);
                                cleaned += size;
                                Log($"Deleted file: MEMORY.DMP ({FormatSize(size)})");
                            }
                            catch
                            {
                                Log("Skipped locked file: MEMORY.DMP");
                            }
                        }
                        break;
                    case JunkType.DeveloperCache:
                        string appDataR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string locApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\Cache"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\CachedData"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\CachedExtensionVSIXs"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"npm-cache"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(userProf, @".npm\_cacache"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"pip\cache"));
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"NuGet\v3-cache"));
                        break;
                }

                totalCleanedBytes += cleaned;
                Log($"Cleaned {cat.Name}: Reclaimed {FormatSize(cleaned)}");
                currentProgress += increment;
                ProgressChanged?.Invoke((int)currentProgress);
            }

            ProgressChanged?.Invoke(100);
            Log($"Junk cleaning complete. Cleaned: {FormatSize(totalCleanedBytes)}");
            Database.DbManager.LogAction($"Cleaned {totalCleanedBytes} bytes", "Junk Cleaner", "Success");
            return totalCleanedBytes;
        });
    }

    public static bool IsPathSafeToClean(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return WinCarePro.Core.Helpers.SafePathGuard.IsPathSafeForDeletion(path);
    }

    private long ClearDirectoryRecursively(string path)
    {
        if (!IsPathSafeToClean(path) || !Directory.Exists(path)) return 0;
        long bytesDeleted = 0;

        // Delete files in current directory
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    long size = info.Length;
                    if (info.IsReadOnly)
                    {
                        info.IsReadOnly = false;
                    }
                    File.Delete(file);
                    bytesDeleted += size;
                }
                catch
                {
                    try
                    {
                        MoveFileEx(file, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                    }
                    catch { }
                }
            }
        }
        catch { }

        // Recurse into subdirectories
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        Directory.Delete(dir, false);
                        Log($"Deleted directory link: {Path.GetFileName(dir)}");
                        continue;
                    }
                }
                catch { }

                bytesDeleted += ClearDirectoryRecursively(dir);
                try
                {
                    Directory.Delete(dir, false); // only delete if empty
                }
                catch { }
            }
        }
        catch { }

        return bytesDeleted;
    }

    private async Task<long> ClearDirectoryAsync(string path)
    {
        if (!IsPathSafeToClean(path) || !Directory.Exists(path)) return 0;
        return await Task.Run(() => ClearDirectoryRecursively(path));
    }

    private async Task<long> ClearFilesMatchingAsync(string path, string searchPattern)
    {
        if (!IsPathSafeToClean(path) || !Directory.Exists(path)) return 0;
        long bytesDeleted = 0;
        await Task.Run(() =>
        {
            try
            {
                foreach (var file in Directory.GetFiles(path, searchPattern))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        if (info.IsReadOnly)
                        {
                            info.IsReadOnly = false;
                        }
                        File.Delete(file);
                        bytesDeleted += size;
                    }
                    catch
                    {
                        try
                        {
                            MoveFileEx(file, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        });
        return bytesDeleted;
    }

    private async Task<long> ClearBrowserCachesAsync()
    {
        long cleaned = 0;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var cachePaths = new List<string>
        {
            Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"),
            Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"),
            Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache\Cache_Data"),
            Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache\Cache_Data")
        };

        foreach (var cachePath in cachePaths)
        {
            cleaned += await ClearDirectoryAsync(cachePath);
        }

        string firefoxProfiles = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
        if (Directory.Exists(firefoxProfiles))
        {
            try
            {
                foreach (var profileDir in Directory.GetDirectories(firefoxProfiles))
                {
                    string cache2 = Path.Combine(profileDir, "cache2");
                    cleaned += await ClearDirectoryAsync(cache2);
                }
            }
            catch { }
        }

        return cleaned;
    }

    private JunkCategory GetRecycleBinCategory()
    {
        long size = 0;
        long count = 0;
        try
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
            int hr = SHQueryRecycleBin(null, ref info);
            if (hr == 0)
            {
                size = Math.Max(0, info.i64Size);
                count = Math.Max(0, info.i64NumItems);
            }
        }
        catch { }

        return new JunkCategory
        {
            Name = Services.TranslationManager.Instance.T("Recycle Bin"),
            Description = Services.TranslationManager.Instance.T("Deleted files stored in your Recycle Bin."),
            Type = JunkType.RecycleBin,
            SizeBytes = size,
            CleanableBytes = size,
            LockedBytes = 0,
            FileCount = (int)Math.Min(int.MaxValue, count),
            IsSelected = true,
            IconGlyph = "\uEB7E",
            IconColor = "#FFEF4444",
            FolderPath = "shell:RecycleBinFolder"
        };
    }

    private void ClearRecycleBin()
    {
        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
        }
        catch { }
    }
}

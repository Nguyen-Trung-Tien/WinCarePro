using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class JunkCleanerEngine
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

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

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

    private JunkCategory ScanPaths(IEnumerable<string> paths, string name, string description, JunkType type, string iconGlyph, string iconColor, string primaryFolder, string searchPattern = "*", bool recursive = true, CancellationToken token = default, bool isSelected = true)
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
            IsSelected = isSelected,
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
        token.ThrowIfCancellationRequested();
        return await Task.Run(async () =>
        {
            token.ThrowIfCancellationRequested();
            Log("Starting Parallel Junk scan...");
            ProgressChanged?.Invoke(5);

            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            int completedCategories = 0;
            const int totalCategories = 12;

            void ReportCategoryDone(string catName)
            {
                int done = System.Threading.Interlocked.Increment(ref completedCategories);
                int pct = Math.Min(98, 5 + (done * 90 / totalCategories));
                ProgressChanged?.Invoke(pct);
                Log($"Scanned {catName} ({done}/{totalCategories})");
            }

            // Define parallel scan tasks for all 12 categories
            var task1 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string userTemp = Path.GetTempPath();
                var cat = ScanPaths(new[] { userTemp }, "User Temp Files", "Local application cache files and user account temp data.", JunkType.UserTemp, "\uE71B", "#FFF59E0B", userTemp, token: token);
                ReportCategoryDone("User Temp");
                return cat;
            }, token);

            var task2 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string winTemp = Path.Combine(systemRoot, "Temp");
                var cat = ScanPaths(new[] { winTemp }, "Windows Temp Files", "Temporary files generated by the Windows OS operating system.", JunkType.WindowsTemp, "\uE7F4", "#FFF97316", winTemp, token: token);
                ReportCategoryDone("Windows Temp");
                return cat;
            }, token);

            var task3 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string winUpdate = Path.Combine(systemRoot, "SoftwareDistribution\\Download");
                var cat = ScanPaths(new[] { winUpdate }, "Update Installer Cache", "Leftover software update components and cached windows setup files.", JunkType.UpdateCache, "\uE777", "#FF10B981", winUpdate, token: token);
                ReportCategoryDone("Update Cache");
                return cat;
            }, token);

            var task4 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var cat = GetRecycleBinCategory();
                ReportCategoryDone("Recycle Bin");
                return cat;
            }, token);

            var task5 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var browserPaths = new List<string>
                {
                    Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"),
                    Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"),
                    Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache\Cache_Data"),
                    Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache\Cache_Data")
                };

                string firefoxProfiles = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(firefoxProfiles))
                {
                    try
                    {
                        foreach (var profileDir in Directory.GetDirectories(firefoxProfiles))
                        {
                            string cache2 = Path.Combine(profileDir, "cache2");
                            if (Directory.Exists(cache2)) browserPaths.Add(cache2);
                        }
                    }
                    catch { }
                }

                string primaryBrowserFolder = browserPaths.FirstOrDefault(Directory.Exists) ?? localAppData;
                var cat = ScanPaths(browserPaths, "Browser Cache", "Cached webpages, images, and offline resources from Microsoft Edge.", JunkType.BrowserCache, "\uE774", "#FF3B82F6", primaryBrowserFolder, token: token);
                ReportCategoryDone("Browser Cache");
                return cat;
            }, token);

            var task6 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string shaderCachePath = Path.Combine(localAppData, @"D3DSCache");
                var cat = ScanPaths(new[] { shaderCachePath }, "DirectX Shader Cache", "Graphics driver compiled shaders cache. Cleaning may cause temporary frame stutter while shaders recompile.", JunkType.ShaderCache, "\uE7F6", "#FF8B5CF6", shaderCachePath, token: token, isSelected: false);
                ReportCategoryDone("Shader Cache");
                return cat;
            }, token);

            var task7 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var logPaths = new List<string>
                {
                    Path.Combine(localAppData, @"Microsoft\Windows\WER"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles"),
                    Path.Combine(systemRoot, "Logs")
                };
                var cat = ScanPaths(logPaths, "System Log Files", "Diagnostic logging traces and Windows event log reports.", JunkType.SystemLog, "\uEA37", "#FF64748B", logPaths[2], token: token);
                ReportCategoryDone("System Logs");
                return cat;
            }, token);

            var task8 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string explorerFolder = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                var thumbCat = ScanPaths(new[] { explorerFolder }, "Thumbnail Cache", "Cached preview image files of system explorer folders.", JunkType.ThumbnailCache, "\uEB9F", "#FFEC4899", explorerFolder, "thumbcache_*.db", false, token);
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
                ReportCategoryDone("Thumbnails");
                return thumbCat;
            }, token);

            var task9 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string doPath = Path.Combine(systemRoot, "SoftwareDistribution\\DeliveryOptimization");
                var cat = ScanPaths(new[] { doPath }, "Delivery Optimization Files", "Cache files used for downloading and sharing Windows updates.", JunkType.DeliveryOptimization, "\uE72C", "#FF06B6D4", doPath, token: token);
                ReportCategoryDone("Delivery Optimization");
                return cat;
            }, token);

            var task10 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string prefetchPath = Path.Combine(systemRoot, "Prefetch");
                var cat = ScanPaths(new[] { prefetchPath }, "System Prefetch Files", "System prefetch cache files created to speed up application startup. Cleaning forces cache rebuild.", JunkType.Prefetch, "\uE8A3", "#FFEAB308", prefetchPath, token: token, isSelected: false);
                ReportCategoryDone("Prefetch");
                return cat;
            }, token);

            var task11 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string minidumpPath = Path.Combine(systemRoot, "Minidump");
                string memoryDmp = Path.Combine(systemRoot, "MEMORY.DMP");
                var cat = ScanPaths(new[] { minidumpPath, memoryDmp }, "System Crash Dumps", "Memory dump files and log traces created when system crash or error occurs.", JunkType.CrashDumps, "\uE7BA", "#FFDC2626", minidumpPath, token: token);
                ReportCategoryDone("Crash Dumps");
                return cat;
            }, token);

            var task12 = Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
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
                var cat = ScanPaths(devPaths, "Developer & IDE Caches", "Cached build artifacts, package manager caches (npm, pip, NuGet, VS Code).", JunkType.DeveloperCache, "\uE943", "#FF8B5CF6", primaryDevFolder, token: token);
                ReportCategoryDone("Developer Caches");
                return cat;
            }, token);

            try
            {
                // Await all parallel scan tasks
                var results = await Task.WhenAll(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);

                ProgressChanged?.Invoke(100);
                Log("Parallel Junk scan completed.");
                return results.ToList();
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException(token);
            }
        }, token);
    }

}

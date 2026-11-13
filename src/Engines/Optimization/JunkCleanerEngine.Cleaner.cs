using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class JunkCleanerEngine
{
    public async Task<long> CleanJunkAsync(List<JunkCategory> categories, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await Task.Run(async () =>
        {
            Log("Starting Junk cleaning process...");
            long totalCleanedBytes = 0;
            ProgressChanged?.Invoke(5);

            double increment = 95.0 / Math.Max(1, categories.Count);
            double currentProgress = 5.0;

            foreach (var cat in categories)
            {
                token.ThrowIfCancellationRequested();
                if (!cat.IsSelected) continue;

                ProgressMessage?.Invoke($"Sweeping {cat.Name}...");
                Log($"Cleaning category: {cat.Name}...");
                long cleaned = 0;

                switch (cat.Type)
                {
                    case JunkType.UserTemp:
                        cleaned = await ClearDirectoryAsync(Path.GetTempPath(), token);
                        break;
                    case JunkType.WindowsTemp:
                        string winTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp");
                        cleaned = await ClearDirectoryAsync(winTemp, token);
                        break;
                    case JunkType.UpdateCache:
                        string winUpdate = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\Download");
                        cleaned = await ClearDirectoryAsync(winUpdate, token);
                        break;
                    case JunkType.RecycleBin:
                        cleaned = cat.SizeBytes;
                        ClearRecycleBin();
                        Log("Recycle Bin emptied successfully.");
                        break;
                    case JunkType.BrowserCache:
                        cleaned = await ClearBrowserCachesAsync(token);
                        break;
                    case JunkType.ShaderCache:
                        string shaderCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"D3DSCache");
                        cleaned = await ClearDirectoryAsync(shaderCachePath, token);
                        break;
                    case JunkType.SystemLog:
                        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cleaned += await ClearDirectoryAsync(Path.Combine(localApp, @"Microsoft\Windows\WER"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Logs"), token);
                        break;
                    case JunkType.ThumbnailCache:
                        string explorerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
                        cleaned += await ClearFilesMatchingAsync(explorerFolder, "thumbcache_*.db", token);
                        cleaned += await ClearFilesMatchingAsync(explorerFolder, "iconcache_*.db", token);
                        break;
                    case JunkType.DeliveryOptimization:
                        string doPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\DeliveryOptimization");
                        cleaned = await ClearDirectoryAsync(doPath, token);
                        break;
                    case JunkType.Prefetch:
                        string prefetchPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Prefetch");
                        cleaned = await ClearDirectoryAsync(prefetchPath, token);
                        break;
                    case JunkType.CrashDumps:
                        string minidumpPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Minidump");
                        cleaned += await ClearDirectoryAsync(minidumpPath, token);
                        string memoryDmp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "MEMORY.DMP");
                        if (File.Exists(memoryDmp) && IsPathSafeToClean(memoryDmp))
                        {
                            try
                            {
                                var info = new FileInfo(memoryDmp);
                                long size = info.Length;
                                File.Delete(memoryDmp);
                                cleaned += size;
                                Log($"Deleted file: MEMORY.DMP ({FormatSize(size)})");
                            }
                            catch (Exception)
                            {
                                Log("Skipped locked file: MEMORY.DMP");
                            }
                        }
                        break;
                    case JunkType.DeveloperCache:
                        string appDataR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string locApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\Cache"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\CachedData"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(appDataR, @"Code\CachedExtensionVSIXs"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"npm-cache"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(userProf, @".npm\_cacache"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"pip\cache"), token);
                        cleaned += await ClearDirectoryAsync(Path.Combine(locApp, @"NuGet\v3-cache"), token);
                        break;
                }

                totalCleanedBytes += cleaned;
                Log($"Cleaned {cat.Name}: Reclaimed {FormatSize(cleaned)}");
                currentProgress += increment;
                ProgressChanged?.Invoke(Math.Min(100, (int)currentProgress));

                await Task.Delay(50, token);
            }

            ProgressChanged?.Invoke(100);
            Log($"Junk cleaning complete. Cleaned: {FormatSize(totalCleanedBytes)}");
            Database.DbManager.LogAction($"Cleaned {totalCleanedBytes} bytes", "Junk Cleaner", "Success");
            return totalCleanedBytes;
        }, token);
    }

    public static bool IsPathSafeToClean(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return WinCarePro.Core.Helpers.SafePathGuard.IsPathSafeForDeletion(path);
    }

    public static bool IsDirectorySafeToClean(string? dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath)) return false;
        return WinCarePro.Core.Helpers.SafePathGuard.IsSafeToCleanDirectory(dirPath);
    }

    private long ClearDirectoryRecursively(string path, CancellationToken token = default)
    {
        if (!IsDirectorySafeToClean(path) || !Directory.Exists(path)) return 0;
        long bytesDeleted = 0;

        token.ThrowIfCancellationRequested();

        // 1. Delete files in current directory
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (IsPathSafeToClean(file))
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
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    // Locked or permission restricted — safely skipped without modifying reboot queues
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        // 2. Recurse into subdirectories (skipping junctions and symlinks)
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        // Skip junction points completely to avoid traversing into external targets
                        continue;
                    }
                }
                catch { }

                bytesDeleted += ClearDirectoryRecursively(dir, token);
                try
                {
                    if (IsPathSafeToClean(dir))
                    {
                        Directory.Delete(dir, false); // only delete if directory is now empty and safe
                    }
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        return bytesDeleted;
    }

    private async Task<long> ClearDirectoryAsync(string path, CancellationToken token = default)
    {
        if (!IsDirectorySafeToClean(path) || !Directory.Exists(path)) return 0;
        token.ThrowIfCancellationRequested();
        return await Task.Run(() => ClearDirectoryRecursively(path, token), token);
    }

    private async Task<long> ClearFilesMatchingAsync(string path, string searchPattern, CancellationToken token = default)
    {
        if (!IsDirectorySafeToClean(path) || !Directory.Exists(path)) return 0;
        token.ThrowIfCancellationRequested();
        long bytesDeleted = 0;
        await Task.Run(() =>
        {
            try
            {
                foreach (var file in Directory.GetFiles(path, searchPattern))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if (IsPathSafeToClean(file))
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
                    }
                    catch (OperationCanceledException) { throw; }
                    catch
                    {
                        // In-use files safely skipped
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }, token);
        return bytesDeleted;
    }

    private async Task<long> ClearBrowserCachesAsync(CancellationToken token = default)
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
            token.ThrowIfCancellationRequested();
            cleaned += await ClearDirectoryAsync(cachePath, token);
        }

        string firefoxProfiles = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
        if (Directory.Exists(firefoxProfiles))
        {
            try
            {
                foreach (var profileDir in Directory.GetDirectories(firefoxProfiles))
                {
                    token.ThrowIfCancellationRequested();
                    string cache2 = Path.Combine(profileDir, "cache2");
                    cleaned += await ClearDirectoryAsync(cache2, token);
                }
            }
            catch (OperationCanceledException) { throw; }
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

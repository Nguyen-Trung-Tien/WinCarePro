using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public partial class IconCacheService
{
    private const int MaxMemoryCacheSize = 250;
    private readonly string _cacheDirectory;
    private static readonly object _lruLock = new();
    private static readonly Dictionary<string, (string IconPath, long LastAccessTicks)> _lruCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task<string>> _activeIconTasks = new();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    public IconCacheService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinCarePro",
            "IconCache"
        );

        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
            PruneDiskCacheIfNeeded();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("IconCacheService.Init", ex);
        }
    }

    private void PruneDiskCacheIfNeeded()
    {
        Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory)) return;
                var dir = new DirectoryInfo(_cacheDirectory);
                var files = dir.GetFiles("*.png").OrderBy(f => f.LastAccessTimeUtc).ToList();
                long totalSize = files.Sum(f => f.Length);
                const long maxSizeBytes = 25 * 1024 * 1024; // 25 MB max disk cache

                if (totalSize > maxSizeBytes || files.Count > 350)
                {
                    int filesToDelete = Math.Max(files.Count - 200, 0);
                    for (int i = 0; i < filesToDelete && i < files.Count; i++)
                    {
                        try
                        {
                            if (Core.Helpers.SafePathGuard.IsSafeToDelete(files[i].FullName))
                            {
                                files[i].Delete();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException("IconCacheService.PruneDiskCache", ex);
            }
        });
    }

    private static void CacheIcon(string filePath, string iconPath)
    {
        lock (_lruLock)
        {
            if (_lruCache.Count >= MaxMemoryCacheSize && !_lruCache.ContainsKey(filePath))
            {
                var oldestKeys = _lruCache.OrderBy(kv => kv.Value.LastAccessTicks)
                                          .Take(MaxMemoryCacheSize / 5)
                                          .Select(kv => kv.Key)
                                          .ToList();
                foreach (var k in oldestKeys)
                {
                    _lruCache.Remove(k);
                }
            }
            _lruCache[filePath] = (iconPath, Environment.TickCount64);
        }
    }

    private static bool TryGetCachedIcon(string filePath, out string iconPath)
    {
        lock (_lruLock)
        {
            if (_lruCache.TryGetValue(filePath, out var item))
            {
                _lruCache[filePath] = (item.IconPath, Environment.TickCount64);
                iconPath = item.IconPath;
                return true;
            }
        }
        iconPath = "";
        return false;
    }

    public async Task<string> GetIconForExecutableAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath == "System Process" || !File.Exists(filePath))
        {
            return "";
        }

        if (TryGetCachedIcon(filePath, out var cachedPath) && !string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
                CacheIcon(filePath, destPng);
                return destPng;
            }

            var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            if (storageFile != null)
            {
                using var thumbnail = await storageFile.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem,
                    32,
                    Windows.Storage.FileProperties.ThumbnailOptions.None
                );

                if (thumbnail != null)
                {
                    using (var fileStream = new FileStream(destPng, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        using (var readStream = thumbnail.AsStreamForRead())
                        {
                            await readStream.CopyToAsync(fileStream);
                        }
                    }
                    CacheIcon(filePath, destPng);
                    return destPng;
                }
            }
        }
        catch
        {
            // Fail silently, return empty string
        }

        return "";
    }

    public string GetIconForExecutable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath == "System Process" || !File.Exists(filePath))
        {
            return "";
        }

        // Fast RAM Cache lookup (0 disk I/O)
        if (TryGetCachedIcon(filePath, out var cachedPath) && !string.IsNullOrEmpty(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
                CacheIcon(filePath, destPng);
                return destPng;
            }

            // Extract asynchronously in background to prevent blocking, avoiding duplicate task writes
            _activeIconTasks.GetOrAdd(destPng, key => Task.Run(async () =>
            {
                try
                {
                    var result = await GetIconForExecutableAsync(filePath);
                    if (!string.IsNullOrEmpty(result))
                    {
                        CacheIcon(filePath, result);
                    }
                    return result;
                }
                finally
                {
                    _activeIconTasks.TryRemove(key, out _);
                }
            }));
        }
        catch
        {
            // Fail silently
        }

        return "";
    }

    private static string GetMd5Hash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

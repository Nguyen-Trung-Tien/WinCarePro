using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public class IconCacheService
{
    private readonly string _cacheDirectory;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task<string>> _activeIconTasks = new();

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
        }
        catch { }
    }

    public async Task<string> GetIconForExecutableAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath == "System Process" || !File.Exists(filePath))
        {
            return "";
        }

        if (_memoryCache.TryGetValue(filePath, out var cachedPath) && !string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
                _memoryCache[filePath] = destPng;
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
                    _memoryCache[filePath] = destPng;
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
        if (_memoryCache.TryGetValue(filePath, out var cachedPath) && !string.IsNullOrEmpty(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
                _memoryCache[filePath] = destPng;
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
                        _memoryCache[filePath] = result;
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

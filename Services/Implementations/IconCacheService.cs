using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WinCarePro.Services.Implementations;

public class IconCacheService
{
    private readonly string _cacheDirectory;

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

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
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

        try
        {
            string hash = GetMd5Hash(filePath.ToLowerInvariant());
            string destPng = Path.Combine(_cacheDirectory, $"{hash}.png");

            if (File.Exists(destPng))
            {
                return destPng;
            }

            var getFileTask = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath).AsTask();
            getFileTask.Wait();
            var storageFile = getFileTask.Result;

            if (storageFile != null)
            {
                var getThumbTask = storageFile.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem,
                    32,
                    Windows.Storage.FileProperties.ThumbnailOptions.None
                ).AsTask();
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

    private static string GetMd5Hash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

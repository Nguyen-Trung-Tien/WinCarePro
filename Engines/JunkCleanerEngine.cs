using System;
using System.Collections.Generic;
using System.IO;
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

    public async Task<List<JunkCategory>> ScanJunkAsync()
    {
        Log("Starting Junk scan...");
        ProgressChanged?.Invoke(5);
        var categories = new List<JunkCategory>();

        // 1. User Temp
        Log("Scanning User Temp files...");
        string userTemp = Path.GetTempPath();
        var userTempCat = ScanDirectory(userTemp, "User Temporary Files", "Temporary files created by active applications.", JunkType.UserTemp);
        categories.Add(userTempCat);
        ProgressChanged?.Invoke(20);

        // 2. Windows Temp
        Log("Scanning Windows Temp directory...");
        string winTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "Temp");
        var winTempCat = ScanDirectory(winTemp, "System Temporary Files", "Temporary files created by the Windows OS.", JunkType.WindowsTemp);
        categories.Add(winTempCat);
        ProgressChanged?.Invoke(40);

        // 3. Windows Update Cache
        Log("Scanning Windows Update Cache...");
        string winUpdate = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows", "SoftwareDistribution\\Download");
        var winUpdateCat = ScanDirectory(winUpdate, "Windows Update Cache", "Old downloaded Windows Update installers.", JunkType.UpdateCache);
        categories.Add(winUpdateCat);
        ProgressChanged?.Invoke(55);

        // 4. Recycle Bin
        Log("Querying Recycle Bin status...");
        var rbCat = GetRecycleBinCategory();
        categories.Add(rbCat);
        ProgressChanged?.Invoke(70);

        // 5. Browser Caches (Edge & Chrome)
        Log("Scanning Browser caches...");
        long browserBytes = 0;
        int browserFiles = 0;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string chromeCache = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data");
        string edgeCache = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data");
        string braveCache = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache\Cache_Data");

        var caches = new[] { chromeCache, edgeCache, braveCache };
        foreach (var cachePath in caches)
        {
            if (Directory.Exists(cachePath))
            {
                var (bytes, count) = GetDirectorySize(cachePath);
                browserBytes += bytes;
                browserFiles += count;
            }
        }

        categories.Add(new JunkCategory
        {
            Name = "Web Browser Caches",
            Description = "Cached web pages, scripts, and media files from Edge, Chrome, and Brave.",
            Type = JunkType.BrowserCache,
            SizeBytes = browserBytes,
            FileCount = browserFiles,
            IsSelected = true
        });
        ProgressChanged?.Invoke(85);

        // 6. DirectX Shader Cache & Diagnostics
        Log("Scanning DirectX shader caches...");
        string shaderCachePath = Path.Combine(localAppData, @"D3DSCache");
        long shaderBytes = 0;
        int shaderCount = 0;
        if (Directory.Exists(shaderCachePath))
        {
            var (bytes, count) = GetDirectorySize(shaderCachePath);
            shaderBytes += bytes;
            shaderCount += count;
        }

        categories.Add(new JunkCategory
        {
            Name = "DirectX Shader Cache",
            Description = "Graphics processor shader cache. Speed up application load times, but safe to clear.",
            Type = JunkType.ShaderCache,
            SizeBytes = shaderBytes,
            FileCount = shaderCount,
            IsSelected = true
        });

        // 7. System Error Reports and Logs
        Log("Scanning System Error Reports and Log files...");
        long logBytes = 0;
        int logCount = 0;

        string werLocal = Path.Combine(localAppData, @"Microsoft\Windows\WER");
        string werCommon = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER");
        string iisLogs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles");

        var logPaths = new[] { werLocal, werCommon, iisLogs };
        foreach (var logPath in logPaths)
        {
            if (Directory.Exists(logPath))
            {
                var (bytes, count) = GetDirectorySize(logPath);
                logBytes += bytes;
                logCount += count;
            }
        }

        categories.Add(new JunkCategory
        {
            Name = "System Log Files & Error Reports",
            Description = "Activity logs, crash dump registers, and error reports generated by Windows.",
            Type = JunkType.SystemLog,
            SizeBytes = logBytes,
            FileCount = logCount,
            IsSelected = true
        });

        ProgressChanged?.Invoke(100);
        Log("Junk scan completed.");
        return categories;
    }

    public async Task<long> CleanJunkAsync(List<JunkCategory> categories)
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
                    break;
                case JunkType.BrowserCache:
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    cleaned += await ClearDirectoryAsync(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"));
                    cleaned += await ClearDirectoryAsync(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"));
                    cleaned += await ClearDirectoryAsync(Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache\Cache_Data"));
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
                    break;
            }

            totalCleanedBytes += cleaned;
            currentProgress += increment;
            ProgressChanged?.Invoke((int)currentProgress);
        }

        ProgressChanged?.Invoke(100);
        Log($"Junk cleaning complete. Cleaned: {(totalCleanedBytes / 1024.0 / 1024.0):F2} MB");
        Database.DbManager.LogAction($"Cleaned {totalCleanedBytes} bytes", "Junk Cleaner", "Success");
        return totalCleanedBytes;
    }

    private JunkCategory ScanDirectory(string path, string name, string description, JunkType type)
    {
        long bytes = 0;
        int count = 0;
        if (Directory.Exists(path))
        {
            (bytes, count) = GetDirectorySize(path);
        }
        return new JunkCategory
        {
            Name = name,
            Description = description,
            Type = type,
            SizeBytes = bytes,
            FileCount = count,
            IsSelected = true
        };
    }

    private (long bytes, int count) GetDirectorySize(string path)
    {
        long bytes = 0;
        int count = 0;
        try
        {
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    bytes += info.Length;
                    count++;
                }
                catch { } // skip locked files
            }
        }
        catch { }
        return (bytes, count);
    }

    private async Task<long> ClearDirectoryAsync(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long bytesDeleted = 0;

        await Task.Run(() =>
        {
            // Delete files
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        File.Delete(file);
                        bytesDeleted += size;
                    }
                    catch
                    {
                        // File locked, skip
                    }
                }
            }
            catch { }

            // Delete subdirectories
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    try
                    {
                        long size = GetDirectorySize(dir).bytes;
                        Directory.Delete(dir, true);
                        bytesDeleted += size;
                    }
                    catch
                    {
                        // Subdirectory locked, skip or recurse delete subfiles
                        try
                        {
                            bytesDeleted += ClearDirectorySync(dir);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        });

        return bytesDeleted;
    }

    private long ClearDirectorySync(string path)
    {
        long bytesDeleted = 0;
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    long size = info.Length;
                    File.Delete(file);
                    bytesDeleted += size;
                }
                catch { }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    bytesDeleted += ClearDirectorySync(dir);
                    Directory.Delete(dir, false);
                }
                catch { }
            }
        }
        catch { }
        return bytesDeleted;
    }

    private JunkCategory GetRecycleBinCategory()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
        int hr = SHQueryRecycleBin(null, ref info);
        long size = hr == 0 ? info.i64Size : 0;
        long count = hr == 0 ? info.i64NumItems : 0;

        return new JunkCategory
        {
            Name = "Recycle Bin",
            Description = "Deleted files stored in your Recycle Bin.",
            Type = JunkType.RecycleBin,
            SizeBytes = size,
            FileCount = (int)count,
            IsSelected = true
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

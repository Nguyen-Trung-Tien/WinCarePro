using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class LockingAppService : ILockingAppService
{
    private static readonly Dictionary<string, string> ProcessMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "chrome", "Google Chrome" },
        { "msedge", "Microsoft Edge" },
        { "brave", "Brave Browser" },
        { "firefox", "Mozilla Firefox" },
        { "opera", "Opera Browser" },
        { "Discord", "Discord" },
        { "Code", "Visual Studio Code" }
    };

    private readonly IconCacheService _iconCache;

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, System.Text.StringBuilder lpExeName, ref int lpdwSize);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private static string GetProcessExecutablePath(Process p)
    {
        if (p.Id <= 4) return "";

        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
        if (hProcess != IntPtr.Zero)
        {
            try
            {
                int capacity = 2048;
                var builder = new System.Text.StringBuilder(capacity);
                if (QueryFullProcessImageName(hProcess, 0, builder, ref capacity))
                {
                    return builder.ToString();
                }
            }
            catch { }
            finally
            {
                CloseHandle(hProcess);
            }
        }
        return "";
    }

    public LockingAppService(IconCacheService iconCache)
    {
        _iconCache = iconCache;
    }

    public LockingAppService() : this((App.Services?.GetService(typeof(IconCacheService)) as IconCacheService) ?? new IconCacheService())
    {
    }

    public async Task<List<LockingAppInfo>> GetLockingAppsAsync()
    {
        return await Task.Run(() =>
        {
            var lockingApps = new List<LockingAppInfo>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tempPath = Path.GetTempPath();

            foreach (var kvp in ProcessMap)
            {
                try
                {
                    var processes = Process.GetProcessesByName(kvp.Key);
                    if (processes.Length > 0)
                    {
                        long size = GetEstimatedCacheSize(kvp.Key, localAppData, appData, tempPath);
                        
                        string iconPath = "";
                        try
                        {
                            var firstProc = processes.FirstOrDefault();
                            if (firstProc != null)
                            {
                                string exePath = GetProcessExecutablePath(firstProc);
                                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                                {
                                    iconPath = _iconCache.GetIconForExecutable(exePath);
                                }
                            }
                        }
                        catch {}

                        var info = new LockingAppInfo
                        {
                            Name = kvp.Value,
                            ProcessCount = processes.Length,
                            LockedSizeBytes = size,
                            ProcessIds = processes.Select(p => p.Id).ToList(),
                            IconPath = iconPath
                        };
                        lockingApps.Add(info);
                    }
                }
                catch {}
            }
            return lockingApps;
        });
    }

    public async Task CloseAppsAsync(IEnumerable<LockingAppInfo> apps, Func<string, Task<bool>> confirmForceClose)
    {
        foreach (var app in apps)
        {
            foreach (var pid in app.ProcessIds)
            {
                try
                {
                    using var process = Process.GetProcessById(pid);
                    if (process == null || process.HasExited) continue;

                    // Try to close gracefully
                    process.CloseMainWindow();
                    
                    // Wait up to 5 seconds
                    bool exited = false;
                    for (int i = 0; i < 50; i++) // 50 * 100ms = 5000ms
                    {
                        if (process.HasExited)
                        {
                            exited = true;
                            break;
                        }
                        await Task.Delay(100);
                    }

                    if (!exited)
                    {
                        // Prompt user before force termination
                        bool force = await confirmForceClose(app.Name);
                        if (force)
                        {
                            process.Kill(true);
                        }
                    }
                }
                catch {}
            }
        }
        await Task.Delay(500); // Give the system some time to release locks
    }

    private static long GetEstimatedCacheSize(string processName, string localAppData, string appData, string tempPath)
    {
        try
        {
            switch (processName.ToLower())
            {
                case "chrome":
                    return GetDirectorySize(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"));
                case "msedge":
                    return GetDirectorySize(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"));
                case "brave":
                    return GetDirectorySize(Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache"));
                case "opera":
                    return GetDirectorySize(Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache"));
                case "firefox":
                    string ffPath = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
                    if (Directory.Exists(ffPath))
                    {
                        long size = 0;
                        foreach (var d in Directory.GetDirectories(ffPath))
                        {
                            size += GetDirectorySize(Path.Combine(d, "cache2"));
                        }
                        return size;
                    }
                    break;
                case "discord":
                    return GetDirectorySize(Path.Combine(appData, @"discord\Cache"));
                case "code":
                    return GetDirectorySize(Path.Combine(appData, @"Code\Cache"));
            }
        }
        catch {}
        return 0;
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch {}
            }
        }
        catch {}
        return size;
    }
}

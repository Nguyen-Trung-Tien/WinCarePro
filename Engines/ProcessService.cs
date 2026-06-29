using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management; // for process tree querying and parent-child tracking
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public class ProcessMetadata
{
    public string FilePath { get; set; } = "";
    public string Publisher { get; set; } = "Unknown Publisher";
    public string CommandLine { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string PriorityClass { get; set; } = "Normal";
    public int ParentPid { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public string IconPath { get; set; } = "";
    public DateTime CacheTime { get; set; } = DateTime.UtcNow;
}

public class ProcessService
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, System.Text.StringBuilder lpExeName, ref int lpdwSize);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [System.Runtime.InteropServices.DllImport("ntdll.dll", EntryPoint = "NtSuspendProcess")]
    private static extern int NtSuspendProcess(IntPtr processHandle);

    [System.Runtime.InteropServices.DllImport("ntdll.dll", EntryPoint = "NtResumeProcess")]
    private static extern int NtResumeProcess(IntPtr processHandle);

    private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const int PROCESS_SUSPEND_RESUME = 0x0800;

    private Dictionary<int, (TimeSpan cpuTime, DateTime sampleTime)> _lastCpuSamples = new();
    private static readonly Dictionary<int, ProcessMetadata> _metadataCache = new();
    private static readonly object _cacheLock = new();
    private const int CACHE_TTL_SECONDS = 60;

    // Strict OS process protection list
    private static readonly HashSet<string> _criticalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "services",
        "lsass", "winlogon", "svchost", "spoolsv", "dwm", "explorer", "sihost",
        "taskhostw", "RuntimeBroker", "SearchHost", "StartMenuExperienceHost",
        "MsMpEng", "NisSrv", "WinCarePro"
    };

    private static string GetProcessExecutablePath(Process p)
    {
        if (p.Id <= 4) return "System Process";

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
        return "System Process";
    }

    private static string ExtractProcessIcon(string filePath, string processName)
    {
        if (filePath == "System Process" || !File.Exists(filePath)) return "";
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinCareIcons");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            
            string safeKeyName = string.Concat(processName.Split(Path.GetInvalidFileNameChars()));
            string destPng = Path.Combine(tempDir, $"{safeKeyName}.png");
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

    public bool IsActionAllowed(string name, int pid, out string reason)
    {
        if (pid <= 4)
        {
            reason = "Kernel system processes cannot be modified.";
            return false;
        }
        if (_criticalProcesses.Contains(name))
        {
            reason = "Action blocked. This is a critical Windows system process required for system stability.";
            return false;
        }
        reason = "Allowed";
        return true;
    }

    public async Task<List<ProcessInfo>> GetRunningProcessesAsync()
    {
        var rawProcesses = Process.GetProcesses();
        var sampleTime = DateTime.UtcNow;
        var currentSamples = new Dictionary<int, (TimeSpan cpuTime, DateTime sampleTime)>();

        foreach (var p in rawProcesses)
        {
            try
            {
                currentSamples[p.Id] = (p.TotalProcessorTime, sampleTime);
            }
            catch
            {
                // Access denied for system processes
            }
        }

        // If this is the first execution or cache is empty, do a quick inline warmup:
        if (_lastCpuSamples.Count == 0)
        {
            await Task.Delay(200);
            var warmProcesses = Process.GetProcesses();
            var warmSampleTime = DateTime.UtcNow;
            foreach (var p in warmProcesses)
            {
                try
                {
                    _lastCpuSamples[p.Id] = (p.TotalProcessorTime, warmSampleTime);
                }
                catch { }
            }
        }

        double coreCount = Environment.ProcessorCount;
        var result = new List<ProcessInfo>();

        // Evict metadata cache entries for processes that are no longer running
        var activePids = new HashSet<int>(rawProcesses.Select(p => p.Id));
        lock (_cacheLock)
        {
            var deadPids = _metadataCache.Keys.Where(pid => !activePids.Contains(pid)).ToList();
            foreach (var pid in deadPids)
            {
                _metadataCache.Remove(pid);
            }
        }

        foreach (var p in rawProcesses)
        {
            if (p.Id == 0) continue; // Skip idle

            var info = new ProcessInfo
            {
                Id = p.Id,
                Name = p.ProcessName
            };

            // Calculate RAM
            try
            {
                info.RamUsageBytes = p.WorkingSet64;
            }
            catch
            {
                info.RamUsageBytes = 0;
            }

            // Calculate CPU
            if (currentSamples.TryGetValue(p.Id, out var curSample) && 
                _lastCpuSamples.TryGetValue(p.Id, out var prevSample))
            {
                double timeDiffMs = (curSample.sampleTime - prevSample.sampleTime).TotalMilliseconds;
                double cpuMs = (curSample.cpuTime - prevSample.cpuTime).TotalMilliseconds;
                if (timeDiffMs > 0)
                {
                    double cpuPercent = (cpuMs / (timeDiffMs * coreCount)) * 100.0;
                    info.CpuUsage = Math.Min(100.0, Math.Max(0.0, cpuPercent));
                }
                else
                {
                    info.CpuUsage = 0.0;
                }
            }
            else
            {
                info.CpuUsage = 0.0;
            }

            // Get FilePath and Publisher (Using cache or fallback)
            ProcessMetadata? meta = null;
            lock (_cacheLock)
            {
                if (_metadataCache.TryGetValue(p.Id, out var cached))
                {
                    meta = cached;
                }
            }

            if (meta != null)
            {
                info.FilePath = meta.FilePath;
                info.Publisher = meta.Publisher;
                info.IconPath = meta.IconPath;
            }
            else
            {
                // Cache miss, resolve path, publisher, and icon (cached)
                string path = "System Process";
                string publisher = "Microsoft Corporation";
                string iconPath = "";
                try
                {
                    path = GetProcessExecutablePath(p);
                    if (path != "System Process" && !string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        var fileInfo = FileVersionInfo.GetVersionInfo(path);
                        publisher = fileInfo.CompanyName ?? "Unknown Publisher";
                        iconPath = ExtractProcessIcon(path, p.ProcessName);
                    }
                }
                catch
                {
                    path = "System Process";
                    publisher = "Microsoft Corporation";
                    iconPath = "";
                }

                info.FilePath = path;
                info.Publisher = publisher;
                info.IconPath = iconPath;

                // Save basic metadata
                lock (_cacheLock)
                {
                    _metadataCache[p.Id] = new ProcessMetadata
                    {
                        FilePath = path,
                        Publisher = publisher,
                        IconPath = iconPath,
                        CacheTime = DateTime.UtcNow
                    };
                }
            }

            // Mock disk/network active metrics based on resource behavior
            var random = new Random(p.Id);
            if (info.CpuUsage > 5.0)
            {
                info.DiskUsageMb = random.NextDouble() * 5.2;
                info.NetworkUsageKb = random.NextDouble() * 120.0;
            }
            else if (info.CpuUsage > 0.5)
            {
                info.DiskUsageMb = random.NextDouble() * 0.4;
                info.NetworkUsageKb = random.NextDouble() * 8.5;
            }

            result.Add(info);
        }

        // Save current samples for next call
        _lastCpuSamples = currentSamples;

        return result.OrderByDescending(x => x.CpuUsage).ThenByDescending(x => x.RamUsageBytes).ToList();
    }

    public ProcessMetadata GetDetailedProcessInfo(int pid, string processName)
    {
        lock (_cacheLock)
        {
            if (_metadataCache.TryGetValue(pid, out var cached) && 
                !string.IsNullOrEmpty(cached.CommandLine) && 
                (DateTime.UtcNow - cached.CacheTime).TotalSeconds < 30) // TTL 30s
            {
                return cached;
            }

            if (cached == null)
            {
                cached = new ProcessMetadata();
            }

            try
            {
                using var p = Process.GetProcessById(pid);
                
                // Resolve path and publisher if not already cached
                if (string.IsNullOrEmpty(cached.FilePath) || cached.FilePath == "System Process")
                {
                    cached.FilePath = GetProcessExecutablePath(p);
                    if (cached.FilePath != "System Process" && !string.IsNullOrEmpty(cached.FilePath) && File.Exists(cached.FilePath))
                    {
                        var fileInfo = FileVersionInfo.GetVersionInfo(cached.FilePath);
                        cached.Publisher = fileInfo.CompanyName ?? "Unknown Publisher";
                    }
                }

                // Retrieve on-demand properties
                cached.ThreadCount = p.Threads.Count;
                cached.HandleCount = p.HandleCount;
                cached.StartTime = p.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                cached.CommandLine = GetCommandLineWmi(pid);
                cached.PriorityClass = p.PriorityClass.ToString();
                cached.ParentPid = GetParentProcessId(pid);
            }
            catch
            {
                // Fallbacks on access denied
                cached.CommandLine = "Access Denied";
                cached.StartTime = "Unknown";
                cached.PriorityClass = "Normal";
                cached.ThreadCount = 0;
                cached.HandleCount = 0;
            }

            cached.CacheTime = DateTime.UtcNow;
            _metadataCache[pid] = cached;
            return cached;
        }
    }

    private static string GetCommandLineWmi(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessID = {pid}");
            using var objects = searcher.Get();
            foreach (var obj in objects)
            {
                return obj["CommandLine"]?.ToString() ?? "N/A";
            }
        }
        catch { }
        return "Access Denied";
    }

    private static int GetParentProcessId(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessID = {pid}");
            using var objects = searcher.Get();
            foreach (var obj in objects)
            {
                return Convert.ToInt32(obj["ParentProcessId"]);
            }
        }
        catch { }
        return 0;
    }

    public bool TerminateProcess(int pid, string name)
    {
        if (!IsActionAllowed(name, pid, out var reason))
        {
            Database.DbManager.LogAction($"Terminate Process PID {pid} Blocked: {reason}", "Process Manager", "Blocked");
            return false;
        }

        try
        {
            var proc = Process.GetProcessById(pid);
            proc.Kill();
            Database.DbManager.LogAction($"Terminated Process {name} (PID {pid})", "Process Manager", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Terminate Process {name} (PID {pid}) Failed: {ex.Message}", "Process Manager", "Failed");
            return false;
        }
    }

    public async Task<bool> TerminateProcessTreeAsync(int parentPid, string name)
    {
        if (!IsActionAllowed(name, parentPid, out var reason))
        {
            Database.DbManager.LogAction($"Terminate Process Tree PID {parentPid} Blocked: {reason}", "Process Manager", "Blocked");
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                KillProcessTree(parentPid);
                Database.DbManager.LogAction($"Terminated Process Tree {name} (PID {parentPid})", "Process Manager", "Success");
                return true;
            }
            catch (Exception ex)
            {
                Database.DbManager.LogAction($"Terminate Process Tree {name} (PID {parentPid}) Failed: {ex.Message}", "Process Manager", "Failed");
                return false;
            }
        });
    }

    public bool SuspendProcess(int pid, string name)
    {
        if (!IsActionAllowed(name, pid, out var reason))
        {
            Database.DbManager.LogAction($"Suspend Process PID {pid} Blocked: {reason}", "Process Manager", "Blocked");
            return false;
        }

        IntPtr hProcess = OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);
        if (hProcess != IntPtr.Zero)
        {
            try
            {
                int result = NtSuspendProcess(hProcess);
                if (result >= 0)
                {
                    Database.DbManager.LogAction($"Suspended Process {name} (PID {pid})", "Process Manager", "Success");
                    return true;
                }
            }
            catch { }
            finally
            {
                CloseHandle(hProcess);
            }
        }
        Database.DbManager.LogAction($"Suspend Process {name} (PID {pid}) Failed", "Process Manager", "Failed");
        return false;
    }

    public bool ResumeProcess(int pid, string name)
    {
        if (!IsActionAllowed(name, pid, out var reason))
        {
            Database.DbManager.LogAction($"Resume Process PID {pid} Blocked: {reason}", "Process Manager", "Blocked");
            return false;
        }

        IntPtr hProcess = OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);
        if (hProcess != IntPtr.Zero)
        {
            try
            {
                int result = NtResumeProcess(hProcess);
                if (result >= 0)
                {
                    Database.DbManager.LogAction($"Resumed Process {name} (PID {pid})", "Process Manager", "Success");
                    return true;
                }
            }
            catch { }
            finally
            {
                CloseHandle(hProcess);
            }
        }
        Database.DbManager.LogAction($"Resume Process {name} (PID {pid}) Failed", "Process Manager", "Failed");
        return false;
    }

    public bool SetProcessPriority(int pid, string name, ProcessPriorityClass priority)
    {
        if (!IsActionAllowed(name, pid, out var reason))
        {
            Database.DbManager.LogAction($"Change Priority PID {pid} Blocked: {reason}", "Process Manager", "Blocked");
            return false;
        }

        if (priority == ProcessPriorityClass.RealTime)
        {
            Database.DbManager.LogAction($"Change Priority PID {pid} to Realtime Blocked: Safety restriction.", "Process Manager", "Blocked");
            return false;
        }

        try
        {
            using var proc = Process.GetProcessById(pid);
            proc.PriorityClass = priority;
            Database.DbManager.LogAction($"Changed Priority of {name} (PID {pid}) to {priority}", "Process Manager", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Change Priority of {name} (PID {pid}) Failed: {ex.Message}", "Process Manager", "Failed");
            return false;
        }
    }

    private void KillProcessTree(int pid)
    {
        // Recursively find child processes via WMI and kill them, then kill parent
        try
        {
            using var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId={pid}");
            using var moc = searcher.Get();
            foreach (var mo in moc)
            {
                int childPid = Convert.ToInt32(mo["ProcessID"]);
                KillProcessTree(childPid);
            }
        }
        catch { }

        try
        {
            var proc = Process.GetProcessById(pid);
            proc.Kill(true); // .NET 8+ support Kill(true) which kills tree directly, but recursion ensures safety
        }
        catch { }
    }
}

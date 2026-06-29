using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class NetworkHistoryService : INetworkHistoryService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "WinCarePro",
        "NetworkHistory"
    );
    
    private static readonly string SpeedHistoryPath = Path.Combine(AppDataPath, "speed_history.json");
    private static readonly string DnsHistoryPath = Path.Combine(AppDataPath, "dns_history.json");
    
    private readonly object _lock = new();

    public NetworkHistoryService()
    {
        try
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }
        catch { }
    }

    public async Task SaveSpeedTestResultAsync(SpeedTestResult result)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var list = LoadList<SpeedTestResult>(SpeedHistoryPath);
                    list.Insert(0, result);
                    if (list.Count > 120)
                    {
                        list.RemoveRange(120, list.Count - 120);
                    }
                    SaveList(SpeedHistoryPath, list);
                }
                catch { }
            }
        });
    }

    public async Task<List<SpeedTestResult>> GetSpeedTestHistoryAsync(int limit = 120)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var list = LoadList<SpeedTestResult>(SpeedHistoryPath);
                    if (list.Count > limit)
                    {
                        return list.GetRange(0, limit);
                    }
                    return list;
                }
                catch
                {
                    return new List<SpeedTestResult>();
                }
            }
        });
    }

    public async Task SaveDnsBenchmarkResultAsync(List<DnsServerInfo> results)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var list = LoadList<List<DnsServerInfo>>(DnsHistoryPath);
                    list.Insert(0, results);
                    if (list.Count > 20)
                    {
                        list.RemoveRange(20, list.Count - 20);
                    }
                    SaveList(DnsHistoryPath, list);
                }
                catch { }
            }
        });
    }

    public async Task<List<List<DnsServerInfo>>> GetDnsBenchmarkHistoryAsync(int limit = 20)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var list = LoadList<List<DnsServerInfo>>(DnsHistoryPath);
                    if (list.Count > limit)
                    {
                        return list.GetRange(0, limit);
                    }
                    return list;
                }
                catch
                {
                    return new List<List<DnsServerInfo>>();
                }
            }
        });
    }

    public async Task ClearHistoryAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(SpeedHistoryPath)) File.Delete(SpeedHistoryPath);
                    if (File.Exists(DnsHistoryPath)) File.Delete(DnsHistoryPath);
                }
                catch { }
            }
        });
    }

    private List<T> LoadList<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private void SaveList<T>(string filePath, List<T> list)
    {
        try
        {
            string json = JsonSerializer.Serialize(list);
            File.WriteAllText(filePath, json);
        }
        catch { }
    }
}

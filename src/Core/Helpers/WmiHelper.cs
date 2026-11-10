using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management;

namespace WinCarePro.Core.Helpers;

public static class WmiHelper
{
    private static readonly ConcurrentDictionary<string, (object Data, DateTime Expiry)> Cache = new();

    /// <summary>
    /// Executes a WMI query and maps each ManagementObject to type T with deterministic resource disposal.
    /// </summary>
    public static List<T> Query<T>(string query, Func<ManagementObject, T> mapper, string scope = @"root\cimv2")
    {
        var list = new List<T>();
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, query);
            searcher.Options.Timeout = TimeSpan.FromSeconds(5);
            searcher.Options.ReturnImmediately = true;
            using var collection = searcher.Get();
            foreach (ManagementObject obj in collection)
            {
                using (obj)
                {
                    try
                    {
                        list.Add(mapper(obj));
                    }
                    catch
                    {
                        // Ignore mapping error for individual item
                    }
                }
            }
        }
        catch
        {
            // Ignore WMI errors
        }
        return list;
    }

    /// <summary>
    /// Executes a WMI query with TTL memory caching. Prevents repetitive expensive WMI COM queries
    /// for static hardware specs (CPU model, total RAM, GPU device ID, BIOS info).
    /// </summary>
    public static List<T> QueryCached<T>(string query, Func<ManagementObject, T> mapper, TimeSpan cacheDuration, string scope = @"root\cimv2")
    {
        string cacheKey = $"{scope}::{query}";
        DateTime now = DateTime.UtcNow;

        if (Cache.TryGetValue(cacheKey, out var entry) && entry.Expiry > now && entry.Data is List<T> cachedList)
        {
            return cachedList;
        }

        var result = Query(query, mapper, scope);
        Cache[cacheKey] = (result, now.Add(cacheDuration));
        return result;
    }

    /// <summary>
    /// Clears expired entries or invalidates all/specific WMI cache entries.
    /// </summary>
    public static void InvalidateCache(string? query = null, string scope = @"root\cimv2")
    {
        if (string.IsNullOrEmpty(query))
        {
            Cache.Clear();
        }
        else
        {
            string cacheKey = $"{scope}::{query}";
            Cache.TryRemove(cacheKey, out _);
        }
    }
}


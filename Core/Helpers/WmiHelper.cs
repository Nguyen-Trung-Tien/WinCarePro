using System;
using System.Collections.Generic;
using System.Management;

namespace WinCarePro.Core.Helpers;

public static class WmiHelper
{
    public static List<T> Query<T>(string query, Func<ManagementObject, T> mapper, string scope = @"root\cimv2")
    {
        var list = new List<T>();
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, query);
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
}

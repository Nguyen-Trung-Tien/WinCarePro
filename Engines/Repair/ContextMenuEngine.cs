using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Engines;

public class ContextMenuEngine
{
    public event Action<string>? ProgressMessage;
    private void Log(string msg) => ProgressMessage?.Invoke(msg);

    private readonly string[] _targetPaths = new[]
    {
        @"*\shellex\ContextMenuHandlers",
        @"Directory\Background\shellex\ContextMenuHandlers",
        @"Directory\shellex\ContextMenuHandlers",
        @"Folder\shellex\ContextMenuHandlers"
    };

    private string GetTypeName(string registryPath)
    {
        if (registryPath.Contains(@"*\shellex")) return "All Files".T();
        if (registryPath.Contains(@"Directory\Background")) return "Desktop Background".T();
        if (registryPath.Contains(@"Directory\shellex")) return "Folders".T();
        if (registryPath.Contains(@"Folder\shellex")) return "Folders".T();
        return "Unknown".T();
    }

    private bool IsGuid(string str)
    {
        if (string.IsNullOrEmpty(str)) return false;
        string clean = str.Trim().Replace("-", ""); // ignore leading minus sign
        if (clean.StartsWith("{") && clean.EndsWith("}") && clean.Length == 38)
        {
            return true;
        }
        return false;
    }

    private string GetFriendlyNameFromClsid(string clsid)
    {
        if (string.IsNullOrEmpty(clsid)) return "";
        string cleanClsid = clsid.Trim().Replace("-{", "{"); // clean leading minus
        if (cleanClsid.StartsWith("-")) cleanClsid = cleanClsid.Substring(1);

        try
        {
            using var clsidKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{cleanClsid}");
            var val = clsidKey?.GetValue("")?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                return val;
            }
        }
        catch { }
        return "";
    }

    public Task<List<ContextMenuItem>> ScanContextMenuItemsAsync()
    {
        return Task.Run(() =>
        {
            var items = new List<ContextMenuItem>();
            var seenIds = new HashSet<string>();

            foreach (var basePath in _targetPaths)
            {
                Log(string.Format("Scanning registry path: {0}".T(), basePath));
                try
                {
                    using var parentKey = Registry.ClassesRoot.OpenSubKey(basePath, false);
                    if (parentKey == null) continue;

                    var subKeyNames = parentKey.GetSubKeyNames();
                    foreach (var subKeyName in subKeyNames)
                    {
                        try
                        {
                            using var subKey = parentKey.OpenSubKey(subKeyName, false);
                            if (subKey == null) continue;

                            string defaultValue = subKey.GetValue("")?.ToString() ?? "";
                            string clsid = "";
                            string displayName = "";
                            bool isEnabled = true;

                            // Determine if key is disabled by check prefix
                            string cleanKeyName = subKeyName;
                            if (cleanKeyName.StartsWith("-"))
                            {
                                isEnabled = false;
                                cleanKeyName = cleanKeyName.Substring(1);
                            }

                            string cleanDefaultValue = defaultValue;
                            if (cleanDefaultValue.StartsWith("-"))
                            {
                                isEnabled = false;
                                cleanDefaultValue = cleanDefaultValue.Substring(1);
                            }

                            // Resolve CLSID & Name
                            if (IsGuid(cleanDefaultValue))
                            {
                                clsid = cleanDefaultValue;
                                displayName = GetFriendlyNameFromClsid(clsid);
                            }
                            else if (IsGuid(cleanKeyName))
                            {
                                clsid = cleanKeyName;
                                displayName = GetFriendlyNameFromClsid(clsid);
                            }

                            if (string.IsNullOrEmpty(displayName))
                            {
                                displayName = cleanKeyName;
                            }

                            // Build unique ID to prevent duplicates
                            string uniqueId = $"{basePath}\\{subKeyName}".ToLower();
                            if (seenIds.Contains(uniqueId)) continue;
                            seenIds.Add(uniqueId);

                            items.Add(new ContextMenuItem
                            {
                                Id = subKeyName,
                                Name = displayName,
                                RegistryPath = Path.Combine("HKEY_CLASSES_ROOT", basePath, subKeyName),
                                Type = GetTypeName(basePath),
                                IsEnabled = isEnabled,
                                ClassId = clsid
                            });
                        }
                        catch (Exception ex)
                        {
                            Log(string.Format("Error scanning subkey {0}: {1}".T(), subKeyName, ex.Message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(string.Format("Error opening parent key {0}: {1}".T(), basePath, ex.Message));
                }
            }

            return items;
        });
    }

    public Task<bool> ToggleContextMenuItemAsync(ContextMenuItem item, bool enable)
    {
        return Task.Run(() =>
        {
            if (item.IsEnabled == enable) return true;

            // Extract the registry path relative to HKEY_CLASSES_ROOT
            // RegistryPath looks like: HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\SubKeyName
            string prefix = @"HKEY_CLASSES_ROOT\";
            if (!item.RegistryPath.StartsWith(prefix)) return false;

            string relativePath = item.RegistryPath.Substring(prefix.Length);
            // relativePath: *\shellex\ContextMenuHandlers\SubKeyName
            string parentPath = Path.GetDirectoryName(relativePath) ?? "";
            string keyName = Path.GetFileName(relativePath) ?? "";

            try
            {
                using var parentKey = Registry.ClassesRoot.OpenSubKey(parentPath, true);
                if (parentKey == null) return false;

                // Case 1: Check if the key name itself needs to be renamed
                // (e.g. if the key name is the GUID, like HKEY_CLASSES_ROOT\...\ContextMenuHandlers\{GUID})
                if (IsGuid(keyName) || IsGuid(keyName.StartsWith("-") ? keyName.Substring(1) : keyName))
                {
                    string oldKeyName = keyName;
                    string newKeyName = enable ? 
                        (oldKeyName.StartsWith("-") ? oldKeyName.Substring(1) : oldKeyName) :
                        (oldKeyName.StartsWith("-") ? oldKeyName : "-" + oldKeyName);

                    if (oldKeyName != newKeyName)
                    {
                        RenameSubKey(parentKey, oldKeyName, newKeyName);
                        // Update item path
                        item.Id = newKeyName;
                        item.RegistryPath = Path.Combine(prefix, parentPath, newKeyName);
                        item.IsEnabled = enable;
                        return true;
                    }
                }

                // Case 2: Key default value is the GUID (e.g. default value is {GUID})
                using var subKey = parentKey.OpenSubKey(keyName, true);
                if (subKey != null)
                {
                    string defaultValue = subKey.GetValue("")?.ToString() ?? "";
                    if (IsGuid(defaultValue) || IsGuid(defaultValue.StartsWith("-") ? defaultValue.Substring(1) : defaultValue))
                    {
                        string newDefaultValue = enable ?
                            (defaultValue.StartsWith("-") ? defaultValue.Substring(1) : defaultValue) :
                            (defaultValue.StartsWith("-") ? defaultValue : "-" + defaultValue);
                        
                        subKey.SetValue("", newDefaultValue);
                        item.IsEnabled = enable;
                        return true;
                    }
                }

                // Case 3: If it's a normal key name and does not have a GUID default value, we can rename the key
                string fallbackOldName = keyName;
                string fallbackNewName = enable ?
                    (fallbackOldName.StartsWith("-") ? fallbackOldName.Substring(1) : fallbackOldName) :
                    (fallbackOldName.StartsWith("-") ? fallbackOldName : "-" + fallbackOldName);

                if (fallbackOldName != fallbackNewName)
                {
                    RenameSubKey(parentKey, fallbackOldName, fallbackNewName);
                    item.Id = fallbackNewName;
                    item.RegistryPath = Path.Combine(prefix, parentPath, fallbackNewName);
                    item.IsEnabled = enable;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log(string.Format("Error modifying registry: {0}".T(), ex.Message));
                return false;
            }
        });
    }

    private static void RenameSubKey(RegistryKey parentKey, string oldName, string newName)
    {
        using var oldKey = parentKey.OpenSubKey(oldName);
        if (oldKey == null) return;

        using var newKey = parentKey.CreateSubKey(newName);
        // Copy values
        foreach (var valueName in oldKey.GetValueNames())
        {
            newKey.SetValue(valueName, oldKey.GetValue(valueName)!, oldKey.GetValueKind(valueName));
        }
        // Copy subkeys recursively
        CopyRegistrySubKeys(oldKey, newKey);
        
        oldKey.Close();
        parentKey.DeleteSubKeyTree(oldName);
    }

    private static void CopyRegistrySubKeys(RegistryKey source, RegistryKey destination)
    {
        foreach (var subKeyName in source.GetSubKeyNames())
        {
            using var sourceSubKey = source.OpenSubKey(subKeyName);
            using var destSubKey = destination.CreateSubKey(subKeyName);
            if (sourceSubKey != null && destSubKey != null)
            {
                foreach (var valName in sourceSubKey.GetValueNames())
                {
                    destSubKey.SetValue(valName, sourceSubKey.GetValue(valName)!, sourceSubKey.GetValueKind(valName));
                }
                CopyRegistrySubKeys(sourceSubKey, destSubKey);
            }
        }
    }
}

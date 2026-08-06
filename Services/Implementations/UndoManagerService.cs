using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Database;

namespace WinCarePro.Services.Implementations;

public class UndoManagerService
{
    /// <summary>
    /// Records a snapshot of a Registry DWORD/String value prior to modification.
    /// </summary>
    public void RecordRegistrySnapshot(string category, string registryKeyPath, string valueName, object? currentValue, object? newValue)
    {
        string keyIdentifier = $"{registryKeyPath}\\{valueName}";
        string origValStr = currentValue?.ToString() ?? "";
        string newValStr = newValue?.ToString() ?? "";
        
        DbManager.SaveSnapshot(category, keyIdentifier, origValStr, newValStr);
    }

    /// <summary>
    /// Rolls back a recorded snapshot key to its original value.
    /// </summary>
    public bool RollbackSnapshot(StateSnapshotEntry snapshot)
    {
        if (snapshot == null || string.IsNullOrEmpty(snapshot.KeyName)) return false;

        try
        {
            if (snapshot.Category.Equals("Registry", StringComparison.OrdinalIgnoreCase) ||
                snapshot.KeyName.Contains('\\'))
            {
                int lastSlash = snapshot.KeyName.LastIndexOf('\\');
                if (lastSlash <= 0) return false;

                string keyPath = snapshot.KeyName.Substring(0, lastSlash);
                string valName = snapshot.KeyName.Substring(lastSlash + 1);

                RegistryKey baseKey;
                string subPath;

                if (keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
                {
                    baseKey = Registry.CurrentUser;
                    subPath = keyPath.Substring(5);
                }
                else if (keyPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
                {
                    baseKey = Registry.LocalMachine;
                    subPath = keyPath.Substring(5);
                }
                else
                {
                    return false;
                }

                using var targetKey = baseKey.OpenSubKey(subPath, writable: true);
                if (targetKey != null)
                {
                    if (string.IsNullOrEmpty(snapshot.OriginalValue))
                    {
                        targetKey.DeleteValue(valName, false);
                    }
                    else
                    {
                        if (int.TryParse(snapshot.OriginalValue, out int intVal))
                        {
                            targetKey.SetValue(valName, intVal, RegistryValueKind.DWord);
                        }
                        else
                        {
                            targetKey.SetValue(valName, snapshot.OriginalValue, RegistryValueKind.String);
                        }
                    }
                    DbManager.LogAction($"Rollback Snapshot: {snapshot.KeyName}", "UndoManager", "Success");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Rollback Failed: {snapshot.KeyName} - {ex.Message}", "UndoManager", "Failed");
        }
        return false;
    }

    public List<StateSnapshotEntry> GetRecentSnapshots(string? category = null)
    {
        return DbManager.GetSnapshots(category);
    }
}

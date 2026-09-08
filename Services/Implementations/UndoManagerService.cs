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
    /// Checks SafeRegistryGuard before every OpenSubKey writable, SetValue, and DeleteValue.
    /// Rejects invalid snapshots and protected keys/values safely, writing an audit log.
    /// </summary>
    public bool RollbackSnapshot(StateSnapshotEntry snapshot)
    {
        var result = RollbackSnapshotDetailed(snapshot);
        return result.IsSuccess;
    }

    /// <summary>
    /// Detailed rollback operation returning an OperationResult with explicit error context.
    /// </summary>
    public WinCarePro.Core.Models.OperationResult RollbackSnapshotDetailed(StateSnapshotEntry snapshot)
    {
        if (snapshot == null)
        {
            DbManager.LogAction("Rollback Rejected: Snapshot entry is null.", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail("Snapshot entry is null.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.KeyName))
        {
            DbManager.LogAction("Rollback Rejected: Snapshot KeyName is null or whitespace.", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail("Snapshot KeyName is null or whitespace.");
        }

        int lastSlash = snapshot.KeyName.LastIndexOf('\\');
        if (lastSlash <= 0 || lastSlash >= snapshot.KeyName.Length - 1)
        {
            DbManager.LogAction($"Rollback Rejected: Malformed snapshot KeyName '{snapshot.KeyName}'.", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail($"Malformed snapshot KeyName '{snapshot.KeyName}'.");
        }

        string keyPath = snapshot.KeyName.Substring(0, lastSlash).Trim();
        string valName = snapshot.KeyName.Substring(lastSlash + 1).Trim();

        if (string.IsNullOrWhiteSpace(valName))
        {
            DbManager.LogAction($"Rollback Rejected: Empty value name in snapshot '{snapshot.KeyName}'.", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail("Value name cannot be empty for rollback.");
        }

        bool isDelete = string.IsNullOrEmpty(snapshot.OriginalValue);

        // Security check via SafeRegistryGuard before ANY registry access
        if (!WinCarePro.Core.Helpers.SafeRegistryGuard.IsSafeToModifyKey(keyPath))
        {
            DbManager.LogAction($"Rollback Blocked: Protected registry key '{keyPath}' rejected by SafeRegistryGuard.", "UndoManager", "Blocked");
            return WinCarePro.Core.Models.OperationResult.Fail($"Operation blocked: Registry key '{keyPath}' is protected.");
        }

        if (!WinCarePro.Core.Helpers.SafeRegistryGuard.IsSafeToModifyValue(keyPath, valName))
        {
            DbManager.LogAction($"Rollback Blocked: Protected registry value '{valName}' under '{keyPath}' rejected by SafeRegistryGuard.", "UndoManager", "Blocked");
            return WinCarePro.Core.Models.OperationResult.Fail($"Operation blocked: Registry value '{valName}' is protected.");
        }

        if (isDelete && !WinCarePro.Core.Helpers.SafeRegistryGuard.IsSafeToDeleteValue(keyPath, valName))
        {
            DbManager.LogAction($"Rollback Blocked: Value '{valName}' under '{keyPath}' is protected against deletion.", "UndoManager", "Blocked");
            return WinCarePro.Core.Models.OperationResult.Fail($"Operation blocked: Value '{valName}' is protected against deletion.");
        }

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
            DbManager.LogAction($"Rollback Rejected: Unsupported registry hive in '{snapshot.KeyName}'.", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail($"Unsupported registry hive in '{snapshot.KeyName}'.");
        }

        try
        {
            using var targetKey = baseKey.OpenSubKey(subPath, writable: true);
            if (targetKey == null)
            {
                DbManager.LogAction($"Rollback Failed: Subkey '{subPath}' could not be opened writable.", "UndoManager", "Failed");
                return WinCarePro.Core.Models.OperationResult.Fail($"Subkey '{subPath}' could not be opened writable.");
            }

            if (isDelete)
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
                    targetKey.SetValue(valName, snapshot.OriginalValue!, RegistryValueKind.String);
                }
            }

            DbManager.LogAction($"Rollback Snapshot: {snapshot.KeyName}", "UndoManager", "Success");
            return WinCarePro.Core.Models.OperationResult.Ok($"Rollback completed successfully for '{snapshot.KeyName}'.");
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Rollback Failed: {snapshot.KeyName} - {ex.Message}", "UndoManager", "Failed");
            return WinCarePro.Core.Models.OperationResult.Fail($"Rollback failed: {ex.Message}", ex);
        }
    }

    public List<StateSnapshotEntry> GetRecentSnapshots(string? category = null)
    {
        return DbManager.GetSnapshots(category);
    }
}

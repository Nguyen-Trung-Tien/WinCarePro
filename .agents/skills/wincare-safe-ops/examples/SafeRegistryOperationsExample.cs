using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines.Backup;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.SafeOps;

/// <summary>
/// Reference example demonstrating pre-mutation registry backup and SafeRegistryGuard.
/// </summary>
public sealed class SafeRegistryOperationsExample
{
    private readonly RegistryBackupEngine _backupEngine;

    public SafeRegistryOperationsExample(RegistryBackupEngine backupEngine)
    {
        _backupEngine = backupEngine;
    }

    public async Task<OperationResult<bool>> DeleteRegistrySubKeySafelyAsync(
        RegistryHive hive,
        string subKeyPath,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            // 1. Guard check: Must be at least 3 levels deep, not a root hive or core system key
            if (!SafeRegistryGuard.IsSafeToDeleteKey(hive, subKeyPath))
            {
                AppLogger.LogWarning($"[SafeRegistryGuard] Attempted deletion of protected key: {hive}\\{subKeyPath}");
                return OperationResult<bool>.Fail($"Registry key '{hive}\\{subKeyPath}' is protected by SafeRegistryGuard.");
            }

            // 2. Pre-mutation backup: ALWAYS create a .reg backup prior to mutating
            var backupResult = await _backupEngine.BackupKeyAsync(hive, subKeyPath, ct);
            if (!backupResult.Success)
            {
                AppLogger.LogError($"[RegistryBackup] Pre-mutation backup failed for {subKeyPath}. Aborting.");
                return OperationResult<bool>.Fail("Registry backup failed. Modification aborted for safety.");
            }

            // 3. Perform mutation safely
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            baseKey.DeleteSubKeyTree(subKeyPath, throwOnMissingSubKey: false);

            return OperationResult<bool>.Ok(true, $"Registry key deleted safely. Restore point: {backupResult.Data}");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<bool>.Fail($"Registry mutation failed: {ex.Message}");
        }
    }
}


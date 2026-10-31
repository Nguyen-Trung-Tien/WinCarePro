using System;
using System.IO;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.SafeOps;

/// <summary>
/// Reference example demonstrating mandatory SafePathGuard file deletion.
/// </summary>
public static class SafeFileOperationsExample
{
    public static OperationResult<bool> DeleteFileSafely(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return OperationResult<bool>.Fail("Target path cannot be null or empty.");
            }

            // Inviolable Rule: Verify safety BEFORE touching the filesystem
            if (!SafePathGuard.IsSafeToDelete(filePath))
            {
                AppLogger.LogWarning($"[SafePathGuard] Blocked deletion attempt on protected path: {filePath}");
                return OperationResult<bool>.Fail($"Operation rejected: '{filePath}' is protected.");
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return OperationResult<bool>.Ok(true, $"File deleted safely: {filePath}");
            }

            return OperationResult<bool>.Ok(false, "File did not exist.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<bool>.Fail($"File deletion failed: {ex.Message}");
        }
    }
}


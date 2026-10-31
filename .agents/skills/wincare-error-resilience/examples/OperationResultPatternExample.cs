using System;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.ErrorResilience;

/// <summary>
/// Reference example demonstrating the OperationResult<T> pattern in WinCare Pro.
/// Invariants:
/// - Methods that can fail MUST return OperationResult or OperationResult<T>.
/// - NEVER return null or throw unhandled exceptions across architectural boundaries.
/// </summary>
public static class OperationResultPatternExample
{
    public static OperationResult<int> CalculateAvailableMemoryMegabytes(long totalBytes, long usedBytes)
    {
        if (totalBytes <= 0)
        {
            return OperationResult<int>.Fail("Invalid system capacity: total memory must be positive.");
        }

        if (usedBytes < 0 || usedBytes > totalBytes)
        {
            return OperationResult<int>.Fail("Memory metrics inconsistency: used memory exceeds total memory.");
        }

        long availableBytes = totalBytes - usedBytes;
        int availableMb = (int)(availableBytes / (1024 * 1024));

        return OperationResult<int>.Ok(availableMb, "Available memory calculated successfully.");
    }

    public static OperationResult SafeExecute(Action action, string actionName)
    {
        try
        {
            action();
            return OperationResult.Ok($"{actionName} executed successfully.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult.Fail($"{actionName} failed: {ex.Message}");
        }
    }
}


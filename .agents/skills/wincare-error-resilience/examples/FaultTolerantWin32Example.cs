using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.ErrorResilience;

/// <summary>
/// Reference example demonstrating graceful fault tolerance for Win32 and WMI errors.
/// Invariants:
/// - NEVER let Win32 faults crash the application.
/// - Log exceptions to CrashLogger.
/// - Fall back to safe defaults or return OperationResult.Fail.
/// </summary>
public static class FaultTolerantWin32Example
{
    public static OperationResult<long> QuerySystemCommitLimit()
    {
        try
        {
            // Simulated native call or WMI query
            bool nativeSuccess = true;
            if (!nativeSuccess)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }

            long simulatedCommitLimitBytes = 16L * 1024 * 1024 * 1024;
            return OperationResult<long>.Ok(simulatedCommitLimitBytes);
        }
        catch (Win32Exception w32Ex)
        {
            CrashLogger.LogError(w32Ex);
            return OperationResult<long>.Fail($"Win32 query failed with error code: {w32Ex.NativeErrorCode}");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<long>.Fail($"System query failed: {ex.Message}");
        }
    }
}


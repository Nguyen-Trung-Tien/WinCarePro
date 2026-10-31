using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.SafeOps;

/// <summary>
/// Reference example demonstrating zero command injection via ProcessRunner.
/// NEVER concatenate shell strings (e.g. cmd.exe /c "ping " + input).
/// ALWAYS pass arguments via ArgumentList and sanitize input.
/// </summary>
public static class SafeProcessExecutionExample
{
    public static async Task<OperationResult<string>> ExecuteSystemDiagnosticsAsync(
        string targetHost,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            // 1. Sanitize user/external input
            string sanitizedHost = InputSanitizer.Sanitize(targetHost);
            if (string.IsNullOrWhiteSpace(sanitizedHost))
            {
                return OperationResult<string>.Fail("Host parameter is invalid.");
            }

            // 2. Build ArgumentList array
            var arguments = new[] { "-n", "2", sanitizedHost };

            // 3. Execute directly without cmd.exe or PowerShell shell wrapper
            var processResult = await ProcessRunner.RunAsync("ping.exe", arguments, ct);

            if (!processResult.Success)
            {
                return OperationResult<string>.Fail($"Command execution failed: {processResult.StandardError}");
            }

            return OperationResult<string>.Ok(processResult.StandardOutput, "Command executed successfully.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<string>.Fail($"Process execution failed: {ex.Message}");
        }
    }
}


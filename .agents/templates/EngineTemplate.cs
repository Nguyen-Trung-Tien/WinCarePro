using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Engines;

/// <summary>
/// Canonical production engine template for WinCare Pro.
/// Adheres to 4-tier architecture: 100% independent from WinUI types, receives CancellationToken, wraps in OperationResult.
/// </summary>
public class TemplateEngine
{
    private readonly CrashLogger _crashLogger;

    public TemplateEngine(CrashLogger crashLogger)
    {
        _crashLogger = crashLogger ?? throw new ArgumentNullException(nameof(crashLogger));
    }

    /// <summary>
    /// Executes the primary engine operation with safety guards and cancellation support.
    /// </summary>
    public async Task<OperationResult<string>> ExecuteSafeOperationAsync(string targetPath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            // 1. Validate inputs
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return OperationResult<string>.Fail("Target path cannot be empty.");
            }

            // 2. SafePathGuard check (If file operations are involved)
            if (!SafePathGuard.IsSafeToDelete(targetPath))
            {
                AppLogger.LogWarning($"[TemplateEngine] Guard blocked operation on: {targetPath}");
                return OperationResult<string>.Fail($"Path '{targetPath}' is protected by SafePathGuard.");
            }

            // 3. Perform business logic asynchronously
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            AppLogger.LogInformation($"[TemplateEngine] Successfully processed: {targetPath}");
            return OperationResult<string>.Ok("Operation completed successfully.");
        }
        catch (OperationCanceledException)
        {
            AppLogger.LogInformation("[TemplateEngine] Operation was cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
            _crashLogger.LogError(ex, "TemplateEngine.ExecuteSafeOperationAsync");
            return OperationResult<string>.Fail(ex.Message);
        }
    }
}

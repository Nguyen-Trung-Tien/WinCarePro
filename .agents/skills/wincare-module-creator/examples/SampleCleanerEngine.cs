using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Engines.Examples;

/// <summary>
/// Reference implementation of a business logic Engine demonstrating 4-tier standards:
/// - Zero WinUI types referenced
/// - SafePathGuard integration
/// - CancellationToken propagation
/// - OperationResult return pattern
/// </summary>
public sealed class SampleCleanerEngine
{
    public SampleCleanerEngine()
    {
    }

    public async Task<OperationResult<int>> CleanTempFolderAsync(string directoryPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return OperationResult<int>.Fail("Directory path cannot be empty.");
        }

        if (!Directory.Exists(directoryPath))
        {
            return OperationResult<int>.Fail($"Directory does not exist: {directoryPath}");
        }

        int deletedCount = 0;

        try
        {
            string[] files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                if (SafePathGuard.IsSafeToDelete(file))
                {
                    File.Delete(file);
                    deletedCount++;
                }
                else
                {
                    AppLogger.LogWarning($"[SampleCleanerEngine] Blocked unsafe file: {file}");
                }

                await Task.Yield();
            }

            return OperationResult<int>.Ok(deletedCount, $"Deleted {deletedCount} files safely.");
        }
        catch (OperationCanceledException)
        {
            return OperationResult<int>.Fail("Cleaning operation cancelled by user.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            return OperationResult<int>.Fail($"Failed during directory clean: {ex.Message}");
        }
    }
}


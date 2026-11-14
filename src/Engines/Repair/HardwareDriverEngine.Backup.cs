using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Engines;

public partial class HardwareDriverEngine
{
    /// <summary>
    /// Exports and backs up all third-party and OEM device drivers using pnputil.exe /export-driver or dism.exe.
    /// </summary>
    public async Task<DriverBackupResult> BackupThirdPartyDriversAsync(
        string? customDestinationPath = null,
        IProgress<int>? progress = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var result = new DriverBackupResult();
        try
        {
            progress?.Report(10);
            string backupDir = customDestinationPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "WinCarePro_DriverBackups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            result.BackupPath = backupDir;
            progress?.Report(30);

            // Execute pnputil /export-driver * <backupDir>
            var args = new[] { "/export-driver", "*", backupDir };
            var procResult = await ProcessRunner.RunAsync(
                "pnputil.exe",
                args,
                TimeSpan.FromMinutes(3),
                null,
                null,
                null,
                cancellationToken);

            progress?.Report(80);

            int infCount = 0;
            try
            {
                infCount = Directory.GetFiles(backupDir, "*.inf", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("HardwareDriverEngine.BackupPnpUtil.CountInf", ex);
            }

            if (procResult.Success || infCount > 0)
            {
                result.Success = true;
                result.DriverCount = infCount;
                result.Message = $"Successfully backed up {infCount} device driver packages to {backupDir}";
            }
            else
            {
                // Fallback attempt with DISM online export
                var dismArgs = new[] { "/online", "/export-driver", $"/destination:{backupDir}" };
                var dismResult = await ProcessRunner.RunAsync("dism.exe", dismArgs, TimeSpan.FromMinutes(3), null, null, null, cancellationToken);
                try
                {
                    infCount = Directory.GetFiles(backupDir, "*.inf", SearchOption.AllDirectories).Length;
                }
                catch (Exception ex)
                {
                    CrashLogger.LogException("HardwareDriverEngine.BackupDism.CountInf", ex);
                }
                result.Success = infCount > 0 || dismResult.Success;
                result.DriverCount = infCount;
                result.Message = result.Success
                    ? $"Successfully backed up {infCount} device drivers via DISM."
                    : $"Backup completed with exit code {procResult.ExitCode}.";
            }

            progress?.Report(100);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Driver backup failed: " + ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Restores/adds drivers from a backup folder using pnputil.exe /add-driver <path>\*.inf /subdirs /install
    /// </summary>
    public async Task<DriverBackupResult> RestoreDriversFromBackupAsync(
        string backupSourceDir,
        IProgress<int>? progress = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var result = new DriverBackupResult { BackupPath = backupSourceDir };
        try
        {
            if (!Directory.Exists(backupSourceDir))
            {
                result.Success = false;
                result.Message = "Driver backup directory does not exist.";
                return result;
            }

            progress?.Report(15);
            int infCount = Directory.GetFiles(backupSourceDir, "*.inf", SearchOption.AllDirectories).Length;
            if (infCount == 0)
            {
                result.Success = false;
                result.Message = "No .inf driver packages found in selected directory.";
                return result;
            }

            progress?.Report(40);
            string infSearchPattern = Path.Combine(backupSourceDir, "*.inf");
            var args = new[] { "/add-driver", infSearchPattern, "/subdirs", "/install" };
            
            var procResult = await ProcessRunner.RunAsync(
                "pnputil.exe",
                args,
                TimeSpan.FromMinutes(5),
                null,
                null,
                null,
                cancellationToken);

            progress?.Report(90);
            result.Success = procResult.Success;
            result.DriverCount = infCount;
            result.Message = procResult.Success
                ? $"Successfully re-installed/staged {infCount} driver packages."
                : $"Driver restoration finished with warnings (Exit code: {procResult.ExitCode}).";

            progress?.Report(100);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Driver restore failed: " + ex.Message;
        }

        return result;
    }
}

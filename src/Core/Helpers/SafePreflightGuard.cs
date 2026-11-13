using System;
using System.IO;
using System.Linq;
using WinCarePro.Core.Models;
using WinCarePro.Services.Implementations;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Pre-flight validation result containing diagnostic safety checklist state.
/// </summary>
public record PreflightCheckResult(
    bool CanProceed,
    bool IsAdmin,
    long FreeSpaceBytes,
    string? WarningMessage = null,
    string? ActionSuggestion = null);

/// <summary>
/// Pre-flight verification helper to ensure system operations satisfy prerequisites
/// (Administrator privilege, minimum storage headroom, critical service protection)
/// before triggering potentially disruptive operations.
/// </summary>
public static class SafePreflightGuard
{
    private static readonly ServiceSafetyService _safetyService = new();

    /// <summary>
    /// Executes a standardized pre-flight check before heavy scanning or optimization.
    /// </summary>
    /// <param name="requireAdmin">Whether elevated privileges are strictly required.</param>
    /// <param name="minFreeDiskBytes">Minimum required free disk bytes on target drive (defaults to 100MB).</param>
    /// <param name="targetDriveLetter">Target drive letter to check (e.g., 'C').</param>
    public static PreflightCheckResult VerifyPrerequisites(
        bool requireAdmin = false,
        long minFreeDiskBytes = 104_857_600L, // 100 MB
        char targetDriveLetter = 'C')
    {
        bool isAdmin = ProcessRunner.IsRunningAsAdministrator();
        if (requireAdmin && !isAdmin)
        {
            return new PreflightCheckResult(
                CanProceed: false,
                IsAdmin: false,
                FreeSpaceBytes: 0,
                WarningMessage: "Administrator privileges required to execute this operation safely.",
                ActionSuggestion: "Relaunch WinCare Pro Suite as Administrator to enable full system actions.");
        }

        long freeBytes = 0;
        try
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.Name.StartsWith(targetDriveLetter.ToString(), StringComparison.OrdinalIgnoreCase));
            if (drive != null)
            {
                freeBytes = drive.AvailableFreeSpace;
                if (freeBytes < minFreeDiskBytes)
                {
                    return new PreflightCheckResult(
                        CanProceed: false,
                        IsAdmin: isAdmin,
                        FreeSpaceBytes: freeBytes,
                        WarningMessage: $"Low disk space detected on {targetDriveLetter}: drive ({freeBytes / (1024 * 1024)} MB available).",
                        ActionSuggestion: "Free up storage headroom or purge junk files first to prevent I/O disk thrashing.");
                }
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("SafePreflightGuard.VerifyPrerequisites.Disk", ex);
        }

        return new PreflightCheckResult(
            CanProceed: true,
            IsAdmin: isAdmin,
            FreeSpaceBytes: freeBytes);
    }

    /// <summary>
    /// Verifies if a service is safe to modify or stop against ServiceSafetyService.
    /// </summary>
    public static bool IsSafeToTouchService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return !_safetyService.IsCriticalService(serviceName) && !_safetyService.IsSecurityService(serviceName);
    }
}


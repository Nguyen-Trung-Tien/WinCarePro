using System;
using System.Diagnostics;
using System.IO;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Infrastructure.Security;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Centralized, zero-trust helper for launching external URLs, folders, and safe shell tools.
/// </summary>
public static class ShellLauncher
{
    /// <summary>
    /// Opens an external HTTP/HTTPS URL in the default browser after validating it via <see cref="InputSanitizer.IsSafeUri"/>.
    /// </summary>
    public static bool OpenUrl(string url, string context = "ShellLauncher.OpenUrl")
    {
        if (string.IsNullOrWhiteSpace(url) || !InputSanitizer.IsSafeUri(url))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(context, ex);
            return false;
        }
    }

    /// <summary>
    /// Opens a validated local directory or file in Windows Explorer.
    /// </summary>
    public static bool OpenInExplorer(string path, bool selectFile = false, string context = "ShellLauncher.OpenInExplorer")
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            string explorerPath = ProcessRunner.ResolveSafeExecutablePath("explorer.exe") ?? "explorer.exe";
            var psi = new ProcessStartInfo
            {
                FileName = explorerPath,
                UseShellExecute = true
            };

            if (selectFile && File.Exists(path))
            {
                psi.ArgumentList.Add("/select,");
                psi.ArgumentList.Add(path);
            }
            else if (Directory.Exists(path))
            {
                psi.ArgumentList.Add(path);
            }
            else
            {
                return false;
            }

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(context, ex);
            return false;
        }
    }

    /// <summary>
    /// Opens Registry Editor safely using a resolved system executable path.
    /// </summary>
    public static bool OpenRegedit(string context = "ShellLauncher.OpenRegedit")
    {
        try
        {
            string regeditPath = ProcessRunner.ResolveSafeExecutablePath("regedit.exe") ?? "regedit.exe";
            Process.Start(new ProcessStartInfo(regeditPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.SystemDirectory
            });
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(context, ex);
            return false;
        }
    }
}

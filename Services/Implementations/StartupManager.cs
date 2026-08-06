using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Services.Implementations;

public static class StartupManager
{
    private const string AppName = "WinCarePro";
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            // 1. Check HKCU Registry Run key
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, false);
            if (key != null)
            {
                var val = key.GetValue(AppName)?.ToString();
                if (!string.IsNullOrEmpty(val)) return true;
            }

            // 2. Check Task Scheduler if running elevated or query via schtasks
            if (IsTaskSchedulerEnabled())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("StartupManager.IsAutoStartEnabled", ex);
        }

        return false;
    }

    public static bool SetAutoStart(bool enable)
    {
        bool success = false;
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinCarePro.exe");
            }

            string targetCommand = $"\"{exePath}\" /background";

            if (enable)
            {
                // Set HKCU Registry Run key first for standard execution
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue(AppName, targetCommand, RegistryValueKind.String);
                        success = true;
                    }
                }

                // If Administrator, also create/enable Scheduled Task for bypass UAC on boot
                if (IsAdministrator())
                {
                    CreateOrUpdateTaskScheduler(targetCommand);
                }
            }
            else
            {
                // Delete HKCU Registry key
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (key != null && key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }

                // Delete Scheduled Task if present
                RemoveTaskScheduler();
                success = true;
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("StartupManager.SetAutoStart", ex);
            success = false;
        }

        return success;
    }

    private static bool IsTaskSchedulerEnabled()
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks", $"/Query /TN \"WinCareProAutoStart\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
        }
        catch { }
        return false;
    }

    private static void CreateOrUpdateTaskScheduler(string targetCommand)
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            string args = "/background";

            var psi = new ProcessStartInfo("schtasks", $"/Create /TN \"WinCareProAutoStart\" /TR \"\\\"{exePath}\\\" {args}\" /SC ONLOGON /RL HIGHEST /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("StartupManager.CreateTaskScheduler", ex);
        }
    }

    private static void RemoveTaskScheduler()
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks", "/Delete /TN \"WinCareProAutoStart\" /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }
        catch { }
    }
}

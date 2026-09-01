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

    private const string AutoStartTaskName = "WinCareProAutoStart";

    private static bool IsTaskSchedulerEnabled()
    {
        try
        {
            using var ts = new Microsoft.Win32.TaskScheduler.TaskService();
            var task = ts.GetTask(AutoStartTaskName);
            return task != null && task.Enabled;
        }
        catch { }
        return false;
    }

    private static void CreateOrUpdateTaskScheduler(string targetCommand)
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinCarePro.exe");
            }
            string args = "/background";

            using var ts = new Microsoft.Win32.TaskScheduler.TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = "WinCare Pro Automatic Startup Task";
            td.Principal.RunLevel = Microsoft.Win32.TaskScheduler.TaskRunLevel.Highest;
            td.Triggers.Add(new Microsoft.Win32.TaskScheduler.LogonTrigger());
            td.Actions.Add(new Microsoft.Win32.TaskScheduler.ExecAction(exePath, args, null));
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            ts.RootFolder.RegisterTaskDefinition(AutoStartTaskName, td);
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
            using var ts = new Microsoft.Win32.TaskScheduler.TaskService();
            ts.RootFolder.DeleteTask(AutoStartTaskName, false);
        }
        catch { }
    }
}

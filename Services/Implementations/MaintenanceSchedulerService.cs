using System;
using Microsoft.Win32.TaskScheduler;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class MaintenanceSchedulerService : IMaintenanceSchedulerService
{
    public bool ScheduleTask(string taskName, string exePath, string arguments, DateTime startTime, string frequency)
    {
        try
        {
            using var ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = "Scheduled maintenance for WinCare Pro";

            Trigger trigger = frequency.ToUpperInvariant() switch
            {
                "DAILY" => new DailyTrigger { StartBoundary = startTime, DaysInterval = 1 },
                "WEEKLY" => new WeeklyTrigger { StartBoundary = startTime, WeeksInterval = 1 },
                _ => new TimeTrigger { StartBoundary = startTime }
            };
            td.Triggers.Add(trigger);

            td.Actions.Add(new ExecAction(exePath, arguments, null));
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.RunOnlyIfNetworkAvailable = false;
            td.Settings.StartWhenAvailable = true;

            ts.RootFolder.RegisterTaskDefinition(taskName, td);
            Database.DbManager.LogAction($"Scheduled task: {taskName}", "Scheduler Service", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Failed to schedule task '{taskName}': {ex.Message}", "Scheduler Service", "Failed");
            return false;
        }
    }

    public bool DeleteTask(string taskName)
    {
        try
        {
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(taskName, false);
            Database.DbManager.LogAction($"Deleted scheduled task: {taskName}", "Scheduler Service", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Failed to delete task '{taskName}': {ex.Message}", "Scheduler Service", "Failed");
            return false;
        }
    }

    public bool IsTaskScheduled(string taskName)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(taskName);
            return task != null;
        }
        catch
        {
            return false;
        }
    }
}

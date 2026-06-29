using System;

namespace WinCarePro.Services.Contracts;

public interface IMaintenanceSchedulerService
{
    bool ScheduleTask(string taskName, string exePath, string arguments, DateTime startTime, string frequency);
    bool DeleteTask(string taskName);
    bool IsTaskScheduled(string taskName);
}

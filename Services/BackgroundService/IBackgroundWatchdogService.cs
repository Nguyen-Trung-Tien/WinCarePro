using System;
using System.Threading.Tasks;

namespace WinCarePro.Services.Contracts;

/// <summary>
/// Service contract for low-power background watchdog monitoring and automated system maintenance.
/// </summary>
public interface IBackgroundWatchdogService : IDisposable
{
    /// <summary>
    /// Starts the background watchdog monitoring loop.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the background watchdog monitoring loop.
    /// </summary>
    void Stop();

    /// <summary>
    /// Manually triggers an immediate health and resource check in the background.
    /// </summary>
    Task TriggerImmediateCheckAsync();

    /// <summary>
    /// Gets whether the background watchdog service is actively running.
    /// </summary>
    bool IsRunning { get; }
}

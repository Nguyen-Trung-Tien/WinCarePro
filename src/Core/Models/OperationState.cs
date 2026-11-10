using System;

namespace WinCarePro.Core.Models;

/// <summary>
/// Represents the formal lifecycle state machine for background operations,
/// scans, repairs, cleanups, and optimizations across all WinCare Pro modules.
/// </summary>
public enum OperationState
{
    /// <summary>
    /// System is idle, ready to initiate a new operation.
    /// </summary>
    Idle,

    /// <summary>
    /// Preparing environment, verifying privileges, or loading configurations.
    /// </summary>
    Preparing,

    /// <summary>
    /// Operation is actively executing background analysis or repair tasks.
    /// </summary>
    Running,

    /// <summary>
    /// Cancellation has been requested; waiting for graceful abort.
    /// </summary>
    Cancelling,

    /// <summary>
    /// Operation has finished successfully or with partial success.
    /// </summary>
    Completed,

    /// <summary>
    /// Operation encountered an error or failed to complete.
    /// </summary>
    Failed
}

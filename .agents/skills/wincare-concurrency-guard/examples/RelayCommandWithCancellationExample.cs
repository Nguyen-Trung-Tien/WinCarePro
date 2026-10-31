using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Examples.Concurrency;

/// <summary>
/// Reference example demonstrating cancellation and state hygiene in RelayCommands.
/// Invariants:
/// - Use [RelayCommand(IncludeCancelCommand = true)].
/// - Accept CancellationToken ct.
/// - ALWAYS reset IsBusy = false and OperationState in finally.
/// </summary>
public partial class RelayCommandWithCancellationExample : ViewModelBase
{
    [ObservableProperty]
    private string _executionStateText = "Idle";

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ExecuteHeavyScanAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            OperationState = OperationState.Running;
            RunOnUI(() => ExecutionStateText = "Scanning deep system structures...");

            for (int step = 1; step <= 10; step++)
            {
                // Cooperative cancellation check
                ct.ThrowIfCancellationRequested();

                await Task.Delay(100, ct);

                int progressPercent = step * 10;
                RunOnUI(() => ExecutionStateText = $"Scan in progress: {progressPercent}%");
            }

            RunOnUI(() => ExecutionStateText = "Scan finished successfully.");
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() => ExecutionStateText = "Scan cancelled by user.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            RunOnUI(() => ExecutionStateText = $"Scan failed: {ex.Message}");
        }
        finally
        {
            // Guarantees UI does not get permanently stuck in busy state
            IsBusy = false;
            OperationState = OperationState.Idle;
        }
    }
}


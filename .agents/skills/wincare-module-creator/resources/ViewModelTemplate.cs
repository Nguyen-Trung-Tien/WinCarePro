using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Modules.Templates;

/// <summary>
/// Canonical template for a WinCare Pro ViewModel.
/// INVARIANTS:
/// 1. Inherits ViewModelBase.
/// 2. Wraps 100% of UI mutations and ObservableCollection mutations in RunOnUI(() => { ... }).
/// 3. Resets IsBusy = false and OperationState in finally blocks.
/// 4. Dispatches long-running tasks via RelayCommand with cancellation support.
/// 5. Uses WeakReferenceMessenger for cross-module communication.
/// </summary>
public partial class ViewModelTemplate : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    public ObservableCollection<string> Items { get; } = new();

    public ViewModelTemplate()
    {
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartWorkflowAsync(CancellationToken ct)
    {
        try
        {
            IsBusy = true;
            OperationState = OperationState.Running;
            RunOnUI(() => StatusMessage = "Operation starting...");

            // Simulate progress or engine call
            for (int i = 0; i <= 100; i += 20)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(50, ct);

                int currentProgress = i;
                RunOnUI(() =>
                {
                    ProgressValue = currentProgress;
                    StatusMessage = $"Progress: {currentProgress}%";
                    Items.Add($"Step completed at {currentProgress}%");
                });
            }

            RunOnUI(() => StatusMessage = "Operation completed successfully.");
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() => StatusMessage = "Operation cancelled by user.");
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex);
            RunOnUI(() => StatusMessage = $"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        }
    }
}


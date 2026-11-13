using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

/// <summary>
/// Fine-grained async task progress report payload.
/// </summary>
public record TaskProgressReport(
    double? Percentage = null,
    string? StatusText = null,
    string? TargetItem = null,
    bool? IsIndeterminate = null);

/// <summary>
/// Base ViewModel providing centralized async progress, cancellation, and execution feedback
/// across WinCare Pro v6 hubs.
/// </summary>
public abstract partial class AsyncTaskViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _currentStatusText = string.Empty;

    [ObservableProperty]
    private string _currentTargetItem = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool CanCancel => IsBusy && _cts != null && !_cts.IsCancellationRequested;

    protected CancellationTokenSource? _cts;
    public CancellationToken CurrentCancellationToken => _cts?.Token ?? CancellationToken.None;

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanCancel));
        if (value)
        {
            SetOperationState(OperationState.Running);
        }
        else if (CurrentOperationState == OperationState.Running || CurrentOperationState == OperationState.Cancelling)
        {
            SetOperationState(OperationState.Idle);
        }
        OnBusyChanged(value);
    }

    /// <summary>
    /// Extension hook invoked whenever IsBusy changes.
    /// </summary>
    protected virtual void OnBusyChanged(bool isBusy) { }

    [RelayCommand]
    public virtual void CancelCurrentTask()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            try
            {
                SetOperationState(OperationState.Cancelling);
                CurrentStatusText = "Cancelling...".T();
                OnPropertyChanged(nameof(CanCancel));
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("AsyncTaskViewModelBase.CancelCurrentTask", ex);
            }
        }
    }

    /// <summary>
    /// Executes an asynchronous long-running task with automated state lifecycle management,
    /// safe cancellation, and progress dispatching to the UI thread.
    /// </summary>
    protected async Task<bool> ExecuteAsyncTaskAsync(
        string initialStatus,
        Func<CancellationToken, IProgress<TaskProgressReport>, Task> taskFunc,
        bool isIndeterminate = false)
    {
        if (IsBusy) return false;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        await RunOnUIActionAsync(() =>
        {
            IsBusy = true;
            IsIndeterminate = isIndeterminate;
            ProgressPercentage = 0;
            CurrentStatusText = initialStatus;
            CurrentTargetItem = string.Empty;
            SetOperationState(OperationState.Running);
            OnPropertyChanged(nameof(CanCancel));
        });

        var progress = new Progress<TaskProgressReport>(report =>
        {
            RunOnUI(() =>
            {
                if (report.Percentage.HasValue)
                {
                    ProgressPercentage = Math.Clamp(report.Percentage.Value, 0, 100);
                }
                if (!string.IsNullOrEmpty(report.StatusText))
                {
                    CurrentStatusText = report.StatusText;
                }
                if (report.TargetItem != null)
                {
                    CurrentTargetItem = report.TargetItem;
                }
                if (report.IsIndeterminate.HasValue)
                {
                    IsIndeterminate = report.IsIndeterminate.Value;
                }
            });
        });

        try
        {
            await taskFunc(_cts.Token, progress);
            await RunOnUIActionAsync(() =>
            {
                SetOperationState(OperationState.Completed);
                ProgressPercentage = 100;
            });
            return true;
        }
        catch (OperationCanceledException)
        {
            await RunOnUIActionAsync(() =>
            {
                CurrentStatusText = "Operation cancelled by user.".T();
                CurrentTargetItem = string.Empty;
                SetOperationState(OperationState.Idle);
            });
            return false;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException($"AsyncTaskViewModelBase.{GetType().Name}", ex);
            await RunOnUIActionAsync(() =>
            {
                CurrentStatusText = $"Fault: {ex.Message}";
                SetOperationState(OperationState.Failed);
            });
            return false;
        }
        finally
        {
            await RunOnUIActionAsync(() =>
            {
                IsBusy = false;
                IsIndeterminate = false;
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(CanCancel));
            });

            _cts?.Dispose();
            _cts = null;
        }
    }
}

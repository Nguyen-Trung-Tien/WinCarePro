using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCarePro.Core.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected DispatcherQueue? DispatcherQueueInstance { get; set; } = App.MainDispatcherQueue;

    private OperationState _currentOperationState = OperationState.Idle;
    public OperationState CurrentOperationState
    {
        get => _currentOperationState;
        set
        {
            if (_currentOperationState != value)
            {
                SetPropertyOnUI(() => _currentOperationState, v => _currentOperationState = v, value);
                OnPropertyChanged(nameof(OperationStateText));
                OnPropertyChanged(nameof(IsOperationActive));
                OnPropertyChanged(nameof(CanStartOperation));
            }
        }
    }

    public string OperationStateText => CurrentOperationState switch
    {
        OperationState.Idle => "Idle".T(),
        OperationState.Preparing => "Preparing".T(),
        OperationState.Running => "Running".T(),
        OperationState.Cancelling => "Cancelling".T(),
        OperationState.Completed => "Completed".T(),
        OperationState.Failed => "Failed".T(),
        _ => "Idle".T()
    };

    public bool IsOperationActive => CurrentOperationState is OperationState.Preparing or OperationState.Running or OperationState.Cancelling;
    public bool CanStartOperation => CurrentOperationState is OperationState.Idle or OperationState.Completed or OperationState.Failed;

    public void SetOperationState(OperationState state)
    {
        CurrentOperationState = state;
    }


    public static DispatcherQueue? SafeGetDispatcherQueue()
    {
        try
        {
            return DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue;
        }
        catch
        {
            return App.MainDispatcherQueue;
        }
    }

    protected void SetPropertyOnUI<T>(Func<T> getter, Action<T> setter, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (Equals(getter(), value)) return;

        var dispatcher = DispatcherQueueInstance ?? App.MainDispatcherQueue;
        if (dispatcher != null && !dispatcher.HasThreadAccess)
        {
            T localValue = value;
            try
            {
                dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        if (!Equals(getter(), localValue))
                        {
                            setter(localValue);
                            OnPropertyChanged(propertyName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Infrastructure.Logging.CrashLogger.LogException($"ViewModelBase.SetPropertyOnUI({propertyName})", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException($"ViewModelBase.DispatcherEnqueue({propertyName})", ex);
            }
        }
        else
        {
            try
            {
                setter(value);
                OnPropertyChanged(propertyName);
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException($"ViewModelBase.SetPropertyDirect({propertyName})", ex);
            }
        }
    }

    protected Task<T> RunOnUIAsync<T>(Func<Task<T>> action)
    {
        var dispatcher = DispatcherQueueInstance ?? App.MainDispatcherQueue;
        if (dispatcher == null || dispatcher.HasThreadAccess)
        {
            return action();
        }

        var tcs = new TaskCompletionSource<T>();
        bool queued = dispatcher.TryEnqueue(async () =>
        {
            try
            {
                var result = await action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!queued)
        {
            tcs.SetException(new InvalidOperationException("Failed to queue operation on UI thread."));
        }

        return tcs.Task;
    }

    protected Task RunOnUIAsync(Func<Task> action)
    {
        var dispatcher = DispatcherQueueInstance ?? App.MainDispatcherQueue;
        if (dispatcher == null || dispatcher.HasThreadAccess)
        {
            return action();
        }

        var tcs = new TaskCompletionSource();
        bool queued = dispatcher.TryEnqueue(async () =>
        {
            try
            {
                await action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!queued)
        {
            tcs.SetException(new InvalidOperationException("Failed to queue operation on UI thread."));
        }

        return tcs.Task;
    }

    protected Task RunOnUIActionAsync(Action action)
    {
        var dispatcher = DispatcherQueueInstance ?? App.MainDispatcherQueue;
        if (dispatcher == null || dispatcher.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        bool queued = dispatcher.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!queued)
        {
            tcs.SetException(new InvalidOperationException("Failed to queue operation on UI thread."));
        }

        return tcs.Task;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCarePro.Core.Models;
using WinCarePro.Presentation.Controls;
using WinCarePro.ViewModels;
using Xunit;

namespace WinCarePro.Tests.Presentation;

public class TestAsyncTaskViewModel : AsyncTaskViewModelBase
{
    public bool BusyHookTriggered { get; private set; }

    protected override void OnBusyChanged(bool isBusy)
    {
        base.OnBusyChanged(isBusy);
        BusyHookTriggered = true;
    }

    public Task<bool> RunNormalTaskAsync(int stepCount)
    {
        return ExecuteAsyncTaskAsync("Running test task", async (token, progress) =>
        {
            for (int i = 1; i <= stepCount; i++)
            {
                token.ThrowIfCancellationRequested();
                double pct = (i / (double)stepCount) * 100.0;
                progress.Report(new TaskProgressReport(
                    Percentage: pct,
                    StatusText: $"Step {i} of {stepCount}",
                    TargetItem: $"Item_{i}"));
            }
            await Task.Yield();
        });
    }

    public Task<bool> RunCancellableTaskAsync(TaskCompletionSource startSignal, TaskCompletionSource abortSignal)
    {
        return ExecuteAsyncTaskAsync("Waiting to cancel", async (token, progress) =>
        {
            startSignal.TrySetResult();
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10, CancellationToken.None);
            }
            token.ThrowIfCancellationRequested();
        });
    }

    public Task<bool> RunFaultyTaskAsync()
    {
        return ExecuteAsyncTaskAsync("Faulty task", (token, progress) =>
        {
            throw new InvalidOperationException("Simulated hardware error");
        });
    }
}

public class AsyncTaskProgressFeedbackTests
{
    [Fact]
    public void InitialState_ShouldBeIdleAndNotBusy()
    {
        var vm = new TestAsyncTaskViewModel();

        Assert.False(vm.IsBusy);
        Assert.True(vm.IsNotBusy);
        Assert.False(vm.CanCancel);
        Assert.Equal(0.0, vm.ProgressPercentage);
        Assert.Equal(string.Empty, vm.CurrentStatusText);
        Assert.Equal(string.Empty, vm.CurrentTargetItem);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public async Task ExecuteAsyncTaskAsync_SuccessfulRun_UpdatesStateAndCompletes()
    {
        var vm = new TestAsyncTaskViewModel();

        bool success = await vm.RunNormalTaskAsync(4);

        Assert.True(success);
        Assert.False(vm.IsBusy);
        Assert.True(vm.IsNotBusy);
        Assert.Equal(100.0, vm.ProgressPercentage);
        Assert.Equal(OperationState.Completed, vm.CurrentOperationState);
        Assert.True(vm.BusyHookTriggered);
    }

    [Fact]
    public async Task ExecuteAsyncTaskAsync_Cancellation_HandlesGracefully()
    {
        var vm = new TestAsyncTaskViewModel();
        var started = new TaskCompletionSource();
        var aborted = new TaskCompletionSource();

        var runTask = vm.RunCancellableTaskAsync(started, aborted);
        await started.Task;

        Assert.True(vm.IsBusy);
        Assert.True(vm.CanCancel);

        // Cancel the task
        vm.CancelCurrentTask();

        bool success = await runTask;

        Assert.False(success);
        Assert.False(vm.IsBusy);
        Assert.True(vm.IsNotBusy);
        Assert.False(vm.CanCancel);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public async Task ExecuteAsyncTaskAsync_Exception_ResetsIsBusyAndFails()
    {
        var vm = new TestAsyncTaskViewModel();

        bool success = await vm.RunFaultyTaskAsync();

        Assert.False(success);
        Assert.False(vm.IsBusy);
        Assert.True(vm.IsNotBusy);
        Assert.Equal(OperationState.Failed, vm.CurrentOperationState);
        Assert.Contains("Simulated hardware error", vm.CurrentStatusText);
    }

    [Theory]
    [InlineData(0, "0%")]
    [InlineData(45.6, "45%")]
    [InlineData(100, "100%")]
    [InlineData(120, "100%")]
    [InlineData(-5, "0%")]
    public void ExecutionProgressBanner_FormatPercent_ClampsCorrectly(double input, string expected)
    {
        string formatted = ExecutionProgressBanner.FormatPercent(input);
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void ExecutionProgressBanner_DeterminateVisibility_BehavesCorrectly()
    {
        Assert.Equal(Visibility.Visible, ExecutionProgressBanner.DeterminateVisibility(false));
        Assert.Equal(Visibility.Collapsed, ExecutionProgressBanner.DeterminateVisibility(true));
    }

    [Theory]
    [InlineData(null, Visibility.Collapsed)]
    [InlineData("", Visibility.Collapsed)]
    [InlineData("   ", Visibility.Collapsed)]
    [InlineData(@"C:\Windows\Temp", Visibility.Visible)]
    public void ExecutionProgressBanner_HasTargetItemVisibility_BehavesCorrectly(string? item, Visibility expected)
    {
        Assert.Equal(expected, ExecutionProgressBanner.HasTargetItemVisibility(item));
    }
}


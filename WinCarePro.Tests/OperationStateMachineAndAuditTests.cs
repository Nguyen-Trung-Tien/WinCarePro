using System;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Services;
using WinCarePro.ViewModels;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class OperationStateMachineAndAuditTests
{
    private class TestViewModel : ViewModelBase
    {
        public void TestSetState(OperationState state) => SetOperationState(state);
    }

    [Fact]
    public void OperationState_StateTransitions_MaintainActiveFlags()
    {
        var vm = new TestViewModel();
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        Assert.False(vm.IsOperationActive);
        Assert.True(vm.CanStartOperation);

        vm.TestSetState(OperationState.Preparing);
        Assert.Equal(OperationState.Preparing, vm.CurrentOperationState);
        Assert.True(vm.IsOperationActive);
        Assert.False(vm.CanStartOperation);

        vm.TestSetState(OperationState.Running);
        Assert.Equal(OperationState.Running, vm.CurrentOperationState);
        Assert.True(vm.IsOperationActive);
        Assert.False(vm.CanStartOperation);

        vm.TestSetState(OperationState.Cancelling);
        Assert.Equal(OperationState.Cancelling, vm.CurrentOperationState);
        Assert.True(vm.IsOperationActive);
        Assert.False(vm.CanStartOperation);

        vm.TestSetState(OperationState.Completed);
        Assert.Equal(OperationState.Completed, vm.CurrentOperationState);
        Assert.False(vm.IsOperationActive);
        Assert.True(vm.CanStartOperation);

        vm.TestSetState(OperationState.Failed);
        Assert.Equal(OperationState.Failed, vm.CurrentOperationState);
        Assert.False(vm.IsOperationActive);
        Assert.True(vm.CanStartOperation);

        vm.TestSetState(OperationState.Idle);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        Assert.False(vm.IsOperationActive);
        Assert.True(vm.CanStartOperation);
    }

    [Fact]
    public void OperationState_Localization_TranslatesProperly()
    {
        var vm = new TestViewModel();
        var tm = TranslationManager.Instance;
        var prevLang = tm.CurrentLanguage;

        try
        {
            // English check
            tm.CurrentLanguage = AppLanguage.English;
            vm.TestSetState(OperationState.Idle);
            Assert.Equal("Idle", vm.OperationStateText);

            vm.TestSetState(OperationState.Preparing);
            Assert.Equal("Preparing", vm.OperationStateText);

            vm.TestSetState(OperationState.Running);
            Assert.Equal("Running", vm.OperationStateText);

            vm.TestSetState(OperationState.Cancelling);
            Assert.Equal("Cancelling", vm.OperationStateText);

            vm.TestSetState(OperationState.Completed);
            Assert.Equal("Completed", vm.OperationStateText);

            vm.TestSetState(OperationState.Failed);
            Assert.Equal("Failed", vm.OperationStateText);

            // Vietnamese check
            tm.CurrentLanguage = AppLanguage.Vietnamese;
            vm.TestSetState(OperationState.Idle);
            Assert.Equal("Chờ", vm.OperationStateText);

            vm.TestSetState(OperationState.Preparing);
            Assert.Equal("Đang chuẩn bị", vm.OperationStateText);

            vm.TestSetState(OperationState.Running);
            Assert.Equal("Đang Chạy", vm.OperationStateText);

            vm.TestSetState(OperationState.Cancelling);
            Assert.Equal("Đang hủy", vm.OperationStateText);

            vm.TestSetState(OperationState.Completed);
            Assert.Equal("Đã hoàn thành", vm.OperationStateText);

            vm.TestSetState(OperationState.Failed);
            Assert.Equal("Thất bại", vm.OperationStateText);
        }
        finally
        {
            tm.CurrentLanguage = AppLanguage.English;
        }
    }

    [Fact]
    public async Task ContextMenuEngine_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var engine = new ContextMenuEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await engine.ScanContextMenuItemsAsync(cts.Token);
        });
    }

    [Fact]
    public void UninstallEngine_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var engine = new UninstallEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
        {
            engine.ScanInstalledApps(cts.Token);
        });
    }

    [Fact]
    public async Task CrashLogger_ConcurrentAccess_DoesNotDeadlock()
    {
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                CrashLogger.LogMessage("TestCategory", $"Concurrent test message {index}");
                CrashLogger.LogException($"ConcurrentTest_{index}", new InvalidOperationException($"Test error {index}"));
            });
        }

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SecurityViewModel_CancelScan_RestoresIdleState()
    {
        var vm = new SecurityViewModel();
        vm.CancelScan();
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        Assert.False(vm.IsScanning);
    }

    [Fact]
    public void UpdaterViewModel_CancelOperations_RestoresIdleState()
    {
        var vm = new UpdaterViewModel();
        vm.CancelOperations();
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void StartupViewModel_Cleanup_SetsStateToIdle()
    {
        var vm = new StartupViewModel();
        vm.Cleanup();
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void RegistryViewModel_Cleanup_SetsStateToIdle()
    {
        var vm = new RegistryViewModel();
        vm.Cleanup();
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        Assert.False(vm.IsBusy);
    }
}

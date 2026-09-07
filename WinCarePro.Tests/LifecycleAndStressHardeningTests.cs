using System;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Services.Implementations;
using WinCarePro.Shared.Animations;
using WinCarePro.ViewModels;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class LifecycleAndStressHardeningTests
{
    [Fact]
    public void UninstallViewModel_CancelScan_RestoresIsBusyAndIdle()
    {
        var vm = new UninstallViewModel();
        vm.CancelScan();

        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void UninstallViewModel_Dispose_CleansUpAndResetsState()
    {
        var vm = new UninstallViewModel();
        vm.Dispose();

        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void SystemOptimizerViewModel_CancelAiScan_ResetsIsAiScanningAndState()
    {
        var vm = new SystemOptimizerViewModel();
        vm.CancelAiScan();

        Assert.False(vm.IsAiScanning);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void SystemOptimizerViewModel_Cleanup_ResetsAllFlags()
    {
        var vm = new SystemOptimizerViewModel();
        vm.Cleanup();

        Assert.False(vm.IsAiScanning);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsBoosting);
        Assert.False(vm.IsCleaningCache);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void ContextMenuViewModel_CancelScan_ResetsBusyAndIdle()
    {
        var vm = new ContextMenuViewModel();
        vm.CancelScan();

        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void RegistryViewModel_CancelScan_ResetsBusyAndIdle()
    {
        var vm = new RegistryViewModel();
        vm.CancelScan();

        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void ReducedMotionHelper_RespectsSettingsProfile()
    {
        var settings = SettingsService.Instance.CurrentSettings;
        bool originalSetting = settings.EnableAnimations;

        try
        {
            settings.EnableAnimations = false;
            Assert.False(ReducedMotionHelper.AreAnimationsEnabled);

            settings.EnableAnimations = true;
            // When setting is true, AreAnimationsEnabled returns Windows UI setting (which is true or false depending on host OS)
            // It should not crash or throw
            _ = ReducedMotionHelper.AreAnimationsEnabled;
        }
        finally
        {
            settings.EnableAnimations = originalSetting;
        }
    }

    [Fact]
    public void Animation3DHelper_StopAll3DScanEffects_ExecutesWithoutException()
    {
        var ex = Record.Exception(() =>
        {
            Animation3DHelper.StopAll3DScanEffects();
            Animation3DHelper.StopAll3DScanEffects();
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task Stress_RapidCancelAndStart_LeavesConsistentState()
    {
        var vm = new ContextMenuViewModel();

        for (int i = 0; i < 5; i++)
        {
            var task = vm.ScanAsync();
            vm.CancelScan();
            await Task.Yield();
        }

        vm.CancelScan();
        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }
}

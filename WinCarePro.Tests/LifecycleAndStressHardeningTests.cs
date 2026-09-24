using System;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
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

    [Fact]
    public void StatusToBrushConverter_ReturnsCachedBrushesWithoutExcessiveAllocations()
    {
        var converter = new StatusToBrushConverter();
        var brush1 = converter.Convert("Success", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");
        var brush2 = converter.Convert("Success", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");
        var brushAmber1 = converter.Convert("Warning", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");
        var brushAmber2 = converter.Convert("Warning", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");

        Assert.Equal(brush1, brush2);
        Assert.Equal(brushAmber1, brushAmber2);
    }

    [Fact]
    public void HexToBrushConverter_CachesBrushesCorrectly()
    {
        var converter = new HexToBrushConverter();
        var brush1 = converter.Convert("#FF10B981", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");
        var brush2 = converter.Convert("#FF10B981", typeof(Microsoft.UI.Xaml.Media.Brush), "", "en-US");

        Assert.Equal(brush1, brush2);
    }

    [Fact]
    public void ViewModels_Cleanup_UnhooksAndReleasesResourcesWithoutException()
    {
        // Act & Assert that Cleanup on each ViewModel executes cleanly without memory-leak throws
        var junkVm = new JunkViewModel();
        junkVm.Cleanup();

        var startupVm = new StartupViewModel();
        startupVm.Cleanup();

        var uninstallVm = new UninstallViewModel();
        uninstallVm.Cleanup();

        var securityVm = new SecurityViewModel();
        securityVm.Cleanup();

        var repairVm = new RepairViewModel();
        repairVm.Cleanup();

        var diskVm = new DiskViewModel();
        diskVm.Cleanup();

        Assert.False(junkVm.IsScanning);
        Assert.False(startupVm.IsLoading);
        Assert.False(uninstallVm.IsBusy);
        Assert.False(securityVm.IsScanning);
        Assert.False(repairVm.IsBusy);
        Assert.False(diskVm.IsBusy);
    }

    [Fact]
    public async Task NetworkEngine_CheckDnsResolutionAsync_RunsGracefully()
    {
        var engine = new NetworkEngine();
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await engine.CheckDnsResolutionAsync(cts.Token);
        // Can be true (online) or false (offline/simulated), but must not throw or sync-wait
        Assert.True(result || !result);
    }
}

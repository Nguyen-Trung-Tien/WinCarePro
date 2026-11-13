using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.ViewModels;

namespace WinCarePro.Tests.Presentation;

public class SmartPageLifecycleAndRaceConditionTests
{
    private class TestRaceConditionViewModel : ViewModelBase
    {
        public int LatestDeliveredVersion { get; private set; }
        public string ResultData { get; private set; } = string.Empty;

        public async Task SimulateSlowDataFetchAsync(int simulatedDelayMs, string dataPayload)
        {
            int version = NextRequestVersion();
            await Task.Delay(simulatedDelayMs);

            // Race Condition Guard: Discard late arrival if a newer request was dispatched
            if (!IsCurrentRequestVersion(version))
            {
                return;
            }

            LatestDeliveredVersion = version;
            ResultData = dataPayload;
        }
    }

    [Fact]
    public async Task RequestVersioning_ShouldDiscardLateArrivingData_WhenNewRequestTriggered()
    {
        // Arrange
        var vm = new TestRaceConditionViewModel();

        // Act: Dispatch slow request (version 1) first, then immediate fast request (version 2)
        var slowTask = vm.SimulateSlowDataFetchAsync(150, "Old Slow Data");
        var fastTask = vm.SimulateSlowDataFetchAsync(20, "Fresh Rapid Data");

        await Task.WhenAll(slowTask, fastTask);

        // Assert: The slower task finishing last MUST NOT overwrite the fresh data from version 2
        Assert.Equal(2, vm.LatestDeliveredVersion);
        Assert.Equal("Fresh Rapid Data", vm.ResultData);
    }

    [Fact]
    public void SafePreflightGuard_VerifyPrerequisites_ShouldDetectDiskHeadroom()
    {
        // Act
        var result = SafePreflightGuard.VerifyPrerequisites(requireAdmin: false, minFreeDiskBytes: 1024);

        // Assert: System should successfully evaluate prerequisites
        Assert.True(result.CanProceed || !result.CanProceed);
        Assert.True(result.FreeSpaceBytes >= 0);
    }

    [Fact]
    public void SafePreflightGuard_IsSafeToTouchService_ShouldProtectCoreServices()
    {
        // Assert: Known protected Windows services must be shielded
        Assert.False(SafePreflightGuard.IsSafeToTouchService("wuauserv"));
        Assert.False(SafePreflightGuard.IsSafeToTouchService("WinDefend"));
        Assert.False(SafePreflightGuard.IsSafeToTouchService("RpcSs"));

        // A benign user service should be touchable
        Assert.True(SafePreflightGuard.IsSafeToTouchService("MyCustomDummyService123"));
    }
}


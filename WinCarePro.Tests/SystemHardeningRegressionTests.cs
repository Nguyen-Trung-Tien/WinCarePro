using System;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;
using WinCarePro.ViewModels;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;

namespace WinCarePro.Tests;

public class SystemHardeningRegressionTests
{
    [Fact]
    public void StartupEngine_GetLastBootTimeSeconds_DoesNotThrowException()
    {
        // Arrange
        var engine = new StartupEngine();

        // Act & Assert: Phải chạy an toàn, không ném FileNotFoundException hay bất kỳ unhandled exception nào
        var bootSec = engine.GetLastBootTimeSeconds();

        // Trả về -1 (khi không tìm thấy/không mở được log) hoặc số dương (thời gian boot)
        Assert.True(bootSec >= -1);
    }

    [Fact]
    public void ViewModelBase_RunOnUI_ExecutesActionSafelyWithoutDispatcher()
    {
        // Arrange
        var vm = new ViewModelBase();
        bool executed = false;

        // Act
        vm.RunOnUI(() =>
        {
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void ViewModelBase_RunOnUI_CatchesAndSuppressesInnerExceptions()
    {
        // Arrange
        var vm = new ViewModelBase();

        // Act & Assert: Exception trong action không được làm sập ứng dụng
        var ex = Record.Exception(() =>
        {
            vm.RunOnUI(() =>
            {
                throw new InvalidOperationException("Test exception inside RunOnUI");
            });
        });

        Assert.Null(ex);
    }

    [Fact]
    public void SafeRegistryGuard_RejectsCriticalKeys()
    {
        // Assert
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKEY_LOCAL_MACHINE\SYSTEM"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKEY_LOCAL_MACHINE\SAM"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKEY_CLASSES_ROOT"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKEY_CURRENT_USER"));
    }

    [Fact]
    public void SecurityViewModel_CancelScan_ResetsStateSafely()
    {
        // Arrange
        var vm = new SecurityViewModel();

        // Act
        vm.CancelScan();

        // Assert
        Assert.False(vm.IsScanning);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }
}

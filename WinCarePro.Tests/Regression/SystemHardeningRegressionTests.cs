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
}

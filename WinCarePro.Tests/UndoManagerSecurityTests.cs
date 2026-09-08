using System;
using Microsoft.Win32;
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests;

public class UndoManagerSecurityTests
{
    private readonly UndoManagerService _undoManager = new();

    // ============================================================
    // 1. Invalid Snapshot Rejection Tests
    // ============================================================

    [Fact]
    public void RollbackSnapshot_RejectsNullSnapshot()
    {
        bool result = _undoManager.RollbackSnapshot(null!);
        Assert.False(result);

        var detailed = _undoManager.RollbackSnapshotDetailed(null!);
        Assert.False(detailed.IsSuccess);
        Assert.Contains("null", detailed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RollbackSnapshot_RejectsEmptyOrWhitespaceKeyName(string? keyName)
    {
        var snapshot = new StateSnapshotEntry
        {
            Category = "Registry",
            KeyName = keyName!,
            OriginalValue = "1",
            NewValue = "0"
        };

        bool result = _undoManager.RollbackSnapshot(snapshot);
        Assert.False(result);

        var detailed = _undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.False(detailed.IsSuccess);
    }

    [Theory]
    [InlineData("NoSlashKeyIdentifier")]
    [InlineData(@"\LeadingSlashOnly")]
    [InlineData(@"TrailingSlashOnly\")]
    [InlineData(@"HKCU\")]
    public void RollbackSnapshot_RejectsMalformedKeyName(string malformedKey)
    {
        var snapshot = new StateSnapshotEntry
        {
            Category = "Registry",
            KeyName = malformedKey,
            OriginalValue = "100",
            NewValue = "200"
        };

        bool result = _undoManager.RollbackSnapshot(snapshot);
        Assert.False(result);

        var detailed = _undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.False(detailed.IsSuccess);
        Assert.Contains("Malformed", detailed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(@"HKCR\Applications\test.exe\shell\open\command\testVal")]
    [InlineData(@"HKU\.DEFAULT\Control Panel\Desktop\testVal")]
    [InlineData(@"HKCC\System\CurrentControlSet\Control\testVal")]
    public void RollbackSnapshot_RejectsUnsupportedHives(string unsupportedPath)
    {
        var snapshot = new StateSnapshotEntry
        {
            Category = "Registry",
            KeyName = unsupportedPath,
            OriginalValue = "1",
            NewValue = "0"
        };

        bool result = _undoManager.RollbackSnapshot(snapshot);
        Assert.False(result);

        var detailed = _undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.False(detailed.IsSuccess);
    }

    // ============================================================
    // 2. Sensitive & Protected Registry Target Rejection Tests
    // ============================================================

    [Theory]
    [InlineData(@"HKLM\SAM\Domains\Account\Users\F")]
    [InlineData(@"HKLM\SECURITY\Policy\Secrets\CurrVal")]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa\AuthenticationPackages")]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\BootExecute")]
    [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell")]
    [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Userinit")]
    [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\notepad.exe\Debugger")]
    public void RollbackSnapshot_RejectsProtectedSystemTargets(string protectedKeyName)
    {
        var snapshot = new StateSnapshotEntry
        {
            Category = "Registry",
            KeyName = protectedKeyName,
            OriginalValue = "MaliciousPayload.exe",
            NewValue = "explorer.exe"
        };

        bool result = _undoManager.RollbackSnapshot(snapshot);
        Assert.False(result, $"Rollback of protected target '{protectedKeyName}' must be rejected by fail-closed guard.");

        var detailed = _undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.False(detailed.IsSuccess);
        Assert.Contains("protected", detailed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Shell")]
    [InlineData("Userinit")]
    [InlineData("AppInit_DLLs")]
    [InlineData("BootExecute")]
    public void RollbackSnapshot_RejectsCriticalValueNamesEvenInSubkeys(string criticalValName)
    {
        var snapshot = new StateSnapshotEntry
        {
            Category = "Registry",
            KeyName = $@"HKCU\Software\SomeVendor\App\{criticalValName}",
            OriginalValue = "test.dll",
            NewValue = ""
        };

        bool result = _undoManager.RollbackSnapshot(snapshot);
        Assert.False(result);

        var detailed = _undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.False(detailed.IsSuccess);
        Assert.Contains("protected", detailed.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // 3. Safe Registry Rollback Execution Test
    // ============================================================

    [Fact]
    public void RollbackSnapshot_SuccessfullyRestoresSafeUserLeafKey()
    {
        string subKeyPath = @"Software\WinCarePro_Test_SafeRollback_" + Guid.NewGuid().ToString("N");
        string valName = "TestSetting";
        string fullKeyIdentifier = $@"HKCU\{subKeyPath}\{valName}";

        try
        {
            // 1. Setup a test key in HKCU
            using (var key = Registry.CurrentUser.CreateSubKey(subKeyPath, true))
            {
                key.SetValue(valName, 42, RegistryValueKind.DWord);
            }

            // Verify initial state
            using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath))
            {
                Assert.NotNull(key);
                Assert.Equal(42, key.GetValue(valName));
            }

            // 2. Simulate application changing value to 999
            using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath, true))
            {
                key!.SetValue(valName, 999, RegistryValueKind.DWord);
            }

            // 3. Rollback snapshot to original value 42
            var snapshot = new StateSnapshotEntry
            {
                Category = "Registry",
                KeyName = fullKeyIdentifier,
                OriginalValue = "42",
                NewValue = "999"
            };

            var result = _undoManager.RollbackSnapshotDetailed(snapshot);
            Assert.True(result.IsSuccess, $"Rollback should succeed: {result.Message}");

            // 4. Verify value was restored to 42
            using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath))
            {
                Assert.NotNull(key);
                Assert.Equal(42, key.GetValue(valName));
            }

            // 5. Test rollback to empty (value deletion)
            var deleteSnapshot = new StateSnapshotEntry
            {
                Category = "Registry",
                KeyName = fullKeyIdentifier,
                OriginalValue = "", // Empty indicates value did not exist originally -> delete value
                NewValue = "42"
            };

            var delResult = _undoManager.RollbackSnapshotDetailed(deleteSnapshot);
            Assert.True(delResult.IsSuccess, $"Value deletion rollback should succeed: {delResult.Message}");

            using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath))
            {
                Assert.NotNull(key);
                Assert.Null(key.GetValue(valName));
            }
        }
        finally
        {
            // Cleanup test subkey
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(subKeyPath, false);
            }
            catch { }
        }
    }
}

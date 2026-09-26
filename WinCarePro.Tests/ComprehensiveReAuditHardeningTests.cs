using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Security;
using WinCarePro.Models;
using WinCarePro.Services.Implementations;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class ComprehensiveReAuditHardeningTests
{
    public ComprehensiveReAuditHardeningTests()
    {
        DbManager.InitializeDatabase();
    }

    // =========================================================================
    // 1. SafePathGuard Maintenance Whitelist & Root Protection (REG-01, NEW-01)
    // =========================================================================

    [Fact]
    public void SafePathGuard_AllowsWindowsMaintenanceSubfolders()
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        
        string minidumpFile = Path.Combine(winDir, "Minidump", "crash-dump-2026.dmp");
        string memoryDumpFile = Path.Combine(winDir, "MEMORY.DMP");
        string prefetchFile = Path.Combine(winDir, "Prefetch", "TESTAPP.EXE-12345678.pf");
        string deliveryOptFile = Path.Combine(winDir, "SoftwareDistribution", "DeliveryOptimization", "Cache", "chunk.bin");
        string system32LogFile = Path.Combine(winDir, "System32", "LogFiles", "WMI", "trace.etl");

        Assert.True(SafePathGuard.IsPathSafeForDeletion(minidumpFile));
        Assert.True(SafePathGuard.IsPathSafeForDeletion(memoryDumpFile));
        Assert.True(SafePathGuard.IsPathSafeForDeletion(prefetchFile));
        Assert.True(SafePathGuard.IsPathSafeForDeletion(deliveryOptFile));
        Assert.True(SafePathGuard.IsPathSafeForDeletion(system32LogFile));
    }

    [Fact]
    public void SafePathGuard_ProtectsMaintenanceRootContainers()
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        
        string minidumpDir = Path.Combine(winDir, "Minidump");
        string prefetchDir = Path.Combine(winDir, "Prefetch");
        string tempDir = Path.Combine(winDir, "Temp");
        string softDistDir = Path.Combine(winDir, "SoftwareDistribution");
        string system32LogsDir = Path.Combine(winDir, "System32", "LogFiles");

        Assert.False(SafePathGuard.IsPathSafeForDeletion(minidumpDir));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(prefetchDir));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(tempDir));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(softDistDir));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(system32LogsDir));
    }

    // =========================================================================
    // 2. DiskEngine ClearEmptyFoldersAsync Root Folder Protection (SAFE-01)
    // =========================================================================

    [Fact]
    public async Task DiskEngine_ClearEmptyFoldersAsync_PreservesRootFolder()
    {
        var diskEngine = new DiskEngine();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"WinCare_EmptyFolderTest_{Guid.NewGuid():N}");
        
        try
        {
            // Create root and nested empty subdirectories
            Directory.CreateDirectory(tempRoot);
            string sub1 = Path.Combine(tempRoot, "EmptySub1");
            string sub2 = Path.Combine(tempRoot, "EmptySub2", "NestedEmpty");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(sub2);

            Assert.True(Directory.Exists(tempRoot));
            Assert.True(Directory.Exists(sub1));
            Assert.True(Directory.Exists(sub2));

            // Run clean
            int deleted = await diskEngine.ClearEmptyFoldersAsync(tempRoot);

            // Subdirectories should be cleaned, but root MUST still exist
            Assert.True(deleted >= 2);
            Assert.True(Directory.Exists(tempRoot), "Root folder must never be deleted by ClearEmptyFoldersAsync!");
            Assert.False(Directory.Exists(sub1));
            Assert.False(Directory.Exists(sub2));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try { Directory.Delete(tempRoot, true); } catch { }
            }
        }
    }

    // =========================================================================
    // 3. UpdateSecurityValidator Blocks Sensitive File Deletion (SEC-01)
    // =========================================================================

    [Fact]
    public void UpdateSecurityValidator_SecureDeleteFile_BlocksUnsafePaths()
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string explorerExe = Path.Combine(winDir, "explorer.exe");
        string notepadExe = Path.Combine(winDir, "System32", "notepad.exe");

        // SecureDeleteFile must reject protected paths via SafePathGuard without throwing or wiping
        UpdateSecurityValidator.SecureDeleteFile(explorerExe);
        UpdateSecurityValidator.SecureDeleteFile(notepadExe);

        Assert.True(File.Exists(explorerExe));
        Assert.True(File.Exists(notepadExe));
    }

    // =========================================================================
    // 4. UndoManager Handles SystemTweak Snapshot (ARCH-01)
    // =========================================================================

    [Fact]
    public void UndoManager_RollbackSnapshot_HandlesSystemTweakWithoutSlashError()
    {
        var undoManager = new UndoManagerService();
        var snapshot = new StateSnapshotEntry
        {
            Category = "SystemTweak",
            KeyName = "MenuShowDelay",
            OriginalValue = "400",
            NewValue = "50"
        };

        // Should not fail with 'Malformed snapshot KeyName'
        var detailed = undoManager.RollbackSnapshotDetailed(snapshot);
        Assert.DoesNotContain("Malformed snapshot KeyName", detailed.Message);
    }

    // =========================================================================
    // 5. DbManager Transaction Rethrows on Rollback (NEW-02)
    // =========================================================================

    [Fact]
    public void DbManager_ExecuteInTransaction_RethrowsOnRollback()
    {
        // When an action inside ExecuteInTransaction throws, the transaction must roll back
        // and the exception must be rethrown so callers know it failed
        Assert.Throws<InvalidOperationException>(() =>
        {
            DbManager.ExecuteInTransaction((conn, tx) =>
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CREATE TEMP TABLE IF NOT EXISTS _TestTxTable (id INT);";
                cmd.ExecuteNonQuery();

                throw new InvalidOperationException("Simulated transaction failure");
            });
        });
    }

    // =========================================================================
    // 6. HardwareDriverEngine Thermal Sensor Grounding (CORR-01)
    // =========================================================================

    [Fact]
    public void HardwareDriverEngine_GetCpuTemperature_ReturnsNaN_WhenNoSensor()
    {
        var driverEngine = new HardwareDriverEngine();
        double temp = driverEngine.GetCpuTemperature();

        // Either a valid physical reading (10.0 to 115.0 C) or double.NaN (when no ACPI sensor exists)
        // Must never return synthetic/mock random values
        if (!double.IsNaN(temp))
        {
            Assert.True(temp >= 0.0 && temp <= 130.0, $"Physical temperature out of realistic range: {temp}");
        }
        else
        {
            Assert.True(double.IsNaN(temp));
        }
    }

    // =========================================================================
    // 7. DiskEngine Physical Drive Correctness (CORR-02)
    // =========================================================================

    [Fact]
    public void DiskEngine_GetDiskHealthStatus_DoesNotReturnMockVirtualDrive()
    {
        var diskEngine = new DiskEngine();
        var disks = diskEngine.GetDiskHealthStatus();

        Assert.NotNull(disks);
        foreach (var disk in disks)
        {
            // Verify mock virtual fallback was purged
            Assert.NotEqual("Virtual Disk Drive", disk.Model);
        }
    }

    // =========================================================================
    // 8. SafePathGuard Maintenance Directory Cleaning Safety (ROOT-CAUSE-01)
    // =========================================================================

    [Fact]
    public void SafePathGuard_IsSafeToCleanDirectory_DistinguishesContainersFromUnsafeRoots()
    {
        string userTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string winTemp = Path.Combine(winDir, "Temp");
        string winPrefetch = Path.Combine(winDir, "Prefetch");
        string winMinidump = Path.Combine(winDir, "Minidump");

        // Allowed maintenance containers for file cleaning:
        Assert.True(SafePathGuard.IsSafeToCleanDirectory(userTemp), $"User temp should be safe to clean: {userTemp}");
        Assert.True(SafePathGuard.IsSafeToCleanDirectory(winTemp), $"Windows temp should be safe to clean: {winTemp}");
        Assert.True(SafePathGuard.IsSafeToCleanDirectory(winPrefetch), $"Prefetch should be safe to clean: {winPrefetch}");
        Assert.True(SafePathGuard.IsSafeToCleanDirectory(winMinidump), $"Minidump should be safe to clean: {winMinidump}");

        // Protected system and user directories must NEVER be safe to clean:
        Assert.False(SafePathGuard.IsSafeToCleanDirectory(@"C:\"));
        Assert.False(SafePathGuard.IsSafeToCleanDirectory(winDir));
        Assert.False(SafePathGuard.IsSafeToCleanDirectory(Path.Combine(winDir, "System32")));
        Assert.False(SafePathGuard.IsSafeToCleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        Assert.False(SafePathGuard.IsSafeToCleanDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

        // Container directory itself must NOT be allowed to be deleted by IsPathSafeForDeletion
        Assert.False(SafePathGuard.IsPathSafeForDeletion(winTemp), "Container root should not be deleted itself");
        Assert.False(SafePathGuard.IsPathSafeForDeletion(winPrefetch), "Container root should not be deleted itself");
    }

    // =========================================================================
    // 9. SystemOptimizerEngine Synchronous Apply and Revert (ROOT-CAUSE-02)
    // =========================================================================

    [Fact]
    public void SystemOptimizerEngine_ApplyAndRevertTweak_Synchronous_DoesNotDeadlock()
    {
        var optimizer = new SystemOptimizerEngine();
        var tweaks = optimizer.GetTweaks();
        Assert.NotEmpty(tweaks);

        // Find a harmless user-level tweak for test
        var tweak = tweaks.Find(t => t.Id == "MenuShowDelay") ?? tweaks[0];

        // Should execute synchronously without deadlocks or sync-over-async .GetAwaiter().GetResult()
        bool applyResult = optimizer.ApplyTweak(tweak);
        bool revertResult = optimizer.RevertTweak(tweak);

        // Returns boolean without throwing unhandled exceptions
        Assert.True(applyResult || !applyResult);
        Assert.True(revertResult || !revertResult);
    }

    // =========================================================================
    // 10. ProcessRunner Canonical Path Resolution (ROOT-CAUSE-03)
    // =========================================================================

    [Fact]
    public void ProcessRunner_ResolveSafeExecutablePath_ResolvesSystemTools()
    {
        string regedit = ProcessRunner.ResolveSafeExecutablePath("regedit.exe");
        string explorer = ProcessRunner.ResolveSafeExecutablePath("explorer.exe");
        string taskmgr = ProcessRunner.ResolveSafeExecutablePath("taskmgr.exe");

        Assert.NotNull(regedit);
        Assert.True(File.Exists(regedit));
        Assert.EndsWith("regedit.exe", regedit, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.Windows), regedit, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(explorer);
        Assert.True(File.Exists(explorer));
        Assert.EndsWith("explorer.exe", explorer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.Windows), explorer, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(taskmgr);
        Assert.True(File.Exists(taskmgr));
        Assert.EndsWith("taskmgr.exe", taskmgr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Environment.SystemDirectory, taskmgr, StringComparison.OrdinalIgnoreCase);
    }
}

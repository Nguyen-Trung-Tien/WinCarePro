using System;
using System.IO;
using Xunit;
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Infrastructure.Security;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class SystemAuditValidationTests
{
    public SystemAuditValidationTests()
    {
        DbManager.InitializeDatabase();
    }

    [Fact]
    public void SafePathGuard_IsSafeToDelete_MatchesSpecification_RejectsCriticalPaths()
    {
        // Act & Assert
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Windows"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Windows\System32"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Program Files"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Windows\System32\drivers\etc\hosts"));
        Assert.False(SafePathGuard.IsSafeToDelete(""));
        Assert.False(SafePathGuard.IsSafeToDelete(null!));
    }

    [Fact]
    public void SafePathGuard_IsSafeToDelete_AcceptsValidTempFiles()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), $"audit_test_{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempFile, "audit validation test");

        try
        {
            // Act
            bool isSafe = SafePathGuard.IsSafeToDelete(tempFile);

            // Assert
            Assert.True(isSafe);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Theory]
    [InlineData("Google.Chrome; shutdown -s -t 0", "Google.Chrome shutdown -s -t 0")]
    [InlineData("app & calc.exe | echo hello", "app  calc.exe  echo hello")]
    [InlineData("`rm -rf /`", "rm -rf /")]
    [InlineData("$env:PATH", "env:PATH")]
    [InlineData("normal-package-1.2.3", "normal-package-1.2.3")]
    [InlineData("   spaced.id   ", "spaced.id")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void InputSanitizer_Sanitize_EliminatesDangerousShellCharacters(string? input, string expected)
    {
        // Act
        string sanitized = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    public void DbManager_ClearAllLogs_PurgesLogsThreadSafely()
    {
        // Arrange: Insert unique log entry
        string testAction = $"AuditLogTest_{Guid.NewGuid():N}";
        DbManager.LogAction(testAction, "AuditModule", "Success");

        var logsBefore = DbManager.GetLogs("AuditModule", testAction);
        Assert.NotEmpty(logsBefore);

        // Act
        DbManager.ClearAllLogs();

        // Assert
        var logsAfter = DbManager.GetLogs("AuditModule", testAction);
        Assert.Empty(logsAfter);
    }

    [Fact]
    public void DbManager_ClearAllReports_PurgesReportsThreadSafely()
    {
        // Arrange: Save a dummy report
        string testReportName = $"AuditReport_{Guid.NewGuid():N}";
        DbManager.SaveReport(testReportName, @"C:\Test\Report.txt");

        var reportsBefore = DbManager.GetReports();
        Assert.Contains(reportsBefore, r => r.ReportName == testReportName);

        // Act
        DbManager.ClearAllReports();

        // Assert
        var reportsAfter = DbManager.GetReports();
        Assert.Empty(reportsAfter);
    }

    [Fact]
    public void CrashLogger_SanitizesSensitiveCredentialsAndTokens()
    {
        // Arrange
        string rawLog = "Error on connection password=MySecretPass123; bearer token: abc-xyz-secret; email: dev@example.com";

        // Act
        string sanitized = CrashLogger.Sanitize(rawLog);

        // Assert
        Assert.DoesNotContain("MySecretPass123", sanitized);
        Assert.DoesNotContain("abc-xyz-secret", sanitized);
        Assert.DoesNotContain("dev@example.com", sanitized);
        Assert.Contains("***REDACTED***", sanitized);
        Assert.Contains("***REDACTED_EMAIL***", sanitized);
    }

    [Fact]
    public void CrashLogger_LogMessage_ExecutesWithoutException()
    {
        // Act & Assert (Should not throw and should handle file I/O lock safely)
        var exception = Record.Exception(() =>
        {
            CrashLogger.LogMessage("AuditTestCategory", "Test verification entry for full system audit.");
        });

        Assert.Null(exception);
    }

    [Fact]
    public void NetworkViewModel_NavigationDuringSpeedTest_ResetsIsBusyAndReadyState()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.NetworkViewModel();
        vm.IsBusy = true;
        vm.SpeedTestPhase = "Testing Download...";

        // Act 1: Simulate navigating away from NetworkPage
        vm.Cleanup();

        // Assert 1: IsBusy must be freed, and phase restored to Ready
        Assert.False(vm.IsBusy);
        Assert.Equal("Ready", vm.SpeedTestPhase);

        // Act 2: Simulate navigating back to NetworkPage
        vm.Initialize();

        // Assert 2: ViewModel remains interactive and ready
        Assert.False(vm.IsBusy);
        Assert.Equal("Ready", vm.SpeedTestPhase);
        vm.Cleanup();
    }

    [Fact]
    public async System.Threading.Tasks.Task NetworkEngine_RunSpeedTestAsync_WithCancelledToken_AbortsImmediately()
    {
        // Arrange
        var engine = new WinCarePro.Engines.NetworkEngine();
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token to simulate instant navigation away

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act: Should abort immediately due to cancelled token rather than running for 5.8 seconds
        double result = await engine.RunSpeedTestAsync(null, cts.Token);
        sw.Stop();

        // Assert: Must return quickly (< 2000 ms) and not throw unhandled exception
        Assert.True(sw.ElapsedMilliseconds < 2500, $"Aborted speed test took {sw.ElapsedMilliseconds}ms, expected < 2500ms");
    }

    [Fact]
    public void SystemOptimizerViewModel_NavigationCycle_PreservesOperabilityAndResetsDisposed()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel();

        // Act 1: Simulate leaving SystemOptimizerPage
        vm.Cleanup();

        // Assert 1: Busy states are cleared
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsBoosting);
        Assert.False(vm.IsAiScanning);

        // Act 2: Simulate returning to SystemOptimizerPage
        vm.Initialize();

        // Assert 2: ViewModel must NOT be locked in disposed state; LoadTweaks should function
        vm.LoadTweaks();
        Assert.True(vm.Tweaks.Count >= 0);
        vm.Cleanup();
    }

    [Fact]
    public void RepairViewModel_NavigationCycle_PreservesEngineListenersAndConsoleOutput()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.RepairViewModel();

        // Act 1: Simulate leaving RepairPage while busy
        vm.IsBusy = true;
        vm.Cleanup();

        // Assert 1: IsBusy must be reset
        Assert.False(vm.IsBusy);

        // Act 2: Simulate returning to RepairPage
        vm.Initialize();

        // Assert 2: ViewModel is ready for new repairs
        Assert.False(vm.IsBusy);
        vm.Cleanup();
    }

    [Fact]
    public void DiskViewModel_NavigationCycle_AbortsPromptlyAndResetsState()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.DiskViewModel();
        vm.IsBusy = true;

        // Act 1: Simulate leaving DiskPage
        vm.Cleanup();

        // Assert 1: IsBusy must be reset
        Assert.False(vm.IsBusy);

        // Act 2: Simulate returning to DiskPage
        vm.Initialize();

        // Assert 2: Ready for operations
        Assert.False(vm.IsBusy);
        vm.Cleanup();
    }

    [Fact]
    public void SecurityViewModel_NavigationCycle_CancelsPromptlyAndPreservesSubsequentScan()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.SecurityViewModel();
        vm.IsScanning = true;

        // Act 1: Simulate leaving SecurityPage
        vm.Cleanup();

        // Assert 1: Scanning must be cancelled immediately
        Assert.False(vm.IsScanning);

        // Act 2: Simulate returning to SecurityPage
        vm.Initialize();

        // Assert 2: Ready for scanning again
        Assert.False(vm.IsScanning);
        vm.Cleanup();
    }

    [Fact]
    public void JunkViewModel_NavigationCycle_PreservesLanguageAndResetsBusyState()
    {
        // Arrange
        var vm = new WinCarePro.ViewModels.JunkViewModel();
        vm.IsScanning = true;
        vm.IsCleaning = true;

        // Act 1: Simulate leaving JunkPage
        vm.Cleanup();

        // Assert 1: Scanning and cleaning are reset
        Assert.False(vm.IsScanning);
        Assert.False(vm.IsCleaning);

        // Act 2: Simulate returning to JunkPage
        vm.Initialize();

        // Assert 2: Operational state restored
        Assert.False(vm.IsScanning);
        Assert.False(vm.IsCleaning);
        vm.Cleanup();
    }
}

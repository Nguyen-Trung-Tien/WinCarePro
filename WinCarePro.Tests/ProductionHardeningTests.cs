using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests;

public class ProductionHardeningTests
{
    // ============================================================
    // 1. SafeRegistryGuard Tests
    // ============================================================

    [Theory]
    [InlineData("HKEY_LOCAL_MACHINE", false)]
    [InlineData("HKLM", false)]
    [InlineData("HKCU", false)]
    [InlineData("HKEY_CURRENT_USER", false)]
    [InlineData(@"HKLM\SYSTEM", false)]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet", false)]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet\Services", false)]
    [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion", false)]
    [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SafeRegistryGuard_RejectsCriticalKeys(string? keyPath, bool expected)
    {
        bool isSafe = SafeRegistryGuard.IsSafeToDeleteKey(keyPath!);
        Assert.Equal(expected, isSafe);
    }

    [Fact]
    public void SafeRegistryGuard_AcceptsSafeLeafUninstallKey()
    {
        string safeKey = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\MyTestApp_12345";
        bool isSafe = SafeRegistryGuard.IsSafeToDeleteKey(safeKey);
        Assert.True(isSafe);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Shell", false)]
    [InlineData("Userinit", false)]
    public void SafeRegistryGuard_RejectsCriticalValues(string? valueName, bool expected)
    {
        bool isSafe = SafeRegistryGuard.IsSafeToDeleteValue(@"HKLM\Software\Test", valueName!);
        Assert.Equal(expected, isSafe);
    }

    [Fact]
    public void SafeRegistryGuard_AcceptsNormalValueInSafeSubkey()
    {
        bool isSafe = SafeRegistryGuard.IsSafeToDeleteValue(@"HKCU\Software\MyCompany\MyApp", "AppSetting1");
        Assert.True(isSafe);
    }

    // ============================================================
    // 2. SafePathGuard Tests
    // ============================================================

    [Theory]
    [InlineData(@"C:\", false)]
    [InlineData(@"C:\Windows", false)]
    [InlineData(@"C:\Windows\System32", false)]
    [InlineData(@"C:\Program Files", false)]
    [InlineData(@"C:\ProgramData", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void SafePathGuard_RejectsForbiddenSystemPaths(string? path, bool expected)
    {
        bool isSafe = SafePathGuard.IsPathSafeForDeletion(path!);
        Assert.Equal(expected, isSafe);
    }

    [Fact]
    public void SafeCleanDirectoryContents_DoesNotRecursivelyForceDeleteUnemptyDirectories()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"WinCare_SafeClean_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a subfolder with a file
            string subDir = Path.Combine(tempDir, "SubFolder");
            Directory.CreateDirectory(subDir);
            string fileInSub = Path.Combine(subDir, "inner.tmp");
            File.WriteAllText(fileInSub, "content");

            // Also create a file in the root temp directory
            string rootFile = Path.Combine(tempDir, "root.tmp");
            File.WriteAllText(rootFile, "content");

            long deletedFiles = SafePathGuard.SafeCleanDirectoryContents(tempDir);

            // Files directly in tempDir should be deleted
            Assert.True(deletedFiles >= 1);
            Assert.False(File.Exists(rootFile));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    // ============================================================
    // 3. OperationResult Standardization Tests
    // ============================================================

    [Fact]
    public void OperationResult_Ok_SetsSuccessAndPropertiesCorrectly()
    {
        var result = OperationResult.Ok("Everything completed.");
        Assert.True(result.IsSuccess);
        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.Null(result.Exception);
        Assert.Equal("Everything completed.", result.Message);
    }

    [Fact]
    public void OperationResult_Fail_PropagatesExceptionAndErrors()
    {
        var ex = new InvalidOperationException("Registry hive locked.");
        var result = OperationResult.Fail("Failed to access registry.", ex);

        Assert.False(result.IsSuccess);
        Assert.True(result.HasErrors);
        Assert.NotNull(result.Exception);
        Assert.Equal(ex, result.Exception);
        Assert.Equal("Failed to access registry.", result.Message);
    }

    [Fact]
    public void OperationResultGeneric_Ok_RetainsPayloadAndProperties()
    {
        var payload = new InstalledAppInfo { DisplayName = "Test App" };
        var result = OperationResult<InstalledAppInfo>.Ok(payload, "Found app.");

        Assert.True(result.IsSuccess);
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Data);
        Assert.Equal("Test App", result.Data.DisplayName);
    }

    [Fact]
    public void OperationResultGeneric_Fail_RetainsException()
    {
        var ex = new IOException("Disk error");
        var result = OperationResult<int>.Fail("Read failure", ex);

        Assert.False(result.IsSuccess);
        Assert.True(result.HasErrors);
        Assert.Equal(ex, result.Exception);
        Assert.Equal(0, result.Data);
    }

    // ============================================================
    // 4. Critical Service Safety Tests
    // ============================================================

    [Theory]
    [InlineData("RpcSs", true)]
    [InlineData("WinDefend", true)]
    [InlineData("SamSs", true)]
    [InlineData("PlugPlay", true)]
    [InlineData("RpcEptMapper", true)]
    [InlineData("DcomLaunch", true)]
    [InlineData("RandomThirdPartyService", false)]
    public void ServiceSafetyService_CorrectlyIdentifiesCriticalServices(string serviceName, bool expected)
    {
        var safety = new ServiceSafetyService();
        bool isCritical = safety.IsCriticalService(serviceName);
        Assert.Equal(expected, isCritical);
    }

    [Fact]
    public void StartupEngine_RejectsDisablingCriticalService()
    {
        var engine = new StartupEngine();
        // Disabling RpcSs must be rejected by fail-safe
        bool result = engine.SetServiceStartupType("RpcSs", ServiceStartMode.Disabled);
        Assert.False(result);
    }

    [Fact]
    public void StartupEngine_RejectsStoppingCriticalService()
    {
        var engine = new StartupEngine();
        // Stopping WinDefend must be rejected by fail-safe
        bool result = engine.ControlService("WinDefend", "Stop");
        Assert.False(result);
    }

    // ============================================================
    // 5. Cancellation Resilience Tests
    // ============================================================

    [Fact]
    public async Task UninstallEngine_Leftovers_RespectsCancellationToken()
    {
        var engine = new UninstallEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var app = new InstalledAppInfo
        {
            DisplayName = "Test App",
            InstallLocation = Path.GetTempPath()
        };

        // ScanLeftovers should exit quickly when token is cancelled
        var leftovers = await Task.Run(() => engine.ScanLeftovers(app, cts.Token));
        Assert.Empty(leftovers);
    }

    [Fact]
    public void RegistryBackupEngine_Scan_RespectsCancellationToken()
    {
        var engine = new RegistryBackupEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        Assert.Throws<OperationCanceledException>(() => engine.ScanRegistryIssues(cts.Token));
    }

    [Fact]
    public async Task AiWinCareEngine_AnalyzeSystemHealthAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        await Assert.ThrowsAsync<OperationCanceledException>(() => Modules.AiAssistant.AiWinCareEngine.AnalyzeSystemHealthAsync(cts.Token));
    }
}

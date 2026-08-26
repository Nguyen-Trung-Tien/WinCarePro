using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class SystemOptimizationAndMotionTests
{
    [Fact]
    public void SafePathGuard_CriticalWindowsPaths_MustNeverBeAllowedForDeletion()
    {
        string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        Assert.False(SafePathGuard.IsPathSafeForDeletion(systemRoot));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(system32));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(programFiles));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\"));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(@"C:\Windows\System32\cmd.exe"));
    }

    [Fact]
    public void SafePathGuard_UserTempFiles_ArePermittedForClean()
    {
        string tempDir = Path.GetTempPath();
        string testFile = Path.Combine(tempDir, $"wincare_test_{Guid.NewGuid():N}.tmp");
        
        Assert.True(SafePathGuard.IsPathSafeForDeletion(testFile));
    }

    [Fact]
    public void ProcessRunner_ServiceNameValidation_RejectsCommandInjectionCharacters()
    {
        Assert.True(ProcessRunner.IsValidServiceName("wuauserv"));
        Assert.True(ProcessRunner.IsValidServiceName("bits"));
        Assert.True(ProcessRunner.IsValidServiceName("WinCarePro_Svc"));

        Assert.False(ProcessRunner.IsValidServiceName("wuauserv & calc.exe"));
        Assert.False(ProcessRunner.IsValidServiceName("wuauserv; rm -rf /"));
        Assert.False(ProcessRunner.IsValidServiceName("bits | powershell"));
        Assert.False(ProcessRunner.IsValidServiceName(""));
        Assert.False(ProcessRunner.IsValidServiceName("   "));
    }

    [Fact]
    public void AiWinCareScoringEngine_HighLoadMetrics_CalculatesAppropriateScoreAndInsights()
    {
        var engine = new AiWinCareScoringEngine();
        var assessment = engine.EvaluateHealth(
            cpuUsage: 95.0,
            ramUsage: 92.0,
            diskActiveTime: 90.0,
            freeSpaceGB: 5.0,
            totalSpaceGB: 500.0,
            startupAppsCount: 25,
            junkSizeBytes: 15L * 1024 * 1024 * 1024, // 15 GB
            registryIssuesCount: 40,
            outdatedAppsCount: 6,
            pingLatencyMs: 150.0,
            packetLossPercent: 12.0,
            securityAudits: new List<string> { "Windows Defender Real-time protection disabled" }
        );

        Assert.NotNull(assessment);
        Assert.True(assessment.OverallScore < 60, "Severe metric saturation should yield degraded overall score.");
        Assert.NotEmpty(assessment.Insights);
        Assert.NotEmpty(assessment.Categories);
        Assert.Contains(assessment.Insights, i => i.ImpactLevel == "Critical" || i.ImpactLevel == "High");
    }

    [Fact]
    public void AiWinCareScoringEngine_CleanSystemMetrics_YieldsOptimalHealthScore()
    {
        var engine = new AiWinCareScoringEngine();
        var assessment = engine.EvaluateHealth(
            cpuUsage: 12.0,
            ramUsage: 35.0,
            diskActiveTime: 5.0,
            freeSpaceGB: 350.0,
            totalSpaceGB: 500.0,
            startupAppsCount: 3,
            junkSizeBytes: 100L * 1024 * 1024, // 100 MB
            registryIssuesCount: 0,
            outdatedAppsCount: 0,
            pingLatencyMs: 15.0,
            packetLossPercent: 0.0,
            securityAudits: new List<string>()
        );

        Assert.NotNull(assessment);
        Assert.True(assessment.OverallScore >= 90, "Clean system metrics must yield optimal score >= 90.");
        Assert.Equal("Low", assessment.RiskLevel);
    }

    [Fact]
    public async Task JunkCleanerEngine_Cancellation_GracefullyCancels()
    {
        var engine = new JunkCleanerEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Immediately cancel

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await engine.ScanJunkAsync(cts.Token);
        });
    }

    [Fact]
    public void DbManager_LogActionAndQuery_RetrievesAccurateHistory()
    {
        string uniqueAction = $"TestExecution_{Guid.NewGuid():N}";
        string module = "UnitTest";
        string status = "Verified";

        DbManager.LogAction(uniqueAction, module, status);
        var logs = DbManager.GetLogs(module, uniqueAction);

        Assert.NotEmpty(logs);
        Assert.Equal(uniqueAction, logs[0].Action);
        Assert.Equal(module, logs[0].Module);
        Assert.Equal(status, logs[0].Status);
    }

    [Fact]
    public void DbManager_Notifications_AddAndReadBadge()
    {
        int initialUnread = DbManager.GetUnreadNotificationsCount();
        string testTitle = $"Notification_{Guid.NewGuid():N}";

        DbManager.AddNotification(testTitle, "Detailed test notification body", "Info", showToast: false);
        int afterAddUnread = DbManager.GetUnreadNotificationsCount();

        Assert.True(afterAddUnread >= initialUnread + 1);

        var recent = DbManager.GetRecentNotifications(10);
        Assert.Contains(recent, n => n.Title == testTitle);
    }

    [Fact]
    public void AnimationHelper_ReducedMotion_CheckDoesNotThrow()
    {
        bool animationsEnabled = AnimationHelper.AreAnimationsEnabled();
        // Should execute cleanly on any Windows system
        Assert.True(animationsEnabled || !animationsEnabled);
    }

    [Fact]
    public async Task SystemOptimizerEngine_WorkingSetOptimization_ReturnsNonNegativeMetrics()
    {
        var engine = new SystemOptimizerEngine();
        var (processesOptimized, memoryReclaimedBytes) = await engine.OptimizeRamAsync();

        Assert.True(processesOptimized >= 0);
        Assert.True(memoryReclaimedBytes >= 0);
    }

    [Fact]
    public void SoftwareUpdaterEngine_VerifyDigitalSignature_NullOrEmptyRejects()
    {
        Assert.False(SoftwareUpdaterEngine.VerifyDigitalSignature("", "Microsoft Corporation"));
        Assert.False(SoftwareUpdaterEngine.VerifyDigitalSignature(null!, "Microsoft Corporation"));
    }
}

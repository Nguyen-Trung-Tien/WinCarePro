using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class MasterUpgradePhaseTests
{
    [Fact]
    public void OperationResult_Success_CreatesValidResult()
    {
        var result = OperationResult.Ok("Operation finished successfully.");
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal("Operation finished successfully.", result.Message);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void OperationResult_Fail_CreatesFailedResultWithErrors()
    {
        var result = OperationResult.Fail("Operation failed.", "ERR_001", "details", default, new[] { "ERR_002" });
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("ERR_001", result.Errors);
        Assert.Contains("ERR_002", result.Errors);
    }

    [Fact]
    public void OperationResultGeneric_ValueAccess_ReturnsPayloadOnSuccess()
    {
        var result = OperationResult<int>.Ok(42, "Calculated");
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void OperationResultGeneric_ImplicitConversion_WorksSeamlessly()
    {
        OperationResult<string> result = "Direct payload";
        Assert.True(result.IsSuccess);
        Assert.Equal("Direct payload", result.Data);
    }

    [Fact]
    public void OperationResult_Combine_CombinesMultipleResultsCorrectly()
    {
        var res1 = OperationResult.Ok("Step 1 OK");
        var res2 = OperationResult.Fail("Step 2 Failed", "ERR_STEP2");
        var combined = OperationResult.Combine(res1, res2);

        Assert.False(combined.IsSuccess);
        Assert.Equal(OperationStatus.Failed, combined.Status);
        Assert.Single(combined.Errors);
        Assert.Contains("ERR_STEP2", combined.Errors);
    }

    [Fact]
    public void SoftwareUpdater_VerifyDigitalSignature_RejectsNonExistentFile()
    {
        string fakePath = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid():N}.exe");
        bool verified = SoftwareUpdaterEngine.VerifyDigitalSignature(fakePath);
        Assert.False(verified);
    }

    [Fact]
    public void SoftwareUpdater_VerifyDigitalSignature_RejectsUnsignedArbitraryFile()
    {
        string dummyFile = Path.Combine(Path.GetTempPath(), $"dummy_{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(dummyFile, new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }); // Dummy MZ header

        try
        {
            bool verified = SoftwareUpdaterEngine.VerifyDigitalSignature(dummyFile);
            Assert.False(verified);
        }
        finally
        {
            if (File.Exists(dummyFile)) File.Delete(dummyFile);
        }
    }

    [Fact]
    public void DbManager_SaveAndRetrieveSnapshot_WorksCorrectly()
    {
        string category = "SystemTweak";
        string key = $"TestTweak_{Guid.NewGuid():N}";
        string originalVal = "400";
        string newVal = "50";

        DbManager.SaveSnapshot(category, key, originalVal, newVal);
        var snapshot = DbManager.GetLastSnapshot(category, key);

        Assert.NotNull(snapshot);
        Assert.Equal(originalVal, snapshot.Value.OriginalValue);
        Assert.Equal(newVal, snapshot.Value.NewValue);
    }

    [Fact]
    public void SystemTweak_MetadataProperties_AreProperlyAssigned()
    {
        var engine = new SystemOptimizerEngine();
        var tweaks = engine.GetTweaks();

        Assert.NotEmpty(tweaks);
        foreach (var tweak in tweaks)
        {
            Assert.False(string.IsNullOrWhiteSpace(tweak.Id));
            Assert.False(string.IsNullOrWhiteSpace(tweak.Name));
            Assert.False(string.IsNullOrWhiteSpace(tweak.Category));
            Assert.False(string.IsNullOrWhiteSpace(tweak.RecommendedValue));
            Assert.False(string.IsNullOrWhiteSpace(tweak.DefaultValue));
            Assert.False(string.IsNullOrWhiteSpace(tweak.RiskLevel));
        }
    }

    [Fact]
    public void UninstallEngine_Scanning_DoesNotInjectMockAppsInProduction()
    {
        var engine = new UninstallEngine();
        var apps = engine.ScanInstalledApps();

        Assert.NotNull(apps);
        // Ensure mock apps are not injected into user application listings
        Assert.DoesNotContain(apps, a => a.DisplayName == "Mock Store Game" || a.DisplayName == "Mock Trash App");
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Tests;

public class SystemOptimizerEngineTests
{
    private readonly SystemOptimizerEngine _engine = new();

    public SystemOptimizerEngineTests()
    {
        Services.TranslationManager.Instance.CurrentLanguage = Services.AppLanguage.English;
    }

    [Fact]
    public void GetTweaks_ReturnsAllConfiguredTweaks()
    {
        // Act
        var tweaks = _engine.GetTweaks();

        // Assert
        Assert.NotNull(tweaks);
        Assert.Equal(13, tweaks.Count);
        
        // Verify specific tweak IDs exist
        Assert.Contains(tweaks, t => t.Id == "MenuShowDelay");
        Assert.Contains(tweaks, t => t.Id == "AutoEndTasks");
        Assert.Contains(tweaks, t => t.Id == "WaitToKillAppTimeout");
        Assert.Contains(tweaks, t => t.Id == "NtfsDisableLastAccessUpdate");
        Assert.Contains(tweaks, t => t.Id == "NetworkThrottlingIndex");
        Assert.Contains(tweaks, t => t.Id == "SystemResponsiveness");
        Assert.Contains(tweaks, t => t.Id == "HwSchMode");
        Assert.Contains(tweaks, t => t.Id == "AllowTelemetry");
        Assert.Contains(tweaks, t => t.Id == "AllowCortana");
        Assert.Contains(tweaks, t => t.Id == "WerDisabled");
        Assert.Contains(tweaks, t => t.Id == "MinAnimate");
        Assert.Contains(tweaks, t => t.Id == "GameDVR_Enabled");
        Assert.Contains(tweaks, t => t.Id == "DisableLocation");
    }

    [Theory]
    [InlineData("MenuShowDelay", "50", "Performance")]
    [InlineData("AutoEndTasks", "1", "Performance")]
    [InlineData("WaitToKillAppTimeout", "2000", "Performance")]
    [InlineData("NtfsDisableLastAccessUpdate", "1", "System & Disk")]
    [InlineData("NetworkThrottlingIndex", "-1", "Performance")]
    [InlineData("SystemResponsiveness", "0", "Performance")]
    [InlineData("HwSchMode", "2", "Performance")]
    [InlineData("AllowTelemetry", "0", "Privacy & Logs")]
    [InlineData("AllowCortana", "0", "Privacy & Logs")]
    [InlineData("WerDisabled", "1", "Privacy & Logs")]
    [InlineData("MinAnimate", "0", "Performance")]
    [InlineData("GameDVR_Enabled", "0", "Performance")]
    [InlineData("DisableLocation", "1", "Privacy & Logs")]
    public void GetTweaks_VerifiesTweakConfigurations(string id, string recommendedValue, string category)
    {
        Services.TranslationManager.Instance.CurrentLanguage = Services.AppLanguage.English;
        // Act
        var tweaks = _engine.GetTweaks();
        var tweak = tweaks.FirstOrDefault(t => t.Id == id);

        // Assert
        Assert.NotNull(tweak);
        Assert.Equal(recommendedValue, tweak.RecommendedValue);
        Assert.Equal(WinCarePro.Services.TranslationManager.Instance.T(category), tweak.Category);
        Assert.False(string.IsNullOrWhiteSpace(tweak.Name));
        Assert.False(string.IsNullOrWhiteSpace(tweak.Description));
    }

    [Fact]
    public void GetRamStatus_ReturnsValidMetrics()
    {
        // Act
        var (totalGb, availGb, usedGb, pct) = _engine.GetRamStatus();

        // Assert
        Assert.True(totalGb > 0, "Total RAM should be greater than 0");
        Assert.True(availGb >= 0, "Available RAM should be non-negative");
        Assert.True(usedGb >= 0, "Used RAM should be non-negative");
        Assert.True(pct >= 0 && pct <= 100, "RAM percentage should be between 0 and 100");
    }

    [Fact]
    public async Task CleanDeliveryOptimizationCacheAsync_ExecutesSafelyAndReturnsBytes()
    {
        // Act
        long freedBytes = await _engine.CleanDeliveryOptimizationCacheAsync();

        // Assert - Delivery Optimization cache cleaning must safely succeed without throwing
        Assert.True(freedBytes >= 0, "Freed bytes must be non-negative");
    }

    [Fact]
    public void SafeCleanDirectoryWithStats_AccuratelyCleansSubdirectoriesWithoutCrossingJunctions()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"WinCare_StatsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            // Level 1 file
            string rootFile = Path.Combine(tempRoot, "root.tmp");
            File.WriteAllText(rootFile, "Hello root file");
            long rootFileSize = new FileInfo(rootFile).Length;

            // Level 2 subfolder and file
            string subDir = Path.Combine(tempRoot, "SubLevel1");
            Directory.CreateDirectory(subDir);
            string subFile = Path.Combine(subDir, "nested.tmp");
            File.WriteAllText(subFile, "Nested file data");
            long subFileSize = new FileInfo(subFile).Length;

            // Target external folder to simulate junction target
            string outsideDir = Path.Combine(Path.GetTempPath(), $"WinCare_Outside_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outsideDir);
            string outsideFile = Path.Combine(outsideDir, "critical_do_not_delete.dat");
            File.WriteAllText(outsideFile, "Critical data outside tree");

            string junctionInTree = Path.Combine(tempRoot, "JunctionToOutside");
            bool junctionCreated = false;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /J \"{junctionInTree}\" \"{outsideDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit(3000);
                junctionCreated = Directory.Exists(junctionInTree);
            }
            catch { }

            try
            {
                // Act: SafeCleanDirectoryWithStats
                var (deletedBytes, filesDeleted) = SafePathGuard.SafeCleanDirectoryWithStats(tempRoot, recursive: true);

                // Assert
                Assert.Equal(2, filesDeleted);
                Assert.Equal(rootFileSize + subFileSize, deletedBytes);
                Assert.False(File.Exists(rootFile), "Root file should be deleted");
                Assert.False(File.Exists(subFile), "Sub file should be deleted");
                Assert.False(Directory.Exists(subDir), "Empty subfolder should be deleted");

                // Critical safety assertion: files inside junction target MUST remain intact
                Assert.True(File.Exists(outsideFile), "Files in junction target must NOT be deleted!");
            }
            finally
            {
                if (junctionCreated && Directory.Exists(junctionInTree))
                {
                    try { Directory.Delete(junctionInTree, false); } catch { }
                }
                try { Directory.Delete(outsideDir, true); } catch { }
            }
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public void ApplyAndRevertTweakWithResult_ReturnsValidOperationResult()
    {
        var tweaks = _engine.GetTweaks();
        Assert.NotEmpty(tweaks);

        var tweak = tweaks.Find(t => t.Id == "MenuShowDelay") ?? tweaks[0];

        var applyResult = _engine.ApplyTweakWithResult(tweak);
        Assert.NotNull(applyResult);

        var revertResult = _engine.RevertTweakWithResult(tweak);
        Assert.NotNull(revertResult);
    }

    [Fact]
    public void GenerateOptimizationReport_ContainsMemoryAndTweakSections()
    {
        string report = _engine.GenerateOptimizationReport();

        Assert.NotNull(report);
        Assert.Contains("WINCARE PRO - SYSTEM OPTIMIZER AUDIT REPORT", report);
        Assert.Contains("--- MEMORY (RAM) STATUS ---", report);
        Assert.Contains("--- SYSTEM TWEAKS OVERVIEW ---", report);
        Assert.Contains("MenuShowDelay", report);
    }

    [Fact]
    public async Task SystemOptimizerViewModel_ExportReportAsync_Succeeds()
    {
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel(_engine);
        string tempPath = Path.Combine(Path.GetTempPath(), $"WinCare_ReportTest_{Guid.NewGuid():N}.txt");

        try
        {
            var result = await vm.ExportReportAsync(tempPath);
            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(tempPath));

            string content = await File.ReadAllTextAsync(tempPath);
            Assert.Contains("SYSTEM OPTIMIZER AUDIT REPORT", content);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            vm.Cleanup();
        }
    }

    [Fact]
    public void SystemOptimizerViewModel_SortAndFilter_OrdersCorrectly()
    {
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel(_engine);
        vm.LoadTweaks();

        Assert.NotEmpty(vm.FilteredTweaks);
        Assert.False(vm.HasNoTweaks);

        // Filter for nonexistent query
        vm.SearchQuery = "NONEXISTENT_QUERY_XYZ_123";
        Assert.Empty(vm.FilteredTweaks);
        Assert.True(vm.HasNoTweaks);

        // Clear query
        vm.SearchQuery = "";
        Assert.NotEmpty(vm.FilteredTweaks);
        Assert.False(vm.HasNoTweaks);

        // Sort by Name
        vm.CurrentSort = "Name";
        var names = vm.FilteredTweaks.Select(t => t.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);

        vm.Cleanup();
    }

    [Fact]
    public void SystemTweak_AutoEndTasks_ComparisonTextFormatsEnabledDisabled()
    {
        Services.TranslationManager.Instance.CurrentLanguage = Services.AppLanguage.English;
        var tweaks = _engine.GetTweaks();
        var autoEndTweak = tweaks.FirstOrDefault(t => t.Id == "AutoEndTasks");

        Assert.NotNull(autoEndTweak);
        autoEndTweak.CurrentValue = "0";
        Assert.Contains("Disabled", autoEndTweak.ComparisonText);
        Assert.Contains("Enabled", autoEndTweak.ComparisonText);

        autoEndTweak.CurrentValue = "1";
        Assert.DoesNotContain("0", autoEndTweak.ComparisonText);
    }

    [Fact]
    public void SystemOptimizerEngine_IsAdministrator_DoesNotThrow()
    {
        bool isAdmin = SystemOptimizerEngine.IsAdministrator();
        // Simply assert it evaluates without exception
        Assert.True(isAdmin || !isAdmin);
    }

    [Fact]
    public void SystemOptimizerViewModel_IsBusy_ReflectsSubOperationFlags()
    {
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel(_engine);
        Assert.False(vm.IsBusy);

        vm.IsLoading = true;
        Assert.True(vm.IsBusy);

        vm.IsLoading = false;
        Assert.False(vm.IsBusy);

        vm.IsBoosting = true;
        Assert.True(vm.IsBusy);

        vm.IsBoosting = false;
        Assert.False(vm.IsBusy);

        vm.IsCleaningCache = true;
        Assert.True(vm.IsBusy);

        vm.IsCleaningCache = false;
        Assert.False(vm.IsBusy);

        vm.Cleanup();
    }

    [Fact]
    public void SystemOptimizerViewModel_SelectedTweaksCount_UpdatesCorrectly()
    {
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel(_engine);
        vm.LoadTweaks();

        Assert.Equal(vm.Tweaks.Count, vm.TotalTweaksCount);
        Assert.True(vm.SelectedTweaksCount > 0);

        vm.DeselectAll();
        Assert.Equal(0, vm.SelectedTweaksCount);

        vm.SelectAll();
        Assert.Equal(vm.Tweaks.Count, vm.SelectedTweaksCount);

        int gamingCount = vm.SelectPresetProfile("Gaming");
        Assert.Equal(gamingCount, vm.SelectedTweaksCount);

        vm.Cleanup();
    }

    [Fact]
    public void SystemOptimizerViewModel_ActiveServicesCountText_ReturnsValidMetric()
    {
        var vm = new WinCarePro.ViewModels.SystemOptimizerViewModel(_engine);
        string text = vm.ActiveServicesCountText;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Monitored", text);
        vm.Cleanup();
    }
}

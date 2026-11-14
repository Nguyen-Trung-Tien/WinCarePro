using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Core;
using WinCarePro.Core.Helpers;
using WinCarePro.Engines;
using WinCarePro.Presentation.Controls;
using WinCarePro.ViewModels;

namespace WinCarePro.Tests.Regression;

public class PolarisV6UpgradeTests
{
    [Fact]
    public void AppConstants_SSOT_Integrity_V6()
    {
        Assert.Equal("6.0.0", AppConstants.DefaultVersionString);
        Assert.Equal("Polaris", AppConstants.Codename);
        Assert.Equal("WinCare Pro", AppConstants.AppName);
    }

    [Fact]
    public void InlineStatusCard_SuggestionType_ResolvesCorrectBadgeAndGlyph()
    {
        var glyph = InlineStatusCard.ResolveGlyph(InlineStatusCardType.Suggestion, "");
        var badge = InlineStatusCard.ResolveBadgeLabel(InlineStatusCardType.Suggestion, "");

        Assert.Equal("\uE946", glyph);
        Assert.Equal("RECOMMENDED", badge);
    }

    [Fact]
    public void CleanerViewModel_HasCompletedScan_And_Caching_State()
    {
        var junkEngine = new JunkCleanerEngine();
        var regEngine = new RegistryBackupEngine();
        var vm = new CleanerViewModel(junkEngine, regEngine);

        Assert.False(vm.HasCompletedScan);
        Assert.NotEmpty(vm.ActionSuggestionText);
        Assert.NotNull(vm.CleanerCategories);
        Assert.Empty(vm.CleanerCategories);
        Assert.False(vm.CanClean);
    }

    [Fact]
    public void HardwareDriverEngine_ReturnsValidHardwareSpecs()
    {
        var engine = new HardwareDriverEngine();
        var specs = engine.GetHardwareSpecifications();

        Assert.NotNull(specs);
        Assert.False(string.IsNullOrWhiteSpace(specs.OsVersion));
        Assert.False(string.IsNullOrWhiteSpace(specs.CpuModel));
        Assert.True(specs.CpuCores >= 1);
        Assert.True(specs.RamCapacityGb >= 1.0);
    }

    [Fact]
    public void SingleInstanceMutex_Release_ExceptionResilience()
    {
        // Must never throw unhandled exception on release
        var exception = Record.Exception(() => App.ReleaseSingleInstanceMutex());
        Assert.Null(exception);
    }

    [Fact]
    public void SafePathGuard_Blocks_Critical_Root_And_Windows_Paths()
    {
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Windows"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Windows\System32"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Users"));
        Assert.False(SafePathGuard.IsSafeToDelete(@"C:\Program Files"));
    }
}

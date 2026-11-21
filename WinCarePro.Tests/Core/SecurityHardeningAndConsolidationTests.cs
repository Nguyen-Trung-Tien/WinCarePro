using System;
using System.Threading.Tasks;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Services;
using WinCarePro.Presentation.Controls;
using WinCarePro.Services;
using Xunit;

namespace WinCarePro.Tests;

public class SecurityHardeningAndConsolidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    [InlineData("ftp://example.com/file.exe")]
    public void ShellLauncher_OpenUrl_RejectsUnsafeOrEmptyUrls(string unsafeUrl)
    {
        bool launched = ShellLauncher.OpenUrl(unsafeUrl, "UnitTest");
        Assert.False(launched);
    }

    [Fact]
    public async Task SafeAsyncExtensions_RunSafeAsync_CatchesExceptionWithoutCrashing()
    {
        var ex = await Record.ExceptionAsync(() =>
            SafeAsyncExtensions.RunSafeAsync(
                () => throw new InvalidOperationException("Simulated UI handler error"),
                "UnitTest.SafeAsync"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SafeAsyncExtensions_RunSafeAsync_SuppressesOperationCanceledException()
    {
        var ex = await Record.ExceptionAsync(() =>
            SafeAsyncExtensions.RunSafeAsync(
                () => throw new OperationCanceledException(),
                "UnitTest.SafeAsyncCancel"));

        Assert.Null(ex);
    }

    [Fact]
    public void AppUpdateService_ParseUpdateManifest_SelectsStableAndDetectsNewerVersion()
    {
        const string json = """
        {
          "version": "6.1.0",
          "url": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v6.1.0/WinCareProSetup.exe",
          "sha256": "abc123",
          "changelog": "Stability fixes",
          "beta_version": "6.2.0-beta",
          "beta_url": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v6.2.0/WinCareProSetup.exe"
        }
        """;

        var stable = AppUpdateService.ParseUpdateManifest(json, betaEnabled: false, new Version(6, 0, 0));
        Assert.True(stable.HasUpdate);
        Assert.Equal("6.1.0", stable.VersionString);
        Assert.Equal("abc123", stable.ExpectedSha256);

        var beta = AppUpdateService.ParseUpdateManifest(json, betaEnabled: true, new Version(6, 0, 0));
        Assert.True(beta.HasUpdate);
        Assert.Equal("6.2.0-beta", beta.VersionString);
    }

    [Theory]
    [InlineData("System Cleaner")]
    [InlineData("Clean Now")]
    [InlineData("Export CSV")]
    [InlineData("8 Core Modules")]
    [InlineData("7. Notifications & Activity Logs")]
    [InlineData("8. Settings, Backups & Maintenance")]
    public void TranslationManager_ContainsConsolidatedCleanerKeys(string key)
    {
        string vi = TranslationManager.Instance.GetTranslationForLanguage(key, AppLanguage.Vietnamese);
        string en = TranslationManager.Instance.GetTranslationForLanguage(key, AppLanguage.English);
        Assert.False(string.IsNullOrWhiteSpace(vi));
        Assert.False(string.IsNullOrWhiteSpace(en));
    }

    [Theory]
    [InlineData(AuraAlertSeverity.Info, "\uE946", "AppStatusInfoBackground")]
    [InlineData(AuraAlertSeverity.Success, "\uE73E", "AppStatusGoodBackground")]
    [InlineData(AuraAlertSeverity.Warning, "\uE7BA", "AppStatusWarningBackground")]
    [InlineData(AuraAlertSeverity.Error, "\uEA39", "AppStatusDangerBackground")]
    public void AuraAlertBanner_ResolveSeverityTokens_ReturnsConsistentAuraTokens(
        AuraAlertSeverity severity,
        string expectedGlyph,
        string expectedBgKey)
    {
        var (glyph, bgKey, _, _) = AuraAlertBanner.ResolveSeverityTokens(severity);
        Assert.Equal(expectedGlyph, glyph);
        Assert.Equal(expectedBgKey, bgKey);
    }
}

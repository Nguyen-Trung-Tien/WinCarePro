using System;
using Microsoft.UI.Xaml;
using WinCarePro.Services;
using Xunit;

namespace WinCarePro.Tests;

public class UiThemeAndConsistencyTests
{
    [Fact]
    public void ThemeManager_Instance_ShouldBeSingleton()
    {
        var instance1 = ThemeManager.Instance;
        var instance2 = ThemeManager.Instance;

        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void ThemeManager_ApplyTheme_ShouldUpdateCurrentThemeAndFireEvent()
    {
        var manager = ThemeManager.Instance;
        bool eventFired = false;
        EventHandler handler = (s, e) => eventFired = true;

        manager.ThemeChanged += handler;
        try
        {
            manager.ApplyTheme(ElementTheme.Light);
            Assert.Equal(ElementTheme.Light, manager.CurrentTheme);
            Assert.True(eventFired);

            eventFired = false;
            manager.ApplyTheme(ElementTheme.Dark);
            Assert.Equal(ElementTheme.Dark, manager.CurrentTheme);
            Assert.True(eventFired);
        }
        finally
        {
            manager.ThemeChanged -= handler;
        }
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Green")]
    [InlineData("Purple")]
    [InlineData("Pink")]
    [InlineData("Amber")]
    public void ThemeManager_ApplyAccent_ShouldSupportAllPalettes(string accent)
    {
        var manager = ThemeManager.Instance;
        bool accentEventFired = false;
        EventHandler handler = (s, e) => accentEventFired = true;

        manager.AccentChanged += handler;
        try
        {
            manager.ApplyAccent(accent);
            Assert.Equal(accent, manager.CurrentAccent);
            Assert.True(accentEventFired);
        }
        finally
        {
            manager.AccentChanged -= handler;
        }
    }

    [Fact]
    public void ThemeManager_ApplyAccent_NullOrEmpty_ShouldFallbackToDefault()
    {
        var manager = ThemeManager.Instance;
        manager.ApplyAccent("");
        Assert.Equal("Default", manager.CurrentAccent);
    }

    [Fact]
    public void TranslationManager_Instance_ShouldBeSingleton()
    {
        var instance1 = TranslationManager.Instance;
        var instance2 = TranslationManager.Instance;

        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void TranslationManager_Translate_EnglishAndVietnamese()
    {
        var manager = TranslationManager.Instance;
        string en = manager.GetTranslationForLanguage("Check for Updates", AppLanguage.English);
        Assert.Equal("Check for Updates", en);

        string vi = manager.GetTranslationForLanguage("Check for Updates", AppLanguage.Vietnamese);
        Assert.Equal("Kiểm tra cập nhật", vi);
    }

    [Fact]
    public void TranslationManager_WhitespacePreserved()
    {
        var manager = TranslationManager.Instance;
        string translated = manager.GetTranslationForLanguage("  Check for Updates  ", AppLanguage.Vietnamese);
        Assert.Equal("  Kiểm tra cập nhật  ", translated);
    }

    [Fact]
    public void TranslationManager_DynamicRegex_Translations()
    {
        var manager = TranslationManager.Instance;

        string gamingTurbo = manager.GetTranslationForLanguage("🚀 Gaming Turbo ACTIVE! Freed 350 MB RAM across 12 processes.", AppLanguage.Vietnamese);
        Assert.Contains("Gaming Turbo HOẠT ĐỘNG", gamingTurbo);
        Assert.Contains("350 MB RAM", gamingTurbo);

        string uninstall = manager.GetTranslationForLanguage("Uninstalling app: Google Chrome...", AppLanguage.Vietnamese);
        Assert.Equal("Đang gỡ cài đặt ứng dụng: Google Chrome...", uninstall);

        string cleanDirs = manager.GetTranslationForLanguage("Cleaned 5 empty directories under C:\\Temp", AppLanguage.Vietnamese);
        Assert.Equal("Đã dọn dẹp 5 thư mục rỗng trong C:\\Temp", cleanDirs);
    }

    [Fact]
    public void TranslationManager_Multiline_Normalized_Translations()
    {
        var manager = TranslationManager.Instance;

        string crlf = "1. Open Dashboard from the top of the left navigation pane.\r\n2. Observe hardware loads and current Health Score.\r\n3. Click [Quick Boost] to instantly reclaim RAM and optimize system processes.";
        string translatedCrlf = manager.GetTranslationForLanguage(crlf, AppLanguage.Vietnamese);
        Assert.Contains("Mở trang Tổng quan", translatedCrlf);

        string lf = "1. Open Dashboard from the top of the left navigation pane.\n2. Observe hardware loads and current Health Score.\n3. Click [Quick Boost] to instantly reclaim RAM and optimize system processes.";
        string translatedLf = manager.GetTranslationForLanguage(lf, AppLanguage.Vietnamese);
        Assert.Contains("Mở trang Tổng quan", translatedLf);
    }
}

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
        manager.CurrentLanguage = AppLanguage.English;
        string en = manager.T("Check for Updates");
        Assert.Equal("Check for Updates", en);

        manager.CurrentLanguage = AppLanguage.Vietnamese;
        string vi = manager.T("Check for Updates");
        Assert.Equal("Kiểm tra cập nhật", vi);
    }

    [Fact]
    public void TranslationManager_WhitespacePreserved()
    {
        var manager = TranslationManager.Instance;
        manager.CurrentLanguage = AppLanguage.Vietnamese;
        string translated = manager.T("  Check for Updates  ");
        Assert.Equal("  Kiểm tra cập nhật  ", translated);
    }
}

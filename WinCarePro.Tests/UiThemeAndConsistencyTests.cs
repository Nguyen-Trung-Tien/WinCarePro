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
    [InlineData("Cyan")]
    [InlineData("Cyberpunk")]
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

        string driveHealthy = manager.GetTranslationForLanguage("Drive C: usage is healthy at 39% (155,0 GB free). Storage sustainability is over 232 days.", AppLanguage.Vietnamese);
        Assert.Contains("Ổ C: mức sử dụng tốt", driveHealthy);
        Assert.Contains("232 ngày", driveHealthy);

        string preset = manager.GetTranslationForLanguage("Preset: Competitive FPS", AppLanguage.Vietnamese);
        Assert.Equal("Cấu hình: FPS Cạnh Tranh", preset);

        string bootSav = manager.GetTranslationForLanguage("-5.3s Boot Time", AppLanguage.Vietnamese);
        Assert.Equal("-5.3s Khởi Động", bootSav);

        string aiProc = manager.GetTranslationForLanguage("AI detected 268 active background processes. Disabling unnecessary startup items can improve boot time.", AppLanguage.Vietnamese);
        Assert.Contains("AI phát hiện 268 tiến trình nền", aiProc);
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

    [Fact]
    public void TranslationManager_AllPageSubtitles_TranslateCorrectly()
    {
        var manager = TranslationManager.Instance;
        var subtitles = new[]
        {
            "Real-time hardware telemetry & AI care.",
            "Intelligent diagnostics & automated optimization.",
            "Purge system caches, temp files & debris.",
            "Cleanly uninstall apps & remove leftover data.",
            "Network telemetry, DNS speed & TCP/IP repairs.",
            "SFC integrity, DISM & core Windows services.",
            "Hardware trust, credential safety & privacy.",
            "Kernel tuning, process scheduling & memory caching.",
            "Prioritize CPU threads & minimize latency.",
            "Manage shell extensions and declutter right-click menus.",
            "Control startup apps & boot velocity.",
            "S.M.A.R.T telemetry, storage & duplicate finder.",
            "Invalid registry entries, repairs & backups.",
            "Apps & packages updates via WinGet.",
            "System telemetry alerts, diagnostic logs & operation records.",
            "Personalize preferences, language & updates."
        };

        foreach (var sub in subtitles)
        {
            string vi = manager.GetTranslationForLanguage(sub, AppLanguage.Vietnamese);
            Assert.False(string.IsNullOrWhiteSpace(vi));
            Assert.NotEqual(sub, vi); // Must be translated to Vietnamese, not falling back to English unchanged

            string backToEn = manager.GetTranslationForLanguage(vi, AppLanguage.English);
            Assert.Equal(sub, backToEn);
        }

        // Context Menu Guideline card tests
        string guideDesc = manager.GetTranslationForLanguage("Disabling unused shell extensions removes right-click popup delay and prevents File Explorer freezes.", AppLanguage.Vietnamese);
        Assert.Equal("Vô hiệu hóa tiện ích không dùng giúp menu chuột phải mở tức thì và tránh đơ File Explorer.", guideDesc);

        string safeModDesc = manager.GetTranslationForLanguage("Disabled keys are safely prefixed with '-' rather than deleted, allowing instant restoration anytime.", AppLanguage.Vietnamese);
        Assert.Equal("Các khóa bị tắt được gắn tiền tố '-' thay vì bị xóa, cho phép khôi phục tức thì bất kỳ lúc nào.", safeModDesc);

        // Startup Health & Smart Insights tests
        string startupHealth = manager.GetTranslationForLanguage("Startup Health", AppLanguage.Vietnamese);
        Assert.Equal("Sức khỏe khởi động", startupHealth);

        string smartInsights = manager.GetTranslationForLanguage("Smart Insights", AppLanguage.Vietnamese);
        Assert.Equal("Đề xuất thông minh", smartInsights);

        string boostBtn = manager.GetTranslationForLanguage("Boost Boot Speed", AppLanguage.Vietnamese);
        Assert.Equal("Tăng tốc khởi động", boostBtn);

        string svcInsight = string.Format(manager.GetTranslationForLanguage("There are {0} running non-Microsoft background services. Consider disabling those you don't use regularly.", AppLanguage.Vietnamese), 10);
        Assert.Equal("Có 10 dịch vụ nền bên thứ ba đang chạy. Hãy tắt các dịch vụ không thường xuyên dùng.", svcInsight);

        // Disk Stat Ribbon tests
        string drivesVols = manager.GetTranslationForLanguage("Drives & Volumes", AppLanguage.Vietnamese);
        Assert.Equal("Ổ đĩa & Phân vùng", drivesVols);

        string physDevs = manager.GetTranslationForLanguage("Physical Devices", AppLanguage.Vietnamese);
        Assert.Equal("Thiết bị vật lý", physDevs);

        string storageItems = manager.GetTranslationForLanguage("Storage Items Analyzed", AppLanguage.Vietnamese);
        Assert.Equal("Mục lưu trữ đã phân tích", storageItems);

        string dirsFiles = manager.GetTranslationForLanguage("Directories & Files", AppLanguage.Vietnamese);
        Assert.Equal("Thư mục & Tệp tin", dirsFiles);

        string dupGroups = manager.GetTranslationForLanguage("Duplicate File Groups", AppLanguage.Vietnamese);
        Assert.Equal("Nhóm tệp trùng lặp", dupGroups);

        string reclaimClusters = manager.GetTranslationForLanguage("Reclaimable Clusters", AppLanguage.Vietnamese);
        Assert.Equal("Cụm có thể giải phóng", reclaimClusters);

        // AI Quick Actions Hub tests
        string aiHubTitle = manager.GetTranslationForLanguage("AI Automated Quick Remedies", AppLanguage.Vietnamese);
        Assert.Equal("Tác vụ khắc phục nhanh từ AI", aiHubTitle);

        string freeWs = manager.GetTranslationForLanguage("Free Working Set", AppLanguage.Vietnamese);
        Assert.Equal("Xóa bộ đệm RAM", freeWs);

        string trimBoot = manager.GetTranslationForLanguage("Trim Boot Overhead", AppLanguage.Vietnamese);
        Assert.Equal("Rút ngắn mở máy", trimBoot);

        string repairSocket = manager.GetTranslationForLanguage("Repair Socket Cache", AppLanguage.Vietnamese);
        Assert.Equal("Sửa kết nối socket", repairSocket);

        // Header User Guide button tests
        string guideTip = manager.GetTranslationForLanguage("User Guide & Documentation", AppLanguage.Vietnamese);
        Assert.Equal("Hướng Dẫn Sử Dụng & Tài Liệu", guideTip);

        string toggleTheme = manager.GetTranslationForLanguage("Toggle Theme", AppLanguage.Vietnamese);
        Assert.Equal("Đổi Giao Diện", toggleTheme);
    }
}

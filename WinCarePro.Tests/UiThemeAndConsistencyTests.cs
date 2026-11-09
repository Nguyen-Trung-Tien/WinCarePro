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
    public void ThemeManager_IsDark_Property_TracksCurrentThemeAccurately()
    {
        var manager = ThemeManager.Instance;

        manager.ApplyTheme(ElementTheme.Dark);
        Assert.True(manager.IsDark);
        Assert.Equal(ElementTheme.Dark, manager.CurrentTheme);

        manager.ApplyTheme(ElementTheme.Light);
        Assert.False(manager.IsDark);
        Assert.Equal(ElementTheme.Light, manager.CurrentTheme);

        manager.ApplyTheme(ElementTheme.Dark);
        Assert.True(manager.IsDark);
    }

    [Fact]
    public void ThemeManager_RapidToggleStress_MaintainsConsistencyAndDoesNotThrow()
    {
        var manager = ThemeManager.Instance;
        int themeEventCount = 0;
        EventHandler handler = (s, e) => themeEventCount++;

        manager.ThemeChanged += handler;
        try
        {
            for (int i = 0; i < 100; i++)
            {
                var targetTheme = (i % 2 == 0) ? ElementTheme.Light : ElementTheme.Dark;
                manager.ApplyTheme(targetTheme);
                Assert.Equal(targetTheme, manager.CurrentTheme);
                Assert.Equal(targetTheme == ElementTheme.Dark, manager.IsDark);
            }
            Assert.Equal(100, themeEventCount);
        }
        finally
        {
            manager.ThemeChanged -= handler;
            manager.ApplyTheme(ElementTheme.Dark);
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
    public void ThemeManager_ApplyAccent_PaletteSwitchingStress(string accent)
    {
        var manager = ThemeManager.Instance;
        manager.ApplyAccent(accent);
        Assert.Equal(accent, manager.CurrentAccent);
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

        // Settings Section Header, Badges & About Cards tests
        Assert.Equal("Hệ Thống Cốt Lõi", manager.GetTranslationForLanguage("Core System", AppLanguage.Vietnamese));
        Assert.Equal("Aura Studio", manager.GetTranslationForLanguage("Aura Studio", AppLanguage.Vietnamese));
        Assert.Equal("Tự Động Hóa", manager.GetTranslationForLanguage("Automation", AppLanguage.Vietnamese));
        Assert.Equal("Giám Sát", manager.GetTranslationForLanguage("Monitoring", AppLanguage.Vietnamese));
        Assert.Equal("Lá Chắn An Toàn", manager.GetTranslationForLanguage("Safety Shield", AppLanguage.Vietnamese));
        Assert.Equal("Động Cơ Lưu Trữ", manager.GetTranslationForLanguage("Storage Engine", AppLanguage.Vietnamese));
        Assert.Equal("Mạng Phân Phối CDN", manager.GetTranslationForLanguage("Release CDN", AppLanguage.Vietnamese));
        Assert.Equal("Bàn Làm Việc Kỹ Thuật", manager.GetTranslationForLanguage("Workbench", AppLanguage.Vietnamese));
        Assert.Equal("Hồ Sơ Cấu Hình", manager.GetTranslationForLanguage("Profiles", AppLanguage.Vietnamese));
        Assert.Equal("Bộ Nova Suite", manager.GetTranslationForLanguage("Nova Suite", AppLanguage.Vietnamese));
        Assert.Equal("Bộ Evolution Suite", manager.GetTranslationForLanguage("Evolution Suite", AppLanguage.Vietnamese));
        Assert.Equal("Kiến Trúc Sư Hệ Thống Trưởng", manager.GetTranslationForLanguage("Lead Systems Architect", AppLanguage.Vietnamese));
        Assert.Equal("Kỹ Sư Phần Mềm Chính • Tác Giả & Phát Triển WinCare Pro", manager.GetTranslationForLanguage("Principal Software Engineer • Creator & Maintainer of WinCare Pro", AppLanguage.Vietnamese));
        Assert.Equal("Gửi Email Cho Nhà Phát Triển", manager.GetTranslationForLanguage("Email Developer", AppLanguage.Vietnamese));
        Assert.Equal("Sao Chép Email Liên Hệ", manager.GetTranslationForLanguage("Copy Contact Email", AppLanguage.Vietnamese));
        Assert.Equal("Báo Cáo Lỗi / Góp Ý", manager.GetTranslationForLanguage("Report Bug / Issue", AppLanguage.Vietnamese));
        Assert.Equal("Cam Kết Quyền Riêng Tư & Không Thu Thập Dữ Liệu (Zero-Telemetry)", manager.GetTranslationForLanguage("Zero-Telemetry & Privacy Guarantee Pledge", AppLanguage.Vietnamese));
        Assert.Equal("BỘ NHỚ TIẾN TRÌNH", manager.GetTranslationForLanguage("PROCESS WORKING SET", AppLanguage.Vietnamese));
        Assert.Equal("THỜI GIAN PHIÊN HOẠT ĐỘNG", manager.GetTranslationForLanguage("ACTIVE SESSION UPTIME", AppLanguage.Vietnamese));
        Assert.Equal("ĐỘNG CƠ LƯU TRỮ CỤC BỘ", manager.GetTranslationForLanguage("LOCAL STORAGE ENGINE", AppLanguage.Vietnamese));
        Assert.Equal("WAL Mã Hóa • Tốt", manager.GetTranslationForLanguage("Encrypted WAL • Healthy", AppLanguage.Vietnamese));
        Assert.Equal("Dọn Dẹp Bộ Nhớ Đã Chọn", manager.GetTranslationForLanguage("Purge Selected Storage", AppLanguage.Vietnamese));
        Assert.Equal("Kênh Phân Phối WinCare Pro", manager.GetTranslationForLanguage("WinCare Pro Distribution Channel", AppLanguage.Vietnamese));
        Assert.Equal("Tải từ Trang Web", manager.GetTranslationForLanguage("Download from Website", AppLanguage.Vietnamese));
        Assert.Equal("Đã Kết Nối CDN", manager.GetTranslationForLanguage("CDN Connected", AppLanguage.Vietnamese));
        Assert.Equal("Trạng Thái Đồng Bộ Dữ Liệu", manager.GetTranslationForLanguage("Data Synchronization Status", AppLanguage.Vietnamese));
        Assert.Equal("Chẩn Đoán Trực Tiếp & Kiểm Soát Bộ Nhớ Cho Nhà Phát Triển", manager.GetTranslationForLanguage("Live Developer Diagnostics & Memory Control", AppLanguage.Vietnamese));
        Assert.Equal("Giải Phóng RAM & Thu Gom Rác GC", manager.GetTranslationForLanguage("Trim RAM & Force GC", AppLanguage.Vietnamese));
        Assert.Equal("Xem Nhật Ký Kiểm Toán SQLite", manager.GetTranslationForLanguage("View SQLite Audit Logs", AppLanguage.Vietnamese));
        Assert.Equal("Xuất Chẩn Đoán (JSON)", manager.GetTranslationForLanguage("Export Diagnostics (JSON)", AppLanguage.Vietnamese));
        Assert.Equal("14 Phân Hệ Cốt Lõi", manager.GetTranslationForLanguage("14 Core Modules", AppLanguage.Vietnamese));

        // Dynamic regex tests
        Assert.Equal("Lần kiểm tra cuối: Vừa xong", manager.GetTranslationForLanguage("Last Checked: Just now", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 5.1.1 (Polaris) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 5.1.1 (Polaris) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 5.0.0 (Polaris) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 5.0.0 (Polaris) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 4.9.3 (Orion) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 4.9.3 (Orion) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 4.9.2 (Orion) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 4.9.2 (Orion) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 4.9.1 (Nova) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 4.9.1 (Nova) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Phiên bản 4.9.0 (Nova) • Bộ Công Cụ Hệ Thống 64-bit Native", manager.GetTranslationForLanguage("Version 4.9.0 (Nova) • 64-bit Native System Suite", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v5.1.1", manager.GetTranslationForLanguage("What's New in v5.1.1", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v5.0.0", manager.GetTranslationForLanguage("What's New in v5.0.0", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v4.9.3", manager.GetTranslationForLanguage("What's New in v4.9.3", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v4.9.2", manager.GetTranslationForLanguage("What's New in v4.9.2", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v4.9.1", manager.GetTranslationForLanguage("What's New in v4.9.1", AppLanguage.Vietnamese));
        Assert.Equal("Điểm mới trong v4.9", manager.GetTranslationForLanguage("What's New in v4.9", AppLanguage.Vietnamese));
    }

    [Fact]
    public void Translations_StartupEmptyStates_ShouldBeLocalized()
    {
        var manager = TranslationManager.Instance;
        Assert.Equal("Khởi động & Dịch vụ", manager.GetTranslationForLanguage("Startup & Services", AppLanguage.Vietnamese));
    }

    [Fact]
    public void Translations_AiDiagnosticsAndNetwork_ShouldBeLocalizedAndReversible()
    {
        var manager = TranslationManager.Instance;

        // AI Diagnostics Engine header
        Assert.Equal("Động cơ chẩn đoán thông minh", manager.GetTranslationForLanguage("Smart Diagnostics Engine", AppLanguage.Vietnamese));
        Assert.Equal("Lõi sức khỏe hệ thống", manager.GetTranslationForLanguage("System Health Core", AppLanguage.Vietnamese));
        Assert.Equal("Chẩn đoán thông minh đa yếu tố & đề xuất sức khỏe hệ thống.", manager.GetTranslationForLanguage("Intelligent multi-factor diagnostics & system health recommendations.", AppLanguage.Vietnamese));

        // Network Center & Diagnostics
        Assert.Equal("Trung tâm Mạng & Chẩn đoán", manager.GetTranslationForLanguage("Network Center & Diagnostics", AppLanguage.Vietnamese));
        Assert.Equal("Trung tâm Mạng & Chẩn đoán", manager.GetTranslationForLanguage("Network Center &amp; Diagnostics", AppLanguage.Vietnamese));
        Assert.Equal("Tự động sửa chữa mạng thông minh", manager.GetTranslationForLanguage("Smart Network Auto-Repair", AppLanguage.Vietnamese));
        Assert.Equal("Đặt lại TCP/IP", manager.GetTranslationForLanguage("Reset TCP/IP", AppLanguage.Vietnamese));

        // Software Updater Empty State
        Assert.Equal("Tất cả ứng dụng đã mới nhất", manager.GetTranslationForLanguage("All Applications Up to Date", AppLanguage.Vietnamese));
        Assert.Equal("Hệ thống không có bản cập nhật phần mềm nào đang chờ.", manager.GetTranslationForLanguage("No pending software updates detected for your system.", AppLanguage.Vietnamese));
        Assert.Equal("Kho lưu trữ WinGet & Kênh cập nhật trực tiếp đã đồng bộ", manager.GetTranslationForLanguage("WinGet Repository & Direct Channel Synced", AppLanguage.Vietnamese));
        Assert.Equal("Kho lưu trữ WinGet & Kênh cập nhật trực tiếp đã đồng bộ", manager.GetTranslationForLanguage("WinGet Repository &amp; Direct Channel Synced", AppLanguage.Vietnamese));

        // Bi-directional reversibility
        Assert.Equal("Smart Diagnostics Engine", manager.GetTranslationForLanguage("Động cơ chẩn đoán thông minh", AppLanguage.English));
        Assert.Equal("System Health Core", manager.GetTranslationForLanguage("Lõi sức khỏe hệ thống", AppLanguage.English));
        Assert.Equal("Network Center & Diagnostics", manager.GetTranslationForLanguage("Trung tâm Mạng & Chẩn đoán", AppLanguage.English));
        Assert.Equal("All Applications Up to Date", manager.GetTranslationForLanguage("Tất cả ứng dụng đã mới nhất", AppLanguage.English));
    }

    [Fact]
    public void ThemeManager_MotionPreferenceChanged_FiresEvent()
    {
        var manager = ThemeManager.Instance;
        bool motionEventFired = false;
        EventHandler handler = (s, e) => motionEventFired = true;

        manager.MotionPreferenceChanged += handler;
        try
        {
            manager.NotifyMotionPreferenceChanged();
            Assert.True(motionEventFired);
            _ = manager.IsReducedMotion;
        }
        finally
        {
            manager.MotionPreferenceChanged -= handler;
        }
    }

    [Fact]
    public void AppXaml_DesignTokens_CornerRadiusAndSpacingMustBeDefined()
    {
        var current = AppContext.BaseDirectory;
        string? root = null;
        while (!string.IsNullOrEmpty(current))
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(current, "WinCarePro.csproj")))
            {
                root = current;
                break;
            }
            var parent = System.IO.Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        Assert.NotNull(root);
        var appXamlPath = System.IO.Path.Combine(root, "App.xaml");
        Assert.True(System.IO.File.Exists(appXamlPath));

        var xdoc = System.Xml.Linq.XDocument.Load(appXamlPath);
        var keys = xdoc.Descendants()
            .Select(e => e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet();

        Assert.Contains("AppControlCornerRadius", keys);
        Assert.Contains("AppCardCornerRadius", keys);
        Assert.Contains("AppSubCardCornerRadius", keys);
        Assert.Contains("AppBadgeCornerRadius", keys);
        Assert.Contains("AppDialogCornerRadius", keys);
        Assert.Contains("AppPillCornerRadius", keys);
        Assert.Contains("AppPagePadding", keys);
        Assert.Contains("AppCardPadding", keys);
        Assert.Contains("DestructiveButtonStyle", keys);
        Assert.Contains("StringFormatConverter", keys);
    }

    [Fact]
    public void StringFormatConverter_ShouldFormatStringProperly()
    {
        var converter = new WinCarePro.StringFormatConverter();

        // With parameter
        var formatted = converter.Convert("120 GB", typeof(string), "Đã dùng: {0}", "vi-VN");
        Assert.Equal("Đã dùng: 120 GB", formatted);

        // With null value
        var formattedNull = converter.Convert(null!, typeof(string), "Đã dùng: {0}", "vi-VN");
        Assert.Equal("Đã dùng: ", formattedNull);

        // Without parameter
        var formattedNoParam = converter.Convert("TestVal", typeof(string), null!, "vi-VN");
        Assert.Equal("TestVal", formattedNoParam);

        // ConvertBack returns UnsetValue
        var back = converter.ConvertBack("val", typeof(string), null!, "vi-VN");
        Assert.NotNull(back);
    }

    [Fact]
    public void TranslationManager_DialogAndPunctuationFallback_TranslatesCorrectly()
    {
        var manager = TranslationManager.Instance;

        // 1. Factory Reset dialog texts
        Assert.Equal("Đặt lại về mặc định gốc?", manager.GetTranslationForLanguage("Reset to Factory Defaults?", AppLanguage.Vietnamese));
        Assert.Equal("Đặt lại tất cả", manager.GetTranslationForLanguage("Reset All", AppLanguage.Vietnamese));
        Assert.Contains("Bạn có chắc chắn muốn đặt lại", manager.GetTranslationForLanguage("Are you sure you want to reset all configuration settings to factory defaults? This cannot be undone.", AppLanguage.Vietnamese));

        // 2. Dialog Badges
        Assert.Equal("NGUY HIỂM", manager.GetTranslationForLanguage("DESTRUCTIVE", AppLanguage.Vietnamese));
        Assert.Equal("XÁC NHẬN", manager.GetTranslationForLanguage("CONFIRMATION", AppLanguage.Vietnamese));
        Assert.Equal("THÀNH CÔNG", manager.GetTranslationForLanguage("SUCCESS", AppLanguage.Vietnamese));
        Assert.Equal("CẢNH BÁO", manager.GetTranslationForLanguage("WARNING", AppLanguage.Vietnamese));

        // 3. Punctuation fallback (string ending in ? without explicit ? entry)
        string testQuestion = manager.GetTranslationForLanguage("Check for Updates?", AppLanguage.Vietnamese);
        Assert.Equal("Kiểm tra cập nhật?", testQuestion);

        // 4. Reverse translation back to English
        Assert.Equal("Reset to Factory Defaults?", manager.GetTranslationForLanguage("Đặt lại về mặc định gốc?", AppLanguage.English));
        Assert.Contains(manager.GetTranslationForLanguage("NGUY HIỂM", AppLanguage.English), new[] { "DESTRUCTIVE", "Bad" });

        // 5. Dashboard Health & Registry translations from user reports
        Assert.Equal("Tình trạng sức khỏe hệ thống", manager.GetTranslationForLanguage("System Health Status", AppLanguage.Vietnamese));
        Assert.Equal("Tốt", manager.GetTranslationForLanguage("Good", AppLanguage.Vietnamese));
        Assert.Equal("Trung tâm Registry & Tối ưu AI", manager.GetTranslationForLanguage("Registry Center & AI Optimizer", AppLanguage.Vietnamese));
        Assert.Equal("Lá chắn Registry Zero-Trust", manager.GetTranslationForLanguage("Zero-Trust Registry Armor", AppLanguage.Vietnamese));
        Assert.Equal("Sửa an toàn 1 chạm", manager.GetTranslationForLanguage("1-Click Safe Fix", AppLanguage.Vietnamese));
        Assert.Equal("Dọn các mục đã chọn", manager.GetTranslationForLanguage("Clean Selected", AppLanguage.Vietnamese));
        Assert.Equal("Trình kiểm tra AI thông minh", manager.GetTranslationForLanguage("Smart AI Inspector", AppLanguage.Vietnamese));

        // 6. Polaris v5.2.0 Search, Vitality, Network segmented tabs, System Repair
        Assert.Equal("Tìm kiếm công cụ, chẩn đoán, cài đặt... (Ctrl + K)", manager.GetTranslationForLanguage("Search tools, diagnostics, settings... (Ctrl + K)", AppLanguage.Vietnamese));
        Assert.Contains(manager.GetTranslationForLanguage("VITALITY", AppLanguage.Vietnamese), new[] { "SỨC SỐNG", "Sức sống" });
        Assert.Equal("🚀 Băng thông & Radar", manager.GetTranslationForLanguage("🚀 Bandwidth & Radar", AppLanguage.Vietnamese));
        Assert.Equal("🌐 Trí tuệ DNS", manager.GetTranslationForLanguage("🌐 DNS Intelligence", AppLanguage.Vietnamese));
        Assert.Equal("⚡ Socket ứng dụng & Lưu lượng", manager.GetTranslationForLanguage("⚡ App Sockets & Traffic", AppLanguage.Vietnamese));
        Assert.Equal("🛠️ Tự phục hồi & Công cụ", manager.GetTranslationForLanguage("🛠️ Self-Healing & Tools", AppLanguage.Vietnamese));
        Assert.Equal("Sửa chữa Hệ thống & Tự phục hồi", manager.GetTranslationForLanguage("System Repair & Self-Healing", AppLanguage.Vietnamese));
        Assert.Equal("Động cơ Thông minh 6.0", manager.GetTranslationForLanguage("Smart Engine 6.0", AppLanguage.Vietnamese));
        Assert.Equal("Xác minh linh kiện SFC/DISM chuyên sâu, bỏ chặn registry an toàn và bảo vệ sao lưu hoàn tác tự động.", manager.GetTranslationForLanguage("Deep SFC/DISM component verification, registry safety unban, and automated rollback protection.", AppLanguage.Vietnamese));

        // 7. Dynamic emoji round-trip back to English
        Assert.Equal("🚀 Bandwidth & Radar", manager.GetTranslationForLanguage("🚀 Băng thông & Radar", AppLanguage.English));
        Assert.Equal("🌐 DNS Intelligence", manager.GetTranslationForLanguage("🌐 Trí tuệ DNS", AppLanguage.English));

        // 8. Polaris v5.2.0 Settings actions & Risk Advisory
        Assert.Equal("Đặt lại mặc định", manager.GetTranslationForLanguage("Reset Defaults", AppLanguage.Vietnamese));
        Assert.Equal("Xuất bản sao lưu", manager.GetTranslationForLanguage("Export Backup", AppLanguage.Vietnamese));
        Assert.Equal("Cảnh báo rủi ro thử nghiệm", manager.GetTranslationForLanguage("Experimental Risk Advisory", AppLanguage.Vietnamese));
        Assert.Equal("Tính năng tự động khắc phục phỏng đoán thực hiện các điều chỉnh hệ thống chạy ngầm mà không có thông báo xác nhận. Nên tắt đối với môi trường doanh nghiệp ổn định.", manager.GetTranslationForLanguage("Heuristic auto-remediations execute background system adjustments without prompt confirmations. Keep disabled for stable enterprise production environments.", AppLanguage.Vietnamese));

        // 9. Resource Insights Telemetry Banner
        Assert.Equal("Phát hiện mức sử dụng bộ xử lý cao. Hãy tối ưu các dịch vụ chạy ngầm và ứng dụng khởi động.", manager.GetTranslationForLanguage("High processor demand observed. Optimize background services and startup applications.", AppLanguage.Vietnamese));
        Assert.Equal("CPU TB: {0}% | RAM: {1}%", manager.GetTranslationForLanguage("Avg CPU: {0}% | RAM: {1}%", AppLanguage.Vietnamese));

        // 10. Settings Hub Sidebar Header
        Assert.Equal("TRUNG TÂM CÀI ĐẶT", manager.GetTranslationForLanguage("SETTINGS HUB", AppLanguage.Vietnamese));
        Assert.Equal("Tùy chọn & Hệ thống", manager.GetTranslationForLanguage("Preferences & System", AppLanguage.Vietnamese));
        Assert.Equal("SETTINGS HUB", manager.GetTranslationForLanguage("TRUNG TÂM CÀI ĐẶT", AppLanguage.English));
        Assert.Equal("Preferences & System", manager.GetTranslationForLanguage("Tùy chọn & Hệ thống", AppLanguage.English));
    }

    [Fact]
    public void TranslationManager_ModularPartialClassArchitecture_ZeroBreakingChange()
    {
        var manager = TranslationManager.Instance;
        manager.CurrentLanguage = AppLanguage.Vietnamese;

        // 1. Indexer API: TranslationManager.Instance[key]
        Assert.Equal("Tổng quan", manager["Dashboard"]);
        Assert.Equal("BẢNG ĐIỀU KHIỂN HỆ THỐNG", manager["SYSTEM COCKPIT"]);

        // 2. GetString API: TranslationManager.Instance.GetString(key)
        Assert.Equal("Tổng quan", manager.GetString("Dashboard"));
        Assert.Equal("Dọn dẹp", manager.GetString("Clean"));

        // 3. Culture dictionaries
        var viDict = manager.GetTranslations("vi-VN");
        var enDict = manager.GetTranslations("en-US");
        Assert.NotNull(viDict);
        Assert.NotNull(enDict);
        Assert.True(viDict.Count >= 3300, $"Expected >= 3300 keys in viDict, got {viDict.Count}");
        Assert.True(enDict.Count >= 3300, $"Expected >= 3300 keys in enDict, got {enDict.Count}");

        // 4. TranslationConverter verification
        var converter = new TranslationConverter();
        var converted = converter.Convert("Dashboard", typeof(string), null!, "vi-VN");
        Assert.Equal("Tổng quan", converted);

        // 5. Test keys across all 15 modular subsets
        // Common
        Assert.Equal("Đang chuẩn bị", manager["Preparing"]);
        // Dashboard
        Assert.Equal("Lõi trực tuyến", manager["Core Online"]);
        // AiAssistant
        Assert.Equal("Chẩn đoán thông minh", manager["Smart Diagnostics & AI Health Advisor"]);
        // DiskCleaner
        Assert.Equal("Dọn dẹp & Lưu trữ", manager["Clean & Storage"]);
        // StartupManager
        Assert.Equal("Tăng tốc & Khởi động", manager["Speed & Startup"]);
        // SystemOptimizer
        Assert.Equal("Bác sĩ Hệ thống", manager["System Doctor"]);
        // Registry
        Assert.Equal("Dọn dẹp Registry", manager["Registry Cleaner"]);
        // Repair
        Assert.Equal("Sửa chữa Hệ thống & Tự phục hồi", manager["System Repair & Self-Healing"]);
        // Network
        Assert.Equal("Trung tâm Mạng", manager["Network Center"]);
        // Security
        Assert.Equal("Bảo vệ an ninh & quyền riêng tư Windows.", manager["Windows security & privacy protection."]);
        // Uninstall
        Assert.Equal("Cập nhật Phần mềm", manager["Software Updater"]);
        // Updates
        Assert.Equal("Ghi Chú Phát Hành & Nhật Ký Thay Đổi", manager["Release Notes & Changelog"]);
        // DesktopWidget
        Assert.Equal("Widget HUD", manager["Widget HUD"]);
        // Notifications
        Assert.Equal("Thông báo", manager["Notifications"]);
        // Settings
        Assert.Equal("Tối ưu hóa", manager["System Optimizer"]);

        // Reset to English
        manager.CurrentLanguage = AppLanguage.English;
        Assert.Equal("Dashboard", manager["Dashboard"]);
    }
}


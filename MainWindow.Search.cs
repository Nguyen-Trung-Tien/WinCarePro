using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Database;
using WinCarePro.Services;

namespace WinCarePro;

public sealed partial class MainWindow : Window
{
    private List<SearchItem> _searchRegistry = new();

    private void PopulateSearchRegistry()
    {
        _searchRegistry = new List<SearchItem>
        {
            new SearchItem { Title = "Dashboard".T(), Description = "Real-time performance monitors, telemetry and system health overview.".T(), PageTag = "Dashboard", Keywords = "home diagnostic dashboard trang chu main telemetry monitor cpu ram gpu disk", IconGlyph = "\uE80F" },
            new SearchItem { Title = "AI WinCare Engine".T(), Description = "Neural diagnostics, AI health score, bottleneck detection and smart recommendations.".T(), PageTag = "AiWinCareEngine", Keywords = "ai wincare engine assistant neural intelligence tro ly ao chan doan thong minh", IconGlyph = "\uE99A" },
            new SearchItem { Title = "Junk Cleaner".T(), Description = "Clean temporary files, browser debris, memory dumps, and recycle bin.".T(), PageTag = "Junk", Keywords = "clean junk temp cache trash don rac cache xoa file rac", IconGlyph = "\uEA99" },
            new SearchItem { Title = "App Uninstaller".T(), Description = "Uninstall desktop software, UWP apps, and purge leftovers completely.".T(), PageTag = "Uninstall", Keywords = "uninstall remove program app uninstaller go ung dung xoa phan mem", IconGlyph = "\uE74D" },
            new SearchItem { Title = "Network Center".T(), Description = "Ping benchmarks, speed tests, DNS latency optimizer, and TCP/IP repair.".T(), PageTag = "Network", Keywords = "network ping dns benchmark speed test internet mang wifi bandwidth", IconGlyph = "\uE701" },
            new SearchItem { Title = "System Repair".T(), Description = "Diagnose and fix Windows components, SFC scan, DISM repair, and service restoration.".T(), PageTag = "Repair", Keywords = "repair sfc dism fix component system sua loi he thong windows", IconGlyph = "\uE90F" },
            new SearchItem { Title = "Security Shield".T(), Description = "Evaluate security integrity, Windows Defender, TPM/Secure Boot, and privacy safeguards.".T(), PageTag = "Security", Keywords = "security defender shield privacy firewalls bao mat quyen rieng tu", IconGlyph = "\uE727" },
            new SearchItem { Title = "System Optimizer".T(), Description = "Kernel response tuning, RAM working set purger, latency reduction, and system acceleration.".T(), PageTag = "Optimizer", Keywords = "optimizer ram speed performance memory boost toi uu toc do tang toc", IconGlyph = "\uE7FC" },
            new SearchItem { Title = "Context Menu".T(), Description = "Clean shell extensions, declutter right-click menus, and accelerate Windows Explorer.".T(), PageTag = "ContextMenu", Keywords = "context menu right click explorer shell extension menu chuot phai", IconGlyph = "\uE8EC" },
            new SearchItem { Title = "Disk Tools".T(), Description = "Analyze storage layout, S.M.A.R.T attributes, and clean duplicate files.".T(), PageTag = "Disk", Keywords = "disk storage folder analysis duplicates o dia don o dia", IconGlyph = "\uE7F1" },
            new SearchItem { Title = "Registry Center".T(), Description = "Backup, restore, and repair broken system registry keys and invalid CLSIDs.".T(), PageTag = "Registry", Keywords = "registry backup hive restore scan database quan ly registry don registry", IconGlyph = "\uEDA2" },
            new SearchItem { Title = "Software Updater".T(), Description = "Check and install updates for installed applications via WinGet.".T(), PageTag = "Updater", Keywords = "software updater winget upgrade phan mem cap nhat update", IconGlyph = "\uE895" },
            new SearchItem { Title = "Notifications".T(), Description = "Review system alerts, optimization history, and activity logs.".T(), PageTag = "notification", Keywords = "notifications activity log alerts thong bao nhat ky lich su", IconGlyph = "\uEA8F" },
            new SearchItem { Title = "Settings".T(), Description = "Personalization configurations, theme switching, language options, and background scheduling.".T(), PageTag = "Settings:0", Keywords = "settings theme accent transparent language config cai dat tuy chinh", IconGlyph = "\uE713" },
            
            // --- Settings Sections Search Entries ---
            new SearchItem { Title = "Settings: General & Language".T(), Description = "Application startup, system tray behavior, and instant language selection.".T(), PageTag = "Settings:0", Keywords = "language ngon ngu tieng viet tieng anh english startup khoi dong minimize thu nho tray he thong general config cai dat chung", IconGlyph = "\uE713" },
            new SearchItem { Title = "Settings: Appearance & Theme".T(), Description = "Custom theme mode, accent color palettes, acrylic transparency, and fluid UI.".T(), PageTag = "Settings:1", Keywords = "theme dark mode light mode acrylic mica color accent palette mau chu de giao dien gradient tuy bien", IconGlyph = "\uE790" },
            new SearchItem { Title = "Settings: Auto Maintenance".T(), Description = "Background auto cleanup intervals, smart optimization triggers, and silent maintenance.".T(), PageTag = "Settings:2", Keywords = "auto maintenance tu dong bao tri don rac auto clean schedule lich trinh silent mode", IconGlyph = "\uE812" },
            new SearchItem { Title = "Settings: Telemetry & Alert Policy".T(), Description = "System monitoring threshold limits, critical hardware notifications, and alerts.".T(), PageTag = "Settings:3", Keywords = "telemetry alerts thong bao canh bao cpu threshold ram smart privacy nguong giam sat", IconGlyph = "\uEA8F" },
            new SearchItem { Title = "Settings: Safety & Rollback".T(), Description = "Windows Restore Points creation, registry backup snapshots, and transactional safety.".T(), PageTag = "Settings:4", Keywords = "safety rollback restore point diem khoi phuc registry backup snapshot sao luu an toan", IconGlyph = "\uE727" },
            new SearchItem { Title = "Settings: Database & Storage".T(), Description = "Local database optimization, WAL log maintenance, and cache storage size management.".T(), PageTag = "Settings:5", Keywords = "database storage sqlite vacuum wal co so du lieu don dep logs nhat ky storage size", IconGlyph = "\uE7F1" },
            new SearchItem { Title = "Settings: Software Updates".T(), Description = "CDN distribution channel, automated update polling, and release updates.".T(), PageTag = "Settings:6", Keywords = "update software cap nhat phan mem cdn channel beta auto check kiem tra cap nhat changelog", IconGlyph = "\uE75C" },
            new SearchItem { Title = "Settings: Advanced & Developer Workbench".T(), Description = "Process working set RAM trimmer, forced GC collection, environment inspector, and audit logs.".T(), PageTag = "Settings:7", Keywords = "developer workbench trim ram force gc inspect clr audit logs sandbox debug plugin go loi nha phat trien don ram", IconGlyph = "\uE7B4" },
            new SearchItem { Title = "Settings: Backup & Reset".T(), Description = "Export and import application configuration profiles, or reset settings to defaults.".T(), PageTag = "Settings:8", Keywords = "backup reset restore defaults sao luu khoi phuc mac dinh dat lai export import cau hinh", IconGlyph = "\uE8AC" },
            new SearchItem { Title = "Settings: About & Developer".T(), Description = "System architecture, Lead Developer portfolio, zero-telemetry guarantee pledge, and what's new.".T(), PageTag = "Settings:9", Keywords = "about developer thong tin tac gia nguyen trung tien portfolio privacy pledge cam ket rieng tu whats new phien ban", IconGlyph = "\uE946" },
            new SearchItem { Title = "Settings: Feature Guide & Manual".T(), Description = "Comprehensive step-by-step visual handbook and safety guidelines for all 14 modules.".T(), PageTag = "Settings:10", Keywords = "user guide manual huong dan su dung handbook document so tay huong dan chi tiet", IconGlyph = "\uE897" }
        };
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (char c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLower();
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string rawQuery = sender.Text.Trim();
            if (string.IsNullOrEmpty(rawQuery))
            {
                sender.ItemsSource = null;
                return;
            }

            string cleanQuery = RemoveDiacritics(rawQuery);
            var results = new List<SearchItemScore>();

            foreach (var item in _searchRegistry)
            {
                string cleanTitle = RemoveDiacritics(item.Title);
                string cleanDesc = RemoveDiacritics(item.Description);
                string cleanKeywords = RemoveDiacritics(item.Keywords);

                int score = 0;
                if (cleanTitle.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 100; // Exact match
                }
                else if (cleanTitle.StartsWith(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 80;
                }
                else if (cleanTitle.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 60;
                }
                else if (cleanKeywords.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 40;
                }
                else if (cleanDesc.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 20;
                }

                if (score > 0)
                {
                    results.Add(new SearchItemScore { Item = item, Score = score });
                }
            }

            sender.ItemsSource = results.OrderByDescending(x => x.Score).Select(x => x.Item).ToList();
        }
    }

    private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchItem item)
        {
            if (!string.IsNullOrEmpty(item.PageTag) && RootFrame.Content is MainPage mainPage)
            {
                mainPage.NavigateToPageExternal(item.PageTag);
            }
        }
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchItem item)
        {
            if (!string.IsNullOrEmpty(item.PageTag) && RootFrame.Content is MainPage mainPage)
            {
                mainPage.NavigateToPageExternal(item.PageTag);
            }
            return;
        }

        string query = sender.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        string cleanQuery = RemoveDiacritics(query);
        var firstMatch = _searchRegistry
            .Select(i => new { Item = i, CleanTitle = RemoveDiacritics(i.Title), CleanKeywords = RemoveDiacritics(i.Keywords) })
            .FirstOrDefault(x => x.CleanTitle.Contains(cleanQuery) || x.CleanKeywords.Contains(cleanQuery));

        if (firstMatch != null && RootFrame.Content is MainPage mainPage2)
        {
            mainPage2.NavigateToPageExternal(firstMatch.Item.PageTag);
        }
    }

    private class SearchItemScore
    {
        public SearchItem Item { get; set; } = null!;
        public int Score { get; set; }
    }

    private void UserGuideButton_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is MainPage mainPage)
        {
            mainPage.NavigateToUserGuide();
        }
    }

    private void NotificationButton_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is MainPage mainPage)
        {
            mainPage.NavigateToNotificationPage();
            DbManager.MarkAllNotificationsAsRead();
            UpdateNotificationBadge();
        }
    }
}


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
            new SearchItem { Title = "AI Assistant".T(), Description = "Neural diagnostics, AI health score, bottleneck detection and smart recommendations.".T(), PageTag = "AiCopilot", Keywords = "ai copilot assistant neural intelligence tro ly ao chan doan thong minh", IconGlyph = "\uE99A" },
            new SearchItem { Title = "Junk Cleaner".T(), Description = "Clean temporary files, browser debris, memory dumps, and recycle bin.".T(), PageTag = "Junk", Keywords = "clean junk temp cache trash don rac cache xoa file rac", IconGlyph = "\uEA99" },
            new SearchItem { Title = "App Uninstaller".T(), Description = "Uninstall desktop software, UWP apps, and purge leftovers completely.".T(), PageTag = "Uninstall", Keywords = "uninstall remove program app uninstaller go ung dung xoa phan mem", IconGlyph = "\uE74D" },
            new SearchItem { Title = "Network Center".T(), Description = "Ping benchmarks, speed tests, DNS latency optimizer, and TCP/IP repair.".T(), PageTag = "Network", Keywords = "network ping dns benchmark speed test internet mang wifi bandwidth", IconGlyph = "\uE701" },
            new SearchItem { Title = "System Repair".T(), Description = "Diagnose and fix Windows components, SFC scan, DISM repair, and service restoration.".T(), PageTag = "Repair", Keywords = "repair sfc dism fix component system sua loi he thong windows", IconGlyph = "\uE90F" },
            new SearchItem { Title = "Security Shield".T(), Description = "Evaluate security integrity, Windows Defender, TPM/Secure Boot, and privacy safeguards.".T(), PageTag = "Security", Keywords = "security defender shield privacy firewalls bao mat quyen rieng tu", IconGlyph = "\uE727" },
            new SearchItem { Title = "System Optimizer".T(), Description = "Kernel response tuning, RAM working set purger, latency reduction, and gaming acceleration.".T(), PageTag = "Optimizer", Keywords = "optimizer ram speed performance memory boost toi uu toc do tang toc", IconGlyph = "\uE7FC" },
            new SearchItem { Title = "Gaming Turbo".T(), Description = "Ultra low-latency gaming engine, process priority booster, and FPS acceleration.".T(), PageTag = "GamingTurbo", Keywords = "gaming turbo fps game boost play latency lag tang toc game", IconGlyph = "\uE785" },
            new SearchItem { Title = "Context Menu".T(), Description = "Clean shell extensions, declutter right-click menus, and accelerate Windows Explorer.".T(), PageTag = "ContextMenu", Keywords = "context menu right click explorer shell extension menu chuot phai", IconGlyph = "\uE8EC" },
            new SearchItem { Title = "Startup & Services".T(), Description = "Configure system startup apps, background services, and delay triggers.".T(), PageTag = "Startup", Keywords = "startup services boot background tasks khoi dong dich vu", IconGlyph = "\uE7B5" },
            new SearchItem { Title = "Disk Tools".T(), Description = "Analyze storage layout, S.M.A.R.T attributes, and clean duplicate files.".T(), PageTag = "Disk", Keywords = "disk storage folder analysis duplicates o dia don o dia", IconGlyph = "\uE7F1" },
            new SearchItem { Title = "Registry Center".T(), Description = "Backup, restore, and repair broken system registry keys and invalid CLSIDs.".T(), PageTag = "Registry", Keywords = "registry backup hive restore scan database quan ly registry don registry", IconGlyph = "\uEDA2" },
            new SearchItem { Title = "Software Updater".T(), Description = "Check and install updates for installed applications via WinGet.".T(), PageTag = "Updater", Keywords = "software updater winget upgrade phan mem cap nhat update", IconGlyph = "\uE895" },
            new SearchItem { Title = "Notifications".T(), Description = "Review system alerts, optimization history, and activity logs.".T(), PageTag = "notification", Keywords = "notifications activity log alerts thong bao nhat ky lich su", IconGlyph = "\uEA8F" },
            new SearchItem { Title = "Settings".T(), Description = "Personalization configurations, theme switching, language options, and background scheduling.".T(), PageTag = "Settings", Keywords = "settings theme accent transparent language config cai dat tuy chinh", IconGlyph = "\uE713" }
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


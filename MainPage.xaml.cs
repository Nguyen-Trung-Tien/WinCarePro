using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinCarePro.Views;
using WinCarePro.Services;

namespace WinCarePro;

public sealed partial class MainPage : Page
{
    public Frame NavigationFrame => ContentFrame;

    public MainPage()
    {
        InitializeComponent();
        
        // Populate user chip with system info
        NavUserName.Text = Environment.UserName;
        NavMachineName.Text = Environment.MachineName;

        // Register to theme changes to force update RequestedTheme for this page, children, and navigated content
        ThemeManager.Instance.ThemeChanged += (s, e) =>
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            this.RequestedTheme = theme;
            NavView.RequestedTheme = theme;
            if (ContentFrame.Content is Page page)
            {
                page.RequestedTheme = theme;
            }
        };
        // Apply initial theme
        var initialTheme = ThemeManager.Instance.CurrentTheme;
        this.RequestedTheme = initialTheme;
        NavView.RequestedTheme = initialTheme;
        
        // Register to language changes to force translation for this page and current navigated page
        TranslationManager.Instance.LanguageChanged += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            if (ContentFrame.Content is Page currentPage)
            {
                TranslationManager.Instance.Translate(currentPage);
            }
        };
        
        // Auto-translate and synchronize theme for navigated pages
        ContentFrame.Navigated += (s, e) =>
        {
            if (e.Content is Page page)
            {
                page.RequestedTheme = ThemeManager.Instance.CurrentTheme;
                TranslationManager.Instance.Translate(page);
                page.Loaded += (sender, args) => TranslationManager.Instance.Translate(page);

                page.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    TranslationManager.Instance.Translate(page);
                });
            }
        };

        // Load default page on startup
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        NavigateToPage("Dashboard");

        // Load animations setting
        LoadAnimationsConfiguration();

        // Translate this container page
        this.Loaded += (s, e) => TranslationManager.Instance.Translate(this);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateToPage("Settings");
        }
        else if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateToPage(tag);
        }
    }

    public void NavigateToPageExternal(string tag)
    {
        if (tag.Equals("Settings", StringComparison.OrdinalIgnoreCase))
        {
            NavView.SelectedItem = NavView.SettingsItem;
            NavigateToPage("Settings");
            return;
        }

        var menuItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(x => x.Tag?.ToString()?.Equals(tag, StringComparison.OrdinalIgnoreCase) == true);
        
        if (menuItem != null)
        {
            NavView.SelectedItem = menuItem;
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string tag)
    {
        Type? pageType = tag.ToLower() switch
        {
            "dashboard" => typeof(DashboardPage),
            "aicopilot" => typeof(WinCarePro.Modules.AiAssistant.AiCopilotPage),
            "gamingturbo" => typeof(WinCarePro.Modules.GamingTurbo.GamingTurboPage),
            "junk" => typeof(JunkPage),
            "uninstall" => typeof(UninstallPage),
            "network" => typeof(NetworkPage),
            "repair" => typeof(RepairPage),
            "security" => typeof(SecurityPage),
            "optimizer" => typeof(SystemOptimizerPage),
            "contextmenu" => typeof(ContextMenuPage),
            "startup" => typeof(StartupPage),
            "disk" => typeof(DiskPage),
            "registry" => typeof(RegistryPage),
            "updater" => typeof(UpdaterPage),
            "settings" => typeof(SettingsPage),
            "notification" => typeof(NotificationPage),
            _ => null
        };

        if (pageType != null)
        {
            if (ContentFrame.CurrentSourcePageType == pageType) return;

            ContentFrame.Navigate(pageType);
            
            // Set header text using the centralized UpdateHeader method
            UpdateHeader();
        }
    }

    public void NavigateToNotificationPage()
    {
        NavView.SelectedItem = null;
        NavigateToPage("notification");
    }

    private void LoadAnimationsConfiguration()
    {
        try
        {
            string raw = Database.DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("EnableAnimations", out var animProp))
                {
                    ApplyAnimationsEnabled(animProp.GetBoolean());
                }
            }
        }
        catch { }
    }

    public void ApplyAnimationsEnabled(bool enabled)
    {
        if (enabled)
        {
            if (ContentFrame.ContentTransitions == null || ContentFrame.ContentTransitions.Count == 0)
            {
                ContentFrame.ContentTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
                {
                    new Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition
                    {
                        DefaultNavigationTransitionInfo = new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo
                        {
                            Effect = Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromRight
                        }
                    }
                };
            }
        }
        else
        {
            ContentFrame.ContentTransitions = null;
        }
    }

    public void UpdateHeader()
    {
        NavView.Header = null;
    }
}

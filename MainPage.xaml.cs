using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinCarePro.Views;
using WinCarePro.Services;

namespace WinCarePro;

public sealed partial class MainPage : Page
{
    public Frame NavigationFrame => ContentFrame;

    private Page? _lastTranslatedPage;
    private RoutedEventHandler? _lastLoadedHandler;
    private readonly EventHandler _themeChangedHandler;
    private readonly EventHandler _accentChangedHandler;
    private readonly EventHandler _languageChangedHandler;
    private readonly EventHandler<WinCarePro.Services.Contracts.SettingsChangedEventArgs> _settingsChangedHandler;
    private SplitView? _splitView;

    public MainPage()
    {
        InitializeComponent();
        
        // Populate user chip with system info
        NavUserName.Text = Environment.UserName;
        NavMachineName.Text = Environment.MachineName;
        ToolTipService.SetToolTip(UserProfileBorder, $"{Environment.UserName} • {Environment.MachineName}");

        // Register to theme changes to force update RequestedTheme for this page, children, and navigated content
        ThemeManager.Instance.RegisterPage(this);
        TranslationManager.Instance.RegisterPage(this);

        _themeChangedHandler = (s, e) =>
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                ApplyNavTheme(ThemeManager.Instance.CurrentTheme);
            });
        };
        ThemeManager.Instance.ThemeChanged += _themeChangedHandler;

        // Apply initial theme
        ApplyNavTheme(ThemeManager.Instance.CurrentTheme);

        // Register to accent color changes to dynamically update user avatar and brand gradients
        _accentChangedHandler = (s, e) =>
        {
            UpdateUserAvatarAccent();
        };
        ThemeManager.Instance.AccentChanged += _accentChangedHandler;
        UpdateUserAvatarAccent();
        
        // Register to language changes to force translation for this page and current navigated page
        _languageChangedHandler = (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            if (ContentFrame.Content is Page currentPage)
            {
                TranslationManager.Instance.Translate(currentPage);
            }
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;

        // Synchronize Nav menu background reactively with Settings (TransparencyLevel, BackdropType, AccentColor)
        _settingsChangedHandler = (s, e) =>
        {
            if (e.PropertyName is "TransparencyLevel" or "BackdropType" or "Theme" or "AccentColor")
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    var cur = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
                    UpdateNavTransparencyAndBackdrop(cur.TransparencyLevel, cur.BackdropType);
                });
            }
        };
        WinCarePro.Services.Implementations.SettingsService.Instance.SettingsChanged += _settingsChangedHandler;
        
        // Auto-translate and synchronize theme for navigated pages
        ContentFrame.Navigated += (s, e) =>
        {
            if (e.Content is Page page)
            {
                ThemeManager.Instance.RegisterPage(page);
                TranslationManager.Instance.RegisterPage(page);
                page.RequestedTheme = ThemeManager.Instance.CurrentTheme;

                // Detach previous page's Loaded handler to prevent memory leak
                if (_lastTranslatedPage != null && _lastLoadedHandler != null)
                {
                    _lastTranslatedPage.Loaded -= _lastLoadedHandler;
                }

                // Attach new handler: translation runs asynchronously on Loaded to keep entrance transition at 120 FPS
                _lastLoadedHandler = (sender, args) => TranslationManager.Instance.Translate(page);
                page.Loaded += _lastLoadedHandler;
                _lastTranslatedPage = page;

                // For cached pages that are already loaded in visual tree, Loaded does not re-fire
                if (page.IsLoaded)
                {
                    DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(page));
                }

                // Synchronize NavigationView back button & selection indicator
                NavView.IsBackEnabled = ContentFrame.CanGoBack;
                UpdateSelectedNavItem();
            }
        };

        // Load default page on startup
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        NavigateToPage("Dashboard");

        // Load animations setting
        LoadAnimationsConfiguration();

        NavView.Loaded += (s, e) =>
        {
            var cur = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
            UpdateNavTransparencyAndBackdrop(cur.TransparencyLevel, cur.BackdropType);
        };

        // Translate this container page and ensure sidebar theme consistency
        this.Loaded += (s, e) =>
        {
            ApplyNavTheme(ThemeManager.Instance.CurrentTheme);
            TranslationManager.Instance.Translate(this);
        };
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    public void UpdateNavTransparencyAndBackdrop(double transparencyLevel, string? backdropType)
    {
        bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
        double clampedLevel = Math.Clamp(transparencyLevel, 10.0, 100.0);
        double fraction = (clampedLevel - 10.0) / 90.0; // 0.0 to 1.0

        // Synchronize pane alpha with window transparency:
        // at 10% (solid/contrast): alpha ~165
        // at 100% (pure frosted glass): alpha ~18
        byte paneAlpha = (byte)Math.Clamp((int)(165 - (fraction * 145)), 18, 175);

        Brush paneBg;
        if (isDark)
        {
            var accent = ThemeManager.Instance.CurrentAccent?.ToLowerInvariant();
            if (accent == "cyberpunk" || accent == "neon")
            {
                paneBg = new SolidColorBrush(Windows.UI.Color.FromArgb(paneAlpha, 14, 12, 24));
            }
            else
            {
                paneBg = new SolidColorBrush(Windows.UI.Color.FromArgb(paneAlpha, 18, 20, 26));
            }
        }
        else
        {
            paneBg = new SolidColorBrush(Windows.UI.Color.FromArgb(paneAlpha, 243, 245, 249));
        }

        // Apply resource overrides for native WinUI 3 NavigationView template
        NavView.Resources["NavigationViewDefaultPaneBackground"] = paneBg;
        NavView.Resources["NavigationViewExpandedPaneBackground"] = paneBg;
        NavView.Resources["NavigationViewPaneBackground"] = paneBg;
        NavView.Resources["SplitViewPaneBackground"] = paneBg;

        // Apply directly to cached SplitView in O(1) without visual tree walking
        _splitView ??= FindVisualChild<SplitView>(NavView);
        if (_splitView != null)
        {
            _splitView.PaneBackground = paneBg;
        }

        // Synchronize UserProfileBorder in PaneFooter
        byte userChipAlpha = (byte)Math.Clamp(paneAlpha + 30, 35, 220);
        UserProfileBorder.Background = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(userChipAlpha, 26, 28, 38))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(userChipAlpha, 255, 255, 255));
        UserProfileBorder.BorderBrush = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(35, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(35, 0, 0, 0));
    }

    private void ApplyNavTheme(ElementTheme theme)
    {
        this.RequestedTheme = theme;
        NavView.RequestedTheme = theme;
        UserProfileBorder.RequestedTheme = theme;

        bool isDark = (theme == ElementTheme.Dark);
        var cur = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
        UpdateNavTransparencyAndBackdrop(cur.TransparencyLevel, cur.BackdropType);

        var itemFg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)) 
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42));
        var headerFg = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184)) 
                              : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85));

        NavUserName.Foreground = itemFg;
        NavMachineName.Foreground = headerFg;

        foreach (var item in NavView.MenuItems)
        {
            if (item is FrameworkElement fe)
            {
                fe.RequestedTheme = theme;
            }
        }
        foreach (var item in NavView.FooterMenuItems)
        {
            if (item is FrameworkElement fe)
            {
                fe.RequestedTheme = theme;
            }
        }
        if (NavView.SettingsItem is FrameworkElement settingsElem)
        {
            settingsElem.RequestedTheme = theme;
        }

        if (ContentFrame.Content is Page page)
        {
            page.RequestedTheme = theme;
        }
    }

    private void OnNavBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
        {
            CleanupActivePage();
            ContentFrame.GoBack();
            UpdateSelectedNavItem();
        }
    }

    public void UpdateSelectedNavItem()
    {
        if (ContentFrame.Content is not Page currentPage) return;
        var curType = currentPage.GetType();

        if (curType == typeof(SettingsPage))
        {
            NavView.SelectedItem = NavView.SettingsItem;
            return;
        }
        if (curType == typeof(NotificationPage))
        {
            NavView.SelectedItem = null;
            return;
        }

        string? targetTag = curType.Name switch
        {
            nameof(DashboardPage) => "Dashboard",
            "AiWinCareEnginePage" => "AiWinCareEngine",
            nameof(JunkPage) => "Junk",
            nameof(UninstallPage) => "Uninstall",
            nameof(NetworkPage) => "Network",
            nameof(RepairPage) => "Repair",
            nameof(SecurityPage) => "Security",
            nameof(SystemOptimizerPage) => "Optimizer",
            nameof(StartupPage) => "Startup",
            nameof(ContextMenuPage) => "ContextMenu",
            nameof(DiskPage) => "Disk",
            nameof(RegistryPage) => "Registry",
            nameof(UpdaterPage) => "Updater",
            _ => null
        };

        if (targetTag != null)
        {
            var match = NavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x => x.Tag?.ToString()?.Equals(targetTag, StringComparison.OrdinalIgnoreCase) == true);
            if (match != null)
            {
                NavView.SelectedItem = match;
            }
        }
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
        if (tag.StartsWith("Settings", StringComparison.OrdinalIgnoreCase))
        {
            int sectionIndex = 0;
            var parts = tag.Split(':');
            if (parts.Length > 1 && int.TryParse(parts[1], out int idx))
            {
                sectionIndex = idx;
            }

            NavView.SelectedItem = NavView.SettingsItem;
            NavigateToPage("Settings");

            if (ContentFrame.Content is SettingsPage settingsPage)
            {
                settingsPage.SelectSection(sectionIndex);
            }
            return;
        }

        if (tag.Equals("notification", StringComparison.OrdinalIgnoreCase))
        {
            NavigateToNotificationPage();
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
        else
        {
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string tag)
    {
        Type? pageType = tag.ToLower() switch
        {
            "dashboard" => typeof(DashboardPage),
            "aiwincareengine" => typeof(WinCarePro.Modules.AiAssistant.AiWinCareEnginePage),
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

            // Senior Optimization: Release memory, event listeners, and timers of previously active page
            CleanupActivePage();

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

    public void NavigateToUserGuide()
    {
        NavView.SelectedItem = NavView.SettingsItem;
        if (ContentFrame.Content is SettingsPage settingsPage)
        {
            settingsPage.SelectSection(10);
        }
        else
        {
            ContentFrame.Navigate(typeof(SettingsPage), 10);
            UpdateHeader();
        }
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
            ContentFrame.ContentTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection
            {
                new Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition
                {
                    DefaultNavigationTransitionInfo = new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo()
                }
            };
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

    public void Cleanup()
    {
        ThemeManager.Instance.ThemeChanged -= _themeChangedHandler;
        ThemeManager.Instance.AccentChanged -= _accentChangedHandler;
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        WinCarePro.Services.Implementations.SettingsService.Instance.SettingsChanged -= _settingsChangedHandler;
        ThemeManager.Instance.UnregisterPage(this);
        TranslationManager.Instance.UnregisterPage(this);

        if (_lastTranslatedPage != null && _lastLoadedHandler != null)
        {
            _lastTranslatedPage.Loaded -= _lastLoadedHandler;
            _lastTranslatedPage = null;
            _lastLoadedHandler = null;
        }
    }

    public void CleanupActivePage()
    {
        try
        {
            // Halt any running 3D holographic composition scan effects globally on navigation
            WinCarePro.Core.Helpers.Animation3DHelper.StopAll3DScanEffects();

            if (ContentFrame.Content is Page oldPage)
            {
                // Only unregister and dispose transient non-cached pages to prevent destroying cached ViewModels
                if (oldPage.NavigationCacheMode != Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required)
                {
                    ThemeManager.Instance.UnregisterPage(oldPage);
                    TranslationManager.Instance.UnregisterPage(oldPage);
                    if (oldPage.DataContext is IDisposable disposableVm)
                    {
                        disposableVm.Dispose();
                    }
                    else if (oldPage is IDisposable disposablePage)
                    {
                        disposablePage.Dispose();
                    }
                }
            }
        }
        catch { }
    }

    private void UpdateUserAvatarAccent()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (Application.Current.Resources.TryGetValue("CyberAccentGradient", out var brushObj) && brushObj is Microsoft.UI.Xaml.Media.Brush cyberBrush)
            {
                UserAvatarBorder.Background = null;
                UserAvatarBorder.Background = cyberBrush;
            }
        });
    }

    private void OnUserProfileClick(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        NavigateToPageExternal("settings");
    }
}

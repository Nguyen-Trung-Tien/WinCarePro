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
        
        // Auto-translate navigated pages
        ContentFrame.Navigated += (s, e) =>
        {
            if (e.Content is Page page)
            {
                if (page.IsLoaded)
                {
                    TranslationManager.Instance.Translate(page);
                }
                else
                {
                    page.Loaded += (sender, args) => TranslationManager.Instance.Translate(page);
                }
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
            "junk" => typeof(JunkPage),
            "uninstall" => typeof(UninstallPage),
            "network" => typeof(NetworkPage),
            "repair" => typeof(RepairPage),
            "security" => typeof(SecurityPage),
            "optimizer" => typeof(SystemOptimizerPage),
            "startup" => typeof(StartupPage),
            "process" => typeof(ProcessPage),
            "disk" => typeof(DiskPage),
            "hardware" => typeof(HardwarePage),
            "registry" => typeof(RegistryPage),
            "updater" => typeof(UpdaterPage),
            "driver" => typeof(DriverPage),
            "settings" => typeof(SettingsPage),
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
                    new Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition()
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

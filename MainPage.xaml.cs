using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinCarePro.Views;

namespace WinCarePro;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        
        // Load default page on startup
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        NavigateToPage("Dashboard");
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
            .FirstOrDefault(x => x.Tag?.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase) == true);
        
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
            ContentFrame.Navigate(pageType);
            
            // Set header text
            if (tag.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                NavView.Header = "Settings & Personalization";
            }
            else
            {
                var menuItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(x => x.Tag?.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase) == true);
                if (menuItem != null)
                {
                    NavView.Header = menuItem.Content;
                }
            }
        }
    }
}

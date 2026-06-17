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
        
        // Load default page
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

    private void NavigateToPage(string tag)
    {
        Type? pageType = tag.ToLower() switch
        {
            "dashboard" => typeof(DashboardPage),
            "junk" => typeof(JunkPage),
            "network" => typeof(NetworkPage),
            "repair" => typeof(RepairPage),
            "startup" => typeof(StartupPage),
            "process" => typeof(ProcessPage),
            "disk" => typeof(DiskPage),
            "security" => typeof(SecurityPrivacyPage),
            "registry" => typeof(RegistryBackupPage),
            "hardware" => typeof(HardwarePage),
            "logs" => typeof(LogsPage),
            "updater" => typeof(UpdaterPage),
            "settings" => typeof(SettingsPage),
            _ => null
        };

        if (pageType != null)
        {
            ContentFrame.Navigate(pageType);
            
            // Set header text
            if (tag == "Settings")
            {
                NavView.Header = "Settings & Preferences";
            }
            else
            {
                var menuItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(x => x.Tag?.ToString() == tag);
                if (menuItem != null)
                {
                    NavView.Header = menuItem.Content;
                }
            }
        }
    }
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;

namespace WinCarePro.Views;

public sealed partial class ContextMenuPage : Page
{
    public ContextMenuViewModel ViewModel { get; }

    public ContextMenuPage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ContextMenuViewModel>();
        this.DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ScanAsync();
    }

    private async void OnItemToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is ContextMenuItem item)
        {
            if (item.IsEnabled != toggle.IsOn)
            {
                bool success = await ViewModel.ToggleItemAsync(item, toggle.IsOn);
                if (!success)
                {
                    // Revert toggle switch state on UI if modification failed
                    toggle.IsOn = item.IsEnabled;
                }
            }
        }
    }

    internal bool IsNot(bool b) => !b;
}

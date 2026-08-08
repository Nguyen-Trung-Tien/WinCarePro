using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Components;

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

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsBusy))
            {
                UpdateLoadingOverlayState();
            }
        };

        this.Loaded += (s, e) => UpdateLoadingOverlayState();
    }

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsBusy)
        {
            LoadingOverlayGrid.Visibility = Visibility.Visible;
            FadeInLoading.Begin();
        }
        else
        {
            FadeOutLoading.Begin();
        }
    }

    private void FadeOutLoading_Completed(object? sender, object e)
    {
        if (!ViewModel.IsBusy && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanRing, ScanText, null,
            "Scanning Context Menus...", "Scan Context Menus",
            async () =>
            {
                await ViewModel.ScanAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnItemToggled(object sender, RoutedEventArgs e)
    {
        if (sender is LoadingToggleSwitch loadingToggle && loadingToggle.DataContext is ContextMenuItem item)
        {
            if (item.IsEnabled != loadingToggle.IsOn)
            {
                loadingToggle.IsLoading = true;
                try
                {
                    bool success = await ViewModel.ToggleItemAsync(item, loadingToggle.IsOn);
                    if (!success)
                    {
                        loadingToggle.IsOn = item.IsEnabled;
                    }
                }
                finally
                {
                    loadingToggle.IsLoading = false;
                }
            }
        }
        else if (sender is ToggleSwitch toggle && toggle.DataContext is ContextMenuItem legacyItem)
        {
            if (legacyItem.IsEnabled != toggle.IsOn)
            {
                bool success = await ViewModel.ToggleItemAsync(legacyItem, toggle.IsOn);
                if (!success)
                {
                    toggle.IsOn = legacyItem.IsEnabled;
                }
            }
        }
    }

    public bool IsNot(bool b) => !b;
}

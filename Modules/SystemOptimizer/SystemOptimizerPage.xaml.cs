using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SystemOptimizerPage : Page
{
    private DispatcherTimer? _ramTimer;

    public SystemOptimizerViewModel ViewModel { get; }

    public SystemOptimizerPage()
    {
        ViewModel = App.Services.GetRequiredService<SystemOptimizerViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Refresh values on page entry
        ViewModel.UpdateRamAndServices();
        ViewModel.LoadTweaks();

        // Setup periodic RAM update timer (1.5 seconds)
        if (_ramTimer == null)
        {
            _ramTimer = new DispatcherTimer();
            _ramTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _ramTimer.Tick += RamTimer_Tick;
        }
        _ramTimer.Start();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Stop timer when navigating away to conserve resources
        _ramTimer?.Stop();
    }

    private void RamTimer_Tick(object sender, object e)
    {
        ViewModel.UpdateRamAndServices();
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplySelectedAsync();
    }

    private void OnReloadTweaksClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadTweaks();
    }

    private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Confirm Restore".T(),
            Content = "Are you sure you want to restore default Windows settings for all tweaks?".T(),
            PrimaryButtonText = "Yes, Restore".T(),
            CloseButtonText = "Cancel".T(),
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RestoreDefaultsAsync();
        }
    }

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Pivot pivot && pivot.SelectedItem is PivotItem item)
        {
            string category = item.Tag as string ?? "All";
            ViewModel.FilterTweaks(category);
        }
    }

    private async void OnToggleTweakClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SystemTweak tweak)
        {
            await ViewModel.ToggleTweakAsync(tweak);
        }
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BoostRamAsync();
    }

    internal bool IsNot(bool val) => !val;

    internal string GetPercentageText(double val) => $"{val:F0}%";

    internal Style? GetStatusBadgeStyle(bool isOptimized)
    {
        if (isOptimized)
        {
            return Application.Current.Resources.TryGetValue("StatusBadgeGoodStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
        else
        {
            return Application.Current.Resources.TryGetValue("StatusBadgeWarningStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
    }

    internal string GetActionButtonText(bool isOptimized)
    {
        return isOptimized ? "Revert".T() : "Apply".T();
    }

    internal Style? GetActionButtonStyle(bool isOptimized)
    {
        if (isOptimized)
        {
            return Application.Current.Resources.TryGetValue("DefaultButtonStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
        else
        {
            return Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
    }

}

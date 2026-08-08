using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Views;

public sealed partial class UpdaterPage : Page
{
    public UpdaterViewModel ViewModel { get; }

    public UpdaterPage()
    {
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<UpdaterViewModel>();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private async void OnScanUpdatesClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanUpdatesBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ScanUpdatesRing, ScanUpdatesText, null,
            "Scanning Updates...", "Scan Updates",
            async () =>
            {
                await ViewModel.ScanUpdatesAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnUpdateAllClick(object sender, RoutedEventArgs e)
    {
        var btn = UpdateAllBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, UpdateAllRing, UpdateAllText, null,
            "Updating All...", "Update All",
            async () =>
            {
                await ViewModel.UpdateAllAppsAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnUpdateSelectedClick(object sender, RoutedEventArgs e)
    {
        var btn = UpdateSelectedBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, UpdateSelectedRing, UpdateSelectedText, null,
            "Updating Selected...", "Update Selected",
            async () =>
            {
                await ViewModel.UpdateSelectedAppsAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnUpdateSingleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SoftwareUpdateInfo app)
        {
            await ViewModel.UpdateSingleAppAsync(app);
        }
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SetAllSelection(true);
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SetAllSelection(false);
    }



    public bool CanUpdateSelected(bool hasSelected, bool isBusy) => hasSelected && !isBusy;

    public Visibility IsListNotEmpty(int count, bool isBusy) => (count > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListEmpty(int count, bool isBusy) => (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Brush GetBrushFromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length < 7) 
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        
        try
        {
            string cleanHex = hex.Replace("#", "");
            byte a = 255;
            byte r = 0, g = 0, b = 0;
            
            if (cleanHex.Length == 8)
            {
                a = Convert.ToByte(cleanHex.Substring(0, 2), 16);
                r = Convert.ToByte(cleanHex.Substring(2, 2), 16);
                g = Convert.ToByte(cleanHex.Substring(4, 2), 16);
                b = Convert.ToByte(cleanHex.Substring(6, 2), 16);
            }
            else if (cleanHex.Length == 6)
            {
                r = Convert.ToByte(cleanHex.Substring(0, 2), 16);
                g = Convert.ToByte(cleanHex.Substring(2, 2), 16);
                b = Convert.ToByte(cleanHex.Substring(4, 2), 16);
            }
            return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        }
        catch
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }
    }

    public double GetSecurityRating(int count)
    {
        if (count <= 0) return 100.0;
        return Math.Max(100.0 - count * 15.0, 20.0);
    }

    public string GetSecurityRatingText(int count)
    {
        if (count <= 0) return "100";
        return $"{Math.Max(100 - count * 15, 20)}";
    }

    public bool IsNot(bool val) => !val;
}

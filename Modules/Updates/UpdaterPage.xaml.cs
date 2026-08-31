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
        ViewModel = App.Services.GetRequiredService<UpdaterViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Initialize();
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        WinCarePro.Services.TranslationManager.Instance.Translate(this);
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        ViewModel.Cleanup();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            WinCarePro.Services.TranslationManager.Instance.Translate(this);
            ViewModel.ApplyFilters();
        });
    }

    private async void OnScanUpdatesClick(object sender, RoutedEventArgs e)
    {
        var btn = ScanUpdatesBtn ?? (sender as Button);
        if (btn != null) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
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
        if (btn != null) WinCarePro.Shared.Animations.FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 300);
        
        bool hasSelection = ViewModel.HasSelectedUpdates;
        string loadingMsg = hasSelection ? "Updating Selected..." : "Updating All...";
        string idleMsg = "Update All Apps";

        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, UpdateAllRing, UpdateAllText, null,
            loadingMsg, idleMsg,
            async () =>
            {
                if (hasSelection)
                    await ViewModel.UpdateSelectedAppsAsync();
                else
                    await ViewModel.UpdateAllAppsAsync();
            },
            minDurationMs: 1200);
    }

    private async void OnUpdateSelectedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateSelectedAppsAsync();
    }

    private async void OnUpdateSingleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SoftwareUpdateInfo app)
        {
            await ViewModel.UpdateSingleAppAsync(app);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelOperations();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SetAllSelection(true);
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SetAllSelection(false);
    }

    private void OnMasterSelectAllChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb)
        {
            ViewModel.SetAllSelection(cb.IsChecked == true);
        }
    }

    private void OnFilterPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Pivot pivot && pivot.SelectedItem is PivotItem item && item.Tag is string tag)
        {
            ViewModel.SelectedStatusFilter = tag;
        }
    }

    private void OnFilterAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedStatusFilter = "All";
    }

    private void OnFilterPendingClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedStatusFilter = "Pending";
    }

    private void OnFilterUpdatingClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedStatusFilter = "Updating";
    }

    private void OnFilterCompletedClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedStatusFilter = "Completed";
    }

    public bool IsFilterSelected(string currentFilter, string targetFilter) => string.Equals(currentFilter, targetFilter, StringComparison.OrdinalIgnoreCase);

    public bool CanUpdateSelected(bool hasSelected, bool isBusy) => hasSelected && !isBusy;

    public Visibility HasItemsVisibility(int count, bool isBusy) => (count > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListNotEmpty(int count, bool isBusy) => (count > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListEmpty(int count, bool isBusy) => (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HasPendingUpdatesVisibility(int pendingCount, bool isBusy) => (pendingCount > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HasNoPendingUpdatesVisibility(int pendingCount) => (pendingCount <= 0) ? Visibility.Visible : Visibility.Collapsed;

    public string GetUpdateAllButtonText(int pendingCount)
    {
        if (pendingCount <= 0) return "Update All Apps";
        return $"Update All ({pendingCount})";
    }

    public Brush GetBrushFromHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) 
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        
        try
        {
            string cleanHex = hex.Replace("#", "").Trim();
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

    private async void OnBackupDriversClick(object sender, RoutedEventArgs e)
    {
        var result = await ViewModel.BackupDriversAsync();
        var dialog = new ContentDialog
        {
            Title = result.Success ? "Driver Backup Completed" : "Driver Backup Alert",
            Content = result.Message + (result.Success ? $"\n\nSaved Location: {result.BackupPath}" : ""),
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void OnRestoreDriversClick(object sender, RoutedEventArgs e)
    {
        string defaultDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "WinCarePro_DriverBackups");

        if (!System.IO.Directory.Exists(defaultDir))
        {
            var noDirDialog = new ContentDialog
            {
                Title = "Restore Drivers",
                Content = "No driver backup directory found at default location:\n" + defaultDir,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            await noDirDialog.ShowAsync();
            return;
        }

        var subDirs = System.IO.Directory.GetDirectories(defaultDir);
        string targetDir = subDirs.Length > 0 ? subDirs.OrderByDescending(d => d).First() : defaultDir;

        var confirmDialog = new ContentDialog
        {
            Title = "Restore Hardware Drivers",
            Content = $"Are you sure you want to restore drivers from the latest backup folder?\n\n{targetDir}",
            PrimaryButtonText = "Restore Now",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var result = await ViewModel.RestoreDriversAsync(targetDir);
            var resultDialog = new ContentDialog
            {
                Title = result.Success ? "Restore Complete" : "Restore Result",
                Content = result.Message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await resultDialog.ShowAsync();
        }
    }
}

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Components;

namespace WinCarePro.Views;

public sealed partial class UpdaterPage : Page
{
    public UpdaterViewModel ViewModel { get; }
    private SizeChangedEventHandler? _sizeChangedHandler;

    public UpdaterPage()
    {
        ViewModel = App.Services.GetRequiredService<UpdaterViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;

        _sizeChangedHandler = (s, e) => UpdateResponsiveLayout(e.NewSize.Width);
        this.SizeChanged += _sizeChangedHandler;

        this.Loaded += (s, e) =>
        {
            UpdateResponsiveLayout(this.ActualWidth);
            WinCarePro.Services.TranslationManager.Instance.Translate(this);
            this.DispatcherQueue?.TryEnqueue(() => WinCarePro.Services.TranslationManager.Instance.Translate(this));
        };
        this.Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_sizeChangedHandler != null) this.SizeChanged -= _sizeChangedHandler;
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        ViewModel?.Cleanup();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateResponsiveLayout(this.ActualWidth);
        ViewModel.Initialize();
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        WinCarePro.Services.TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        WinCarePro.Services.TranslationManager.Instance.Translate(this);
        this.DispatcherQueue?.TryEnqueue(() => WinCarePro.Services.TranslationManager.Instance.Translate(this));
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
                WinCarePro.Services.TranslationManager.Instance.Translate(this);
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

    public Visibility HasItemsVisibility(int count, bool isBusy) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListNotEmpty(int count, bool isBusy) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListEmpty(int count, bool isBusy) => (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsListLoading(int count, bool isBusy) => (count == 0 && isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HasPendingUpdatesVisibility(int pendingCount, bool isBusy) => (pendingCount > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HasNoPendingUpdatesVisibility(int pendingCount) => (pendingCount <= 0) ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        // 1. Header Action Buttons Responsive Stacking
        if (HeaderCol0 != null && HeaderCol1 != null && HeaderActionsPanel != null)
        {
            if (width >= 1050)
            {
                HeaderCol0.Width = new GridLength(1, GridUnitType.Star);
                HeaderCol1.Width = GridLength.Auto;
                Grid.SetRow(HeaderActionsPanel, 0);
                Grid.SetColumn(HeaderActionsPanel, 1);
                Grid.SetColumnSpan(HeaderActionsPanel, 1);
                HeaderActionsPanel.Margin = new Thickness(0);
                HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                HeaderCol0.Width = new GridLength(1, GridUnitType.Star);
                HeaderCol1.Width = new GridLength(0, GridUnitType.Pixel);
                Grid.SetRow(HeaderActionsPanel, 1);
                Grid.SetColumn(HeaderActionsPanel, 0);
                Grid.SetColumnSpan(HeaderActionsPanel, 2);
                HeaderActionsPanel.Margin = new Thickness(0, 10, 0, 0);
                HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        // 2. Toolbar & Search Box Responsive Stacking
        if (ToolbarCol0 != null && ToolbarCol1 != null && ToolbarActionsPanel != null)
        {
            if (width >= 780)
            {
                ToolbarCol0.Width = new GridLength(1, GridUnitType.Star);
                ToolbarCol1.Width = GridLength.Auto;
                Grid.SetRow(ToolbarActionsPanel, 0);
                Grid.SetColumn(ToolbarActionsPanel, 1);
                ToolbarActionsPanel.Margin = new Thickness(0);
                ToolbarActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                ToolbarCol0.Width = new GridLength(1, GridUnitType.Star);
                ToolbarCol1.Width = new GridLength(0, GridUnitType.Pixel);
                Grid.SetRow(ToolbarActionsPanel, 1);
                Grid.SetColumn(ToolbarActionsPanel, 0);
                ToolbarActionsPanel.Margin = new Thickness(0, 8, 0, 0);
                ToolbarActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }
    }

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

    public Visibility HasTextVisibility(string? text) => !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;

    public string GetSpeedDisplay(string? speed) => string.IsNullOrWhiteSpace(speed) ? "" : $"•  {speed}";

    public string GetPercentText(int percent) => percent > 0 ? $"{Math.Clamp(percent, 0, 100)}%" : "";

    private async void OnBackupDriversClick(object sender, RoutedEventArgs e)
    {
        var result = await ViewModel.BackupDriversAsync();
        if (result.Success)
        {
            await ResultDialogHelper.ShowSuccessAsync(
                this.XamlRoot,
                "Driver Backup Completed",
                result.Message,
                detailLog: $"Saved Location:\n{result.BackupPath}");
        }
        else
        {
            await ResultDialogHelper.ShowWarningAsync(
                this.XamlRoot,
                "Driver Backup Alert",
                result.Message);
        }
    }

    private async void OnRestoreDriversClick(object sender, RoutedEventArgs e)
    {
        string defaultDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "WinCarePro_DriverBackups");

        if (!System.IO.Directory.Exists(defaultDir))
        {
            await ResultDialogHelper.ShowWarningAsync(
                this.XamlRoot,
                "Restore Drivers",
                "No driver backup directory found at default location:\n" + defaultDir);
            return;
        }

        var subDirs = System.IO.Directory.GetDirectories(defaultDir);
        string targetDir = subDirs.Length > 0 ? subDirs.OrderByDescending(d => d).First() : defaultDir;

        bool confirmed = await ResultDialogHelper.ShowConfirmAsync(
            this.XamlRoot,
            "Restore Hardware Drivers",
            $"Are you sure you want to restore drivers from the latest backup folder?\n\n{targetDir}",
            confirmText: "Restore Now",
            cancelText: "Cancel");

        if (confirmed)
        {
            var result = await ViewModel.RestoreDriversAsync(targetDir);
            if (result.Success)
            {
                await ResultDialogHelper.ShowSuccessAsync(
                    this.XamlRoot,
                    "Restore Complete",
                    result.Message);
            }
            else
            {
                await ResultDialogHelper.ShowErrorAsync(
                    this.XamlRoot,
                    "Restore Result",
                    result.Message);
            }
        }
    }
}

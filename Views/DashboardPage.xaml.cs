using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;
using WinCarePro.Models;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        ViewModel = new DashboardViewModel(this.DispatcherQueue);
        this.Loaded += async (s, e) => 
        {
            ViewModel.DispatcherQueue = this.DispatcherQueue;
            DataContext = ViewModel;
            
            // Lazy load the extended layer after initial UI renders to prevent lag
            await Task.Delay(200);
            ViewModel.IsExtendedLayerLoaded = true;
        };
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.DispatcherQueue = this.DispatcherQueue;
        ViewModel.StartMonitoring();
        ViewModel.RefreshActionLogs();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunFullDiagnosticsAsync();
    }

    private async void OnOptimizeClick(SplitButton sender, SplitButtonClickEventArgs e)
    {
        await RunOptimizationFlow(OptimizationMode.Recommended);
    }

    private async void OnSafeOptimizeClick(object sender, RoutedEventArgs e)
    {
        await RunOptimizationFlow(OptimizationMode.Safe);
    }

    private async void OnRecommendedOptimizeClick(object sender, RoutedEventArgs e)
    {
        await RunOptimizationFlow(OptimizationMode.Recommended);
    }

    private async void OnAdvancedOptimizeClick(object sender, RoutedEventArgs e)
    {
        await RunOptimizationFlow(OptimizationMode.Advanced);
    }

    private async void OnUndoOptimizeClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UndoLastOptimizationAsync();
        ViewModel.RefreshActionLogs();
    }

    private async Task RunOptimizationFlow(OptimizationMode mode)
    {
        var summary = await ViewModel.OptimizeSystemAsync(mode);
        if (summary != null)
        {
            await ShowOptimizationSummaryDialogAsync(summary);
        }
        ViewModel.RefreshActionLogs();
    }

    private async Task ShowOptimizationSummaryDialogAsync(OptimizationSummary summary)
    {
        long totalDiskCleanedBytes = summary.JunkBytesCleaned + summary.DoCacheBytesCleaned;
        double totalDiskCleanedMb = totalDiskCleanedBytes / 1024.0 / 1024.0;
        double ramReclaimedMb = summary.RamBytesReclaimed / 1024.0 / 1024.0;

        var mainPanel = new StackPanel { Spacing = 16, Width = 380 };

        var headerPanel = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 8) };
        var icon = new FontIcon 
        { 
            Glyph = "\uE73E", 
            FontSize = 48, 
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) 
        };
        var titleText = new TextBlock 
        { 
            Text = "System Optimized Successfully", 
            FontSize = 18, 
            FontWeight = Microsoft.UI.Text.FontWeights.Bold, 
            HorizontalAlignment = HorizontalAlignment.Center 
        };
        var subText = new TextBlock 
        { 
            Text = "All diagnosed areas have been optimized to peak health.", 
            FontSize = 12, 
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184)), 
            HorizontalAlignment = HorizontalAlignment.Center 
        };

        headerPanel.Children.Add(icon);
        headerPanel.Children.Add(titleText);
        headerPanel.Children.Add(subText);
        mainPanel.Children.Add(headerPanel);

        var separator = new Border 
        { 
            Height = 1, 
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255)), 
            Margin = new Thickness(0, 4, 0, 4) 
        };
        mainPanel.Children.Add(separator);

        var detailsGrid = new Grid { RowSpacing = 12 };
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        int rowIndex = 0;
        
        void AddDetailRow(string glyph, Windows.UI.Color glyphColor, string title, string description)
        {
            detailsGrid.RowDefinitions.Add(new RowDefinition());

            var rowIcon = new FontIcon 
            { 
                Glyph = glyph, 
                FontSize = 14, 
                Foreground = new SolidColorBrush(glyphColor), 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(rowIcon, rowIndex);
            Grid.SetColumn(rowIcon, 0);
            detailsGrid.Children.Add(rowIcon);

            var titleBlock = new TextBlock 
            { 
                Text = title, 
                FontSize = 13, 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, 
                VerticalAlignment = VerticalAlignment.Center 
            };
            Grid.SetRow(titleBlock, rowIndex);
            Grid.SetColumn(titleBlock, 1);
            detailsGrid.Children.Add(titleBlock);

            var descBlock = new TextBlock 
            { 
                Text = description, 
                FontSize = 13, 
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(descBlock, rowIndex);
            Grid.SetColumn(descBlock, 2);
            detailsGrid.Children.Add(descBlock);

            rowIndex++;
        }

        AddDetailRow("\uE7F1", Windows.UI.Color.FromArgb(255, 245, 158, 11), "Disk Junk Cleaned", $"{totalDiskCleanedMb:F1} MB");
        AddDetailRow("\uE949", Windows.UI.Color.FromArgb(255, 168, 85, 247), "Registry Errors Fixed", $"{summary.RegistryIssuesFixed} resolved");
        AddDetailRow("\uE950", Windows.UI.Color.FromArgb(255, 59, 130, 246), "RAM Reclaimed (Boost)", $"{ramReclaimedMb:F1} MB");
        AddDetailRow("\uE8F1", Windows.UI.Color.FromArgb(255, 20, 184, 166), "Active Apps Boosted", $"{summary.RamProcessesOptimized} processes");
        AddDetailRow("\uE774", Windows.UI.Color.FromArgb(255, 6, 182, 212), "DNS Resolver Cache", summary.DnsCacheFlushed ? "Flushed" : "Done");
        AddDetailRow("\uE945", Windows.UI.Color.FromArgb(255, 236, 72, 153), "Performance Tweaks", $"{summary.TweaksApplied} activated");

        mainPanel.Children.Add(detailsGrid);

        ContentDialog dialog = new ContentDialog
        {
            Content = mainPanel,
            CloseButtonText = "Done",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }

    internal string GetStatusText(int score)
    {
        if (score >= 90) return "EXCELLENT - Your system is highly optimized and clean.";
        if (score >= 70) return "GOOD - Some areas can be optimized to reclaim storage.";
        return "NEEDS OPTIMIZATION - Heavy junk logs or updates required.";
    }

    internal Brush GetHealthScoreBrush(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); 
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); 
    }

    internal Brush GetHealthScoreBadgeBackground(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11));
        return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68));
    }

    internal bool IsNot(bool val) => !val;
    internal string FormatPercent(double val) => $"{val:F1}%";

    internal Visibility GetVisibility(int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetProgressVisibility(bool isScanning)
    {
        return isScanning ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetOptimizeVisibility(bool hasScanned, bool isScanning)
    {
        return (hasScanned && !isScanning) ? Visibility.Visible : Visibility.Collapsed;
    }

    internal bool GetProgressRingActive(bool isScanning, bool isOptimizing)
    {
        return isScanning || isOptimizing;
    }

    internal static string GetStatusIcon(bool healthy)
    {
        return healthy ? "\uE73E" : "\uE7BA"; 
    }

    internal static string GetStatusLabel(bool healthy)
    {
        return healthy ? "Optimized" : "Action Recommended";
    }

    internal static Brush GetStatusColor(bool healthy)
    {
        var color = healthy ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 245, 158, 11);
        return new SolidColorBrush(color);
    }

    internal static Brush GetStatusBadgeBg(bool healthy)
    {
        var color = healthy ? Windows.UI.Color.FromArgb(30, 16, 185, 129) : Windows.UI.Color.FromArgb(30, 245, 158, 11);
        return new SolidColorBrush(color);
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BoostRamAsync();
    }

    private async void OnCleanDiskClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanDiskJunkAsync();
    }

    private async void OnFixItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is DiagnosticResult item)
        {
            await ViewModel.FixDiagnosticItemAsync(item);
        }
    }

    internal static Visibility GetFixButtonVisibility(bool isHealthy)
    {
        return isHealthy ? Visibility.Collapsed : Visibility.Visible;
    }

    internal static Visibility GetStatusBadgeVisibility(bool isHealthy)
    {
        return isHealthy ? Visibility.Visible : Visibility.Collapsed;
    }

    // Chart dynamic series toggles
    private void OnChartFilterChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && ViewModel != null)
        {
            string? name = cb.Content?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                ViewModel.ToggleChartSeries(name, cb.IsChecked == true);
            }
        }
    }

    // Dynamic show/hide deep diagnostics panel
    private void OnShowDeepLayerChecked(object sender, RoutedEventArgs e)
    {
        this.FindName("DeepLayerPanel");
        if (DeepLayerPanel != null)
        {
            DeepLayerPanel.Visibility = Visibility.Visible;
            ViewModel.RefreshActionLogs();
        }
    }

    private void OnShowDeepLayerUnchecked(object sender, RoutedEventArgs e)
    {
        if (DeepLayerPanel != null)
        {
            DeepLayerPanel.Visibility = Visibility.Collapsed;
        }
    }

    // Export report call
    private async void OnExportReportClick(object sender, RoutedEventArgs e)
    {
        if (ExportFormatCombo.SelectedItem is ComboBoxItem item)
        {
            string format = item.Content.ToString() ?? "TXT";
            try
            {
                ExportStatusText.Visibility = Visibility.Visible;
                ExportStatusText.Text = "Exporting report...";
                string path = await ViewModel.ExportDiagnosticReportAsync(format);
                ExportStatusText.Text = $"Report exported successfully to: {path}";
            }
            catch (Exception ex)
            {
                ExportStatusText.Text = $"Export failed: {ex.Message}";
            }
        }
    }

    // Recommendation card action button click
    private async void OnRecommendationFixClick(object sender, RoutedEventArgs e)
    {
        await RunOptimizationFlow(OptimizationMode.Recommended);
    }

    private MainPage? GetMainPage()
    {
        if (App.MainWindowInstance?.Content is Frame rootFrame)
        {
            return rootFrame.Content as MainPage;
        }
        return null;
    }

    // Quick Stats Navigation handlers
    private void OnQuickStatUptimeClick(object sender, RoutedEventArgs e)
    {
        GetMainPage()?.NavigateToPageExternal("hardware");
    }

    private void OnQuickStatNetworkClick(object sender, RoutedEventArgs e)
    {
        GetMainPage()?.NavigateToPageExternal("network");
    }

    private void OnQuickStatAppsClick(object sender, RoutedEventArgs e)
    {
        GetMainPage()?.NavigateToPageExternal("uninstall");
    }

    private void OnQuickStatJunkClick(object sender, RoutedEventArgs e)
    {
        GetMainPage()?.NavigateToPageExternal("junk");
    }

    // Visual overload indicator helpers
    internal Brush GetCpuCardBorderBrush(double cpu)
    {
        if (cpu > 85.0)
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Crimson);
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    internal Thickness GetCpuCardBorderThickness(double cpu)
    {
        return cpu > 85.0 ? new Thickness(2) : new Thickness(0);
    }

    internal Brush GetRamCardBackground(double ram)
    {
        if (ram > 90.0)
        {
            return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68));
        }
        return (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
    }

    internal Brush GetDiskCardBorderBrush(double disk)
    {
        if (disk > 90.0)
        {
            return new SolidColorBrush(Microsoft.UI.Colors.DarkOrange);
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    internal Thickness GetDiskCardBorderThickness(double disk)
    {
        return disk > 90.0 ? new Thickness(2) : new Thickness(0);
    }

    internal Brush GetBottleneckBadgeBg(bool hasBottleneck)
    {
        var color = hasBottleneck ? Windows.UI.Color.FromArgb(30, 239, 68, 68) : Windows.UI.Color.FromArgb(30, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    internal Brush GetBottleneckBadgeFg(bool hasBottleneck)
    {
        var color = hasBottleneck ? Microsoft.UI.Colors.Crimson : Microsoft.UI.Colors.MediumSeaGreen;
        return new SolidColorBrush(color);
    }
}

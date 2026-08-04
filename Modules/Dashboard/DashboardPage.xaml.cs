using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;

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
            
            // Start glowing sweep animation
            try
            {
                PulsingRadarGlow.Begin();
            }
            catch { }

            // Lazy load the extended layer after initial UI renders to prevent lag
            await Task.Delay(200);
            ViewModel.IsExtendedLayerLoaded = true;
            
            // Force responsive update after extended layer is fully initialized
            UpdateResponsiveLayout(this.ActualWidth);

            // Translate page content
            TranslationManager.Instance.Translate(this);
        };

        TranslationManager.Instance.LanguageChanged += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
        };

        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        };
    }

    private void OnLaunchDesktopWidgetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.ShowWindow();
        }
        catch { }
    }

    private void OnLaunchGamingTurboClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindowInstance is MainWindow mw)
            {
                if (mw.MainFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal("gamingturbo");
                }
            }
        }
        catch { }
    }

    private void UpdateResponsiveLayout(double width)
    {
        bool isWide = width >= 800;
        
        if (LeftCol != null && RightCol != null)
        {
            if (isWide)
            {
                // Wide layout: 2 columns
                LeftCol.Width = new GridLength(3, GridUnitType.Star);
                RightCol.Width = new GridLength(2, GridUnitType.Star);
                
                // Alignments for Column 0 (Left side)
                if (HealthGaugeCard != null)
                {
                    Grid.SetRow(HealthGaugeCard, 0);
                    Grid.SetColumn(HealthGaugeCard, 0);
                    Grid.SetColumnSpan(HealthGaugeCard, 1);
                }
                if (CpuRamGrid != null)
                {
                    Grid.SetRow(CpuRamGrid, 1);
                    Grid.SetColumn(CpuRamGrid, 0);
                    Grid.SetColumnSpan(CpuRamGrid, 1);
                }
                if (CpuCard != null)
                {
                    Grid.SetRow(CpuCard, 0);
                    Grid.SetColumn(CpuCard, 0);
                    Grid.SetColumnSpan(CpuCard, 1);
                }
                if (RamCard != null)
                {
                    Grid.SetRow(RamCard, 0);
                    Grid.SetColumn(RamCard, 1);
                    Grid.SetColumnSpan(RamCard, 1);
                }
                if (GpuDiskGrid != null)
                {
                    Grid.SetRow(GpuDiskGrid, 2);
                    Grid.SetColumn(GpuDiskGrid, 0);
                    Grid.SetColumnSpan(GpuDiskGrid, 1);
                }
                if (GpuCard != null)
                {
                    Grid.SetRow(GpuCard, 0);
                    Grid.SetColumn(GpuCard, 0);
                    Grid.SetColumnSpan(GpuCard, 1);
                }
                if (DiskCard != null)
                {
                    Grid.SetRow(DiskCard, 0);
                    Grid.SetColumn(DiskCard, 1);
                    Grid.SetColumnSpan(DiskCard, 1);
                }
                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 3);
                    Grid.SetColumn(PerformanceChartCard, 0);
                    Grid.SetColumnSpan(PerformanceChartCard, 1);
                }
                
                // Alignments for Column 1 (Right side)
                if (BottleneckCard != null)
                {
                    Grid.SetRow(BottleneckCard, 0);
                    Grid.SetColumn(BottleneckCard, 1);
                    Grid.SetColumnSpan(BottleneckCard, 1);
                    BottleneckCard.Margin = new Thickness(8);
                }
                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 1);
                    Grid.SetColumn(QuickStatsGrid, 1);
                    Grid.SetColumnSpan(QuickStatsGrid, 1);
                }
                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 2);
                    Grid.SetColumn(RecommendationsCard, 1);
                    Grid.SetColumnSpan(RecommendationsCard, 1);
                    Grid.SetRowSpan(RecommendationsCard, 2);
                }
            }
            else
            {
                // Narrow layout: 1 column
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(0, GridUnitType.Pixel);
                
                if (HealthGaugeCard != null)
                {
                    Grid.SetRow(HealthGaugeCard, 0);
                    Grid.SetColumn(HealthGaugeCard, 0);
                    Grid.SetColumnSpan(HealthGaugeCard, 2);
                }
                if (CpuRamGrid != null)
                {
                    Grid.SetRow(CpuRamGrid, 1);
                    Grid.SetColumn(CpuRamGrid, 0);
                    Grid.SetColumnSpan(CpuRamGrid, 2);
                }
                if (CpuCard != null)
                {
                    Grid.SetRow(CpuCard, 0);
                    Grid.SetColumn(CpuCard, 0);
                    Grid.SetColumnSpan(CpuCard, 2);
                }
                if (RamCard != null)
                {
                    Grid.SetRow(RamCard, 1);
                    Grid.SetColumn(RamCard, 0);
                    Grid.SetColumnSpan(RamCard, 2);
                }
                if (BottleneckCard != null)
                {
                    Grid.SetRow(BottleneckCard, 2);
                    Grid.SetColumn(BottleneckCard, 0);
                    Grid.SetColumnSpan(BottleneckCard, 2);
                    BottleneckCard.Margin = new Thickness(8);
                }
                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 3);
                    Grid.SetColumn(QuickStatsGrid, 0);
                    Grid.SetColumnSpan(QuickStatsGrid, 2);
                }
                if (GpuDiskGrid != null)
                {
                    Grid.SetRow(GpuDiskGrid, 4);
                    Grid.SetColumn(GpuDiskGrid, 0);
                    Grid.SetColumnSpan(GpuDiskGrid, 2);
                }
                if (GpuCard != null)
                {
                    Grid.SetRow(GpuCard, 0);
                    Grid.SetColumn(GpuCard, 0);
                    Grid.SetColumnSpan(GpuCard, 2);
                }
                if (DiskCard != null)
                {
                    Grid.SetRow(DiskCard, 1);
                    Grid.SetColumn(DiskCard, 0);
                    Grid.SetColumnSpan(DiskCard, 2);
                }
                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 5);
                    Grid.SetColumn(PerformanceChartCard, 0);
                    Grid.SetColumnSpan(PerformanceChartCard, 2);
                }
                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 6);
                    Grid.SetColumn(RecommendationsCard, 0);
                    Grid.SetColumnSpan(RecommendationsCard, 2);
                    Grid.SetRowSpan(RecommendationsCard, 1);
                }
            }
        }
    }

    private void OnExtendedLayerPanelLoaded(object sender, RoutedEventArgs e)
    {
        UpdateResponsiveLayout(this.ActualWidth);
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

        bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;

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
            Text = "System Optimized Successfully".T(), 
            FontSize = 18, 
            FontWeight = Microsoft.UI.Text.FontWeights.Bold, 
            HorizontalAlignment = HorizontalAlignment.Center 
        };
        var subText = new TextBlock 
        { 
            Text = "All diagnosed areas have been optimized to peak health.".T(), 
            FontSize = 12, 
            Foreground = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 148, 163, 184) : Windows.UI.Color.FromArgb(255, 100, 116, 139)), 
            HorizontalAlignment = HorizontalAlignment.Center 
        };

        headerPanel.Children.Add(icon);
        headerPanel.Children.Add(titleText);
        headerPanel.Children.Add(subText);
        mainPanel.Children.Add(headerPanel);

        var separator = new Border 
        { 
            Height = 1, 
            Background = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(30, 255, 255, 255) : Windows.UI.Color.FromArgb(30, 0, 0, 0)), 
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

        AddDetailRow("\uE7F1", Windows.UI.Color.FromArgb(255, 245, 158, 11), "Disk Junk Cleaned".T(), $"{totalDiskCleanedMb:F1} MB");
        AddDetailRow("\uE949", Windows.UI.Color.FromArgb(255, 168, 85, 247), "Registry Errors Fixed".T(), $"{summary.RegistryIssuesFixed} " + "resolved".T());
        AddDetailRow("\uE950", Windows.UI.Color.FromArgb(255, 59, 130, 246), "RAM Reclaimed (Boost)".T(), $"{ramReclaimedMb:F1} MB");
        AddDetailRow("\uE8F1", Windows.UI.Color.FromArgb(255, 20, 184, 166), "Active Apps Boosted".T(), $"{summary.RamProcessesOptimized} " + "processes".T());
        AddDetailRow("\uE774", Windows.UI.Color.FromArgb(255, 6, 182, 212), "DNS Resolver Cache".T(), summary.DnsCacheFlushed ? "Flushed".T() : "Done".T());
        AddDetailRow("\uE945", Windows.UI.Color.FromArgb(255, 236, 72, 153), "Performance Tweaks".T(), $"{summary.TweaksApplied} " + "activated".T());

        mainPanel.Children.Add(detailsGrid);

        if (summary.TweaksApplied > 0)
        {
            var restartWarningBorder = new Border
            {
                Background = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(20, 245, 158, 11) : Windows.UI.Color.FromArgb(20, 217, 119, 6)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 245, 158, 11)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var warningPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var warningIcon = new FontIcon
            {
                Glyph = "\uE7BA",
                FontSize = 14,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var warningText = new TextBlock
            {
                Text = "Restart is recommended to fully apply system tweaks.".T(),
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 320
            };
            warningPanel.Children.Add(warningIcon);
            warningPanel.Children.Add(warningText);
            restartWarningBorder.Child = warningPanel;
            mainPanel.Children.Add(restartWarningBorder);
        }

        ContentDialog dialog = new ContentDialog
        {
            Content = mainPanel,
            CloseButtonText = "Done".T(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }

    internal string GetStatusText(int score)
    {
        if (score >= 90) return "EXCELLENT - Your system is highly optimized and clean.".T();
        if (score >= 70) return "GOOD - Some areas can be optimized to reclaim storage.".T();
        return "NEEDS OPTIMIZATION - Heavy junk logs or updates required.".T();
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
        return (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    internal Thickness GetCpuCardBorderThickness(double cpu)
    {
        return new Thickness(1);
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
        return (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    internal Thickness GetDiskCardBorderThickness(double disk)
    {
        return new Thickness(1);
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

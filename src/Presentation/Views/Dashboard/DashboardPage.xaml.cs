using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WinCarePro.Core.Helpers;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; set; } = null!;
    private readonly EventHandler<WinCarePro.Services.Contracts.SettingsChangedEventArgs> _settingsChangedHandler;
    private readonly System.ComponentModel.PropertyChangedEventHandler? _vmPropertyChangedHandler;
    private readonly EventHandler? _themeChangedHandler;

    public DashboardPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        ViewModel = App.Services?.GetService<DashboardViewModel>() ?? new DashboardViewModel(this.DispatcherQueue);
        ViewModel.DispatcherQueue = this.DispatcherQueue;
        ViewModel.IsExtendedLayerLoaded = true;
        ThemeManager.Instance.RegisterPage(this);

        _themeChangedHandler = (s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() =>
            {
                this.RequestedTheme = ThemeManager.Instance.CurrentTheme;
                if (AmbientMeshGlowGrid != null)
                {
                    AmbientMeshGlowGrid.Visibility = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark ? Visibility.Visible : Visibility.Collapsed;
                }
                Bindings.Update();
            });
        };
        ThemeManager.Instance.ThemeChanged += _themeChangedHandler;

        _settingsChangedHandler = (s, e) =>
        {
            if (e.PropertyName is "IsProMode")
            {
                var cur = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
                UpdateExperienceMode(cur.IsProMode);
            }
        };
        WinCarePro.Services.Implementations.SettingsService.Instance.SettingsChanged += _settingsChangedHandler;

        _vmPropertyChangedHandler = (s, e) =>
        {
            if (string.IsNullOrEmpty(e.PropertyName)) return;
            this.DispatcherQueue?.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(DashboardViewModel.CpuUsage):
                        UpdateCircularGauge(CpuPathFigure, CpuArcSegment, CpuGlowBorder, ViewModel.CpuUsage);
                        break;
                    case nameof(DashboardViewModel.RamUsage):
                        UpdateCircularGauge(RamPathFigure, RamArcSegment, RamGlowBorder, ViewModel.RamUsage);
                        break;
                    case nameof(DashboardViewModel.GpuUsage):
                        UpdateCircularGauge(GpuPathFigure, GpuArcSegment, GpuGlowBorder, ViewModel.GpuUsage);
                        break;
                    case nameof(DashboardViewModel.DiskUsage):
                        UpdateCircularGauge(DiskPathFigure, DiskArcSegment, DiskGlowBorder, ViewModel.DiskUsage);
                        break;
                    case nameof(DashboardViewModel.HealthScore):
                        UpdateCircularGauge(HealthPathFigure, HealthArcSegment, HealthGlowBorder, ViewModel.HealthScore);
                        break;
                }
            });
        };
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += _vmPropertyChangedHandler;
        }

        this.Loaded += async (s, e) => 
        {
            if (ViewModel != null)
            {
                ViewModel.DispatcherQueue = this.DispatcherQueue;
                DataContext = ViewModel;
            }

            // Sync current experience mode
            var curMode = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
            UpdateExperienceMode(curMode.IsProMode);

            if (AmbientMeshGlowGrid != null)
            {
                AmbientMeshGlowGrid.Visibility = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark ? Visibility.Visible : Visibility.Collapsed;
            }

            // Setup GPU Visual Effects (Hover Lift & Button Spring Physics) and Circular Gauges
            SetupVisualEffectsAndGauges();

            // Ensure GaugeRingGrid has full 100% crisp opacity
            if (GaugeRingGrid != null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(GaugeRingGrid);
                if (visual != null)
                {
                    visual.StopAnimation("Opacity");
                    visual.Opacity = 1.0f;
                }
            }

            // Lazy load the extended layer after initial UI renders to prevent lag
            await Task.Delay(100);
            if (ViewModel != null)
            {
                ViewModel.IsExtendedLayerLoaded = true;
            }
            
            // Force responsive update after extended layer is fully initialized
            UpdateResponsiveLayout(this.ActualWidth);

            // Translate page content
            TranslationManager.Instance.Translate(this);
        };

        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;

        this.Unloaded += (s, e) =>
        {
            try { ViewModel?.StopMonitoring(); } catch { }
            ThemeManager.Instance.UnregisterPage(this);
            if (_themeChangedHandler != null)
            {
                ThemeManager.Instance.ThemeChanged -= _themeChangedHandler;
            }
            TranslationManager.Instance.LanguageChanged -= langHandler;
            WinCarePro.Services.Implementations.SettingsService.Instance.SettingsChanged -= _settingsChangedHandler;
            if (ViewModel != null && _vmPropertyChangedHandler != null)
            {
                ViewModel.PropertyChanged -= _vmPropertyChangedHandler;
            }
        };

        this.SizeChanged += (s, e) =>
        {
            UpdateResponsiveLayout(e.NewSize.Width);
        };

        this.ActualThemeChanged += (s, e) =>
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                Bindings.Update();
            });
        };
    }

    public void UpdateExperienceMode(bool isPro)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (QuickStatsGrid != null) QuickStatsGrid.Visibility = Visibility.Visible;
            if (CpuRamGrid != null) CpuRamGrid.Visibility = Visibility.Visible;
            if (GpuDiskGrid != null) GpuDiskGrid.Visibility = Visibility.Visible;
            if (PerformanceChartCard != null) PerformanceChartCard.Visibility = Visibility.Visible;
            if (RecommendationsCard != null) RecommendationsCard.Visibility = Visibility.Collapsed;

            UpdateResponsiveLayout(this.ActualWidth);
            TranslationManager.Instance.Translate(this);
        });
    }

    private void OnQuickRepairDoctorClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindowInstance is MainWindow mw && mw.MainFrame.Content is MainPage mp)
            {
                mp.NavigateToPageExternal("repair");
            }
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("DashboardPage.OnQuickRepairDoctorClick", ex);
        }
    }

    private void OnLaunchDesktopWidgetClick(object sender, RoutedEventArgs e)
    {
    }

    private void OnLaunchOptimizerClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindowInstance is MainWindow mw)
            {
                if (mw.MainFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal("optimizer");
                }
            }
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("DashboardPage.OnLaunchOptimizerClick", ex);
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        bool isWide = width >= 850;
        
        if (LeftCol != null && RightCol != null)
        {
            if (isWide)
            {
                // Wide layout: 2 equal 50/50 columns
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(1, GridUnitType.Star);
                
                if (HealthGaugeCard != null)
                {
                    Grid.SetRow(HealthGaugeCard, 0);
                    Grid.SetColumn(HealthGaugeCard, 0);
                    Grid.SetColumnSpan(HealthGaugeCard, 2);
                }

                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 1);
                    Grid.SetColumn(QuickStatsGrid, 0);
                    Grid.SetColumnSpan(QuickStatsGrid, 2);
                }

                if (HardwareSensorsPanel != null)
                {
                    Grid.SetRow(HardwareSensorsPanel, 2);
                    Grid.SetColumn(HardwareSensorsPanel, 0);
                    Grid.SetColumnSpan(HardwareSensorsPanel, 1);
                }

                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 2);
                    Grid.SetColumn(PerformanceChartCard, 1);
                    Grid.SetColumnSpan(PerformanceChartCard, 1);
                }

                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 3);
                    Grid.SetColumn(RecommendationsCard, 0);
                    Grid.SetColumnSpan(RecommendationsCard, 2);
                }
            }
            else
            {
                // Narrow layout: Stacked
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(0, GridUnitType.Pixel);
                
                if (HealthGaugeCard != null)
                {
                    Grid.SetRow(HealthGaugeCard, 0);
                    Grid.SetColumn(HealthGaugeCard, 0);
                    Grid.SetColumnSpan(HealthGaugeCard, 2);
                }
                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 1);
                    Grid.SetColumn(QuickStatsGrid, 0);
                    Grid.SetColumnSpan(QuickStatsGrid, 2);
                }
                if (HardwareSensorsPanel != null)
                {
                    Grid.SetRow(HardwareSensorsPanel, 2);
                    Grid.SetColumn(HardwareSensorsPanel, 0);
                    Grid.SetColumnSpan(HardwareSensorsPanel, 2);
                }
                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 3);
                    Grid.SetColumn(PerformanceChartCard, 0);
                    Grid.SetColumnSpan(PerformanceChartCard, 2);
                }
                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 4);
                    Grid.SetColumn(RecommendationsCard, 0);
                    Grid.SetColumnSpan(RecommendationsCard, 2);
                }
            }

            // Adapt GaugeActionPanel if window width is tight
            if (GaugeActionPanel != null)
            {
                if (width < 740)
                {
                    Grid.SetRow(GaugeActionPanel, 1);
                    Grid.SetColumn(GaugeActionPanel, 0);
                    Grid.SetColumnSpan(GaugeActionPanel, 2);
                    GaugeActionPanel.Margin = new Thickness(0, 12, 0, 0);
                    GaugeActionPanel.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else
                {
                    Grid.SetRow(GaugeActionPanel, 0);
                    Grid.SetColumn(GaugeActionPanel, 1);
                    Grid.SetColumnSpan(GaugeActionPanel, 1);
                    GaugeActionPanel.Margin = new Thickness(0);
                    GaugeActionPanel.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }

            // Dynamically adapt QuickStatsGrid columns based on container width
            // Under 1050px: 2 columns x 2 rows (Disk & Startup in Row 0; Security & Registry in Row 1)
            // 1050px and above: 4 columns x 1 row
            if (QuickStatsGrid != null && QuickStatsCardDisk != null && QuickStatsCardStartup != null && 
                QuickStatsCardSecurity != null && QuickStatsCardRegistry != null)
            {
                if (width < 1050)
                {
                    // 2x2 Layout
                    QuickStatsGrid.ColumnDefinitions.Clear();
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    Grid.SetRow(QuickStatsCardDisk, 0);
                    Grid.SetColumn(QuickStatsCardDisk, 0);

                    Grid.SetRow(QuickStatsCardStartup, 0);
                    Grid.SetColumn(QuickStatsCardStartup, 1);

                    Grid.SetRow(QuickStatsCardSecurity, 1);
                    Grid.SetColumn(QuickStatsCardSecurity, 0);

                    Grid.SetRow(QuickStatsCardRegistry, 1);
                    Grid.SetColumn(QuickStatsCardRegistry, 1);
                }
                else
                {
                    // 4x1 Layout
                    QuickStatsGrid.ColumnDefinitions.Clear();
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    QuickStatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    Grid.SetRow(QuickStatsCardDisk, 0);
                    Grid.SetColumn(QuickStatsCardDisk, 0);

                    Grid.SetRow(QuickStatsCardStartup, 0);
                    Grid.SetColumn(QuickStatsCardStartup, 1);

                    Grid.SetRow(QuickStatsCardSecurity, 0);
                    Grid.SetColumn(QuickStatsCardSecurity, 2);

                    Grid.SetRow(QuickStatsCardRegistry, 0);
                    Grid.SetColumn(QuickStatsCardRegistry, 3);
                }
            }
        }
    }

    private void OnExtendedLayerPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (GpuDiskGrid != null) GpuDiskGrid.Visibility = Visibility.Visible;
        if (PerformanceChartCard != null) PerformanceChartCard.Visibility = Visibility.Visible;
        if (RecommendationsCard != null) RecommendationsCard.Visibility = Visibility.Visible;

        UpdateResponsiveLayout(this.ActualWidth);
        if (sender is FrameworkElement fe)
        {
            TranslationManager.Instance.Translate(fe);
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.DispatcherQueue = this.DispatcherQueue;
        ViewModel.StartMonitoring();
        ViewModel.RefreshActionLogs();

        var curMode = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings;
        UpdateExperienceMode(curMode.IsProMode);
        if (PerformanceChartCard != null)
        {
            PerformanceChartCard.Visibility = Visibility.Visible;
        }
        UpdateResponsiveLayout(this.ActualWidth);
        Bindings.Update();

        if (GaugeRingGrid != null)
        {
            var visual = ElementCompositionPreview.GetElementVisual(GaugeRingGrid);
            if (visual != null)
            {
                visual.StopAnimation("Opacity");
                visual.Opacity = 1.0f;
            }
        }
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        try
        {
            if (HealthGaugeCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(HealthGaugeCard);
            }
        }
        catch { }
        ViewModel.StopMonitoring();
    }

    private async void OnOneClickSmartCareClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ViewModel.HasScanned)
            {
                await ViewModel.RunFullDiagnosticsAsync();
            }
            await RunOptimizationFlow(OptimizationMode.Recommended);
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("DashboardPage.OnOneClickSmartCareClick", ex);
        }
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.RunFullDiagnosticsAsync();
            string msg = string.Format("Scan completed. Health Score: {0}/100. Junk files: {1}.".T(), 
                ViewModel.HealthScore, ViewModel.JunkFileSize);
            App.MainWindowInstance?.ShowToastNotification("System Scan Completed".T(), msg, "Info");
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("DashboardPage.OnScanClick", ex);
        }
    }

    private void OnScanCategoryMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string modeTag)
        {
            ViewModel.OpenScanPanel(modeTag);
        }
    }

    private void OnRunAllDiagnosticsMenuClick(object sender, RoutedEventArgs e)
    {
        OnScanClick(sender, e);
    }

    // Modular Scan Card Click Handlers
    private void OnScanCardDiskClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenScanPanel("DiskAndCache");
    }

    private void OnScanCardStartupClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenScanPanel("StartupAndPerformance");
    }

    private void OnScanCardSecurityClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenScanPanel("SecurityCheck");
    }

    private void OnScanCardRegistryClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenScanPanel("RegistryAndDiskHealth");
    }

    // Modal / Side Panel event handlers
    private void OnCloseScanPanelClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseScanPanel();
    }

    private void OnScanPanelScrimClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseScanPanel();
    }

    private async void OnStartModeScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartModeScanAsync();
    }

    private void OnCancelModeScanClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelModeScan();
    }

    private async void OnExecuteModeQuickFixClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExecuteModeQuickFixAsync();
    }

    private void OnModeDeepNavClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseScanPanel();
        try
        {
            if (App.MainWindowInstance is MainWindow mw && mw.MainFrame.Content is MainPage mp)
            {
                mp.NavigateToPageExternal(ViewModel.ModeDeepNavTarget);
            }
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("DashboardPage.OnModeDeepNavClick", ex);
        }
    }

    // Helpers for XAML bindings
    public Visibility GetScanButtonVisibility(bool isScanning, bool hasScanned)
    {
        return (!isScanning && !hasScanned) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetQuickFixButtonVisibility(bool isScanning, bool hasScanned)
    {
        return (!isScanning && hasScanned) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetQuickFixResultVisibility(string? message)
    {
        return !string.IsNullOrWhiteSpace(message) ? Visibility.Visible : Visibility.Collapsed;
    }

    public SolidColorBrush GetFindingBadgeBg(ScanFindingSeverity severity)
    {
        return severity switch
        {
            ScanFindingSeverity.Healthy => new SolidColorBrush(Windows.UI.Color.FromArgb(35, 16, 185, 129)),
            ScanFindingSeverity.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(35, 245, 158, 11)),
            ScanFindingSeverity.Danger => new SolidColorBrush(Windows.UI.Color.FromArgb(35, 239, 68, 68)),
            _ => new SolidColorBrush(Windows.UI.Color.FromArgb(25, 148, 163, 184))
        };
    }

    public SolidColorBrush GetFindingBadgeFg(ScanFindingSeverity severity)
    {
        return severity switch
        {
            ScanFindingSeverity.Healthy => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            ScanFindingSeverity.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
            ScanFindingSeverity.Danger => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184))
        };
    }

    private async void OnOptimizeClick(object sender, RoutedEventArgs e)
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
        OptimizationSummary? summary = null;
        try
        {
            summary = await ViewModel.OptimizeSystemAsync(mode);
        }
        finally
        {
            try
            {
                // Cascade 3D Quantum Ripple Wave through telemetry cards
                var rippleCards = new System.Collections.Generic.List<FrameworkElement>();
                if (CpuCard != null) rippleCards.Add(CpuCard);
                if (RamCard != null) rippleCards.Add(RamCard);
                if (GpuCard != null) rippleCards.Add(GpuCard);
                if (DiskCard != null) rippleCards.Add(DiskCard);

                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DCascadeWave(rippleCards, 70);
            }
            catch { }
        }

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

        var currentTheme = ThemeManager.Instance.CurrentTheme;
        bool isDark = ThemeManager.Instance.IsDark;

        // Core dynamic palette for optimal contrast in Dark Mode & Light Mode
        var dialogBg = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(248, 18, 20, 29))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(254, 255, 255, 255));
        var dialogBorder = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
        var cardBg = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(220, 24, 27, 38))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(245, 248, 250, 252));
        var cardBorder = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(35, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
        var dividerBrush = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(20, 0, 0, 0));
        var textPrimary = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42));
        var textSecondary = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
        var emeraldText = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 5, 150, 105));
        var skyText = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 2, 132, 199));
        var purpleText = isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 85, 247))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 58, 237));

        var mainPanel = new StackPanel { Spacing = 14, Width = 430 };

        // 1. Radiant Header with Ambient Glowing Aura & Squircle Checkmark
        var headerPanel = new StackPanel 
        { 
            Spacing = 8, 
            HorizontalAlignment = HorizontalAlignment.Center, 
            Margin = new Thickness(0, 4, 0, 2) 
        };

        var iconContainer = new Grid
        {
            Width = 68,
            Height = 68,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var auraHalo = new Ellipse
        {
            Width = 68,
            Height = 68,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb((byte)(isDark ? 50 : 35), 16, 185, 129), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 16, 185, 129), Offset = 1 }
                }
            }
        };
        iconContainer.Children.Add(auraHalo);

        var iconBox = new Border
        {
            Width = 54,
            Height = 54,
            CornerRadius = new CornerRadius(16),
            Background = isDark 
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 16, 185, 129)) 
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 236, 253, 245)),
            BorderBrush = isDark 
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 16, 185, 129)) 
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 167, 243, 208)),
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = "\uE73E",
                FontSize = 26,
                Foreground = emeraldText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        iconContainer.Children.Add(iconBox);
        headerPanel.Children.Add(iconContainer);

        var titleText = new TextBlock
        {
            Text = "System Optimized Successfully".T(),
            FontSize = 19,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = textPrimary,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        headerPanel.Children.Add(titleText);

        var subText = new TextBlock
        {
            Text = "All diagnosed areas have been optimized to peak health.".T(),
            FontSize = 12.5,
            Foreground = textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        headerPanel.Children.Add(subText);
        mainPanel.Children.Add(headerPanel);

        // 2. High-Impact Highlights Hero Banner (Quick KPI summary)
        var heroCard = new Border
        {
            Background = cardBg,
            BorderBrush = cardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 10, 14, 10)
        };
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // KPI 1: Space Reclaimed
        var kpi1 = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
        kpi1.Children.Add(new TextBlock
        {
            Text = "SPACE RECLAIMED".T(),
            FontSize = 9.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        var spaceValueText = new TextBlock
        {
            Text = totalDiskCleanedBytes > 0 ? FormatHelper.FormatBytes(totalDiskCleanedBytes) : "Clean".T(),
            FontSize = 14.5,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = totalDiskCleanedBytes > 0 ? emeraldText : textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Microsoft.UI.Xaml.Documents.Typography.SetNumeralAlignment(spaceValueText, FontNumeralAlignment.Tabular);
        kpi1.Children.Add(spaceValueText);
        Grid.SetColumn(kpi1, 0);
        heroGrid.Children.Add(kpi1);

        // Divider 1
        var div1 = new Border { Width = 1, Height = 26, Background = dividerBrush, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(div1, 1);
        heroGrid.Children.Add(div1);

        // KPI 2: RAM Optimization
        var kpi2 = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
        kpi2.Children.Add(new TextBlock
        {
            Text = "RAM BOOST".T(),
            FontSize = 9.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        var ramValueText = new TextBlock
        {
            Text = ramReclaimedMb > 0 ? $"{ramReclaimedMb:F1} MB" : "Peak Ready".T(),
            FontSize = 14.5,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = ramReclaimedMb > 0 ? skyText : textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Microsoft.UI.Xaml.Documents.Typography.SetNumeralAlignment(ramValueText, FontNumeralAlignment.Tabular);
        kpi2.Children.Add(ramValueText);
        Grid.SetColumn(kpi2, 2);
        heroGrid.Children.Add(kpi2);

        // Divider 2
        var div2 = new Border { Width = 1, Height = 26, Background = dividerBrush, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(div2, 3);
        heroGrid.Children.Add(div2);

        // KPI 3: Health Status
        var kpi3 = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
        kpi3.Children.Add(new TextBlock
        {
            Text = "SYSTEM STATUS".T(),
            FontSize = 9.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = textSecondary,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        kpi3.Children.Add(new TextBlock
        {
            Text = "100% Peak".T(),
            FontSize = 14.5,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = purpleText,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(kpi3, 4);
        heroGrid.Children.Add(kpi3);

        heroCard.Child = heroGrid;
        mainPanel.Children.Add(heroCard);

        // 3. Grouped Diagnostic Breakdown Container
        var breakdownContainer = new Border
        {
            Background = cardBg,
            BorderBrush = cardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 6, 12, 6)
        };

        var breakdownStack = new StackPanel { Spacing = 0 };

        void AddDetailItem(string glyph, Windows.UI.Color darkColor, Windows.UI.Color lightBg, Windows.UI.Color lightBorder, Windows.UI.Color lightFg, string title, string subDesc, string valueDisplay, bool isPositiveHighlight, bool isLast = false)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Themed Icon Badge Box
            var rowIconBox = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(30, darkColor.R, darkColor.G, darkColor.B))
                    : new SolidColorBrush(lightBg),
                BorderBrush = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(60, darkColor.R, darkColor.G, darkColor.B))
                    : new SolidColorBrush(lightBorder),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new FontIcon
                {
                    Glyph = glyph,
                    FontSize = 12.5,
                    Foreground = isDark ? new SolidColorBrush(darkColor) : new SolidColorBrush(lightFg),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(rowIconBox, 0);
            rowGrid.Children.Add(rowIconBox);

            // Title & Subtitle Stack
            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI Variable Display"),
                Foreground = textPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            textStack.Children.Add(titleBlock);

            if (!string.IsNullOrEmpty(subDesc))
            {
                var subDescription = new TextBlock
                {
                    Text = subDesc,
                    FontSize = 10,
                    Foreground = textSecondary
                };
                textStack.Children.Add(subDescription);
            }
            Grid.SetColumn(textStack, 1);
            rowGrid.Children.Add(textStack);

            // Styled Value Pill Badge
            var pillBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2.5, 8, 2.5),
                VerticalAlignment = VerticalAlignment.Center
            };

            var valBlock = new TextBlock
            {
                Text = valueDisplay,
                FontSize = 11.5,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI Variable Display"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Microsoft.UI.Xaml.Documents.Typography.SetNumeralAlignment(valBlock, FontNumeralAlignment.Tabular);

            if (isPositiveHighlight)
            {
                pillBorder.Background = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 236, 253, 245));
                pillBorder.BorderBrush = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(65, 16, 185, 129))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 167, 243, 208));
                pillBorder.BorderThickness = new Thickness(1);
                valBlock.Foreground = emeraldText;
            }
            else
            {
                pillBorder.Background = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(16, 255, 255, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249));
                pillBorder.BorderBrush = isDark
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 255, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
                pillBorder.BorderThickness = new Thickness(1);
                valBlock.Foreground = textSecondary;
            }

            pillBorder.Child = valBlock;
            Grid.SetColumn(pillBorder, 2);
            rowGrid.Children.Add(pillBorder);

            breakdownStack.Children.Add(rowGrid);

            if (!isLast)
            {
                var microDivider = new Border
                {
                    Height = 1,
                    Background = dividerBrush,
                    Margin = new Thickness(38, 0, 0, 0)
                };
                breakdownStack.Children.Add(microDivider);
            }
        }

        AddDetailItem("\uE7F1", 
            Windows.UI.Color.FromArgb(255, 245, 158, 11), Windows.UI.Color.FromArgb(255, 254, 243, 199), Windows.UI.Color.FromArgb(255, 253, 230, 138), Windows.UI.Color.FromArgb(255, 217, 119, 6),
            "Disk Junk Cleaned".T(), "Temporary & cache storage".T(), $"{totalDiskCleanedMb:F1} MB", totalDiskCleanedBytes > 0);

        AddDetailItem("\uE949", 
            Windows.UI.Color.FromArgb(255, 168, 85, 247), Windows.UI.Color.FromArgb(255, 245, 243, 255), Windows.UI.Color.FromArgb(255, 221, 214, 254), Windows.UI.Color.FromArgb(255, 124, 58, 237),
            "Registry Errors Fixed".T(), "Invalid keys & orphaned entries".T(), summary.RegistryIssuesFixed > 0 ? $"{summary.RegistryIssuesFixed} " + "resolved".T() : "0 resolved".T(), summary.RegistryIssuesFixed > 0);

        AddDetailItem("\uE950", 
            Windows.UI.Color.FromArgb(255, 59, 130, 246), Windows.UI.Color.FromArgb(255, 239, 246, 255), Windows.UI.Color.FromArgb(255, 191, 219, 254), Windows.UI.Color.FromArgb(255, 37, 99, 235),
            "RAM Reclaimed (Boost)".T(), "Working set memory freed".T(), ramReclaimedMb > 0 ? $"{ramReclaimedMb:F1} MB" : "0.0 MB", ramReclaimedMb > 0);

        AddDetailItem("\uE8F1", 
            Windows.UI.Color.FromArgb(255, 20, 184, 166), Windows.UI.Color.FromArgb(255, 240, 253, 250), Windows.UI.Color.FromArgb(255, 153, 246, 228), Windows.UI.Color.FromArgb(255, 13, 148, 136),
            "Active Apps Boosted".T(), "Background processes tuned".T(), summary.RamProcessesOptimized > 0 ? $"{summary.RamProcessesOptimized} " + "processes".T() : "0 processes".T(), summary.RamProcessesOptimized > 0);

        AddDetailItem("\uE774", 
            Windows.UI.Color.FromArgb(255, 6, 182, 212), Windows.UI.Color.FromArgb(255, 236, 254, 255), Windows.UI.Color.FromArgb(255, 165, 243, 252), Windows.UI.Color.FromArgb(255, 8, 145, 178),
            "DNS Resolver Cache".T(), "Network socket & cache reset".T(), summary.DnsCacheFlushed ? "Flushed".T() : "Done".T(), summary.DnsCacheFlushed);

        AddDetailItem("\uE945", 
            Windows.UI.Color.FromArgb(255, 236, 72, 153), Windows.UI.Color.FromArgb(255, 253, 242, 248), Windows.UI.Color.FromArgb(255, 251, 207, 232), Windows.UI.Color.FromArgb(255, 219, 39, 119),
            "Performance Tweaks".T(), "System responsiveness applied".T(), summary.TweaksApplied > 0 ? $"{summary.TweaksApplied} " + "activated".T() : "0 activated".T(), summary.TweaksApplied > 0, isLast: true);

        breakdownContainer.Child = breakdownStack;
        mainPanel.Children.Add(breakdownContainer);

        // 4. Restart Recommended Banner (if tweaks applied)
        if (summary.TweaksApplied > 0)
        {
            var restartWarningBorder = new Border
            {
                Background = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(25, 245, 158, 11)) 
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199)),
                BorderBrush = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(65, 245, 158, 11)) 
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 253, 230, 138)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 2, 0, 0)
            };

            var warningPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
            var warningIcon = new FontIcon
            {
                Glyph = "\uE7BA",
                FontSize = 15,
                Foreground = isDark 
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)) 
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 119, 6)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var warningText = new TextBlock
            {
                Text = "Restart is recommended to fully apply system tweaks.".T(),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isDark 
                    ? textPrimary 
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 146, 64, 14)),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 350
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
            RequestedTheme = currentTheme,
            Background = dialogBg,
            BorderBrush = dialogBorder,
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1)
        };

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj) && styleObj is Style accentStyle)
        {
            dialog.CloseButtonStyle = accentStyle;
        }

        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }

    public string GetStatusText(int score)
    {
        if (score >= 90) return "Excellent".T().ToUpperInvariant();
        if (score >= 70) return "Good".T().ToUpperInvariant();
        return "Needs Optimization".T().ToUpperInvariant();
    }

    public Brush GetHealthScoreBrush(int score)
    {
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        if (score >= 90) return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 5, 150, 105) : Windows.UI.Color.FromArgb(255, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 217, 119, 6) : Windows.UI.Color.FromArgb(255, 245, 158, 11)); 
        return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 220, 38, 38) : Windows.UI.Color.FromArgb(255, 239, 68, 68)); 
    }

    public Brush GetHealthScoreBadgeBackground(int score)
    {
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        if (score >= 90) return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 236, 253, 245) : Windows.UI.Color.FromArgb(30, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 255, 251, 235) : Windows.UI.Color.FromArgb(30, 245, 158, 11));
        return new SolidColorBrush(isLight ? Windows.UI.Color.FromArgb(255, 254, 242, 242) : Windows.UI.Color.FromArgb(30, 239, 68, 68));
    }

    public bool IsNot(bool val) => !val;
    public string FormatPercent(double val) => $"{val:F1}%";

    public Visibility GetVisibility(int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal Visibility GetProgressVisibility(bool isScanning)
    {
        return isScanning ? Visibility.Visible : Visibility.Collapsed;
    }

    internal bool GetOneClickEnabled(bool isScanning, bool isOptimizing)
    {
        return !isScanning && !isOptimizing;
    }

    internal Visibility GetOptimizeVisibility(bool hasScanned, bool isScanning)
    {
        return !isScanning ? Visibility.Visible : Visibility.Collapsed;
    }

    internal bool GetProgressRingActive(bool isScanning, bool isOptimizing)
    {
        return isScanning || isOptimizing;
    }

    internal Visibility GetProgressRingVisibility(bool isScanning, bool isOptimizing)
    {
        return (isScanning || isOptimizing) ? Visibility.Visible : Visibility.Collapsed;
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
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        var color = healthy ? 
            (isLight ? Windows.UI.Color.FromArgb(255, 5, 150, 105) : Windows.UI.Color.FromArgb(255, 16, 185, 129)) : 
            (isLight ? Windows.UI.Color.FromArgb(255, 217, 119, 6) : Windows.UI.Color.FromArgb(255, 245, 158, 11));
        return new SolidColorBrush(color);
    }

    internal static Brush GetStatusBadgeBg(bool healthy)
    {
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        var color = healthy ? 
            (isLight ? Windows.UI.Color.FromArgb(255, 236, 253, 245) : Windows.UI.Color.FromArgb(30, 16, 185, 129)) : 
            (isLight ? Windows.UI.Color.FromArgb(255, 255, 251, 235) : Windows.UI.Color.FromArgb(30, 245, 158, 11));
        return new SolidColorBrush(color);
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        if (sender is UIElement elem)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(elem, 1.08f, 350);
        }
        if (RamCard != null)
        {
            WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(RamCard, 1.05f, 350);
        }
        await ViewModel.BoostRamAsync();
    }

    private async void OnCleanDiskClick(object sender, RoutedEventArgs e)
    {
        if (sender is UIElement elem)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(elem, 1.08f, 350);
        }
        if (DiskCard != null)
        {
            WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(DiskCard, 1.05f, 350);
        }
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
            TranslationManager.Instance.Translate(DeepLayerPanel);
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
        GetMainPage()?.NavigateToPageExternal("optimizer");
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
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        var color = hasBottleneck ? 
            (isLight ? Windows.UI.Color.FromArgb(255, 254, 242, 242) : Windows.UI.Color.FromArgb(30, 239, 68, 68)) : 
            (isLight ? Windows.UI.Color.FromArgb(255, 236, 253, 245) : Windows.UI.Color.FromArgb(30, 16, 185, 129));
        return new SolidColorBrush(color);
    }

    internal Brush GetBottleneckBadgeFg(bool hasBottleneck)
    {
        bool isLight = ThemeManager.Instance.CurrentTheme == ElementTheme.Light;
        var color = hasBottleneck ? 
            (isLight ? Windows.UI.Color.FromArgb(255, 220, 38, 38) : Microsoft.UI.Colors.Crimson) : 
            (isLight ? Windows.UI.Color.FromArgb(255, 5, 150, 105) : Microsoft.UI.Colors.MediumSeaGreen);
        return new SolidColorBrush(color);
    }

    internal Visibility GetBoolVisibility(bool val) => val ? Visibility.Visible : Visibility.Collapsed;

    public string FormatRamText(double val)
    {
        string template = "RAM In Use {0:F0}%".T();
        if (template.Contains("{0"))
        {
            return string.Format(template, val);
        }
        return $"RAM {val:F0}%";
    }

    #region Flagship Visual Effects & Circular Gauges

    private void SetupVisualEffectsAndGauges()
    {
        // 1. Initial gauge calculations
        UpdateAllCircularGauges();

        // 2. Interactive Hover Lift (Offset.Y = -3f over 150ms cubic-bezier) & Specular Luminous Rim
        if (HealthGaugeCard != null) FluidAnimationHelper.ApplyCardHoverElevation(HealthGaugeCard, -3f);
        if (QuickStatsCardDisk != null) FluidAnimationHelper.ApplyCardHoverElevation(QuickStatsCardDisk, -3f);
        if (QuickStatsCardStartup != null) FluidAnimationHelper.ApplyCardHoverElevation(QuickStatsCardStartup, -3f);
        if (QuickStatsCardSecurity != null) FluidAnimationHelper.ApplyCardHoverElevation(QuickStatsCardSecurity, -3f);
        if (QuickStatsCardRegistry != null) FluidAnimationHelper.ApplyCardHoverElevation(QuickStatsCardRegistry, -3f);
        if (CpuCard != null) FluidAnimationHelper.ApplyCardHoverElevation(CpuCard, -3f);
        if (RamCard != null) FluidAnimationHelper.ApplyCardHoverElevation(RamCard, -3f);
        if (GpuCard != null) FluidAnimationHelper.ApplyCardHoverElevation(GpuCard, -3f);
        if (DiskCard != null) FluidAnimationHelper.ApplyCardHoverElevation(DiskCard, -3f);
        if (PerformanceChartCard != null) FluidAnimationHelper.ApplyCardHoverElevation(PerformanceChartCard, -3f);

        // 3. Spring Natural Motion Button Physics (DampingRatio = 0.6f, Period = 100ms)
        if (OneClickCareBtn != null) FluidAnimationHelper.ApplyButtonPressPhysics(OneClickCareBtn);
        if (ScanSystemBtn != null) FluidAnimationHelper.ApplyButtonPressPhysics(ScanSystemBtn);
        if (OptimizeBtn != null) FluidAnimationHelper.ApplyButtonPressPhysics(OptimizeBtn);

        // 4. Shimmer Specular Sweep on Primary CTA Button
        ApplyPrimaryCtaShimmerSweep();
    }

    private void ApplyPrimaryCtaShimmerSweep()
    {
        if (OneClickCareBtn == null) return;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(OneClickCareBtn);
            if (visual == null) return;
            var compositor = visual.Compositor;

            OneClickCareBtn.PointerEntered += (s, e) =>
            {
                try
                {
                    var anim = compositor.CreateScalarKeyFrameAnimation();
                    anim.InsertKeyFrame(0.0f, 1.0f);
                    anim.InsertKeyFrame(0.5f, 1.025f, compositor.CreateCubicBezierEasingFunction(new System.Numerics.Vector2(0.1f, 0.9f), new System.Numerics.Vector2(0.2f, 1.0f)));
                    anim.InsertKeyFrame(1.0f, 1.0f);
                    anim.Duration = TimeSpan.FromMilliseconds(400);
                    visual.StartAnimation("Scale.X", anim);
                    visual.StartAnimation("Scale.Y", anim);
                }
                catch { }
            };
        }
        catch { }
    }

    private void UpdateAllCircularGauges()
    {
        UpdateCircularGauge(CpuPathFigure, CpuArcSegment, CpuGlowBorder, ViewModel.CpuUsage, 28, 36, 36);
        UpdateCircularGauge(RamPathFigure, RamArcSegment, RamGlowBorder, ViewModel.RamUsage, 28, 36, 36);
        UpdateCircularGauge(GpuPathFigure, GpuArcSegment, GpuGlowBorder, ViewModel.GpuUsage, 28, 36, 36);
        UpdateCircularGauge(DiskPathFigure, DiskArcSegment, DiskGlowBorder, ViewModel.DiskUsage, 28, 36, 36);
        UpdateCircularGauge(HealthPathFigure, HealthArcSegment, HealthGlowBorder, ViewModel.HealthScore, 28, 38, 38);
    }

    private void UpdateCircularGauge(PathFigure? figure, ArcSegment? arc, Border? glowBorder, double percentage, double radius = 28, double centerX = 36, double centerY = 36)
    {
        if (figure == null || arc == null) return;

        double pct = Math.Clamp(percentage, 0.01, 99.99);
        double angleDegrees = pct * 3.6;
        double angleRadians = (angleDegrees - 90.0) * Math.PI / 180.0;

        double startX = centerX;
        double startY = centerY - radius;
        figure.StartPoint = new Windows.Foundation.Point(startX, startY);

        double endX = centerX + radius * Math.Cos(angleRadians);
        double endY = centerY + radius * Math.Sin(angleRadians);

        arc.Point = new Windows.Foundation.Point(endX, endY);
        arc.Size = new Windows.Foundation.Size(radius, radius);
        arc.IsLargeArc = angleDegrees > 180.0;
        arc.SweepDirection = SweepDirection.Clockwise;

        if (glowBorder != null)
        {
            string glowKey = pct switch
            {
                > 85.0 => "GaugeGlowCriticalBrush",
                > 65.0 => "GaugeGlowWarningBrush",
                _ => "GaugeGlowGoodBrush"
            };

            if (Application.Current.Resources.TryGetValue(glowKey, out var brushObj) && brushObj is Brush b)
            {
                glowBorder.Background = b;
            }
        }
    }

    #endregion
}

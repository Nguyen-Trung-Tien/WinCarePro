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
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        
        ViewModel = App.Services?.GetService<DashboardViewModel>() ?? new DashboardViewModel(this.DispatcherQueue);
        ViewModel.DispatcherQueue = this.DispatcherQueue;
        this.Loaded += async (s, e) => 
        {
            ViewModel.DispatcherQueue = this.DispatcherQueue;
            DataContext = ViewModel;

            // Lazy load the extended layer after initial UI renders to prevent lag
            await Task.Delay(100);
            ViewModel.IsExtendedLayerLoaded = true;
            
            // Force responsive update after extended layer is fully initialized
            UpdateResponsiveLayout(this.ActualWidth);

            // v4.0.0 — Auto-trigger embedded AI WinCare Engine scan
            _ = ViewModel.RunEmbeddedAiScanAsync().ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    // Animate the AI score count-up after scan completes
                    if (ViewModel.HasAiScanned && ViewModel.AiRecommendations.Count > 0)
                    {
                        var topRec = ViewModel.AiRecommendations[0];
                        TopRecommendationTitle.Text = topRec.Title;
                        TopRecommendationDesc.Text = topRec.Description;
                    }
                });
            }, TaskScheduler.Default);

            // Translate page content
            TranslationManager.Instance.Translate(this);
        };

        this.Unloaded += (s, e) =>
        {
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

    private void OnLaunchAiWinCareEngineClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindowInstance is MainWindow mw && mw.MainFrame.Content is MainPage mp)
            {
                mp.NavigateToPageExternal("aiwincareengine");
            }
        }
        catch { }
    }

    private void OnLaunchDesktopWidgetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.ShowWindow();
        }
        catch { }
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
        catch { }
    }

    /// <summary>
    /// v4.0.0 — Handles the embedded AI WinCare Engine scan button click on Dashboard.
    /// </summary>
    private async void OnEmbeddedAiScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunEmbeddedAiScanAsync();

        // Update top recommendation card after scan
        if (ViewModel.HasAiScanned && ViewModel.AiRecommendations.Count > 0)
        {
            var topRec = ViewModel.AiRecommendations[0];
            TopRecommendationTitle.Text = topRec.Title;
            TopRecommendationDesc.Text = topRec.Description;

            // Animate the recommendation card entrance
            FluidAnimationHelper.ApplySpringEntranceAnimation(TopAiRecommendationCard, 100);
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        bool isWide = width >= 800;
        
        if (LeftCol != null && RightCol != null)
        {
            if (isWide)
            {
                // Wide layout: 2 equal 50/50 columns
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(1, GridUnitType.Star);
                
                // Row 0: System Health Overview (Left) & AI WinCare Engine (Right) — SIDE-BY-SIDE
                if (HealthGaugeCard != null)
                {
                    Grid.SetRow(HealthGaugeCard, 0);
                    Grid.SetColumn(HealthGaugeCard, 0);
                    Grid.SetColumnSpan(HealthGaugeCard, 1);
                }
                if (AiWinCareEngineEmbeddedPanel != null)
                {
                    Grid.SetRow(AiWinCareEngineEmbeddedPanel, 0);
                    Grid.SetColumn(AiWinCareEngineEmbeddedPanel, 1);
                    Grid.SetColumnSpan(AiWinCareEngineEmbeddedPanel, 1);
                }

                // Row 1: CPU/RAM (Left Col 0) & Bottleneck Advisor (Right Col 1) — SIDE-BY-SIDE
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
                if (BottleneckCard != null)
                {
                    Grid.SetRow(BottleneckCard, 1);
                    Grid.SetColumn(BottleneckCard, 1);
                    Grid.SetColumnSpan(BottleneckCard, 1);
                }

                // Row 2: GPU/Disk (Left Col 0) & Quick Stats (Right Col 1) — SIDE-BY-SIDE
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
                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 2);
                    Grid.SetColumn(QuickStatsGrid, 1);
                    Grid.SetColumnSpan(QuickStatsGrid, 1);
                }

                // Row 3: Smart AI Advice (Left Col 0) & Performance Trend Chart (Right Col 1)
                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 3);
                    Grid.SetColumn(RecommendationsCard, 0);
                    Grid.SetColumnSpan(RecommendationsCard, 1);
                    Grid.SetRowSpan(RecommendationsCard, 1);
                }

                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 3);
                    Grid.SetColumn(PerformanceChartCard, 1);
                    Grid.SetColumnSpan(PerformanceChartCard, 1);
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
                if (AiWinCareEngineEmbeddedPanel != null)
                {
                    Grid.SetRow(AiWinCareEngineEmbeddedPanel, 1);
                    Grid.SetColumn(AiWinCareEngineEmbeddedPanel, 0);
                    Grid.SetColumnSpan(AiWinCareEngineEmbeddedPanel, 2);
                }
                if (BottleneckCard != null)
                {
                    Grid.SetRow(BottleneckCard, 2);
                    Grid.SetColumn(BottleneckCard, 0);
                    Grid.SetColumnSpan(BottleneckCard, 2);
                }
                if (CpuRamGrid != null)
                {
                    Grid.SetRow(CpuRamGrid, 3);
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
                if (QuickStatsGrid != null)
                {
                    Grid.SetRow(QuickStatsGrid, 5);
                    Grid.SetColumn(QuickStatsGrid, 0);
                    Grid.SetColumnSpan(QuickStatsGrid, 2);
                }
                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 6);
                    Grid.SetColumn(PerformanceChartCard, 0);
                    Grid.SetColumnSpan(PerformanceChartCard, 2);
                }
                if (RecommendationsCard != null)
                {
                    Grid.SetRow(RecommendationsCard, 7);
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
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (HealthGaugeCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(HealthGaugeCard, Windows.UI.Color.FromArgb(120, 0, 242, 254));
            }
        }
        catch { }

        await ViewModel.RunFullDiagnosticsAsync();

        try
        {
            if (HealthGaugeCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(HealthGaugeCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(HealthGaugeCard, Windows.UI.Color.FromArgb(120, 0, 242, 254));
            }
        }
        catch { }
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
        try
        {
            if (HealthGaugeCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(HealthGaugeCard, Windows.UI.Color.FromArgb(220, 16, 185, 129));
            }
        }
        catch { }

        var summary = await ViewModel.OptimizeSystemAsync(mode);

        try
        {
            if (HealthGaugeCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(HealthGaugeCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(HealthGaugeCard, Windows.UI.Color.FromArgb(255, 16, 185, 129));
            }

            // Cascade 3D Quantum Ripple Wave through telemetry cards
            var rippleCards = new System.Collections.Generic.List<FrameworkElement>();
            if (CpuCard != null) rippleCards.Add(CpuCard);
            if (RamCard != null) rippleCards.Add(RamCard);
            if (GpuCard != null) rippleCards.Add(GpuCard);
            if (DiskCard != null) rippleCards.Add(DiskCard);
            if (TopAiRecommendationCard != null) rippleCards.Add(TopAiRecommendationCard);

            WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DCascadeWave(rippleCards, 70);
        }
        catch { }

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
        bool isDark = currentTheme == ElementTheme.Dark ||
                      (currentTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);

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
            "DNS Resolver Cache".T(), "Network socket & cache reset".T(), summary.DnsCacheFlushed ? "✓ " + "Flushed".T() : "Done".T(), summary.DnsCacheFlushed);

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
        if (score >= 90) return "EXCELLENT - Your system is highly optimized and clean.".T();
        if (score >= 70) return "GOOD - Some areas can be optimized to reclaim storage.".T();
        return "NEEDS OPTIMIZATION - Heavy junk logs or updates required.".T();
    }

    public Brush GetHealthScoreBrush(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); 
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); 
    }

    public Brush GetHealthScoreBadgeBackground(int score)
    {
        if (score >= 90) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129)); 
        if (score >= 70) return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 245, 158, 11));
        return new SolidColorBrush(Windows.UI.Color.FromArgb(30, 239, 68, 68));
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
        if (sender is UIElement elem)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(elem, 1.08f, 350);
        }
        await ViewModel.BoostRamAsync();
    }

    private async void OnCleanDiskClick(object sender, RoutedEventArgs e)
    {
        if (sender is UIElement elem)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(elem, 1.08f, 350);
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
        var color = hasBottleneck ? Windows.UI.Color.FromArgb(30, 239, 68, 68) : Windows.UI.Color.FromArgb(30, 16, 185, 129);
        return new SolidColorBrush(color);
    }

    internal Brush GetBottleneckBadgeFg(bool hasBottleneck)
    {
        var color = hasBottleneck ? Microsoft.UI.Colors.Crimson : Microsoft.UI.Colors.MediumSeaGreen;
        return new SolidColorBrush(color);
    }

    internal Visibility GetBoolVisibility(bool val) => val ? Visibility.Visible : Visibility.Collapsed;
}

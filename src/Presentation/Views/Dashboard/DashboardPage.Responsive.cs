using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0) return;

        bool isWide = width >= 850;
        
        if (LeftCol != null && RightCol != null)
        {
            if (isWide)
            {
                // Wide layout: 2 equal 50/50 columns (Sensors Left, Chart Right)
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(1, GridUnitType.Star);
                
                if (HardwareSensorsPanel != null)
                {
                    Grid.SetRow(HardwareSensorsPanel, 0);
                    Grid.SetColumn(HardwareSensorsPanel, 0);
                    Grid.SetColumnSpan(HardwareSensorsPanel, 1);
                }

                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 0);
                    Grid.SetColumn(PerformanceChartCard, 1);
                    Grid.SetColumnSpan(PerformanceChartCard, 1);
                }
            }
            else
            {
                // Narrow layout: Stacked in single column
                LeftCol.Width = new GridLength(1, GridUnitType.Star);
                RightCol.Width = new GridLength(0, GridUnitType.Pixel);
                
                if (HardwareSensorsPanel != null)
                {
                    Grid.SetRow(HardwareSensorsPanel, 0);
                    Grid.SetColumn(HardwareSensorsPanel, 0);
                    Grid.SetColumnSpan(HardwareSensorsPanel, 1);
                }
                if (PerformanceChartCard != null)
                {
                    Grid.SetRow(PerformanceChartCard, 1);
                    Grid.SetColumn(PerformanceChartCard, 0);
                    Grid.SetColumnSpan(PerformanceChartCard, 1);
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
}

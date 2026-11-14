using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
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
                catch (Exception pEx)
                {
                    CrashLogger.LogException("DashboardPage.PointerEnteredAnim", pEx);
                }
            };
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardPage.ApplyPrimaryCtaShimmerSweep", ex);
        }
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

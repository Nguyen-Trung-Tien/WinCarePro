using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Zero-Bloat flat stub: All 3D tilt, spatial depth, shockwaves, and composition pointer hooks have been permanently disabled.
/// </summary>
public static class Animation3DHelper
{
    public static readonly DependencyProperty Is3DTiltEnabledProperty =
        DependencyProperty.RegisterAttached("Is3DTiltEnabled", typeof(bool), typeof(Animation3DHelper), new PropertyMetadata(false));

    public static bool GetIs3DTiltEnabled(FrameworkElement element) => false;
    public static void SetIs3DTiltEnabled(FrameworkElement element, bool value) { }

    public static readonly DependencyProperty MaxTiltAngleProperty =
        DependencyProperty.RegisterAttached("MaxTiltAngle", typeof(double), typeof(Animation3DHelper), new PropertyMetadata(0.0));

    public static double GetMaxTiltAngle(FrameworkElement element) => 0.0;
    public static void SetMaxTiltAngle(FrameworkElement element, double value) { }

    public static readonly DependencyProperty DepthZProperty =
        DependencyProperty.RegisterAttached("DepthZ", typeof(double), typeof(Animation3DHelper), new PropertyMetadata(0.0));

    public static double GetDepthZ(FrameworkElement element) => 0.0;
    public static void SetDepthZ(FrameworkElement element, double value) { }

    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached("HoverScale", typeof(double), typeof(Animation3DHelper), new PropertyMetadata(1.0));

    public static double GetHoverScale(FrameworkElement element) => 1.0;
    public static void SetHoverScale(FrameworkElement element, double value) { }

    public static readonly DependencyProperty IsMagneticEnabledProperty =
        DependencyProperty.RegisterAttached("IsMagneticEnabled", typeof(bool), typeof(Animation3DHelper), new PropertyMetadata(false));

    public static bool GetIsMagneticEnabled(FrameworkElement element) => false;
    public static void SetIsMagneticEnabled(FrameworkElement element, bool value) { }

    public static void Start3DScanEffect(FrameworkElement element, Windows.UI.Color? beamColor = null) { }
    public static void Stop3DScanEffect(FrameworkElement element) { }
    public static void StopAll3DScanEffects() { }
    public static void Trigger3DOptimizeBurst(FrameworkElement element, Windows.UI.Color? auraColor = null) { }
    public static void Trigger3DCascadeWave(IEnumerable<FrameworkElement> elements, double delayStepMs = 45) { }
}


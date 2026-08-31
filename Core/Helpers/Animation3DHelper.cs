using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Next-Gen 3D Interaction Engine for WinUI 3 (Windows App SDK).
/// Provides hardware-accelerated 3D Parallax Tilt, Spatial Depth (Translation.Z),
/// Tactile Spring Rebound, and Magnetic Pointer Pull using Microsoft.UI.Composition.
/// Zero memory leaks with ConditionalWeakTable and automatic lifecycle management.
/// </summary>
public static class Animation3DHelper
{
    private static readonly ConditionalWeakTable<FrameworkElement, Element3DState> _states = new();

    private class Element3DState
    {
        public bool IsPointerOver { get; set; }
        public bool IsPressed { get; set; }
        public Visual? Visual { get; set; }
        public Compositor? Compositor { get; set; }
        public DateTime LastMoveTime { get; set; }
    }

    private static Element3DState GetOrCreateState(FrameworkElement element)
    {
        return _states.GetValue(element, el =>
        {
            var visual = ElementCompositionPreview.GetElementVisual(el);
            return new Element3DState
            {
                Visual = visual,
                Compositor = visual?.Compositor
            };
        });
    }

    // =========================================================================
    // 1. Attached Property: Is3DTiltEnabled
    // =========================================================================
    public static readonly DependencyProperty Is3DTiltEnabledProperty =
        DependencyProperty.RegisterAttached(
            "Is3DTiltEnabled",
            typeof(bool),
            typeof(Animation3DHelper),
            new PropertyMetadata(false, OnIs3DTiltEnabledChanged));

    public static bool GetIs3DTiltEnabled(FrameworkElement element) => (bool)element.GetValue(Is3DTiltEnabledProperty);
    public static void SetIs3DTiltEnabled(FrameworkElement element, bool value) => element.SetValue(Is3DTiltEnabledProperty, value);

    // =========================================================================
    // 2. Attached Property: MaxTiltAngle (Degrees)
    // =========================================================================
    public static readonly DependencyProperty MaxTiltAngleProperty =
        DependencyProperty.RegisterAttached(
            "MaxTiltAngle",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(0.0));

    public static double GetMaxTiltAngle(FrameworkElement element) => (double)element.GetValue(MaxTiltAngleProperty);
    public static void SetMaxTiltAngle(FrameworkElement element, double value) => element.SetValue(MaxTiltAngleProperty, value);

    // =========================================================================
    // 3. Attached Property: DepthZ (Spatial elevation)
    // =========================================================================
    public static readonly DependencyProperty DepthZProperty =
        DependencyProperty.RegisterAttached(
            "DepthZ",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(0.0));

    public static double GetDepthZ(FrameworkElement element) => (double)element.GetValue(DepthZProperty);
    public static void SetDepthZ(FrameworkElement element, double value) => element.SetValue(DepthZProperty, value);

    // =========================================================================
    // 4. Attached Property: HoverScale
    // =========================================================================
    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached(
            "HoverScale",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(1.0));

    public static double GetHoverScale(FrameworkElement element) => (double)element.GetValue(HoverScaleProperty);
    public static void SetHoverScale(FrameworkElement element, double value) => element.SetValue(HoverScaleProperty, value);

    // =========================================================================
    // 5. Attached Property: IsMagneticEnabled (Pulls element slightly toward cursor)
    // =========================================================================
    public static readonly DependencyProperty IsMagneticEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsMagneticEnabled",
            typeof(bool),
            typeof(Animation3DHelper),
            new PropertyMetadata(false));

    public static bool GetIsMagneticEnabled(FrameworkElement element) => (bool)element.GetValue(IsMagneticEnabledProperty);
    public static void SetIsMagneticEnabled(FrameworkElement element, bool value) => element.SetValue(IsMagneticEnabledProperty, value);

    // =========================================================================
    // Property Change Callbacks
    // =========================================================================
    private static void OnIs3DTiltEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.PointerEntered -= Element_PointerEntered;
            element.PointerMoved -= Element_PointerMoved;
            element.PointerExited -= Element_PointerExited;
            element.PointerPressed -= Element_PointerPressed;
            element.PointerReleased -= Element_PointerReleased;
            element.PointerCanceled -= Element_PointerExited;
            element.SizeChanged -= Element_SizeChanged;
            element.Unloaded -= Element_Unloaded;

            if (e.NewValue is bool enabled && enabled)
            {
                ElementCompositionPreview.SetIsTranslationEnabled(element, true);
                element.PointerEntered += Element_PointerEntered;
                element.PointerMoved += Element_PointerMoved;
                element.PointerExited += Element_PointerExited;
                element.PointerPressed += Element_PointerPressed;
                element.PointerReleased += Element_PointerReleased;
                element.PointerCanceled += Element_PointerExited;
                element.SizeChanged += Element_SizeChanged;
                element.Unloaded += Element_Unloaded;
            }
            else
            {
                Reset3DTransform(element, 0);
            }
        }
    }

    private static void Element_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            Reset3DTransform(element, 0);
            _states.Remove(element);
        }
    }

    private static void Element_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UpdateCenterPoint(element);
        }
    }

    private static void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Hover protrusion disabled per user preference
    }

    private static void Element_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Hover protrusion disabled per user preference
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPointerOver = false;
            state.IsPressed = false;
            Reset3DTransform(element, 200);
        }
    }

    private static void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPressed = true;

            var pt = e.GetCurrentPoint(element).Position;
            Apply3DDepression(element, (float)pt.X, (float)pt.Y, 90);
        }
    }

    private static void Element_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPressed = false;
            Reset3DTransform(element, 200);
        }
    }

    // =========================================================================
    // Core 3D Composition Logic
    // =========================================================================
    private static void UpdateCenterPoint(FrameworkElement element)
    {
        try
        {
            var w = (float)element.ActualWidth;
            var h = (float)element.ActualHeight;

            if (w > 0 && h > 0 && !float.IsNaN(w) && !float.IsNaN(h) && !float.IsInfinity(w) && !float.IsInfinity(h))
            {
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual != null)
                {
                    visual.Size = new Vector2(w, h);
                    visual.CenterPoint = new Vector3(w / 2.0f, h / 2.0f, 0.0f);
                }
            }
        }
        catch { }
    }

    private static void Apply3DTilt(FrameworkElement element, float cursorX, float cursorY, double durationMs)
    {
        // Hover protrusion disabled per user preference
    }

    private static void Apply3DDepression(FrameworkElement element, float cursorX, float cursorY, double durationMs)
    {
        try
        {
            var w = (float)element.ActualWidth;
            var h = (float)element.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;
            var compositor = visual.Compositor;

            UpdateCenterPoint(element);

            float nx = Math.Clamp(((cursorX - (w / 2f)) / (w / 2f)), -1.0f, 1.0f);
            float ny = Math.Clamp(((cursorY - (h / 2f)) / (h / 2f)), -1.0f, 1.0f);

            double maxTiltDeg = GetMaxTiltAngle(element) * 0.4;
            float rotX = -ny * (float)maxTiltDeg;
            float rotY = nx * (float)maxTiltDeg;

            float totalAngle = MathF.Sqrt((rotX * rotX) + (rotY * rotY));
            if (totalAngle > 0.001f)
            {
                visual.RotationAxis = new Vector3(rotX / totalAngle, rotY / totalAngle, 0.0f);
            }

            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.15f, 0.95f), new Vector2(0.3f, 1.0f));

            var rotAnim = compositor.CreateScalarKeyFrameAnimation();
            rotAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            rotAnim.InsertKeyFrame(1.0f, totalAngle, easing);
            visual.StartAnimation("RotationAngleInDegrees", rotAnim);

            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(0.965f, 0.965f, 1.0f), easing);
            visual.StartAnimation("Scale", scaleAnim);

            var transAnim = compositor.CreateVector3KeyFrameAnimation();
            transAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            transAnim.InsertKeyFrame(1.0f, new Vector3(0, 0, -8.0f), easing);
            visual.StartAnimation("Translation", transAnim);
        }
        catch { }
    }

    private static void Reset3DTransform(FrameworkElement element, double durationMs)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;
            var compositor = visual.Compositor;

            UpdateCenterPoint(element);

            if (durationMs <= 0)
            {
                visual.RotationAngleInDegrees = 0.0f;
                visual.Scale = new Vector3(1.0f, 1.0f, 1.0f);
                visual.Properties.InsertVector3("Translation", Vector3.Zero);
                return;
            }

            var springEasing = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.18f, 0.92f),
                new Vector2(0.28f, 1.0f)
            );

            var rotAnim = compositor.CreateScalarKeyFrameAnimation();
            rotAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            rotAnim.InsertKeyFrame(1.0f, 0.0f, springEasing);
            visual.StartAnimation("RotationAngleInDegrees", rotAnim);

            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), springEasing);
            visual.StartAnimation("Scale", scaleAnim);

            var transAnim = compositor.CreateVector3KeyFrameAnimation();
            transAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            transAnim.InsertKeyFrame(1.0f, Vector3.Zero, springEasing);
            visual.StartAnimation("Translation", transAnim);
        }
        catch { }
    }

    // =========================================================================
    // 6. 3D SYSTEM SCANNING EFFECT (Ultra-Smooth Holographic Ambient Scan Beam)
    // =========================================================================

    private static readonly Dictionary<FrameworkElement, SpriteVisual> _activeScanVisuals = new();

    /// <summary>
    /// Starts a smooth, hardware-accelerated Holographic Ambient Scan Beam across a UI element.
    /// The host card remains firmly stable (no jitter, shaking or strobing).
    /// </summary>
    public static void Start3DScanEffect(FrameworkElement element, Windows.UI.Color? beamColor = null)
    {
        try
        {
            if (element == null) return;
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            if (hostVisual == null) return;
            var compositor = hostVisual.Compositor;

            float w = (float)Math.Max(element.ActualWidth, 120);
            float h = (float)Math.Max(element.ActualHeight, 120);

            Stop3DScanEffect(element); // Clear previous if active

            var scanVisual = compositor.CreateSpriteVisual();
            scanVisual.Size = new Vector2(w, Math.Min(64, h * 0.3f));
            scanVisual.CenterPoint = new Vector3(w / 2f, scanVisual.Size.Y / 2f, 0);

            var color = beamColor ?? Windows.UI.Color.FromArgb(120, 0, 242, 254);
            var transparent = Windows.UI.Color.FromArgb(0, color.R, color.G, color.B);

            var gradientBrush = compositor.CreateLinearGradientBrush();
            gradientBrush.StartPoint = new Vector2(0.5f, 0.0f);
            gradientBrush.EndPoint = new Vector2(0.5f, 1.0f);

            var stop0 = compositor.CreateColorGradientStop(0.0f, transparent);
            var stop1 = compositor.CreateColorGradientStop(0.5f, color);
            var stop2 = compositor.CreateColorGradientStop(1.0f, transparent);

            gradientBrush.ColorStops.Add(stop0);
            gradientBrush.ColorStops.Add(stop1);
            gradientBrush.ColorStops.Add(stop2);

            scanVisual.Brush = gradientBrush;
            scanVisual.Opacity = 0.55f;

            // Smooth, slow, elegant sine sweep without strobing
            var sineEasing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0.0f), new Vector2(0.58f, 1.0f));
            var sweepAnim = compositor.CreateVector3KeyFrameAnimation();
            sweepAnim.Duration = TimeSpan.FromMilliseconds(3000);
            sweepAnim.IterationBehavior = AnimationIterationBehavior.Forever;
            sweepAnim.InsertKeyFrame(0.0f, new Vector3(0, -scanVisual.Size.Y, 0), sineEasing);
            sweepAnim.InsertKeyFrame(0.5f, new Vector3(0, h, 0), sineEasing);
            sweepAnim.InsertKeyFrame(1.0f, new Vector3(0, -scanVisual.Size.Y, 0), sineEasing);
            scanVisual.StartAnimation("Offset", sweepAnim);

            ElementCompositionPreview.SetElementChildVisual(element, scanVisual);
            _activeScanVisuals[element] = scanVisual;
        }
        catch { }
    }

    /// <summary>
    /// Smoothly fades out and stops the Holographic Scan Beam without flickering.
    /// </summary>
    public static void Stop3DScanEffect(FrameworkElement element)
    {
        try
        {
            if (element == null) return;

            if (_activeScanVisuals.TryGetValue(element, out var scanVisual))
            {
                _activeScanVisuals.Remove(element);
                var compositor = scanVisual.Compositor;

                // Soft fade out over 250ms
                var fadeAnim = compositor.CreateScalarKeyFrameAnimation();
                fadeAnim.Duration = TimeSpan.FromMilliseconds(250);
                fadeAnim.InsertKeyFrame(1.0f, 0.0f);
                scanVisual.StartAnimation("Opacity", fadeAnim);

                var cleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
                cleanupTimer.Tick += (s, e) =>
                {
                    cleanupTimer.Stop();
                    try
                    {
                        ElementCompositionPreview.SetElementChildVisual(element, null);
                        scanVisual.Dispose();
                    }
                    catch { }
                };
                cleanupTimer.Start();
            }
        }
        catch { }
    }

    // =========================================================================
    // 7. 3D SYSTEM OPTIMIZATION EFFECT (Refined, Non-Jarring Tactile Glow Pulse)
    // =========================================================================

    /// <summary>
    /// Triggers a smooth, elegant pulse and soft ambient aura upon system optimization completion.
    /// Free of aggressive screen shaking or violent flashing.
    /// </summary>
    public static void Trigger3DOptimizeBurst(FrameworkElement element, Windows.UI.Color? auraColor = null)
    {
        try
        {
            if (element == null) return;
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            if (hostVisual == null) return;
            var compositor = hostVisual.Compositor;

            UpdateCenterPoint(element);

            // Subtle, luxurious micro-scale pulse (1.0 -> 1.012 -> 1.0)
            var smoothEasing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.3f, 1.0f));
            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(400);
            scaleAnim.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
            scaleAnim.InsertKeyFrame(0.4f, new Vector3(1.012f, 1.012f, 1.0f), smoothEasing);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), smoothEasing);
            hostVisual.StartAnimation("Scale", scaleAnim);

            // Soft Ambient Aura Ring
            float w = (float)Math.Max(element.ActualWidth, 100);
            float h = (float)Math.Max(element.ActualHeight, 100);

            var shockwave = compositor.CreateSpriteVisual();
            shockwave.Size = new Vector2(w, h);
            shockwave.CenterPoint = new Vector3(w / 2f, h / 2f, 0);

            var color = auraColor ?? Windows.UI.Color.FromArgb(90, 16, 185, 129); // Soft Emerald
            var transparent = Windows.UI.Color.FromArgb(0, color.R, color.G, color.B);

            var gradientBrush = compositor.CreateLinearGradientBrush();
            gradientBrush.StartPoint = new Vector2(0.0f, 0.0f);
            gradientBrush.EndPoint = new Vector2(1.0f, 1.0f);

            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, transparent));
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0.5f, color));
            gradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, transparent));

            shockwave.Brush = gradientBrush;

            var waveScaleAnim = compositor.CreateVector3KeyFrameAnimation();
            waveScaleAnim.Duration = TimeSpan.FromMilliseconds(500);
            waveScaleAnim.InsertKeyFrame(0.0f, new Vector3(0.9f, 0.9f, 1.0f));
            waveScaleAnim.InsertKeyFrame(1.0f, new Vector3(1.15f, 1.15f, 1.0f), smoothEasing);
            shockwave.StartAnimation("Scale", waveScaleAnim);

            var waveOpacityAnim = compositor.CreateScalarKeyFrameAnimation();
            waveOpacityAnim.Duration = TimeSpan.FromMilliseconds(500);
            waveOpacityAnim.InsertKeyFrame(0.0f, 0.45f);
            waveOpacityAnim.InsertKeyFrame(1.0f, 0.0f, smoothEasing);
            shockwave.StartAnimation("Opacity", waveOpacityAnim);

            ElementCompositionPreview.SetElementChildVisual(element, shockwave);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(520) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                try
                {
                    ElementCompositionPreview.SetElementChildVisual(element, null);
                    shockwave.Dispose();
                }
                catch { }
            };
            timer.Start();
        }
        catch { }
    }

    /// <summary>
    /// Cascades a gentle sequential pulse across multiple items without jarring jumps.
    /// </summary>
    public static void Trigger3DCascadeWave(System.Collections.Generic.IEnumerable<FrameworkElement> elements, double delayStepMs = 45)
    {
        double currentDelay = 0;
        foreach (var el in elements)
        {
            if (el != null)
            {
                var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(currentDelay, 1)) };
                var target = el;
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    Trigger3DOptimizeBurst(target);
                };
                delayTimer.Start();
                currentDelay += delayStepMs;
            }
        }
    }
}

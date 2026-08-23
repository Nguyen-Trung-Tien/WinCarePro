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

    public static bool GetIs3DTiltEnabled(DependencyObject obj) => (bool)obj.GetValue(Is3DTiltEnabledProperty);
    public static void SetIs3DTiltEnabled(DependencyObject obj, bool value) => obj.SetValue(Is3DTiltEnabledProperty, value);

    // =========================================================================
    // 2. Attached Property: MaxTiltAngle (Degrees)
    // =========================================================================
    public static readonly DependencyProperty MaxTiltAngleProperty =
        DependencyProperty.RegisterAttached(
            "MaxTiltAngle",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(7.0));

    public static double GetMaxTiltAngle(DependencyObject obj) => (double)obj.GetValue(MaxTiltAngleProperty);
    public static void SetMaxTiltAngle(DependencyObject obj, double value) => obj.SetValue(MaxTiltAngleProperty, value);

    // =========================================================================
    // 3. Attached Property: DepthZ (Spatial elevation on hover)
    // =========================================================================
    public static readonly DependencyProperty DepthZProperty =
        DependencyProperty.RegisterAttached(
            "DepthZ",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(16.0));

    public static double GetDepthZ(DependencyObject obj) => (double)obj.GetValue(DepthZProperty);
    public static void SetDepthZ(DependencyObject obj, double value) => obj.SetValue(DepthZProperty, value);

    // =========================================================================
    // 4. Attached Property: HoverScale
    // =========================================================================
    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached(
            "HoverScale",
            typeof(double),
            typeof(Animation3DHelper),
            new PropertyMetadata(1.025));

    public static double GetHoverScale(DependencyObject obj) => (double)obj.GetValue(HoverScaleProperty);
    public static void SetHoverScale(DependencyObject obj, double value) => obj.SetValue(HoverScaleProperty, value);

    // =========================================================================
    // 5. Attached Property: IsMagneticEnabled (Pulls element slightly toward cursor)
    // =========================================================================
    public static readonly DependencyProperty IsMagneticEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsMagneticEnabled",
            typeof(bool),
            typeof(Animation3DHelper),
            new PropertyMetadata(false));

    public static bool GetIsMagneticEnabled(DependencyObject obj) => (bool)obj.GetValue(IsMagneticEnabledProperty);
    public static void SetIsMagneticEnabled(DependencyObject obj, bool value) => obj.SetValue(IsMagneticEnabledProperty, value);

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
                Reset3DTransform(element, 200);
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
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPointerOver = true;
            Canvas.SetZIndex(element, 20);
            UpdateCenterPoint(element);

            var pt = e.GetCurrentPoint(element).Position;
            Apply3DTilt(element, (float)pt.X, (float)pt.Y, 200);
        }
    }

    private static void Element_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            if (!state.IsPointerOver) return;

            // Throttle to 60/120Hz smooth movement
            var now = DateTime.UtcNow;
            if ((now - state.LastMoveTime).TotalMilliseconds < 12) return;
            state.LastMoveTime = now;

            var pt = e.GetCurrentPoint(element).Position;
            Apply3DTilt(element, (float)pt.X, (float)pt.Y, 120);
        }
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPointerOver = false;
            state.IsPressed = false;

            Canvas.SetZIndex(element, 0);
            Reset3DTransform(element, 350);
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

            if (state.IsPointerOver)
            {
                var pt = e.GetCurrentPoint(element).Position;
                Apply3DTilt(element, (float)pt.X, (float)pt.Y, 220);
            }
            else
            {
                Canvas.SetZIndex(element, 0);
                Reset3DTransform(element, 300);
            }
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
        try
        {
            var w = (float)element.ActualWidth;
            var h = (float)element.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;
            var compositor = visual.Compositor;

            UpdateCenterPoint(element);

            // Normalized coordinates [-1.0 .. +1.0]
            float nx = Math.Clamp(((cursorX - (w / 2f)) / (w / 2f)), -1.0f, 1.0f);
            float ny = Math.Clamp(((cursorY - (h / 2f)) / (h / 2f)), -1.0f, 1.0f);

            double maxTiltDeg = GetMaxTiltAngle(element);
            double depthZ = GetDepthZ(element);
            double scaleVal = GetHoverScale(element);
            bool isMagnetic = GetIsMagneticEnabled(element);

            // Calculate rotation axis perpendicular to tilt vector
            // ny tilts around X-axis (tilt vector is (1, 0, 0)), nx tilts around Y-axis (tilt vector is (0, 1, 0))
            float rotX = -ny * (float)maxTiltDeg;
            float rotY = nx * (float)maxTiltDeg;

            float totalAngle = MathF.Sqrt((rotX * rotX) + (rotY * rotY));
            if (totalAngle > 0.001f)
            {
                visual.RotationAxis = new Vector3(rotX / totalAngle, rotY / totalAngle, 0.0f);
            }

            var rotAnim = compositor.CreateScalarKeyFrameAnimation();
            rotAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.12f, 0.88f), new Vector2(0.24f, 1.0f));
            rotAnim.InsertKeyFrame(1.0f, totalAngle, easing);
            visual.StartAnimation("RotationAngleInDegrees", rotAnim);

            // Scale & Translation
            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3((float)scaleVal, (float)scaleVal, 1.0f), easing);
            visual.StartAnimation("Scale", scaleAnim);

            float magX = isMagnetic ? (nx * 5.0f) : 0.0f;
            float magY = isMagnetic ? (ny * 5.0f) : 0.0f;
            var transAnim = compositor.CreateVector3KeyFrameAnimation();
            transAnim.Duration = TimeSpan.FromMilliseconds(durationMs);
            transAnim.InsertKeyFrame(1.0f, new Vector3(magX, magY, (float)depthZ), easing);
            visual.StartAnimation("Translation", transAnim);
        }
        catch { }
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
}

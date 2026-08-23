using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;

namespace WinCarePro.Core.Helpers;

public static class AnimationHelper
{
    private static readonly ConditionalWeakTable<FrameworkElement, PointerState> _elementStates = new();

    private class PointerState
    {
        public bool IsPointerOver { get; set; }
        public bool IsPressed { get; set; }
    }

    private static PointerState GetOrCreateState(FrameworkElement element)
    {
        return _elementStates.GetValue(element, _ => new PointerState());
    }
    // ==========================================
    // 1. Attached Property: HoverScale
    // ==========================================
    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached(
            "HoverScale",
            typeof(double),
            typeof(AnimationHelper),
            new PropertyMetadata(1.0, OnHoverScaleChanged));

    public static double GetHoverScale(DependencyObject obj) => (double)obj.GetValue(HoverScaleProperty);
    public static void SetHoverScale(DependencyObject obj, double value) => obj.SetValue(HoverScaleProperty, value);

    private static void OnHoverScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.PointerEntered -= Element_PointerEntered;
            element.PointerExited -= Element_PointerExited;
            element.SizeChanged -= Element_SizeChanged;

            if (e.NewValue is double scale && scale != 1.0)
            {
                element.PointerEntered += Element_PointerEntered;
                element.PointerExited += Element_PointerExited;
                element.SizeChanged += Element_SizeChanged;
            }
        }
    }

    // ==========================================
    // 2. Attached Property: PressedScale
    // ==========================================
    public static readonly DependencyProperty PressedScaleProperty =
        DependencyProperty.RegisterAttached(
            "PressedScale",
            typeof(double),
            typeof(AnimationHelper),
            new PropertyMetadata(1.0, OnPressedScaleChanged));

    public static double GetPressedScale(DependencyObject obj) => (double)obj.GetValue(PressedScaleProperty);
    public static void SetPressedScale(DependencyObject obj, double value) => obj.SetValue(PressedScaleProperty, value);

    private static void OnPressedScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.PointerPressed -= Element_PointerPressed;
            element.PointerReleased -= Element_PointerReleased;
            element.PointerCanceled -= Element_PointerReleased;

            if (e.NewValue is double scale && scale != 1.0)
            {
                element.PointerPressed += Element_PointerPressed;
                element.PointerReleased += Element_PointerReleased;
                element.PointerCanceled += Element_PointerReleased;
            }
        }
    }

    // ==========================================
    // 3. Attached Property: EntranceDelay (NEW v4.0.0)
    //    Auto-triggers spring entrance animation with specified delay when element loads.
    // ==========================================
    public static readonly DependencyProperty EntranceDelayProperty =
        DependencyProperty.RegisterAttached(
            "EntranceDelay",
            typeof(double),
            typeof(AnimationHelper),
            new PropertyMetadata(-1.0, OnEntranceDelayChanged));

    public static double GetEntranceDelay(DependencyObject obj) => (double)obj.GetValue(EntranceDelayProperty);
    public static void SetEntranceDelay(DependencyObject obj, double value) => obj.SetValue(EntranceDelayProperty, value);

    // ==========================================
    // 3b. Attached Property: StaggerIndex
    //    Convenience index multiplier for staggered entrance (e.g. index 0 -> 0ms, 1 -> 45ms, 2 -> 90ms)
    // ==========================================
    public static readonly DependencyProperty StaggerIndexProperty =
        DependencyProperty.RegisterAttached(
            "StaggerIndex",
            typeof(int),
            typeof(AnimationHelper),
            new PropertyMetadata(-1, OnStaggerIndexChanged));

    public static int GetStaggerIndex(DependencyObject obj) => (int)obj.GetValue(StaggerIndexProperty);
    public static void SetStaggerIndex(DependencyObject obj, int value) => obj.SetValue(StaggerIndexProperty, value);

    private static void OnStaggerIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && e.NewValue is int index && index >= 0)
        {
            SetEntranceDelay(element, index * 45.0);
        }
    }

    private static void OnEntranceDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && e.NewValue is double delayMs && delayMs >= 0)
        {
            element.Loaded -= Element_EntranceLoaded;
            element.Loaded += Element_EntranceLoaded;
        }
    }

    private static void Element_EntranceLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            double delayMs = GetEntranceDelay(element);
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            // Set initial state via Translation (does not corrupt XAML layout Offset)
            visual.Properties.InsertVector3("Translation", new Vector3(0, 20, 0));
            visual.Opacity = 0.0f;

            // Spring translation animation
            var springTranslation = compositor.CreateSpringVector3Animation();
            springTranslation.Target = "Translation";
            springTranslation.FinalValue = new Vector3(0, 0, 0);
            springTranslation.DampingRatio = 0.78f;
            springTranslation.Period = TimeSpan.FromMilliseconds(320);

            // Opacity fade-in
            var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(350);

            if (delayMs > 0)
            {
                springTranslation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                springTranslation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                opacityAnim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                opacityAnim.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            }

            visual.StartAnimation("Translation", springTranslation);
            visual.StartAnimation("Opacity", opacityAnim);

            // Unsubscribe after first load to prevent re-animation
            element.Loaded -= Element_EntranceLoaded;
        }
    }

    // ==========================================
    // 4. Attached Property: GlowOnFocus (NEW v4.0.0)
    //    Adds a pulsing opacity glow when element receives keyboard focus.
    // ==========================================
    public static readonly DependencyProperty GlowOnFocusProperty =
        DependencyProperty.RegisterAttached(
            "GlowOnFocus",
            typeof(bool),
            typeof(AnimationHelper),
            new PropertyMetadata(false, OnGlowOnFocusChanged));

    public static bool GetGlowOnFocus(DependencyObject obj) => (bool)obj.GetValue(GlowOnFocusProperty);
    public static void SetGlowOnFocus(DependencyObject obj, bool value) => obj.SetValue(GlowOnFocusProperty, value);

    private static void OnGlowOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.GotFocus -= Element_GotFocus_Glow;
            element.LostFocus -= Element_LostFocus_Glow;

            if (e.NewValue is bool enabled && enabled)
            {
                element.GotFocus += Element_GotFocus_Glow;
                element.LostFocus += Element_LostFocus_Glow;
            }
        }
    }

    private static void Element_GotFocus_Glow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            // Subtle scale-up on focus
            UpdateCenterPoint(element);
            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(200);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.02f, 1.02f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            visual.StartAnimation("Scale", scaleAnim);
        }
    }

    private static void Element_LostFocus_Glow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(200);
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            visual.StartAnimation("Scale", scaleAnim);
        }
    }

    // ==========================================
    // 5. Attached Property: ShakeOnError (NEW v4.0.0)
    //    Set to true to trigger a horizontal shake animation (for validation errors).
    //    Reset to false then true again to re-trigger.
    // ==========================================
    public static readonly DependencyProperty ShakeOnErrorProperty =
        DependencyProperty.RegisterAttached(
            "ShakeOnError",
            typeof(bool),
            typeof(AnimationHelper),
            new PropertyMetadata(false, OnShakeOnErrorChanged));

    public static bool GetShakeOnError(UIElement element) => (bool)element.GetValue(ShakeOnErrorProperty);
    public static void SetShakeOnError(UIElement element, bool value) => element.SetValue(ShakeOnErrorProperty, value);

    private static void OnShakeOnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && e.NewValue is bool shake && shake)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            // Horizontal shake keyframes: 0 → -8 → 8 → -5 → 5 → -2 → 0
            var shakeAnim = compositor.CreateVector3KeyFrameAnimation();
            shakeAnim.Duration = TimeSpan.FromMilliseconds(400);
            var linear = compositor.CreateLinearEasingFunction();

            shakeAnim.InsertKeyFrame(0.0f, new Vector3(0, 0, 0), linear);
            shakeAnim.InsertKeyFrame(0.15f, new Vector3(-8, 0, 0), linear);
            shakeAnim.InsertKeyFrame(0.30f, new Vector3(8, 0, 0), linear);
            shakeAnim.InsertKeyFrame(0.45f, new Vector3(-5, 0, 0), linear);
            shakeAnim.InsertKeyFrame(0.60f, new Vector3(5, 0, 0), linear);
            shakeAnim.InsertKeyFrame(0.80f, new Vector3(-2, 0, 0), linear);
            shakeAnim.InsertKeyFrame(1.0f, new Vector3(0, 0, 0), linear);

            visual.StartAnimation("Offset", shakeAnim);
        }
    }

    // ==========================================
    // 6. Attached Property: IsShimmering
    //    Enables a continuous smooth opacity pulse shimmer loading animation.
    // ==========================================
    public static readonly DependencyProperty IsShimmeringProperty =
        DependencyProperty.RegisterAttached(
            "IsShimmering",
            typeof(bool),
            typeof(AnimationHelper),
            new PropertyMetadata(false, OnIsShimmeringChanged));

    public static bool GetIsShimmering(UIElement element) => (bool)element.GetValue(IsShimmeringProperty);
    public static void SetIsShimmering(UIElement element, bool value) => element.SetValue(IsShimmeringProperty, value);

    private static void OnIsShimmeringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && e.NewValue is bool isShimmering)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            if (isShimmering)
            {
                var shimmerAnim = compositor.CreateScalarKeyFrameAnimation();
                shimmerAnim.Duration = TimeSpan.FromMilliseconds(1000);
                shimmerAnim.IterationBehavior = AnimationIterationBehavior.Forever;
                shimmerAnim.Direction = AnimationDirection.Alternate;
                shimmerAnim.InsertKeyFrame(0.0f, 0.4f);
                shimmerAnim.InsertKeyFrame(1.0f, 0.95f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
                visual.StartAnimation("Opacity", shimmerAnim);
            }
            else
            {
                visual.StopAnimation("Opacity");
                var resetAnim = compositor.CreateScalarKeyFrameAnimation();
                resetAnim.Duration = TimeSpan.FromMilliseconds(200);
                resetAnim.InsertKeyFrame(1.0f, 1.0f);
                visual.StartAnimation("Opacity", resetAnim);
            }
        }
    }

    // ==========================================
    // Event Handlers
    // ==========================================
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
        }
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPointerOver = false;

            if (!state.IsPressed)
            {
                AnimateScale(element, 1.0f, 150);
            }
        }
    }

    private static void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var state = GetOrCreateState(element);
            state.IsPressed = true;

            double scale = GetPressedScale(element);
            AnimateScale(element, (float)scale, 100);
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
                double scale = GetHoverScale(element);
                AnimateScale(element, (float)scale, 200);
            }
            else
            {
                Canvas.SetZIndex(element, 0);
                AnimateScale(element, 1.0f, 200);
            }
        }
    }

    // ==========================================
    // Animation Logic using Composition APIs
    // ==========================================
    private static void UpdateCenterPoint(FrameworkElement element)
    {
        try
        {
            var width = (float)element.ActualWidth;
            var height = (float)element.ActualHeight;

            if (width > 0 && height > 0 && !float.IsNaN(width) && !float.IsNaN(height) && !float.IsInfinity(width) && !float.IsInfinity(height))
            {
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual != null)
                {
                    visual.Size = new Vector2(width, height);
                    visual.CenterPoint = new Vector3(width / 2.0f, height / 2.0f, 0.0f);
                }
            }
        }
        catch { }
    }

    private static void AnimateScale(FrameworkElement element, float scaleTo, double durationMs)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;
            var compositor = visual.Compositor;

            UpdateCenterPoint(element);

            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);

            var easing = compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f),
                new Vector2(0.2f, 1.0f)
            );

            scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scaleTo, scaleTo, 1.0f), easing);

            visual.StartAnimation("Scale", scaleAnimation);
        }
        catch { }
    }
}

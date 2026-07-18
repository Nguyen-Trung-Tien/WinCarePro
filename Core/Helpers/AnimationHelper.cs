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

                UpdateCenterPoint(element);
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

            // Lift card ZIndex on hover to prevent layout lines or neighbor overlays from clipping it
            Canvas.SetZIndex(element, 10);

            double scale = GetHoverScale(element);
            AnimateScale(element, (float)scale, 250);
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
                Canvas.SetZIndex(element, 0);
                AnimateScale(element, 1.0f, 250);
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
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var width = (float)element.ActualWidth;
        var height = (float)element.ActualHeight;
        visual.Size = new Vector2(width, height);
        visual.CenterPoint = new Vector3(width / 2.0f, height / 2.0f, 0.0f);
    }

    private static void AnimateScale(FrameworkElement element, float scaleTo, double durationMs)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        UpdateCenterPoint(element);

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);

        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.1f, 0.9f),
            new Vector2(0.2f, 1.0f)
        );

        // Pass composition easing function directly into InsertKeyFrame
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scaleTo, scaleTo, 1.0f), easing);

        visual.StartAnimation("Scale", scaleAnimation);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Core.Helpers;

/// <summary>
/// Zero-Bloat flat stub: Eliminates all pointer hover/press scale hooks, composition timers, and shimmer animations.
/// </summary>
public static class AnimationHelper
{
    public static bool AreAnimationsEnabled() => false;

    public static readonly DependencyProperty HoverScaleProperty =
        DependencyProperty.RegisterAttached("HoverScale", typeof(double), typeof(AnimationHelper), new PropertyMetadata(1.0));
    public static double GetHoverScale(FrameworkElement element) => 1.0;
    public static void SetHoverScale(FrameworkElement element, double value) { }

    public static readonly DependencyProperty PressedScaleProperty =
        DependencyProperty.RegisterAttached("PressedScale", typeof(double), typeof(AnimationHelper), new PropertyMetadata(1.0));
    public static double GetPressedScale(FrameworkElement element) => 1.0;
    public static void SetPressedScale(FrameworkElement element, double value) { }

    public static readonly DependencyProperty EntranceDelayProperty =
        DependencyProperty.RegisterAttached("EntranceDelay", typeof(double), typeof(AnimationHelper), new PropertyMetadata(-1.0));
    public static double GetEntranceDelay(FrameworkElement element) => -1.0;
    public static void SetEntranceDelay(FrameworkElement element, double value) { }

    public static readonly DependencyProperty StaggerIndexProperty =
        DependencyProperty.RegisterAttached("StaggerIndex", typeof(int), typeof(AnimationHelper), new PropertyMetadata(-1));
    public static int GetStaggerIndex(FrameworkElement element) => -1;
    public static void SetStaggerIndex(FrameworkElement element, int value) { }

    public static readonly DependencyProperty GlowOnFocusProperty =
        DependencyProperty.RegisterAttached("GlowOnFocus", typeof(bool), typeof(AnimationHelper), new PropertyMetadata(false));
    public static bool GetGlowOnFocus(FrameworkElement element) => false;
    public static void SetGlowOnFocus(FrameworkElement element, bool value) { }

    public static readonly DependencyProperty ShakeOnErrorProperty =
        DependencyProperty.RegisterAttached("ShakeOnError", typeof(bool), typeof(AnimationHelper), new PropertyMetadata(false));
    public static bool GetShakeOnError(UIElement element) => false;
    public static void SetShakeOnError(UIElement element, bool value) { }

    public static readonly DependencyProperty IsShimmeringProperty =
        DependencyProperty.RegisterAttached("IsShimmering", typeof(bool), typeof(AnimationHelper), new PropertyMetadata(false));
    public static bool GetIsShimmering(UIElement element) => false;
    public static void SetIsShimmering(UIElement element, bool value) { }

    public static void StartPulseAnimation(FrameworkElement element, float minScale = 1.0f, float maxScale = 1.0f, double durationMs = 0) { }
    public static void StopPulseAnimation(FrameworkElement element, double durationMs = 0) { }
    public static void StartSuccessPopAnimation(FrameworkElement element, float peakScale = 1.0f, double durationMs = 0) { }

    public static Task AnimateCounterAsync(
        TextBlock textBlock,
        double fromValue,
        double toValue,
        string format = "{0:0}",
        int durationMs = 0,
        CancellationToken ct = default)
    {
        if (textBlock != null)
        {
            textBlock.Text = string.Format(format, toValue);
        }
        return Task.CompletedTask;
    }
}


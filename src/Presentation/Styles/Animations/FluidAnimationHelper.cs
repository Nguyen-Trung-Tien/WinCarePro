using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// Zero-Bloat flat stub: All spring physics, shimmer sweeps, parallax scrolling, and composition timers are disabled.
    /// </summary>
    public static class FluidAnimationHelper
    {
        public static void SafeSetCenterPoint(UIElement element, Visual visual) { }
        public static void ApplySpringEntranceAnimation(UIElement element, float delayMs = 0) { }
        public static void EnableHoverDepthEffect(FrameworkElement element, float maxScale = 1.0f) { }
        public static void PrepareConnectedAnimation(string key, UIElement sourceElement) { }
        public static bool TryStartConnectedAnimation(string key, UIElement targetElement) => false;
        public static void ApplyStaggeredEntrance(IList<UIElement> elements, float baseDelayMs = 0, float slideDistanceY = 0) { }
        public static void ApplyShimmerEffect(UIElement element) { }
        public static void StopShimmerEffect(UIElement element) { }
        public static void ApplyPulseGlow(UIElement element, float minOpacity = 1.0f, float maxOpacity = 1.0f, int cycleDurationMs = 0) { }
        public static void StopPulseGlow(UIElement element, float restoreOpacity = 1.0f) { }
        public static void ApplyParallaxOnScroll(ScrollViewer scrollViewer, UIElement backgroundElement, float ratio = 0f) { }
        public static void AnimateMorph(UIElement element, Vector3 fromScale, Vector3 toScale, int durationMs = 0) { }

        public static void AnimateCountUp(TextBlock textBlock, int from, int to, int durationMs = 0, string suffix = "")
        {
            if (textBlock != null) textBlock.Text = $"{to}{suffix}";
        }

        public static void ApplySlideAndFadeOut(UIElement element, float slideX = 0, int durationMs = 0, Action? onCompleted = null)
        {
            onCompleted?.Invoke();
        }

        public static void AnimateSmoothScaleTransition(UIElement element, float targetScale, int durationMs = 0) { }
        public static void ApplyGlowSparkBurst(UIElement element, float peakScale = 1.0f, int durationMs = 0) { }

        public static void AnimateFloatingDeck(UIElement deckElement, bool isVisible, float slideDistanceY = 0, int durationMs = 0)
        {
            if (deckElement != null)
            {
                deckElement.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static void AnimateNumberFloat(TextBlock textBlock, double from, double to, string format = "0.00", string suffix = "", int durationMs = 0)
        {
            if (textBlock != null) textBlock.Text = $"{to.ToString(format)}{suffix}";
        }

        public static void ApplyRipplePress(UIElement element) { }

        public static void AnimateNumberInt(TextBlock textBlock, int from, int to, string suffix = "", int durationMs = 0)
        {
            if (textBlock != null) textBlock.Text = $"{to}{suffix}";
        }

        public static void ApplyImplicitOffsetAndScale(UIElement element) { }

        public static void ApplyCardHoverElevation(FrameworkElement element, float elevationOffset = -3f)
        {
            if (element == null) return;
            try
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
                if (visual == null) return;
                var compositor = visual.Compositor;

                element.PointerEntered += (s, e) =>
                {
                    try
                    {
                        var anim = compositor.CreateScalarKeyFrameAnimation();
                        anim.InsertKeyFrame(1.0f, elevationOffset, compositor.CreateCubicBezierEasingFunction(
                            new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                        anim.Duration = TimeSpan.FromMilliseconds(150);
                        visual.StartAnimation("Offset.Y", anim);

                        if (element is Border border && Application.Current.Resources.TryGetValue("SpecularGlassBorderHoverBrush", out var hoverBrush) && hoverBrush is Microsoft.UI.Xaml.Media.Brush b)
                        {
                            border.BorderBrush = b;
                        }
                    }
                    catch { }
                };

                element.PointerExited += (s, e) =>
                {
                    try
                    {
                        var anim = compositor.CreateScalarKeyFrameAnimation();
                        anim.InsertKeyFrame(1.0f, 0f, compositor.CreateCubicBezierEasingFunction(
                            new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                        anim.Duration = TimeSpan.FromMilliseconds(150);
                        visual.StartAnimation("Offset.Y", anim);

                        if (element is Border border && Application.Current.Resources.TryGetValue("SpecularGlassBorderBrush", out var normBrush) && normBrush is Microsoft.UI.Xaml.Media.Brush b)
                        {
                            border.BorderBrush = b;
                        }
                    }
                    catch { }
                };
            }
            catch { }
        }

        public static void ApplyButtonPressPhysics(FrameworkElement element)
        {
            if (element == null) return;
            try
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
                if (visual == null) return;
                var compositor = visual.Compositor;

                void UpdateCenter()
                {
                    visual.CenterPoint = new Vector3((float)element.ActualWidth / 2f, (float)element.ActualHeight / 2f, 0f);
                }

                element.Loaded += (s, e) => UpdateCenter();
                element.SizeChanged += (s, e) => UpdateCenter();

                element.PointerPressed += (s, e) =>
                {
                    try
                    {
                        UpdateCenter();
                        var pressAnim = compositor.CreateVector3KeyFrameAnimation();
                        pressAnim.InsertKeyFrame(1.0f, new Vector3(0.96f, 0.96f, 1.0f));
                        pressAnim.Duration = TimeSpan.FromMilliseconds(70);
                        visual.StartAnimation("Scale", pressAnim);
                    }
                    catch { }
                };

                element.PointerReleased += (s, e) =>
                {
                    try
                    {
                        UpdateCenter();
                        var springAnim = compositor.CreateSpringVector3Animation();
                        springAnim.DampingRatio = 0.6f;
                        springAnim.Period = TimeSpan.FromMilliseconds(100);
                        springAnim.FinalValue = new Vector3(1.0f, 1.0f, 1.0f);
                        visual.StartAnimation("Scale", springAnim);
                    }
                    catch { }
                };

                element.PointerCaptureLost += (s, e) =>
                {
                    try
                    {
                        var anim = compositor.CreateVector3KeyFrameAnimation();
                        anim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f));
                        anim.Duration = TimeSpan.FromMilliseconds(100);
                        visual.StartAnimation("Scale", anim);
                    }
                    catch { }
                };
            }
            catch { }
        }
    }
}


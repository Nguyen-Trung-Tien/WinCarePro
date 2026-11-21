using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// Unified GPU-composited animation helper for WinCare Pro Aura Glass UI.
    /// Automatically respects <see cref="ReducedMotionHelper.AreAnimationsEnabled"/>.
    /// </summary>
    public static class FluidAnimationHelper
    {
        public static void SafeSetCenterPoint(UIElement element, Visual visual)
        {
            if (element == null || visual == null) return;
            if (element is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
            {
                visual.CenterPoint = new Vector3((float)(fe.ActualWidth / 2.0), (float)(fe.ActualHeight / 2.0), 0f);
            }
        }

        public static void ApplySpringEntranceAnimation(UIElement element, float delayMs = 0)
        {
            if (element == null || !ReducedMotionHelper.AreAnimationsEnabled) return;
            ApplyStaggeredEntrance(new[] { element }, delayMs, 14f);
        }

        public static void EnableHoverDepthEffect(FrameworkElement element, float maxScale = 1.02f)
        {
            ApplyCardHoverElevation(element, -3f);
        }

        public static void PrepareConnectedAnimation(string key, UIElement sourceElement) { }
        public static bool TryStartConnectedAnimation(string key, UIElement targetElement) => false;

        public static void ApplyStaggeredEntrance(IList<UIElement> elements, float baseDelayMs = 35, float slideDistanceY = 14f)
        {
            if (elements == null || elements.Count == 0 || !ReducedMotionHelper.AreAnimationsEnabled) return;
            float effectiveSlide = slideDistanceY > 0 ? slideDistanceY : 14f;
            float stepDelay = baseDelayMs > 0 ? baseDelayMs : 30f;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                try
                {
                    var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
                    var compositor = visual.Compositor;
                    Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetIsTranslationEnabled(element, true);

                    visual.Opacity = 0f;
                    var offsetAnim = compositor.CreateScalarKeyFrameAnimation();
                    offsetAnim.InsertKeyFrame(0f, effectiveSlide);
                    offsetAnim.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(
                        new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                    offsetAnim.Duration = TimeSpan.FromMilliseconds(220);
                    offsetAnim.DelayTime = TimeSpan.FromMilliseconds(Math.Min(i * stepDelay, 350f));

                    var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnim.InsertKeyFrame(0f, 0f);
                    opacityAnim.InsertKeyFrame(1f, 1f);
                    opacityAnim.Duration = TimeSpan.FromMilliseconds(200);
                    opacityAnim.DelayTime = offsetAnim.DelayTime;

                    visual.StartAnimation("Translation.Y", offsetAnim);
                    visual.StartAnimation("Opacity", opacityAnim);
                }
                catch (Exception ex)
                {
                    WinCarePro.Infrastructure.Logging.CrashLogger.LogException("FluidAnimationHelper.ApplyStaggeredEntrance", ex);
                }
            }
        }

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

        public static void ApplyGlowSparkBurst(UIElement element, float peakScale = 1.05f, int durationMs = 260)
        {
            if (element == null || !ReducedMotionHelper.AreAnimationsEnabled) return;
            try
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
                var compositor = visual.Compositor;
                SafeSetCenterPoint(element, visual);

                float scale = peakScale > 1.0f ? peakScale : 1.05f;
                int dur = durationMs > 0 ? durationMs : 260;

                var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                scaleAnim.InsertKeyFrame(0.0f, Vector3.One);
                scaleAnim.InsertKeyFrame(0.4f, new Vector3(scale, scale, 1f), compositor.CreateCubicBezierEasingFunction(
                    new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                scaleAnim.InsertKeyFrame(1.0f, Vector3.One);
                scaleAnim.Duration = TimeSpan.FromMilliseconds(dur);

                visual.StartAnimation("Scale", scaleAnim);
            }
            catch (Exception ex)
            {
                WinCarePro.Infrastructure.Logging.CrashLogger.LogException("FluidAnimationHelper.ApplyGlowSparkBurst", ex);
            }
        }

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

        private static readonly ConditionalWeakTable<FrameworkElement, object> _hoverAttached = new();

        public static void ApplyCardHoverElevation(FrameworkElement element, float elevationOffset = -3f)
        {
            if (element == null || !ReducedMotionHelper.AreAnimationsEnabled) return;
            if (_hoverAttached.TryGetValue(element, out _)) return;
            _hoverAttached.Add(element, new object());

            try
            {
                Microsoft.UI.Xaml.Media.Brush? originalBorderBrush = element is Border b0 ? b0.BorderBrush : null;

                element.PointerEntered += (s, e) =>
                {
                    try
                    {
                        if (element is Border border && Application.Current.Resources.TryGetValue("SpecularGlassBorderHoverBrush", out var hoverBrush) && hoverBrush is Microsoft.UI.Xaml.Media.Brush b)
                        {
                            originalBorderBrush ??= border.BorderBrush;
                            border.BorderBrush = b;
                        }
                    }
                    catch { }
                };

                element.PointerExited += (s, e) =>
                {
                    try
                    {
                        if (element is Border border && originalBorderBrush != null)
                        {
                            border.BorderBrush = originalBorderBrush;
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


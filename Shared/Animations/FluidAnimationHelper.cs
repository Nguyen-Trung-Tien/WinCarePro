using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// WinCare Pro v4.0.0 — Comprehensive GPU-accelerated Composition Animation Engine.
    /// Provides 60/120 FPS spring animations, staggered entrances, shimmer loading effects,
    /// pulse glow, parallax scroll, morph transitions, count-up animations, and slide-out exits.
    /// </summary>
    public static class FluidAnimationHelper
    {
        // ============================================================
        // 0. UTILITY: SAFE CENTER POINT (Prevents layout race condition)
        // ============================================================

        /// <summary>
        /// Safely sets the CenterPoint of a visual to the center of its element.
        /// Handles the case where ActualWidth/Height may be 0 if element hasn't been laid out yet.
        /// </summary>
        public static void SafeSetCenterPoint(UIElement element, Visual visual)
        {
            if (element is FrameworkElement fe)
            {
                if (fe.ActualWidth > 0 && fe.ActualHeight > 0)
                {
                    visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
                }
                else
                {
                    // Element hasn't been laid out yet — defer CenterPoint until SizeChanged fires
                    void handler(object s, SizeChangedEventArgs args)
                    {
                        visual.CenterPoint = new Vector3((float)args.NewSize.Width / 2f, (float)args.NewSize.Height / 2f, 0);
                        fe.SizeChanged -= handler; // One-shot: detach after first layout
                    }
                    fe.SizeChanged += handler;
                }
            }
        }

        // ============================================================
        // 1. SPRING ENTRANCE ANIMATION (existing, refined)
        // ============================================================

        /// <summary>
        /// Applies a natural spring entrance animation (scale + offset) to a target UI element.
        /// </summary>
        public static void ApplySpringEntranceAnimation(UIElement element, float delayMs = 0)
        {
            if (element == null) return;

            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Define starting state via Translation
            visual.Properties.InsertVector3("Translation", new Vector3(0, 24, 0));
            visual.Opacity = 0.0f;

            // Create Spring Vector3 Animation for smooth entrance
            SpringVector3NaturalMotionAnimation springTranslation = compositor.CreateSpringVector3Animation();
            springTranslation.Target = "Translation";
            springTranslation.FinalValue = new Vector3(0, 0, 0);
            springTranslation.DampingRatio = 0.75f; // Slight spring bounce
            springTranslation.Period = TimeSpan.FromMilliseconds(350);

            // Create Opacity Animation
            ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(400);

            if (delayMs > 0)
            {
                springTranslation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                opacityAnim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            }

            visual.StartAnimation("Translation", springTranslation);
            visual.StartAnimation("Opacity", opacityAnim);
        }

        // ============================================================
        // 2. HOVER DEPTH EFFECT (existing, refined)
        // ============================================================

        /// <summary>
        /// Applies a 3D depth tilt &amp; elevation scale micro-interaction on mouse pointer hover.
        /// </summary>
        public static void EnableHoverDepthEffect(FrameworkElement element, float maxScale = 1.025f)
        {
            if (element == null) return;

            element.PointerEntered += (s, e) =>
            {
                Visual visual = ElementCompositionPreview.GetElementVisual(element);
                Compositor compositor = visual.Compositor;

                // Use safe center point to prevent (0,0) when element not yet laid out
                SafeSetCenterPoint(element, visual);

                SpringVector3NaturalMotionAnimation scaleAnim = compositor.CreateSpringVector3Animation();
                scaleAnim.Target = "Scale";
                scaleAnim.FinalValue = new Vector3(maxScale, maxScale, 1.0f);
                scaleAnim.DampingRatio = 0.8f;
                scaleAnim.Period = TimeSpan.FromMilliseconds(200);

                visual.StartAnimation("Scale", scaleAnim);
            };

            element.PointerExited += (s, e) =>
            {
                Visual visual = ElementCompositionPreview.GetElementVisual(element);
                Compositor compositor = visual.Compositor;

                SpringVector3NaturalMotionAnimation scaleAnim = compositor.CreateSpringVector3Animation();
                scaleAnim.Target = "Scale";
                scaleAnim.FinalValue = new Vector3(1.0f, 1.0f, 1.0f);
                scaleAnim.DampingRatio = 0.85f;
                scaleAnim.Period = TimeSpan.FromMilliseconds(250);

                visual.StartAnimation("Scale", scaleAnim);
            };
        }

        // ============================================================
        // 3. CONNECTED ANIMATION HELPERS (existing)
        // ============================================================

        /// <summary>
        /// Prepares a ConnectedAnimation service transition key.
        /// </summary>
        public static void PrepareConnectedAnimation(string key, UIElement sourceElement)
        {
            if (string.IsNullOrEmpty(key) || sourceElement == null) return;
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(key, sourceElement);
        }

        /// <summary>
        /// Plays a ConnectedAnimation on the target element in destination view.
        /// </summary>
        public static bool TryStartConnectedAnimation(string key, UIElement targetElement)
        {
            if (string.IsNullOrEmpty(key) || targetElement == null) return false;

            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(key);
            if (animation != null)
            {
                animation.Configuration = new BasicConnectedAnimationConfiguration();
                return animation.TryStart(targetElement);
            }
            return false;
        }

        // ============================================================
        // 4. STAGGERED ENTRANCE ANIMATION (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Applies cascading staggered entrance animation to a list of UI elements.
        /// Each element appears sequentially with a delay offset, creating a waterfall effect.
        /// </summary>
        /// <param name="elements">Ordered list of elements to animate in sequence.</param>
        /// <param name="baseDelayMs">Delay in ms between each element's entrance. Default 60ms.</param>
        /// <param name="slideDistanceY">Vertical slide distance in pixels. Default 30px.</param>
        public static void ApplyStaggeredEntrance(IList<UIElement> elements, float baseDelayMs = 60, float slideDistanceY = 30)
        {
            if (elements == null || elements.Count == 0) return;

            for (int i = 0; i < elements.Count; i++)
            {
                UIElement element = elements[i];
                if (element == null) continue;

                ElementCompositionPreview.SetIsTranslationEnabled(element, true);
                Visual visual = ElementCompositionPreview.GetElementVisual(element);
                Compositor compositor = visual.Compositor;

                // Set initial state via Translation
                visual.Properties.InsertVector3("Translation", new Vector3(0, slideDistanceY, 0));
                visual.Opacity = 0.0f;

                float delay = i * baseDelayMs;

                // Spring translation animation with cascading delay
                SpringVector3NaturalMotionAnimation springTranslation = compositor.CreateSpringVector3Animation();
                springTranslation.Target = "Translation";
                springTranslation.FinalValue = new Vector3(0, 0, 0);
                springTranslation.DampingRatio = 0.78f;
                springTranslation.Period = TimeSpan.FromMilliseconds(320);
                springTranslation.DelayTime = TimeSpan.FromMilliseconds(delay);
                springTranslation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                // Opacity animation
                ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
                opacityAnim.InsertKeyFrame(0.0f, 0.0f);
                opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                    new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                opacityAnim.Duration = TimeSpan.FromMilliseconds(350);
                opacityAnim.DelayTime = TimeSpan.FromMilliseconds(delay);
                opacityAnim.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                // Scale from 0.95 → 1.0 for subtle pop-in
                Vector3KeyFrameAnimation scaleAnim = compositor.CreateVector3KeyFrameAnimation();
                scaleAnim.InsertKeyFrame(0.0f, new Vector3(0.95f, 0.95f, 1.0f));
                scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f),
                    compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                scaleAnim.Duration = TimeSpan.FromMilliseconds(400);
                scaleAnim.DelayTime = TimeSpan.FromMilliseconds(delay);
                scaleAnim.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                if (element is FrameworkElement fe)
                {
                    visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
                }

                visual.StartAnimation("Translation", springTranslation);
                visual.StartAnimation("Opacity", opacityAnim);
                visual.StartAnimation("Scale", scaleAnim);
            }
        }

        // ============================================================
        // 5. SHIMMER LOADING EFFECT (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Applies a repeating horizontal shimmer highlight effect on a skeleton placeholder element.
        /// Uses Composition offset animation to simulate a loading shimmer sweep.
        /// </summary>
        public static void ApplyShimmerEffect(UIElement element)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Pulsing opacity shimmer as a lightweight GPU-only effect
            ScalarKeyFrameAnimation shimmerAnim = compositor.CreateScalarKeyFrameAnimation();
            shimmerAnim.InsertKeyFrame(0.0f, 0.4f);
            shimmerAnim.InsertKeyFrame(0.5f, 0.8f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
            shimmerAnim.InsertKeyFrame(1.0f, 0.4f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
            shimmerAnim.Duration = TimeSpan.FromMilliseconds(1800);
            shimmerAnim.IterationBehavior = AnimationIterationBehavior.Forever;

            visual.StartAnimation("Opacity", shimmerAnim);
        }

        /// <summary>
        /// Stops shimmer effect and restores full opacity with a smooth fade-in.
        /// </summary>
        public static void StopShimmerEffect(UIElement element)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            visual.StopAnimation("Opacity");

            ScalarKeyFrameAnimation fadeIn = compositor.CreateScalarKeyFrameAnimation();
            fadeIn.InsertKeyFrame(0.0f, visual.Opacity);
            fadeIn.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            fadeIn.Duration = TimeSpan.FromMilliseconds(300);

            visual.StartAnimation("Opacity", fadeIn);
        }

        // ============================================================
        // 6. PULSE GLOW ANIMATION (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Applies a continuous breathing pulse glow animation on an element.
        /// Ideal for health score rings, active status indicators, and accent glows.
        /// </summary>
        /// <param name="element">Target glow element (e.g., an Ellipse or Border).</param>
        /// <param name="minOpacity">Minimum opacity during pulse cycle.</param>
        /// <param name="maxOpacity">Maximum opacity during pulse cycle.</param>
        /// <param name="cycleDurationMs">Full cycle duration in ms. Default 2200ms.</param>
        public static void ApplyPulseGlow(UIElement element, float minOpacity = 0.05f, float maxOpacity = 0.30f, int cycleDurationMs = 2200)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            ScalarKeyFrameAnimation pulseAnim = compositor.CreateScalarKeyFrameAnimation();
            pulseAnim.InsertKeyFrame(0.0f, minOpacity);
            pulseAnim.InsertKeyFrame(0.5f, maxOpacity, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
            pulseAnim.InsertKeyFrame(1.0f, minOpacity, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
            pulseAnim.Duration = TimeSpan.FromMilliseconds(cycleDurationMs);
            pulseAnim.IterationBehavior = AnimationIterationBehavior.Forever;

            visual.StartAnimation("Opacity", pulseAnim);
        }

        /// <summary>
        /// Stops pulse glow animation and restores target opacity.
        /// </summary>
        public static void StopPulseGlow(UIElement element, float restoreOpacity = 0.1f)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            visual.StopAnimation("Opacity");
            visual.Opacity = restoreOpacity;
        }

        // ============================================================
        // 7. PARALLAX SCROLL EFFECT (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Applies a parallax depth scroll effect where a background element scrolls at a fraction
        /// of the scroll speed, creating a sense of depth and immersion.
        /// </summary>
        /// <param name="scrollViewer">The ScrollViewer driving the parallax.</param>
        /// <param name="backgroundElement">The background element to parallax.</param>
        /// <param name="ratio">Parallax ratio (0.0 = static, 1.0 = scrolls with content). Default 0.3.</param>
        public static void ApplyParallaxOnScroll(ScrollViewer scrollViewer, UIElement backgroundElement, float ratio = 0.3f)
        {
            if (scrollViewer == null || backgroundElement == null) return;

            Visual bgVisual = ElementCompositionPreview.GetElementVisual(backgroundElement);
            Compositor compositor = bgVisual.Compositor;

            // Get scroll manipulation property set
            CompositionPropertySet scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);

            // Create expression animation: Offset.Y = -ScrollY * ratio
            ExpressionAnimation parallaxExpression = compositor.CreateExpressionAnimation(
                $"scroll.Translation.Y * {ratio}");
            parallaxExpression.SetReferenceParameter("scroll", scrollProperties);

            bgVisual.StartAnimation("Offset.Y", parallaxExpression);
        }

        // ============================================================
        // 8. MORPH TRANSITION (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Animates a smooth morphing transition for an element changing its visual size/position.
        /// Useful for expand/collapse panels, card detail transitions, etc.
        /// </summary>
        /// <param name="element">Element to animate.</param>
        /// <param name="fromScale">Starting scale vector.</param>
        /// <param name="toScale">Target scale vector.</param>
        /// <param name="durationMs">Animation duration in ms. Default 400ms.</param>
        public static void AnimateMorph(UIElement element, Vector3 fromScale, Vector3 toScale, int durationMs = 400)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Center point for scaling
            if (element is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
            }

            // Spring scale animation for natural morph feel
            SpringVector3NaturalMotionAnimation morphScale = compositor.CreateSpringVector3Animation();
            morphScale.Target = "Scale";
            morphScale.InitialValue = fromScale;
            morphScale.FinalValue = toScale;
            morphScale.DampingRatio = 0.72f;
            morphScale.Period = TimeSpan.FromMilliseconds(durationMs);

            // Opacity transition during morph
            ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.7f);
            opacityAnim.InsertKeyFrame(0.4f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Scale", morphScale);
            visual.StartAnimation("Opacity", opacityAnim);
        }

        // ============================================================
        // 9. COUNT-UP ANIMATION (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Animates a TextBlock's text value counting up from a start number to an end number.
        /// Creates a dynamic numerical reveal effect for scores, statistics, and KPIs.
        /// </summary>
        /// <param name="textBlock">Target TextBlock to animate.</param>
        /// <param name="from">Starting value.</param>
        /// <param name="to">Ending value.</param>
        /// <param name="durationMs">Total animation duration. Default 800ms.</param>
        /// <param name="suffix">Optional suffix appended after the number (e.g., "%", " MB").</param>
        public static void AnimateCountUp(TextBlock textBlock, int from, int to, int durationMs = 800, string suffix = "")
        {
            if (textBlock == null) return;

            DispatcherQueue dispatcherQueue = textBlock.DispatcherQueue;
            if (dispatcherQueue == null) return;

            int totalSteps = Math.Max(1, Math.Abs(to - from));
            int frameCount = Math.Min(totalSteps, 60); // Cap at 60 frames for performance
            double intervalMs = (double)durationMs / frameCount;
            double valueStep = (double)(to - from) / frameCount;

            int currentFrame = 0;
            double currentValue = from;

            DispatcherQueueTimer timer = dispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Math.Max(16, intervalMs)); // Minimum 16ms (~60fps)
            timer.IsRepeating = true;

            timer.Tick += (s, e) =>
            {
                currentFrame++;
                // Apply ease-out curve for natural deceleration feel
                double progress = (double)currentFrame / frameCount;
                double easedProgress = 1.0 - Math.Pow(1.0 - progress, 3); // Cubic ease-out
                int displayValue = from + (int)((to - from) * easedProgress);

                textBlock.Text = $"{displayValue}{suffix}";

                if (currentFrame >= frameCount)
                {
                    textBlock.Text = $"{to}{suffix}";
                    timer.Stop();
                }
            };

            textBlock.Text = $"{from}{suffix}";
            timer.Start();
        }

        // ============================================================
        // 10. SLIDE AND FADE OUT (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Applies an exit animation that slides and fades an element out.
        /// Ideal for removing list items, dismissing cards, or closing panels.
        /// </summary>
        /// <param name="element">Element to animate out.</param>
        /// <param name="slideX">Horizontal slide distance. Positive = slide right. Default 60px.</param>
        /// <param name="durationMs">Animation duration. Default 300ms.</param>
        /// <param name="onCompleted">Optional callback when animation finishes.</param>
        public static void ApplySlideAndFadeOut(UIElement element, float slideX = 60, int durationMs = 300, Action? onCompleted = null)
        {
            if (element == null) return;

            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Slide translation animation
            Vector3KeyFrameAnimation slideAnim = compositor.CreateVector3KeyFrameAnimation();
            slideAnim.InsertKeyFrame(0.0f, new Vector3(0, 0, 0));
            slideAnim.InsertKeyFrame(1.0f, new Vector3(slideX, 0, 0),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            slideAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            // Fade out
            ScalarKeyFrameAnimation fadeAnim = compositor.CreateScalarKeyFrameAnimation();
            fadeAnim.InsertKeyFrame(0.0f, 1.0f);
            fadeAnim.InsertKeyFrame(1.0f, 0.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            fadeAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            // Scale down slightly during exit
            Vector3KeyFrameAnimation scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(0.92f, 0.92f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Translation", slideAnim);
            visual.StartAnimation("Opacity", fadeAnim);
            visual.StartAnimation("Scale", scaleAnim);

            // Fire completion callback after duration
            if (onCompleted != null)
            {
                DispatcherQueue? dq = (element as FrameworkElement)?.DispatcherQueue;
                if (dq != null)
                {
                    DispatcherQueueTimer timer = dq.CreateTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(durationMs + 50);
                    timer.IsRepeating = false;
                    timer.Tick += (s, e) =>
                    {
                        onCompleted.Invoke();
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }

        // ============================================================
        // 11. SMOOTH VALUE TRANSITION (NEW v4.0.0)
        // ============================================================

        /// <summary>
        /// Smoothly animates a ProgressRing or ProgressBar value change using spring animation feel.
        /// Used for Dashboard resource meters transitioning between telemetry samples.
        /// </summary>
        public static void AnimateSmoothScaleTransition(UIElement element, float targetScale, int durationMs = 250)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            if (element is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
            }

            SpringVector3NaturalMotionAnimation scaleAnim = compositor.CreateSpringVector3Animation();
            scaleAnim.Target = "Scale";
            scaleAnim.FinalValue = new Vector3(targetScale, targetScale, 1.0f);
            scaleAnim.DampingRatio = 0.85f;
            scaleAnim.Period = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Scale", scaleAnim);
        }

        // ============================================================
        // 12. GLOW SPARK BURST ANIMATION (Master Micro-Interaction)
        // ============================================================

        /// <summary>
        /// Triggers an exhilarating radial spark pulse & haptic-like scale burst on a button or card
        /// when an action like Boost, Clean Now, or Turbo Mode is activated.
        /// </summary>
        public static void ApplyGlowSparkBurst(UIElement element, float peakScale = 1.06f, int durationMs = 380)
        {
            if (element == null) return;

            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            if (element is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
            }

            // Keyframe Scale animation: 1.0 -> peakScale -> 0.98 -> 1.0
            Vector3KeyFrameAnimation burstScale = compositor.CreateVector3KeyFrameAnimation();
            burstScale.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
            burstScale.InsertKeyFrame(0.35f, new Vector3(peakScale, peakScale, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            burstScale.InsertKeyFrame(0.7f, new Vector3(0.98f, 0.98f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(0.6f, 1.0f)));
            burstScale.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.3f, 1.0f)));
            burstScale.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Scale", burstScale);
        }

        // ============================================================
        // 13. FLOATING ACTION DECK ANIMATION (Sticky Bottom Toolbar)
        // ============================================================

        /// <summary>
        /// Smoothly slides and fades a floating action deck into or out of view.
        /// </summary>
        public static void AnimateFloatingDeck(UIElement deckElement, bool isVisible, float slideDistanceY = 36, int durationMs = 300)
        {
            if (deckElement == null) return;

            ElementCompositionPreview.SetIsTranslationEnabled(deckElement, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(deckElement);
            Compositor compositor = visual.Compositor;

            if (isVisible)
            {
                deckElement.Visibility = Visibility.Visible;
                visual.Properties.InsertVector3("Translation", new Vector3(0, slideDistanceY, 0));
                visual.Opacity = 0.0f;

                SpringVector3NaturalMotionAnimation springSlide = compositor.CreateSpringVector3Animation();
                springSlide.Target = "Translation";
                springSlide.FinalValue = new Vector3(0, 0, 0);
                springSlide.DampingRatio = 0.76f;
                springSlide.Period = TimeSpan.FromMilliseconds(durationMs);

                ScalarKeyFrameAnimation fadeIn = compositor.CreateScalarKeyFrameAnimation();
                fadeIn.InsertKeyFrame(0.0f, 0.0f);
                fadeIn.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                    new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
                fadeIn.Duration = TimeSpan.FromMilliseconds((int)(durationMs * 0.8));

                visual.StartAnimation("Translation", springSlide);
                visual.StartAnimation("Opacity", fadeIn);
            }
            else
            {
                Vector3KeyFrameAnimation slideDown = compositor.CreateVector3KeyFrameAnimation();
                slideDown.InsertKeyFrame(0.0f, new Vector3(0, 0, 0));
                slideDown.InsertKeyFrame(1.0f, new Vector3(0, slideDistanceY, 0),
                    compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
                slideDown.Duration = TimeSpan.FromMilliseconds(durationMs);

                ScalarKeyFrameAnimation fadeOut = compositor.CreateScalarKeyFrameAnimation();
                fadeOut.InsertKeyFrame(0.0f, 1.0f);
                fadeOut.InsertKeyFrame(1.0f, 0.0f, compositor.CreateCubicBezierEasingFunction(
                    new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
                fadeOut.Duration = TimeSpan.FromMilliseconds(durationMs);

                visual.StartAnimation("Translation", slideDown);
                visual.StartAnimation("Opacity", fadeOut);

                DispatcherQueue? dq = (deckElement as FrameworkElement)?.DispatcherQueue;
                if (dq != null)
                {
                    DispatcherQueueTimer timer = dq.CreateTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(durationMs + 20);
                    timer.IsRepeating = false;
                    timer.Tick += (s, e) =>
                    {
                        deckElement.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }

        // ============================================================
        // 14. NUMBER FLOAT COUNT-UP ANIMATION
        // ============================================================

        /// <summary>
        /// Animates a TextBlock showing floating point numerical changes (e.g., "1.25 GB" -> "5.80 GB").
        /// </summary>
        public static void AnimateNumberFloat(TextBlock textBlock, double from, double to, string format = "0.00", string suffix = "", int durationMs = 700)
        {
            if (textBlock == null) return;

            DispatcherQueue dispatcherQueue = textBlock.DispatcherQueue;
            if (dispatcherQueue == null) return;

            int frameCount = 45;
            double intervalMs = (double)durationMs / frameCount;

            int currentFrame = 0;
            DispatcherQueueTimer timer = dispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Math.Max(16, intervalMs));
            timer.IsRepeating = true;

            timer.Tick += (s, e) =>
            {
                currentFrame++;
                double progress = (double)currentFrame / frameCount;
                double easedProgress = 1.0 - Math.Pow(1.0 - progress, 3); // Cubic ease out
                double currentVal = from + ((to - from) * easedProgress);

                textBlock.Text = $"{currentVal.ToString(format)}{suffix}";

                if (currentFrame >= frameCount)
                {
                    textBlock.Text = $"{to.ToString(format)}{suffix}";
                    timer.Stop();
                }
            };

            textBlock.Text = $"{from.ToString(format)}{suffix}";
            timer.Start();
        }

        // ============================================================
        // 16. RIPPLE PRESS ANIMATION (Premium button click feedback)
        // ============================================================

        /// <summary>
        /// Applies a subtle scale-down press feedback followed by springy release.
        /// Attach to PointerPressed/Released for premium button feel.
        /// </summary>
        public static void ApplyRipplePress(UIElement element)
        {
            if (element == null) return;

            if (element is FrameworkElement fe)
            {
                fe.PointerPressed += (s, e) =>
                {
                    Visual visual = ElementCompositionPreview.GetElementVisual(element);
                    Compositor compositor = visual.Compositor;
                    SafeSetCenterPoint(element, visual);

                    // Quick press-down scale
                    Vector3KeyFrameAnimation pressAnim = compositor.CreateVector3KeyFrameAnimation();
                    pressAnim.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
                    pressAnim.InsertKeyFrame(1.0f, new Vector3(0.96f, 0.96f, 1.0f),
                        compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.3f, 1.0f)));
                    pressAnim.Duration = TimeSpan.FromMilliseconds(100);
                    visual.StartAnimation("Scale", pressAnim);
                };

                fe.PointerReleased += (s, e) =>
                {
                    Visual visual = ElementCompositionPreview.GetElementVisual(element);
                    Compositor compositor = visual.Compositor;

                    // Springy release back to normal
                    SpringVector3NaturalMotionAnimation releaseAnim = compositor.CreateSpringVector3Animation();
                    releaseAnim.Target = "Scale";
                    releaseAnim.FinalValue = new Vector3(1.0f, 1.0f, 1.0f);
                    releaseAnim.DampingRatio = 0.55f;
                    releaseAnim.Period = TimeSpan.FromMilliseconds(200);
                    visual.StartAnimation("Scale", releaseAnim);
                };
            }
        }

        /// <summary>
        /// Animates a TextBlock showing integer numerical changes with smooth rolling count-down/up.
        /// </summary>
        public static void AnimateNumberInt(TextBlock textBlock, int from, int to, string suffix = "", int durationMs = 600)
        {
            if (textBlock == null) return;

            DispatcherQueue dispatcherQueue = textBlock.DispatcherQueue;
            if (dispatcherQueue == null) return;

            int frameCount = 30;
            double intervalMs = (double)durationMs / frameCount;

            int currentFrame = 0;
            DispatcherQueueTimer timer = dispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Math.Max(16, intervalMs));
            timer.IsRepeating = true;

            timer.Tick += (s, e) =>
            {
                currentFrame++;
                double progress = (double)currentFrame / frameCount;
                double easedProgress = 1.0 - Math.Pow(1.0 - progress, 3); // Cubic ease out
                int currentVal = (int)Math.Round(from + ((to - from) * easedProgress));

                textBlock.Text = $"{currentVal}{suffix}";

                if (currentFrame >= frameCount)
                {
                    textBlock.Text = $"{to}{suffix}";
                    timer.Stop();
                }
            };

            textBlock.Text = $"{from}{suffix}";
            timer.Start();
        }
    }
}

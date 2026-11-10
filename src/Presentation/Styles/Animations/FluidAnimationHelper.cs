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
        public static void ApplyCardHoverElevation(FrameworkElement element, float elevationOffset = 0f) { }
        public static void ApplyButtonPressPhysics(FrameworkElement element) { }
    }
}


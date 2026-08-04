using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// WinCare Pro v4.0.0 — Advanced page transition system providing premium
    /// DrillIn, SlideUp, CrossFade, and ScaleReveal transitions for NavigationView navigation.
    /// </summary>
    public static class PageTransitionHelper
    {
        /// <summary>
        /// Applies a DrillIn entrance animation on a newly navigated page's root element.
        /// Creates a forward-zooming depth effect similar to Windows 11 Settings app.
        /// </summary>
        /// <param name="pageRoot">The root Grid/StackPanel of the arriving page.</param>
        /// <param name="delayMs">Optional delay before animation starts.</param>
        public static void ApplyDrillInEntrance(UIElement pageRoot, float delayMs = 0)
        {
            if (pageRoot == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(pageRoot);
            Compositor compositor = visual.Compositor;

            // Start scaled down and slightly transparent
            visual.Opacity = 0.0f;
            visual.Scale = new Vector3(0.94f, 0.94f, 1.0f);

            if (pageRoot is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
            }

            // Scale spring animation
            SpringVector3NaturalMotionAnimation scaleAnim = compositor.CreateSpringVector3Animation();
            scaleAnim.Target = "Scale";
            scaleAnim.FinalValue = new Vector3(1.0f, 1.0f, 1.0f);
            scaleAnim.DampingRatio = 0.82f;
            scaleAnim.Period = TimeSpan.FromMilliseconds(350);

            // Opacity fade-in
            ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(300);

            if (delayMs > 0)
            {
                scaleAnim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                opacityAnim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                opacityAnim.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            }

            visual.StartAnimation("Scale", scaleAnim);
            visual.StartAnimation("Opacity", opacityAnim);
        }

        /// <summary>
        /// Applies a SlideUp reveal entrance animation from the bottom of the viewport.
        /// Ideal for Settings pages, detail panels, or modal-like transitions.
        /// </summary>
        /// <param name="pageRoot">The root element of the arriving page.</param>
        /// <param name="slideDistance">Distance in pixels to slide up from. Default 40px.</param>
        public static void ApplySlideUpReveal(UIElement pageRoot, float slideDistance = 40)
        {
            if (pageRoot == null) return;

            ElementCompositionPreview.SetIsTranslationEnabled(pageRoot, true);
            Visual visual = ElementCompositionPreview.GetElementVisual(pageRoot);
            Compositor compositor = visual.Compositor;

            visual.Opacity = 0.0f;
            visual.Properties.InsertVector3("Translation", new Vector3(0, slideDistance, 0));

            // Spring translation for natural deceleration
            SpringVector3NaturalMotionAnimation springTranslation = compositor.CreateSpringVector3Animation();
            springTranslation.Target = "Translation";
            springTranslation.FinalValue = new Vector3(0, 0, 0);
            springTranslation.DampingRatio = 0.80f;
            springTranslation.Period = TimeSpan.FromMilliseconds(380);

            // Opacity
            ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(350);

            visual.StartAnimation("Translation", springTranslation);
            visual.StartAnimation("Opacity", opacityAnim);
        }

        /// <summary>
        /// Applies a smooth CrossFade transition on an element. Useful for swapping content
        /// sections within a page without a full navigation event.
        /// </summary>
        /// <param name="outElement">Element fading out (will be set to Collapsed after animation).</param>
        /// <param name="inElement">Element fading in.</param>
        /// <param name="durationMs">Transition duration. Default 250ms.</param>
        public static void ApplyCrossFade(UIElement outElement, UIElement inElement, int durationMs = 250)
        {
            if (outElement == null || inElement == null) return;

            Compositor compositor = ElementCompositionPreview.GetElementVisual(outElement).Compositor;

            // Fade out
            Visual outVisual = ElementCompositionPreview.GetElementVisual(outElement);
            ScalarKeyFrameAnimation fadeOut = compositor.CreateScalarKeyFrameAnimation();
            fadeOut.InsertKeyFrame(0.0f, 1.0f);
            fadeOut.InsertKeyFrame(1.0f, 0.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            fadeOut.Duration = TimeSpan.FromMilliseconds(durationMs / 2);
            outVisual.StartAnimation("Opacity", fadeOut);

            // Make inElement visible but transparent first
            if (inElement is FrameworkElement inFe)
            {
                inFe.Visibility = Visibility.Visible;
            }

            // Fade in with slight delay
            Visual inVisual = ElementCompositionPreview.GetElementVisual(inElement);
            inVisual.Opacity = 0.0f;

            ScalarKeyFrameAnimation fadeIn = compositor.CreateScalarKeyFrameAnimation();
            fadeIn.InsertKeyFrame(0.0f, 0.0f);
            fadeIn.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            fadeIn.Duration = TimeSpan.FromMilliseconds(durationMs / 2);
            fadeIn.DelayTime = TimeSpan.FromMilliseconds(durationMs / 2);
            fadeIn.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            inVisual.StartAnimation("Opacity", fadeIn);
        }

        /// <summary>
        /// Applies an exit transition for pages being navigated away from.
        /// Subtle scale-down and fade-out to create depth perception.
        /// </summary>
        /// <param name="pageRoot">The root element of the departing page.</param>
        /// <param name="durationMs">Exit animation duration. Default 200ms.</param>
        public static void ApplyPageExitTransition(UIElement pageRoot, int durationMs = 200)
        {
            if (pageRoot == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(pageRoot);
            Compositor compositor = visual.Compositor;

            if (pageRoot is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0);
            }

            // Scale down slightly
            Vector3KeyFrameAnimation scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
            scaleAnim.InsertKeyFrame(1.0f, new Vector3(0.96f, 0.96f, 1.0f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            // Fade out
            ScalarKeyFrameAnimation fadeAnim = compositor.CreateScalarKeyFrameAnimation();
            fadeAnim.InsertKeyFrame(0.0f, 1.0f);
            fadeAnim.InsertKeyFrame(1.0f, 0.0f, compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f), new Vector2(1.0f, 1.0f)));
            fadeAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Scale", scaleAnim);
            visual.StartAnimation("Opacity", fadeAnim);
        }
    }
}

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// Provide 60/120 FPS GPU-accelerated composition spring animations, 
    /// 3D hover depth tilt effects, and smooth view transitions for WinCare Pro v4.0.0.
    /// </summary>
    public static class FluidAnimationHelper
    {
        /// <summary>
        /// Applies a natural spring entrance animation (scale + offset) to a target UI element.
        /// </summary>
        public static void ApplySpringEntranceAnimation(UIElement element, float delayMs = 0)
        {
            if (element == null) return;

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Define starting state
            visual.Opacity = 0.0f;
            visual.Offset = new Vector3(0, 24, 0);

            // Create Spring Vector3 Animation for smooth entrance
            SpringVector3NaturalMotionAnimation springOffset = compositor.CreateSpringVector3Animation();
            springOffset.Target = "Offset";
            springOffset.FinalValue = new Vector3(0, 0, 0);
            springOffset.DampingRatio = 0.75f; // Slight spring bounce
            springOffset.Period = TimeSpan.FromMilliseconds(350);

            // Create Opacity Animation
            ScalarKeyFrameAnimation opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0.0f, 0.0f);
            opacityAnim.InsertKeyFrame(1.0f, 1.0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
            opacityAnim.Duration = TimeSpan.FromMilliseconds(400);

            if (delayMs > 0)
            {
                springOffset.DelayTime = TimeSpan.FromMilliseconds(delayMs);
                opacityAnim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            }

            visual.StartAnimation("Offset", springOffset);
            visual.StartAnimation("Opacity", opacityAnim);
        }

        /// <summary>
        /// Applies a 3D depth tilt & elevation scale micro-interaction on mouse pointer hover.
        /// </summary>

        public static void EnableHoverDepthEffect(FrameworkElement element, float maxScale = 1.025f)
        {
            if (element == null) return;

            element.PointerEntered += (s, e) =>
            {
                Visual visual = ElementCompositionPreview.GetElementVisual(element);
                Compositor compositor = visual.Compositor;

                // Center point for scaling
                visual.CenterPoint = new Vector3((float)element.ActualWidth / 2f, (float)element.ActualHeight / 2f, 0);

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
    }
}

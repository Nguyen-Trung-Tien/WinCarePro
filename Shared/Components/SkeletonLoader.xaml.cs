using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Shared.Components
{
    /// <summary>
    /// WinCare Pro v4.0.0 — Skeleton Loader component.
    /// Displays shimmer-animated placeholder bars while content is loading.
    /// Call Show() to display and Hide() to transition to actual content.
    /// </summary>
    public sealed partial class SkeletonLoader : UserControl
    {
        public SkeletonLoader()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Apply shimmer effect to all skeleton bars
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine1);
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine2);
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine3);
        }

        /// <summary>
        /// Hides the skeleton loader with a fade-out transition.
        /// </summary>
        public void Hide()
        {
            FluidAnimationHelper.StopShimmerEffect(SkeletonLine1);
            FluidAnimationHelper.StopShimmerEffect(SkeletonLine2);
            FluidAnimationHelper.StopShimmerEffect(SkeletonLine3);

            FluidAnimationHelper.ApplySlideAndFadeOut(SkeletonContainer, slideX: 0, durationMs: 250, onCompleted: () =>
            {
                this.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Shows the skeleton loader with shimmer animations.
        /// </summary>
        public void Show()
        {
            this.Visibility = Visibility.Visible;
            FluidAnimationHelper.ApplySpringEntranceAnimation(SkeletonContainer);
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine1);
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine2);
            FluidAnimationHelper.ApplyShimmerEffect(SkeletonLine3);
        }
    }
}

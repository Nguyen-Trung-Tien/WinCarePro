using Microsoft.UI.Xaml;

namespace WinCarePro.Shared.Animations
{
    /// <summary>
    /// Zero-Bloat flat stub: Instantaneous page transitions without GPU composition zoom/slide overhead.
    /// </summary>
    public static class PageTransitionHelper
    {
        public static void ApplyDrillInEntrance(UIElement pageRoot, float delayMs = 0) { }
        public static void ApplySlideUpReveal(UIElement pageRoot, float slideDistance = 0) { }

        public static void ApplyCrossFade(UIElement outElement, UIElement inElement, int durationMs = 0)
        {
            if (outElement is FrameworkElement outFe) outFe.Visibility = Visibility.Collapsed;
            if (inElement is FrameworkElement inFe) inFe.Visibility = Visibility.Visible;
        }

        public static void ApplyPageExitTransition(UIElement pageRoot, int durationMs = 0) { }
    }
}


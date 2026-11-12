using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Presentation.Controls;

/// <summary>
/// Skeleton Card shimmer placeholder control for smooth list loading states.
/// </summary>
public sealed partial class SkeletonCardControl : UserControl
{
    public SkeletonCardControl()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ShimmerPulseStoryboard.Begin();
        }
        catch
        {
            // Ignore if layout not ready
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ShimmerPulseStoryboard.Stop();
        }
        catch
        {
            // Ignore on teardown
        }
    }
}


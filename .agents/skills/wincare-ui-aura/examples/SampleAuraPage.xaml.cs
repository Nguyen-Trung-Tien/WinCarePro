using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Examples.UI;

/// <summary>
/// Code-behind demonstrating clean lifecycle and event unhooking in Unloaded
/// to guarantee zero memory leaks in WinUI 3.
/// </summary>
public sealed partial class SampleAuraPage : Page
{
    public SampleAuraPage()
    {
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to view-specific events or timers
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // Invariant: ALWAYS unhook event handlers to prevent memory leaks
        this.Loaded -= OnPageLoaded;
        this.Unloaded -= OnPageUnloaded;

        // Clean up any canvas/composition animations here
    }
}


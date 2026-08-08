using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Core.Helpers;

public static class UiLoadingHelper
{
    /// <summary>
    /// Synchronizes button loading animation state, progress ring, icon visibility,
    /// dynamic localized text, button locking, and minimum display duration.
    /// </summary>
    public static async Task ExecuteWithLoadingAsync(
        Button? button,
        ProgressRing? progressRing,
        TextBlock? textBlock,
        FontIcon? fontIcon,
        string loadingText,
        string originalText,
        Func<Task> action,
        int minDurationMs = 1200)
    {
        if (button != null) button.IsEnabled = false;

        if (progressRing != null)
        {
            progressRing.Visibility = Visibility.Visible;
            progressRing.IsActive = true;
        }

        if (fontIcon != null) fontIcon.Visibility = Visibility.Collapsed;
        if (textBlock != null) textBlock.Text = loadingText.T();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action();
        }
        finally
        {
            stopwatch.Stop();
            int remainingMs = minDurationMs - (int)stopwatch.ElapsedMilliseconds;
            if (remainingMs > 0)
            {
                await Task.Delay(remainingMs);
            }

            if (progressRing != null)
            {
                progressRing.IsActive = false;
                progressRing.Visibility = Visibility.Collapsed;
            }

            if (fontIcon != null) fontIcon.Visibility = Visibility.Visible;
            if (textBlock != null) textBlock.Text = originalText.T();
            if (button != null) button.IsEnabled = true;
        }
    }
}

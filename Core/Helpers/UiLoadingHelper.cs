using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using WinCarePro.Services;

namespace WinCarePro.Core.Helpers;

public static class UiLoadingHelper
{
    private static readonly ConcurrentDictionary<Button, bool> ActiveLoadingButtons = new();

    /// <summary>
    /// Synchronizes button loading animation state, progress ring, icon visibility,
    /// dynamic localized text, button locking, and minimum display duration safely across UI threads.
    /// Includes layout stability protection against width shifting and click debouncing.
    /// </summary>
    public static async Task ExecuteWithLoadingAsync(
        Button? button,
        ProgressRing? progressRing,
        TextBlock? textBlock,
        FontIcon? fontIcon,
        string loadingText,
        string originalText,
        Func<Task> action,
        int minDurationMs = 400)
    {
        // Debounce: prevent rapid multi-clicking re-entrancy
        if (button != null)
        {
            if (!ActiveLoadingButtons.TryAdd(button, true))
            {
                return; // Already loading, ignore duplicate rapid clicks
            }
        }

        var dispatcher = button?.DispatcherQueue ?? progressRing?.DispatcherQueue ?? textBlock?.DispatcherQueue ?? App.MainDispatcherQueue;

        void SafeUiUpdate(Action updateAction)
        {
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() => updateAction());
            }
            else
            {
                updateAction();
            }
        }

        double originalMinWidth = button?.MinWidth ?? 0;

        SafeUiUpdate(() =>
        {
            if (button != null)
            {
                // Lock current width to prevent layout shift ("nhảy giao diện button")
                if (button.ActualWidth > 0 && button.MinWidth < button.ActualWidth)
                {
                    button.MinWidth = button.ActualWidth;
                }
                button.IsEnabled = false;
            }
            if (progressRing != null)
            {
                progressRing.Visibility = Visibility.Visible;
                progressRing.IsActive = true;
            }
            if (fontIcon != null) fontIcon.Visibility = Visibility.Collapsed;
            if (textBlock != null) textBlock.Text = loadingText.T();
        });

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

            SafeUiUpdate(() =>
            {
                if (progressRing != null)
                {
                    progressRing.IsActive = false;
                    progressRing.Visibility = Visibility.Collapsed;
                }
                if (fontIcon != null) fontIcon.Visibility = Visibility.Visible;
                if (textBlock != null) textBlock.Text = originalText.T();
                if (button != null)
                {
                    button.MinWidth = originalMinWidth;
                    button.IsEnabled = true;
                }
            });

            if (button != null)
            {
                ActiveLoadingButtons.TryRemove(button, out _);
            }
        }
    }
}

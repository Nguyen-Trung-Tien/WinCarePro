using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.Presentation.Controls.Dialogs;

/// <summary>
/// Unified Aura Glass modal builder so pages never duplicate raw ContentDialog styling or ShowAsync error handling.
/// </summary>
public static class AuraModalHelper
{
    public static ContentDialog CreateThemedDialog(
        XamlRoot xamlRoot,
        object content,
        string? title = null,
        string? closeButtonText = null,
        string? primaryButtonText = null,
        string? secondaryButtonText = null)
    {
        var theme = Application.Current.RequestedTheme == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = closeButtonText,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            DefaultButton = !string.IsNullOrEmpty(primaryButtonText) ? ContentDialogButton.Primary : ContentDialogButton.Close,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1)
        };

        if (Application.Current.Resources.TryGetValue("AppCardBackground", out var bg) && bg is Brush bgBrush)
            dialog.Background = bgBrush;
        if (Application.Current.Resources.TryGetValue("AppCardBorder", out var border) && border is Brush borderBrush)
            dialog.BorderBrush = borderBrush;
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj) && styleObj is Style accentStyle)
        {
            if (!string.IsNullOrEmpty(primaryButtonText))
                dialog.PrimaryButtonStyle = accentStyle;
            else
                dialog.CloseButtonStyle = accentStyle;
        }

        return dialog;
    }

    public static async Task<ContentDialogResult> ShowSafeAsync(ContentDialog dialog, string context)
    {
        try
        {
            return await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(context, ex);
            return ContentDialogResult.None;
        }
    }
}

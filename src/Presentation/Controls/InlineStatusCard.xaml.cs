using System;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Presentation.Controls;

public enum InlineStatusCardType
{
    SafetyNotice,
    Summary,
    AlertElevation
}

public sealed partial class InlineStatusCard : UserControl
{
    public static readonly DependencyProperty CardTypeProperty =
        DependencyProperty.Register(nameof(CardType), typeof(InlineStatusCardType), typeof(InlineStatusCard),
            new PropertyMetadata(InlineStatusCardType.SafetyNotice));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(InlineStatusCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(InlineStatusCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(InlineStatusCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(InlineStatusCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CustomGlyphProperty =
        DependencyProperty.Register(nameof(CustomGlyph), typeof(string), typeof(InlineStatusCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CustomBadgeTextProperty =
        DependencyProperty.Register(nameof(CustomBadgeText), typeof(string), typeof(InlineStatusCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsDismissibleProperty =
        DependencyProperty.Register(nameof(IsDismissible), typeof(bool), typeof(InlineStatusCard),
            new PropertyMetadata(false));

    public event RoutedEventHandler? Dismissed;

    public InlineStatusCard()
    {
        InitializeComponent();
    }

    public InlineStatusCardType CardType
    {
        get => (InlineStatusCardType)GetValue(CardTypeProperty);
        set => SetValue(CardTypeProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public string CustomGlyph
    {
        get => (string)GetValue(CustomGlyphProperty);
        set => SetValue(CustomGlyphProperty, value);
    }

    public string CustomBadgeText
    {
        get => (string)GetValue(CustomBadgeTextProperty);
        set => SetValue(CustomBadgeTextProperty, value);
    }

    public bool IsDismissible
    {
        get => (bool)GetValue(IsDismissibleProperty);
        set => SetValue(IsDismissibleProperty, value);
    }

    private void OnDismissClick(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
        Dismissed?.Invoke(this, e);
    }

    private static bool TryGetAppResource<T>(string key, out T resource)
    {
        if (Application.Current?.Resources == null)
        {
            resource = default!;
            return false;
        }

        if (Application.Current.Resources.TryGetValue(key, out var val) && val is T res)
        {
            resource = res;
            return true;
        }

        foreach (var md in Application.Current.Resources.MergedDictionaries)
        {
            if (md.TryGetValue(key, out var mVal) && mVal is T mRes)
            {
                resource = mRes;
                return true;
            }
        }

        resource = default!;
        return false;
    }

    public static Style? ResolveCardStyle(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyNoticeCardStyle",
            InlineStatusCardType.Summary => "InlineSummaryCardStyle",
            InlineStatusCardType.AlertElevation => "InlineAlertElevationCardStyle",
            _ => "InlineSafetyNoticeCardStyle"
        };

        return TryGetAppResource<Style>(key, out var style) ? style : null;
    }

    public static Brush ResolveCardBackground(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyCardBackground",
            InlineStatusCardType.Summary => "InlineSummaryCardBackground",
            InlineStatusCardType.AlertElevation => "InlineAlertCardBackground",
            _ => "InlineSafetyCardBackground"
        };

        if (TryGetAppResource<Brush>(key, out var b)) return b;
        return new SolidColorBrush(ColorHelper.FromArgb(0x18, 0x10, 0xB9, 0x81));
    }

    public static Brush ResolveCardBorder(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyCardBorder",
            InlineStatusCardType.Summary => "InlineSummaryCardBorder",
            InlineStatusCardType.AlertElevation => "InlineAlertCardBorder",
            _ => "InlineSafetyCardBorder"
        };

        if (TryGetAppResource<Brush>(key, out var b)) return b;
        return new SolidColorBrush(ColorHelper.FromArgb(0x35, 0x10, 0xB9, 0x81));
    }

    public static Brush ResolveBadgeBackground(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyCardBackground",
            InlineStatusCardType.Summary => "InlineSummaryCardBackground",
            InlineStatusCardType.AlertElevation => "InlineAlertCardBackground",
            _ => "InlineSafetyCardBackground"
        };

        if (TryGetAppResource<Brush>(key, out var brush)) return brush;
        return new SolidColorBrush(ColorHelper.FromArgb(0x18, 0x10, 0xB9, 0x81));
    }

    public static Brush ResolveBadgeBorder(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyCardBorder",
            InlineStatusCardType.Summary => "InlineSummaryCardBorder",
            InlineStatusCardType.AlertElevation => "InlineAlertCardBorder",
            _ => "InlineSafetyCardBorder"
        };

        if (TryGetAppResource<Brush>(key, out var brush)) return brush;
        return new SolidColorBrush(ColorHelper.FromArgb(0x35, 0x10, 0xB9, 0x81));
    }

    public static Brush ResolveGlyphBrush(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyCardGlyph",
            InlineStatusCardType.Summary => "InlineSummaryCardGlyph",
            InlineStatusCardType.AlertElevation => "InlineAlertCardGlyph",
            _ => "InlineSafetyCardGlyph"
        };

        if (TryGetAppResource<Brush>(key, out var brush)) return brush;
        return new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x10, 0xB9, 0x81));
    }

    public static string ResolveGlyph(InlineStatusCardType type, string custom)
    {
        if (!string.IsNullOrEmpty(custom)) return custom;
        return type switch
        {
            InlineStatusCardType.SafetyNotice => "\uE72E",   // Lock / Shield
            InlineStatusCardType.Summary => "\uE73E",        // Checkmark
            InlineStatusCardType.AlertElevation => "\uE7BA", // Warning / Alert
            _ => "\uE72E"
        };
    }

    public static string ResolveBadgeLabel(InlineStatusCardType type, string custom)
    {
        if (!string.IsNullOrEmpty(custom)) return custom;
        return type switch
        {
            InlineStatusCardType.SafetyNotice => "PROTECTED",
            InlineStatusCardType.Summary => "OPTIMIZED",
            InlineStatusCardType.AlertElevation => "ELEVATION NEEDED",
            _ => "NOTICE"
        };
    }

    public static Visibility HasTextVisibility(string text) =>
        string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility BoolToVisibility(bool val) =>
        val ? Visibility.Visible : Visibility.Collapsed;
}


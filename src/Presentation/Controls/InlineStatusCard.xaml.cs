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

    public static Style? ResolveCardStyle(InlineStatusCardType type)
    {
        var key = type switch
        {
            InlineStatusCardType.SafetyNotice => "InlineSafetyNoticeCardStyle",
            InlineStatusCardType.Summary => "InlineSummaryCardStyle",
            InlineStatusCardType.AlertElevation => "InlineAlertElevationCardStyle",
            _ => "InlineSafetyNoticeCardStyle"
        };

        return Application.Current.Resources.TryGetValue(key, out var style) && style is Style s ? s : null;
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

        if (Application.Current.Resources.TryGetValue(key, out var brush) && brush is Brush b) return b;
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

        if (Application.Current.Resources.TryGetValue(key, out var brush) && brush is Brush b) return b;
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

        if (Application.Current.Resources.TryGetValue(key, out var brush) && brush is Brush b) return b;
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


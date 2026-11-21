using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Presentation.Controls;

public sealed partial class AuraPageHeader : UserControl
{
    public AuraPageHeader()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AuraPageHeader), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AuraPageHeader), new PropertyMetadata(string.Empty));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(AuraPageHeader), new PropertyMetadata("\uE80F"));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(AuraPageHeader), new PropertyMetadata(null));

    public Brush? IconForeground
    {
        get => (Brush?)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(AuraPageHeader), new PropertyMetadata(null));

    public Brush? IconBackground
    {
        get => (Brush?)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }

    public static readonly DependencyProperty IconBorderBrushProperty =
        DependencyProperty.Register(nameof(IconBorderBrush), typeof(Brush), typeof(AuraPageHeader), new PropertyMetadata(null));

    public Brush? IconBorderBrush
    {
        get => (Brush?)GetValue(IconBorderBrushProperty);
        set => SetValue(IconBorderBrushProperty, value);
    }

    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register(nameof(RightContent), typeof(object), typeof(AuraPageHeader), new PropertyMetadata(null));

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public Visibility HasSubtitleVisibility(string? subtitle) =>
        string.IsNullOrWhiteSpace(subtitle) ? Visibility.Collapsed : Visibility.Visible;
}

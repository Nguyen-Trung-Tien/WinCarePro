using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Presentation.Controls;

public sealed partial class AuraPageHeader : UserControl
{
    public AuraPageHeader()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        ActualThemeChanged += OnActualThemeChanged;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ApplyDefaultIconBrushes();
    private void OnActualThemeChanged(FrameworkElement sender, object args) => ApplyDefaultIconBrushes();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ActualThemeChanged -= OnActualThemeChanged;
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    private void ApplyDefaultIconBrushes()
    {
        if (IconBackground == null && Application.Current?.Resources.TryGetValue("AppBadgeNeutralBg", out var bg) == true && bg is Brush bgBrush)
            IconBadgeBorder.Background = bgBrush;
        if (IconBorderBrush == null && Application.Current?.Resources.TryGetValue("AppBadgeNeutralBorder", out var br) == true && br is Brush brBrush)
            IconBadgeBorder.BorderBrush = brBrush;
        if (IconForeground == null && Application.Current?.Resources.TryGetValue("AppBadgeNeutralFg", out var fg) == true && fg is Brush fgBrush)
            HeaderIcon.Foreground = fgBrush;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (RightActionsPresenter == null || RightCol == null) return;
        bool isNarrow = e.NewSize.Width > 0 && e.NewSize.Width < 640;
        if (isNarrow)
        {
            RightCol.Width = new GridLength(0, GridUnitType.Pixel);
            Grid.SetRow(RightActionsPresenter, 1);
            Grid.SetColumn(RightActionsPresenter, 0);
            RightActionsPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            RightCol.Width = GridLength.Auto;
            Grid.SetRow(RightActionsPresenter, 0);
            Grid.SetColumn(RightActionsPresenter, 1);
            RightActionsPresenter.HorizontalAlignment = HorizontalAlignment.Right;
        }
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

using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Presentation.Controls;

public enum AuraAlertSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public sealed partial class AuraAlertBanner : UserControl
{
    public AuraAlertBanner()
    {
        InitializeComponent();
        ApplySeverityVisuals(Severity);
        ActualThemeChanged += OnActualThemeChanged;
        Unloaded += OnUnloaded;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args) => ApplySeverityVisuals(Severity);

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
        Unloaded -= OnUnloaded;
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(AuraAlertBanner), new PropertyMetadata(true));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(AuraAlertBanner), new PropertyMetadata(false));

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(AuraAlertSeverity), typeof(AuraAlertBanner),
            new PropertyMetadata(AuraAlertSeverity.Info, OnSeverityChanged));

    public AuraAlertSeverity Severity
    {
        get => (AuraAlertSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AuraAlertBanner), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(AuraAlertBanner), new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(AuraAlertBanner), new PropertyMetadata(string.Empty));

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(AuraAlertBanner), new PropertyMetadata(null));

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuraAlertBanner banner && e.NewValue is AuraAlertSeverity severity)
        {
            banner.ApplySeverityVisuals(severity);
        }
    }

    public static (string Glyph, string BgKey, string BorderKey, string FgKey) ResolveSeverityTokens(AuraAlertSeverity severity)
        => severity switch
        {
            AuraAlertSeverity.Success => ("\uE73E", "AppStatusGoodBackground", "AppStatusGoodBorder", "AppStatusGoodBrush"),
            AuraAlertSeverity.Warning => ("\uE7BA", "AppStatusWarningBackground", "AppStatusWarningBorder", "AppStatusWarningBrush"),
            AuraAlertSeverity.Error => ("\uEA39", "AppStatusDangerBackground", "AppStatusDangerBorder", "AppStatusDangerBrush"),
            _ => ("\uE946", "AppStatusInfoBackground", "AppStatusInfoBorder", "AppStatusInfoBrush")
        };

    private void ApplySeverityVisuals(AuraAlertSeverity severity)
    {
        if (BannerBorder == null || SeverityIcon == null) return;
        var (glyph, bgKey, borderKey, fgKey) = ResolveSeverityTokens(severity);
        SeverityIcon.Glyph = glyph;

        if (Application.Current?.Resources.TryGetValue(bgKey, out var bg) == true && bg is Brush bgBrush)
            BannerBorder.Background = bgBrush;
        if (Application.Current?.Resources.TryGetValue(borderKey, out var br) == true && br is Brush borderBrush)
            BannerBorder.BorderBrush = borderBrush;
        if (Application.Current?.Resources.TryGetValue(fgKey, out var fg) == true && fg is Brush fgBrush)
            SeverityIcon.Foreground = fgBrush;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => IsOpen = false;

    public Visibility HasTextVisibility(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
}

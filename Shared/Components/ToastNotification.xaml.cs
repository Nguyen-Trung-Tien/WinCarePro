using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Components;

public sealed partial class ToastNotification : UserControl
{
    public Button CloseButton => CloseBtn;
    public string SeverityText { get; private set; } = "Info";
    public string TitleText { get; private set; } = "";
    public int CurrentRepeatCount { get; private set; } = 1;
    public Action<ToastNotification>? DismissRequested { get; set; }

    private List<NotificationAction>? _currentActions;
    private DispatcherTimer? _countdownTimer;
    private int _remainingDurationMs = 5500;
    private int _totalDurationMs = 5500;
    private bool _isPaused = false;
    private bool _isEventsHooked = false;

    public ToastNotification()
    {
        this.InitializeComponent();
        CloseBtn.Click += (s, e) => DismissRequested?.Invoke(this);

        this.PointerEntered += ToastNotification_PointerEntered;
        this.PointerExited += ToastNotification_PointerExited;

        this.Loaded += ToastNotification_Loaded;
        this.Unloaded += ToastNotification_Unloaded;

        SyncTheme();
    }

    public ToastNotification(string title, string message, string level) : this()
    {
        Update(title, message, level, null, 1);
    }

    private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isEventsHooked)
        {
            ThemeManager.Instance.ThemeChanged += OnThemeOrAccentChanged;
            ThemeManager.Instance.AccentChanged += OnThemeOrAccentChanged;
            _isEventsHooked = true;
        }
        SyncTheme();
    }

    private void ToastNotification_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isEventsHooked)
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeOrAccentChanged;
            ThemeManager.Instance.AccentChanged -= OnThemeOrAccentChanged;
            _isEventsHooked = false;
        }
    }

    private void OnThemeOrAccentChanged(object? sender, EventArgs e)
    {
        this.DispatcherQueue?.TryEnqueue(() =>
        {
            SyncTheme();
            ApplySeverityStyle(SeverityText);
        });
    }

    private void SyncTheme()
    {
        this.RequestedTheme = ThemeManager.Instance.CurrentTheme;
    }

    private void ToastNotification_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPaused = true;
    }

    private void ToastNotification_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPaused = false;
    }

    public void Update(string title, string message, string level, List<NotificationAction>? actions, int repeatCount)
    {
        SyncTheme();

        TitleTextBlock.Text = title;
        TitleText = title;
        CurrentRepeatCount = repeatCount;
        DescTextBlock.Text = message;
        SeverityText = level;

        // Repeat counter badge
        if (repeatCount > 1)
        {
            RepeatBadge.Visibility = Visibility.Visible;
            RepeatCountText.Text = $"x{repeatCount}";
            try
            {
                RepeatPulseAnimation.Begin();
            }
            catch { }
        }
        else
        {
            RepeatBadge.Visibility = Visibility.Collapsed;
        }

        // Style according to severity level and current active theme
        ApplySeverityStyle(level);

        // Setup contextual action button
        _currentActions = actions;
        if (actions != null && actions.Count > 0)
        {
            var firstAction = actions[0];
            ActionBtn.Content = firstAction.Label;
            ActionBtn.Visibility = Visibility.Visible;
            
            ActionBtn.Click -= ActionBtn_Click;
            ActionBtn.Click += ActionBtn_Click;
        }
        else
        {
            ActionBtn.Visibility = Visibility.Collapsed;
        }

        // Start / Reset countdown progress
        StartCountdown(5500);
    }

    private void ActionBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentActions != null && _currentActions.Count > 0)
        {
            try
            {
                _currentActions[0].Action?.Invoke();
            }
            catch { }
        }
        DismissRequested?.Invoke(this);
    }

    public void StartCountdown(int durationMs = 5500)
    {
        _totalDurationMs = durationMs;
        _remainingDurationMs = durationMs;
        _isPaused = false;

        DismissProgressBar.Value = 100;

        _countdownTimer?.Stop();
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };

        _countdownTimer.Tick += (s, e) =>
        {
            if (_isPaused) return;

            _remainingDurationMs -= 50;
            if (_remainingDurationMs <= 0)
            {
                _countdownTimer.Stop();
                DismissProgressBar.Value = 0;
                DismissRequested?.Invoke(this);
            }
            else
            {
                double pct = ((double)_remainingDurationMs / _totalDurationMs) * 100.0;
                DismissProgressBar.Value = Math.Max(0, Math.Min(100, pct));
            }
        };

        _countdownTimer.Start();
    }

    public void Reset()
    {
        _countdownTimer?.Stop();
        _countdownTimer = null;
        _isPaused = false;

        try
        {
            PulseAnimation.Stop();
            RepeatPulseAnimation.Stop();
        }
        catch { }

        TitleTextBlock.Text = "";
        DescTextBlock.Text = "";
        ActionBtn.Visibility = Visibility.Collapsed;
        ActionBtn.Click -= ActionBtn_Click;
        _currentActions = null;
        DismissRequested = null;
        RepeatBadge.Visibility = Visibility.Collapsed;
        DismissProgressBar.Value = 100;
        Opacity = 0;
        RenderTransform = new TranslateTransform { X = 380 };
    }

    private void ApplySeverityStyle(string level)
    {
        bool isDark = (ThemeManager.Instance.CurrentTheme == ElementTheme.Dark || 
                      (ThemeManager.Instance.CurrentTheme == ElementTheme.Default && 
                       this.ActualTheme != ElementTheme.Light));

        string glyph = "\uE946"; // Info
        Color fgColor;
        Color bgColor;
        Color borderColor;

        if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uE7BA";
            if (isDark)
            {
                fgColor = Color.FromArgb(255, 251, 191, 36);   // #FBBF24
                bgColor = Color.FromArgb(32, 245, 158, 11);    // 12% amber
                borderColor = Color.FromArgb(68, 245, 158, 11);// 27% amber
            }
            else
            {
                fgColor = Color.FromArgb(255, 180, 83, 9);     // #B45309
                bgColor = Color.FromArgb(255, 255, 251, 235);  // #FFFBEB
                borderColor = Color.FromArgb(255, 253, 230, 138);// #FDE68A
            }
        }
        else if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uEA39";
            if (isDark)
            {
                fgColor = Color.FromArgb(255, 239, 68, 68);    // #EF4444
                bgColor = Color.FromArgb(32, 239, 68, 68);     // 12% red
                borderColor = Color.FromArgb(68, 239, 68, 68); // 27% red
            }
            else
            {
                fgColor = Color.FromArgb(255, 220, 38, 38);    // #DC2626
                bgColor = Color.FromArgb(255, 254, 242, 242);  // #FEF2F2
                borderColor = Color.FromArgb(255, 254, 202, 202);// #FECACA
            }
        }
        else if (level.Equals("Success", StringComparison.OrdinalIgnoreCase) || level.Equals("Info", StringComparison.OrdinalIgnoreCase))
        {
            // Success uses checkmark glyph \uE73E, Info uses info glyph \uE946
            glyph = level.Equals("Success", StringComparison.OrdinalIgnoreCase) ? "\uE73E" : "\uE946";
            
            var accent = ThemeManager.Instance.GetPrimaryAccentColor();
            fgColor = accent;

            if (isDark)
            {
                bgColor = Color.FromArgb(32, accent.R, accent.G, accent.B);
                borderColor = Color.FromArgb(70, accent.R, accent.G, accent.B);
            }
            else
            {
                byte bgR = (byte)Math.Clamp(accent.R + (255 - accent.R) * 0.92, 0, 255);
                byte bgG = (byte)Math.Clamp(accent.G + (255 - accent.G) * 0.92, 0, 255);
                byte bgB = (byte)Math.Clamp(accent.B + (255 - accent.B) * 0.92, 0, 255);
                bgColor = Color.FromArgb(255, bgR, bgG, bgB);

                byte bdrR = (byte)Math.Clamp(accent.R + (255 - accent.R) * 0.65, 0, 255);
                byte bdrG = (byte)Math.Clamp(accent.G + (255 - accent.G) * 0.65, 0, 255);
                byte bdrB = (byte)Math.Clamp(accent.B + (255 - accent.B) * 0.65, 0, 255);
                borderColor = Color.FromArgb(255, bdrR, bdrG, bdrB);
            }
        }
        else // Default / Fallback
        {
            glyph = "\uE946";
            var accent = ThemeManager.Instance.GetPrimaryAccentColor();
            fgColor = accent;
            bgColor = isDark ? Color.FromArgb(32, accent.R, accent.G, accent.B) : Color.FromArgb(255, 240, 249, 255);
            borderColor = isDark ? Color.FromArgb(70, accent.R, accent.G, accent.B) : Color.FromArgb(255, 186, 230, 253);
        }

        var fgBrush = new SolidColorBrush(fgColor);
        var bgBrush = new SolidColorBrush(bgColor);
        var borderBrush = new SolidColorBrush(borderColor);

        StatusIcon.Glyph = glyph;
        StatusIcon.Foreground = fgBrush;
        IconGlowRing.Background = bgBrush;
        IconGlowRing.BorderBrush = borderBrush;
        AmbientTintBorder.Background = bgBrush;
        RepeatBadge.Background = fgBrush;

        if ((level.Equals("Info", StringComparison.OrdinalIgnoreCase) || level.Equals("Success", StringComparison.OrdinalIgnoreCase)) && 
            Application.Current.Resources.TryGetValue("PrimaryAccentGradient", out var gradObj) && 
            gradObj is Brush gradBrush)
        {
            DismissProgressBar.Foreground = gradBrush;
        }
        else
        {
            DismissProgressBar.Foreground = fgBrush;
        }

        if (Application.Current.Resources.TryGetValue("PrimaryAccentGradient", out var actGradObj) && 
            actGradObj is Brush actGradBrush)
        {
            ActionBtn.Background = actGradBrush;
        }

        // Container Card Theme Adapting
        if (isDark)
        {
            ContainerBorder.Background = new SolidColorBrush(Color.FromArgb(238, 20, 22, 32)); // #E8141620
            if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                ContainerBorder.BorderBrush = fgBrush;
            }
            else
            {
                ContainerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            }
        }
        else
        {
            ContainerBorder.Background = new SolidColorBrush(Color.FromArgb(252, 255, 255, 255)); // Crisp Glass White
            if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                ContainerBorder.BorderBrush = fgBrush;
            }
            else
            {
                ContainerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)); // #E2E8F0
            }
        }

        // Apply critical pulse if needed
        if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                PulseAnimation.Begin();
            }
            catch { }
        }
        else
        {
            try
            {
                PulseAnimation.Stop();
            }
            catch { }
        }
    }

    public void AnimateIn(Action? onCompleted = null)
    {
        var trans = new TranslateTransform { X = 380 };
        this.RenderTransform = trans;
        this.Opacity = 0;

        var sb = new Storyboard();
        var ease = new CircleEase { EasingMode = EasingMode.EaseOut };
        var animX = new DoubleAnimation { From = 380, To = 0, Duration = TimeSpan.FromMilliseconds(380), EasingFunction = ease };
        var animOpacity = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(280), EasingFunction = ease };

        Storyboard.SetTarget(animX, this);
        Storyboard.SetTargetProperty(animX, "(UIElement.RenderTransform).(TranslateTransform.X)");

        Storyboard.SetTarget(animOpacity, this);
        Storyboard.SetTargetProperty(animOpacity, "Opacity");

        sb.Children.Add(animX);
        sb.Children.Add(animOpacity);
        
        if (onCompleted != null)
        {
            sb.Completed += (s, e) => onCompleted();
        }
        
        sb.Begin();
    }

    public void AnimateOut(Action? onCompleted = null)
    {
        var sb = new Storyboard();
        var ease = new CircleEase { EasingMode = EasingMode.EaseIn };
        var animOpacity = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(220), EasingFunction = ease };
        var animX = new DoubleAnimation { To = 140, Duration = TimeSpan.FromMilliseconds(220), EasingFunction = ease };

        Storyboard.SetTarget(animOpacity, this);
        Storyboard.SetTargetProperty(animOpacity, "Opacity");

        Storyboard.SetTarget(animX, this);
        Storyboard.SetTargetProperty(animX, "(UIElement.RenderTransform).(TranslateTransform.X)");

        sb.Children.Add(animOpacity);
        sb.Children.Add(animX);
        
        sb.Completed += (s, e) =>
        {
            onCompleted?.Invoke();
        };
        
        sb.Begin();
    }
}



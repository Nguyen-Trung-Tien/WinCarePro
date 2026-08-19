using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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

    public ToastNotification()
    {
        this.InitializeComponent();
        CloseBtn.Click += (s, e) => DismissRequested?.Invoke(this);

        this.PointerEntered += ToastNotification_PointerEntered;
        this.PointerExited += ToastNotification_PointerExited;
    }

    public ToastNotification(string title, string message, string level) : this()
    {
        Update(title, message, level, null, 1);
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

        // Style according to severity level
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
        string glyph = "\uE946"; // Info
        string hexColor = "#FF8B5CF6"; // Aurora Violet

        if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uE7BA";
            hexColor = "#FFF59E0B"; // Amber
        }
        else if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uEA39";
            hexColor = "#FFEF4444"; // Red
        }
        else if (level.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uE73E";
            hexColor = "#FF10B981"; // Emerald
        }

        byte a = 255;
        byte r = Convert.ToByte(hexColor.Substring(1, 2), 16);
        byte g = Convert.ToByte(hexColor.Substring(3, 2), 16);
        byte b = Convert.ToByte(hexColor.Substring(5, 2), 16);

        var solidColor = Windows.UI.Color.FromArgb(a, r, g, b);
        var alpha15Color = Windows.UI.Color.FromArgb(38, r, g, b);
        var alpha35Color = Windows.UI.Color.FromArgb(90, r, g, b);

        var solidBrush = new SolidColorBrush(solidColor);
        var tintBrush = new SolidColorBrush(alpha15Color);
        var borderBrush = new SolidColorBrush(alpha35Color);

        StatusIcon.Glyph = glyph;
        StatusIcon.Foreground = solidBrush;
        IconGlowRing.Background = tintBrush;
        IconGlowRing.BorderBrush = borderBrush;
        AmbientTintBorder.Background = tintBrush;
        DismissProgressBar.Foreground = solidBrush;
        RepeatBadge.Background = solidBrush;

        // Apply critical pulse if needed
        if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            ContainerBorder.BorderBrush = solidBrush;
            try
            {
                PulseAnimation.Begin();
            }
            catch { }
        }
        else
        {
            ContainerBorder.ClearValue(Border.BorderBrushProperty);
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


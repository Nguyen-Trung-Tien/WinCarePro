using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    public ToastNotification()
    {
        this.InitializeComponent();
        CloseBtn.Click += (s, e) => DismissRequested?.Invoke(this);
    }

    public ToastNotification(string title, string message, string level) : this()
    {
        Update(title, message, level, null, 1);
    }

    public void Update(string title, string message, string level, List<NotificationAction>? actions, int repeatCount)
    {
        TitleTextBlock.Text = title;
        TitleText = title;
        CurrentRepeatCount = repeatCount;
        DescTextBlock.Text = message;
        SeverityText = level;

        // Reset visibility of elements
        RepeatBadge.Visibility = repeatCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        RepeatCountText.Text = repeatCount.ToString();

        // Style according to severity level
        ApplySeverityStyle(level);

        // Setup action buttons
        _currentActions = actions;
        if (actions != null && actions.Count > 0)
        {
            var firstAction = actions[0];
            ActionBtn.Content = firstAction.Label;
            ActionBtn.Visibility = Visibility.Visible;
            
            // Remove existing handlers to avoid memory leak and multiple registrations
            ActionBtn.Click -= ActionBtn_Click;
            ActionBtn.Click += ActionBtn_Click;
        }
        else
        {
            ActionBtn.Visibility = Visibility.Collapsed;
        }
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

    public void Reset()
    {
        // Stop animations
        try
        {
            PulseAnimation.Stop();
        }
        catch { }

        TitleTextBlock.Text = "";
        DescTextBlock.Text = "";
        ActionBtn.Visibility = Visibility.Collapsed;
        ActionBtn.Click -= ActionBtn_Click;
        _currentActions = null;
        DismissRequested = null;
        RepeatBadge.Visibility = Visibility.Collapsed;
        Opacity = 0;
        RenderTransform = new TranslateTransform { X = 360 };
    }

    private void ApplySeverityStyle(string level)
    {
        string glyph = "\uE946"; // Info
        string hexColor = "#FF3B82F6"; // Blue

        if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uE7BA";
            hexColor = "#FFF59E0B"; // Amber
        }
        else if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uEA39";
            hexColor = "#FFEF4444"; // Red
        }
        else if (level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uEA39";
            hexColor = "#FFEF4444"; // Red
        }
        else if (level.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            glyph = "\uE73E";
            hexColor = "#FF10B981"; // Green
        }

        var statusColor = Windows.UI.Color.FromArgb(255, 
            Convert.ToByte(hexColor.Substring(1, 2), 16), 
            Convert.ToByte(hexColor.Substring(3, 2), 16), 
            Convert.ToByte(hexColor.Substring(5, 2), 16));
        
        var statusBrush = new SolidColorBrush(statusColor);

        StatusAccentBar.Background = statusBrush;
        StatusIcon.Glyph = glyph;
        StatusIcon.Foreground = statusBrush;

        // Apply critical pulse if needed
        if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            ContainerBorder.BorderBrush = statusBrush;
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
        var trans = new TranslateTransform { X = 360 };
        this.RenderTransform = trans;
        this.Opacity = 0;

        var sb = new Storyboard();
        var animX = new DoubleAnimation { From = 360, To = 0, Duration = TimeSpan.FromMilliseconds(200) };
        var animOpacity = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(200) };

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
        var animOpacity = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(150) };

        Storyboard.SetTarget(animOpacity, this);
        Storyboard.SetTargetProperty(animOpacity, "Opacity");

        sb.Children.Add(animOpacity);
        
        sb.Completed += (s, e) =>
        {
            onCompleted?.Invoke();
        };
        
        sb.Begin();
    }
}


using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Services;

namespace WinCarePro.Components;

public sealed partial class CircularProgressMeter : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(CircularProgressMeter), new PropertyMetadata(100, OnValueChanged));

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private readonly RoutedEventHandler _loadedHandler;

    public CircularProgressMeter()
    {
        this.InitializeComponent();
        _loadedHandler = (s, e) => UpdateUI(Value);
        this.Loaded += _loadedHandler;
        this.Unloaded += (s, e) => this.Loaded -= _loadedHandler;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgressMeter meter)
        {
            meter.UpdateUI((int)e.NewValue);
        }
    }

    private void UpdateUI(int val)
    {
        if (ScoreLabel == null || StatusLabel == null)
            return;

        ScoreLabel.Text = val.ToString();

        // Calculate circular arc geometry (Radius = 60, Center = 85, 85)
        if (ArcFigure != null && ProgressArc != null)
        {
            double pct = Math.Clamp(val, 0.01, 99.99);
            double angleDegrees = pct * 3.6;
            double angleRadians = (angleDegrees - 90.0) * Math.PI / 180.0;
            const double radius = 60.0;
            const double centerX = 85.0;
            const double centerY = 85.0;

            ArcFigure.StartPoint = new Windows.Foundation.Point(centerX, centerY - radius);
            double endX = centerX + radius * Math.Cos(angleRadians);
            double endY = centerY + radius * Math.Sin(angleRadians);
            ProgressArc.Point = new Windows.Foundation.Point(endX, endY);
            ProgressArc.Size = new Windows.Foundation.Size(radius, radius);
            ProgressArc.IsLargeArc = angleDegrees > 180.0;
            ProgressArc.SweepDirection = SweepDirection.Clockwise;
        }

        Brush color;
        string status;
        string glowKey;

        if (val >= 90)
        {
            status = "Excellent";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Emerald Green
            glowKey = "GaugeGlowGoodBrush";
        }
        else if (val >= 70)
        {
            status = "Good";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)); // Blue
            glowKey = "GaugeGlowGoodBrush";
        }
        else if (val >= 50)
        {
            status = "Warning";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
            glowKey = "GaugeGlowWarningBrush";
        }
        else
        {
            status = "Critical";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            glowKey = "GaugeGlowCriticalBrush";
        }

        ScoreLabel.Foreground = color;
        StatusLabel.Text = status.T();
        StatusLabel.Foreground = color;

        if (GaugeProgressPath != null)
        {
            GaugeProgressPath.Stroke = color;
        }

        if (AuraGlowBorder != null && Application.Current.Resources.TryGetValue(glowKey, out var brushObj) && brushObj is Brush b)
        {
            AuraGlowBorder.Background = b;
        }
    }
}

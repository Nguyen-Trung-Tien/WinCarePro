using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

    public CircularProgressMeter()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => UpdateUI(Value);
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
        if (HealthProgressRing == null || ScoreLabel == null || StatusLabel == null)
            return;

        HealthProgressRing.Value = val;
        ScoreLabel.Text = val.ToString();

        Brush color;
        string status;

        if (val >= 90)
        {
            status = "Excellent";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Emerald Green
            ScoreLabel.Foreground = color;
        }
        else if (val >= 70)
        {
            status = "Good";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)); // Blue
            ScoreLabel.Foreground = color;
        }
        else if (val >= 50)
        {
            status = "Warning";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
            ScoreLabel.Foreground = color;
        }
        else
        {
            status = "Critical";
            color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            ScoreLabel.Foreground = color;
        }

        StatusLabel.Text = status;
        StatusLabel.Foreground = color;
        HealthProgressRing.Foreground = color;
    }
}

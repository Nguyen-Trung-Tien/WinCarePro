using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace WinCarePro.Presentation.Controls;

/// <summary>
/// Custom Circular Ring Gauge with rounded stroke caps and Tabular numerals
/// for WinCare Pro Fluent 2.0 Aura Glass telemetry and progress indicators.
/// </summary>
public sealed partial class AuraCircularRing : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(AuraCircularRing),
            new PropertyMetadata(0.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(AuraCircularRing),
            new PropertyMetadata(48.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(AuraCircularRing),
            new PropertyMetadata(4.5, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowPercentageProperty =
        DependencyProperty.Register(nameof(ShowPercentage), typeof(bool), typeof(AuraCircularRing),
            new PropertyMetadata(true));

    public static readonly DependencyProperty RingBrushProperty =
        DependencyProperty.Register(nameof(RingBrush), typeof(Brush), typeof(AuraCircularRing),
            new PropertyMetadata(null, OnBrushPropertyChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool ShowPercentage
    {
        get => (bool)GetValue(ShowPercentageProperty);
        set => SetValue(ShowPercentageProperty, value);
    }

    public Brush? RingBrush
    {
        get => (Brush?)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public Brush EffectiveRingBrush =>
        RingBrush ?? (Application.Current?.Resources.TryGetValue("AppStatusCyanBrush", out var res) == true && res is Brush b
            ? b
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 210, 255)));

    public string FormattedPercent => $"{Math.Clamp(Value, 0, 100):F0}%";

    public double LabelFontSize => Math.Max(9.0, Size * 0.24);

    public AuraCircularRing()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) => UpdateArcGeometry();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuraCircularRing ring)
        {
            ring.UpdateArcGeometry();
            ring.Bindings.Update();
        }
    }

    private static void OnBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuraCircularRing ring)
        {
            ring.Bindings.Update();
        }
    }

    private void UpdateArcGeometry()
    {
        if (ArcPath == null) return;

        double size = Math.Max(16.0, Size);
        double thickness = Math.Clamp(StrokeThickness, 1.0, size / 3.0);
        double radius = (size - thickness) / 2.0;
        double center = size / 2.0;
        double clamped = Math.Clamp(Value, 0.0, 100.0);

        if (clamped <= 0.01)
        {
            ArcPath.Data = null;
            return;
        }

        // Prevent degenerate 360-degree start==end point in ArcSegment
        double angleDeg = clamped >= 99.99 ? 359.99 : (clamped / 100.0) * 360.0;
        double angleRad = (angleDeg - 90.0) * Math.PI / 180.0;

        var startPoint = new Point(center, center - radius);
        var endPoint = new Point(
            center + radius * Math.Cos(angleRad),
            center + radius * Math.Sin(angleRad));

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false,
            IsFilled = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = angleDeg > 180.0,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        ArcPath.Data = geometry;
    }
}

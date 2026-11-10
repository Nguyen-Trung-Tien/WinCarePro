using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Shared.Components
{
    /// <summary>
    /// WinCare Pro v4.0.0 — AnimatedProgressRing component.
    /// Premium progress ring with gradient stroke, pulsing glow halo,
    /// smooth count-up value animation, and spring-based value transitions.
    /// </summary>
    public sealed partial class AnimatedProgressRing : UserControl
    {
        public AnimatedProgressRing()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
        }

        // ============================================
        // Dependency Properties
        // ============================================

        /// <summary>
        /// The ring's current value (0–100).
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(AnimatedProgressRing),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// The ring's diameter in pixels.
        /// </summary>
        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register("Diameter", typeof(double), typeof(AnimatedProgressRing),
                new PropertyMetadata(120.0, OnDiameterChanged));

        public double Diameter
        {
            get => (double)GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        /// <summary>
        /// Label text displayed below the value.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(AnimatedProgressRing),
                new PropertyMetadata("Score", OnLabelChanged));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Font size of the center value text.
        /// </summary>
        public static readonly DependencyProperty ValueFontSizeProperty =
            DependencyProperty.Register("ValueFontSize", typeof(double), typeof(AnimatedProgressRing),
                new PropertyMetadata(32.0, OnValueFontSizeChanged));

        public double ValueFontSize
        {
            get => (double)GetValue(ValueFontSizeProperty);
            set => SetValue(ValueFontSizeProperty, value);
        }

        /// <summary>
        /// Whether to animate the count-up on value change.
        /// </summary>
        public static readonly DependencyProperty AnimateCountProperty =
            DependencyProperty.Register("AnimateCount", typeof(bool), typeof(AnimatedProgressRing),
                new PropertyMetadata(true));

        public bool AnimateCount
        {
            get => (bool)GetValue(AnimateCountProperty);
            set => SetValue(AnimateCountProperty, value);
        }

        // ============================================
        // Property Changed Handlers
        // ============================================

        private int _previousValue = 0;

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressRing ring)
            {
                int newVal = (int)(double)e.NewValue;
                ring.ValueRing.Value = (double)e.NewValue;

                if (ring.AnimateCount && ring.IsLoaded)
                {
                    FluidAnimationHelper.AnimateCountUp(ring.ValueText, ring._previousValue, newVal, 800);
                }
                else
                {
                    ring.ValueText.Text = newVal.ToString();
                }

                ring._previousValue = newVal;
            }
        }

        private static void OnDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressRing ring)
            {
                ring.UpdateDimensions();
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressRing ring && e.NewValue is string label)
            {
                ring.LabelText.Text = label;
            }
        }

        private static void OnValueFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedProgressRing ring && e.NewValue is double size)
            {
                ring.ValueText.FontSize = size;
            }
        }

        // ============================================
        // Layout
        // ============================================

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDimensions();

            // Start pulsing glow on the halo
            FluidAnimationHelper.ApplyPulseGlow(GlowHalo, 0.04f, 0.20f, 2500);

            // Entrance animation
            FluidAnimationHelper.ApplySpringEntranceAnimation(RingContainer);

            // Set initial text
            ValueText.Text = ((int)Value).ToString();
            _previousValue = (int)Value;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDimensions();
        }

        private void UpdateDimensions()
        {
            double diameter = Diameter;

            RingContainer.Width = diameter;
            RingContainer.Height = diameter;

            GlowHalo.Width = diameter + 10;
            GlowHalo.Height = diameter + 10;

            TrackRing.Width = diameter - 6;
            TrackRing.Height = diameter - 6;

            ValueRing.Width = diameter;
            ValueRing.Height = diameter;

            ValueText.FontSize = ValueFontSize;
        }
    }
}

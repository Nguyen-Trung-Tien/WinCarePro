using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Modules.GamingTurbo
{
    public sealed partial class GamingTurboPage : Page
    {
        private readonly GamingTurboViewModel _viewModel = new();

        public GamingTurboPage()
        {
            InitializeComponent();
            
            this.Loaded += (s, e) =>
            {
                TranslationManager.Instance.Translate(this);
                try
                {
                    TurboGlowAnimation?.Begin();
                }
                catch { }
            };

            this.Unloaded += (s, e) =>
            {
                try
                {
                    TurboGlowAnimation?.Stop();
                }
                catch { }
            };

            _viewModel.PropertyChanged += (s, e) =>
            {
                StatusMessageText.Text = _viewModel.GameStatusMessage;
                FreedRamText.Text = _viewModel.RamFreedText;
                string processLabel = TranslationManager.Instance.T("Processes");
                ProcCountText.Text = $"{_viewModel.OptimizedProcessesCount} {processLabel}";
                ButtonText.Text = _viewModel.IsTurboActive ? TranslationManager.Instance.T("DISABLE TURBO") : TranslationManager.Instance.T("ENABLE TURBO NOW");
                ActivePresetLabel.Text = _viewModel.ActivePresetName;
                UpdatePresetButtonStyles(_viewModel.ActivePresetName);

                if (_viewModel.IsTurboActive)
                {
                    TurboStatusPillText.Text = "HYPER-TURBO ACTIVE".T();
                    TurboStatusPillText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
                    TurboStatusPill.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 16, 185, 129));
                    TurboStatusPill.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(90, 16, 185, 129));
                    TurboPulseRing.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
                }
                else
                {
                    TurboStatusPillText.Text = "Standby".T();
                    TurboStatusPillText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
                    TurboStatusPill.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(32, 245, 158, 11));
                    TurboStatusPill.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 245, 158, 11));
                    TurboPulseRing.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
                }
            };
        }

        private void UpdatePresetButtonStyles(string activePreset)
        {
            var activeStyle = Application.Current.Resources["ActivePresetChipButtonStyle"] as Style;
            var inactiveStyle = Application.Current.Resources["InactivePresetChipButtonStyle"] as Style;

            if (activeStyle == null || inactiveStyle == null) return;

            if (PresetBtnFps != null) PresetBtnFps.Style = (activePreset == "Competitive FPS") ? activeStyle : inactiveStyle;
            if (PresetBtnRpg != null) PresetBtnRpg.Style = (activePreset == "Open-World RPG") ? activeStyle : inactiveStyle;
            if (PresetBtnRts != null) PresetBtnRts.Style = (activePreset == "Simulators & RTS") ? activeStyle : inactiveStyle;
            if (PresetBtnVr != null) PresetBtnVr.Style = (activePreset == "VR / Max Quality") ? activeStyle : inactiveStyle;
        }

        private async void OnToggleTurboClick(object sender, RoutedEventArgs e)
        {
            if (sender is UIElement elem)
            {
                FluidAnimationHelper.ApplyGlowSparkBurst(elem, 1.08f, 380);
            }
            if (TurboPulseRing != null)
            {
                FluidAnimationHelper.ApplyGlowSparkBurst(TurboPulseRing, 1.12f, 400);
            }

            await _viewModel.ToggleTurboCommand.ExecuteAsync(null);
            
            if (App.MainWindowInstance is MainWindow mw)
            {
                if (_viewModel.IsTurboActive)
                {
                    mw.ShowToastFromDb("Gaming Turbo Activated".T(), 
                        $"System boosted! Reclaimed {_viewModel.RamFreedText} RAM for maximum gaming performance.".T(), "Success");
                }
                else
                {
                    mw.ShowToastFromDb("Gaming Turbo Deactivated".T(), 
                        "System resources restored to standard desktop mode.".T(), "Info");
                }
            }
        }

        private void OnPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string preset)
            {
                FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 250);
                _viewModel.ApplyPreset(preset);
                UpdatePresetButtonStyles(preset);
                
                if (App.MainWindowInstance is MainWindow mw)
                {
                    mw.ShowToastFromDb("Preset Applied".T(), $"Gaming profile switched to '{preset}'.".T(), "Success");
                }
            }
        }
    }
}

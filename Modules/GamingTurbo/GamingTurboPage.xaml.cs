using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Modules.GamingTurbo
{
    public sealed partial class GamingTurboPage : Page
    {
        private readonly GamingTurboViewModel _viewModel = new();

        public GamingTurboPage()
        {
            InitializeComponent();
            _viewModel.PropertyChanged += (s, e) =>
            {
                StatusMessageText.Text = _viewModel.GameStatusMessage;
                FreedRamText.Text = _viewModel.RamFreedText;
                string processLabel = TranslationManager.Instance.T("Processes");
                ProcCountText.Text = $"{_viewModel.OptimizedProcessesCount} {processLabel}";
                ButtonText.Text = _viewModel.IsTurboActive ? TranslationManager.Instance.T("DISABLE TURBO") : TranslationManager.Instance.T("ENABLE TURBO NOW");
            };
        }

        private async void OnToggleTurboClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.ToggleTurboCommand.ExecuteAsync(null);
        }
    }
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
                ProcCountText.Text = $"{_viewModel.OptimizedProcessesCount} Tiến Trình";
                ButtonText.Text = _viewModel.IsTurboActive ? "TẮT TURBO" : "BẬT TURBO NOW";
            };
        }

        private async void OnToggleTurboClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.ToggleTurboCommand.ExecuteAsync(null);
        }
    }
}

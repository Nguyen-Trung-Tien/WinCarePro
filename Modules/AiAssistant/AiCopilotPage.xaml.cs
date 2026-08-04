using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinCarePro.Modules.AiAssistant
{
    public sealed partial class AiCopilotPage : Page
    {
        public AiCopilotPage()
        {
            InitializeComponent();
            RunAiScanAsync();
        }

        private async void RunAiScanAsync()
        {
            try
            {
                var report = await AiHealthEngine.AnalyzeSystemHealthAsync();
                ScoreText.Text = report.OverallScore.ToString();
                StatusTitleText.Text = $"Trạng Thái: {report.HealthStatus}";
                SummaryMessageText.Text = report.SummaryText;
                RecommendationsListView.ItemsSource = report.Recommendations;
            }
            catch { }
        }

        private void OnRunAiScanClick(object sender, RoutedEventArgs e)
        {
            RunAiScanAsync();
        }
    }
}

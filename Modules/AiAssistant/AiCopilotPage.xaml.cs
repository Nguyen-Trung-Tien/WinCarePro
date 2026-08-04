using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Modules.AiAssistant
{
    public sealed partial class AiCopilotPage : Page
    {
        public AiCopilotPage()
        {
            InitializeComponent();
            
            this.Loaded += (s, e) =>
            {
                TranslationManager.Instance.Translate(this);
            };

            TranslationManager.Instance.LanguageChanged += (s, e) =>
            {
                TranslationManager.Instance.Translate(this);
                RunAiScanAsync();
            };

            RunAiScanAsync();
        }

        private async void RunAiScanAsync()
        {
            try
            {
                var report = await AiHealthEngine.AnalyzeSystemHealthAsync();
                ScoreText.Text = report.OverallScore.ToString();
                StatusTitleText.Text = $"{TranslationManager.Instance.T("Status")}: {report.HealthStatus}";
                SummaryMessageText.Text = report.SummaryText;
                PredictiveStorageText.Text = report.PredictiveStorageDaysText;
                PredictiveBootText.Text = report.PredictiveBootTimeSavingsText;
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

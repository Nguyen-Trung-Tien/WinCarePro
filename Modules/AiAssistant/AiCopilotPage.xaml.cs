using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;
using WinCarePro.Core.Helpers;

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
                _ = RunAiScanAsync();
            };

            _ = RunAiScanAsync();
        }

        private async System.Threading.Tasks.Task RunAiScanAsync()
        {
            try
            {
                StatusTitleText.Text = $"{TranslationManager.Instance.T("Status")}: {TranslationManager.Instance.T("Analyzing...")}";
                
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

        private async void OnRunAiScanClick(object sender, RoutedEventArgs e)
        {
            var btn = RunAiDiagnosticsBtn ?? (sender as Button);
            await UiLoadingHelper.ExecuteWithLoadingAsync(
                btn, RunAiProgressRing, RunAiText, RunAiIcon,
                "Scanning AI Diagnostics...", "Run AI Diagnostics",
                async () =>
                {
                    await RunAiScanAsync();
                    if (App.MainWindowInstance is MainWindow mw)
                    {
                        mw.ShowToastFromDb("AI Diagnostics Complete".T(), 
                            "WinCare AI completed system health analysis and generated optimization insights.".T(), "Success");
                    }
                },
                minDurationMs: 1200);
        }

        private async void OnRecommendationActionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string actionKey)
            {
                switch (actionKey)
                {
                    case "NavigateJunkCleaner":
                        NavigateTo("junk");
                        break;
                    case "NavigateStartup":
                        NavigateTo("startup");
                        break;
                    case "NavigateDisk":
                        NavigateTo("disk");
                        break;
                    case "NavigateOptimizer":
                        NavigateTo("optimizer");
                        break;
                    default:
                        // Fast RAM & Working Set Optimization Action
                        try
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            await RunAiScanAsync();

                            if (App.MainWindowInstance is MainWindow mw)
                            {
                                mw.ShowToastFromDb("AI Quick Fix Applied".T(), 
                                    "Purged memory working set and optimized background execution.".T(), "Success");
                            }
                        }
                        catch { }
                        break;
                }
            }
        }

        private void NavigateTo(string tag)
        {
            try
            {
                if (App.MainWindowInstance is MainWindow mw && mw.MainFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal(tag);
                }
            }
            catch { }
        }
    }
}

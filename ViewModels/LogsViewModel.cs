using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Database;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public class LogsViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly HardwareDriverEngine _hardwareEngine = new();
    private readonly AiDiagnosticsEngine _aiEngine = new();

    private string _searchQuery = "";
    private string _selectedModuleFilter = "";
    private bool _isBusy;
    private string _exportFormat = "TXT";

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                RefreshLogs();
            }
        }
    }

    public string SelectedModuleFilter
    {
        get => _selectedModuleFilter;
        set
        {
            if (SetProperty(ref _selectedModuleFilter, value))
            {
                RefreshLogs();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string ExportFormat
    {
        get => _exportFormat;
        set => SetProperty(ref _exportFormat, value);
    }

    public ObservableCollection<LogEntry> Logs { get; } = new();
    public ObservableCollection<ReportEntry> Reports { get; } = new();

    public LogsViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        RefreshAllData();
    }

    public void RefreshAllData()
    {
        RefreshLogs();
        RefreshReports();
    }

    public void RefreshLogs()
    {
        Logs.Clear();
        try
        {
            var dbLogs = DbManager.GetLogs(
                string.IsNullOrEmpty(SelectedModuleFilter) ? null : SelectedModuleFilter,
                string.IsNullOrEmpty(SearchQuery) ? null : SearchQuery
            );
            foreach (var log in dbLogs)
            {
                Logs.Add(log);
            }
        }
        catch { }
    }

    public void RefreshReports()
    {
        Reports.Clear();
        try
        {
            var dbReports = DbManager.GetReports();
            foreach (var r in dbReports)
            {
                Reports.Add(r);
            }
        }
        catch { }
    }

    public async Task GenerateNewReportAsync(DashboardViewModel dashboardVm)
    {
        IsBusy = true;
        try
        {
            var specs = await Task.Run(() => _hardwareEngine.GetHardwareSpecifications());
            
            // Generate a summary of completed actions from database logs to represent in the report
            var dbLogs = DbManager.GetLogs(null, null);
            string mResults = "Operations Summary:\n";
            if (dbLogs.Count > 0)
            {
                mResults += string.Join("\n", dbLogs.Take(10).Select(l => $"- [{l.CreatedAt:yyyy-MM-dd HH:mm}] [{l.Module}] {l.Action} : {l.Status}"));
            }
            else
            {
                mResults += "No operations logged in this database session.";
            }

            // Create evaluation context
            var diagSummary = new AiDiagnosticsEngine.DiagnosticSummary
            {
                HealthScore = dashboardVm.HealthScore,
                Results = dashboardVm.DiagnosticItems.ToList(),
                Recommendations = dashboardVm.Recommendations.ToList()
            };

            string path = await Task.Run(() => _aiEngine.ExportMaintenanceReport(
                ExportFormat,
                specs,
                diagSummary,
                mResults
            ));

            _dispatcherQueue.TryEnqueue(() =>
            {
                RefreshReports();
                DbManager.LogAction($"Generated report {Path.GetFileName(path)}", "Reporting System", "Success");
                RefreshLogs();
            });
        }
        catch { }
        finally
        {
            IsBusy = false;
        }
    }
}

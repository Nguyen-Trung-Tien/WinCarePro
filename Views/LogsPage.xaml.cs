using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }

    public LogsPage()
    {
        InitializeComponent();
        ViewModel = new LogsViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshAllData();
    }

    private void OnModuleFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModuleFilterCombo == null || ViewModel == null) return;
        
        if (ModuleFilterCombo.SelectedItem is ComboBoxItem item)
        {
            string val = item.Content.ToString() ?? "";
            ViewModel.SelectedModuleFilter = val == "All Modules" ? "" : val;
        }
    }

    private void OnFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FormatCombo == null || ViewModel == null) return;
        
        if (FormatCombo.SelectedItem is ComboBoxItem item && item.Tag is string fmt)
        {
            ViewModel.ExportFormat = fmt;
        }
    }

    private async void OnGenerateReportClick(object sender, RoutedEventArgs e)
    {
        // To build a realistic report, let's find the active shell window's main view, or just pass a mock dashboard VM.
        // We instantiate a quick temporary dashboard VM to fetch specs and scores asynchronously
        var tempDashboard = new DashboardViewModel();
        await tempDashboard.RunFullDiagnosticsAsync();
        
        await ViewModel.GenerateNewReportAsync(tempDashboard);
    }

    private void OnOpenReportFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && File.Exists(path))
        {
            try
            {
                // Open directory and select report file
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            catch { }
        }
    }

    internal bool IsNot(bool val) => !val;

    internal static string GetStatusIcon(string status)
    {
        return status == "Success" ? "\uE73E" : "\uE7A6"; // Check or Error glyph
    }

    internal static Brush GetStatusColor(string status)
    {
        var color = status == "Success" ? Colors.LightGreen : Colors.Red;
        return new SolidColorBrush(color);
    }

    internal static string FormatDateTime(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss");
    internal static string FormatDateTimeShort(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm");
}

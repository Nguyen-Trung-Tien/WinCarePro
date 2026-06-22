using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Tests;

public class DbAndReportTests
{
    public DbAndReportTests()
    {
        // Ensure database is initialized before running tests
        DbManager.InitializeDatabase();
    }

    [Fact]
    public void DbManager_InitializeDatabase_CreatesAppFoldersAndDb()
    {
        // Act
        DbManager.InitializeDatabase();

        // Assert
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
        Assert.True(Directory.Exists(appData));
        Assert.True(File.Exists(Path.Combine(appData, "wincaredb.db")));
    }

    [Fact]
    public void DbManager_LogActionAndGetLogs_SavesAndRetrievesLog()
    {
        // Arrange
        string action = $"Test Action {Guid.NewGuid()}";
        string module = "Test Module";
        string status = "Success";

        // Act
        DbManager.LogAction(action, module, status);
        var logs = DbManager.GetLogs(module);

        // Assert
        Assert.NotEmpty(logs);
        Assert.Contains(logs, l => l.Action == action && l.Module == module && l.Status == status);
    }

    [Fact]
    public void DbManager_SaveAndGetReports_SavesAndRetrievesReport()
    {
        // Arrange
        string reportName = $"TestReport_{Guid.NewGuid()}.txt";
        string filePath = @"C:\Temp\" + reportName;

        // Act
        DbManager.SaveReport(reportName, filePath);
        var reports = DbManager.GetReports();

        // Assert
        Assert.NotEmpty(reports);
        Assert.Contains(reports, r => r.ReportName == reportName && r.FilePath == filePath);
    }

    [Fact]
    public void DbManager_SaveAndGetSettings_SavesAndRetrievesSettings()
    {
        // Arrange
        string testSettings = "{\"Theme\":\"Light\",\"AutoScan\":true,\"ReportFormat\":\"JSON\"}";

        // Act
        DbManager.SaveSettings(testSettings);
        var settings = DbManager.GetSettings();

        // Assert
        Assert.Equal(testSettings, settings);
    }

    [Fact]
    public void DbManager_SaveAndGetUpdatedApps_SavesAndRetrievesApps()
    {
        // Arrange
        string appId = $"test-app-{Guid.NewGuid()}";
        string version = "1.2.3.4";

        // Act
        DbManager.SaveUpdatedApp(appId, version);
        var apps = DbManager.GetUpdatedApps();

        // Assert
        Assert.True(apps.ContainsKey(appId));
        Assert.Equal(version, apps[appId]);
    }

    [Fact]
    public async Task AiDiagnosticsEngine_ExportMaintenanceReport_CreatesFilesCorrectly()
    {
        // Arrange
        var engine = new AiDiagnosticsEngine();
        var specs = new HardwareSpecs
        {
            OsVersion = "Windows 11 Test",
            SystemUptime = "1h",
            CpuModel = "Intel Core i7 Test",
            CpuCores = 8,
            CpuThreads = 16,
            RamCapacityGb = 16,
            RamSpeed = "3200 MHz",
            GpuModel = "NVIDIA Test",
            GpuVram = "8 GB",
            StorageInfo = "C: 512GB"
        };
        var summary = await engine.RunHealthEvaluationAsync(
            5000000, 2, 1, 45, 0, 4, new List<string>()
        );
        string maintenanceResults = "Cleaned 5MB junk. Repaired 2 registry issues.";

        // Act
        // 1. Export as JSON
        string jsonReportPath = engine.ExportMaintenanceReport("JSON", specs, summary, maintenanceResults);
        // 2. Export as TXT
        string txtReportPath = engine.ExportMaintenanceReport("TXT", specs, summary, maintenanceResults);

        // Assert
        Assert.True(File.Exists(jsonReportPath), $"JSON report not found at {jsonReportPath}");
        Assert.True(File.Exists(txtReportPath), $"TXT report not found at {txtReportPath}");

        // Clean up
        try
        {
            File.Delete(jsonReportPath);
            File.Delete(txtReportPath);
        }
        catch { }
    }
}

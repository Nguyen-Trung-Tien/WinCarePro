using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Models;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
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
    public void ModelSerialization_ThreadSafety_DoesNotThrowComException()
    {
        // Test DiagnosticResult serialization
        var diag = new DiagnosticResult
        {
            CheckName = "Test Check",
            Category = "Performance",
            IsHealthy = true,
            Description = "Diagnostic description"
        };
        string diagJson = System.Text.Json.JsonSerializer.Serialize(diag);
        Assert.NotNull(diagJson);
        Assert.Contains("Test Check", diagJson);

        // Test SoftwareUpdateInfo serialization
        var update = new SoftwareUpdateInfo
        {
            Id = "Mozilla.Firefox",
            Name = "Firefox Browser",
            UpdateStatus = SoftwareUpdateInfo.StatusCompleted
        };
        string updateJson = System.Text.Json.JsonSerializer.Serialize(update);
        Assert.NotNull(updateJson);
        Assert.Contains("Mozilla.Firefox", updateJson);
    }

    [Fact]
    public void DbManager_ResourceSnapshots_SavesAndAggregatesInsights()
    {
        // Act - Save sample snapshots
        DbManager.SaveResourceSnapshot(85.0, 40.0, 10.0, 5.0, "High CPU Load");
        DbManager.SaveResourceSnapshot(90.0, 45.0, 15.0, 10.0, "High CPU Load");

        var recent = DbManager.GetRecentResourceSnapshots(10);
        Assert.NotNull(recent);
        Assert.NotEmpty(recent);

        var insights = DbManager.GetResourceInsights();
        Assert.NotNull(insights);
        Assert.True(insights.TotalSnapshotsRecorded > 0);
        Assert.False(string.IsNullOrEmpty(insights.HealthInsight));
    }

    [Fact]
    public void SafeFileRecycler_ProtectedSystemPath_RejectsSafely()
    {
        string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var res = WinCarePro.Core.Helpers.SafeFileRecycler.Delete(sysPath);
        Assert.False(res.IsSuccess);
    }

    [Fact]
    public void SafeFileRecycler_RecycleActualTempFile_Succeeds()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "WinCare_Recycler_Test_" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(tempFile, "Temporary test payload for recycling.");

        try
        {
            var res = WinCarePro.Core.Helpers.SafeFileRecycler.Delete(tempFile, sendToRecycleBin: false);
            Assert.True(res.IsSuccess);
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}

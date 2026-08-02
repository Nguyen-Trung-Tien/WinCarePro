using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Database;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class DbManagerRegressionTests
{
    public DbManagerRegressionTests()
    {
        DbManager.InitializeDatabase();
    }

    [Fact]
    public void DbManager_GetLogs_HandlesFormattedStringDatesWithoutThrowing()
    {
        // Arrange
        string uniqueAction = $"StringDateTest_{Guid.NewGuid()}";
        DbManager.LogAction(uniqueAction, "TestModule", "Success");

        // Act
        var logs = DbManager.GetLogs("TestModule", uniqueAction);

        // Assert
        Assert.NotEmpty(logs);
        var entry = Assert.Single(logs);
        Assert.Equal(uniqueAction, entry.Action);
        Assert.True(entry.CreatedAt <= DateTime.Now.AddMinutes(5));
    }

    [Fact]
    public void DbManager_GetRecentNotifications_HandlesDatesSafely()
    {
        // Arrange
        string title = $"Notif_{Guid.NewGuid()}";
        DbManager.AddNotification(title, "Test body message", "Info", showToast: false);

        // Act
        var notifications = DbManager.GetRecentNotifications(10);

        // Assert
        Assert.NotEmpty(notifications);
        Assert.Contains(notifications, n => n.Title == title);
    }

    [Fact]
    public async Task ProcessService_GetRunningProcessesAsync_RunsConcurrentlyWithoutExceptions()
    {
        // Arrange
        var service = new ProcessService();

        // Act: Run multiple concurrent requests to test thread-safety of ConcurrentDictionary
        var task1 = service.GetRunningProcessesAsync();
        var task2 = service.GetRunningProcessesAsync();
        var task3 = service.GetRunningProcessesAsync();

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
        Assert.NotNull(results[2]);
        Assert.NotEmpty(results[0]);
    }

    [Fact]
    public void JunkCleanerEngine_GetDirectoryDetails_HonorsCancellationToken()
    {
        // Arrange
        var engine = new JunkCleanerEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled token

        var method = typeof(JunkCleanerEngine).GetMethod("GetDirectoryDetails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        string tempDir = Path.GetTempPath();

        // Act & Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            method.Invoke(engine, new object[] { tempDir, "*", true, cts.Token, 5000 });
        });

        Assert.IsType<OperationCanceledException>(ex.InnerException);
    }
}

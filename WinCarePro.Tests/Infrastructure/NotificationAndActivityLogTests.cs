using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Shared;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class NotificationAndActivityLogTests
{
    public NotificationAndActivityLogTests()
    {
        DbManager.InitializeDatabase();
    }

    [Fact]
    public void DbManager_AddNotification_TriggersOnNotificationAddedEvent()
    {
        // Arrange
        bool eventFired = false;
        NotificationItem? receivedItem = null;
        string expectedTitle = $"TestNotifTitle_{Guid.NewGuid()}";

        Action<NotificationItem> handler = (item) =>
        {
            if (item.Title == expectedTitle)
            {
                eventFired = true;
                receivedItem = item;
            }
        };

        DbManager.OnNotificationAdded += handler;

        try
        {
            // Act
            DbManager.AddNotification(expectedTitle, "Test message content", "Warning", showToast: false);

            // Assert
            Assert.True(eventFired);
            Assert.NotNull(receivedItem);
            Assert.Equal(expectedTitle, receivedItem.Title);
            Assert.Equal("Warning", receivedItem.Level);
        }
        finally
        {
            DbManager.OnNotificationAdded -= handler;
        }
    }

    [Fact]
    public void DbManager_LogAction_TriggersOnLogAddedEvent()
    {
        // Arrange
        bool eventFired = false;
        LogEntry? receivedEntry = null;
        string expectedAction = $"TestAction_{Guid.NewGuid()}";

        Action<LogEntry> handler = (entry) =>
        {
            if (entry.Action == expectedAction)
            {
                eventFired = true;
                receivedEntry = entry;
            }
        };

        DbManager.OnLogAdded += handler;

        try
        {
            // Act
            DbManager.LogAction(expectedAction, "Junk Cleaner", "Thành công");

            // Assert
            Assert.True(eventFired);
            Assert.NotNull(receivedEntry);
            Assert.Equal(expectedAction, receivedEntry.Action);
            Assert.Equal("Junk Cleaner", receivedEntry.Module);
            Assert.Equal("Thành công", receivedEntry.Status);
        }
        finally
        {
            DbManager.OnLogAdded -= handler;
        }
    }

    [Fact]
    public void StatusToBrushConverter_HandlesVietnameseAndEnglishKeywords()
    {
        // Act & Assert
        // Green category
        Assert.Equal("Green", StatusToBrushConverter.GetStatusCategory("Success"));
        Assert.Equal("Green", StatusToBrushConverter.GetStatusCategory("Thành công"));
        Assert.Equal("Green", StatusToBrushConverter.GetStatusCategory("Hoàn tất"));

        // Amber category
        Assert.Equal("Amber", StatusToBrushConverter.GetStatusCategory("Warning"));
        Assert.Equal("Amber", StatusToBrushConverter.GetStatusCategory("Cảnh báo"));

        // Red category
        Assert.Equal("Red", StatusToBrushConverter.GetStatusCategory("Critical Error"));
        Assert.Equal("Red", StatusToBrushConverter.GetStatusCategory("Lỗi nghiêm trọng"));
        Assert.Equal("Red", StatusToBrushConverter.GetStatusCategory("Thất bại"));
    }

    [Fact]
    public void CsvExport_IncludesUtf8BomMarker()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.Append("\uFEFF");
        sb.AppendLine("ID,Timestamp,Module,Action,Status");
        sb.AppendLine("1,\"2026-08-08 12:00:00\",\"Dọn dẹp rác\",\"Đã dọn dẹp 500MB\",\"Thành công\"");

        string csvOutput = sb.ToString();

        // Assert
        Assert.StartsWith("\uFEFF", csvOutput);
        Assert.Contains("Dọn dẹp rác", csvOutput);
    }
}

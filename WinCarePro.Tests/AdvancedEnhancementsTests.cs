using System;
using System.IO;
using System.Threading.Tasks;
using WinCarePro.Database;
using WinCarePro.Engines;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests;

public class AdvancedEnhancementsTests
{
    [Fact]
    public void DPAPI_EncryptAndDecrypt_ReturnsOriginalString()
    {
        // Arrange
        string originalText = "WinCarePro_Secret_Key_12345";

        // Act
        string encrypted = DbManager.EncryptProtectedData(originalText);
        string decrypted = DbManager.DecryptProtectedData(encrypted);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEqual(originalText, encrypted);
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void UndoManager_SaveAndGetSnapshots_ReturnsSavedEntries()
    {
        // Arrange
        DbManager.InitializeDatabase();
        var undoManager = new UndoManagerService();
        string cat = "TestRegistryCategory";
        string key = "HKCU\\Software\\WinCareTestKey\\TestValue";

        // Act
        undoManager.RecordRegistrySnapshot(cat, "HKCU\\Software\\WinCareTestKey", "TestValue", "0", "1");
        var snapshots = undoManager.GetRecentSnapshots(cat);

        // Assert
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);
        Assert.Contains(snapshots, s => s.KeyName == key);
    }

    [Fact]
    public async Task JunkCleaner_ParallelGetDirectoryDetails_ScansSuccessfully()
    {
        // Arrange
        var engine = new JunkCleanerEngine();
        string tempPath = Path.GetTempPath();

        // Act
        var result = await engine.ScanJunkAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}

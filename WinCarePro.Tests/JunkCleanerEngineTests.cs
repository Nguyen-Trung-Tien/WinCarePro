using System;
using System.IO;
using System.Reflection;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class JunkCleanerEngineTests
{
    [Fact]
    public void IsFileLocked_ReadOnlyFile_ReturnsFalse()
    {
        // Arrange
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test data");
            File.SetAttributes(tempFile, FileAttributes.ReadOnly);

            // Act
            var method = typeof(JunkCleanerEngine).GetMethod("IsFileLocked", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            
            var isLocked = method.Invoke(null, new object[] { tempFile });
            Assert.NotNull(isLocked);

            // Assert
            Assert.False((bool)isLocked);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.SetAttributes(tempFile, FileAttributes.Normal);
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void JunkType_ContainsDeveloperCache()
    {
        Assert.True(Enum.IsDefined(typeof(WinCarePro.Models.JunkType), WinCarePro.Models.JunkType.DeveloperCache));
    }

    [Fact]
    public void JunkCategory_FormatProperties_ReturnAccurateStrings()
    {
        var category = new WinCarePro.Models.JunkCategory
        {
            Name = "Developer & IDE Caches",
            Type = WinCarePro.Models.JunkType.DeveloperCache,
            SizeBytes = 10485760, // 10 MB
            CleanableBytes = 10485760,
            LockedBytes = 0,
            FileCount = 42
        };

        Assert.Equal("42 files", category.FileCountFormatted);
        Assert.Contains("10", category.SizeFormatted);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, category.LockedSizeVisibility);
    }
}

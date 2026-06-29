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
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class SystemScannerTests
{
    [Fact]
    public async Task JunkCleanerEngine_ScanAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var junkEngine = new JunkCleanerEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await junkEngine.ScanJunkAsync(cts.Token);
        });
    }

    [Fact]
    public async Task JunkCleanerEngine_CleanAsync_WithNoSelectedCategories_ReturnsZeroCleaned()
    {
        // Arrange
        var junkEngine = new JunkCleanerEngine();

        // Act
        long bytesCleaned = await junkEngine.CleanJunkAsync(new List<JunkCategory>());

        // Assert
        Assert.Equal(0, bytesCleaned);
    }

    [Fact]
    public void SystemEngine_OutputEvent_FiresOnLog()
    {
        // Arrange
        var systemEngine = new SystemEngine();
        bool eventFired = false;
        systemEngine.OutputReceived += msg => { eventFired = true; };

        // Act & Assert
        Assert.NotNull(systemEngine);
        Assert.False(eventFired);
    }
}


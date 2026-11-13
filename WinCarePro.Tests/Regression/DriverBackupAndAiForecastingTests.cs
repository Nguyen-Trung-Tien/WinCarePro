using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class DriverBackupAndAiForecastingTests
{
    [Fact]
    public void HardwareDriverEngine_CanInstantiateAndQueryDriversSafely()
    {
        var engine = new HardwareDriverEngine();
        var drivers = engine.GetInstalledDrivers();
        Assert.NotNull(drivers);
    }

    [Fact]
    public async Task HardwareDriverEngine_RestoreNonExistentDirectory_FailsSafely()
    {
        var engine = new HardwareDriverEngine();
        string fakeDir = Path.Combine(Path.GetTempPath(), "NonExistentDriverBackupFolder_" + Guid.NewGuid());

        var result = await engine.RestoreDriversFromBackupAsync(fakeDir);
        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}

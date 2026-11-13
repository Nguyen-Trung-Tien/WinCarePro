using System;
using System.IO;
using System.Linq;
using WinCarePro.Core.Interop;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Services;
using Xunit;

namespace WinCarePro.Tests;

public class PolarisMajorUpgradeTests
{
    [Fact]
    public void NativeApi_GetSystemPowerStatus_ExecutesSafely()
    {
        bool success = NativeApi.GetSystemPowerStatus(out var status);
        if (success)
        {
            // ACLineStatus: 0 = Offline, 1 = Online, 255 = Unknown
            Assert.True(status.ACLineStatus == 0 || status.ACLineStatus == 1 || status.ACLineStatus == 255);
            // BatteryLifePercent: 0..100 or 255
            Assert.True(status.BatteryLifePercent <= 100 || status.BatteryLifePercent == 255);
        }
    }

    [Fact]
    public void TranslationManager_PolarisNewKeys_AreProperlyMapped()
    {
        var tm = TranslationManager.Instance;

        string viVolumes = tm.GetTranslationForLanguage("Logical Volumes", AppLanguage.Vietnamese);
        Assert.Equal("Phân vùng ổ đĩa", viVolumes);

        string viAnalyze = tm.GetTranslationForLanguage("Analyze Space", AppLanguage.Vietnamese);
        Assert.Equal("Phân tích dung lượng", viAnalyze);
    }
}

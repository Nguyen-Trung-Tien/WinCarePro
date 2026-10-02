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
    public void SecurityPrivacyEngine_KernelIsolationAndDefenses_ReturnValidStatus()
    {
        var engine = new SecurityPrivacyEngine();

        var (hvciOk, hvciStatus) = engine.CheckCoreIsolationStatus();
        Assert.NotNull(hvciStatus);
        Assert.Contains("Memory Integrity", hvciStatus);

        var (tamperOk, tamperStatus) = engine.CheckTamperProtectionStatus();
        Assert.NotNull(tamperStatus);
        Assert.Contains("Tamper Protection", tamperStatus);

        var (rdpOk, rdpStatus) = engine.CheckRdpNlaStatus();
        Assert.NotNull(rdpStatus);
        Assert.True(rdpStatus.Contains("RDP") || rdpStatus.Contains("Remote Desktop"));
    }

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

        string viHvci = tm.GetTranslationForLanguage("Memory Integrity (HVCI)", AppLanguage.Vietnamese);
        Assert.Equal("Toàn vẹn bộ nhớ (HVCI)", viHvci);

        string viTamper = tm.GetTranslationForLanguage("Tamper Protection", AppLanguage.Vietnamese);
        Assert.Equal("Bảo vệ chống can thiệp", viTamper);

        string viAnalyze = tm.GetTranslationForLanguage("Analyze Space", AppLanguage.Vietnamese);
        Assert.Equal("Phân tích dung lượng", viAnalyze);
    }
}

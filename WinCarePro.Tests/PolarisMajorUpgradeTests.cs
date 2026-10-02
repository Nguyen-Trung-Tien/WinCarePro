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
    public void DiskEngine_GetLogicalVolumes_ReturnsValidDrives()
    {
        var engine = new DiskEngine();
        var volumes = engine.GetLogicalVolumes();
        Assert.NotNull(volumes);
        Assert.NotEmpty(volumes);

        var systemDrive = volumes.FirstOrDefault(v => v.DriveLetter.StartsWith("C", StringComparison.OrdinalIgnoreCase));
        if (systemDrive != null)
        {
            Assert.True(systemDrive.TotalBytes > 0);
            Assert.True(systemDrive.PercentUsed >= 0.0 && systemDrive.PercentUsed <= 100.0);
            Assert.False(string.IsNullOrEmpty(systemDrive.StatusBadge));
        }
    }

    [Theory]
    [InlineData(100L * 1024 * 1024 * 1024, 3L * 1024 * 1024 * 1024, true, 0)] // 3 GB free (<= 5 GB) => Critical (0)
    [InlineData(100L * 1024 * 1024 * 1024, 10L * 1024 * 1024 * 1024, true, 6)] // 10 GB free (< 15%), burn 0.85 GB/day => (10-5)/0.85 = 5.88 -> 6 days
    [InlineData(100L * 1024 * 1024 * 1024, 20L * 1024 * 1024 * 1024, false, 45)] // 20 GB free (< 25%) => Moderate (45 days)
    [InlineData(100L * 1024 * 1024 * 1024, 50L * 1024 * 1024 * 1024, false, -1)] // 50 GB free (50%) => Safe (-1)
    public void DiskEngine_CalculateVolumeExhaustion_ComputesCorrectDays(long totalBytes, long freeBytes, bool isSystem, int expectedDays)
    {
        var (days, forecast) = DiskEngine.CalculateVolumeExhaustion(totalBytes, freeBytes, isSystem);
        Assert.Equal(expectedDays, days);
        Assert.False(string.IsNullOrWhiteSpace(forecast));
    }

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

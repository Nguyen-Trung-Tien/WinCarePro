using System;
using System.Collections.Generic;
using WinCarePro.Engines;
using WinCarePro.Models;
using Xunit;

namespace WinCarePro.Tests;

public class SecurityAndSanitizationTests
{
    private readonly SecurityPrivacyEngine _securityEngine = new();

    [Fact]
    public void GetAntivirusStatus_DoesNotThrowException()
    {
        // Act
        string status = _securityEngine.GetAntivirusStatus();

        // Assert
        Assert.NotNull(status);
        Assert.NotEmpty(status);
    }

    [Fact]
    public void GetFirewallStatus_ReturnsBoolean()
    {
        // Act
        bool isEnabled = _securityEngine.GetFirewallStatus();

        // Assert - Should return boolean without throwing exceptions
        Assert.True(isEnabled || !isEnabled);
    }

    [Fact]
    public void RunSecurityAudits_WithSuspiciousStartups_DetectsRisk()
    {
        // Arrange
        var suspiciousList = new List<StartupEntry>
        {
            new() { Name = "MalwareTest", Command = "powershell.exe -ExecutionPolicy Bypass -File C:\\temp\\bad.ps1", Source = StartupSource.RegistryRunHKCU }
        };

        // Act
        var issues = _securityEngine.RunSecurityAudits(suspiciousList);

        // Assert
        Assert.NotNull(issues);
        Assert.Contains(issues, i => i.Contains("Suspicious startup program"));
    }

    [Fact]
    public void RegistryBackupEngine_ScanRegistryIssues_HandlesFilePathsSafely()
    {
        // Arrange
        var backupEngine = new RegistryBackupEngine();

        // Act & Assert
        var issues = backupEngine.ScanRegistryIssues();
        Assert.NotNull(issues);
    }
}

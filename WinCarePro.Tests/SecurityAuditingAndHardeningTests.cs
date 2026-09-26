using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using WinCarePro.Core.Helpers;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Infrastructure.Security;
using WinCarePro.Services.Implementations;

namespace WinCarePro.Tests;

public class SecurityAuditingAndHardeningTests
{
    [Fact]
    public void SafePathGuard_ProtectsWindowsDirectoryAndExecutables()
    {
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string explorerExe = Path.Combine(winDir, "explorer.exe");
        string regeditExe = Path.Combine(winDir, "regedit.exe");
        string fontsFolder = Path.Combine(winDir, "Fonts");
        string winTempRoot = Path.Combine(winDir, "Temp");

        Assert.False(SafePathGuard.IsPathSafeForDeletion(explorerExe));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(regeditExe));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(fontsFolder));
        Assert.False(SafePathGuard.IsPathSafeForDeletion(winTempRoot)); // Root maintenance container must not be wiped
    }

    [Fact]
    public void SafePathGuard_ProtectsUserPersonalDirectories()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrEmpty(desktop))
        {
            string desktopFile = Path.Combine(desktop, "Confidential_Work.docx");
            Assert.False(SafePathGuard.IsPathSafeForDeletion(desktopFile));
        }

        if (!string.IsNullOrEmpty(documents))
        {
            string docsFile = Path.Combine(documents, "TaxReport.pdf");
            Assert.False(SafePathGuard.IsPathSafeForDeletion(docsFile));
        }
    }

    [Fact]
    public void SafeRegistryGuard_RejectsBarePathsWithoutValidHive()
    {
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"SOFTWARE\Microsoft\Windows"));
        Assert.False(SafeRegistryGuard.IsSafeToModifyKey(@"SYSTEM\CurrentControlSet\Services"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteValue(@"SOFTWARE\Microsoft", "TestValue"));
    }

    [Fact]
    public void SafeRegistryGuard_ProtectsHKCUPoliciesAndWinlogon()
    {
        Assert.False(SafeRegistryGuard.IsSafeToModifyKey(@"HKCU\Software\Policies"));
        Assert.False(SafeRegistryGuard.IsSafeToModifyKey(@"HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKCU\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"));
        Assert.False(SafeRegistryGuard.IsSafeToModifyKey(@"HKLM\SOFTWARE\Policies"));
    }

    [Fact]
    public void InputSanitizer_RejectsUntrustedPathsDisguisedAsSystemTools()
    {
        Assert.False(InputSanitizer.IsSafeUri(@"..\malicious\regedit.exe"));
        Assert.False(InputSanitizer.IsSafeUri(@"C:\Temp\regedit.exe"));
        Assert.True(InputSanitizer.IsSafeUri("regedit.exe"));
        Assert.True(InputSanitizer.IsSafeUri("https://microsoft.com/download"));
    }

    [Fact]
    public void ProcessRunner_ResolveSafeExecutablePath_ResolvesSystem32AndPowerShell()
    {
        string netstatPath = ProcessRunner.ResolveSafeExecutablePath("netstat.exe");
        Assert.Contains("System32", netstatPath, StringComparison.OrdinalIgnoreCase);

        string chkdskPath = ProcessRunner.ResolveSafeExecutablePath("chkdsk.exe");
        Assert.Contains("System32", chkdskPath, StringComparison.OrdinalIgnoreCase);

        string psPath = ProcessRunner.ResolveSafeExecutablePath("powershell.exe");
        Assert.Contains("WindowsPowerShell", psPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessService_IsActionAllowed_ProtectsSystemProcessesWithExeExtension()
    {
        var service = new ProcessService();

        Assert.False(service.IsActionAllowed("csrss.exe", 100, out string reason1));
        Assert.Contains("critical", reason1, StringComparison.OrdinalIgnoreCase);

        Assert.False(service.IsActionAllowed("svchost.exe", 200, out _));
        Assert.False(service.IsActionAllowed("lsass.exe", 300, out _));
        Assert.True(service.IsActionAllowed("notepad.exe", 1234, out _));
    }

    [Fact]
    public void ServiceSafetyService_IsProtectedService_CoversCriticalAndSecurityServices()
    {
        var safety = new ServiceSafetyService();

        Assert.True(safety.IsProtectedService("RpcSs"));
        Assert.True(safety.IsProtectedService("WinDefend"));
        Assert.True(safety.IsProtectedService("mpssvc"));
        Assert.True(safety.IsProtectedService("CryptSvc"));
        Assert.False(safety.IsProtectedService("RandomThirdPartyService"));
    }

    [Fact]
    public async Task DiskEngine_RunChkdskAsync_RejectsInvalidDriveLetters()
    {
        var engine = new DiskEngine();

        Assert.False(await engine.RunChkdskAsync("C; format D:"));
        Assert.False(await engine.RunChkdskAsync(""));
        Assert.False(await engine.RunChkdskAsync("12"));
        Assert.False(await engine.RunChkdskAsync("XYZ"));
    }

    [Fact]
    public void CrashLogger_SanitizesJsonPasswordsAndJwtTokens()
    {
        string rawLog = "{\"password\": \"TopSecret123\", \"jwt\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.doNotLeakThisSignature\"}";
        string sanitized = CrashLogger.Sanitize(rawLog);

        Assert.DoesNotContain("TopSecret123", sanitized);
        Assert.DoesNotContain("doNotLeakThisSignature", sanitized);
        Assert.Contains("***REDACTED***", sanitized);
    }

    [Fact]
    public async Task NetworkEngine_SetDohSettingsAsync_RejectsInvalidOrMaliciousInputs()
    {
        var netEngine = new NetworkEngine();

        bool r1 = await netEngine.SetDohSettingsAsync(true, "invalid_ip", "1.1.1.1", "https://dns.quad9.net/dns-query");
        Assert.False(r1);

        bool r2 = await netEngine.SetDohSettingsAsync(true, "1.1.1.1", "1.0.0.1", "https://dns.com/query; rm -rf /");
        Assert.False(r2);
    }
}

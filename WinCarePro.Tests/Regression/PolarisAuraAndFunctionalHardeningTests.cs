using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Core;
using WinCarePro.Core.Helpers;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Security;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Services.Implementations;
using Xunit;

namespace WinCarePro.Tests.Regression;

public class PolarisAuraAndFunctionalHardeningTests
{
    [Fact]
    public void ServiceSafetyService_ProtectsWindowsCoreServices()
    {
        var safety = new ServiceSafetyService();

        // Core OS infrastructure services must be protected from being stopped or disabled
        Assert.True(safety.IsProtectedService("RpcSs"));
        Assert.True(safety.IsProtectedService("DcomLaunch"));
        Assert.True(safety.IsProtectedService("WinDefend"));
        Assert.True(safety.IsProtectedService("CryptSvc"));
        Assert.True(safety.IsProtectedService("mpssvc"));

        // Case insensitivity
        Assert.True(safety.IsProtectedService("rpcss"));
        Assert.True(safety.IsProtectedService("windefend"));

        // Third-party non-core services should not be falsely blocked
        Assert.False(safety.IsProtectedService("SomeRandomCustomAppService123"));
    }

    [Fact]
    public void SettingsProfile_SensitiveCredentials_EncryptedViaDPAPI()
    {
        var profile = new SettingsProfile();
        string plainSecret = "Polaris_Secure_Token_987654321_!@#$";

        profile.SetSensitiveCredential("ApiKey", plainSecret);
        profile.SetSensitiveCredential("CustomToken", "custom_val_456");

        // Stored strings must not be plain text
        Assert.NotEmpty(profile.EncryptedApiKey);
        Assert.NotEqual(plainSecret, profile.EncryptedApiKey);

        // Decryption round-trip must restore the exact secret
        string restoredSecret = profile.GetSensitiveCredential("ApiKey");
        Assert.Equal(plainSecret, restoredSecret);

        string restoredToken = profile.GetSensitiveCredential("CustomToken");
        Assert.Equal("custom_val_456", restoredToken);

        // Non-existent key returns empty string without crash
        Assert.Equal("", profile.GetSensitiveCredential("NonExistentKey"));
    }

    [Fact]
    public async Task JunkCleanerEngine_Cancellation_ThrowsOperationCanceledException()
    {
        var engine = new JunkCleanerEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        // Expect immediate OperationCanceledException upon cancelled token
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await engine.ScanJunkAsync(cts.Token);
        });
    }

    [Fact]
    public void TranslationManager_DynamicLanguageSwitching_PreservesKeys()
    {
        var tm = TranslationManager.Instance;

        // Switch to Vietnamese
        tm.CurrentLanguage = AppLanguage.Vietnamese;
        Assert.Equal(AppLanguage.Vietnamese, tm.CurrentLanguage);
        string viClean = tm.GetString("Clean Now");
        Assert.False(string.IsNullOrWhiteSpace(viClean));

        // Switch to English
        tm.CurrentLanguage = AppLanguage.English;
        Assert.Equal(AppLanguage.English, tm.CurrentLanguage);
        string enClean = tm.GetString("Clean Now");
        Assert.Equal("Clean Now", enClean);
    }

    [Fact]
    public void SafeRegistryGuard_RejectsDangerousHivesAndRootKeys()
    {
        // Must reject null, empty, or root hives
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(null!));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(""));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey("HKEY_LOCAL_MACHINE"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKLM\SYSTEM"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKLM\SYSTEM\CurrentControlSet"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKLM\SAM"));
        Assert.False(SafeRegistryGuard.IsSafeToDeleteKey(@"HKLM\SECURITY"));
    }
}


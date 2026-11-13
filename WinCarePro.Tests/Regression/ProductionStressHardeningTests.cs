using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Services;
using WinCarePro.ViewModels;
using Xunit;

namespace WinCarePro.Tests;

[Collection("Database Tests")]
public class ProductionStressHardeningTests
{
    [Fact]
    public async Task Stress_Scan_Cancel_StartAgain_Cycle_AllViewModels()
    {
        // 1. UninstallViewModel
        var uninstVm = new UninstallViewModel();
        for (int i = 0; i < 3; i++)
        {
            var task = uninstVm.ScanAppsAsync();
            uninstVm.CancelScan();
            await Task.Yield();
        }
        uninstVm.CancelScan();
        Assert.False(uninstVm.IsBusy);
        Assert.Equal(OperationState.Idle, uninstVm.CurrentOperationState);

        // 2. CleanerViewModel
        var junkEngine = new JunkCleanerEngine();
        var regEngine = new RegistryBackupEngine();
        var cleanerVm = new CleanerViewModel(junkEngine, regEngine);
        cleanerVm.CancelCurrentTask();
        Assert.False(cleanerVm.IsBusy);
    }

    [Fact]
    public async Task Stress_Scan_ThemeChange_LanguageChange_Cancel()
    {
        var vm = new UninstallViewModel();
        var origTheme = ThemeManager.Instance.CurrentTheme;
        var origLang = TranslationManager.Instance.CurrentLanguage;

        try
        {
            // Start scan
            var scanTask = vm.ScanAppsAsync();

            // Runtime Theme Switch
            ThemeManager.Instance.ApplyTheme(origTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark);

            // Runtime Language Switch
            TranslationManager.Instance.CurrentLanguage = (origLang == AppLanguage.Vietnamese) ? AppLanguage.English : AppLanguage.Vietnamese;

            // Cancel mid-operation
            vm.CancelScan();
            await Task.Yield();

            Assert.False(vm.IsBusy);
            Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
        }
        finally
        {
            ThemeManager.Instance.ApplyTheme(origTheme);
            TranslationManager.Instance.CurrentLanguage = origLang;
            vm.Dispose();
        }
    }

    [Fact]
    public async Task Stress_MultipleRapidClicks_DuplicateOperationPrevention()
    {
        var vm = new UninstallViewModel();

        // Simulate 10 rapid concurrent clicks
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = vm.ScanAppsAsync();
        }

        // Wait for all clicks to register and at least one operation to commence/finish
        await Task.WhenAll(tasks);
        
        var timeout = 5000;
        while (vm.IsBusy && timeout > 0)
        {
            await Task.Delay(100);
            timeout -= 100;
        }

        // State machine must be valid (either completed or idle, never active flag-locked)
        Assert.False(vm.IsBusy);
        Assert.True(vm.CurrentOperationState == OperationState.Completed || vm.CurrentOperationState == OperationState.Idle);
    }

    [Fact]
    public async Task Stress_StartOperation_Dispose_NoUnhandledExceptions()
    {
        // Start an operation and instantly dispose the ViewModel
        var vm = new UninstallViewModel();
        var scanTask = vm.ScanAppsAsync();

        vm.Dispose();
        await Task.Yield();

        Assert.False(vm.IsBusy);
        Assert.Equal(OperationState.Idle, vm.CurrentOperationState);
    }

    [Fact]
    public void Stress_HighFrequency_MonitorStartStop_NoDeadlocks()
    {
        var vm = new DashboardViewModel();

        // Rapid start and stop monitoring
        for (int i = 0; i < 5; i++)
        {
            vm.StartMonitoring();
            vm.StopMonitoring();
        }

        vm.Dispose();
    }

    [Fact]
    public void Dashboard_ScanModes_StateLifecycle_And_CancellationTokenHandling()
    {
        var vm = new DashboardViewModel();
        try
        {
            Assert.False(vm.IsScanPanelOpen);
            Assert.False(vm.IsModeScanning);
            Assert.False(vm.IsModeFixing);

            // Open Scan Panel for DiskAndCache
            vm.OpenScanPanel("DiskAndCache");
            Assert.True(vm.IsScanPanelOpen);
            Assert.Equal("cleaner", vm.ModeDeepNavTarget);

            // Open Scan Panel for StartupAndPerformance
            vm.OpenScanPanel("StartupAndPerformance");
            Assert.True(vm.IsScanPanelOpen);
            Assert.Equal("startup", vm.ModeDeepNavTarget);

            // Open Scan Panel for SecurityCheck
            vm.OpenScanPanel("SecurityCheck");
            Assert.True(vm.IsScanPanelOpen);
            Assert.Equal("security", vm.ModeDeepNavTarget);

            // Open Scan Panel for RegistryAndDiskHealth
            vm.OpenScanPanel("RegistryAndDiskHealth");
            Assert.True(vm.IsScanPanelOpen);
            Assert.Equal("registry", vm.ModeDeepNavTarget);

            // Close panel
            vm.CloseScanPanel();
            Assert.False(vm.IsScanPanelOpen);
            Assert.False(vm.IsModeScanning);
        }
        finally
        {
            vm.Dispose();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCarePro.Core.Models;
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
        // 1. ContextMenuViewModel
        var contextVm = new ContextMenuViewModel();
        for (int i = 0; i < 3; i++)
        {
            var task = contextVm.ScanAsync();
            contextVm.CancelScan();
            await Task.Yield();
        }
        contextVm.CancelScan();
        Assert.False(contextVm.IsBusy);
        Assert.Equal(OperationState.Idle, contextVm.CurrentOperationState);

        // 2. RegistryViewModel
        var regVm = new RegistryViewModel();
        for (int i = 0; i < 3; i++)
        {
            var task = regVm.ScanRegistryAsync();
            regVm.CancelScan();
            await Task.Yield();
        }
        regVm.CancelScan();
        Assert.False(regVm.IsBusy);
        Assert.Equal(OperationState.Idle, regVm.CurrentOperationState);

        // 3. UninstallViewModel
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

        // 4. SystemOptimizerViewModel
        var optVm = new SystemOptimizerViewModel();
        for (int i = 0; i < 3; i++)
        {
            var task = optVm.RunAiScanAsync();
            optVm.CancelAiScan();
            await Task.Yield();
        }
        optVm.CancelAiScan();
        Assert.False(optVm.IsAiScanning);
        Assert.Equal(OperationState.Idle, optVm.CurrentOperationState);
    }

    [Fact]
    public async Task Stress_Scan_ThemeChange_LanguageChange_Cancel()
    {
        var vm = new ContextMenuViewModel();
        var origTheme = ThemeManager.Instance.CurrentTheme;
        var origLang = TranslationManager.Instance.CurrentLanguage;

        try
        {
            // Start scan
            var scanTask = vm.ScanAsync();

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
        var vm = new RegistryViewModel();

        // Simulate 10 rapid concurrent clicks
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = vm.ScanRegistryAsync();
        }

        // Wait for all clicks to register and at least one operation to commence/finish
        await Task.WhenAll(tasks);

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
}

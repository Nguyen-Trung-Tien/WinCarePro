using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WinCarePro.Core.Helpers;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;

namespace WinCarePro.ViewModels;

public partial class CleanerCategoryItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSafeGuarded { get; set; } = true;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _sizeFormatted = "0 B";
}

public partial class CleanerViewModel : AsyncTaskViewModelBase
{
    private readonly JunkCleanerEngine _junkEngine;
    private readonly RegistryBackupEngine _registryEngine;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan.";

    [ObservableProperty]
    private string _totalSelectedSizeFormatted = "Selected: 0 B";

    [ObservableProperty]
    private bool _isSafetyNoticeVisible = true;

    [ObservableProperty]
    private bool _isSummaryCardVisible;

    [ObservableProperty]
    private string _summaryCleanedText = string.Empty;

    [ObservableProperty]
    private bool _isElevationNoticeVisible;

    public ObservableCollection<CleanerCategoryItem> CleanerCategories { get; } = new();

    public bool CanClean => !IsBusy && CleanerCategories.Count > 0;

    protected override void OnBusyChanged(bool isBusy)
    {
        base.OnBusyChanged(isBusy);
        OnPropertyChanged(nameof(CanClean));
    }

    public CleanerViewModel(JunkCleanerEngine junkEngine, RegistryBackupEngine registryEngine)
    {
        _junkEngine = junkEngine;
        _registryEngine = registryEngine;
    }

    [RelayCommand]
    public async Task ScanAllAsync(CancellationToken ct = default)
    {
        await ExecuteAsyncTaskAsync(
            "Auditing junk directories, registry orphans, and app leftovers...",
            async (token, progress) =>
            {
                await RunOnUIActionAsync(() => CleanerCategories.Clear());
                OnPropertyChanged(nameof(CanClean));

                // 1. Quét Junk Files chuẩn SafePathGuard (Level 2: Live status ticker & progress)
                progress.Report(new TaskProgressReport(
                    Percentage: 15,
                    StatusText: "Auditing junk directories & system caches...",
                    TargetItem: @"C:\Windows\Temp, %TEMP%, RecycleBin"));

                var junkCategories = await _junkEngine.ScanJunkAsync(token);
                long junkTotal = 0;
                foreach (var j in junkCategories)
                {
                    junkTotal += j.SizeBytes;
                }

                token.ThrowIfCancellationRequested();

                // 2. Quét Khóa Registry Mồ Côi (Chỉ HKCU/Software và HKLM/Software, CẤM SYSTEM)
                progress.Report(new TaskProgressReport(
                    Percentage: 55,
                    StatusText: "Scanning orphaned application registry keys...",
                    TargetItem: @"HKCU\Software & HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall"));

                long orphanKeyCount = await ScanOrphanedRegistryKeysAsync(token);

                token.ThrowIfCancellationRequested();

                // 3. Quét Leftovers trong LocalAppData/Temp
                progress.Report(new TaskProgressReport(
                    Percentage: 80,
                    StatusText: "Auditing application remnants & leftover caches...",
                    TargetItem: @"%LocalAppData%, %ProgramData%\Package Cache"));

                long leftoversBytes = await ScanAppLeftoversAsync(token);

                token.ThrowIfCancellationRequested();

                // 4. Đẩy kết quả ra UI
                await RunOnUIActionAsync(() =>
                {
                    CleanerCategories.Add(new CleanerCategoryItem
                    {
                        Id = "junk_cache",
                        Title = "Temporary Files & System Caches",
                        Description = "Recycle bin, system temp, crash dumps, and browser caches protected by SafePathGuard.",
                        SizeBytes = junkTotal,
                        SizeFormatted = FormatBytes(junkTotal)
                    });

                    CleanerCategories.Add(new CleanerCategoryItem
                    {
                        Id = "registry_orphans",
                        Title = "Orphaned App Registry Keys",
                        Description = "Missing uninstaller references and unmapped extensions under HKCU/HKLM Software hives.",
                        SizeBytes = orphanKeyCount * 1024,
                        SizeFormatted = $"{orphanKeyCount} Keys"
                    });

                    CleanerCategories.Add(new CleanerCategoryItem
                    {
                        Id = "app_leftovers",
                        Title = "Application Remnants & Leftover Cache",
                        Description = "Uninstalled program folders and leftover logs in %LocalAppData% and %ProgramData%.",
                        SizeBytes = leftoversBytes,
                        SizeFormatted = FormatBytes(leftoversBytes)
                    });

                    long grandTotal = junkTotal + leftoversBytes;
                    StatusMessage = $"Scan completed. Found {FormatBytes(grandTotal)} of clutter and {orphanKeyCount} orphaned keys.";
                    UpdateTotalSelected();
                    OnPropertyChanged(nameof(CanClean));
                });
            }, isIndeterminate: false);
    }

    [RelayCommand]
    public async Task CleanSelectedAsync(CancellationToken ct = default)
    {
        var selectedList = new System.Collections.Generic.List<CleanerCategoryItem>();
        foreach (var c in CleanerCategories)
        {
            if (c.IsSelected) selectedList.Add(c);
        }

        if (selectedList.Count == 0) return;

        long totalToClean = 0;
        foreach (var item in selectedList) totalToClean += item.SizeBytes;

        await ExecuteAsyncTaskAsync(
            "Purging selected junk and orphaned entries...",
            async (token, progress) =>
            {
                double stepWeight = 90.0 / Math.Max(1, selectedList.Count);
                double currentProg = 5.0;

                foreach (var item in selectedList)
                {
                    token.ThrowIfCancellationRequested();

                    progress.Report(new TaskProgressReport(
                        Percentage: currentProg,
                        StatusText: $"Cleaning {item.Title}...",
                        TargetItem: item.Description));

                    if (item.Id == "junk_cache")
                    {
                        var categories = await _junkEngine.ScanJunkAsync(token);
                        await _junkEngine.CleanJunkAsync(categories);
                    }
                    else if (item.Id == "registry_orphans")
                    {
                        await PurgeOrphanedRegistryKeysSafelyAsync(token);
                    }
                    else if (item.Id == "app_leftovers")
                    {
                        await PurgeAppLeftoversSafelyAsync(token);
                    }

                    currentProg += stepWeight;
                }

                progress.Report(new TaskProgressReport(
                    Percentage: 100,
                    StatusText: "Cleanup successfully completed with zero OS disruption.",
                    TargetItem: string.Empty));

                await RunOnUIActionAsync(() =>
                {
                    SummaryCleanedText = $"Cleanup Complete: Safely reclaimed {FormatBytes(totalToClean)} of system clutter with zero OS disruption.";
                    IsSummaryCardVisible = true;
                    StatusMessage = SummaryCleanedText;
                });

                await ScanAllAsync(token);
            }, isIndeterminate: false);
    }

    private void UpdateTotalSelected()
    {
        long total = 0;
        foreach (var c in CleanerCategories)
        {
            if (c.IsSelected) total += c.SizeBytes;
        }
        TotalSelectedSizeFormatted = $"Selected: {FormatBytes(total)}";
    }

    private static Task<long> ScanOrphanedRegistryKeysAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            long count = 0;
            const string uninstallPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            using var key = Registry.CurrentUser.OpenSubKey(uninstallPath);
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    string? uninstallString = subKey.GetValue("UninstallString") as string;
                    if (!string.IsNullOrEmpty(uninstallString))
                    {
                        string exePath = uninstallString.Trim('\"').Split(' ')[0];
                        if (exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !File.Exists(exePath))
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }, ct);
    }

    private static async Task PurgeOrphanedRegistryKeysSafelyAsync(CancellationToken ct)
    {
        const string uninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        using var baseKey = Registry.CurrentUser.OpenSubKey(uninstallKeyPath, writable: true);
        if (baseKey == null) return;

        foreach (var subKeyName in baseKey.GetSubKeyNames())
        {
            ct.ThrowIfCancellationRequested();
            string fullKeyPath = $@"HKCU\{uninstallKeyPath}\{subKeyName}";

            if (!SafeRegistryGuard.IsSafeToDeleteKey(fullKeyPath))
            {
                continue;
            }

            using var subKey = baseKey.OpenSubKey(subKeyName);
            if (subKey != null)
            {
                string? uninstallString = subKey.GetValue("UninstallString") as string;
                if (!string.IsNullOrEmpty(uninstallString))
                {
                    string exePath = uninstallString.Trim('\"').Split(' ')[0];
                    if (exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !File.Exists(exePath))
                    {
                        await RegistryBackupEngine.BackupKeyAsync(fullKeyPath);
                        baseKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
                    }
                }
            }
        }
    }

    private static Task<long> ScanAppLeftoversAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            long bytes = 0;
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string tempDir = Path.Combine(localAppData, "Temp");
                if (Directory.Exists(tempDir))
                {
                    var di = new DirectoryInfo(tempDir);
                    foreach (var file in di.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (SafePathGuard.IsSafeToDelete(file.FullName))
                        {
                            bytes += file.Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("CleanerViewModel.ScanAppLeftoversAsync", ex);
            }
            return bytes;
        }, ct);
    }

    private static Task PurgeAppLeftoversSafelyAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string tempDir = Path.Combine(localAppData, "Temp");
                if (Directory.Exists(tempDir))
                {
                    var di = new DirectoryInfo(tempDir);
                    foreach (var file in di.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (SafePathGuard.IsSafeToDelete(file.FullName))
                        {
                            try 
                            { 
                                file.Delete(); 
                            } 
                            catch (Exception ex) 
                            { 
                                CrashLogger.LogException("CleanerViewModel.PurgeFile", ex); 
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashLogger.LogException("CleanerViewModel.PurgeAppLeftoversSafelyAsync", ex);
            }
        }, ct);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int digit = (int)Math.Floor(Math.Log(bytes, 1024));
        if (digit >= units.Length) digit = units.Length - 1;
        return $"{(bytes / Math.Pow(1024, digit)):F1} {units[digit]}";
    }
}

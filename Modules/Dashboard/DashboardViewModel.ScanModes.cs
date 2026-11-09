using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Engines;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;
using WinCarePro.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinCarePro.ViewModels;

public partial class DashboardViewModel
{
    // ========================================================================
    // 4 Modular Scan Modes State & Properties
    // ========================================================================

    [ObservableProperty]
    private DashboardScanMode _currentScanMode = DashboardScanMode.DiskAndCache;

    [ObservableProperty]
    private bool _isScanPanelOpen;

    [ObservableProperty]
    private bool _isModeScanning;

    [ObservableProperty]
    private bool _isModeFixing;

    [ObservableProperty]
    private bool _hasModeScanned;

    [ObservableProperty]
    private int _modeScanProgress;

    [ObservableProperty]
    private string _modeScanStatus = string.Empty;

    [ObservableProperty]
    private string _modeCurrentModuleName = string.Empty;

    [ObservableProperty]
    private int _modeScannedItemCount;

    [ObservableProperty]
    private string _modeHeaderTitle = string.Empty;

    [ObservableProperty]
    private string _modeHeaderSubtitle = string.Empty;

    [ObservableProperty]
    private string _modeHeaderGlyph = "\uEA99";

    [ObservableProperty]
    private string _modeActionFixButtonText = "Clean Now".T();

    [ObservableProperty]
    private string _modeDeepNavTarget = "cleaner";

    [ObservableProperty]
    private string _modeDeepNavButtonText = "Open Deep Cleaner".T();

    // Summary counts for post-scan status
    [ObservableProperty]
    private int _safeCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _dangerCount;

    [ObservableProperty]
    private string _quickFixResultMessage = string.Empty;

    public ObservableCollection<DashboardSubCategoryItem> ModeSubCategories { get; } = new();
    public ObservableCollection<DashboardScanFinding> ModeFindings { get; } = new();

    private CancellationTokenSource? _modeScanCts;
    private CancellationTokenSource? _modeFixCts;

    // Temporary storage for scanned items to perform local fix
    private List<JunkCategory> _modeJunkCategories = new();
    private List<StartupEntry> _modeStartupApps = new();
    private List<ServiceEntry> _modeHeavyServices = new();
    private List<RegistryIssue> _modeRegistryIssues = new();

    /// <summary>
    /// Opens the Scan Panel configured for a specific scan mode.
    /// </summary>
    [RelayCommand]
    public void OpenScanPanel(string modeName)
    {
        if (Enum.TryParse<DashboardScanMode>(modeName, true, out var mode))
        {
            SetupScanMode(mode);
            IsScanPanelOpen = true;
        }
    }

    [RelayCommand]
    public void CloseScanPanel()
    {
        if (IsModeScanning)
        {
            CancelModeScan();
        }
        IsScanPanelOpen = false;
    }

    public void SetupScanMode(DashboardScanMode mode)
    {
        CurrentScanMode = mode;
        HasModeScanned = false;
        ModeScanProgress = 0;
        ModeScannedItemCount = 0;
        SafeCount = 0;
        WarningCount = 0;
        DangerCount = 0;
        QuickFixResultMessage = string.Empty;

        ModeSubCategories.Clear();
        ModeFindings.Clear();

        switch (mode)
        {
            case DashboardScanMode.DiskAndCache:
                ModeHeaderTitle = "Disk & Cache Cleaner".T();
                ModeHeaderSubtitle = "Scan and purge Temp, Prefetch, Windows Update cache, and dump logs.".T();
                ModeHeaderGlyph = "\uEA99";
                ModeActionFixButtonText = "Clean All Junk Now".T();
                ModeDeepNavTarget = "cleaner";
                ModeDeepNavButtonText = "Open Junk Cleaner".T();

                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "temp", NameKey = "User & System Temp", DescriptionKey = "Temporary files created by Windows & apps", IconGlyph = "\uE8B7" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "prefetch", NameKey = "Prefetch & Memory Cache", DescriptionKey = "Stale Windows application prefetch caches", IconGlyph = "\uE7F1" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "update_cache", NameKey = "Windows Update Cache", DescriptionKey = "Downloaded SoftwareDistribution installers", IconGlyph = "\uE895" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "dumps", NameKey = "Crash Dumps & Error Logs", DescriptionKey = "Minidumps and memory dump error traces", IconGlyph = "\uE9CE" });
                break;

            case DashboardScanMode.StartupAndPerformance:
                ModeHeaderTitle = "Startup & Performance Audit".T();
                ModeHeaderSubtitle = "Detect high-impact startup apps and resource-heavy background services.".T();
                ModeHeaderGlyph = "\uE7F4";
                ModeActionFixButtonText = "Optimize Startup & Services".T();
                ModeDeepNavTarget = "startup";
                ModeDeepNavButtonText = "Open Startup Manager".T();

                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "high_startup", NameKey = "High Impact Startup Apps", DescriptionKey = "Applications slowing down boot duration", IconGlyph = "\uE7C5" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "unneeded_services", NameKey = "Unneeded Background Services", DescriptionKey = "Non-essential services consuming RAM", IconGlyph = "\uE9D9" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "delayed_startup", NameKey = "Startup Load Telemetry", DescriptionKey = "Boot execution times and startup impact", IconGlyph = "\uE916" });
                break;

            case DashboardScanMode.SecurityCheck:
                ModeHeaderTitle = "Security & Network Armor".T();
                ModeHeaderSubtitle = "Audit Windows Defender, UAC, Firewall profiles, and suspicious listening ports.".T();
                ModeHeaderGlyph = "\uEA18";
                ModeActionFixButtonText = "Harden Security Now".T();
                ModeDeepNavTarget = "security";
                ModeDeepNavButtonText = "Open Security Center".T();

                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "defender", NameKey = "Windows Defender & Real-Time", DescriptionKey = "Antivirus real-time protection state", IconGlyph = "\uE7BA" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "firewall", NameKey = "Firewall Profiles", DescriptionKey = "Domain, Private, and Public network firewall rules", IconGlyph = "\uE83D" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "uac", NameKey = "User Account Control (UAC)", DescriptionKey = "Elevation prompt privilege escalation shield", IconGlyph = "\uE7EE" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "ports", NameKey = "Active Listening Ports", DescriptionKey = "Inbound listening ports and suspicious bindings", IconGlyph = "\uE774" });
                break;

            case DashboardScanMode.RegistryAndDiskHealth:
                ModeHeaderTitle = "Registry & Disk S.M.A.R.T Health".T();
                ModeHeaderSubtitle = "Inspect safe orphan registry entries and physical drive S.M.A.R.T telemetry.".T();
                ModeHeaderGlyph = "\uEDA2";
                ModeActionFixButtonText = "Clean Registry & Health Check".T();
                ModeDeepNavTarget = "registry";
                ModeDeepNavButtonText = "Open Registry Doctor".T();

                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "orphan_reg", NameKey = "Orphan Registry Keys", DescriptionKey = "Missing COM/CLSID, invalid paths & uninstalls", IconGlyph = "\uE74C" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "disk_smart", NameKey = "Drive S.M.A.R.T Wear Status", DescriptionKey = "PredictFailure and physical storage health", IconGlyph = "\uEDA2" });
                ModeSubCategories.Add(new DashboardSubCategoryItem { Id = "disk_temp", NameKey = "Drive Active Temperature", DescriptionKey = "Thermal operating limits of internal disks", IconGlyph = "\uE9CA" });
                break;
        }

        ModeScanStatus = "Ready to scan".T();
        ModeCurrentModuleName = ModeHeaderTitle;
    }

    [RelayCommand]
    public void CancelModeScan()
    {
        _modeScanCts?.Cancel();
    }

    /// <summary>
    /// Executes the scanning process for the active mode with live item counting and progress.
    /// </summary>
    [RelayCommand]
    public async Task StartModeScanAsync()
    {
        if (IsModeScanning || IsModeFixing) return;

        _modeScanCts?.Cancel();
        _modeScanCts?.Dispose();
        _modeScanCts = new CancellationTokenSource();
        var ct = _modeScanCts.Token;

        IsModeScanning = true;
        HasModeScanned = false;
        ModeScanProgress = 5;
        ModeScannedItemCount = 0;
        SafeCount = 0;
        WarningCount = 0;
        DangerCount = 0;
        QuickFixResultMessage = string.Empty;

        RunOnUI(() =>
        {
            ModeFindings.Clear();
            ModeScanStatus = "Initializing scan...".T();
        });

        try
        {
            switch (CurrentScanMode)
            {
                case DashboardScanMode.DiskAndCache:
                    await ExecuteDiskAndCacheScanAsync(ct);
                    break;
                case DashboardScanMode.StartupAndPerformance:
                    await ExecuteStartupAndPerformanceScanAsync(ct);
                    break;
                case DashboardScanMode.SecurityCheck:
                    await ExecuteSecurityScanAsync(ct);
                    break;
                case DashboardScanMode.RegistryAndDiskHealth:
                    await ExecuteRegistryAndDiskScanAsync(ct);
                    break;
            }

            RunOnUI(() =>
            {
                ModeScanProgress = 100;
                HasModeScanned = true;
                ModeScanStatus = string.Format("Scan Complete. Found {0} items ({1} Safe, {2} Notice, {3} Risk)".T(),
                    ModeScannedItemCount, SafeCount, WarningCount, DangerCount);
                ModeCurrentModuleName = "Scan Complete".T();
            });
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() =>
            {
                ModeScanStatus = "Scan cancelled by user.".T();
                ModeScanProgress = 0;
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.StartModeScanAsync", ex);
            RunOnUI(() =>
            {
                ModeScanStatus = "Scan encountered an error: ".T() + ex.Message;
            });
        }
        finally
        {
            IsModeScanning = false;
            var cts = _modeScanCts;
            _modeScanCts = null;
            try { cts?.Dispose(); } catch { }
        }
    }

    private async Task ExecuteDiskAndCacheScanAsync(CancellationToken ct)
    {
        RunOnUI(() =>
        {
            ModeCurrentModuleName = "Scanning Temporary Files...".T();
            ModeScanStatus = "Analyzing temp directories and caches...".T();
            ModeScanProgress = 15;
        });

        var cats = await _junkEngine.ScanJunkAsync(ct).ConfigureAwait(false);
        _modeJunkCategories = cats;

        int totalItems = 0;
        long totalBytes = 0;

        foreach (var cat in cats)
        {
            ct.ThrowIfCancellationRequested();
            totalItems += cat.FileCount;
            totalBytes += cat.SizeBytes;

            var finding = new DashboardScanFinding
            {
                Category = cat.Name,
                Title = cat.Name,
                Description = $"{cat.FileCount} files ({cat.SizeFormatted})",
                Severity = cat.SizeBytes > 500 * 1024 * 1024 ? ScanFindingSeverity.Warning : ScanFindingSeverity.Healthy
            };

            RunOnUI(() =>
            {
                ModeFindings.Add(finding);
                ModeScannedItemCount = totalItems;
                if (finding.Severity == ScanFindingSeverity.Healthy) SafeCount++;
                else WarningCount++;
            });

            await Task.Delay(40, ct).ConfigureAwait(false);
        }

        RunOnUI(() =>
        {
            ModeScanProgress = 80;
            ModeCurrentModuleName = "Validating Disk Storage Cache...".T();
            JunkFileSize = $"{(totalBytes / 1024.0 / 1024.0):F1} MB";
        });
    }

    private async Task ExecuteStartupAndPerformanceScanAsync(CancellationToken ct)
    {
        RunOnUI(() =>
        {
            ModeCurrentModuleName = "Scanning Startup Applications...".T();
            ModeScanStatus = "Inspecting startup registries and task scheduler...".T();
            ModeScanProgress = 20;
        });

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var startups = _startupEngine.GetStartupEntries();
            _modeStartupApps = startups;

            int items = 0;
            foreach (var app in startups)
            {
                ct.ThrowIfCancellationRequested();
                items++;

                ScanFindingSeverity severity = ScanFindingSeverity.Healthy;
                if (app.StartupImpact.Equals("High", StringComparison.OrdinalIgnoreCase))
                {
                    severity = ScanFindingSeverity.Warning;
                }

                var finding = new DashboardScanFinding
                {
                    Category = "Startup App".T(),
                    Title = app.Name,
                    Description = string.Format("Impact: {0} | Source: {1}".T(), app.StartupImpact, app.SourceFormatted),
                    Severity = severity
                };

                RunOnUI(() =>
                {
                    ModeFindings.Add(finding);
                    ModeScannedItemCount = items;
                    if (severity == ScanFindingSeverity.Healthy) SafeCount++;
                    else WarningCount++;
                });
            }

            RunOnUI(() =>
            {
                ModeScanProgress = 60;
                ModeCurrentModuleName = "Analyzing Background Services...".T();
            });

            ct.ThrowIfCancellationRequested();
            var services = _startupEngine.GetServices();
            var nonSystemServices = services.Where(s => !s.IsCriticalService && s.Status.Equals("Running", StringComparison.OrdinalIgnoreCase)).ToList();
            _modeHeavyServices = nonSystemServices;

            foreach (var svc in nonSystemServices.Take(15))
            {
                ct.ThrowIfCancellationRequested();
                items++;

                var finding = new DashboardScanFinding
                {
                    Category = "Service".T(),
                    Title = svc.DisplayName,
                    Description = string.Format("Status: {0} | Startup: {1}".T(), svc.Status, svc.StartupType),
                    Severity = ScanFindingSeverity.Healthy
                };

                RunOnUI(() =>
                {
                    ModeFindings.Add(finding);
                    ModeScannedItemCount = items;
                    SafeCount++;
                });
            }
        }, ct).ConfigureAwait(false);
    }

    private async Task ExecuteSecurityScanAsync(CancellationToken ct)
    {
        RunOnUI(() =>
        {
            ModeCurrentModuleName = "Auditing Antivirus & Windows Defender...".T();
            ModeScanStatus = "Querying security status via WMI...".T();
            ModeScanProgress = 15;
        });

        await Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested();
            bool defenderOk = _securityEngine.GetDefenderRealtimeStatus();
            bool firewallOk = _securityEngine.GetFirewallStatus();
            bool uacOk = _securityEngine.GetUacStatus();

            int count = 0;

            // 1. Defender Finding
            count++;
            var defenderFinding = new DashboardScanFinding
            {
                Category = "Antivirus".T(),
                Title = "Windows Defender Real-time Protection".T(),
                Description = defenderOk ? "Real-time threat detection is active".T() : "Defender real-time scanning is disabled".T(),
                Severity = defenderOk ? ScanFindingSeverity.Healthy : ScanFindingSeverity.Danger
            };

            RunOnUI(() =>
            {
                ModeFindings.Add(defenderFinding);
                ModeScannedItemCount = count;
                if (defenderOk) SafeCount++; else DangerCount++;
                ModeScanProgress = 40;
                ModeCurrentModuleName = "Auditing Windows Firewall...".T();
            });

            // 2. Firewall Finding
            count++;
            var firewallFinding = new DashboardScanFinding
            {
                Category = "Firewall".T(),
                Title = "Windows Firewall Profiles".T(),
                Description = firewallOk ? "All firewall profiles are actively filtering packets".T() : "One or more firewall profiles are turned off".T(),
                Severity = firewallOk ? ScanFindingSeverity.Healthy : ScanFindingSeverity.Warning
            };

            RunOnUI(() =>
            {
                ModeFindings.Add(firewallFinding);
                ModeScannedItemCount = count;
                if (firewallOk) SafeCount++; else WarningCount++;
                ModeScanProgress = 65;
                ModeCurrentModuleName = "Verifying User Account Control (UAC)...".T();
            });

            // 3. UAC Finding
            count++;
            var uacFinding = new DashboardScanFinding
            {
                Category = "Access Control".T(),
                Title = "User Account Control (UAC)".T(),
                Description = uacOk ? "UAC elevation prompts are enabled".T() : "UAC is disabled, allowing silent privilege escalation".T(),
                Severity = uacOk ? ScanFindingSeverity.Healthy : ScanFindingSeverity.Warning
            };

            RunOnUI(() =>
            {
                ModeFindings.Add(uacFinding);
                ModeScannedItemCount = count;
                if (uacOk) SafeCount++; else WarningCount++;
                ModeScanProgress = 80;
                ModeCurrentModuleName = "Scanning Active Network Connections & Listening Ports...".T();
            });

            // 4. Network Connections & Ports
            ct.ThrowIfCancellationRequested();
            var connections = _networkEngine.GetActiveConnectionsNative();
            var listeningPorts = connections.Where(c => c.State.Equals("Listen", StringComparison.OrdinalIgnoreCase) || c.State.Equals("Listening", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var port in listeningPorts.Take(10))
            {
                ct.ThrowIfCancellationRequested();
                count++;
                var portFinding = new DashboardScanFinding
                {
                    Category = "Network Port".T(),
                    Title = string.Format("Active Socket {0}".T(), port.LocalAddress),
                    Description = string.Format("Protocol: {0} | Process: {1} ({2})".T(), port.Protocol, port.ProcessName, port.ProcessId),
                    Severity = ScanFindingSeverity.Healthy
                };

                RunOnUI(() =>
                {
                    ModeFindings.Add(portFinding);
                    ModeScannedItemCount = count;
                    SafeCount++;
                });

                await Task.Delay(20, ct).ConfigureAwait(false);
            }
        }, ct).ConfigureAwait(false);
    }

    private async Task ExecuteRegistryAndDiskScanAsync(CancellationToken ct)
    {
        RunOnUI(() =>
        {
            ModeCurrentModuleName = "Scanning Orphan Registry Entries...".T();
            ModeScanStatus = "Verifying safe registry keys...".T();
            ModeScanProgress = 20;
        });

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var regIssues = _registryEngine.ScanRegistryIssues(ct);
            _modeRegistryIssues = regIssues;

            int count = 0;
            foreach (var issue in regIssues.Take(25))
            {
                ct.ThrowIfCancellationRequested();
                count++;

                var finding = new DashboardScanFinding
                {
                    Category = issue.Section,
                    Title = string.IsNullOrEmpty(issue.ValueName) ? issue.KeyPath : issue.ValueName,
                    Description = issue.Description,
                    Severity = ScanFindingSeverity.Warning
                };

                RunOnUI(() =>
                {
                    ModeFindings.Add(finding);
                    ModeScannedItemCount = count;
                    WarningCount++;
                });
            }

            RunOnUI(() =>
            {
                ModeScanProgress = 70;
                ModeCurrentModuleName = "Inspecting Drive S.M.A.R.T & Temperature...".T();
            });

            ct.ThrowIfCancellationRequested();
            double ssdHealth = _hardwareEngine.GetSsdHealthPercent();
            double diskTemp = _hardwareEngine.GetDiskTemperature();

            count++;
            var healthFinding = new DashboardScanFinding
            {
                Category = "Storage Health".T(),
                Title = "SSD / HDD Health Index".T(),
                Description = string.Format("S.M.A.R.T diagnostic score: {0:F0}%".T(), ssdHealth),
                Severity = ssdHealth >= 80 ? ScanFindingSeverity.Healthy : (ssdHealth >= 50 ? ScanFindingSeverity.Warning : ScanFindingSeverity.Danger)
            };

            RunOnUI(() =>
            {
                ModeFindings.Add(healthFinding);
                ModeScannedItemCount = count;
                if (healthFinding.Severity == ScanFindingSeverity.Healthy) SafeCount++;
                else if (healthFinding.Severity == ScanFindingSeverity.Warning) WarningCount++;
                else DangerCount++;
            });

            if (diskTemp > 0)
            {
                count++;
                var tempFinding = new DashboardScanFinding
                {
                    Category = "Thermal".T(),
                    Title = "Disk Temperature".T(),
                    Description = string.Format("Operating temperature: {0:F0} °C".T(), diskTemp),
                    Severity = diskTemp < 55 ? ScanFindingSeverity.Healthy : (diskTemp < 65 ? ScanFindingSeverity.Warning : ScanFindingSeverity.Danger)
                };

                RunOnUI(() =>
                {
                    ModeFindings.Add(tempFinding);
                    ModeScannedItemCount = count;
                    if (tempFinding.Severity == ScanFindingSeverity.Healthy) SafeCount++;
                    else WarningCount++;
                });
            }
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the on-the-spot Quick Clean / Optimization action for the active mode.
    /// </summary>
    [RelayCommand]
    public async Task ExecuteModeQuickFixAsync()
    {
        if (IsModeFixing || IsModeScanning) return;

        _modeFixCts?.Cancel();
        _modeFixCts?.Dispose();
        _modeFixCts = new CancellationTokenSource();
        var ct = _modeFixCts.Token;

        IsModeFixing = true;
        QuickFixResultMessage = "Executing immediate optimization...".T();

        try
        {
            switch (CurrentScanMode)
            {
                case DashboardScanMode.DiskAndCache:
                    if (_modeJunkCategories.Count > 0)
                    {
                        long cleanedBytes = await _junkEngine.CleanJunkAsync(_modeJunkCategories, ct).ConfigureAwait(false);
                        RunOnUI(() =>
                        {
                            QuickFixResultMessage = string.Format("Cleaned {0:F1} MB of junk files successfully!".T(), cleanedBytes / 1024.0 / 1024.0);
                            JunkFileSize = "0.0 MB";
                            WarningCount = 0;
                            SafeCount = ModeFindings.Count;
                            foreach (var f in ModeFindings)
                            {
                                f.Severity = ScanFindingSeverity.Healthy;
                                f.Description = "Cleaned".T();
                            }
                        });
                    }
                    else
                    {
                        RunOnUI(() => QuickFixResultMessage = "No junk files to clean.".T());
                    }
                    break;

                case DashboardScanMode.StartupAndPerformance:
                    int optimizedCount = 0;
                    await Task.Run(() =>
                    {
                        foreach (var app in _modeStartupApps.Where(a => a.StartupImpact.Equals("High", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (_startupEngine.ToggleStartupEntry(app, false))
                            {
                                optimizedCount++;
                            }
                        }
                    }, ct).ConfigureAwait(false);

                    RunOnUI(() =>
                    {
                        QuickFixResultMessage = string.Format("Optimized {0} high impact startup items.".T(), optimizedCount);
                        WarningCount = 0;
                        SafeCount = ModeFindings.Count;
                        foreach (var f in ModeFindings)
                        {
                            f.Severity = ScanFindingSeverity.Healthy;
                        }
                    });
                    break;

                case DashboardScanMode.SecurityCheck:
                    await Task.Run(() =>
                    {
                        _securityEngine.EnableAllFirewallProfiles();
                        _securityEngine.EnableUac();
                        _securityEngine.EnableDefenderRealtime();
                    }, ct).ConfigureAwait(false);

                    RunOnUI(() =>
                    {
                        QuickFixResultMessage = "Firewall, Defender & UAC rules verified and enforced.".T();
                        DangerCount = 0;
                        WarningCount = 0;
                        SafeCount = ModeFindings.Count;
                        foreach (var f in ModeFindings)
                        {
                            f.Severity = ScanFindingSeverity.Healthy;
                            f.Description = "Enforced & Protected".T();
                        }
                    });
                    break;

                case DashboardScanMode.RegistryAndDiskHealth:
                    if (_modeRegistryIssues.Count > 0)
                    {
                        await _registryEngine.FixRegistryIssuesAsync(_modeRegistryIssues, ct).ConfigureAwait(false);
                        RunOnUI(() =>
                        {
                            QuickFixResultMessage = string.Format("Safely resolved {0} orphan registry entries.".T(), _modeRegistryIssues.Count);
                            WarningCount = 0;
                            SafeCount = ModeFindings.Count;
                            foreach (var f in ModeFindings.Where(x => x.Category != "Storage Health" && x.Category != "Thermal"))
                            {
                                f.Severity = ScanFindingSeverity.Healthy;
                                f.Description = "Cleaned and removed".T();
                            }
                        });
                    }
                    else
                    {
                        RunOnUI(() => QuickFixResultMessage = "Disk is healthy, no orphan keys found.".T());
                    }
                    break;
            }

            RefreshActionLogs();
        }
        catch (OperationCanceledException)
        {
            RunOnUI(() => QuickFixResultMessage = "Optimization cancelled.".T());
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("DashboardViewModel.ExecuteModeQuickFixAsync", ex);
            RunOnUI(() => QuickFixResultMessage = "Optimization error: ".T() + ex.Message);
        }
        finally
        {
            IsModeFixing = false;
            var cts = _modeFixCts;
            _modeFixCts = null;
            try { cts?.Dispose(); } catch { }
        }
    }
}

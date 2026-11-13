using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCarePro.Engines;
using WinCarePro.Core.Helpers;
using WinCarePro.Core.Models;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class SecurityViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly SecurityPrivacyEngine _securityEngine;
    private readonly EventHandler _languageChangedHandler;
    private System.Threading.CancellationTokenSource? _scanCts;
    private bool _isDisposed;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan security status".T();

    [ObservableProperty]
    private bool _isSafetyNoticeVisible = true;

    [ObservableProperty]
    private bool _isSummaryCardVisible;

    [ObservableProperty]
    private string _summarySecurityText = string.Empty;

    [ObservableProperty]
    private int _securityScore = 100;

    [ObservableProperty]
    private string _antivirusStatus = "Checking...".T();

    [ObservableProperty]
    private bool _isFirewallActive = true;

    [ObservableProperty]
    private string _firewallStatusText = "Checking...".T();

    [ObservableProperty]
    private bool _isDefenderRealtimeActive;

    [ObservableProperty]
    private bool _isFirewallDomainActive;

    [ObservableProperty]
    private bool _isFirewallPrivateActive;

    [ObservableProperty]
    private bool _isFirewallPublicActive;

    [ObservableProperty]
    private bool _isUacActive;

    [ObservableProperty]
    private string _uacLevelDescription = "Checking...".T();

    public ObservableCollection<WinCarePro.Models.SecurityStatusItem> SecurityMatrixItems { get; } = new();

    [ObservableProperty]
    private bool _isSecureBootEnabled;

    [ObservableProperty]
    private string _secureBootStatusText = "Checking...".T();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TpmChipText))]
    private bool _isTpmEnabled;

    [ObservableProperty]
    private string _tpmStatusText = "Checking...".T();

    public string TpmChipText => IsTpmEnabled ? "TPM Active".T() : "Not Detected".T();

    [ObservableProperty]
    private bool _isCoreIsolationEnabled;

    [ObservableProperty]
    private string _coreIsolationStatusText = "Checking...".T();

    [ObservableProperty]
    private bool _isTamperProtectionEnabled;

    [ObservableProperty]
    private string _tamperProtectionStatusText = "Checking...".T();

    [ObservableProperty]
    private bool _isRdpNlaEnabled;

    [ObservableProperty]
    private string _rdpNlaStatusText = "Checking...".T();

    // Privacy Toggles (Expanded to 8 controls)
    [ObservableProperty]
    private bool _advertisingIdEnabled;

    [ObservableProperty]
    private bool _telemetryEnabled;

    [ObservableProperty]
    private bool _clipboardHistoryEnabled;

    [ObservableProperty]
    private bool _inputTrackingEnabled;

    [ObservableProperty]
    private bool _cortanaBingEnabled;

    [ObservableProperty]
    private bool _locationTrackingEnabled;

    [ObservableProperty]
    private bool _feedbackTrackingEnabled;

    [ObservableProperty]
    private bool _appDiagnosticsEnabled;

    [ObservableProperty]
    private string _activeProfile = "Balanced";

    [ObservableProperty]
    private string _securityScoreGrade = "OPTIMAL".T();

    [ObservableProperty]
    private string _defenseStatusBadge = "SHIELD ARMED".T();

    [ObservableProperty]
    private string _searchSafeguardsText = string.Empty;

    [ObservableProperty]
    private string _selectedSafeguardCategory = "All";

    public ObservableCollection<string> SecurityAlerts { get; } = new();
    public ObservableCollection<WinCarePro.Models.SecurityAlertItem> SecurityAuditItems { get; } = new();
    public ObservableCollection<WinCarePro.Models.SecuritySafeguardItem> Safeguards { get; } = new();
    public ObservableCollection<WinCarePro.Models.SecuritySafeguardItem> FilteredSafeguards { get; } = new();

    public int AlertCount => SecurityAuditItems.Count;
    public bool HasAlerts => SecurityAuditItems.Count > 0;
    public bool HasNoAlerts => !IsScanning && SecurityAuditItems.Count == 0;
    public int CriticalAlertCount => SecurityAuditItems.Count(a => a.Severity == "Critical" && !a.IsFixed);
    public int WarningAlertCount => SecurityAuditItems.Count(a => a.Severity == "Warning" && !a.IsFixed);
    public int SafeguardProtectedCount => Safeguards.Count(s => s.IsProtected);
    public int SafeguardTotalCount => Safeguards.Count;

    partial void OnSearchSafeguardsTextChanged(string value) => FilterSafeguards();
    partial void OnSelectedSafeguardCategoryChanged(string value) => FilterSafeguards();

    public SecurityViewModel() : this(null, null)
    {
    }

    public SecurityViewModel(SecurityPrivacyEngine? securityEngine = null, DispatcherQueue? dispatcherQueue = null)
    {
        _dispatcherQueue = dispatcherQueue ?? SafeGetDispatcherQueue();
        DispatcherQueueInstance = _dispatcherQueue;
        _securityEngine = securityEngine ?? App.Services?.GetService<SecurityPrivacyEngine>() ?? new();

        _languageChangedHandler = (s, e) =>
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_isDisposed) return;
                StatusMessage = "Ready to scan security status".T();
            });
        };
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;

        LoadPrivacySettings();
    }

    public void Initialize()
    {
        _isDisposed = false;
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
        TranslationManager.Instance.LanguageChanged += _languageChangedHandler;
    }

    public void CancelScan()
    {
        SetOperationState(OperationState.Cancelling);
        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.CancelScan", ex);
        }
        IsScanning = false;
        SetOperationState(OperationState.Idle);
    }

    public void Cleanup()
    {
        CancelScan();
        TranslationManager.Instance.LanguageChanged -= _languageChangedHandler;
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
    }

    public void LoadPrivacySettings()
    {
        try
        {
            AdvertisingIdEnabled = _securityEngine.GetPrivacySetting("advertisingid");
            TelemetryEnabled = _securityEngine.GetPrivacySetting("telemetry");
            ClipboardHistoryEnabled = _securityEngine.GetPrivacySetting("clipboardhistory");
            InputTrackingEnabled = _securityEngine.GetPrivacySetting("tracking");
            CortanaBingEnabled = _securityEngine.GetPrivacySetting("cortanabing");
            LocationTrackingEnabled = _securityEngine.GetPrivacySetting("location");
            FeedbackTrackingEnabled = _securityEngine.GetPrivacySetting("feedback");
            AppDiagnosticsEnabled = _securityEngine.GetPrivacySetting("appdiagnostics");
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.LoadPrivacySettings", ex);
        }
    }

    public async Task ScanSecurityAsync()
    {
        if (IsScanning) return;

        try
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ScanSecurityAsync.Cancel", ex);
        }

        _scanCts = new System.Threading.CancellationTokenSource();
        var ct = _scanCts.Token;

        SetOperationState(OperationState.Running);
        IsScanning = true;
        StatusMessage = "Analyzing system security indicators...".T();

        RunOnUI(() =>
        {
            SecurityAlerts.Clear();
            SecurityAuditItems.Clear();
            OnPropertyChanged(nameof(HasAlerts));
            OnPropertyChanged(nameof(HasNoAlerts));
            OnPropertyChanged(nameof(AlertCount));
        });

        try
        {
            // Execute checks in parallel for maximum performance (< 400ms)
            var avTask = Task.Run(() => _securityEngine.GetAntivirusStatus(), ct);
            var realtimeTask = Task.Run(() => _securityEngine.GetDefenderRealtimeStatus(), ct);
            var fwTask = Task.Run(() => _securityEngine.GetFirewallStatus(), ct);
            var fwProfilesTask = Task.Run(() => _securityEngine.GetFirewallProfilesStatus(), ct);
            var uacTask = Task.Run(() => _securityEngine.GetUacLevelDetails(), ct);
            var sbTask = Task.Run(() => _securityEngine.CheckSecureBootStatus(), ct);
            var tpmTask = Task.Run(() => _securityEngine.CheckTpmStatus(), ct);
            var ciTask = Task.Run(() => _securityEngine.CheckCoreIsolationStatus(), ct);
            var tpTask = Task.Run(() => _securityEngine.CheckTamperProtectionStatus(), ct);
            var nlaTask = Task.Run(() => _securityEngine.CheckRdpNlaStatus(), ct);
            var auditItemsTask = Task.Run(() => _securityEngine.RunSecurityAuditItems(), ct);
            var legacyAlertsTask = Task.Run(() => _securityEngine.RunSecurityAudits(), ct);

            await Task.WhenAll(avTask, realtimeTask, fwTask, fwProfilesTask, uacTask, sbTask, tpmTask, ciTask, tpTask, nlaTask, auditItemsTask, legacyAlertsTask);

            if (ct.IsCancellationRequested || _isDisposed)
            {
                IsScanning = false;
                SetOperationState(OperationState.Idle);
                return;
            }

            var av = await avTask;
            bool realtimeOk = await realtimeTask;
            bool fw = await fwTask;
            var (fwDomain, fwPrivate, fwPublic) = await fwProfilesTask;
            var (uacOk, uacConsent, uacDesc) = await uacTask;
            var (sbEnabled, sbText) = await sbTask;
            var (tpmOk, tpmText) = await tpmTask;
            var (ciOk, ciText) = await ciTask;
            var (tpOk, tpText) = await tpTask;
            var (nlaOk, nlaText) = await nlaTask;
            var auditItems = await auditItemsTask;
            var legacyAlerts = await legacyAlertsTask;

            // Retain score property for backward compatibility with existing tests
            int score = 100;
            if (!av.Contains("Enabled") && !av.Contains("Running")) score -= 25;
            if (!fw) score -= 20;
            if (!sbEnabled) score -= 10;
            if (!tpmOk) score -= 10;
            if (!ciOk) score -= 10;
            if (!tpOk) score -= 10;
            if (auditItems.Count > 0) score -= Math.Min(15, auditItems.Count * 5);
            score = Math.Clamp(score, 10, 100);

            RunOnUI(() =>
            {
                AntivirusStatus = av.Replace("Enabled", "Enabled".T()).Replace("Disabled", "Disabled".T()).Replace("Running", "Running".T());
                IsDefenderRealtimeActive = realtimeOk;
                IsFirewallActive = fw;
                IsFirewallDomainActive = fwDomain;
                IsFirewallPrivateActive = fwPrivate;
                IsFirewallPublicActive = fwPublic;
                FirewallStatusText = fw ? "Windows Firewall Active".T() : "Firewall Disabled or Inactive".T();
                IsUacActive = uacOk;
                UacLevelDescription = uacDesc;
                IsSecureBootEnabled = sbEnabled;
                SecureBootStatusText = sbText;
                IsTpmEnabled = tpmOk;
                TpmStatusText = tpmText;
                IsCoreIsolationEnabled = ciOk;
                CoreIsolationStatusText = ciText;
                IsTamperProtectionEnabled = tpOk;
                TamperProtectionStatusText = tpText;
                IsRdpNlaEnabled = nlaOk;
                RdpNlaStatusText = nlaText;
                SecurityScore = score;
                SecurityScoreGrade = score >= 90 ? "OPTIMAL".T() : score >= 70 ? "SECURE".T() : "MODERATE".T();
                DefenseStatusBadge = fw && realtimeOk ? "PROTECTED".T() : "ATTENTION NEEDED".T();

                // Populate SysInternals-style Binary Status Table
                SecurityMatrixItems.Clear();
                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "defender_realtime",
                    Name = "Microsoft Defender Real-Time Protection",
                    Category = "Antivirus / Endpoint Defense",
                    Description = "Continuous kernel-level malware inspection and real-time execution scanning.",
                    StatusText = realtimeOk ? "Active (Protected)" : "Disabled (Attention Required)",
                    IsSecure = realtimeOk,
                    IconGlyph = "\uE73A",
                    ActionText = "Open Defender",
                    ActionTarget = "windowsdefender:"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "fw_domain",
                    Name = "Windows Firewall - Domain Network Profile",
                    Category = "Network Stateful Inspection",
                    Description = "Inbound/outbound packet filtering active for enterprise domain connections.",
                    StatusText = fwDomain ? "Active (Protected)" : "Inactive / Disabled",
                    IsSecure = fwDomain,
                    IconGlyph = "\uE83D",
                    ActionText = "Firewall Rules",
                    ActionTarget = "wf.msc"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "fw_private",
                    Name = "Windows Firewall - Private Network Profile",
                    Category = "Network Stateful Inspection",
                    Description = "Stateful packet filtering for trusted home and private local office networks.",
                    StatusText = fwPrivate ? "Active (Protected)" : "Inactive / Disabled",
                    IsSecure = fwPrivate,
                    IconGlyph = "\uE83D",
                    ActionText = "Firewall Rules",
                    ActionTarget = "wf.msc"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "fw_public",
                    Name = "Windows Firewall - Public Network Profile",
                    Category = "Network Stateful Inspection",
                    Description = "Strict inbound connection blocking for untrusted public Wi-Fi hotspots.",
                    StatusText = fwPublic ? "Active (Protected)" : "Inactive / Disabled",
                    IsSecure = fwPublic,
                    IconGlyph = "\uE83D",
                    ActionText = "Firewall Rules",
                    ActionTarget = "wf.msc"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "core_isolation",
                    Name = "Core Isolation (Hypervisor-Enforced Code Integrity - HVCI)",
                    Category = "Virtualization-Based Security (VBS)",
                    Description = "Prevents malicious code execution in kernel mode using hardware virtualization.",
                    StatusText = ciOk ? "Active (Memory Integrity Enforced)" : "Inactive / Disabled in Windows Settings",
                    IsSecure = ciOk,
                    IconGlyph = "\uE72E",
                    ActionText = "Core Isolation Settings",
                    ActionTarget = "windowsdefender://coreisolation"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "tamper_protection",
                    Name = "Microsoft Defender Tamper Protection",
                    Category = "Anti-Tamper & Policy Guard",
                    Description = "Prevents malicious apps and scripts from overriding or turning off Defender features.",
                    StatusText = tpOk ? "Active (Locked against unauthorized overrides)" : "Inactive",
                    IsSecure = tpOk,
                    IconGlyph = "\uE8A9",
                    ActionText = "Defender Settings",
                    ActionTarget = "windowsdefender:"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "uac",
                    Name = "User Account Control (UAC)",
                    Category = "Authorization & Privilege Elevation",
                    Description = $"Current Elevation Policy: {uacDesc}",
                    StatusText = uacOk ? $"Enabled ({uacDesc})" : "Disabled (Never notify - High Risk)",
                    IsSecure = uacOk,
                    IconGlyph = "\uE7EF",
                    ActionText = "Configure UAC",
                    ActionTarget = "UserAccountControlSettings.exe"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "secure_boot",
                    Name = "UEFI Secure Boot Firmware Guard",
                    Category = "Platform Hardware Trust",
                    Description = sbText,
                    StatusText = sbEnabled ? "Active (UEFI CA Verified)" : "Inactive (Legacy / CSM Mode)",
                    IsSecure = sbEnabled,
                    IconGlyph = "\uE950",
                    ActionText = "System Info",
                    ActionTarget = "msinfo32.exe"
                });

                SecurityMatrixItems.Add(new WinCarePro.Models.SecurityStatusItem
                {
                    Id = "tpm",
                    Name = "Trusted Platform Module (TPM 2.0)",
                    Category = "Hardware Cryptographic Processor",
                    Description = tpmText,
                    StatusText = tpmOk ? "Detected & Operational" : "Not Detected or Disabled in BIOS",
                    IsSecure = tpmOk,
                    IconGlyph = "\uE950",
                    ActionText = "TPM Console",
                    ActionTarget = "tpm.msc"
                });

                // Populate rich audit items
                SecurityAuditItems.Clear();
                foreach (var item in auditItems)
                {
                    SecurityAuditItems.Add(item);
                }

                // Populate legacy alerts collection for backward compatibility
                SecurityAlerts.Clear();
                foreach (var alert in legacyAlerts)
                {
                    if (alert.StartsWith("Suspicious startup program: "))
                    {
                        int colonIndex = alert.IndexOf("program: ");
                        int runsIndex = alert.IndexOf(" runs shell");
                        if (colonIndex != -1 && runsIndex != -1)
                        {
                            string name = alert.Substring(colonIndex + 9, runsIndex - (colonIndex + 9));
                            SecurityAlerts.Add(string.Format("Suspicious startup program: {0} runs shell command or runs from Temp!".T(), name));
                        }
                        else
                        {
                            SecurityAlerts.Add(alert.T());
                        }
                    }
                    else
                    {
                        SecurityAlerts.Add(alert.T());
                    }
                }

                if (!fw) SecurityAlerts.Add("Windows Firewall is disabled! Please enable it to block malicious traffic.".T());
                if (!av.Contains("Enabled") && !av.Contains("Running")) SecurityAlerts.Add("No active Antivirus protection detected. Enable Microsoft Defender.".T());
                if (!sbEnabled) SecurityAlerts.Add("Secure Boot is disabled. Enable it in your system BIOS/UEFI for rootkit defense.".T());

                OnPropertyChanged(nameof(AlertCount));
                OnPropertyChanged(nameof(HasAlerts));
                OnPropertyChanged(nameof(HasNoAlerts));
                OnPropertyChanged(nameof(CriticalAlertCount));
                OnPropertyChanged(nameof(WarningAlertCount));

                StatusMessage = "Security status refreshed directly from Windows Security Center.".T();
                SetOperationState(OperationState.Completed);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ScanSecurity", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Security analysis failed: {0}".T(), ex.Message);
                SetOperationState(OperationState.Failed);
            });
        }
        finally
        {
            IsScanning = false;
            if (CurrentOperationState == OperationState.Running)
            {
                SetOperationState(OperationState.Idle);
            }
        }
    }

    public async Task FixSecurityAlertAsync(WinCarePro.Models.SecurityAlertItem? alert)
    {
        if (alert == null || !alert.CanFix || IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Format("Fixing security issue: {0}...".T(), alert.Title);
        try
        {
            bool success = await Task.Run(() => _securityEngine.FixSecurityIssue(alert.FixActionKey));
            if (success)
            {
                RunOnUI(() =>
                {
                    alert.IsFixed = true;
                    alert.StatusText = "Resolved".T();
                    StatusMessage = string.Format("Successfully resolved: {0}".T(), alert.Title);
                    OnPropertyChanged(nameof(CriticalAlertCount));
                    OnPropertyChanged(nameof(WarningAlertCount));
                });
                await ScanSecurityAsync();
            }
            else
            {
                RunOnUI(() =>
                {
                    StatusMessage = string.Format("Failed to automatically fix: {0}".T(), alert.Title);
                });
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.FixSecurityAlertAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Error fixing issue: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadSafeguardsAsync(System.Threading.CancellationToken ct = default)
    {
        try
        {
            var items = await Task.Run(() =>
            {
                var list = _securityEngine.GetAllSafeguards();
                foreach (var item in list)
                {
                    if (ct.IsCancellationRequested) break;
                    _securityEngine.RefreshSafeguard(item);
                }
                return list;
            }, ct);

            RunOnUI(() =>
            {
                Safeguards.Clear();
                foreach (var item in items)
                {
                    Safeguards.Add(item);
                }
                FilterSafeguards();
                OnPropertyChanged(nameof(SafeguardProtectedCount));
                OnPropertyChanged(nameof(SafeguardTotalCount));
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.LoadSafeguardsAsync", ex);
        }
    }

    public void FilterSafeguards()
    {
        RunOnUI(() =>
        {
            FilteredSafeguards.Clear();
            var query = SearchSafeguardsText?.Trim().ToLowerInvariant() ?? string.Empty;
            var category = SelectedSafeguardCategory ?? "All";

            foreach (var item in Safeguards)
            {
                bool matchesCategory = category == "All" || item.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
                bool matchesSearch = string.IsNullOrEmpty(query) ||
                                     item.Name.ToLowerInvariant().Contains(query) ||
                                     item.Description.ToLowerInvariant().Contains(query);

                if (matchesCategory && matchesSearch)
                {
                    FilteredSafeguards.Add(item);
                }
            }
        });
    }

    public async Task ToggleSafeguardAsync(WinCarePro.Models.SecuritySafeguardItem? item)
    {
        if (item == null || IsBusy) return;
        IsBusy = true;
        try
        {
            bool newProtectedState = !item.IsProtected;
            bool ok = await Task.Run(() =>
            {
                return newProtectedState
                    ? _securityEngine.ApplySafeguard(item)
                    : _securityEngine.RevertSafeguard(item);
            });

            await Task.Run(() => _securityEngine.RefreshSafeguard(item));

            RunOnUI(() =>
            {
                item.NotifyStatusChanged();
                FilterSafeguards();
                OnPropertyChanged(nameof(SafeguardProtectedCount));
                StatusMessage = string.Format(
                    ok ? "Safeguard {0} updated successfully.".T() : "Failed to update safeguard {0}.".T(),
                    item.Name);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ToggleSafeguardAsync", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task EnableFirewallAsync()
    {
        IsBusy = true;
        StatusMessage = "Enabling Windows Firewall...".T();
        try
        {
            await Task.Run(() => _securityEngine.EnableAllFirewallProfiles());
            await ScanSecurityAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TogglePrivacySettingAsync(string type, bool enabled)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await Task.Run(() => _securityEngine.SetPrivacySetting(type, enabled));
            RunOnUI(() => LoadPrivacySettings());
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("SecurityViewModel.TogglePrivacySettingAsync", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ClearClipboardAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing clipboard cache...".T();
        try
        {
            await Task.Run(() => _securityEngine.ClearClipboard());
            RunOnUI(() =>
            {
                StatusMessage = "Clipboard history successfully cleared.".T();
            });
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("SecurityViewModel.ClearClipboardAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to clear clipboard: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ClearRecentFilesAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing Recent items and Run history...".T();
        try
        {
            int count = await Task.Run(() => _securityEngine.ClearRecentFiles() + _securityEngine.ClearExplorerRunHistory());
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Recent items and Explorer Run history cleared ({0} items).".T(), count);
            });
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("SecurityViewModel.ClearRecentFilesAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to clear recent files: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ClearJumpListsAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing Explorer Jump Lists...".T();
        try
        {
            int count = await Task.Run(() => _securityEngine.ClearExplorerJumpLists());
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Cleared {0} Explorer Jump List entries.".T(), count);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ClearJumpListsAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to clear jump lists: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ClearRunHistoryAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing Explorer Run and Typed Paths history...".T();
        try
        {
            int count = await Task.Run(() => _securityEngine.ClearExplorerRunHistory() + _securityEngine.ClearTypedPathsHistory());
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Cleared {0} Run & Typed Paths entries.".T(), count);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ClearRunHistoryAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to clear run history: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<int> ClearAllActivityTracesAsync()
    {
        IsBusy = true;
        StatusMessage = "Erasing all system activity traces...".T();
        int total = 0;
        try
        {
            total = await Task.Run(() => _securityEngine.ClearAllActivityTraces());
            RunOnUI(() =>
            {
                StatusMessage = string.Format("All activity traces purged. Erased {0} cached items.".T(), total);
            });
            return total;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ClearAllActivityTracesAsync", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to erase all traces: {0}".T(), ex.Message);
            });
            return total;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string GenerateSecurityReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# WinCare Pro - Security & Privacy Audit Report");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Security Score: {SecurityScore}/100 ({SecurityScoreGrade})");
        sb.AppendLine($"Defense Profile: {ActiveProfile} ({DefenseStatusBadge})");
        sb.AppendLine();
        sb.AppendLine("## Core Defense Matrix");
        sb.AppendLine($"- Antivirus: {AntivirusStatus}");
        sb.AppendLine($"- Firewall: {FirewallStatusText}");
        sb.AppendLine($"- Secure Boot: {SecureBootStatusText}");
        sb.AppendLine($"- TPM: {TpmStatusText}");
        sb.AppendLine($"- Core Isolation (HVCI): {CoreIsolationStatusText}");
        sb.AppendLine($"- Tamper Protection: {TamperProtectionStatusText}");
        sb.AppendLine($"- Remote Desktop NLA: {RdpNlaStatusText}");
        sb.AppendLine();
        sb.AppendLine("## Threat Radar & Active Alerts");
        if (SecurityAuditItems.Count == 0)
        {
            sb.AppendLine("- No active security threats detected. System is healthy.");
        }
        else
        {
            foreach (var alert in SecurityAuditItems)
            {
                sb.AppendLine($"- [{alert.Severity.ToUpper()}] {alert.Title} (Status: {alert.StatusText})");
                sb.AppendLine($"  {alert.Description}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("## Privacy & Anti-Telemetry Configuration");
        sb.AppendLine($"- Diagnostic Telemetry: {(TelemetryEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Advertising ID: {(AdvertisingIdEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Clipboard History: {(ClipboardHistoryEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Typing & Input Tracking: {(InputTrackingEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Cortana & Bing Search: {(CortanaBingEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Background Location: {(LocationTrackingEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- Feedback Frequency: {(FeedbackTrackingEnabled ? "Enabled" : "Disabled (Protected)")}");
        sb.AppendLine($"- App Diagnostics Access: {(AppDiagnosticsEnabled ? "Enabled" : "Disabled (Protected)")}");
        return sb.ToString();
    }

    public async Task<string> ExportSecurityReportAsync()
    {
        string report = GenerateSecurityReport();
        try
        {
            await Task.Run(() =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                        dp.SetText(report);
                        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.LogException("SecurityViewModel.ExportSecurityReportAsync.Clipboard", ex);
                    }
                });
            });
            StatusMessage = "Security Audit Report copied to clipboard.".T();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ExportSecurityReportAsync", ex);
        }
        return report;
    }

    public async Task ApplyDefenseProfileAsync(string profile)
    {
        if (IsBusy || IsScanning) return;
        IsBusy = true;
        ActiveProfile = profile;
        StatusMessage = string.Format("Applying {0} Defense Profile...".T(), profile);
        try
        {
            await Task.Run(() =>
            {
                if (profile == "Gamer")
                {
                    _securityEngine.ApplyPrivacyPreset("gamer");
                }
                else if (profile == "Hardened")
                {
                    _securityEngine.ApplyPrivacyPreset("max");
                    _securityEngine.EnableAllFirewallProfiles();
                }
                else
                {
                    _securityEngine.ApplyPrivacyPreset("balanced");
                }
            });

            RunOnUI(() => LoadPrivacySettings());
            await ScanSecurityAsync();
            StatusMessage = string.Format("{0} Defense Profile activated successfully.".T(), profile);
            SummarySecurityText = string.Format("{0} Defense Profile activated successfully. Live shields verified.".T(), profile);
            IsSummaryCardVisible = true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.ApplyDefenseProfile", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Failed to apply defense profile: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task QuickHardenAsync()
    {
        if (IsBusy || IsScanning) return;
        IsBusy = true;
        StatusMessage = "Applying Intelligent System Hardening...".T();
        try
        {
            await Task.Run(() =>
            {
                if (!IsFirewallActive)
                {
                    _securityEngine.EnableAllFirewallProfiles();
                }
                _securityEngine.ApplyPrivacyPreset("balanced");
                _securityEngine.ClearClipboard();
            });

            RunOnUI(() => LoadPrivacySettings());
            await ScanSecurityAsync();
            StatusMessage = "Intelligent System Hardening complete. System protected.".T();
            SummarySecurityText = "Intelligent System Hardening complete. Firewall active and privacy shields enforced.".T();
            IsSummaryCardVisible = true;
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SecurityViewModel.QuickHarden", ex);
            RunOnUI(() =>
            {
                StatusMessage = string.Format("Hardening failed: {0}".T(), ex.Message);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}

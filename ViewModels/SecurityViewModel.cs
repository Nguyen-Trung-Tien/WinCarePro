using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using System.Management;
using WinCarePro.Engines;
using WinCarePro.Models;
using WinCarePro.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCarePro.ViewModels;

public partial class SecurityViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SecurityPrivacyEngine _securityEngine = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan security status".T();

    [ObservableProperty]
    private int _securityScore = 100;

    // Security Statuses
    [ObservableProperty]
    private string _antivirusStatus = "Checking...".T();

    [ObservableProperty]
    private bool _isFirewallActive;

    [ObservableProperty]
    private string _firewallStatusText = "Checking...".T();

    [ObservableProperty]
    private string _bitLockerStatus = "Checking...".T();

    [ObservableProperty]
    private bool _isSecureBootEnabled;

    [ObservableProperty]
    private string _secureBootStatusText = "Checking...".T();

    [ObservableProperty]
    private bool _isTpmEnabled;

    [ObservableProperty]
    private string _tpmStatusText = "Checking...".T();

    // Privacy Toggles
    [ObservableProperty]
    private bool _advertisingIdEnabled;

    [ObservableProperty]
    private bool _telemetryEnabled;

    [ObservableProperty]
    private bool _clipboardHistoryEnabled;

    [ObservableProperty]
    private bool _inputTrackingEnabled;

    public ObservableCollection<string> SecurityAlerts { get; } = new();

    public SecurityViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        LoadPrivacySettings();
        _ = ScanSecurityAsync();
    }

    public void LoadPrivacySettings()
    {
        try
        {
            AdvertisingIdEnabled = _securityEngine.GetPrivacySetting("advertisingid");
            TelemetryEnabled = _securityEngine.GetPrivacySetting("telemetry");
            ClipboardHistoryEnabled = _securityEngine.GetPrivacySetting("clipboardhistory");
            InputTrackingEnabled = _securityEngine.GetPrivacySetting("tracking");
        }
        catch { }
    }

    public async Task ScanSecurityAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "Analyzing system security indicators...".T();
        
        _dispatcherQueue?.TryEnqueue(() =>
        {
            SecurityAlerts.Clear();
        });

        try
        {
            // 1. Antivirus
            var av = await Task.Run(() => _securityEngine.GetAntivirusStatus());
            
            // 2. Firewall
            bool fw = await Task.Run(() => _securityEngine.GetFirewallStatus());
            
            // 3. BitLocker
            var bl = await Task.Run(() => _securityEngine.GetBitLockerStatus());

            // 4. Secure Boot
            var (sbEnabled, sbText) = await Task.Run(() => CheckSecureBootStatus());

            // 5. TPM
            var (tpmOk, tpmText) = await Task.Run(() => CheckTpmStatus());

            // 6. Run Audits
            var alerts = await Task.Run(() => _securityEngine.RunSecurityAudits());

            // 7. Calculate Security Score
            int score = 100;
            if (!av.Contains("Enabled") && !av.Contains("Running")) score -= 30;
            if (!fw) score -= 25;
            if (!sbEnabled) score -= 15;
            if (!tpmOk) score -= 15;
            if (alerts.Count > 0) score -= Math.Min(15, alerts.Count * 5);
            score = Math.Clamp(score, 10, 100);

            _dispatcherQueue?.TryEnqueue(() =>
            {
                AntivirusStatus = av.Replace("Enabled", "Enabled".T()).Replace("Disabled", "Disabled".T()).Replace("Running", "Running".T());
                IsFirewallActive = fw;
                FirewallStatusText = fw ? "Windows Firewall Active".T() : "Firewall Disabled or Misconfigured".T();
                BitLockerStatus = bl.Replace("Off", "Off".T()).Replace("On", "On".T());
                IsSecureBootEnabled = sbEnabled;
                SecureBootStatusText = sbText;
                IsTpmEnabled = tpmOk;
                TpmStatusText = tpmText;
                SecurityScore = score;

                foreach (var alert in alerts)
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

                StatusMessage = string.Format("Scan complete. Security Score: {0}/100".T(), SecurityScore);
                IsScanning = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                StatusMessage = string.Format("Security analysis failed: {0}".T(), ex.Message);
                IsScanning = false;
            });
        }
    }

    private (bool enabled, string status) CheckSecureBootStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key != null)
            {
                var val = key.GetValue("UEFISecureBootEnabled");
                if (val != null && Convert.ToInt32(val) == 1)
                {
                    return (true, "Secure Boot Active (UEFI)".T());
                }
            }
        }
        catch { }
        return (false, "Secure Boot Inactive or Unsupported".T());
    }

    private (bool ok, string status) CheckTpmStatus()
    {
        try
        {
            // Query TPM status using standard WMI namespace
            using var searcher = new ManagementObjectSearcher(@"root\cimv2\Security\MicrosoftTpm", "SELECT IsEnabled_InitialValue, SpecVersion FROM Win32_Tpm");
            using var collection = searcher.Get();
            foreach (ManagementObject obj in collection)
            {
                var ver = obj["SpecVersion"]?.ToString() ?? "2.0";
                return (true, string.Format("TPM v{0} Detected and Ready".T(), ver));
            }
        }
        catch { }
        return (false, "TPM Security Chip Not Detected or Disabled".T());
    }

    public async Task TogglePrivacySettingAsync(string type, bool enabled)
    {
        IsBusy = true;
        try
        {
            await Task.Run(() => _securityEngine.SetPrivacySetting(type, enabled));
            _dispatcherQueue.TryEnqueue(() =>
            {
                LoadPrivacySettings(); // Refresh
                IsBusy = false;
            });
        }
        catch
        {
            _dispatcherQueue.TryEnqueue(() => IsBusy = false);
        }
    }

    public async Task ClearClipboardAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing clipboard cache...".T();
        try
        {
            await Task.Run(() => _securityEngine.ClearClipboard());
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = "Clipboard history successfully cleared.".T();
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = string.Format("Failed to clear clipboard: {0}".T(), ex.Message);
                IsBusy = false;
            });
        }
    }

    public async Task ClearRecentFilesAsync()
    {
        IsBusy = true;
        StatusMessage = "Clearing Recent items and Run history...".T();
        try
        {
            await Task.Run(() => _securityEngine.ClearRecentFiles());
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = "Recent items and Explorer Run history successfully cleared.".T();
                IsBusy = false;
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = string.Format("Failed to clear recent files: {0}".T(), ex.Message);
                IsBusy = false;
            });
        }
    }
}

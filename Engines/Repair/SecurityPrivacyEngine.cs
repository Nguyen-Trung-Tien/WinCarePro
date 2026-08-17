using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Engines;

public class SecurityPrivacyEngine
{
    // ==================== SECURITY MONITORING ====================

    public string GetAntivirusStatus()
    {
        try
        {
            var list = WmiHelper.Query("SELECT displayName, productState FROM AntiVirusProduct", obj =>
            {
                string name = obj["displayName"]?.ToString() ?? "Unknown Antivirus";
                uint state = Convert.ToUInt32(obj["productState"]);
                uint middleByte = (state >> 8) & 0xFF;
                bool isEnabled = middleByte == 0x10 || middleByte == 0x11 || middleByte == 0x01;
                return $"{name} ({(isEnabled ? "Enabled" : "Disabled")})";
            }, @"root\SecurityCenter2");

            if (list.Count > 0) return string.Join(", ", list);
        }
        catch { }

        try
        {
            using var defenderKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender");
            var disableAntiSpyware = defenderKey?.GetValue("DisableAntiSpyware");
            if (disableAntiSpyware != null && Convert.ToInt32(disableAntiSpyware) == 1)
            {
                return "Microsoft Defender (Disabled)";
            }
            return "Microsoft Defender (Enabled)";
        }
        catch
        {
            return "Microsoft Defender (Running)";
        }
    }

    public bool GetFirewallStatus()
    {
        try
        {
            string[] profiles = { "DomainProfile", "StandardProfile", "PublicProfile" };
            bool allEnabled = true;

            foreach (var profile in profiles)
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{profile}");
                if (key != null)
                {
                    var val = key.GetValue("EnableFirewall");
                    if (val == null || Convert.ToInt32(val) == 0)
                    {
                        allEnabled = false;
                        break;
                    }
                }
                else
                {
                    allEnabled = false;
                }
            }
            return allEnabled;
        }
        catch
        {
            return false;
        }
    }

    public bool EnableAllFirewallProfiles()
    {
        bool success = true;
        try
        {
            string[] profiles = { "DomainProfile", "StandardProfile", "PublicProfile" };
            foreach (var profile in profiles)
            {
                using var key = Registry.LocalMachine.CreateSubKey($@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{profile}", true);
                key?.SetValue("EnableFirewall", 1, RegistryValueKind.DWord);
            }

            try
            {
                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall set allprofiles state on",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p?.WaitForExit(3000);
            }
            catch { }
            Database.DbManager.LogAction("Firewall: Enabled all firewall profiles", "Security Center", "Success");
        }
        catch (Exception ex)
        {
            success = false;
            Database.DbManager.LogAction($"Firewall Enable Failed: {ex.Message}", "Security Center", "Failed");
        }
        return success;
    }

    public bool GetUacStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key != null)
            {
                var val = key.GetValue("EnableLUA");
                return val != null && Convert.ToInt32(val) == 1;
            }
        }
        catch { }
        return true;
    }

    public bool EnableUac()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
            key?.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
            key?.SetValue("ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord);
            Database.DbManager.LogAction("Security: Restored User Account Control (UAC)", "Security Center", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Security: Failed to restore UAC: {ex.Message}", "Security Center", "Failed");
            return false;
        }
    }

    public bool GetDefenderRealtimeStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
            if (key != null)
            {
                var val = key.GetValue("DisableRealtimeMonitoring");
                if (val != null && Convert.ToInt32(val) == 1)
                {
                    return false;
                }
            }
        }
        catch { }
        return true;
    }

    public bool EnableDefenderRealtime()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", true);
            key?.SetValue("DisableRealtimeMonitoring", 0, RegistryValueKind.DWord);
            key?.DeleteValue("DisableBehaviorMonitoring", false);
            key?.DeleteValue("DisableOnAccessProtection", false);
            key?.DeleteValue("DisableScanOnRealtimeEnable", false);

            using var defKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender", true);
            defKey?.DeleteValue("DisableAntiSpyware", false);

            Database.DbManager.LogAction("Security: Enabled Windows Defender Real-Time Protection Policy", "Security Center", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Security: Failed to enable Defender Real-time policy: {ex.Message}", "Security Center", "Failed");
            return false;
        }
    }

    public string GetBitLockerStatus()
    {
        try
        {
            var list = WmiHelper.Query("SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume", obj =>
            {
                string letter = obj["DriveLetter"]?.ToString() ?? "";
                uint status = Convert.ToUInt32(obj["ProtectionStatus"]);
                string statusStr = status switch
                {
                    0 => "Off",
                    1 => "On",
                    _ => "Unknown"
                };
                return new { Letter = letter, StatusStr = statusStr };
            }, @"root\cimv2\Security\MicrosoftVolumeEncryption")
            .Where(x => !string.IsNullOrEmpty(x.Letter))
            .Select(x => $"{x.Letter} ({x.StatusStr})")
            .ToList();

            if (list.Count > 0) return string.Join(", ", list);
        }
        catch { }

        return "C: (Off) [Standard/Virtual OS]";
    }

    public (bool enabled, string status) CheckSecureBootStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key != null)
            {
                var val = key.GetValue("UEFISecureBootEnabled");
                if (val != null && Convert.ToInt32(val) == 1)
                {
                    return (true, "Secure Boot Active (UEFI)");
                }
            }
        }
        catch { }
        return (false, "Secure Boot Inactive or Legacy BIOS");
    }

    public (bool ok, string status) CheckTpmStatus()
    {
        try
        {
            var list = WmiHelper.Query("SELECT IsEnabled_InitialValue, SpecVersion FROM Win32_Tpm", obj =>
                obj["SpecVersion"]?.ToString() ?? "2.0", @"root\cimv2\Security\MicrosoftTpm");

            if (list.Count > 0)
            {
                return (true, $"TPM v{list[0]} Detected and Ready");
            }
        }
        catch { }
        return (false, "TPM Security Chip Not Detected or Disabled");
    }

    public List<string> RunSecurityAudits(List<StartupEntry>? startupEntries = null)
    {
        var issues = new List<string>();

        if (!GetDefenderRealtimeStatus())
        {
            issues.Add("Windows Defender Real-Time Protection is disabled in Policy!");
        }

        if (!GetFirewallStatus())
        {
            issues.Add("Windows Firewall is disabled!");
        }

        try
        {
            var startups = startupEntries ?? new StartupEngine().GetStartupEntries();
            foreach (var s in startups)
            {
                string cmd = s.Command.ToLower();
                if (cmd.Contains("cmd.exe") || cmd.Contains("powershell.exe") || cmd.Contains("wscript.exe") || cmd.Contains("mshta.exe") || cmd.Contains(@"\temp\") || cmd.Contains(@"\appdata\local\temp\"))
                {
                    issues.Add($"Suspicious startup program: {s.Name} runs shell command or runs from Temp!");
                }
            }
        }
        catch { }

        return issues;
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush? SafeBrush(byte a, byte r, byte g, byte b)
    {
        try
        {
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(a, r, g, b));
        }
        catch
        {
            return null;
        }
    }

    public List<SecurityAlertItem> RunSecurityAuditItems(List<StartupEntry>? startupEntries = null)
    {
        var issues = new List<SecurityAlertItem>();

        if (!GetDefenderRealtimeStatus())
        {
            issues.Add(new SecurityAlertItem
            {
                Id = "defender_realtime",
                Title = "Windows Defender Real-Time Protection Disabled in Policy",
                Description = "A group policy or registry tweak has turned off Defender real-time background scanning.",
                Category = "Antivirus",
                Severity = "Critical",
                FixActionKey = "defender_realtime",
                SeverityBrush = SafeBrush(255, 239, 68, 68)
            });
        }

        if (!GetFirewallStatus())
        {
            issues.Add(new SecurityAlertItem
            {
                Id = "firewall",
                Title = "Windows Firewall Profiles Inactive or Disabled",
                Description = "One or more network profiles (Domain, Private, Public) have Windows Firewall disabled.",
                Category = "Firewall",
                Severity = "Critical",
                FixActionKey = "firewall",
                SeverityBrush = SafeBrush(255, 239, 68, 68)
            });
        }

        if (!GetUacStatus())
        {
            issues.Add(new SecurityAlertItem
            {
                Id = "uac",
                Title = "User Account Control (UAC) is Disabled",
                Description = "Applications can execute with full administrative privileges without prompting.",
                Category = "Policy",
                Severity = "Warning",
                FixActionKey = "uac",
                SeverityBrush = SafeBrush(255, 245, 158, 11)
            });
        }

        var (sbOk, _) = CheckSecureBootStatus();
        if (!sbOk)
        {
            issues.Add(new SecurityAlertItem
            {
                Id = "secure_boot",
                Title = "UEFI Secure Boot is Inactive",
                Description = "Secure Boot is not active in UEFI BIOS firmware. Enable it in BIOS to protect against bootkits.",
                Category = "Hardware",
                Severity = "Info",
                FixActionKey = "",
                SeverityBrush = SafeBrush(255, 59, 130, 246)
            });
        }

        try
        {
            var startups = startupEntries ?? new StartupEngine().GetStartupEntries();
            foreach (var s in startups)
            {
                string cmd = s.Command.ToLower();
                if (cmd.Contains("cmd.exe") || cmd.Contains("powershell.exe") || cmd.Contains("wscript.exe") || cmd.Contains("mshta.exe") || cmd.Contains(@"\temp\") || cmd.Contains(@"\appdata\local\temp\"))
                {
                    issues.Add(new SecurityAlertItem
                    {
                        Id = $"startup_{s.Name}",
                        Title = $"Suspicious Startup Item: {s.Name}",
                        Description = $"Entry executes via command shell interpreter or runs from temporary directory: {s.Command}",
                        Category = "Startup",
                        Severity = "Warning",
                        FixActionKey = "",
                        SeverityBrush = SafeBrush(255, 245, 158, 11)
                    });
                }
            }
        }
        catch { }

        return issues;
    }

    public bool FixSecurityIssue(string fixKey)
    {
        return fixKey switch
        {
            "firewall" => EnableAllFirewallProfiles(),
            "defender_realtime" => EnableDefenderRealtime(),
            "uac" => EnableUac(),
            _ => false
        };
    }

    // ==================== UNIFIED SECURITY & PRIVACY SAFEGUARDS LIST ====================

    public List<SecuritySafeguardItem> GetAllSafeguards()
    {
        var list = new List<SecuritySafeguardItem>
        {
            // --- Category: System Safeguards ---
            new()
            {
                Id = "UAC_Enforce",
                Name = "User Account Control (UAC) Privilege Guard".T(),
                Description = "Enforce admin confirmation dialogs before applications can execute elevated privilege operations.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uE7EF",
                RegistryPath = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\EnableLUA",
                RecommendedValue = "1",
                FixKey = "uac"
            },
            new()
            {
                Id = "Defender_PolicyUnlock",
                Name = "Windows Defender Policy Lock Prevention".T(),
                Description = "Ensure group policies cannot be hijacked by malware to disable Defender real-time scanning.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uE73A",
                RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableRealtimeMonitoring",
                RecommendedValue = "0",
                FixKey = "defender_realtime"
            },
            new()
            {
                Id = "Firewall_Enforce",
                Name = "Windows Defender Firewall Enforcer".T(),
                Description = "Activate stateful packet inspection on Domain, Private, and Public network boundaries.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uE83D",
                RegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy",
                RecommendedValue = "1",
                FixKey = "firewall"
            },
            new()
            {
                Id = "Block_WSH",
                Name = "Block Windows Script Host (WSH Attacks)".T(),
                Description = "Block execution of standalone .vbs and .js scripts commonly used in phishing droppers.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uEDA2",
                RegistryPath = @"HKLM\SOFTWARE\Microsoft\Windows Script Host\Settings\Enabled",
                RecommendedValue = "0",
                FixKey = "wsh"
            },
            new()
            {
                Id = "RDP_NLA",
                Name = "Enforce Remote Desktop NLA (Network Level Auth)".T(),
                Description = "Require cryptographic pre-authentication before establishing RDP sessions to prevent BlueKeep-style exploits.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uE836",
                RegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp\UserAuthentication",
                RecommendedValue = "1",
                FixKey = "rdp_nla"
            },
            new()
            {
                Id = "Disable_AutoShare",
                Name = "Disable Administrative Auto-Shares (IPC$, Admin$)".T(),
                Description = "Prevent default hidden network share creation across local partitions.".T(),
                Category = "System Safeguards".T(),
                IconGlyph = "\uE71D",
                RegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters\AutoShareWks",
                RecommendedValue = "0",
                FixKey = "autoshare"
            },

            // --- Category: Privacy & Anti-Telemetry ---
            new()
            {
                Id = "Privacy_Telemetry",
                Name = "Diagnostic Telemetry Data Collection".T(),
                Description = "Restrict system diagnostic data transmission to Microsoft down to Security-only levels.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE9D9",
                RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowTelemetry",
                RecommendedValue = "0",
                FixKey = "telemetry"
            },
            new()
            {
                Id = "Privacy_AdvertisingID",
                Name = "Personalized Advertising Identifier".T(),
                Description = "Block apps from tracking usage behaviors and serving targeted advertisements.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE719",
                RegistryPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled",
                RecommendedValue = "0",
                FixKey = "advertisingid"
            },
            new()
            {
                Id = "Privacy_BingSearch",
                Name = "Bing Search Integration in Start Menu".T(),
                Description = "Disable Bing web query transmission and Cortana web suggestions in Windows search bar.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE721",
                RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\DisableSearchBoxSuggestions",
                RecommendedValue = "1",
                FixKey = "cortanabing"
            },
            new()
            {
                Id = "Privacy_TypingTracking",
                Name = "Personalization & Typing Input Tracking".T(),
                Description = "Prevent Windows from logging keyboard typing and handwriting personalization telemetry.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE765",
                RegistryPath = @"HKCU\Software\Microsoft\InputPersonalization\RestrictImplicitConsent",
                RecommendedValue = "1",
                FixKey = "tracking"
            },
            new()
            {
                Id = "Privacy_LocationSensors",
                Name = "Background Geolocation Sensor Access".T(),
                Description = "Block background apps and location providers from polling physical hardware GPS sensors.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE707",
                RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocation",
                RecommendedValue = "1",
                FixKey = "location"
            },
            new()
            {
                Id = "Privacy_FeedbackPrompts",
                Name = "Windows Feedback Request Prompts".T(),
                Description = "Set Windows customer experience feedback prompts to 'Never'.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE939",
                RegistryPath = @"HKCU\Software\Microsoft\Siuf\Rules\NumberOfSIUFInPeriod",
                RecommendedValue = "0",
                FixKey = "feedback"
            },
            new()
            {
                Id = "Privacy_AppDiagnostics",
                Name = "App Diagnostics Cross-Access".T(),
                Description = "Deny third-party Store applications from reading diagnostic states of other installed software.".T(),
                Category = "Privacy & Anti-Telemetry".T(),
                IconGlyph = "\uE7BA",
                RegistryPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsGetDiagnosticInfo",
                RecommendedValue = "2",
                FixKey = "appdiagnostics"
            },

            // --- Category: Trace Eradication ---
            new()
            {
                Id = "Trace_Clipboard",
                Name = "Clipboard History Memory Cache".T(),
                Description = "Prevent clipboard history from retaining copied credentials across system reboots.".T(),
                Category = "Trace Eradication".T(),
                IconGlyph = "\uE77F",
                RegistryPath = @"HKCU\Software\Microsoft\Clipboard\Enabled",
                RecommendedValue = "0",
                FixKey = "clipboardhistory"
            },
            new()
            {
                Id = "Trace_ExplorerRecent",
                Name = "Explorer Recent Files Tracking".T(),
                Description = "Clear and prevent temporary document shortcut traces in Windows Explorer Recent items.".T(),
                Category = "Trace Eradication".T(),
                IconGlyph = "\uE81C",
                RegistryPath = @"%AppData%\Microsoft\Windows\Recent",
                RecommendedValue = "Cleaned",
                FixKey = "recent_clean"
            },
            new()
            {
                Id = "Trace_RunMRU",
                Name = "Explorer Run Dialog MRU History".T(),
                Description = "Purge execution history and parameters entered in Windows Run (Win+R) dialog.".T(),
                Category = "Trace Eradication".T(),
                IconGlyph = "\uE756",
                RegistryPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU",
                RecommendedValue = "Cleaned",
                FixKey = "run_clean"
            }
        };

        foreach (var item in list)
        {
            RefreshSafeguard(item);
        }

        return list;
    }

    public void RefreshSafeguard(SecuritySafeguardItem item)
    {
        try
        {
            switch (item.Id)
            {
                case "UAC_Enforce":
                    bool uac = GetUacStatus();
                    item.CurrentValue = uac ? "1" : "0";
                    item.IsProtected = uac;
                    item.ComparisonText = string.Format("Current: {0} | Target: Enabled (1)".T(), uac ? "Enabled".T() : "Disabled".T());
                    break;

                case "Defender_PolicyUnlock":
                    bool def = GetDefenderRealtimeStatus();
                    item.CurrentValue = def ? "0" : "1";
                    item.IsProtected = def;
                    item.ComparisonText = string.Format("Current: {0} | Target: Protected (0)".T(), def ? "Unlocked".T() : "Disabled by Policy".T());
                    break;

                case "Firewall_Enforce":
                    bool fw = GetFirewallStatus();
                    item.CurrentValue = fw ? "1" : "0";
                    item.IsProtected = fw;
                    item.ComparisonText = string.Format("Current: {0} | Target: Active (1)".T(), fw ? "Active".T() : "Disabled".T());
                    break;

                case "Block_WSH":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Script Host\Settings"))
                    {
                        var val = key?.GetValue("Enabled");
                        bool blocked = val != null && Convert.ToInt32(val) == 0;
                        item.CurrentValue = blocked ? "0" : "1";
                        item.IsProtected = blocked;
                        item.ComparisonText = string.Format("Current: {0} | Target: Blocked (0)".T(), blocked ? "Blocked".T() : "Allowed".T());
                    }
                    break;

                case "RDP_NLA":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp"))
                    {
                        var val = key?.GetValue("UserAuthentication");
                        bool nla = val != null && Convert.ToInt32(val) == 1;
                        item.CurrentValue = nla ? "1" : "0";
                        item.IsProtected = nla;
                        item.ComparisonText = string.Format("Current: {0} | Target: Enforced (1)".T(), nla ? "Enforced".T() : "Open".T());
                    }
                    break;

                case "Disable_AutoShare":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"))
                    {
                        var val = key?.GetValue("AutoShareWks");
                        bool disabled = val != null && Convert.ToInt32(val) == 0;
                        item.CurrentValue = disabled ? "0" : "1";
                        item.IsProtected = disabled;
                        item.ComparisonText = string.Format("Current: {0} | Target: Disabled (0)".T(), disabled ? "Disabled".T() : "Enabled".T());
                    }
                    break;

                case "Privacy_Telemetry":
                    bool tele = GetPrivacySetting("telemetry");
                    item.CurrentValue = tele ? "3" : "0";
                    item.IsProtected = !tele;
                    item.ComparisonText = string.Format("Current: {0} | Target: Security-Only (0)".T(), tele ? "Full Telemetry".T() : "Security-Only".T());
                    break;

                case "Privacy_AdvertisingID":
                    bool ad = GetPrivacySetting("advertisingid");
                    item.CurrentValue = ad ? "1" : "0";
                    item.IsProtected = !ad;
                    item.ComparisonText = string.Format("Current: {0} | Target: Disabled (0)".T(), ad ? "Enabled".T() : "Disabled".T());
                    break;

                case "Privacy_BingSearch":
                    bool bing = GetPrivacySetting("cortanabing");
                    item.CurrentValue = bing ? "1" : "0";
                    item.IsProtected = bing;
                    item.ComparisonText = string.Format("Current: {0} | Target: Disabled (1)".T(), bing ? "Disabled".T() : "Enabled".T());
                    break;

                case "Privacy_TypingTracking":
                    bool track = GetPrivacySetting("tracking");
                    item.CurrentValue = track ? "1" : "0";
                    item.IsProtected = track;
                    item.ComparisonText = string.Format("Current: {0} | Target: Restricted (1)".T(), track ? "Restricted".T() : "Allowed".T());
                    break;

                case "Privacy_LocationSensors":
                    bool loc = GetPrivacySetting("location");
                    item.CurrentValue = loc ? "1" : "0";
                    item.IsProtected = loc;
                    item.ComparisonText = string.Format("Current: {0} | Target: Disabled (1)".T(), loc ? "Disabled".T() : "Allowed".T());
                    break;

                case "Privacy_FeedbackPrompts":
                    bool fb = GetPrivacySetting("feedback");
                    item.CurrentValue = fb ? "0" : "1";
                    item.IsProtected = fb;
                    item.ComparisonText = string.Format("Current: {0} | Target: Never (0)".T(), fb ? "Never".T() : "Prompt".T());
                    break;

                case "Privacy_AppDiagnostics":
                    bool diag = GetPrivacySetting("appdiagnostics");
                    item.CurrentValue = diag ? "2" : "0";
                    item.IsProtected = diag;
                    item.ComparisonText = string.Format("Current: {0} | Target: Deny (2)".T(), diag ? "Denied".T() : "Allowed".T());
                    break;

                case "Trace_Clipboard":
                    bool clip = GetPrivacySetting("clipboardhistory");
                    item.CurrentValue = clip ? "1" : "0";
                    item.IsProtected = !clip;
                    item.ComparisonText = string.Format("Current: {0} | Target: Disabled (0)".T(), clip ? "Active".T() : "Disabled".T());
                    break;

                case "Trace_ExplorerRecent":
                    string recentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent");
                    int count = Directory.Exists(recentFolder) ? Directory.GetFiles(recentFolder).Length : 0;
                    item.CurrentValue = count.ToString();
                    item.IsProtected = count == 0;
                    item.ComparisonText = string.Format("Cached Files: {0} | Target: Clean (0)".T(), count);
                    break;

                case "Trace_RunMRU":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"))
                    {
                        int mruCount = key?.ValueCount ?? 0;
                        item.CurrentValue = mruCount.ToString();
                        item.IsProtected = mruCount == 0;
                        item.ComparisonText = string.Format("Cached Commands: {0} | Target: Clean (0)".T(), mruCount);
                    }
                    break;
            }
        }
        catch { }
        item.NotifyStatusChanged();
    }

    public bool ApplySafeguard(SecuritySafeguardItem item)
    {
        try
        {
            switch (item.Id)
            {
                case "UAC_Enforce":
                    return EnableUac();

                case "Defender_PolicyUnlock":
                    return EnableDefenderRealtime();

                case "Firewall_Enforce":
                    return EnableAllFirewallProfiles();

                case "Block_WSH":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows Script Host\Settings", true))
                    {
                        key.SetValue("Enabled", 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction("Security: Blocked Windows Script Host", "Security Center", "Success");
                        return true;
                    }

                case "RDP_NLA":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
                    {
                        key.SetValue("UserAuthentication", 1, RegistryValueKind.DWord);
                        Database.DbManager.LogAction("Security: Enforced RDP NLA Authentication", "Security Center", "Success");
                        return true;
                    }

                case "Disable_AutoShare":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", true))
                    {
                        key.SetValue("AutoShareWks", 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction("Security: Disabled Administrative Auto-Shares", "Security Center", "Success");
                        return true;
                    }

                case "Privacy_Telemetry":
                    return SetPrivacySetting("telemetry", false);

                case "Privacy_AdvertisingID":
                    return SetPrivacySetting("advertisingid", false);

                case "Privacy_BingSearch":
                    return SetPrivacySetting("cortanabing", true);

                case "Privacy_TypingTracking":
                    return SetPrivacySetting("tracking", true);

                case "Privacy_LocationSensors":
                    return SetPrivacySetting("location", true);

                case "Privacy_FeedbackPrompts":
                    return SetPrivacySetting("feedback", true);

                case "Privacy_AppDiagnostics":
                    return SetPrivacySetting("appdiagnostics", true);

                case "Trace_Clipboard":
                    ClearClipboard();
                    return SetPrivacySetting("clipboardhistory", false);

                case "Trace_ExplorerRecent":
                    ClearRecentFiles();
                    ClearExplorerJumpLists();
                    return true;

                case "Trace_RunMRU":
                    ClearExplorerRunHistory();
                    ClearTypedPathsHistory();
                    return true;
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Apply Safeguard {item.Id} failed: {ex.Message}", "Security Center", "Failed");
        }
        return false;
    }

    public bool RevertSafeguard(SecuritySafeguardItem item)
    {
        try
        {
            switch (item.Id)
            {
                case "UAC_Enforce":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
                    {
                        key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                        return true;
                    }

                case "Block_WSH":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows Script Host\Settings", true))
                    {
                        key.SetValue("Enabled", 1, RegistryValueKind.DWord);
                        return true;
                    }

                case "RDP_NLA":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
                    {
                        key.SetValue("UserAuthentication", 0, RegistryValueKind.DWord);
                        return true;
                    }

                case "Disable_AutoShare":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", true))
                    {
                        key.SetValue("AutoShareWks", 1, RegistryValueKind.DWord);
                        return true;
                    }

                case "Privacy_Telemetry":
                    return SetPrivacySetting("telemetry", true);

                case "Privacy_AdvertisingID":
                    return SetPrivacySetting("advertisingid", true);

                case "Privacy_BingSearch":
                    return SetPrivacySetting("cortanabing", false);

                case "Privacy_TypingTracking":
                    return SetPrivacySetting("tracking", false);

                case "Privacy_LocationSensors":
                    return SetPrivacySetting("location", false);

                case "Privacy_FeedbackPrompts":
                    return SetPrivacySetting("feedback", false);

                case "Privacy_AppDiagnostics":
                    return SetPrivacySetting("appdiagnostics", false);

                case "Trace_Clipboard":
                    return SetPrivacySetting("clipboardhistory", true);
            }
        }
        catch { }
        return false;
    }

    // ==================== PRIVACY CONTROLS & REGISTRY TOGGLES ====================

    public bool GetPrivacySetting(string type)
    {
        try
        {
            switch (type.ToLower())
            {
                case "advertisingid":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                    {
                        return Convert.ToInt32(key?.GetValue("Enabled") ?? 1) == 1;
                    }
                case "telemetry":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                    {
                        var val = key?.GetValue("AllowTelemetry");
                        return val == null || Convert.ToInt32(val) > 0;
                    }
                case "clipboardhistory":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
                    {
                        return Convert.ToInt32(key?.GetValue("Enabled") ?? 0) == 1;
                    }
                case "tracking":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\InputPersonalization"))
                    {
                        return Convert.ToInt32(key?.GetValue("RestrictImplicitConsent") ?? 0) == 1;
                    }
                case "cortanabing":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                    {
                        return Convert.ToInt32(key?.GetValue("DisableSearchBoxSuggestions") ?? 0) == 1;
                    }
                case "location":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"))
                    {
                        return Convert.ToInt32(key?.GetValue("DisableLocation") ?? 0) == 1;
                    }
                case "feedback":
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Siuf\Rules"))
                    {
                        return Convert.ToInt32(key?.GetValue("NumberOfSIUFInPeriod") ?? 1) == 0;
                    }
                case "appdiagnostics":
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                    {
                        return Convert.ToInt32(key?.GetValue("LetAppsGetDiagnosticInfo") ?? 0) == 2;
                    }
            }
        }
        catch { }
        return false;
    }

    public bool SetPrivacySetting(string type, bool enabled)
    {
        try
        {
            int val = enabled ? 1 : 0;
            switch (type.ToLower())
            {
                case "advertisingid":
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", true))
                    {
                        key.SetValue("Enabled", val, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set AdvertisingID to {enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "telemetry":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        key.SetValue("AllowTelemetry", enabled ? 3 : 0, RegistryValueKind.DWord);
                        
                        using var diagnosticKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", true);
                        diagnosticKey?.SetValue("AllowTelemetry", enabled ? 3 : 0, RegistryValueKind.DWord);
                        
                        Database.DbManager.LogAction($"Privacy: Set Telemetry tracking to {enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "clipboardhistory":
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Clipboard", true))
                    {
                        key.SetValue("Enabled", val, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Clipboard History to {enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "tracking":
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\InputPersonalization", true))
                    {
                        key.SetValue("RestrictImplicitConsent", enabled ? 1 : 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Input Tracking to {enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "cortanabing":
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer", true))
                    {
                        key.SetValue("DisableSearchBoxSuggestions", enabled ? 1 : 0, RegistryValueKind.DWord);
                    }
                    using (var key2 = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search", true))
                    {
                        key2.SetValue("BingSearchEnabled", enabled ? 0 : 1, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Cortana/Bing Search Suggestions to {!enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "location":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", true))
                    {
                        key.SetValue("DisableLocation", enabled ? 1 : 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Background Location to {!enabled}", "Privacy Center", "Success");
                        return true;
                    }
                case "feedback":
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Siuf\Rules", true))
                    {
                        key.SetValue("NumberOfSIUFInPeriod", enabled ? 0 : 1, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Windows Feedback frequency to {(enabled ? "Never" : "Default")}", "Privacy Center", "Success");
                        return true;
                    }
                case "appdiagnostics":
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", true))
                    {
                        key.SetValue("LetAppsGetDiagnosticInfo", enabled ? 2 : 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set App Diagnostic Access to {!enabled}", "Privacy Center", "Success");
                        return true;
                    }
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Set Privacy Setting {type} failed: {ex.Message}", "Privacy Center", "Failed");
        }
        return false;
    }

    public int ApplyPrivacyPreset(string preset)
    {
        int count = 0;
        if (preset == "max")
        {
            if (SetPrivacySetting("advertisingid", false)) count++;
            if (SetPrivacySetting("telemetry", false)) count++;
            if (SetPrivacySetting("tracking", true)) count++;
            if (SetPrivacySetting("cortanabing", true)) count++;
            if (SetPrivacySetting("location", true)) count++;
            if (SetPrivacySetting("feedback", true)) count++;
            if (SetPrivacySetting("appdiagnostics", true)) count++;
        }
        else if (preset == "balanced")
        {
            if (SetPrivacySetting("advertisingid", false)) count++;
            if (SetPrivacySetting("tracking", true)) count++;
            if (SetPrivacySetting("cortanabing", true)) count++;
            if (SetPrivacySetting("feedback", true)) count++;
        }
        else if (preset == "default")
        {
            if (SetPrivacySetting("advertisingid", true)) count++;
            if (SetPrivacySetting("telemetry", true)) count++;
            if (SetPrivacySetting("tracking", false)) count++;
            if (SetPrivacySetting("cortanabing", false)) count++;
            if (SetPrivacySetting("location", false)) count++;
            if (SetPrivacySetting("feedback", false)) count++;
            if (SetPrivacySetting("appdiagnostics", false)) count++;
        }
        return count;
    }

    // ==================== ACTIVITY TRACES & PRIVACY CLEANER ====================

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    public bool ClearClipboard()
    {
        try
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    EmptyClipboard();
                }
                finally
                {
                    CloseClipboard();
                }
                Database.DbManager.LogAction("Cleared Clipboard Memory", "Privacy Center", "Success");
                return true;
            }
        }
        catch { }
        return false;
    }

    public int ClearRecentFiles()
    {
        int deleted = 0;
        try
        {
            string recentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent");
            if (Directory.Exists(recentFolder))
            {
                foreach (var file in Directory.GetFiles(recentFolder))
                {
                    try { File.Delete(file); deleted++; } catch { }
                }
            }
            Database.DbManager.LogAction($"Cleared {deleted} Explorer Recent files", "Privacy Center", "Success");
        }
        catch { }
        return deleted;
    }

    public int ClearExplorerJumpLists()
    {
        int deleted = 0;
        try
        {
            string recentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent");
            string autoDest = Path.Combine(recentFolder, "AutomaticDestinations");
            string customDest = Path.Combine(recentFolder, "CustomDestinations");

            if (Directory.Exists(autoDest))
            {
                foreach (var f in Directory.GetFiles(autoDest))
                {
                    try { File.Delete(f); deleted++; } catch { }
                }
            }
            if (Directory.Exists(customDest))
            {
                foreach (var f in Directory.GetFiles(customDest))
                {
                    try { File.Delete(f); deleted++; } catch { }
                }
            }
            Database.DbManager.LogAction($"Cleared {deleted} Quick Access JumpList destinations", "Privacy Center", "Success");
        }
        catch { }
        return deleted;
    }

    public int ClearExplorerRunHistory()
    {
        int deleted = 0;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true);
            if (key != null)
            {
                foreach (var val in key.GetValueNames())
                {
                    try { key.DeleteValue(val, false); deleted++; } catch { }
                }
            }
            Database.DbManager.LogAction($"Cleared {deleted} Explorer Run MRU entries", "Privacy Center", "Success");
        }
        catch { }
        return deleted;
    }

    public int ClearTypedPathsHistory()
    {
        int deleted = 0;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths", true);
            if (key != null)
            {
                foreach (var val in key.GetValueNames())
                {
                    try { key.DeleteValue(val, false); deleted++; } catch { }
                }
            }
            Database.DbManager.LogAction($"Cleared {deleted} Explorer Typed Paths history", "Privacy Center", "Success");
        }
        catch { }
        return deleted;
    }

    public int ClearAllActivityTraces()
    {
        int total = 0;
        ClearClipboard();
        total += ClearRecentFiles();
        total += ClearExplorerJumpLists();
        total += ClearExplorerRunHistory();
        total += ClearTypedPathsHistory();
        return total;
    }
}

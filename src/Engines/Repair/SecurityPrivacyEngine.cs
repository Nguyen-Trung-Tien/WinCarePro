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

public partial class SecurityPrivacyEngine
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

    public (bool domain, bool @private, bool @public) GetFirewallProfilesStatus()
    {
        bool domain = IsFirewallProfileEnabled("DomainProfile");
        bool priv = IsFirewallProfileEnabled("StandardProfile");
        bool pub = IsFirewallProfileEnabled("PublicProfile");
        return (domain, priv, pub);
    }

    private bool IsFirewallProfileEnabled(string profile)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{profile}");
            if (key != null)
            {
                var val = key.GetValue("EnableFirewall");
                return val != null && Convert.ToInt32(val) != 0;
            }
        }
        catch { }
        return false;
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
                _ = ProcessRunner.RunHiddenAsync("netsh.exe", new[] { "advfirewall", "set", "allprofiles", "state", "on" }, 3);
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

    public (bool enabled, int consentLevel, string description) GetUacLevelDetails()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key != null)
            {
                var enableLua = key.GetValue("EnableLUA");
                bool isLua = enableLua != null && Convert.ToInt32(enableLua) == 1;
                if (!isLua)
                {
                    return (false, 0, "UAC Disabled (Never notify)");
                }

                var prompt = key.GetValue("ConsentPromptBehaviorAdmin");
                int promptVal = prompt != null ? Convert.ToInt32(prompt) : 5;
                string desc = promptVal switch
                {
                    0 => "Elevate without prompting (Insecure)",
                    1 => "Prompt for credentials on secure desktop",
                    2 => "Always notify on secure desktop",
                    5 => "Notify for non-Windows apps (Default)",
                    _ => $"Custom Policy (Level {promptVal})"
                };
                return (true, promptVal, desc);
            }
        }
        catch { }
        return (true, 5, "Notify for non-Windows apps (Default)");
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

    public (bool ok, string status) CheckCoreIsolationStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
            if (key != null)
            {
                var val = key.GetValue("Enabled");
                if (val is int intVal && intVal == 1)
                {
                    return (true, "Memory Integrity (HVCI) Active");
                }
            }
        }
        catch { }
        return (false, "Memory Integrity (HVCI) Inactive");
    }

    public (bool ok, string status) CheckTamperProtectionStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features");
            if (key != null)
            {
                var val = key.GetValue("TamperProtection");
                if (val is int intVal && (intVal == 5 || intVal == 1))
                {
                    return (true, "Tamper Protection Enabled");
                }
            }
        }
        catch { }
        return (false, "Tamper Protection Inactive");
    }

    public (bool ok, string status) CheckRdpNlaStatus()
    {
        try
        {
            // First verify if Remote Desktop is actually enabled on the host
            using var tsKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server");
            if (tsKey != null)
            {
                var fDeny = tsKey.GetValue("fDenyTSConnections");
                if (fDeny == null || Convert.ToInt32(fDeny) != 0)
                {
                    // RDP is disabled: zero remote exposure, completely secure
                    return (true, "Remote Desktop Disabled (Secure)");
                }
            }

            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp");
            if (key != null)
            {
                var val = key.GetValue("UserAuthentication");
                if (val is int intVal && intVal == 1)
                {
                    return (true, "RDP Active with NLA Enforced");
                }
                return (false, "RDP Active without NLA Authentication");
            }
        }
        catch { }
        return (true, "Remote Desktop Disabled (Secure)");
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
                FixActionKey = "defender_realtime"
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
                FixActionKey = "firewall"
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
                FixActionKey = "uac"
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
                FixActionKey = ""
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
                        FixActionKey = ""
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

}

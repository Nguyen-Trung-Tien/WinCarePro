using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinCarePro.Engines;

public class SecurityPrivacyEngine
{
    // Security Monitoring
    public string GetAntivirusStatus()
    {
        try
        {
            // SecurityCenter2 namespace is standard for Antivirus listings in Windows Vista/7/8/10/11
            using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT displayName, productState FROM AntiVirusProduct");
            using var collection = searcher.Get();
            var list = new List<string>();

            foreach (ManagementObject obj in collection)
            {
                string name = obj["displayName"]?.ToString() ?? "Unknown Antivirus";
                uint state = Convert.ToUInt32(obj["productState"]);
                
                // productState is a bitmask. The middle byte represents whether active:
                // e.g. state & 0x00001000 = active, 0x00000000 = inactive.
                // A simpler heuristic: if the second hex digit is 1 (e.g. 0x11000 or 0x10100), it's enabled.
                string hexState = state.ToString("X6");
                bool isEnabled = hexState.Substring(1, 2) == "10" || hexState.Substring(1, 2) == "11" || hexState.Substring(1, 2) == "01";

                list.Add($"{name} ({(isEnabled ? "Enabled" : "Disabled")})");
            }

            if (list.Count > 0) return string.Join(", ", list);
        }
        catch { }

        // Fallback: Check if Defender service is running
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
            // Check domain, standard, and public profiles
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

    public string GetBitLockerStatus()
    {
        try
        {
            // Query BitLocker status via WMI
            using var searcher = new ManagementObjectSearcher(@"root\cimv2\Security\MicrosoftVolumeEncryption", "SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume");
            using var collection = searcher.Get();
            var list = new List<string>();

            foreach (var obj in collection)
            {
                string letter = obj["DriveLetter"]?.ToString() ?? "";
                uint status = Convert.ToUInt32(obj["ProtectionStatus"]);
                string statusStr = status switch
                {
                    0 => "Off",
                    1 => "On",
                    _ => "Unknown"
                };
                if (!string.IsNullOrEmpty(letter))
                {
                    list.Add($"{letter} ({statusStr})");
                }
            }

            if (list.Count > 0) return string.Join(", ", list);
        }
        catch { }

        return "C: (Off) [Unmanaged/Virtual OS]";
    }

    // Security Checks
    public List<string> RunSecurityAudits()
    {
        var issues = new List<string>();

        // Check if Windows Defender real-time monitoring is disabled
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
            if (key != null)
            {
                if (Convert.ToInt32(key.GetValue("DisableRealtimeMonitoring") ?? 0) == 1)
                {
                    issues.Add("Windows Defender Real-Time Protection is disabled in Policy!");
                }
            }
        }
        catch { }

        // Check suspicious startups
        try
        {
            var startupEng = new StartupEngine();
            var startups = startupEng.GetStartupEntries();
            foreach (var s in startups)
            {
                string cmd = s.Command.ToLower();
                if (cmd.Contains("cmd.exe") || cmd.Contains("powershell.exe") || cmd.Contains("wscript.exe") || cmd.Contains("mshta.exe") || cmd.Contains("temp"))
                {
                    issues.Add($"Suspicious startup program: {s.Name} runs shell command or runs from Temp!");
                }
            }
        }
        catch { }

        return issues;
    }

    // Privacy Controls - Registry Toggles
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
                        // 0 = Security, 1 = Basic, 2 = Enhanced, 3 = Full. Policies often block/force 0 to disable
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
                        // Telemetry policies: 0 = Security-only/Off, 3 = Full
                        key.SetValue("AllowTelemetry", enabled ? 3 : 0, RegistryValueKind.DWord);
                        
                        // Also apply to setup services
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
                        // 1 = restrict/disable implicit tracking, 0 = allow
                        key.SetValue("RestrictImplicitConsent", enabled ? 1 : 0, RegistryValueKind.DWord);
                        Database.DbManager.LogAction($"Privacy: Set Input Tracking to {enabled}", "Privacy Center", "Success");
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    public void ClearClipboard()
    {
        try
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                CloseClipboard();
                Database.DbManager.LogAction("Cleared Clipboard History", "Privacy Center", "Success");
            }
        }
        catch { }
    }

    public void ClearRecentFiles()
    {
        try
        {
            string recentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent");
            if (Directory.Exists(recentFolder))
            {
                foreach (var file in Directory.GetFiles(recentFolder))
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (var dir in Directory.GetDirectories(recentFolder))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }

            // Clear Run command history
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true))
            {
                if (key != null)
                {
                    foreach (var val in key.GetValueNames())
                    {
                        key.DeleteValue(val, false);
                    }
                }
            }

            Database.DbManager.LogAction("Cleared Recent Files & Run MRU History", "Privacy Center", "Success");
        }
        catch { }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class UninstallEngine
{
    public List<InstalledAppInfo> ScanInstalledApps()
    {
        var appList = new List<InstalledAppInfo>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        string[] registryPaths = 
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };
        
        var hives = new[] 
        {
            (Registry.LocalMachine, "HKLM"),
            (Registry.CurrentUser, "HKCU")
        };
        
        foreach (var (baseKey, hiveName) in hives)
        {
            foreach (var path in registryPaths)
            {
                try
                {
                    using var uninstallKey = baseKey.OpenSubKey(path);
                    if (uninstallKey == null) continue;
                    
                    var subkeys = uninstallKey.GetSubKeyNames();
                    foreach (var subkeyName in subkeys)
                    {
                        try
                        {
                            using var subkey = uninstallKey.OpenSubKey(subkeyName);
                            if (subkey == null) continue;
                            
                            var displayName = subkey.GetValue("DisplayName")?.ToString();
                            if (string.IsNullOrWhiteSpace(displayName)) continue;
                            
                            var systemComponent = subkey.GetValue("SystemComponent");
                            if (systemComponent != null && Convert.ToInt32(systemComponent) == 1) continue;
                            
                            var parentKeyName = subkey.GetValue("ParentKeyName")?.ToString();
                            if (!string.IsNullOrEmpty(parentKeyName)) continue;
                            
                            var uninstallString = subkey.GetValue("UninstallString")?.ToString();
                            if (string.IsNullOrWhiteSpace(uninstallString)) continue;
                            
                            var publisher = subkey.GetValue("Publisher")?.ToString() ?? "Unknown Publisher";
                            var version = subkey.GetValue("DisplayVersion")?.ToString() ?? "Unknown";
                            var installDateRaw = subkey.GetValue("InstallDate")?.ToString() ?? "";
                            var installLocation = subkey.GetValue("InstallLocation")?.ToString() ?? "";
                            var displayIcon = subkey.GetValue("DisplayIcon")?.ToString() ?? "";
                            
                            long sizeBytes = 0;
                            var estimatedSizeVal = subkey.GetValue("EstimatedSize");
                            if (estimatedSizeVal != null)
                            {
                                if (long.TryParse(estimatedSizeVal.ToString(), out long sizeKb))
                                {
                                    sizeBytes = sizeKb * 1024;
                                }
                            }
                            
                            string installDate = "";
                            if (!string.IsNullOrEmpty(installDateRaw) && installDateRaw.Length == 8)
                            {
                                installDate = $"{installDateRaw.Substring(0, 4)}-{installDateRaw.Substring(4, 2)}-{installDateRaw.Substring(6, 2)}";
                            }
                            else if (!string.IsNullOrEmpty(installDateRaw) && DateTime.TryParse(installDateRaw, out var parsedDt))
                            {
                                installDate = parsedDt.ToString("yyyy-MM-dd");
                            }

                            if (sizeBytes == 0 && !string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
                            {
                                try
                                {
                                    sizeBytes = GetDirectorySize(installLocation);
                                    if (string.IsNullOrEmpty(installDate))
                                    {
                                        installDate = Directory.GetCreationTime(installLocation).ToString("yyyy-MM-dd");
                                    }
                                }
                                catch {}
                            }
                            
                            string iconPath = "";
                            if (!string.IsNullOrWhiteSpace(displayIcon))
                            {
                                iconPath = ExtractIconFile(displayIcon, subkeyName);
                            }
                            
                            var appInfo = new InstalledAppInfo
                            {
                                DisplayName = displayName,
                                Publisher = publisher,
                                Version = version,
                                InstallDate = installDate,
                                InstallLocation = installLocation,
                                UninstallString = uninstallString,
                                RegistryKeyName = subkeyName,
                                Hive = hiveName,
                                RegistryPath = Path.Combine(path, subkeyName),
                                SizeBytes = sizeBytes,
                                DisplayIcon = displayIcon,
                                IsStoreApp = false,
                                IconPath = iconPath
                            };
                            
                            if (seenNames.Add(displayName + "_" + version))
                            {
                                appList.Add(appInfo);
                            }
                        }
                        catch {}
                    }
                }
                catch {}
            }
        }
        
        // 2. Scan Microsoft Store (packaged) applications
        try
        {
            var packageManager = new Windows.Management.Deployment.PackageManager();
            var packages = packageManager.FindPackagesForUser("");
            
            foreach (var package in packages)
            {
                try
                {
                    if (package.IsFramework || package.IsResourcePackage) 
                        continue;
                    
                    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                        continue;

                    var appEntries = package.GetAppListEntries();
                    if (appEntries == null || appEntries.Count == 0) 
                        continue;
                    
                    string displayName = package.DisplayName;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = appEntries[0].DisplayInfo.DisplayName;
                    }
                    if (string.IsNullOrWhiteSpace(displayName)) continue;
                    
                    if (displayName.StartsWith("Microsoft.") || displayName.StartsWith("Windows."))
                    {
                        if (package.SignatureKind == Windows.ApplicationModel.PackageSignatureKind.System) continue;
                    }
                    
                    string publisher = package.PublisherDisplayName;
                    if (string.IsNullOrWhiteSpace(publisher))
                    {
                        publisher = "Microsoft Corporation";
                    }
                    
                    var v = package.Id.Version;
                    string version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                    
                    string installLocation = "";
                    try
                    {
                        installLocation = package.InstalledLocation?.Path ?? "";
                    }
                    catch {}
                    
                    string iconPath = "";
                    try
                    {
                        iconPath = package.Logo?.ToString() ?? "";
                    }
                    catch {}
                    
                    var appInfo = new InstalledAppInfo
                    {
                        DisplayName = displayName,
                        Publisher = publisher,
                        Version = version,
                        InstallLocation = installLocation,
                        UninstallString = package.Id.FullName, // Store FullName for uninstall
                        RegistryKeyName = package.Id.Name,
                        Hive = "Store",
                        RegistryPath = "",
                        SizeBytes = 0,
                        IsStoreApp = true,
                        IconPath = iconPath
                    };
                    
                    if (seenNames.Add("Store_" + displayName + "_" + version))
                    {
                        appList.Add(appInfo);
                    }
                }
                catch {}
            }
        }
        catch (Exception ex)
        {
            Log($"Error scanning Microsoft Store apps: {ex.Message}");
        }
        
        return appList.OrderBy(x => x.DisplayName).ToList();
    }

    public async Task<bool> UninstallStoreAppAsync(string packageFullName)
    {
        Log($"Removing Microsoft Store package: {packageFullName}");
        bool uwpSuccess = false;
        try
        {
            var packageManager = new Windows.Management.Deployment.PackageManager();
            var result = await packageManager.RemovePackageAsync(packageFullName);
            
            if (result.ExtendedErrorCode == null || result.ExtendedErrorCode.HResult == 0)
            {
                Log($"Successfully uninstalled Microsoft Store package.");
                uwpSuccess = true;
            }
            else
            {
                Log($"Deployment result error: {result.ErrorText}");
            }
        }
        catch (Exception ex)
        {
            Log($"PackageManager direct uninstall failed: {ex.Message}");
        }

        if (uwpSuccess) return true;

        Log("Attempting fallback uninstallation via PowerShell...");
        try
        {
            var safePackage = packageFullName.Replace("'", "''").Replace("\"", "").Replace(";", "").Replace("$", "").Replace("`", "");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"Remove-AppxPackage -Package '{safePackage}'\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                    if (process.ExitCode == 0)
                    {
                        Log("Successfully uninstalled Microsoft Store package via PowerShell fallback.");
                        return true;
                    }
                    else
                    {
                        Log($"PowerShell fallback exited with code: {process.ExitCode}");
                    }
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(); } catch { }
                    Log("PowerShell fallback uninstall timed out after 30 seconds.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"PowerShell fallback failed: {ex.Message}");
        }

        return false;
    }
}

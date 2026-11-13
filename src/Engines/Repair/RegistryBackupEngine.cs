using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCarePro.Models;
using WinCarePro.Core.Helpers;

namespace WinCarePro.Engines;

public class RestorePointInfo
{
    public uint SequenceNumber { get; set; }
    public string Description { get; set; } = "";
    public string CreatedTime { get; set; } = "";
    public uint RestorePointType { get; set; }
}

public class RegistryBackupItem
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public class RegistryBackupEngine
{
    private static readonly SemaphoreSlim _backupSemaphore = new(1, 1);

    private static readonly string BackupFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"WinCarePro\Backups"
    );

    private static bool SafeFileExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    // Registry Backup
    public async Task<bool> CreateRegistryBackupAsync(string name)
    {
        if (!await _backupSemaphore.WaitAsync(2000))
        {
            Database.DbManager.LogAction("Registry backup timed out acquiring lock", "Registry Tools", "Timeout");
            return false;
        }

        try
        {
            if (!Directory.Exists(BackupFolder))
            {
                Directory.CreateDirectory(BackupFolder);
            }

            string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            string backupFile = Path.Combine(BackupFolder, $"{cleanName}_{DateTime.Now:yyyyMMdd_HHmmss}.reg");

            try
            {
                // 1. We export HKCU hive (user settings) which is safe and easy, and does not require exclusive locks
                var result = await ProcessRunner.RunAsync("reg.exe", new[] { "export", "HKCU", backupFile, "/y" }, TimeSpan.FromSeconds(10));
                bool success = result.ExitCode == 0;

                // 2. Export key HKLM paths we modify and append them to the backup file
                string[] hklmPaths = new[]
                {
                    @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem",
                    @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"
                };

                foreach (var path in hklmPaths)
                {
                    string tempFile = Path.Combine(BackupFolder, $"temp_{Guid.NewGuid():N}.reg");
                    try
                    {
                        var resultHklm = await ProcessRunner.RunAsync("reg.exe", new[] { "export", path, tempFile, "/y" }, TimeSpan.FromSeconds(10));
                        if (resultHklm.ExitCode == 0 && File.Exists(tempFile))
                        {
                            var lines = await File.ReadAllLinesAsync(tempFile, System.Text.Encoding.Unicode);
                            using (var sw = new StreamWriter(backupFile, true, System.Text.Encoding.Unicode))
                            {
                                sw.WriteLine();
                                foreach (var line in lines)
                                {
                                    if (line.Trim().StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    sw.WriteLine(line);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RegistryBackupEngine] Export {path} error: {ex.Message}");
                    }
                    finally
                    {
                        try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                    }
                }

                Database.DbManager.LogAction($"Created Registry Backup: {name}", "Registry Tools", success ? "Success" : "Failed");
                return success;
            }
            catch (Exception ex)
            {
                Database.DbManager.LogAction($"Registry backup failed: {ex.Message}", "Registry Tools", "Failed");
                return false;
            }
        }
        finally
        {
            _backupSemaphore.Release();
        }
    }

    /// <summary>
    /// Backs up a specific registry key before mutation or deletion, satisfying Rule 02 and Rule 11.
    /// </summary>
    public static async Task<bool> BackupKeyAsync(string keyPath, string? targetFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(keyPath)) return false;

        if (!await _backupSemaphore.WaitAsync(2000))
        {
            System.Diagnostics.Debug.WriteLine($"[RegistryBackupEngine] Timeout acquiring semaphore for {keyPath}");
            return false;
        }

        try
        {
            if (!Directory.Exists(BackupFolder)) Directory.CreateDirectory(BackupFolder);
            string safeKeyName = string.Join("_", keyPath.Split(Path.GetInvalidFileNameChars())).Trim('_');
            string backupPath = targetFilePath ?? Path.Combine(BackupFolder, $"KeyBackup_{safeKeyName}_{DateTime.Now:yyyyMMdd_HHmmss}.reg");

            var result = await ProcessRunner.RunAsync("reg.exe", new[] { "export", keyPath, backupPath, "/y" }, TimeSpan.FromSeconds(15));
            bool ok = result.Success && File.Exists(backupPath);
            Database.DbManager.LogAction($"Backup Key: {keyPath}", "Registry Tools", ok ? "Success" : "Failed");
            return ok;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegistryBackupEngine] BackupKeyAsync failed for {keyPath}: {ex.Message}");
            return false;
        }
        finally
        {
            _backupSemaphore.Release();
        }
    }

    /// <summary>
    /// Synchronous backup of a specific registry key for immediate guards.
    /// </summary>
    public static bool BackupKey(string keyPath, string? targetFilePath = null)
    {
        try
        {
            return BackupKeyAsync(keyPath, targetFilePath).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    [Obsolete("Use CreateRegistryBackupAsync instead to avoid thread blocking.")]
    public bool CreateRegistryBackup(string name)
    {
        try
        {
            string fileName = $"Backup_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.reg";
            string filePath = Path.Combine(BackupFolder, fileName);
            if (!Directory.Exists(BackupFolder)) Directory.CreateDirectory(BackupFolder);

            var result = ProcessRunner.RunAsync("reg.exe", new[] { "export", "HKCU", filePath, "/y" }, TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            return result.Success;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Registry backup failed: {ex.Message}", "Registry Tools", "Failed");
            return false;
        }
    }

    public async Task<bool> RestoreRegistryBackupAsync(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        if (!await _backupSemaphore.WaitAsync(2000))
        {
            Database.DbManager.LogAction("Registry restore timed out acquiring lock", "Registry Tools", "Timeout");
            return false;
        }

        try
        {
            // Verify header to ensure valid .reg backup file
            string firstLine = (await File.ReadAllLinesAsync(filePath)).FirstOrDefault() ?? "";
            if (!firstLine.Trim().StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase) &&
                !firstLine.Trim().StartsWith("REGEDIT", StringComparison.OrdinalIgnoreCase))
            {
                Database.DbManager.LogAction($"Invalid registry backup file format: {Path.GetFileName(filePath)}", "Registry Tools", "Failed");
                return false;
            }

            var result = await ProcessRunner.RunAsync("reg.exe", new[] { "import", filePath }, TimeSpan.FromSeconds(15));
            bool success = result.ExitCode == 0;
            Database.DbManager.LogAction($"Restored Registry Backup: {Path.GetFileName(filePath)}", "Registry Tools", success ? "Success" : "Failed");
            return success;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Registry restore failed: {ex.Message}", "Registry Tools", "Failed");
            return false;
        }
        finally
        {
            _backupSemaphore.Release();
        }
    }

    [Obsolete("Use RestoreRegistryBackupAsync instead to avoid thread blocking.")]
    public bool RestoreRegistryBackup(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            return RestoreRegistryBackupAsync(filePath).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Registry restore failed: {ex.Message}", "Registry Tools", "Failed");
            return false;
        }
    }

    public List<RegistryBackupItem> GetRegistryBackupsList()
    {
        var list = new List<RegistryBackupItem>();
        if (Directory.Exists(BackupFolder))
        {
            foreach (var file in Directory.GetFiles(BackupFolder, "*.reg"))
            {
                list.Add(new RegistryBackupItem
                {
                    Name = Path.GetFileName(file),
                    FilePath = file
                });
            }
        }
        return list;
    }

    public List<RestorePointInfo> GetSystemRestorePoints()
    {
        var list = WmiHelper.Query("SELECT SequenceNumber, Description, CreationTime, RestorePointType FROM SystemRestore", obj =>
        {
            // Format the creation date
            string rawTime = obj["CreationTime"]?.ToString() ?? "";
            string formattedDate = rawTime;
            if (rawTime.Length >= 8)
            {
                // WMI datetime is yyyymmddhhmmss.xxxxxx±zzz
                formattedDate = $"{rawTime.Substring(0, 4)}-{rawTime.Substring(4, 2)}-{rawTime.Substring(6, 2)} {rawTime.Substring(8, 2)}:{rawTime.Substring(10, 2)}";
            }

            return new RestorePointInfo
            {
                SequenceNumber = Convert.ToUInt32(obj["SequenceNumber"]),
                Description = obj["Description"]?.ToString() ?? "Restore Point",
                CreatedTime = formattedDate,
                RestorePointType = Convert.ToUInt32(obj["RestorePointType"])
            };
        }, @"root\default");
        return list;
    }

    public bool CreateSystemRestorePoint(string description)
    {
        try
        {
            using var mc = new ManagementClass(@"root\default:SystemRestore");
            // Define parameters for CreateRestorePoint
            // Type: 100 (DEVICE_DRIVER_INSTALL), 101 (APPLICATION_INSTALL), 102 (APPLICATION_UNINSTALL), 103 (MODIFY_SETTINGS), 104 (CANCELLED_OPERATION)
            // EventType: 100 (BEGIN_SYSTEM_CHANGE), 101 (END_SYSTEM_CHANGE)
            using var inParams = mc.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = description;
            inParams["RestorePointType"] = 103; // MODIFY_SETTINGS
            inParams["EventType"] = 100; // BEGIN_SYSTEM_CHANGE

            using var outParams = mc.InvokeMethod("CreateRestorePoint", inParams, null);
            uint result = Convert.ToUInt32(outParams?["ReturnValue"] ?? 1);

            Database.DbManager.LogAction($"Created System Restore Point: {description}", "Backup & Restore", result == 0 ? "Success" : $"Failed (Code {result})");
            return result == 0;
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Create Restore Point failed: {ex.Message}", "Backup & Restore", "Failed");
            return false;
        }
    }

    public void LaunchRestoreWizard()
    {
        try
        {
            string? rstruiPath = Core.Helpers.ProcessRunner.ResolveSafeExecutablePath("rstrui.exe");
            if (string.IsNullOrEmpty(rstruiPath))
            {
                Database.DbManager.LogAction("Windows System Restore Wizard (rstrui.exe) not found on system.", "Backup & Restore", "Failed");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = rstruiPath,
                UseShellExecute = true,
                WorkingDirectory = Environment.SystemDirectory
            });
            Database.DbManager.LogAction("Launched Windows System Restore Wizard", "Backup & Restore", "Success");
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction($"Failed to launch Windows System Restore Wizard: {ex.Message}", "Backup & Restore", "Failed");
        }
    }
}

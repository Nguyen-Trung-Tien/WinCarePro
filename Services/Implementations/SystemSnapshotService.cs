using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Engines;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class SystemSnapshotService : ISystemSnapshotService
{
    private readonly RegistryBackupEngine _registryEngine = new();
    private static readonly string SnapshotMetadataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"WinCarePro\Snapshots"
    );

    public SystemSnapshotService()
    {
        if (!Directory.Exists(SnapshotMetadataDir))
        {
            Directory.CreateDirectory(SnapshotMetadataDir);
        }
    }

    public async Task<string> CreateSnapshotAsync(string description, CancellationToken token = default)
    {
        return await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            string snapshotId = Guid.NewGuid().ToString("N");
            
            // 1. Create registry backup
            string backupName = $"Snapshot_{snapshotId}";
            bool regSuccess = _registryEngine.CreateRegistryBackup(backupName);

            // 2. Create System Restore Point (requires admin rights)
            bool rpSuccess = false;
            try
            {
                rpSuccess = _registryEngine.CreateSystemRestorePoint($"{description} ({snapshotId})");
            }
            catch (Exception ex)
            {
                Database.DbManager.LogAction($"System Restore Point creation failed: {ex.Message}", "Snapshot Service", "Failed");
            }

            token.ThrowIfCancellationRequested();

            // Save snapshot metadata
            string metadataFile = Path.Combine(SnapshotMetadataDir, $"{snapshotId}.txt");
            string metadataContent = $"Id: {snapshotId}\nDescription: {description}\nDate: {DateTime.Now}\nRegBackup: {backupName}\nRestorePointCreated: {rpSuccess}\nRegistryBackupCreated: {regSuccess}";
            File.WriteAllText(metadataFile, metadataContent);

            Database.DbManager.LogAction($"Created System Snapshot: {description}", "Snapshot Service", (regSuccess || rpSuccess) ? "Success" : "Failed");
            return snapshotId;
        }, token);
    }

    public async Task<bool> RestoreSnapshotAsync(string snapshotId, CancellationToken token = default)
    {
        return await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            string metadataFile = Path.Combine(SnapshotMetadataDir, $"{snapshotId}.txt");
            if (!File.Exists(metadataFile))
            {
                Database.DbManager.LogAction($"Restore failed: Snapshot ID {snapshotId} metadata not found", "Snapshot Service", "Failed");
                return false;
            }

            // Read metadata
            string[] lines = File.ReadAllLines(metadataFile);
            string regBackupName = "";
            foreach (var line in lines)
            {
                if (line.StartsWith("RegBackup: "))
                {
                    regBackupName = line.Substring(11).Trim();
                    break;
                }
            }

            token.ThrowIfCancellationRequested();

            // Find the registry backup file
            bool regRestored = false;
            if (!string.IsNullOrEmpty(regBackupName))
            {
                string backupsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"WinCarePro\Backups"
                );
                
                // Search for the .reg file containing the backup name prefix
                if (Directory.Exists(backupsDir))
                {
                    var files = Directory.GetFiles(backupsDir, $"*{regBackupName}*.reg");
                    if (files.Length > 0)
                    {
                        regRestored = _registryEngine.RestoreRegistryBackup(files[0]);
                    }
                }
            }

            // For full restore point recovery, inform the user via logs/wizards
            _registryEngine.LaunchRestoreWizard();

            Database.DbManager.LogAction($"Restored Registry backup for snapshot {snapshotId}", "Snapshot Service", regRestored ? "Success" : "Failed");
            return regRestored;
        }, token);
    }

    public async Task<bool> DeleteSnapshotAsync(string snapshotId)
    {
        return await Task.Run(() =>
        {
            string metadataFile = Path.Combine(SnapshotMetadataDir, $"{snapshotId}.txt");
            if (File.Exists(metadataFile))
            {
                try
                {
                    File.Delete(metadataFile);
                    Database.DbManager.LogAction($"Deleted Snapshot Metadata: {snapshotId}", "Snapshot Service", "Success");
                    return true;
                }
                catch { }
            }
            return false;
        });
    }
}

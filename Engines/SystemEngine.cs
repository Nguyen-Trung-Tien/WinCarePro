using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess; // Note: System.ServiceProcess requires referencing System.ServiceProcess.ServiceController.
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WinCarePro.Engines;

public class SystemEngine
{
    public event Action<string>? OutputReceived;
    public event Action<int>? ProgressChanged;

    private void Log(string message) => OutputReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");

    public async Task<bool> RunSfcScanAsync(bool repair = false)
    {
        string arguments = repair ? "/scannow" : "/verifyonly";
        Log($"Starting SFC tool (sfc {arguments})...");
        ProgressChanged?.Invoke(10);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sfc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) Log($"ERROR: {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ProgressChanged?.Invoke(50);
            await process.WaitForExitAsync();
            ProgressChanged?.Invoke(100);

            Log($"SFC finished with exit code {process.ExitCode}");
            Database.DbManager.LogAction($"SFC {arguments}", "System Repair", process.ExitCode == 0 ? "Success" : "Issues Found");
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log($"Failed to run SFC: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RunDismAsync(string mode)
    {
        // Modes: checkhealth, scanhealth, restorehealth, cleancomponent
        string arguments = mode.ToLower() switch
        {
            "checkhealth" => "/online /cleanup-image /checkhealth",
            "scanhealth" => "/online /cleanup-image /scanhealth",
            "restorehealth" => "/online /cleanup-image /restorehealth",
            "cleancomponent" => "/online /cleanup-image /startcomponentcleanup",
            _ => "/online /cleanup-image /checkhealth"
        };

        Log($"Starting DISM tool (dism {arguments})...");
        ProgressChanged?.Invoke(15);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) Log($"ERROR: {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ProgressChanged?.Invoke(60);
            await process.WaitForExitAsync();
            ProgressChanged?.Invoke(100);

            Log($"DISM finished with exit code {process.ExitCode}");
            Database.DbManager.LogAction($"DISM {mode}", "System Repair", process.ExitCode == 0 ? "Success" : "Failed");
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log($"Failed to run DISM: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RepairWindowsUpdateAsync()
    {
        Log("Initializing Windows Update components reset...");
        ProgressChanged?.Invoke(10);

        try
        {
            // 1. Stop Windows Update Services
            string[] services = { "wuauserv", "cryptSvc", "bits", "msiserver" };
            foreach (var svc in services)
            {
                Log($"Stopping service: {svc}...");
                RunCmd($"net stop {svc} /y");
                await Task.Delay(1000);
            }
            ProgressChanged?.Invoke(40);

            // 2. Rename SoftwareDistribution & Catroot2 folder
            Log("Renaming SoftwareDistribution & catroot2 directories...");
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string softDist = Path.Combine(windir, "SoftwareDistribution");
            string softDistOld = softDist + ".old";

            if (Directory.Exists(softDistOld))
            {
                try { Directory.Delete(softDistOld, true); } catch { }
            }

            if (Directory.Exists(softDist))
            {
                try
                {
                    Directory.Move(softDist, softDistOld);
                    Log("SoftwareDistribution successfully renamed to SoftwareDistribution.old");
                }
                catch (Exception ex)
                {
                    Log($"Could not rename SoftwareDistribution folder: {ex.Message}");
                }
            }

            string catroot2 = Path.Combine(windir, @"System32\catroot2");
            string catroot2Old = catroot2 + ".old";

            if (Directory.Exists(catroot2Old))
            {
                try { Directory.Delete(catroot2Old, true); } catch { }
            }

            if (Directory.Exists(catroot2))
            {
                try
                {
                    Directory.Move(catroot2, catroot2Old);
                    Log("catroot2 successfully renamed to catroot2.old");
                }
                catch (Exception ex)
                {
                    Log($"Could not rename catroot2 folder: {ex.Message}");
                }
            }
            ProgressChanged?.Invoke(70);

            // 3. Start services again
            foreach (var svc in services)
            {
                Log($"Starting service: {svc}...");
                RunCmd($"net start {svc}");
                await Task.Delay(1000);
            }
            ProgressChanged?.Invoke(100);

            Log("Windows Update components have been reset.");
            Database.DbManager.LogAction("Reset Windows Update", "System Repair", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Windows Update Repair failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RepairServicesConfigAsync(System.Collections.Generic.IEnumerable<string> servicesToRepair)
    {
        Log("Restoring default configuration for selected services...");
        ProgressChanged?.Invoke(20);

        try
        {
            var serviceList = new System.Collections.Generic.List<string>(servicesToRepair);
            if (serviceList.Count == 0)
            {
                Log("No services selected for restoration.");
                ProgressChanged?.Invoke(100);
                return true;
            }

            int count = 0;
            foreach (var svc in serviceList)
            {
                Log($"Setting service {svc} startup type to Automatic...");
                RunCmd($"sc config {svc} start= auto");
                RunCmd($"sc start {svc}");
                count++;
                ProgressChanged?.Invoke(20 + (80 * count / serviceList.Count));
                await Task.Delay(500);
            }
            ProgressChanged?.Invoke(100);

            Log("Selected services configuration restored.");
            Database.DbManager.LogAction($"Restore Services Config ({string.Join(", ", serviceList)})", "System Repair", "Success");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Services Config Repair failed: {ex.Message}");
            return false;
        }
    }

    private void RunCmd(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch
        {
            // Ignore
        }
    }
}

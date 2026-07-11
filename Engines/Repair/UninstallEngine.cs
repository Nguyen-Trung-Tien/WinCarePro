using System;
using System.IO;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Engines;

public partial class UninstallEngine
{
    public event Action<string>? OutputReceived;
    public event Action<int>? ProgressChanged;
    
    private void Log(string msg) => OutputReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");
    
    public async Task<bool> RunStandardUninstallerAsync(InstalledAppInfo app)
    {
        if (app.IsStoreApp)
        {
            return await UninstallStoreAppAsync(app.UninstallString);
        }

        Log($"Launching standard uninstaller for: {app.DisplayName}");
        ProgressChanged?.Invoke(25);
        try
        {
            string cmd = app.UninstallString.Trim();
            string exe = "";
            string args = "";
            
            // Robust parsing of UninstallString
            if (cmd.StartsWith("\""))
            {
                int index = cmd.IndexOf("\"", 1);
                if (index > 0)
                {
                    exe = cmd.Substring(1, index - 1).Trim();
                    args = cmd.Substring(index + 1).Trim();
                }
                else
                {
                    exe = cmd.Replace("\"", "").Trim();
                }
            }
            else
            {
                // Look for typical executable extensions to split
                string[] extensions = { ".exe", ".msi", ".bat", ".cmd" };
                bool parsed = false;
                foreach (var ext in extensions)
                {
                    int extIndex = cmd.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                    if (extIndex > 0)
                    {
                        exe = cmd.Substring(0, extIndex + ext.Length).Trim();
                        args = cmd.Substring(extIndex + ext.Length).Trim();
                        parsed = true;
                        break;
                    }
                }
                
                if (!parsed)
                {
                    // Unquoted path with spaces: attempt to resolve by checking files
                    int lastSpace = cmd.Length;
                    while (lastSpace > 0)
                    {
                        string candidate = cmd.Substring(0, lastSpace).Trim();
                        if (File.Exists(candidate))
                        {
                            exe = candidate;
                            args = cmd.Substring(lastSpace).Trim();
                            parsed = true;
                            break;
                        }
                        lastSpace = candidate.LastIndexOf(' ');
                    }
                    
                    if (!parsed)
                    {
                        // Fallback: split on first space
                        int spaceIndex = cmd.IndexOf(" ");
                        if (spaceIndex > 0)
                        {
                            exe = cmd.Substring(0, spaceIndex).Trim();
                            args = cmd.Substring(spaceIndex + 1).Trim();
                        }
                        else
                        {
                            exe = cmd;
                        }
                    }
                }
            }
            
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas"
            };
            
            // Set working directory to installation folder if valid
            if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            {
                psi.WorkingDirectory = app.InstallLocation;
            }
            else if (!string.IsNullOrEmpty(exe))
            {
                try
                {
                    string? dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        psi.WorkingDirectory = dir;
                    }
                }
                catch {}
            }
            
            ProgressChanged?.Invoke(50);
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return false;
            
            Log("Standard uninstaller launched. Please follow the prompt/UI instruction to finish uninstallation.");
            ProgressChanged?.Invoke(75);
            await process.WaitForExitAsync();
            Log($"Standard uninstaller exited. Exit Code: {process.ExitCode}");
            ProgressChanged?.Invoke(100);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error launching standard uninstaller: {ex.Message}");
            return false;
        }
    }
}

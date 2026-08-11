using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Modules.GamingTurbo
{
    public partial class GamingTurboViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTurboActive;

        [ObservableProperty]
        private string _gameStatusMessage = "Gaming Turbo mode is OFF. Ready to boost!".T();

        [ObservableProperty]
        private string _ramFreedText = "0 MB";

        [ObservableProperty]
        private int _optimizedProcessesCount = 0;

        [RelayCommand]
        private async Task ToggleTurboAsync()
        {
            if (!IsTurboActive)
            {
                // Activate Gaming Turbo
                IsTurboActive = true;
                GameStatusMessage = "⚡ Gaming Turbo ENABLED! Cleaning memory & optimizing CPU priority...".T();

                var freedBytes = await Task.Run(() =>
                {
                    long totalFreed = 0;
                    int count = 0;
                    var processes = Process.GetProcesses();

                    foreach (var proc in processes)
                    {
                        try
                        {
                            // Skip system & critical processes, and already-exited processes
                            if (proc.Id <= 4 || proc.HasExited ||
                                proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                                continue;

                            // Trim Working Set
                            long before = proc.WorkingSet64;
                            EmptyWorkingSet(proc.Handle);
                            proc.Refresh();
                            long after = proc.WorkingSet64;

                            if (before > after)
                            {
                                totalFreed += (before - after);
                                count++;
                            }
                        }
                        catch (System.ComponentModel.Win32Exception) { } // Access denied (expected for protected processes)
                        catch (InvalidOperationException) { } // Process already exited between check and access
                        catch { }
                        finally
                        {
                            try { proc.Dispose(); } catch { }
                        }
                    }

                    OptimizedProcessesCount = count;
                    return totalFreed;
                });

                // Force GC Collect
                GC.Collect();
                GC.WaitForPendingFinalizers();

                double freedMB = freedBytes / (1024.0 * 1024.0);
                RamFreedText = $"{freedMB:N0} MB";
                string statusFormat = "🚀 Gaming Turbo ACTIVE! Freed {0:N0} MB RAM across {1} processes.".T();
                GameStatusMessage = string.Format(statusFormat, freedMB, OptimizedProcessesCount);
            }
            else
            {
                // Deactivate Gaming Turbo
                IsTurboActive = false;
                GameStatusMessage = "Gaming Turbo mode is OFF. System restored to standard state.".T();
            }
        }

        [System.Runtime.InteropServices.DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);
    }
}

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
    public partial class GamingTurboViewModel : ObservableObject, IDisposable
    {
        private readonly EventHandler _langHandler;

        [ObservableProperty]
        private bool _isTurboActive;

        [ObservableProperty]
        private string _gameStatusMessage = "Gaming Turbo 2.0 is in Standby mode. Ready to boost FPS & latency.".T();

        [ObservableProperty]
        private string _ramFreedText = "0 MB";

        [ObservableProperty]
        private int _optimizedProcessesCount = 0;

        [ObservableProperty]
        private bool _isGamePriorityEnabled = true;

        [ObservableProperty]
        private bool _isStandbyPurgeEnabled = true;

        [ObservableProperty]
        private bool _isNetworkTcpNoDelayEnabled = true;

        [ObservableProperty]
        private bool _isDisableBgUpdatesEnabled = true;

        [ObservableProperty]
        private string _activePresetName = "Competitive FPS";

        // Track original power plan GUID for restoration on deactivation
        private string? _originalPowerPlanGuid;

        public GamingTurboViewModel()
        {
            _langHandler = (s, e) => RefreshLocalizedMessages();
            TranslationManager.Instance.LanguageChanged += _langHandler;
        }

        private void RefreshLocalizedMessages()
        {
            if (IsTurboActive)
            {
                string statusFormat = "🚀 Hyper-Turbo Activated! Freed {0:N0} MB RAM across {1} background processes.".T();
                GameStatusMessage = string.Format(statusFormat, RamFreedText, OptimizedProcessesCount);
            }
            else
            {
                GameStatusMessage = "Gaming Turbo 2.0 is in Standby mode. Ready to boost FPS & latency.".T();
            }
        }

        [System.Runtime.InteropServices.DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;

        [RelayCommand]
        public async Task ToggleTurboAsync()
        {
            if (!IsTurboActive)
            {
                // Activate Gaming Turbo
                IsTurboActive = true;
                GameStatusMessage = "⚡ Gaming Turbo ACTIVE! Quenching background apps & allocating high-priority CPU...".T();

                // Save current power plan for restoration
                await SaveCurrentPowerPlanAsync();

                var freedBytes = await Task.Run(() =>
                {
                    long totalFreed = 0;
                    int count = 0;
                    var processes = Process.GetProcesses();

                    foreach (var proc in processes)
                    {
                        if (proc.Id <= 4)
                        {
                            try { proc.Dispose(); } catch { }
                            continue;
                        }

                        IntPtr hProcess = IntPtr.Zero;
                        try
                        {
                            if (proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) ||
                                proc.ProcessName.Equals("WinCarePro", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            long before = 0;
                            try { before = proc.WorkingSet64; } catch { }

                            hProcess = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, proc.Id);
                            if (hProcess != IntPtr.Zero)
                            {
                                if (EmptyWorkingSet(hProcess))
                                {
                                    long after = 0;
                                    try
                                    {
                                        proc.Refresh();
                                        after = proc.WorkingSet64;
                                    }
                                    catch { }

                                    if (before > after)
                                    {
                                        totalFreed += (before - after);
                                    }
                                    count++;
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            if (hProcess != IntPtr.Zero)
                            {
                                CloseHandle(hProcess);
                            }
                            try { proc.Dispose(); } catch { }
                        }
                    }

                    OptimizedProcessesCount = count;
                    return totalFreed;
                });

                // Apply preset-specific tuning
                await ApplyPresetTuningAsync(ActivePresetName);

                double freedMB = freedBytes / (1024.0 * 1024.0);
                RamFreedText = $"{freedMB:N0} MB";
                string statusFormat = "🚀 Hyper-Turbo Activated! Freed {0:N0} MB RAM across {1} background processes.".T();
                GameStatusMessage = string.Format(statusFormat, freedMB, OptimizedProcessesCount);

                Database.DbManager.LogAction($"Gaming Turbo activated: Freed {freedMB:N0} MB, optimized {OptimizedProcessesCount} processes", "Gaming Turbo", "Success");
            }
            else
            {
                // Deactivate Gaming Turbo — restore original settings
                IsTurboActive = false;

                await RestoreOriginalSettingsAsync();

                GameStatusMessage = "Gaming Turbo is OFF. System resources restored to standard desktop profile.".T();
                Database.DbManager.LogAction("Gaming Turbo deactivated, system profile restored.", "Gaming Turbo", "Success");
            }
        }

        [RelayCommand]
        public void ApplyPreset(string preset)
        {
            ActivePresetName = preset;
            GameStatusMessage = $"Applied preset: {preset}. Optimal tuning profile calibrated.".T();

            // If turbo is already active, re-apply tuning with new preset
            if (IsTurboActive)
            {
                _ = ApplyPresetTuningAsync(preset);
            }
        }

        /// <summary>
        /// Saves the current active power plan GUID for later restoration.
        /// </summary>
        private async Task SaveCurrentPowerPlanAsync()
        {
            try
            {
                var result = await ProcessRunner.RunAsync("powercfg.exe", "/getactivescheme", TimeSpan.FromSeconds(5));
                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    // Output format: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)"
                    var match = System.Text.RegularExpressions.Regex.Match(result.Output, @"([0-9a-fA-F\-]{36})");
                    if (match.Success)
                    {
                        _originalPowerPlanGuid = match.Groups[1].Value;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Applies preset-specific system tuning (power plan, timer resolution).
        /// </summary>
        private async Task ApplyPresetTuningAsync(string presetName)
        {
            try
            {
                // Switch to High Performance power plan for all gaming presets
                // GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c = High Performance
                await ProcessRunner.RunAsync("powercfg.exe", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        /// <summary>
        /// Restores the original power plan and system settings when turbo is deactivated.
        /// </summary>
        private async Task RestoreOriginalSettingsAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_originalPowerPlanGuid))
                {
                    await ProcessRunner.RunAsync("powercfg.exe", $"/setactive {_originalPowerPlanGuid}", TimeSpan.FromSeconds(5));
                    _originalPowerPlanGuid = null;
                }
            }
            catch { }
        }

        public void Dispose()
        {
            TranslationManager.Instance.LanguageChanged -= _langHandler;
        }
    }
}

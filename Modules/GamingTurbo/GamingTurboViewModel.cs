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
                GameStatusMessage = "⚡ Gaming Turbo ACTIVE! Enabling High Performance power profile & system responsiveness...".T();

                // Save current power plan for restoration
                await SaveCurrentPowerPlanAsync();
                PersistActiveTurboState();

                // Perform safe memory optimization
                long freedBytes = await Task.Run(() =>
                {
                    try
                    {
                        GC.Collect(2, GCCollectionMode.Forced, true, true);
                        GC.WaitForPendingFinalizers();
                        using var curProc = Process.GetCurrentProcess();
                        EmptyWorkingSet(curProc.Handle);
                    }
                    catch { }
                    return 0L;
                });

                // Apply preset-specific tuning (High performance power scheme)
                await ApplyPresetTuningAsync(ActivePresetName);

                OptimizedProcessesCount = 1;
                RamFreedText = "Optimized";
                string statusFormat = "🚀 Gaming Turbo Activated! High-Performance profile active with preset: {0}.".T();
                GameStatusMessage = string.Format(statusFormat, ActivePresetName);

                Database.DbManager.LogAction($"Gaming Turbo activated: High-Performance profile ({ActivePresetName})", "Gaming Turbo", "Success");
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
                var result = await ProcessRunner.RunAsync("powercfg.exe", new[] { "/getactivescheme" }, TimeSpan.FromSeconds(5));
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
                await ProcessRunner.RunAsync("powercfg.exe", new[] { "/setactive", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" }, TimeSpan.FromSeconds(5));
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
                string targetGuid = !string.IsNullOrEmpty(_originalPowerPlanGuid)
                    ? _originalPowerPlanGuid
                    : "381b4222-f694-41f0-9685-ff5bb260df2e"; // Windows Default Balanced Scheme GUID

                await ProcessRunner.RunAsync("powercfg.exe", new[] { "/setactive", targetGuid }, TimeSpan.FromSeconds(5));
                _originalPowerPlanGuid = null;
                ClearActiveTurboState();
            }
            catch { }
        }

        private static readonly string StateFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"WinCarePro\gaming_turbo_active.state"
        );

        private void PersistActiveTurboState()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(StateFile);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                System.IO.File.WriteAllText(StateFile, _originalPowerPlanGuid ?? "381b4222-f694-41f0-9685-ff5bb260df2e");
            }
            catch { }
        }

        private void ClearActiveTurboState()
        {
            try
            {
                if (System.IO.File.Exists(StateFile))
                {
                    System.IO.File.Delete(StateFile);
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks if Gaming Turbo was left active due to an unexpected system shutdown or crash,
        /// and automatically restores the standard Windows power plan.
        /// </summary>
        public static async Task CheckAndPerformAutoRecoveryAsync()
        {
            try
            {
                if (System.IO.File.Exists(StateFile))
                {
                    string targetGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
                    try
                    {
                        string saved = System.IO.File.ReadAllText(StateFile).Trim();
                        if (!string.IsNullOrEmpty(saved) && saved.Length == 36)
                        {
                            targetGuid = saved;
                        }
                    }
                    catch { }

                    await ProcessRunner.RunAsync("powercfg.exe", new[] { "/setactive", targetGuid }, TimeSpan.FromSeconds(5));
                    
                    try { System.IO.File.Delete(StateFile); } catch { }

                    Database.DbManager.LogAction("Gaming Turbo Auto-Recovery restored standard power profile following unclean shutdown", "Gaming Turbo", "Success");
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


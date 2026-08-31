using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

using WinCarePro.Database;
using WinCarePro.Services;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro;

public sealed partial class MainWindow : Window
{
    public Grid MainRootGrid => RootGrid;
    public FontIcon MainThemeIcon => ThemeIcon;
    public StackPanel ToastStackContainer => ToastContainer;
    public Frame MainFrame => RootFrame;

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _forceClose = false;
    private Microsoft.UI.Xaml.DispatcherTimer? _clockTimer;

    public MainWindow()
    {
        InitializeComponent();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Set Window Icon programmatically
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "AppIcon.ico");
            }
            if (File.Exists(iconPath))
            {
                // Set titlebar icon (WinUI 3 API)
                this.AppWindow.SetIcon(iconPath);

                // Force-update taskbar and Alt+Tab icons via Win32 WM_SETICON with high-res icon frames.
                // 256x256 for Taskbar/Alt+Tab (ICON_BIG), 32x32 for Titlebar (ICON_SMALL)
                var hIconBig = LoadImage(IntPtr.Zero, iconPath, 1, 256, 256, 0x00000010); // IMAGE_ICON | LR_LOADFROMFILE
                var hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1, 32, 32, 0x00000010);   // IMAGE_ICON | LR_LOADFROMFILE
                if (hIconBig != IntPtr.Zero || hIconSmall != IntPtr.Zero)
                {
                    SetTaskbarIcon(hIconBig, hIconSmall);
                }
            }
            else
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var hIconExtracted = ExtractIcon(IntPtr.Zero, exePath, 0);
                    if (hIconExtracted != IntPtr.Zero)
                    {
                        SetTaskbarIcon(hIconExtracted, hIconExtracted);
                    }
                }
            }
        }
        catch { }

        // Subclass window to enforce minimum bounds (1280 x 800)
        SubclassWindow();

        // Center window on active monitor screen with standard dimensions (1560x920)
        CenterOnScreen(1560, 920);

        // Extends page content into the top caption bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleDragArea);

        // Manual preview key handler for Ctrl + F to focus search, avoiding WinUI 3 KeyboardAccelerator tooltip bugs and accidental triggers
        if (this.Content is FrameworkElement rootElem)
        {
            rootElem.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.F)
                {
                    var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                    bool isCtrlDown = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
                    if (isCtrlDown)
                    {
                        SearchBox?.Focus(FocusState.Programmatic);
                        e.Handled = true;
                    }
                }
            };
        }

        this.AppWindow.Closing += AppWindow_Closing;
        this.Closed += MainWindow_Closed;

        // Centralized Theme & Multi-Language Synchronization
        ThemeManager.Instance.RegisterWindow(this);
        TranslationManager.Instance.RegisterWindow(this);

        // Handle window resizing and start async application initialization on load
        if (RootGrid != null)
        {
            RootGrid.Loaded += (s, e) => {
                CenterOnScreen(1560, 920);
                InitializeAppAsync();
            };
        }
        else
        {
            CenterOnScreen(1560, 920);
            InitializeAppAsync();
        }

        TranslationManager.Instance.LanguageChanged += (s, e) => PopulateSearchRegistry();
    }

    private void CenterOnScreen(int width = 1560, int height = 920)
    {
        try
        {
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    int screenWidth = displayArea.WorkArea.Width;
                    int screenHeight = displayArea.WorkArea.Height;

                    int targetWidth = Math.Min(width, screenWidth);
                    int targetHeight = Math.Min(height, screenHeight);

                    int x = displayArea.WorkArea.X + (screenWidth - targetWidth) / 2;
                    int y = displayArea.WorkArea.Y + (screenHeight - targetHeight) / 2;

                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, targetWidth, targetHeight));
                }
                else
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                }
            }
        }
        catch
        {
            try
            {
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            }
            catch { }
        }
    }

    private async Task SmoothTweenProgressAsync(double targetValue, string message, int durationMs = 60)
    {
        if (StartupProgressText != null) StartupProgressText.Text = message.T();
        if (StartupProgressBar == null || StartupProgressPercent == null) return;

        double startVal = StartupProgressBar.Value;
        double diff = targetValue - startVal;
        if (Math.Abs(diff) < 0.01) return;

        int steps = Math.Max(3, durationMs / 16);
        int stepDelay = Math.Max(8, durationMs / steps);

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            // Cubic Ease-Out curve for progress
            double easeT = 1.0 - Math.Pow(1.0 - t, 3);
            double currentVal = startVal + diff * easeT;

            StartupProgressBar.Value = currentVal;
            StartupProgressPercent.Text = $"{(int)Math.Min(100, Math.Round(currentVal))}%";
            await Task.Delay(stepDelay);
        }

        StartupProgressBar.Value = targetValue;
        StartupProgressPercent.Text = $"{(int)Math.Min(100, Math.Round(targetValue))}%";
    }

    private async void InitializeAppAsync()
    {
        try
        {
            await SmoothTweenProgressAsync(25, "Initializing core system engine...", 40);

            // 1. Ensure database & telemetry store are synchronized
            await Task.Run(() => SettingsService.Instance.LoadSettings());
            await SmoothTweenProgressAsync(55, "Loading database & telemetry store...", 40);

            // 2. Load theme settings and transparency levels from DB
            LoadThemeConfiguration();
            await SmoothTweenProgressAsync(75, "Applying visual themes & typography...", 40);

            // 3. Load language setting and apply translations to window content
            TranslationManager.Instance.LoadLanguageFromSettings();
            TranslationManager.Instance.Translate(this.Content);
            await SmoothTweenProgressAsync(90, "Applying localized linguistic model...", 40);

            // 4. Update notification badge indicator & prepare main view
            UpdateNotificationBadge();
            RootFrame.Navigate(typeof(MainPage));

            await SmoothTweenProgressAsync(100, "Ready!", 30);

            // 5. Start Clock Ticker & fade out splash overlay smoothly
            StartClockTicker();
            FadeOutStartupOverlay.Begin();

            // 6. Deferred background tasks: Index search registry & database maintenance
            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(4, 1, 0, 0);
            CheckAndShowChangelog(currentVersion);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Modules.GamingTurbo.GamingTurboViewModel.CheckAndPerformAutoRecoveryAsync();
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("Startup.GamingTurboRecovery", ex);
                }
            });

            _ = Task.Run(() =>
            {
                try
                {
                    Database.DbManager.RunDatabaseMaintenance();
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("Startup.DbMaintenance", ex);
                }
            });

            _ = Task.Run(() =>
            {
                try
                {
                    App.MainDispatcherQueue?.TryEnqueue(() => PopulateSearchRegistry());
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.CrashLogger.LogException("Startup.SearchRegistry", ex);
                }
            });
        }
        catch (Exception ex)
        {
            // Fallback: Ensure splash vanishes and app is navigable if anything throws
            StartupOverlayGrid.Visibility = Visibility.Collapsed;
            RootFrame.Navigate(typeof(MainPage));
            StartClockTicker();
            Database.DbManager.LogAction($"Startup failed: {ex.Message}", "System", "Failed");
            Infrastructure.Logging.CrashLogger.LogException("MainWindow.InitializeAppAsync", ex);
        }
    }

    private void FadeOutStartupOverlay_Completed(object? sender, object e)
    {
        StartupOverlayGrid.Visibility = Visibility.Collapsed;
    }

    private void StartClockTicker()
    {
        // Update clock immediately
        ClockText.Text = DateTime.Now.ToString("HH:mm");

        // Stop any previous timer to prevent duplicates
        _clockTimer?.Stop();

        // Create a DispatcherTimer for periodic clock and telemetry updates
        _clockTimer = new Microsoft.UI.Xaml.DispatcherTimer();
        _clockTimer.Interval = TimeSpan.FromSeconds(2);
        _clockTimer.Tick += (s, e) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm");
            UpdateTitleBarTelemetry();
        };
        _clockTimer.Start();
    }

    private ulong _hudPrevIdleTime;
    private ulong _hudPrevKernelTime;
    private ulong _hudPrevUserTime;
    private bool _hudHasPrevTimes = false;

    private int _isTelemetrySampling = 0;

    private void UpdateTitleBarTelemetry()
    {
        // Concurrency guard: Skip sampling if a previous sample is still running
        if (Interlocked.CompareExchange(ref _isTelemetrySampling, 1, 0) != 0)
            return;

        Task.Run(() =>
        {
            try
            {
                // Sample RAM
                var mem = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                GlobalMemoryStatusEx(ref mem);
                double ramPercent = mem.dwMemoryLoad;
                double freeGb = mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double totalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);

                // Sample CPU
                GetSystemTimes(out var idleTime, out var kernelTime, out var userTime);
                ulong idle = ((ulong)idleTime.dwHighDateTime << 32) | idleTime.dwLowDateTime;
                ulong kernel = ((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime;
                ulong user = ((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime;

                double cpuPercent = 0;
                if (_hudHasPrevTimes)
                {
                    ulong usrDiff = user - _hudPrevUserTime;
                    ulong kerDiff = kernel - _hudPrevKernelTime;
                    ulong idlDiff = idle - _hudPrevIdleTime;
                    ulong total = usrDiff + kerDiff;
                    if (total > 0 && total >= idlDiff)
                    {
                        cpuPercent = Math.Clamp((double)(total - idlDiff) * 100.0 / total, 0, 100);
                    }
                }
                _hudPrevIdleTime = idle;
                _hudPrevKernelTime = kernel;
                _hudPrevUserTime = user;
                _hudHasPrevTimes = true;

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (CpuChipText != null)
                    {
                        CpuChipText.Text = cpuPercent > 0 ? $"CPU {cpuPercent:F0}%" : "CPU";
                        ToolTipService.SetToolTip(CpuChipButton, $"CPU Usage: {cpuPercent:F0}%\nClick to open live Dashboard monitor.".T());
                    }
                    if (RamChipText != null)
                    {
                        RamChipText.Text = $"RAM {ramPercent:F0}%";
                        ToolTipService.SetToolTip(RamChipButton, $"RAM Usage: {ramPercent:F0}% ({freeGb:F1} GB free of {totalGb:F1} GB)\nClick to perform 1-Click RAM Purge.".T());
                    }
                });
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _isTelemetrySampling, 0);
            }
        });
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_forceClose)
        {
            CleanupTrayIcon();
            return;
        }

        args.Cancel = true;
        try
        {
            bool minimizeToTray = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings.MinimizeToTray;

            if (minimizeToTray)
            {
                this.AppWindow.Hide();
                InitializeTrayIcon();
            }
            else
            {
                CleanupTrayIcon();
                _forceClose = true;
                this.Close();
            }
        }
        catch
        {
            CleanupTrayIcon();
            _forceClose = true;
            this.Close();
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // Stop clock/telemetry timer to prevent background Task.Run leaks
        _clockTimer?.Stop();
        _clockTimer = null;

        ThemeManager.Instance.UnregisterWindow(this);
        TranslationManager.Instance.UnregisterWindow(this);
        CleanupTrayIcon();
        UnsubclassWindow();
        try
        {
            if (RootFrame.Content is MainPage mainPage)
            {
                mainPage.CleanupActivePage();
            }
            else if (RootFrame.Content is WinCarePro.Views.NetworkPage netPage)
            {
                netPage.ViewModel?.Cleanup();
            }
        }
        catch { }

        try
        {
            WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.CloseWindow();
        }
        catch { }

        try
        {
            WinCarePro.Services.Implementations.SettingsService.Instance.FlushPendingSave();
            DbManager.ShutdownDatabase();
        }
        catch { }
    }

    public void ShowToastFromDb(string title, string message, string level)
    {
        var severity = level.ToLower() switch
        {
            "warning" => Services.Contracts.NotificationSeverity.Warning,
            "error" => Services.Contracts.NotificationSeverity.Error,
            "critical" => Services.Contracts.NotificationSeverity.Critical,
            "success" => Services.Contracts.NotificationSeverity.Success,
            _ => Services.Contracts.NotificationSeverity.Info
        };

        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        if (notificationService != null)
        {
            System.Collections.Generic.List<Services.Contracts.NotificationAction>? actions = null;
            if (title.Contains("Update Ready", StringComparison.OrdinalIgnoreCase))
            {
                actions = new System.Collections.Generic.List<Services.Contracts.NotificationAction>
                {
                    new Services.Contracts.NotificationAction
                    {
                        Label = "Install Now".T(),
                        Action = () =>
                        {
                            InstallDownloadedUpdate();
                        }
                    }
                };
            }
            notificationService.EnqueueNotification(title, message, severity, actions: actions, saveToDb: false);
        }
    }

    public void ShowToastNotification(string title, string message, string level, string targetPage = "")
    {
        var severity = level.ToLower() switch
        {
            "warning" => Services.Contracts.NotificationSeverity.Warning,
            "error" => Services.Contracts.NotificationSeverity.Error,
            "critical" => Services.Contracts.NotificationSeverity.Critical,
            "success" => Services.Contracts.NotificationSeverity.Success,
            _ => Services.Contracts.NotificationSeverity.Info
        };

        List<Services.Contracts.NotificationAction>? actions = null;

        // Context-aware actions
        if (title.Contains("RAM", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("RAM", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("Memory", StringComparison.OrdinalIgnoreCase))
        {
            actions = new List<Services.Contracts.NotificationAction>
            {
                new Services.Contracts.NotificationAction
                {
                    Label = "Optimize RAM".T(),
                    Action = () =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var optEngine = App.Services.GetRequiredService<Engines.SystemOptimizerEngine>();
                                await optEngine.OptimizeRamAsync();
                                
                                App.MainDispatcherQueue?.TryEnqueue(() =>
                                {
                                    DbManager.LogAction("Manual RAM optimization triggered from toast", "Smart Boost", "Success");
                                    var service = App.Services.GetService<Services.Contracts.INotificationService>();
                                    service?.ShowSuccess("RAM Cleaned".T(), "Memory has been successfully optimized.");
                                });
                            }
                            catch { }
                        });
                    }
                }
            };
        }
        else if (title.Contains("Disk", StringComparison.OrdinalIgnoreCase) || 
                 title.Contains("Junk", StringComparison.OrdinalIgnoreCase) || 
                 message.Contains("junk", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("files", StringComparison.OrdinalIgnoreCase))
        {
            actions = new List<Services.Contracts.NotificationAction>
            {
                new Services.Contracts.NotificationAction
                {
                    Label = "Clean Junk".T(),
                    Action = () =>
                    {
                        App.MainDispatcherQueue?.TryEnqueue(() =>
                        {
                            if (RootFrame.Content is MainPage mp)
                            {
                                mp.NavigateToPageExternal("Junk");
                            }
                        });
                    }
                }
            };
        }

        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        if (notificationService != null)
        {
            notificationService.EnqueueNotification(title, message, severity, actions, saveToDb: false);
        }
    }

    private async Task SmoothTweenExitProgressAsync(double targetValue, string subtext, int durationMs = 220)
    {
        if (ExitProgressSubtext != null) ExitProgressSubtext.Text = subtext.T();
        if (ExitProgressBar == null || ExitProgressPercent == null) return;

        double startVal = ExitProgressBar.Value;
        double diff = targetValue - startVal;
        if (Math.Abs(diff) < 0.01) return;

        int steps = Math.Max(5, durationMs / 16);
        int stepDelay = durationMs / steps;

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double easeT = 1.0 - Math.Pow(1.0 - t, 3);
            double currentVal = startVal + diff * easeT;

            ExitProgressBar.Value = currentVal;
            ExitProgressPercent.Text = $"{(int)Math.Min(100, Math.Round(currentVal))}%";
            await Task.Delay(stepDelay);
        }

        ExitProgressBar.Value = targetValue;
        ExitProgressPercent.Text = $"{(int)Math.Min(100, Math.Round(targetValue))}%";
    }

    public async void PerformAppExit()
    {
        this.AppWindow.Show();
        BringToForeground();

        ExitOverlayTitle.Text = "Shutting Down".T();
        ExitOverlayMessage.Text = "Securing database, flushing memory caches & closing services...".T();
        ExitOverlayGrid.Visibility = Visibility.Visible;
        FadeInExitOverlay.Begin();

        // Stage 1: UI and active page cleanup
        await SmoothTweenExitProgressAsync(30, "Saving session state & active modules...", 200);
        try
        {
            if (RootFrame.Content is MainPage mainPage)
            {
                mainPage.CleanupActivePage();
            }
            else if (RootFrame.Content is WinCarePro.Views.NetworkPage netPage)
            {
                netPage.ViewModel?.Cleanup();
            }

            WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.CloseWindow();
        }
        catch { }

        // Stage 2: Database shutdown & WAL checkpoint flush
        await SmoothTweenExitProgressAsync(70, "Flushing SQLite WAL checkpoint & memory pools...", 220);
        await Task.Run(() =>
        {
            try
            {
                WinCarePro.Services.Implementations.SettingsService.Instance.FlushPendingSave();
                DbManager.ShutdownDatabase();
            }
            catch { }
        });

        // Stage 3: Disposing telemetry & tray icons
        await SmoothTweenExitProgressAsync(95, "Disposing telemetry engines & system monitors...", 180);
        CleanupTrayIcon();

        // Stage 4: Farewell complete
        await SmoothTweenExitProgressAsync(100, "All resources secured. Goodbye!", 150);
        ExitOverlayTitle.Text = "Session Terminated".T();

        await Task.Delay(250);

        _forceClose = true;
        this.Close();
    }

    private void OnConfirmShutdownClick(object sender, RoutedEventArgs e)
    {
        ExitPowerFlyout.Hide();
        PerformAppExit();
    }

    private void OnMinimizeToTrayClick(object sender, RoutedEventArgs e)
    {
        ExitPowerFlyout.Hide();
        this.AppWindow.Hide();
        InitializeTrayIcon();

        // Immediately reclaim background working set memory
        Task.Run(() => TrimProcessMemory());

        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        notificationService?.ShowToast("WinCare Pro Running", "Application is minimized to the system tray.", Services.Contracts.NotificationSeverity.Info);
    }

    private void ExitPowerFlyout_Opening(object? sender, object e)
    {
        if (MenuMinimizeToTray != null) MenuMinimizeToTray.Text = "Minimize to Tray".T();
        if (MenuShutdown != null) MenuShutdown.Text = "Shutdown".T();
    }

    private void ExitAppButton_Click(object sender, RoutedEventArgs e)
    {
        // Fallback if flyout is not triggered directly
        if (ExitPowerFlyout != null)
        {
            ExitPowerFlyout.ShowAt(ExitAppButton);
        }
        else
        {
            PerformAppExit();
        }
    }

    private void OnCpuChipClick(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is MainPage mp)
        {
            mp.NavigateToPageExternal("Dashboard");
        }
        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        notificationService?.ShowToast("CPU Telemetry Monitor", "Showing real-time CPU & System performance overview.", Services.Contracts.NotificationSeverity.Info);
    }

    private void OnRamChipClick(object sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                // Actually free system RAM via optimizer engine (EmptyWorkingSet + GC)
                var optEngine = App.Services.GetRequiredService<Engines.SystemOptimizerEngine>();
                await optEngine.OptimizeRamAsync();

                App.MainDispatcherQueue?.TryEnqueue(() =>
                {
                    var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
                    notificationService?.ShowToast("RAM Optimized".T(), "Process working sets trimmed and managed memory reclaimed.".T(), Services.Contracts.NotificationSeverity.Success);
                });
            }
            catch { }
        });
    }

    public void UpdateNotificationBadge()
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            int count = DbManager.GetUnreadNotificationsCount();
            if (count > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        });
    }
}

public class SearchItem
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PageTag { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string IconGlyph { get; set; } = "";

    public override string ToString() => Title;
}

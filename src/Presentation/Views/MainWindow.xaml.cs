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

    /// <summary>
    /// Dynamically resolved application version using Assembly or Package identity.
    /// </summary>
    public string AppVersion
    {
        get
        {
            try
            {
                var packageVer = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"v{packageVer.Major}.{packageVer.Minor}.{packageVer.Build}";
            }
            catch
            {
                var asmVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return asmVer != null ? $"v{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}" : WinCarePro.Core.AppConstants.DisplayVersion;
            }
        }
    }

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _forceClose = false;
    private Microsoft.UI.Xaml.DispatcherTimer? _clockTimer;
    private IntPtr _hIconBig = IntPtr.Zero;
    private IntPtr _hIconSmall = IntPtr.Zero;
    private EventHandler? _mainLanguageChangedHandler;
    internal EventHandler? _themeAccentChangedHandler;

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
                _hIconBig = LoadImage(IntPtr.Zero, iconPath, 1, 256, 256, 0x00000010); // IMAGE_ICON | LR_LOADFROMFILE
                _hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1, 32, 32, 0x00000010);   // IMAGE_ICON | LR_LOADFROMFILE
                if (_hIconBig != IntPtr.Zero || _hIconSmall != IntPtr.Zero)
                {
                    SetTaskbarIcon(_hIconBig, _hIconSmall);
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

        // Manual preview key handler for Ctrl + K and Ctrl + F to focus search / command bar
        if (this.Content is FrameworkElement rootElem)
        {
            rootElem.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.F || e.Key == Windows.System.VirtualKey.K)
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
        this.AppWindow.Changed += (s, e) =>
        {
            if (e.DidPresenterChange || e.DidVisibilityChange)
            {
                if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized || !this.AppWindow.IsVisible)
                    {
                        // Pause background animations and telemetry when minimized to keep CPU <= 0.3% & RAM <= 15MB
                        WinCarePro.Core.Helpers.Animation3DHelper.StopAll3DScanEffects();
                        WinCarePro.ViewModels.DashboardViewModel.ActiveInstance?.StopMonitoring();
                    }
                    else if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Restored || presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
                    {
                        // Resume monitoring if Dashboard is currently the active page
                        if (App.MainWindowInstance?.Content is Microsoft.UI.Xaml.Controls.Frame rootFrame &&
                            rootFrame.Content is WinCarePro.MainPage mainPage &&
                            mainPage.NavigationFrame?.Content is WinCarePro.Views.DashboardPage)
                        {
                            WinCarePro.ViewModels.DashboardViewModel.ActiveInstance?.StartMonitoring();
                        }
                    }
                }
            }
        };
        this.Closed += MainWindow_Closed;

        // Centralized Theme & Multi-Language Synchronization
        ThemeManager.Instance.RegisterWindow(this);
        TranslationManager.Instance.RegisterWindow(this);
        ThemeManager.Instance.MotionPreferenceChanged += OnMotionPreferenceChanged;
        ApplyMotionPreferences();

        // Decoupled notification listener from Database layer
        DbManager.OnNotificationAdded += OnDbNotificationAdded;
        DbManager.OnNotificationsChanged += UpdateNotificationBadge;

        // Handle window resizing and start async application initialization on load
        if (RootGrid != null)
        {
            if (RootGrid.IsLoaded)
            {
                CenterOnScreen(1560, 920);
                _ = InitializeAppAsync();
            }
            else
            {
                RootGrid.Loaded += (s, e) => {
                    CenterOnScreen(1560, 920);
                    _ = InitializeAppAsync();
                };
            }
        }
        else
        {
            CenterOnScreen(1560, 920);
            _ = InitializeAppAsync();
        }

        _mainLanguageChangedHandler = (s, e) =>
        {
            PopulateSearchRegistry();
            if (SearchBox != null)
            {
                SearchBox.PlaceholderText = "Search tools, diagnostics, settings... (Ctrl + K)".T();
            }
            UpdateTitleBarExperienceMode(WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings.IsProMode);
        };
        TranslationManager.Instance.LanguageChanged += _mainLanguageChangedHandler;

        if (SearchBox != null)
        {
            SearchBox.PlaceholderText = "Search tools, diagnostics, settings... (Ctrl + K)".T();
        }
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
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException("MainWindow.ResizeWindowFallback", ex);
            }
        }
    }

    private int _isAppInitialized = 0;

    private async Task InitializeAppAsync()
    {
        if (Interlocked.Exchange(ref _isAppInitialized, 1) != 0)
        {
            return;
        }

        try
        {
            // 1. Ensure database & telemetry store are synchronized
            await Task.Run(() => SettingsService.Instance.LoadSettings());

            // 2. Load theme settings and transparency levels from DB
            LoadThemeConfiguration();

            // 3. Load language setting and apply translations to window content
            TranslationManager.Instance.LoadLanguageFromSettings();
            if (this.Content != null)
            {
                TranslationManager.Instance.Translate(this.Content);
            }

            // 4. Update notification badge indicator & prepare main view
            UpdateNotificationBadge();
            if (RootFrame != null)
            {
                RootFrame.Navigate(typeof(MainPage));
            }
            UpdateTitleBarExperienceMode(true);

            // 5. Start Clock Ticker
            StartClockTicker();

            // 6. Deferred background tasks: Database maintenance
            if (AppTitleVersionBadge != null)
            {
                AppTitleVersionBadge.Text = AppVersion;
            }
            var currentVersion = WinCarePro.Core.AppConstants.CurrentVersion;
            CheckAndShowChangelog(currentVersion);

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
            // Fallback: Ensure app is navigable if anything throws
            Infrastructure.Logging.CrashLogger.LogException("MainWindow.InitializeAppAsync", ex);
            try { StartClockTicker(); } catch { }
            try { Database.DbManager.LogAction($"Startup failed: {ex.Message}", "System", "Failed"); } catch { }
            try
            {
                if (RootFrame != null && RootFrame.Content == null)
                {
                    RootFrame.Navigate(typeof(MainPage));
                }
            }
            catch (Exception navEx)
            {
                Infrastructure.Logging.CrashLogger.LogException("MainWindow.InitializeAppAsync.FallbackNavigate", navEx);
            }
        }
    }

    private ulong _hudPrevIdleTime;
    private ulong _hudPrevKernelTime;
    private ulong _hudPrevUserTime;
    private bool _hudHasPrevTimes = false;

    private void StartClockTicker()
    {
        try
        {
            UpdateTitleBarTelemetry();
            _clockTimer?.Stop();
            _clockTimer = new Microsoft.UI.Xaml.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _clockTimer.Tick += (s, e) => UpdateTitleBarTelemetry();
            _clockTimer.Start();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("MainWindow.StartClockTicker", ex);
        }
    }

    private void UpdateTitleBarTelemetry()
    {
        if (this.AppWindow == null || !this.AppWindow.IsVisible)
            return;

        try
        {
            if (ClockText != null)
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm");
            }

            var mem = Core.Interop.NativeApi.MEMORYSTATUSEX.Create();
            if (Core.Interop.NativeApi.GlobalMemoryStatusEx(ref mem) && RamChipText != null)
            {
                double ramPercent = mem.dwMemoryLoad;
                RamChipText.Text = $"RAM {ramPercent:F0}%";
            }

            if (Core.Interop.NativeApi.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                ulong idle = idleTime.ToUInt64();
                ulong kernel = kernelTime.ToUInt64();
                ulong user = userTime.ToUInt64();

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

                if (CpuChipText != null)
                {
                    CpuChipText.Text = $"CPU {cpuPercent:F0}%";
                }
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("UpdateTitleBarTelemetry", ex);
        }
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

                // Immediately reclaim background working set memory (Rule 04: <= 15MB)
                Task.Run(() => TrimProcessMemory());
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
        ThemeManager.Instance.MotionPreferenceChanged -= OnMotionPreferenceChanged;
        if (_mainLanguageChangedHandler != null)
        {
            TranslationManager.Instance.LanguageChanged -= _mainLanguageChangedHandler;
        }
        if (_themeAccentChangedHandler != null)
        {
            ThemeManager.Instance.AccentChanged -= _themeAccentChangedHandler;
        }
        DbManager.OnNotificationAdded -= OnDbNotificationAdded;
        DbManager.OnNotificationsChanged -= UpdateNotificationBadge;
        CleanupTrayIcon();
        UnsubclassWindow();
        if (_hIconBig != IntPtr.Zero)
        {
            DestroyIcon(_hIconBig);
            _hIconBig = IntPtr.Zero;
        }
        if (_hIconSmall != IntPtr.Zero)
        {
            DestroyIcon(_hIconSmall);
            _hIconSmall = IntPtr.Zero;
        }
        try
        {
            if (RootFrame?.Content is MainPage mainPage)
            {
                mainPage.CleanupActivePage();
                mainPage.Cleanup();
            }
            else if (RootFrame?.Content is WinCarePro.Views.NetworkPage netPage)
            {
                netPage.ViewModel?.Cleanup();
            }
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("MainWindow_Closed.PageCleanup", ex);
        }

        try
        {
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("MainWindow_Closed.WidgetClose", ex);
        }

        try
        {
            WinCarePro.Services.Implementations.SettingsService.Instance.FlushPendingSave();
            DbManager.ShutdownDatabase();
            App.ReleaseSingleInstanceMutex();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("MainWindow_Closed.DbShutdown", ex);
        }
    }

    private void OnDbNotificationAdded(WinCarePro.Models.NotificationItem item)
    {
        this.DispatcherQueue?.TryEnqueue(() =>
        {
            UpdateNotificationBadge();
            if (item.ShowToast)
            {
                ShowToastFromDb(item.Title, item.Message, item.Level);
            }
        });
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
                        Task.Run(() =>
                        {
                            try
                            {
                                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                                GC.WaitForPendingFinalizers();
                                
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
                            if (RootFrame?.Content is MainPage mp)
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

    public async void PerformAppExit() => await PerformAppExitAsync();

    public async Task PerformAppExitAsync()
    {
        try
        {
            if (RootFrame?.Content is MainPage mainPage)
            {
                mainPage.CleanupActivePage();
                mainPage.Cleanup();
            }
            else if (RootFrame?.Content is WinCarePro.Views.NetworkPage netPage)
            {
                netPage.ViewModel?.Cleanup();
            }

            WinCarePro.Core.Helpers.Animation3DHelper.StopAll3DScanEffects();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("PerformAppExit.Cleanup", ex);
        }

        await Task.Run(() =>
        {
            try
            {
                WinCarePro.Services.Implementations.SettingsService.Instance.FlushPendingSave();
                DbManager.ShutdownDatabase();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.CrashLogger.LogException("PerformAppExit.DbShutdown", ex);
            }
        });

        try
        {
            App.Services?.GetService<Services.Contracts.IBackgroundWatchdogService>()?.Stop();
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("PerformAppExit.WatchdogStop", ex);
        }
        CleanupTrayIcon();

        App.ReleaseSingleInstanceMutex();
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

    private void OnWidgetLaunchClick(object sender, RoutedEventArgs e)
    {
    }

    public void UpdateTitleBarExperienceMode(bool isPro)
    {
        // Unified experience mode — all hubs and tools directly accessible
    }

    private void OnCpuChipClick(object sender, RoutedEventArgs e)
    {
        if (RootFrame?.Content is MainPage mp)
        {
            mp.NavigateToPageExternal("Dashboard");
        }
        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        notificationService?.ShowToast("CPU Telemetry Monitor", "Showing real-time CPU & System performance overview.", Services.Contracts.NotificationSeverity.Info);
    }

    private void OnRamChipClick(object sender, RoutedEventArgs e)
    {
        if (RootFrame?.Content is MainPage mp)
        {
            mp.NavigateToPageExternal("Dashboard");
        }
        var notificationService = App.Services.GetService<Services.Contracts.INotificationService>();
        notificationService?.ShowToast("RAM Telemetry Monitor".T(), "Showing real-time memory metrics on the Dashboard.".T(), Services.Contracts.NotificationSeverity.Info);
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

    private void OnMotionPreferenceChanged(object? sender, EventArgs e)
    {
        ApplyMotionPreferences();
    }

    public void ApplyMotionPreferences()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (AmbientAuraMesh != null)
            {
                AmbientAuraMesh.Visibility = Visibility.Collapsed;
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

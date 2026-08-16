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

    private async void InitializeAppAsync()
    {
        try
        {
            // 1. Initialize SQLite Database asynchronously
            StartupProgressText.Text = "Initializing database...".T();
            await Task.Run(() => Database.DbManager.InitializeDatabase());
            await Task.Delay(300);

            // 2. Load theme settings and transparency levels from DB
            StartupProgressText.Text = "Loading configuration...".T();
            LoadThemeConfiguration();
            await Task.Delay(300);

            // 3. Load language setting and apply translations to window content
            StartupProgressText.Text = "Applying translations...".T();
            TranslationManager.Instance.LoadLanguageFromSettings();
            TranslationManager.Instance.Translate(this.Content);
            await Task.Delay(250);

            // 4. Update notification badge indicator & prepare main view
            StartupProgressText.Text = "Starting WinCare Pro...".T();
            UpdateNotificationBadge();
            RootFrame.Navigate(typeof(MainPage));
            await Task.Delay(350);

            // 5. Start Clock Ticker & fade out splash overlay smoothly
            StartClockTicker();
            FadeOutStartupOverlay.Begin();

            // 7. Deferred background tasks: Index search registry & database maintenance
            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(4, 0, 0, 0);
            CheckAndShowChangelog(currentVersion);

            _ = Task.Run(() =>
            {
                try
                {
                    Database.DbManager.RunDatabaseMaintenance();
                }
                catch { }
            });

            _ = Task.Run(() =>
            {
                try
                {
                    App.MainDispatcherQueue?.TryEnqueue(() => PopulateSearchRegistry());
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            // Fallback: Ensure splash vanishes and app is navigable if anything throws
            StartupOverlayGrid.Visibility = Visibility.Collapsed;
            RootFrame.Navigate(typeof(MainPage));
            StartClockTicker();
            Database.DbManager.LogAction($"Startup failed: {ex.Message}", "System", "Failed");
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

        // Create a DispatcherTimer for periodic updates
        var timer = new Microsoft.UI.Xaml.DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(15);
        timer.Tick += (s, e) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm");
        };
        timer.Start();
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
            string raw = DbManager.GetSettings();
            bool minimizeToTray = false;
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("MinimizeToTray", out var minProp) && minProp.GetBoolean())
                {
                    minimizeToTray = true;
                }
            }

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

    public async void PerformAppExit()
    {
        this.AppWindow.Show();
        BringToForeground();

        ExitOverlayTitle.Text = "Shutting Down".T();
        ExitOverlayMessage.Text = "Closing database connections and freeing resources...".T();
        ExitOverlayGrid.Visibility = Visibility.Visible;
        FadeInExitOverlay.Begin();

        // Step 1: Perform active page cleanup on the UI thread first (synchronous)
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

        // Step 2: Safe database WAL checkpoint & connection pool clear (background, after UI cleanup)
        await Task.Run(() =>
        {
            DbManager.ShutdownDatabase();
        });

        await Task.Delay(1000); // Smooth visual feedback padding

        CleanupTrayIcon();
        _forceClose = true;
        this.Close();
    }

    private void ExitAppButton_Click(object sender, RoutedEventArgs e)
    {
        PerformAppExit();
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

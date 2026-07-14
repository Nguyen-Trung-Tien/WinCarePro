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
                this.AppWindow.SetIcon(iconPath);
            }
        }
        catch { }

        // Subclass window to enforce minimum bounds (1280 x 800)
        SubclassWindow();

        // Extends page content into the top caption bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleDragArea);

        // Manual preview key handler for Ctrl + F to focus search, avoiding WinUI 3 KeyboardAccelerator tooltip bugs and accidental triggers
        this.Content.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.F)
            {
                var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                bool isCtrlDown = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
                if (isCtrlDown)
                {
                    SearchBox.Focus(FocusState.Programmatic);
                    e.Handled = true;
                }
            }
        };

        this.AppWindow.Closing += AppWindow_Closing;
        this.Closed += MainWindow_Closed;

        // Handle window resizing and start async application initialization on load
        RootGrid.Loaded += (s, e) => {
            try
            {
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
            }
            catch { }

            InitializeAppAsync();
        };

        TranslationManager.Instance.LanguageChanged += (s, e) => PopulateSearchRegistry();
    }

    private async void InitializeAppAsync()
    {
        try
        {
            // 1. Initialize SQLite Database asynchronously to prevent blocking the UI thread
            StartupProgressText.Text = "Initializing database...".T();
            await Task.Run(() => Database.DbManager.InitializeDatabase());

            // 2. Load theme settings and transparency levels from DB
            StartupProgressText.Text = "Loading configuration...".T();
            LoadThemeConfiguration();

            // 3. Load language setting and apply translations to window content
            StartupProgressText.Text = "Applying translations...".T();
            TranslationManager.Instance.LoadLanguageFromSettings();
            TranslationManager.Instance.Translate(this.Content);

            // 4. Update notification badge indicator
            UpdateNotificationBadge();

            // 5. Index pages, actions, and settings keywords for Search bar
            StartupProgressText.Text = "Indexing search registry...".T();
            PopulateSearchRegistry();

            // 6. Check for app changelog version bumps and trigger database optimization maintenance in background
            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(2, 0, 0, 0);
            CheckAndShowChangelog(currentVersion);
            _ = Task.Run(() => Database.DbManager.RunDatabaseMaintenance());

            StartupProgressText.Text = "Starting WinCare Pro...".T();
            await Task.Delay(400); // Visual padding delay

            // 7. Navigate Frame to MainPage
            RootFrame.Navigate(typeof(MainPage));

            // 8. Start Clock Ticker
            StartClockTicker();

            // 9. Play splash fade out animation
            FadeOutStartupOverlay.Begin();
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
        timer.Interval = TimeSpan.FromSeconds(30);
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
        try
        {
            if (RootFrame.Content is WinCarePro.Views.NetworkPage netPage)
            {
                netPage.ViewModel?.Cleanup();
            }
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
                                var optEngine = new Engines.SystemOptimizerEngine();
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

    private async void ExitAppButton_Click(object sender, RoutedEventArgs e)
    {
        ExitOverlayTitle.Text = "Shutting Down".T();
        ExitOverlayMessage.Text = "Closing database connections and freeing resources...".T();
        ExitOverlayGrid.Visibility = Visibility.Visible;
        FadeInExitOverlay.Begin();

        // Let user experience the fade animation and show database cleanup context
        await Task.Delay(1500);

        CleanupTrayIcon();
        _forceClose = true;
        this.Close();
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

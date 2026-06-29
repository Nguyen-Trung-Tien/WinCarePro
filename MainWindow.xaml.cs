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

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _forceClose = false;

    // WndProc subclassing to enforce minimum window dimensions (1280x800)
    private delegate IntPtr WinProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WinProc? _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private const int GWL_WNDPROC = -4;
    private const uint WM_GETMINMAXINFO = 0x0024;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    public MainWindow()
    {
        InitializeComponent();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Custom window size (1400 x 900)
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

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

        // Translate and apply theme configurations on load
        RootGrid.Loaded += (s, e) => {
            LoadThemeConfiguration();
            TranslationManager.Instance.Translate(this.Content);
            UpdateNotificationBadge();

            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(2, 0, 0, 0);
            CheckAndShowChangelog(currentVersion);
        };

        // Navigate page frame
        RootFrame.Navigate(typeof(MainPage));

        // Start live clock ticker
        StartClockTicker();

        // Initialize Suggestion Search Registry
        PopulateSearchRegistry();
        TranslationManager.Instance.LanguageChanged += (s, e) => PopulateSearchRegistry();
    }

    private void SubclassWindow()
    {
        if (_hwnd == IntPtr.Zero) return;

        _newWndProc = new WinProc(NewWindowProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int NIM_ADD = 0;
    private const int NIM_MODIFY = 1;
    private const int NIM_DELETE = 2;
    private const int NIF_MESSAGE = 1;
    private const int NIF_ICON = 2;
    private const int NIF_TIP = 4;
    private const int NIF_INFO = 0x10;
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1024;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private bool _trayIconRegistered = false;
    private IntPtr _hIcon = IntPtr.Zero;

    private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            
            // Set minimum track sizing constraints (800 x 600 px)
            mmi.ptMinTrackSize.x = 800;
            mmi.ptMinTrackSize.y = 600;

            Marshal.StructureToPtr(mmi, lParam, false);
            return IntPtr.Zero;
        }
        else if (msg == WM_TRAYICON)
        {
            int eventId = (int)lParam;
            if (eventId == WM_LBUTTONDBLCLK || eventId == WM_RBUTTONUP)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.AppWindow.Show();
                    BringToForeground();
                });
            }
            return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
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
                // Show exit overlay with translation and close gracefully
                ExitOverlayTitle.Text = "Shutting Down".T();
                ExitOverlayMessage.Text = "Closing database connections and freeing resources...".T();
                ExitOverlayGrid.Visibility = Visibility.Visible;
                FadeInExitOverlay.Begin();

                await Task.Delay(1500);

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

    private void InitializeTrayIcon()
    {
        if (_trayIconRegistered) return;

        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "AppIcon.ico");
            }
            
            _hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x00000010 | 0x00000020); // IMAGE_ICON | LR_LOADFROMFILE

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "WinCare Pro Suite",
                szInfo = "WinCare Pro is running in the background. Double-click the tray icon to open.",
                szInfoTitle = "Minimized to System Tray",
                dwInfoFlags = 1 // NIIF_INFO
            };

            _trayIconRegistered = Shell_NotifyIcon(NIM_ADD, ref nid);
        }
        catch { }
    }

    private void CleanupTrayIcon()
    {
        if (_trayIconRegistered)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            _trayIconRegistered = false;
        }

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }

    private void BringToForeground()
    {
        if (_hwnd != IntPtr.Zero)
        {
            ShowWindow(_hwnd, 9); // SW_RESTORE
            SetForegroundWindow(_hwnd);
        }
    }

    private void LoadThemeConfiguration()
    {
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("Theme", out var themeProp))
                {
                    string theme = themeProp.GetString() ?? "Dark";
                    ApplyAppTheme(theme == "Dark");
                }
                else
                {
                    ApplyAppTheme(true);
                }

                // Load and apply Accent Color on start
                if (root.TryGetProperty("AccentColor", out var accentProp))
                {
                    App.ApplyAccentColor(accentProp.GetString() ?? "Default");
                }

                // Load and apply Transparency Level on start
                if (root.TryGetProperty("TransparencyLevel", out var transProp))
                {
                    ApplyTransparency(transProp.GetDouble());
                }

                // Check for updates automatically in the background
                if (root.TryGetProperty("AutoCheckUpdates", out var autoProp) && autoProp.GetBoolean())
                {
                    Task.Delay(3000).ContinueWith(async t =>
                    {
                        await RunSilentUpdateCheckAsync();
                    });
                }
            }
            else
            {
                ApplyAppTheme(true); // Default to Dark
            }
        }
        catch
        {
            ApplyAppTheme(true);
        }
    }

    public void ApplyTransparency(double level)
    {
        if (RootGrid == null) return;
        
        bool isDark = RootGrid.RequestedTheme == ElementTheme.Dark;
        byte colorAlpha = (byte)(255 * (level / 100.0));
        
        if (isDark)
        {
            RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 26, 26, 26));
        }
        else
        {
            RootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(colorAlpha, 243, 243, 243));
        }
    }

    private string? _downloadedSetupPath = null;

    private async Task RunSilentUpdateCheckAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
            
            string response;
            // Check for local update.json in app directory (for offline/dev testing)
            string localUpdatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.json");
            if (File.Exists(localUpdatePath))
            {
                response = File.ReadAllText(localUpdatePath);
            }
            else
            {
                string jsonUrl = "https://raw.githubusercontent.com/Nguyen-Trung-Tien/WinCarePro/main/update.json";
                client.Timeout = TimeSpan.FromSeconds(10);
                response = await client.GetStringAsync(jsonUrl);
            }
            
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            string remoteVerStr = root.GetProperty("version").GetString() ?? "2.0.0";
            string downloadUrl = root.GetProperty("url").GetString() ?? "";
            
            var currentVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(2, 0, 0, 0);
            var remoteVersion = new Version(remoteVerStr);

            if (remoteVersion > currentVersion)
            {
                DbManager.LogAction($"Update available: v{remoteVerStr}", "Software Updater", "Success");
                
                // Read configuration to determine if we should auto install
                bool autoInstall = false;
                string settingsRaw = DbManager.GetSettings();
                if (!string.IsNullOrEmpty(settingsRaw))
                {
                    using var setDoc = JsonDocument.Parse(settingsRaw);
                    if (setDoc.RootElement.TryGetProperty("AutoInstallUpdates", out var autoInstallProp))
                    {
                        autoInstall = autoInstallProp.GetBoolean();
                    }
                }

                if (autoInstall)
                {
                    _ = DownloadBackgroundUpdateAsync(downloadUrl, remoteVerStr);
                }
                else
                {
                    DbManager.AddNotification("Software Update Available".T(), string.Format("A new version v{0} of WinCare Pro is available for download.".T(), remoteVerStr), "Warning");
                }
            }
        }
        catch { }
    }

    private async Task DownloadBackgroundUpdateAsync(string downloadUrl, string remoteVerStr)
    {
        if (string.IsNullOrEmpty(downloadUrl)) return;
        
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WinCareProUpdater/1.0)");
            
            using var response = await client.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            string tempFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string setupFilePath = Path.Combine(tempFolder, $"WinCarePro_Setup_{remoteVerStr}.exe");

            using var fileStream = new FileStream(setupFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            int read;
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
            }
            fileStream.Close();

            _downloadedSetupPath = setupFilePath;

            this.DispatcherQueue.TryEnqueue(() =>
            {
                DbManager.AddNotification(
                    "Update Ready to Install".T(),
                    string.Format("Version {0} is successfully downloaded. Click here to restart and install now.".T(), remoteVerStr),
                    "Success"
                );
            });
        }
        catch (Exception ex)
        {
            DbManager.LogAction($"Background download failed: {ex.Message}", "Software Updater", "Failed");
        }
    }

    public StackPanel ToastStackContainer => ToastContainer;

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

    public void ApplyAppTheme(bool dark)
    {
        Services.ThemeManager.Instance.ApplyTheme(dark ? ElementTheme.Dark : ElementTheme.Light);
    }

    public void SetBackdropType(string type)
    {
        try
        {
            this.SystemBackdrop = type.ToLower() switch
            {
                "mica" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base },
                "micaalt" => new Microsoft.UI.Xaml.Media.MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                "acrylic" => new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop(),
                _ => new Microsoft.UI.Xaml.Media.MicaBackdrop()
            };
        }
        catch { }
    }



    // Theme Switcher Toggle
    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDark = RootGrid.RequestedTheme == ElementTheme.Dark;
        bool nextIsDark = !isCurrentlyDark;
        ApplyAppTheme(nextIsDark);

        // Update stored settings
        try
        {
            string raw = DbManager.GetSettings();
            var settingsDict = new System.Collections.Generic.Dictionary<string, object>();
            if (!string.IsNullOrEmpty(raw))
            {
                var parsed = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(raw);
                if (parsed != null) settingsDict = parsed;
            }
            settingsDict["Theme"] = nextIsDark ? "Dark" : "Light";
            string themeJson = JsonSerializer.Serialize(settingsDict);
            Task.Run(() => DbManager.SaveSettings(themeJson));
        }
        catch { }
    }

    private List<SearchItem> _searchRegistry = new();

    private void PopulateSearchRegistry()
    {
        _searchRegistry = new List<SearchItem>
        {
            new SearchItem { Title = "Dashboard".T(), Description = "Real-time performance monitors and system health overview.".T(), PageTag = "Dashboard", Keywords = "home diagnostic dashboard trang chu main", IconGlyph = "\uE80F" },
            new SearchItem { Title = "Junk Cleaner".T(), Description = "Clean local application temp data, cache, and recycle bin.".T(), PageTag = "Junk", Keywords = "clean junk temp cache trash don rac", IconGlyph = "\uE74D" },
            new SearchItem { Title = "App Uninstaller".T(), Description = "Uninstall desktop software and system packages completely.".T(), PageTag = "Uninstall", Keywords = "uninstall remove program app uninstaller go ung dung", IconGlyph = "\uE77C" },
            new SearchItem { Title = "Network Center".T(), Description = "Run ping diagnostic benchmarks, network speed tests, and optimize DNS settings.".T(), PageTag = "Network", Keywords = "network ping dns benchmark speed test internet mang", IconGlyph = "\uE809" },
            new SearchItem { Title = "System Repair".T(), Description = "Fix broken Windows components, SFC scan, and DISM restore.".T(), PageTag = "Repair", Keywords = "repair sfc dism fix component system sua loi", IconGlyph = "\uE777" },
            new SearchItem { Title = "Security Shield".T(), Description = "Evaluate security parameters, defender configurations, and privacy safeguards.".T(), PageTag = "Security", Keywords = "security defender shield privacy firewalls bao mat", IconGlyph = "\uE727" },
            new SearchItem { Title = "System Optimizer".T(), Description = "Tune speed settings, clean system memory RAM, and apply OS custom tweaks.".T(), PageTag = "Optimizer", Keywords = "optimizer ram speed performance memory boost toi uu", IconGlyph = "\uE916" },
            new SearchItem { Title = "Startup & Services".T(), Description = "Configure system startup apps, background services, and delay triggers.".T(), PageTag = "Startup", Keywords = "startup services boot background tasks khoi dong dich vu", IconGlyph = "\uE7B4" },
            new SearchItem { Title = "Process Manager".T(), Description = "Monitor and manage running tasks, CPU loads, and active processes.".T(), PageTag = "Process", Keywords = "process task manager kill cpu memory memory load tien trinh", IconGlyph = "\uE9D9" },
            new SearchItem { Title = "Disk Tools".T(), Description = "Analyze storage layout, find duplicates, and clean heavy directories.".T(), PageTag = "Disk", Keywords = "disk storage folder analysis duplicates o dia", IconGlyph = "\uE770" },
            new SearchItem { Title = "Hardware Center".T(), Description = "View detailed CPU, GPU, RAM, battery sensors, and device info.".T(), PageTag = "Hardware", Keywords = "hardware hardware cpu gpu specifications phan cung sensor", IconGlyph = "\uE950" },
            new SearchItem { Title = "Registry Center".T(), Description = "Backup, restore, and repair broken system registry keys.".T(), PageTag = "Registry", Keywords = "registry backup hive restore scan database quan ly registry", IconGlyph = "\uE7B4" },
            new SearchItem { Title = "Software Updater".T(), Description = "Check and install updates for installed applications.".T(), PageTag = "Updater", Keywords = "software updater winget upgrade phan mem", IconGlyph = "\uE895" },
            new SearchItem { Title = "Driver Updater".T(), Description = "Scan and refresh system hardware driver files.".T(), PageTag = "Driver", Keywords = "driver updater hardware components cap nhat driver", IconGlyph = "\uE9A1" },
            new SearchItem { Title = "Settings".T(), Description = "Personalization configurations, language options, and background scheduling.".T(), PageTag = "Settings", Keywords = "settings theme accent transparent language config cai dat", IconGlyph = "\uE713" }
        };
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (char c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLower();
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string rawQuery = sender.Text.Trim();
            if (string.IsNullOrEmpty(rawQuery))
            {
                sender.ItemsSource = null;
                return;
            }

            string cleanQuery = RemoveDiacritics(rawQuery);
            var results = new List<SearchItemScore>();

            foreach (var item in _searchRegistry)
            {
                string cleanTitle = RemoveDiacritics(item.Title);
                string cleanDesc = RemoveDiacritics(item.Description);
                string cleanKeywords = RemoveDiacritics(item.Keywords);

                int score = 0;
                if (cleanTitle.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 100; // Exact match
                }
                else if (cleanTitle.StartsWith(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 80;
                }
                else if (cleanTitle.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 60;
                }
                else if (cleanKeywords.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 40;
                }
                else if (cleanDesc.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score = 20;
                }

                if (score > 0)
                {
                    results.Add(new SearchItemScore { Item = item, Score = score });
                }
            }

            sender.ItemsSource = results.OrderByDescending(x => x.Score).Select(x => x.Item).ToList();
        }
    }

    private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchItem item)
        {
            if (!string.IsNullOrEmpty(item.PageTag) && RootFrame.Content is MainPage mainPage)
            {
                mainPage.NavigateToPageExternal(item.PageTag);
            }
        }
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchItem item)
        {
            if (!string.IsNullOrEmpty(item.PageTag) && RootFrame.Content is MainPage mainPage)
            {
                mainPage.NavigateToPageExternal(item.PageTag);
            }
            return;
        }

        string query = sender.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        string cleanQuery = RemoveDiacritics(query);
        var firstMatch = _searchRegistry
            .Select(i => new { Item = i, CleanTitle = RemoveDiacritics(i.Title), CleanKeywords = RemoveDiacritics(i.Keywords) })
            .FirstOrDefault(x => x.CleanTitle.Contains(cleanQuery) || x.CleanKeywords.Contains(cleanQuery));

        if (firstMatch != null && RootFrame.Content is MainPage mainPage2)
        {
            mainPage2.NavigateToPageExternal(firstMatch.Item.PageTag);
        }
    }

    private class SearchItemScore
    {
        public SearchItem Item { get; set; } = null!;
        public int Score { get; set; }
    }

    private void NotificationButton_Click(object sender, RoutedEventArgs e)
    {
        if (RootFrame.Content is MainPage mainPage)
        {
            mainPage.NavigateToNotificationPage();
            DbManager.MarkAllNotificationsAsRead();
            UpdateNotificationBadge();
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

        _forceClose = true;
        this.Close();
    }

    private void InstallDownloadedUpdate()
    {
        if (string.IsNullOrEmpty(_downloadedSetupPath) || !File.Exists(_downloadedSetupPath))
        {
            var service = App.Services.GetService<Services.Contracts.INotificationService>();
            service?.ShowError("Installer Not Found".T(), "The downloaded update installer could not be found. Please check again.");
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _downloadedSetupPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        catch (Exception ex)
        {
            var service = App.Services.GetService<Services.Contracts.INotificationService>();
            service?.ShowError("Installation Failed".T(), string.Format("Could not start installer: {0}".T(), ex.Message));
        }
    }

    private void CheckAndShowChangelog(Version currentVersion)
    {
        try
        {
            string raw = DbManager.GetSettings();
            string lastVersionStr = "";
            bool versionChanged = false;

            if (!string.IsNullOrEmpty(raw))
            {
                using (var doc = JsonDocument.Parse(raw))
                {
                    if (doc.RootElement.TryGetProperty("LastVersion", out var verProp))
                    {
                        lastVersionStr = verProp.GetString() ?? "";
                    }
                }
            }

            if (string.IsNullOrEmpty(lastVersionStr))
            {
                versionChanged = true;
            }
            else
            {
                var lastVersion = new Version(lastVersionStr);
                if (currentVersion > lastVersion)
                {
                    versionChanged = true;
                }
            }

            if (versionChanged)
            {
                string newRaw = MergeSetting(raw, "LastVersion", currentVersion.ToString());
                Task.Run(() => DbManager.SaveSettings(newRaw));

                // Log to Activity Log and add to Notifications database!
                string logMessage = string.Format("System updated to version {0}".T(), currentVersion.ToString());
                DbManager.LogAction(logMessage, "System", "Success");

                string notificationTitle = string.Format("System Updated to Version {0}".T(), currentVersion.ToString());
                string notificationMessage = 
                    "WinCare Pro has been successfully updated.".T() + "\n\n" +
                    "What's New:".T() + "\n" +
                    "• " + "Responsive Layout: Dynamic collapse on narrower viewports.".T() + "\n" +
                    "• " + "Skeleton Loader: Beautiful entry shimmer layouts during database scan.".T() + "\n" +
                    "• " + "Stability Fixes: Upgraded SQLite concurrent engines with WAL journal mode.".T() + "\n" +
                    "• " + "Theme Consistency: Optimized contrast ratios for elements in Light Mode.".T();

                DbManager.AddNotification(notificationTitle, notificationMessage, "Success", showToast: false);
            }
        }
        catch { }
    }

    private string MergeSetting(string rawJson, string key, string value)
    {
        var dict = new System.Collections.Generic.Dictionary<string, object>();
        if (!string.IsNullOrEmpty(rawJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(rawJson);
                if (parsed != null)
                {
                    dict = parsed;
                }
            }
            catch { }
        }
        dict[key] = value;
        return JsonSerializer.Serialize(dict);
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

    public Frame MainFrame => RootFrame;
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

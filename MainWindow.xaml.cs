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

namespace WinCarePro;

public sealed partial class MainWindow : Window
{

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

        // Subclass window to enforce minimum bounds (1280 x 800)
        SubclassWindow();

        // Extends page content into the top caption bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleDragArea);

        // Load current configurations
        LoadThemeConfiguration();

        // Keyboard Accelerator for Ctrl + F to focus search
        var ctrlF = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.F,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        };
        ctrlF.Invoked += (s, e) =>
        {
            SearchBox.Focus(FocusState.Programmatic);
            e.Handled = true;
        };
        this.Content.KeyboardAccelerators.Add(ctrlF);

        this.AppWindow.Closing += AppWindow_Closing;
        this.Closed += MainWindow_Closed;

        // Translate window contents on load
        RootGrid.Loaded += (s, e) => {
            TranslationManager.Instance.Translate(this.Content);
            UpdateNotificationBadge();
        };

        // Navigate page frame
        RootFrame.Navigate(typeof(MainPage));

        // Start live clock ticker
        StartClockTicker();
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

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_forceClose)
        {
            CleanupTrayIcon();
            return;
        }
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("MinimizeToTray", out var minProp) && minProp.GetBoolean())
                {
                    args.Cancel = true;
                    this.AppWindow.Hide();
                    InitializeTrayIcon();
                }
                else
                {
                    CleanupTrayIcon();
                }
            }
            else
            {
                CleanupTrayIcon();
            }
        }
        catch
        {
            CleanupTrayIcon();
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        CleanupTrayIcon();
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

    public void ShowToastFromDb(string title, string message, string level)
    {
        bool showNotifications = true;
        bool playSound = true;
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("ShowNotifications", out var showProp) && !showProp.GetBoolean())
                {
                    showNotifications = false;
                }
                if (root.TryGetProperty("NotificationSound", out var soundProp) && !soundProp.GetBoolean())
                {
                    playSound = false;
                }
                
                // Specific notification type filters
                if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    if (title.Contains("Update") && root.TryGetProperty("ShowUpdateNotifications", out var upProp) && !upProp.GetBoolean())
                        showNotifications = false;
                    else if (root.TryGetProperty("NotifyOnLowHealth", out var lhProp) && !lhProp.GetBoolean())
                        showNotifications = false;
                }
                else if (level.Equals("Info", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("NotifyOnMaintenance", out var maintProp) && !maintProp.GetBoolean())
                        showNotifications = false;
                }
            }
        }
        catch { }

        if (showNotifications)
        {
            if (playSound)
            {
                try { MessageBeep(0); } catch { }
            }
            ShowToastNotification(title, message, level);
        }
    }

    public void ShowToastNotification(string title, string message, string level, string targetPage = "")
    {
        this.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var toastBorder = new Border
                {
                    Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 12, 12, 12),
                    Width = 340,
                    Margin = new Thickness(0, 0, 0, 8),
                    Opacity = 0
                };

                var toastGrid = new Grid();
                toastGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                toastGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                toastGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

                string glyph = "\uE946"; // Info
                string hexColor = "#FF3B82F6"; // Blue
                if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    glyph = "\uE7BA";
                    hexColor = "#FFF59E0B"; // Amber
                }
                else if (level.Equals("Critical", StringComparison.OrdinalIgnoreCase))
                {
                    glyph = "\uEA39";
                    hexColor = "#FFEF4444"; // Red
                }
                else if (level.Equals("Success", StringComparison.OrdinalIgnoreCase))
                {
                    glyph = "\uE73E";
                    hexColor = "#FF10B981"; // Green
                }

                var statusBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 
                    Convert.ToByte(hexColor.Substring(1, 2), 16), 
                    Convert.ToByte(hexColor.Substring(3, 2), 16), 
                    Convert.ToByte(hexColor.Substring(5, 2), 16)));

                toastBorder.BorderBrush = statusBrush;
                toastBorder.BorderThickness = new Thickness(4, 1, 1, 1);

                var icon = new FontIcon
                {
                    Glyph = glyph,
                    FontSize = 16,
                    Foreground = statusBrush,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                Grid.SetColumn(icon, 0);
                toastGrid.Children.Add(icon);

                var textStack = new StackPanel { Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                var titleBlock = new TextBlock { Text = title, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 12.5, TextWrapping = TextWrapping.Wrap };
                var descBlock = new TextBlock { Text = message, FontSize = 11, Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
                textStack.Children.Add(titleBlock);
                textStack.Children.Add(descBlock);
                Grid.SetColumn(textStack, 1);
                toastGrid.Children.Add(textStack);

                var closeBtn = new Button
                {
                    Style = (Style)Application.Current.Resources["DateTimeFlyoutCalendarButtonStyle"],
                    Content = new FontIcon { Glyph = "\uE711", FontSize = 10, Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] },
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(closeBtn, 2);
                toastGrid.Children.Add(closeBtn);

                toastBorder.Child = toastGrid;

                var trans = new TranslateTransform { X = 360 };
                toastBorder.RenderTransform = trans;

                var sb = new Storyboard();
                var animX = new DoubleAnimation { From = 360, To = 0, Duration = TimeSpan.FromMilliseconds(300) };
                var animOpacity = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(250) };

                Storyboard.SetTarget(animX, toastBorder);
                Storyboard.SetTargetProperty(animX, "(UIElement.RenderTransform).(TranslateTransform.X)");

                Storyboard.SetTarget(animOpacity, toastBorder);
                Storyboard.SetTargetProperty(animOpacity, "Opacity");

                sb.Children.Add(animX);
                sb.Children.Add(animOpacity);

                toastBorder.PointerPressed += async (s, e) =>
                {
                    if (title.Contains("Update Ready") && !string.IsNullOrEmpty(_downloadedSetupPath) && File.Exists(_downloadedSetupPath))
                    {
                        try
                        {
                            bool createRp = true;
                            string rawSettings = DbManager.GetSettings();
                            if (!string.IsNullOrEmpty(rawSettings))
                            {
                                using var setDoc = JsonDocument.Parse(rawSettings);
                                if (setDoc.RootElement.TryGetProperty("CreateRestorePoint", out var rpProp))
                                {
                                    createRp = rpProp.GetBoolean();
                                }
                            }

                            if (createRp)
                            {
                                var regEng = new Engines.RegistryBackupEngine();
                                await Task.Run(() => regEng.CreateSystemRestorePoint("Before WinCare Pro Auto Update".T()));
                            }

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = _downloadedSetupPath,
                                Arguments = "/SILENT /SP- /NOICONS /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS",
                                UseShellExecute = true
                            });
                            Application.Current.Exit();
                        }
                        catch { }
                    }
                    else
                    {
                        string pageTag = targetPage;
                        if (string.IsNullOrEmpty(pageTag))
                        {
                            string lowerTitle = title.ToLower();
                            if (lowerTitle.Contains("update") || lowerTitle.Contains("software") || lowerTitle.Contains("version"))
                            {
                                pageTag = "Updater";
                            }
                            else if (lowerTitle.Contains("notification") || lowerTitle.Contains("alert") || lowerTitle.Contains("clean") || lowerTitle.Contains("boost"))
                            {
                                pageTag = "notification";
                            }
                        }

                        if (!string.IsNullOrEmpty(pageTag))
                        {
                            if (RootFrame.Content is MainPage mp)
                            {
                                mp.NavigateToPageExternal(pageTag);
                            }
                        }
                        DismissToast(toastBorder);
                    }
                };

                closeBtn.Click += (s, e) => { DismissToast(toastBorder); };

                ToastContainer.Children.Add(toastBorder);
                sb.Begin();

                Task.Delay(6000).ContinueWith(t =>
                {
                    this.DispatcherQueue.TryEnqueue(() => DismissToast(toastBorder));
                });
            }
            catch { }
        });
    }

    private void DismissToast(Border border)
    {
        if (!ToastContainer.Children.Contains(border)) return;

        var sb = new Storyboard();
        var animX = new DoubleAnimation { To = 360, Duration = TimeSpan.FromMilliseconds(250) };
        var animOpacity = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };

        Storyboard.SetTarget(animX, border);
        Storyboard.SetTargetProperty(animX, "(UIElement.RenderTransform).(TranslateTransform.X)");

        Storyboard.SetTarget(animOpacity, border);
        Storyboard.SetTargetProperty(animOpacity, "Opacity");

        sb.Children.Add(animX);
        sb.Children.Add(animOpacity);
        sb.Completed += (s, e) =>
        {
            ToastContainer.Children.Remove(border);
        };
        sb.Begin();
    }

    public void ApplyAppTheme(bool dark)
    {
        if (RootGrid == null)
        {
            throw new NullReferenceException("RootGrid is null in ApplyAppTheme");
        }
        if (ThemeIcon == null)
        {
            throw new NullReferenceException("ThemeIcon is null in ApplyAppTheme");
        }
        RootGrid.RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
        ThemeIcon.Glyph = dark ? "\uE708" : "\uE706"; // Moon vs Sun glyph
        SetBackdropType(dark ? "micaalt" : "mica");

        try
        {
            if (_hwnd != IntPtr.Zero)
            {
                int isDark = dark ? 1 : 0;
                DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDark, sizeof(int));
            }
        }
        catch { }

        try
        {
            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = this.AppWindow.TitleBar;
                if (dark)
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 45, 45, 45);
                    titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 80, 80, 80);
                    titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                }
                else
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 230, 230, 230);
                    titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 200, 200, 200);
                    titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                }
            }
        }
        catch { }
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
            DbManager.SaveSettings(JsonSerializer.Serialize(settingsDict));
        }
        catch { }
    }

    // Global Search AutoSuggestBox event handling
    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string query = sender.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(query)) return;

        string pageTag = "";
        if (query.Contains("dọn") || query.Contains("clean") || query.Contains("rác") || query.Contains("junk"))
            pageTag = "Junk";
        else if (query.Contains("sửa") || query.Contains("repair") || query.Contains("lỗi"))
            pageTag = "Repair";
        else if (query.Contains("tiến") || query.Contains("process") || query.Contains("task") || query.Contains("chạy"))
            pageTag = "Process";
        else if (query.Contains("startup"))
            pageTag = "Startup";
        else if (query.Contains("đĩa") || query.Contains("disk") || query.Contains("storage") || query.Contains("trùng"))
            pageTag = "Disk";
        else if (query.Contains("mạng") || query.Contains("network") || query.Contains("ping") || query.Contains("dns"))
            pageTag = "Network";
        else if (query.Contains("bảo") || query.Contains("security") || query.Contains("defender") || query.Contains("shield"))
            pageTag = "Security";
        else if (query.Contains("phần mềm") || query.Contains("app") || query.Contains("uninstall"))
            pageTag = "Uninstall";
        else if (query.Contains("update") || query.Contains("updater"))
            pageTag = "Updater";
        else if (query.Contains("driver") || query.Contains("drivers"))
            pageTag = "Driver";
        else if (query.Contains("giám") || query.Contains("monitor") || query.Contains("telemetry") || query.Contains("phần cứng") || query.Contains("battery"))
            pageTag = "Hardware";
        else if (query.Contains("registry") || query.Contains("sao lưu") || query.Contains("backup"))
            pageTag = "Registry";
        else if (query.Contains("optimizer") || query.Contains("tối ưu"))
            pageTag = "Optimizer";
        else if (query.Contains("cài") || query.Contains("setting") || query.Contains("cấu hình"))
            pageTag = "Settings";

        if (!string.IsNullOrEmpty(pageTag) && RootFrame.Content is MainPage mainPage)
        {
            mainPage.NavigateToPageExternal(pageTag);
        }
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
        ExitOverlayGrid.Visibility = Visibility.Visible;
        FadeInExitOverlay.Begin();
        
        // Let user experience the fade animation and show database cleanup context
        await Task.Delay(1500);
        
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

    public Frame MainFrame => RootFrame;
}

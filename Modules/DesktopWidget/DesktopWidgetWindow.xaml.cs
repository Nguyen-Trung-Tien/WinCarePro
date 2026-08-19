using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Engines;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Modules.DesktopWidget
{
    public class HudStateConfig
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 440;
        public int Height { get; set; } = 245;
        public bool IsAlwaysOnTop { get; set; } = true;
        public bool IsCompact { get; set; } = false;
    }

    public sealed partial class DesktopWidgetWindow : Window
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = (IntPtr)0;
        private static readonly IntPtr ICON_BIG = (IntPtr)1;

        private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData);

        private SUBCLASSPROC? _subclassProc;

        private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData)
        {
            const uint WM_SYSCOMMAND = 0x0112;
            const uint WM_NCLBUTTONDOWN = 0x00A1;
            const uint WM_NCLBUTTONUP = 0x00A2;
            const uint WM_NCLDBLCLK = 0x00A3;
            const IntPtr HTMAXBUTTON = (IntPtr)9;
            const IntPtr HTCAPTION = (IntPtr)2;

            if (uMsg == WM_SYSCOMMAND)
            {
                uint cmd = (uint)(wParam.ToInt64() & 0xFFF0);
                if (cmd == 0xF030 || cmd == 0xF120) // SC_MAXIMIZE / SC_RESTORE
                {
                    DispatcherQueue.TryEnqueue(() => OnToggleCompactModeClick(null, null));
                    return IntPtr.Zero; // Intercept SC_MAXIMIZE to prevent DWM full-screen animation flicker!
                }
            }
            else if (uMsg == WM_NCLBUTTONDOWN && wParam == HTMAXBUTTON)
            {
                return IntPtr.Zero; // Suppress DWM maximize button press animation flicker
            }
            else if (uMsg == WM_NCLBUTTONUP && wParam == HTMAXBUTTON)
            {
                DispatcherQueue.TryEnqueue(() => OnToggleCompactModeClick(null, null));
                return IntPtr.Zero;
            }
            else if (uMsg == WM_NCLDBLCLK && wParam == HTCAPTION)
            {
                DispatcherQueue.TryEnqueue(() => OnToggleCompactModeClick(null, null));
                return IntPtr.Zero; // Prevent double-clicking titlebar from triggering DWM maximize flicker
            }

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private FILETIME _prevIdleTime;
        private FILETIME _prevKernelTime;
        private FILETIME _prevUserTime;
        private bool _hasPrevTimes = false;

        private static ulong FileTimeToUInt64(FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
        }

        private static DesktopWidgetWindow? _currentInstance;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _badgeResetTimer;
        private readonly EventHandler _themeHandler;
        private readonly EventHandler _langHandler;

        private bool _isAlwaysOnTop = true;
        private bool _isCompact = false;
        private bool _isBoosting = false;

        private long _lastBytesReceived = 0;
        private long _lastBytesSent = 0;
        private DateTime _lastNetworkCheckTime = DateTime.Now;

        private static readonly SolidColorBrush GreenBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129));
        private static readonly SolidColorBrush AmberBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
        private static readonly SolidColorBrush RedBrush = new(Windows.UI.Color.FromArgb(255, 239, 68, 68));
        private static readonly SolidColorBrush BlueBrush = new(Windows.UI.Color.FromArgb(255, 59, 130, 246));

        public static void ShowWindow()
        {
            if (_currentInstance == null)
            {
                _currentInstance = new DesktopWidgetWindow();
                _currentInstance.Activate();
            }
            else
            {
                _currentInstance.AppWindow.Show();
                _currentInstance.Activate();
            }
        }

        public DesktopWidgetWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;

            // Subclass HWND to intercept Win32 SC_MAXIMIZE / HTMAXBUTTON and prevent DWM maximize flicker!
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd != IntPtr.Zero)
            {
                _subclassProc = new SUBCLASSPROC(WndProc);
                SetWindowSubclass(hwnd, _subclassProc, 101, IntPtr.Zero);
            }

            // Set Window and Taskbar / Alt-Tab Icon
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "AppIcon.ico");
                }
                if (File.Exists(iconPath))
                {
                    this.AppWindow.SetIcon(iconPath);

                    if (hwnd != IntPtr.Zero)
                    {
                        var hIconBig = LoadImage(IntPtr.Zero, iconPath, 1, 256, 256, 0x00000010);
                        var hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1, 32, 32, 0x00000010);
                        if (hIconBig != IntPtr.Zero) SendMessage(hwnd, WM_SETICON, ICON_BIG, hIconBig);
                        if (hIconSmall != IntPtr.Zero) SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
                    }
                }
            }
            catch { }

            this.Closed += (s, e) =>
            {
                if (hwnd != IntPtr.Zero && _subclassProc != null)
                {
                    RemoveWindowSubclass(hwnd, _subclassProc, 101);
                }
                ThemeManager.Instance.UnregisterWindow(this);
                TranslationManager.Instance.UnregisterWindow(this);
                _currentInstance = null;
            };

            // Register with ThemeManager and TranslationManager for centralized synchronization
            ThemeManager.Instance.RegisterWindow(this);
            TranslationManager.Instance.RegisterWindow(this);

            // Load position, size and view state configuration
            var state = LoadStateConfig();
            _isAlwaysOnTop = state.IsAlwaysOnTop;
            _isCompact = state.IsCompact;

            // Apply size & compact state
            ApplyViewModeState(state.Width, state.Height);

            // Move to saved position if valid and on screen
            if (state.X >= 0 && state.Y >= 0)
            {
                EnsureWindowIsOnScreen(state.X, state.Y, state.Width, state.Height);
            }

            // Apply current theme and subscribe to changes
            _themeHandler = (s, e) => ApplyTheme(ThemeManager.Instance.CurrentTheme);
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += _themeHandler;

            // Register translation listener
            _langHandler = (s, e) =>
            {
                if (this.Content is FrameworkElement fe)
                {
                    TranslationManager.Instance.Translate(fe);
                }
            };
            TranslationManager.Instance.LanguageChanged += _langHandler;

            // Configure presenter TopMost and Resizable behavior
            ConfigurePresenter();

            // Main telemetry timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Badge reset timer for Boost feedback
            _badgeResetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3.0)
            };
            _badgeResetTimer.Tick += (s, e) =>
            {
                _badgeResetTimer.Stop();
                BoostText.Text = "BOOST";
                BoostIcon.Glyph = "\uE74C";
                BoostBadgeText.Visibility = Visibility.Collapsed;
                if (CompactBoostText != null) CompactBoostText.Text = "BOOST";
                if (CompactBoostIcon != null) CompactBoostIcon.Glyph = "\uE74C";
            };

            // Window closed cleanup & state saving
            this.Closed += (s, e) =>
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _badgeResetTimer.Stop();
                ThemeManager.Instance.ThemeChanged -= _themeHandler;
                TranslationManager.Instance.LanguageChanged -= _langHandler;
                SaveStateConfig();
            };

            UpdateStats();
        }

        private void EnsureWindowIsOnScreen(int x, int y, int width, int height)
        {
            try
            {
                var pt = new Windows.Graphics.PointInt32(x, y);
                var displayArea = DisplayArea.GetFromPoint(pt, DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    int safeX = Math.Clamp(x, displayArea.WorkArea.X, displayArea.WorkArea.X + displayArea.WorkArea.Width - Math.Max(200, width));
                    int safeY = Math.Clamp(y, displayArea.WorkArea.Y, displayArea.WorkArea.Y + displayArea.WorkArea.Height - Math.Max(50, height));
                    this.AppWindow.Move(new Windows.Graphics.PointInt32(safeX, safeY));
                }
                else
                {
                    this.AppWindow.Move(pt);
                }
            }
            catch
            {
                try
                {
                    this.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
                }
                catch { }
            }
        }

        private void UpdateCaptionButtonsMargin()
        {
            try
            {
                int rightInset = AppWindow.TitleBar.RightInset;
                double rightMargin = rightInset > 0 ? Math.Max(50.0, rightInset + 4) : 50.0;

                if (HeaderActionPanel != null)
                {
                    HeaderActionPanel.Margin = new Thickness(0, 0, rightMargin, 0);
                }
            }
            catch { }
        }

        private void ApplyViewModeState(int customWidth = -1, int customHeight = -1)
        {
            if (_isCompact)
            {
                ExpandedViewGrid.Visibility = Visibility.Collapsed;
                CompactViewGrid.Visibility = Visibility.Visible;
                SetTitleBar(CompactDragArea);

                int w = 440; // Consistent 440px width - ample room for all 4 telemetry pills!
                int h = 100;
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }
            else
            {
                ExpandedViewGrid.Visibility = Visibility.Visible;
                CompactViewGrid.Visibility = Visibility.Collapsed;
                SetTitleBar(WidgetDragArea);

                int w = 440; // Full Detailed 440px x 245px HUD Card
                int h = 245;
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }

            UpdateCaptionButtonsMargin();
        }

        private void ApplyTheme(ElementTheme theme)
        {
            try
            {
                if (this.Content is FrameworkElement root)
                {
                    root.RequestedTheme = theme;
                }

                if (AppWindowTitleBar.IsCustomizationSupported())
                {
                    var titleBar = this.AppWindow.TitleBar;
                    titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

                    if (theme == ElementTheme.Light)
                    {
                        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(20, 0, 0, 0);
                        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 0, 0, 0);
                    }
                    else
                    {
                        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
                        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 255, 255, 255);
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 255, 255, 255);
                    }
                }
            }
            catch { }
        }

        private void ConfigurePresenter()
        {
            try
            {
                if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsAlwaysOnTop = _isAlwaysOnTop;
                    presenter.IsResizable = false; // Fixed HUD size
                    presenter.IsMinimizable = false; // Disable redundant minimize button (-)
                    presenter.IsMaximizable = false; // Disable redundant maximize button (☐)
                }

                TopmostDot.Fill = _isAlwaysOnTop ? GreenBrush : AmberBrush;
                TopmostText.Text = _isAlwaysOnTop ? "TOPMOST" : "NORMAL";
                TopmostText.Foreground = _isAlwaysOnTop ? GreenBrush : AmberBrush;

                if (CompactTopmostDot != null) CompactTopmostDot.Fill = _isAlwaysOnTop ? GreenBrush : AmberBrush;
                if (CompactTopmostText != null)
                {
                    CompactTopmostText.Text = _isAlwaysOnTop ? "TOPMOST" : "NORMAL";
                    CompactTopmostText.Foreground = _isAlwaysOnTop ? GreenBrush : AmberBrush;
                }
            }
            catch { }
        }

        private void Timer_Tick(object? sender, object e)
        {
            UpdateStats();
        }

        private double GetCpuUsage()
        {
            try
            {
                if (GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime))
                {
                    if (_hasPrevTimes)
                    {
                        ulong prevIdle = FileTimeToUInt64(_prevIdleTime);
                        ulong prevKernel = FileTimeToUInt64(_prevKernelTime);
                        ulong prevUser = FileTimeToUInt64(_prevUserTime);

                        ulong currIdle = FileTimeToUInt64(idleTime);
                        ulong currKernel = FileTimeToUInt64(kernelTime);
                        ulong currUser = FileTimeToUInt64(userTime);

                        ulong idleDiff = currIdle - prevIdle;
                        ulong kernelDiff = currKernel - prevKernel;
                        ulong userDiff = currUser - prevUser;

                        ulong totalDiff = kernelDiff + userDiff;
                        if (totalDiff > 0)
                        {
                            double cpu = ((double)(totalDiff - idleDiff) / totalDiff) * 100.0;
                            _prevIdleTime = idleTime;
                            _prevKernelTime = kernelTime;
                            _prevUserTime = userTime;
                            return Math.Clamp(cpu, 0.0, 100.0);
                        }
                    }
                    _prevIdleTime = idleTime;
                    _prevKernelTime = kernelTime;
                    _prevUserTime = userTime;
                    _hasPrevTimes = true;
                }
            }
            catch { }
            return 12.0;
        }

        private static SolidColorBrush GetHealthBrush(double percent)
        {
            if (percent >= 85.0) return RedBrush;
            if (percent >= 70.0) return AmberBrush;
            return GreenBrush;
        }

        private void UpdateStats()
        {
            try
            {
                // 1. RAM Usage & Detailed GB
                double ramPercent = 0;
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    ramPercent = memStatus.dwMemoryLoad;
                    double totalGB = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availGB = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    double usedGB = totalGB - availGB;

                    RamText.Text = $"{ramPercent:F0}%";
                    RamProgressBar.Value = ramPercent;
                    RamDetailText.Text = $"{usedGB:F1} / {totalGB:F1} GB";
                }
                else
                {
                    var proc = Process.GetCurrentProcess();
                    long ramMB = proc.WorkingSet64 / (1024 * 1024);
                    ramPercent = Math.Min(100.0, (ramMB / 150.0) * 100.0);
                    RamText.Text = $"{ramMB} MB";
                    RamProgressBar.Value = ramPercent;
                    RamDetailText.Text = $"{ramMB} MB App";
                }

                var ramBrush = GetHealthBrush(ramPercent);
                RamText.Foreground = ramBrush;
                RamProgressBar.Foreground = ramBrush;
                RamIcon.Foreground = ramBrush;
                CompactRamDot.Fill = ramBrush;
                CompactRamText.Text = $"RAM {ramPercent:F0}%";

                // 2. CPU Load Telemetry & Process Count
                double cpuVal = GetCpuUsage();
                CpuText.Text = $"{cpuVal:F0}%";
                CpuProgressBar.Value = cpuVal;
                int procCount = Process.GetProcesses().Length;
                CpuDetailText.Text = $"{procCount} Processes";

                var cpuBrush = GetHealthBrush(cpuVal);
                CpuText.Foreground = cpuBrush;
                CpuProgressBar.Foreground = cpuBrush;
                CpuIcon.Foreground = cpuBrush;
                CompactCpuDot.Fill = cpuBrush;
                CompactCpuText.Text = $"CPU {cpuVal:F0}%";

                // 3. Disk (C:) Telemetry & Free Space
                try
                {
                    var drive = new System.IO.DriveInfo("C");
                    if (drive.IsReady)
                    {
                        double totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        double freeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        double usedGB = totalGB - freeGB;
                        double diskPercent = (usedGB / totalGB) * 100.0;
                        DiskPercentText.Text = $"{diskPercent:F0}%";
                        DiskProgressBar.Value = diskPercent;
                        DiskDetailText.Text = $"{freeGB:F0} GB Free";

                        var diskBrush = GetHealthBrush(diskPercent);
                        DiskPercentText.Foreground = diskBrush;
                        DiskProgressBar.Foreground = diskBrush;
                    }
                }
                catch { }

                // 4. Network Traffic Monitoring
                UpdateNetworkTraffic();
            }
            catch { }
        }

        private void UpdateNetworkTraffic()
        {
            try
            {
                long currentReceived = 0;
                long currentSent = 0;
                string activeAdapter = "Ethernet/WiFi";

                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        var stats = ni.GetIPStatistics();
                        currentReceived += stats.BytesReceived;
                        currentSent += stats.BytesSent;
                        string rawName = ni.Name;
                        if (rawName.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) || rawName.Contains("Wireless", StringComparison.OrdinalIgnoreCase) || rawName.Contains("WLAN", StringComparison.OrdinalIgnoreCase))
                        {
                            activeAdapter = "Wi-Fi";
                        }
                        else if (rawName.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) || rawName.Contains("LAN", StringComparison.OrdinalIgnoreCase))
                        {
                            activeAdapter = "Ethernet";
                        }
                        else
                        {
                            activeAdapter = rawName.Length > 16 ? rawName.Substring(0, 14) + "..." : rawName;
                        }
                    }
                }

                NetInterfaceText.Text = activeAdapter;

                DateTime now = DateTime.Now;
                double seconds = (now - _lastNetworkCheckTime).TotalSeconds;

                if (seconds > 0 && _lastBytesReceived > 0)
                {
                    double rxBytesPerSec = (currentReceived - _lastBytesReceived) / seconds;
                    double txBytesPerSec = (currentSent - _lastBytesSent) / seconds;

                    string downStr = FormatSpeed(rxBytesPerSec);
                    string upStr = FormatSpeed(txBytesPerSec);

                    NetDownText.Text = downStr;
                    NetUpText.Text = upStr;
                    CompactNetText.Text = downStr;
                }

                _lastBytesReceived = currentReceived;
                _lastBytesSent = currentSent;
                _lastNetworkCheckTime = now;
            }
            catch { }
        }

        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024 * 1024):F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024:F0} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        private void OnTogglePinClick(object? sender, object? e)
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            ConfigurePresenter();
            SaveStateConfig();
        }

        private void OnToggleCompactModeClick(object? sender, object? e)
        {
            _isCompact = !_isCompact;
            ApplyViewModeState();
            SaveStateConfig();
        }

        private void OnOpenMainAppClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.MainWindowInstance != null)
                {
                    App.MainWindowInstance.AppWindow.Show();
                    App.MainWindowInstance.BringToForeground();
                }
            }
            catch { }
        }

        private void OnCloseWidgetClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch { }
        }

        private async void OnFastCleanClick(object sender, RoutedEventArgs e)
        {
            if (_isBoosting) return;

            try
            {
                _isBoosting = true;

                // GPU Spark Burst Micro-Interaction
                if (BoostButton != null) FluidAnimationHelper.ApplyGlowSparkBurst(BoostButton);
                if (CompactBoostButton != null) FluidAnimationHelper.ApplyGlowSparkBurst(CompactBoostButton);

                BoostText.Text = "...";
                BoostIcon.Glyph = "\uE895";
                if (CompactBoostText != null) CompactBoostText.Text = "...";
                if (CompactBoostIcon != null) CompactBoostIcon.Glyph = "\uE895";

                // Spin Boost Icon smoothly
                var spinTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                spinTimer.Tick += (s, ev) =>
                {
                    if (BoostIconRotation != null) BoostIconRotation.Angle = (BoostIconRotation.Angle + 20) % 360;
                    if (CompactBoostIconRotation != null) CompactBoostIconRotation.Angle = (CompactBoostIconRotation.Angle + 20) % 360;
                };
                spinTimer.Start();

                int oldRamPercent = 0;
                var memStatusOld = new MEMORYSTATUSEX();
                memStatusOld.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatusOld))
                {
                    oldRamPercent = (int)memStatusOld.dwMemoryLoad;
                }

                var optEngine = new SystemOptimizerEngine();
                var (_, memoryReclaimedBytes) = await optEngine.OptimizeRamAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                spinTimer.Stop();
                if (BoostIconRotation != null) BoostIconRotation.Angle = 0;
                if (CompactBoostIconRotation != null) CompactBoostIconRotation.Angle = 0;

                UpdateStats();

                // Animate RAM % decrease count-down
                var memStatusNew = new MEMORYSTATUSEX();
                memStatusNew.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatusNew))
                {
                    int newRamPercent = (int)memStatusNew.dwMemoryLoad;
                    if (oldRamPercent > 0 && newRamPercent < oldRamPercent)
                    {
                        FluidAnimationHelper.AnimateNumberInt(RamText, oldRamPercent, newRamPercent, "%", 600);
                        FluidAnimationHelper.AnimateNumberInt(CompactRamText, oldRamPercent, newRamPercent, "%", 600);
                    }
                }

                double freedMb = memoryReclaimedBytes / (1024.0 * 1024.0);
                if (freedMb > 0)
                {
                    BoostBadgeText.Text = $"+{freedMb:F0}MB";
                    BoostBadgeText.Visibility = Visibility.Visible;
                    BoostText.Text = "FREED!";
                    BoostIcon.Glyph = "\uE73E"; // Checkmark glyph
                    if (CompactBoostText != null) CompactBoostText.Text = $"+{freedMb:F0}M";
                    if (CompactBoostIcon != null) CompactBoostIcon.Glyph = "\uE73E";
                }
                else
                {
                    BoostText.Text = "OPTIMAL";
                    BoostIcon.Glyph = "\uE73E";
                    if (CompactBoostText != null) CompactBoostText.Text = "OK!";
                    if (CompactBoostIcon != null) CompactBoostIcon.Glyph = "\uE73E";
                }

                _badgeResetTimer.Stop();
                _badgeResetTimer.Start();
            }
            catch
            {
                BoostText.Text = "BOOST";
                BoostIcon.Glyph = "\uE74C";
                if (CompactBoostText != null) CompactBoostText.Text = "BOOST";
                if (CompactBoostIcon != null) CompactBoostIcon.Glyph = "\uE74C";
            }
            finally
            {
                _isBoosting = false;
            }
        }

        private static string GetConfigFilePath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCarePro");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "hud_state.json");
        }

        private static HudStateConfig LoadStateConfig()
        {
            try
            {
                string path = GetConfigFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<HudStateConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch { }
            return new HudStateConfig();
        }

        private void SaveStateConfig()
        {
            try
            {
                string path = GetConfigFilePath();
                var cfg = new HudStateConfig
                {
                    X = this.AppWindow.Position.X,
                    Y = this.AppWindow.Position.Y,
                    Width = _isCompact ? 440 : 440,
                    Height = _isCompact ? 100 : 245,
                    IsAlwaysOnTop = _isAlwaysOnTop,
                    IsCompact = _isCompact
                };
                string json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public static void CloseWindow()
        {
            if (_currentInstance != null)
            {
                try
                {
                    _currentInstance.Close();
                }
                catch { }
                _currentInstance = null;
            }
        }
    }
}


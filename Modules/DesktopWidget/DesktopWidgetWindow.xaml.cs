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

namespace WinCarePro.Modules.DesktopWidget
{
    public class HudStateConfig
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 430;
        public int Height { get; set; } = 210;
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
                _currentInstance.Closed += (s, e) => { _currentInstance = null; };
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

        private void ApplyViewModeState(int customWidth = -1, int customHeight = -1)
        {
            if (_isCompact)
            {
                ExpandedViewGrid.Visibility = Visibility.Collapsed;
                CompactViewGrid.Visibility = Visibility.Visible;
                CompactToggleIcon.Glyph = "\uE73F"; // Expand icon
                ToolTipService.SetToolTip(CompactToggleModeButton, "Mở Rộng HUD (Full Telemetry)");
                SetTitleBar(CompactDragArea);

                int w = 540; // Fixed spacious width for Compact mode to guarantee zero overlap with 138px native caption buttons
                int h = 56;
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }
            else
            {
                ExpandedViewGrid.Visibility = Visibility.Visible;
                CompactViewGrid.Visibility = Visibility.Collapsed;
                CompactToggleIcon.Glyph = "\uE740"; // Collapse icon
                ToolTipService.SetToolTip(CompactToggleModeButton, "Thu Nhỏ HUD (Compact Pill)");
                SetTitleBar(WidgetDragArea);

                int w = 450; // Fixed spacious width for Expanded mode
                int h = 210;
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }
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
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

                    if (theme == ElementTheme.Light)
                    {
                        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 0, 0, 0);
                        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 0, 0, 0);
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(160, 0, 0, 0);
                    }
                    else
                    {
                        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
                        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(80, 255, 255, 255);
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(160, 255, 255, 255);
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
                    presenter.IsResizable = true;
                    presenter.IsMinimizable = true;
                    presenter.IsMaximizable = false;
                }

                string pinGlyph = _isAlwaysOnTop ? "\uE718" : "\uE77A";
                PinIcon.Glyph = pinGlyph;
                if (PinIcon2 != null) PinIcon2.Glyph = pinGlyph;

                TopmostDot.Fill = _isAlwaysOnTop ? GreenBrush : AmberBrush;
                TopmostText.Text = _isAlwaysOnTop ? "TOPMOST" : "NORMAL";
                TopmostText.Foreground = _isAlwaysOnTop ? GreenBrush : AmberBrush;
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
                        activeAdapter = ni.Name;
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

        private void OnTogglePinClick(object sender, RoutedEventArgs e)
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            ConfigurePresenter();
            SaveStateConfig();
        }

        private void OnToggleCompactModeClick(object sender, RoutedEventArgs e)
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
                BoostText.Text = "...";
                BoostIcon.Glyph = "\uE895";

                var optEngine = new SystemOptimizerEngine();
                var (_, memoryReclaimedBytes) = await optEngine.OptimizeRamAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                UpdateStats();

                double freedMb = memoryReclaimedBytes / (1024.0 * 1024.0);
                if (freedMb > 0)
                {
                    BoostBadgeText.Text = $"+{freedMb:F0}MB";
                    BoostBadgeText.Visibility = Visibility.Visible;
                    BoostText.Text = "FREED!";
                }
                else
                {
                    BoostText.Text = "OPTIMAL";
                }

                _badgeResetTimer.Stop();
                _badgeResetTimer.Start();
            }
            catch
            {
                BoostText.Text = "BOOST";
                BoostIcon.Glyph = "\uE74C";
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
                    Width = _isCompact ? 540 : 450,
                    Height = _isCompact ? 56 : 210,
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


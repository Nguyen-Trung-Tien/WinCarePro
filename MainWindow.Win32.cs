using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;
using WinCarePro.Database;
using WinCarePro.Core.Interop;

namespace WinCarePro;

public sealed partial class MainWindow : Window
{
    // WndProc subclassing to enforce minimum window dimensions (1280x800)
    private delegate IntPtr WinProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WinProc? _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static partial IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static partial IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MessageBeep(uint uType);

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        public fixed char szTip[128];
        public int dwState;
        public int dwStateMask;
        public fixed char szInfo[256];
        public int uTimeoutOrVersion;
        public fixed char szInfoTitle[64];
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;

        public void SetTip(string text)
        {
            fixed (char* ptr = szTip)
            {
                CopyString(ptr, text, 128);
            }
        }

        public void SetInfo(string text)
        {
            fixed (char* ptr = szInfo)
            {
                CopyString(ptr, text, 256);
            }
        }

        public void SetInfoTitle(string text)
        {
            fixed (char* ptr = szInfoTitle)
            {
                CopyString(ptr, text, 64);
            }
        }

        private static void CopyString(char* dest, string text, int maxChars)
        {
            int len = Math.Min(text.Length, maxChars - 1);
            for (int i = 0; i < len; i++) dest[i] = text[i];
            dest[len] = '\0';
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [LibraryImport("shell32.dll", EntryPoint = "ExtractIconW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void TrimProcessMemory()
    {
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            NativeApi.EmptyWorkingSet(proc.Handle);
        }
        catch (Exception ex)
        {
            WinCarePro.Infrastructure.Logging.CrashLogger.LogException("TrimProcessMemory", ex);
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyMenu(IntPtr hMenu);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    private static partial uint TrackPopupMenuEx(IntPtr hMenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    private const uint WM_SETICON = 0x0080;
    private const IntPtr ICON_SMALL = (IntPtr)0;
    private const IntPtr ICON_BIG = (IntPtr)1;

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
    private const int WM_CONTEXTMENU = 0x007B;
    private const uint WM_QUERYENDSESSION = 0x0011;
    private const uint WM_ENDSESSION = 0x0016;

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint RegisterWindowMessage(string lpString);

    public static readonly uint WM_ACTIVATE_INSTANCE = RegisterWindowMessage("WinCarePro_Activate_SingleInstance");

    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;

    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;

    private const uint ID_TRAY_OPEN = 1001;
    private const uint ID_TRAY_SETTINGS = 1002;
    private const uint ID_TRAY_EXIT = 1003;
    private const uint ID_TRAY_SCAN = 1004;
    private const uint ID_TRAY_RAM = 1005;
    private const uint ID_TRAY_JUNK = 1006;
    private const uint ID_TRAY_THEME = 1007;
    private const uint ID_TRAY_HUD = 1008;

    private bool _trayIconRegistered = false;
    private IntPtr _hIcon = IntPtr.Zero;

    private void SubclassWindow()
    {
        if (_hwnd == IntPtr.Zero) return;

        _newWndProc = new WinProc(NewWindowProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    public void UnsubclassWindow()
    {
        if (_hwnd != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_hwnd, GWL_WNDPROC, _oldWndProc);
            _oldWndProc = IntPtr.Zero;
            _newWndProc = null;
        }
    }

    private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_ACTIVATE_INSTANCE && WM_ACTIVATE_INSTANCE != 0)
        {
            this.DispatcherQueue?.TryEnqueue(() =>
            {
                this.AppWindow.Show();
                BringToForeground();
            });
            return IntPtr.Zero;
        }

        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            
            // Set minimum track sizing constraints (800 x 600 px)
            mmi.ptMinTrackSize.x = 800;
            mmi.ptMinTrackSize.y = 600;

            Marshal.StructureToPtr(mmi, lParam, false);
            return IntPtr.Zero;
        }
        else if (msg == WM_QUERYENDSESSION)
        {
            // Windows is querying whether application is ready to shutdown/restart.
            // Flush database write-ahead logs and return 1 (TRUE) to allow clean shutdown.
            try
            {
                DbManager.ShutdownDatabase();
            }
            catch { }
            return (IntPtr)1;
        }
        else if (msg == WM_ENDSESSION)
        {
            if (wParam != IntPtr.Zero) // Session is actually ending
            {
                try
                {
                    _clockTimer?.Stop();
                    _clockTimer = null;
                    CleanupTrayIcon();
                    DbManager.ShutdownDatabase();
                }
                catch { }
            }
            return IntPtr.Zero;
        }
        else if (msg == WM_TRAYICON)
        {
            int eventId = (int)lParam;
            if (eventId == WM_LBUTTONDBLCLK)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.AppWindow.Show();
                    BringToForeground();
                });
            }
            else if (eventId == WM_RBUTTONUP || eventId == WM_CONTEXTMENU)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ShowTrayContextMenu();
                });
            }
            return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void ShowTrayContextMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        uint cmd = 0;
        try
        {
            string openText = "🚀 Open WinCare Pro".T();
            string scanText = "🔍 Scan System Diagnostics".T();
            string ramText = "⚡ Optimize Memory (RAM)".T();
            string junkText = "🧹 Clean Junk Files".T();

            bool isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;
            string hudText = "🪟 Desktop HUD Widget".T();
            string themeText = isDark ? "🌙 Theme: Dark (Switch to Light)".T() : "☀️ Theme: Light (Switch to Dark)".T();
            string settingsText = "⚙️ Settings".T();
            string exitText = "🚪 Exit".T();

            AppendMenu(hMenu, MF_STRING, ID_TRAY_OPEN, openText);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_SCAN, scanText);
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_HUD, hudText);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_RAM, ramText);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_JUNK, junkText);
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_THEME, themeText);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_SETTINGS, settingsText);
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, ID_TRAY_EXIT, exitText);

            GetCursorPos(out POINT pt);

            // Set foreground window to ensure popup menu closes when clicking outside
            SetForegroundWindow(_hwnd);

            cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.x, pt.y, _hwnd, IntPtr.Zero);
        }
        finally
        {
            // Always destroy the menu handle to prevent GDI resource leak
            DestroyMenu(hMenu);
        }

        // Process selected command after menu is destroyed
        try
        {
            if (cmd == ID_TRAY_OPEN)
            {
                this.AppWindow.Show();
                BringToForeground();
            }
            else if (cmd == ID_TRAY_HUD)
            {
                WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.ShowWindow();
            }
            else if (cmd == ID_TRAY_SCAN)
            {
                this.AppWindow.Show();
                BringToForeground();
                if (RootFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal("Dashboard");
                    if (mp.NavigationFrame.Content is WinCarePro.Views.DashboardPage dbPage)
                    {
                        _ = dbPage.ViewModel?.RunFullDiagnosticsAsync();
                    }
                }
            }
            else if (cmd == ID_TRAY_RAM)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var optEngine = App.Services.GetRequiredService<Engines.SystemOptimizerEngine>();
                        await optEngine.OptimizeRamAsync();
                        
                        App.MainDispatcherQueue?.TryEnqueue(() =>
                        {
                            Database.DbManager.LogAction("RAM optimization triggered from System Tray", "Smart Boost", "Success");
                            ShowTrayNotification("RAM Cleaned".T(), "Memory has been successfully optimized.".T());
                        });
                    }
                    catch { }
                });
            }
            else if (cmd == ID_TRAY_JUNK)
            {
                this.AppWindow.Show();
                BringToForeground();
                if (RootFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal("Junk");
                }
            }
            else if (cmd == ID_TRAY_THEME)
            {
                var newTheme = (ThemeManager.Instance.CurrentTheme == ElementTheme.Dark) ? ElementTheme.Light : ElementTheme.Dark;
                ThemeManager.Instance.ApplyTheme(newTheme);
                WinCarePro.Services.Implementations.SettingsService.Instance.UpdateSettings(s =>
                {
                    s.Theme = (newTheme == ElementTheme.Dark) ? "Dark" : "Light";
                }, "Theme");

                string title = "Theme Updated".T();
                string msg = (newTheme == ElementTheme.Dark) ? "Switched to Dark Mode.".T() : "Switched to Light Mode.".T();
                ShowTrayNotification(title, msg);
            }
            else if (cmd == ID_TRAY_SETTINGS)
            {
                this.AppWindow.Show();
                BringToForeground();
                if (RootFrame.Content is MainPage mp)
                {
                    mp.NavigateToPageExternal("Settings");
                }
            }
            else if (cmd == ID_TRAY_EXIT)
            {
                PerformAppExit();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] ShowTrayContextMenu error: {ex.Message}");
        }
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
            
            if (File.Exists(iconPath))
            {
                // Load 32x32 crisp icon frame for system tray notification area
                _hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 32, 32, 0x00000010); // IMAGE_ICON | LR_LOADFROMFILE
            }
            else
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    _hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
                }
            }

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                dwInfoFlags = 1 // NIIF_INFO
            };
            nid.SetTip("WinCare Pro Suite");
            nid.SetInfo("WinCare Pro is running in the background. Double-click the tray icon to open.");
            nid.SetInfoTitle("Minimized to System Tray");

            _trayIconRegistered = Shell_NotifyIcon(NIM_ADD, ref nid);
        }
        catch { }
    }

    public void CleanupTrayIcon()
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

    public void BringToForeground()
    {
        if (_hwnd != IntPtr.Zero)
        {
            ShowWindow(_hwnd, 9); // SW_RESTORE
            SetForegroundWindow(_hwnd);
        }
    }

    /// <summary>
    /// Forces the taskbar and Alt+Tab icons to update via WM_SETICON with high-res icon frames.
    /// </summary>
    private void SetTaskbarIcon(IntPtr hIconBig, IntPtr hIconSmall)
    {
        if (_hwnd == IntPtr.Zero) return;
        if (hIconBig != IntPtr.Zero) SendMessage(_hwnd, WM_SETICON, ICON_BIG, hIconBig);
        if (hIconSmall != IntPtr.Zero) SendMessage(_hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
    }

    public void ShowTrayNotification(string title, string message, int dwInfoFlags = 1)
    {
        if (!_trayIconRegistered) return;
        try
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_INFO,
                dwInfoFlags = dwInfoFlags
            };
            nid.SetInfo(message.Length >= 256 ? message.Substring(0, 252) + "..." : message);
            nid.SetInfoTitle(title.Length >= 64 ? title.Substring(0, 60) + "..." : title);
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
        catch { }
    }

}

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using WinCarePro.Database;

namespace WinCarePro;

public sealed partial class MainWindow : Window
{

    private IntPtr _hwnd = IntPtr.Zero;

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

        // Navigate page frame
        RootFrame.Navigate(typeof(MainPage));
    }

    private void SubclassWindow()
    {
        if (_hwnd == IntPtr.Zero) return;

        _newWndProc = new WinProc(NewWindowProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            
            // Set minimum track sizing constraints (1280 x 800 px)
            mmi.ptMinTrackSize.x = 1280;
            mmi.ptMinTrackSize.y = 800;

            Marshal.StructureToPtr(mmi, lParam, false);
            return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
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

    public void ApplyAppTheme(bool dark)
    {
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
}

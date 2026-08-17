using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinCarePro.Services;

public class ThemeManager
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    private readonly List<WeakReference<Window>> _registeredWindows = new();
    private readonly List<WeakReference<Page>> _registeredPages = new();

    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Dark;
    public string CurrentAccent { get; private set; } = "Default";

    public event EventHandler? ThemeChanged;
    public event EventHandler? AccentChanged;

    private ThemeManager() { }

    public void RegisterWindow(Window window)
    {
        lock (_registeredWindows)
        {
            _registeredWindows.RemoveAll(wr => !wr.TryGetTarget(out _));
            if (!_registeredWindows.Exists(wr => wr.TryGetTarget(out var w) && ReferenceEquals(w, window)))
            {
                _registeredWindows.Add(new WeakReference<Window>(window));
            }
        }

        // Apply current theme and accent immediately to newly registered window
        ApplyThemeToWindow(window, CurrentTheme);
    }

    public void UnregisterWindow(Window window)
    {
        lock (_registeredWindows)
        {
            _registeredWindows.RemoveAll(wr => !wr.TryGetTarget(out var w) || ReferenceEquals(w, window));
        }
    }

    public void RegisterPage(Page page)
    {
        lock (_registeredPages)
        {
            _registeredPages.RemoveAll(wr => !wr.TryGetTarget(out _));
            if (!_registeredPages.Exists(wr => wr.TryGetTarget(out var p) && ReferenceEquals(p, page)))
            {
                _registeredPages.Add(new WeakReference<Page>(page));
            }
        }
        page.RequestedTheme = CurrentTheme;
    }

    public void UnregisterPage(Page page)
    {
        lock (_registeredPages)
        {
            _registeredPages.RemoveAll(wr => !wr.TryGetTarget(out var p) || ReferenceEquals(p, page));
        }
    }

    public void ApplyTheme(ElementTheme theme)
    {
        CurrentTheme = theme;

        if (App.MainWindowInstance is MainWindow win)
        {
            win.DispatcherQueue?.TryEnqueue(() =>
            {
                win.MainRootGrid.RequestedTheme = theme;
                win.MainThemeIcon.Glyph = (theme == ElementTheme.Dark) ? "\uE708" : "\uE706";
                win.SetBackdropType((theme == ElementTheme.Dark) ? "micaalt" : "mica");
                win.ApplyTransparency(win.CurrentTransparencyLevel);
            });
        }

        // Synchronize all registered windows
        lock (_registeredWindows)
        {
            _registeredWindows.RemoveAll(wr => !wr.TryGetTarget(out _));
            foreach (var wr in _registeredWindows)
            {
                if (wr.TryGetTarget(out var targetWindow))
                {
                    ApplyThemeToWindow(targetWindow, theme);
                }
            }
        }

        // Synchronize all registered pages
        lock (_registeredPages)
        {
            _registeredPages.RemoveAll(wr => !wr.TryGetTarget(out _));
            foreach (var wr in _registeredPages)
            {
                if (wr.TryGetTarget(out var targetPage))
                {
                    targetPage.DispatcherQueue?.TryEnqueue(() =>
                    {
                        targetPage.RequestedTheme = theme;
                    });
                }
            }
        }

        // Re-apply Accent to get theme-aware colors (High contrast vs Glowing)
        ApplyAccent(CurrentAccent);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyThemeToWindow(Window targetWindow, ElementTheme theme)
    {
        try
        {
            targetWindow.DispatcherQueue?.TryEnqueue(() =>
            {
                if (targetWindow.Content is FrameworkElement root)
                {
                    root.RequestedTheme = theme;
                }

                // Apply Immersive Dark Mode attribute
                try
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(targetWindow);
                    if (hwnd != IntPtr.Zero)
                    {
                        int isDark = (theme == ElementTheme.Dark) ? 1 : 0;
                        DwmSetWindowAttribute(hwnd, 20, ref isDark, sizeof(int));
                    }
                }
                catch { }

                // Title Bar customization
                try
                {
                    if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
                    {
                        var titleBar = targetWindow.AppWindow.TitleBar;
                        titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
                        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

                        if (theme == ElementTheme.Dark)
                        {
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 45, 45, 45);
                            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 80, 80, 80);
                            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        }
                        else
                        {
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 230, 230, 230);
                            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 200, 200, 200);
                            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        }
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    public void ApplyAccent(string tag)
    {
        CurrentAccent = string.IsNullOrWhiteSpace(tag) ? "Default" : tag;
        bool isDark = (CurrentTheme == ElementTheme.Dark);

        try
        {
            Color c0, c1, c2, cyber0, cyber1, cyber2;
            switch (CurrentAccent.ToLower())
            {
                case "green":
                    c0 = isDark ? Color.FromArgb(255, 16, 185, 129) : Color.FromArgb(255, 4, 120, 87);
                    c1 = isDark ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 6, 95, 70);
                    c2 = isDark ? Color.FromArgb(255, 4, 120, 87) : Color.FromArgb(255, 6, 78, 59);

                    cyber0 = isDark ? Color.FromArgb(255, 52, 211, 153) : Color.FromArgb(255, 5, 150, 105);
                    cyber1 = isDark ? Color.FromArgb(255, 16, 185, 129) : Color.FromArgb(255, 4, 120, 87);
                    cyber2 = isDark ? Color.FromArgb(255, 6, 182, 212) : Color.FromArgb(255, 8, 145, 178);
                    break;
                case "purple":
                    c0 = isDark ? Color.FromArgb(255, 139, 92, 246) : Color.FromArgb(255, 109, 40, 217);
                    c1 = isDark ? Color.FromArgb(255, 124, 58, 237) : Color.FromArgb(255, 91, 33, 182);
                    c2 = isDark ? Color.FromArgb(255, 109, 40, 217) : Color.FromArgb(255, 76, 29, 149);

                    cyber0 = isDark ? Color.FromArgb(255, 192, 132, 252) : Color.FromArgb(255, 147, 51, 234);
                    cyber1 = isDark ? Color.FromArgb(255, 139, 92, 246) : Color.FromArgb(255, 109, 40, 217);
                    cyber2 = isDark ? Color.FromArgb(255, 99, 102, 241) : Color.FromArgb(255, 67, 56, 202);
                    break;
                case "pink":
                    c0 = isDark ? Color.FromArgb(255, 236, 72, 153) : Color.FromArgb(255, 190, 24, 93);
                    c1 = isDark ? Color.FromArgb(255, 217, 70, 239) : Color.FromArgb(255, 157, 23, 77);
                    c2 = isDark ? Color.FromArgb(255, 192, 132, 252) : Color.FromArgb(255, 131, 24, 67);

                    cyber0 = isDark ? Color.FromArgb(255, 244, 114, 182) : Color.FromArgb(255, 219, 39, 119);
                    cyber1 = isDark ? Color.FromArgb(255, 236, 72, 153) : Color.FromArgb(255, 190, 24, 93);
                    cyber2 = isDark ? Color.FromArgb(255, 168, 85, 247) : Color.FromArgb(255, 126, 34, 206);
                    break;
                case "amber":
                    c0 = isDark ? Color.FromArgb(255, 245, 158, 11) : Color.FromArgb(255, 194, 65, 12);
                    c1 = isDark ? Color.FromArgb(255, 217, 119, 6) : Color.FromArgb(255, 154, 52, 18);
                    c2 = isDark ? Color.FromArgb(255, 180, 83, 9) : Color.FromArgb(255, 124, 45, 18);

                    cyber0 = isDark ? Color.FromArgb(255, 251, 191, 36) : Color.FromArgb(255, 217, 119, 6);
                    cyber1 = isDark ? Color.FromArgb(255, 245, 158, 11) : Color.FromArgb(255, 194, 65, 12);
                    cyber2 = isDark ? Color.FromArgb(255, 239, 68, 68) : Color.FromArgb(255, 185, 28, 28);
                    break;
                default: // Default (Purple-Indigo-Blue)
                    c0 = isDark ? Color.FromArgb(255, 127, 86, 217) : Color.FromArgb(255, 91, 50, 168);
                    c1 = isDark ? Color.FromArgb(255, 99, 102, 241) : Color.FromArgb(255, 67, 56, 202);
                    c2 = isDark ? Color.FromArgb(255, 59, 130, 246) : Color.FromArgb(255, 29, 78, 216);

                    cyber0 = isDark ? Color.FromArgb(255, 192, 132, 252) : Color.FromArgb(255, 126, 34, 206);
                    cyber1 = isDark ? Color.FromArgb(255, 99, 102, 241) : Color.FromArgb(255, 67, 56, 202);
                    cyber2 = isDark ? Color.FromArgb(255, 6, 182, 212) : Color.FromArgb(255, 8, 145, 178);
                    break;
            }

            if (Application.Current.Resources.TryGetValue("PrimaryAccentGradient", out var brushObj) && 
                brushObj is LinearGradientBrush brush && brush.GradientStops.Count >= 3)
            {
                brush.GradientStops[0].Color = c0;
                brush.GradientStops[1].Color = c1;
                brush.GradientStops[2].Color = c2;
            }

            if (Application.Current.Resources.TryGetValue("CyberAccentGradient", out var cyberBrushObj) &&
                cyberBrushObj is LinearGradientBrush cyberBrush && cyberBrush.GradientStops.Count >= 3)
            {
                cyberBrush.GradientStops[0].Color = cyber0;
                cyberBrush.GradientStops[1].Color = cyber1;
                cyberBrush.GradientStops[2].Color = cyber2;
            }

            if (Application.Current.Resources.TryGetValue("PrimaryAccentBrush", out var solidBrushObj) &&
                solidBrushObj is SolidColorBrush solidBrush)
            {
                solidBrush.Color = c0;
            }

            if (Application.Current.Resources.TryGetValue("PrimaryAccentLightBrush", out var lightBrushObj) &&
                lightBrushObj is SolidColorBrush lightBrush)
            {
                lightBrush.Color = Color.FromArgb(isDark ? (byte)28 : (byte)20, c0.R, c0.G, c0.B);
            }

            if (Application.Current.Resources.TryGetValue("PrimaryAccentBorderBrush", out var borderBrushObj) &&
                borderBrushObj is SolidColorBrush borderBrush)
            {
                borderBrush.Color = Color.FromArgb(isDark ? (byte)80 : (byte)90, c0.R, c0.G, c0.B);
            }

            if (Application.Current.Resources.TryGetValue("NavActiveIndicatorBrush", out var navBrushObj) &&
                navBrushObj is LinearGradientBrush navBrush && navBrush.GradientStops.Count >= 2)
            {
                navBrush.GradientStops[0].Color = c0;
                navBrush.GradientStops[1].Color = c1;
            }
        }
        catch { }

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}

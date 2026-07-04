using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinCarePro.Services;

public class ThemeManager
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Dark;
    public string CurrentAccent { get; private set; } = "Default";

    public event EventHandler? ThemeChanged;
    public event EventHandler? AccentChanged;

    private ThemeManager() { }

    public void ApplyTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
        
        if (App.MainWindowInstance is MainWindow win)
        {
            win.MainRootGrid.RequestedTheme = theme;
            win.MainThemeIcon.Glyph = (theme == ElementTheme.Dark) ? "\uE708" : "\uE706";
            win.SetBackdropType((theme == ElementTheme.Dark) ? "micaalt" : "mica");

            // Apply Immersive Dark Mode attribute
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
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
                    var titleBar = win.AppWindow.TitleBar;
                    if (theme == ElementTheme.Dark)
                    {
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 45, 45, 45);
                        titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 80, 80, 80);
                        titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                    }
                    else
                    {
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 230, 230, 230);
                        titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 200, 200, 200);
                        titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                    }
                }
            }
            catch { }
        }

        // Re-apply Accent to get theme-aware colors (High contrast vs Glowing)
        ApplyAccent(CurrentAccent);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyAccent(string tag)
    {
        CurrentAccent = tag;
        bool isDark = (CurrentTheme == ElementTheme.Dark);

        try
        {
            if (Application.Current.Resources.TryGetValue("PrimaryAccentGradient", out var brushObj) && 
                brushObj is LinearGradientBrush brush)
            {
                Color c0, c1, c2;
                switch (tag.ToLower())
                {
                    case "green":
                        c0 = isDark ? Color.FromArgb(255, 16, 185, 129) : Color.FromArgb(255, 4, 120, 87);
                        c1 = isDark ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 6, 95, 70);
                        c2 = isDark ? Color.FromArgb(255, 4, 120, 87) : Color.FromArgb(255, 6, 78, 59);
                        break;
                    case "purple":
                        c0 = isDark ? Color.FromArgb(255, 139, 92, 246) : Color.FromArgb(255, 109, 40, 217);
                        c1 = isDark ? Color.FromArgb(255, 124, 58, 237) : Color.FromArgb(255, 91, 33, 182);
                        c2 = isDark ? Color.FromArgb(255, 109, 40, 217) : Color.FromArgb(255, 76, 29, 149);
                        break;
                    case "pink":
                        c0 = isDark ? Color.FromArgb(255, 236, 72, 153) : Color.FromArgb(255, 190, 24, 93);
                        c1 = isDark ? Color.FromArgb(255, 217, 70, 239) : Color.FromArgb(255, 157, 23, 77);
                        c2 = isDark ? Color.FromArgb(255, 192, 132, 252) : Color.FromArgb(255, 131, 24, 67);
                        break;
                    case "amber":
                        c0 = isDark ? Color.FromArgb(255, 245, 158, 11) : Color.FromArgb(255, 194, 65, 12);
                        c1 = isDark ? Color.FromArgb(255, 217, 119, 6) : Color.FromArgb(255, 154, 52, 18);
                        c2 = isDark ? Color.FromArgb(255, 180, 83, 9) : Color.FromArgb(255, 124, 45, 18);
                        break;
                    default: // Default (Purple-Indigo-Blue)
                        c0 = isDark ? Color.FromArgb(255, 127, 86, 217) : Color.FromArgb(255, 91, 50, 168);
                        c1 = isDark ? Color.FromArgb(255, 99, 102, 241) : Color.FromArgb(255, 67, 56, 202);
                        c2 = isDark ? Color.FromArgb(255, 59, 130, 246) : Color.FromArgb(255, 29, 78, 216);
                        break;
                }

                brush.GradientStops[0].Color = c0;
                brush.GradientStops[1].Color = c1;
                brush.GradientStops[2].Color = c2;
            }
        }
        catch { }

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}

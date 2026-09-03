using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Shared.Components;

public enum ResultDialogType
{
    Success,
    Warning,
    Error,
    Info,
    Confirmation,
    Destructive
}

/// <summary>
/// Cung cấp hệ thống Popup Dialog trả kết quả chuẩn Aura Glassmorphic Fluent 2.0,
/// tối ưu hóa chiều sâu và độ tương phản cho cả Dark Mode và Light Mode.
/// </summary>
public static class ResultDialogHelper
{
    private static readonly System.Threading.SemaphoreSlim _dialogSemaphore = new(1, 1);

    #region Color & Brush Resolvers

    private static bool ResolveIsDark(ElementTheme theme)
    {
        if (theme == ElementTheme.Dark) return true;
        if (theme == ElementTheme.Light) return false;
        return ThemeManager.Instance.CurrentTheme == ElementTheme.Dark || Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    private static Brush GetDialogBackground(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(248, 18, 20, 29))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(252, 255, 255, 255));
    }

    private static Brush GetCardBackground(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(220, 24, 27, 38))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(245, 248, 250, 252));
    }

    private static Brush GetCardBorder(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
    }

    private static Brush GetDetailBoxBackground(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(240, 13, 15, 22))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(240, 241, 245, 249));
    }

    private static Brush GetTextPrimary(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42));
    }

    private static Brush GetTextSecondary(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Microsoft.UI.Colors.White;
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }
        else if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex.Substring(0, 2), 16);
            byte r = Convert.ToByte(hex.Substring(2, 2), 16);
            byte g = Convert.ToByte(hex.Substring(4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
        return Microsoft.UI.Colors.White;
    }

    private static (string Glyph, Brush IconBg, Brush IconBorder, Brush IconFg, Windows.UI.Color BaseColor) GetTypeVisuals(ResultDialogType type, bool isDark)
    {
        return type switch
        {
            ResultDialogType.Success => (
                "\uE73E",
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 16, 185, 129)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 236, 253, 245)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 16, 185, 129)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 167, 243, 208)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 5, 150, 105)),
                Windows.UI.Color.FromArgb(255, 16, 185, 129)
            ),
            ResultDialogType.Warning => (
                "\uE7BA",
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 245, 158, 11)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 251, 235)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 245, 158, 11)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 253, 230, 138)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 119, 6)),
                Windows.UI.Color.FromArgb(255, 245, 158, 11)
            ),
            ResultDialogType.Error or ResultDialogType.Destructive => (
                "\uE711",
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 239, 68, 68)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 242, 242)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 239, 68, 68)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 202, 202)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38)),
                Windows.UI.Color.FromArgb(255, 239, 68, 68)
            ),
            ResultDialogType.Confirmation => (
                "\uE774",
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 139, 92, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 243, 255)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 139, 92, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 221, 214, 254)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 58, 237)),
                Windows.UI.Color.FromArgb(255, 139, 92, 246)
            ),
            _ => (
                "\uE946",
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(36, 59, 130, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 246, 255)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(80, 59, 130, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 191, 219, 254)),
                isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)),
                Windows.UI.Color.FromArgb(255, 59, 130, 246)
            )
        };
    }

    #endregion

    #region Dialog Builder Engine

    public static async Task<ContentDialogResult> ShowCustomResultDialogAsync(
        XamlRoot xamlRoot,
        ResultDialogType type,
        string title,
        string message,
        string? detailLog = null,
        IEnumerable<(string Label, string Value, string? StatusColor)>? metrics = null,
        string primaryButtonText = "OK",
        string? secondaryButtonText = null,
        string? closeButtonText = null,
        bool isDestructive = false)
    {
        if (xamlRoot == null) return ContentDialogResult.None;

        await _dialogSemaphore.WaitAsync();
        try
        {
            await Task.Delay(100);

            var currentTheme = ThemeManager.Instance.CurrentTheme;
            bool isDark = ResolveIsDark(currentTheme);
            var (iconGlyph, iconBg, iconBorder, iconFg, baseColor) = GetTypeVisuals(type, isDark);

            var rootPanel = new StackPanel
            {
                Spacing = 16,
                Width = 460
            };

            // 1. Header Banner with Ambient Glowing Aura & Typography
            var headerGrid = new Grid
            {
                ColumnSpacing = 14
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconContainer = new Grid
            {
                Width = 58,
                Height = 58,
                VerticalAlignment = VerticalAlignment.Center
            };

            var auraHalo = new Ellipse
            {
                Width = 58,
                Height = 58,
                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop { Color = Windows.UI.Color.FromArgb(40, baseColor.R, baseColor.G, baseColor.B), Offset = 0 },
                        new GradientStop { Color = Windows.UI.Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B), Offset = 1 }
                    }
                }
            };
            iconContainer.Children.Add(auraHalo);

            var iconBorderBox = new Border
            {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(14),
                Background = iconBg,
                BorderBrush = iconBorder,
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var fontIcon = new FontIcon
            {
                Glyph = iconGlyph,
                FontSize = 22,
                Foreground = iconFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorderBox.Child = fontIcon;
            iconContainer.Children.Add(iconBorderBox);
            Grid.SetColumn(iconContainer, 0);
            headerGrid.Children.Add(iconContainer);

            var titleStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            };
            var titleBlock = new TextBlock
            {
                Text = title.T(),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI Variable Display"),
                Foreground = GetTextPrimary(isDark),
                TextWrapping = TextWrapping.Wrap
            };
            titleStack.Children.Add(titleBlock);

            var statusBadge = new Border
            {
                Background = iconBg,
                BorderBrush = iconBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 3, 0, 0)
            };
            statusBadge.Child = new TextBlock
            {
                Text = type.ToString().ToUpperInvariant(),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = iconFg
            };
            titleStack.Children.Add(statusBadge);

            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);

            rootPanel.Children.Add(headerGrid);

            // 2. Message Body Card
            var bodyCard = new Border
            {
                Background = GetCardBackground(isDark),
                BorderBrush = GetCardBorder(isDark),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 14, 16, 14)
            };
            var messageBlock = new TextBlock
            {
                Text = message.T(),
                FontSize = 13,
                Foreground = GetTextPrimary(isDark),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            bodyCard.Child = messageBlock;
            rootPanel.Children.Add(bodyCard);

            // 3. Optional Metrics Summary Grid with Styled Pill Badges
            if (metrics != null)
            {
                var metricsCard = new Border
                {
                    Background = GetDetailBoxBackground(isDark),
                    BorderBrush = GetCardBorder(isDark),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 10, 12, 10)
                };
                var metricsStack = new StackPanel { Spacing = 6 };
                foreach (var (label, val, statColor) in metrics)
                {
                    var mGrid = new Grid();
                    mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var mLabel = new TextBlock
                    {
                        Text = label.T(),
                        FontSize = 12,
                        Foreground = GetTextSecondary(isDark),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(mLabel, 0);
                    mGrid.Children.Add(mLabel);

                    var mValueBorder = new Border
                    {
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 2, 8, 2),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var mValue = new TextBlock
                    {
                        Text = val,
                        FontSize = 11.5,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Segoe UI Variable Display"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Microsoft.UI.Xaml.Documents.Typography.SetNumeralAlignment(mValue, FontNumeralAlignment.Tabular);

                    if (!string.IsNullOrEmpty(statColor))
                    {
                        var c = ParseColor(statColor);
                        mValueBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(28, c.R, c.G, c.B));
                        mValueBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, c.R, c.G, c.B));
                        mValueBorder.BorderThickness = new Thickness(1);
                        mValue.Foreground = new SolidColorBrush(c);
                    }
                    else
                    {
                        mValueBorder.Background = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(16, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 241, 245, 249));
                        mValueBorder.BorderBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(25, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 226, 232, 240));
                        mValueBorder.BorderThickness = new Thickness(1);
                        mValue.Foreground = isDark ? GetTextPrimary(isDark) : GetTextSecondary(isDark);
                    }
                    mValueBorder.Child = mValue;
                    Grid.SetColumn(mValueBorder, 1);
                    mGrid.Children.Add(mValueBorder);

                    metricsStack.Children.Add(mGrid);
                }
                metricsCard.Child = metricsStack;
                rootPanel.Children.Add(metricsCard);
            }

            // 4. Optional Scrollable Detail / Log Box
            if (!string.IsNullOrWhiteSpace(detailLog))
            {
                var detailExpander = new Expander
                {
                    Header = "Technical Details & Output Log".T(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Background = GetCardBackground(isDark),
                    BorderBrush = GetCardBorder(isDark),
                    CornerRadius = new CornerRadius(10)
                };

                var logScroll = new ScrollViewer
                {
                    MaxHeight = 140,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Background = GetDetailBoxBackground(isDark),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(6)
                };

                var logText = new TextBlock
                {
                    Text = detailLog,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = isDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 2, 132, 199)),
                    TextWrapping = TextWrapping.Wrap
                };
                logScroll.Content = logText;
                detailExpander.Content = logScroll;

                rootPanel.Children.Add(detailExpander);
            }

            var dialog = new ContentDialog
            {
                Content = rootPanel,
                PrimaryButtonText = primaryButtonText.T(),
                SecondaryButtonText = secondaryButtonText != null ? secondaryButtonText.T() : string.Empty,
                CloseButtonText = closeButtonText != null ? closeButtonText.T() : string.Empty,
                DefaultButton = isDestructive ? ContentDialogButton.Close : ContentDialogButton.Primary,
                XamlRoot = xamlRoot,
                RequestedTheme = currentTheme,
                Background = GetDialogBackground(isDark),
                BorderBrush = GetCardBorder(isDark),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16)
            };

            if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj) && styleObj is Style accentStyle)
            {
                if (!isDestructive)
                {
                    dialog.PrimaryButtonStyle = accentStyle;
                }
            }

            return await dialog.ShowAsync();
        }
        finally
        {
            _dialogSemaphore.Release();
        }
    }

    #endregion

    #region Convenient Shortcut Methods

    public static Task<ContentDialogResult> ShowSuccessAsync(
        XamlRoot xamlRoot, 
        string title, 
        string message, 
        string? detailLog = null,
        IEnumerable<(string Label, string Value, string? StatusColor)>? metrics = null)
    {
        return ShowCustomResultDialogAsync(
            xamlRoot,
            ResultDialogType.Success,
            title,
            message,
            detailLog: detailLog,
            metrics: metrics,
            primaryButtonText: "OK");
    }

    public static Task<ContentDialogResult> ShowWarningAsync(
        XamlRoot xamlRoot, 
        string title, 
        string message, 
        string? detailLog = null,
        string primaryBtnText = "Proceed", 
        string closeBtnText = "Cancel")
    {
        return ShowCustomResultDialogAsync(
            xamlRoot,
            ResultDialogType.Warning,
            title,
            message,
            detailLog: detailLog,
            primaryButtonText: primaryBtnText,
            closeButtonText: closeBtnText);
    }

    public static Task<ContentDialogResult> ShowErrorAsync(
        XamlRoot xamlRoot, 
        string title, 
        string message, 
        string? detailLog = null)
    {
        return ShowCustomResultDialogAsync(
            xamlRoot,
            ResultDialogType.Error,
            title,
            message,
            detailLog: detailLog,
            primaryButtonText: "Close");
    }

    public static async Task<bool> ShowConfirmAsync(
        XamlRoot xamlRoot, 
        string title, 
        string message, 
        string confirmText = "Confirm", 
        string cancelText = "Cancel", 
        bool isDestructive = false)
    {
        var res = await ShowCustomResultDialogAsync(
            xamlRoot,
            isDestructive ? ResultDialogType.Destructive : ResultDialogType.Confirmation,
            title,
            message,
            primaryButtonText: confirmText,
            closeButtonText: cancelText,
            isDestructive: isDestructive);

        return res == ContentDialogResult.Primary;
    }

    #endregion
}

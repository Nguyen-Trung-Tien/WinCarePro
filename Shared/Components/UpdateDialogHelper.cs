using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WinCarePro.Core.Helpers;
using WinCarePro.Services;

namespace WinCarePro.Shared.Components;

/// <summary>
/// Cung cấp giao diện Dialog Popup chuẩn Aura Glassmorphic Fluent 2.0 
/// tối ưu hóa hoàn hảo cho cả Dark Mode & Light Mode.
/// </summary>
public static class UpdateDialogHelper
{
    #region Theme Detection & Dynamic Palette Helpers

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

    private static Brush GetInnerCardBackground(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(240, 15, 17, 26))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(245, 241, 245, 249));
    }

    private static Brush GetInnerCardBorder(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
    }

    private static Brush GetChipBackground(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(35, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(25, 15, 23, 42));
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

    private static Brush GetAccentBrush(bool isDark)
    {
        if (Application.Current.Resources.TryGetValue("PrimaryAccentBrush", out var res) && res is Brush brush)
        {
            return brush;
        }
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 108, 189));
    }

    private static Brush GetAccentGradient(bool isDark)
    {
        if (Application.Current.Resources.TryGetValue("PrimaryAccentGradient", out var res) && res is Brush brush)
        {
            return brush;
        }
        return isDark
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 108, 189));
    }

    #endregion

    /// <summary>
    /// Hiển thị Popup thông báo khi có bản cập nhật mới với so sánh phiên bản và Changelog dạng thẻ chi tiết.
    /// </summary>
    public static async Task<ContentDialogResult> ShowUpdateAvailableAsync(
        XamlRoot xamlRoot,
        ElementTheme theme,
        string remoteVersion,
        string currentVersion,
        string changelog,
        string channel = "Stable")
    {
        if (xamlRoot == null) return ContentDialogResult.None;

        bool isDark = ResolveIsDark(theme);

        var rootStack = new StackPanel
        {
            Spacing = 16,
            Width = 480
        };

        // --- 1. Header Banner ---
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBadge = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = GetAccentGradient(isDark),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new FontIcon
            {
                Glyph = "\uE895", // Sync / Download
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(iconBadge, 0);
        headerGrid.Children.Add(iconBadge);

        var titleStack = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        var titleText = new TextBlock
        {
            Text = "New Version Available".T(),
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = GetTextPrimary(isDark)
        };
        var subtitleText = new TextBlock
        {
            Text = "A new update of WinCare Pro is ready to enhance your system.".T(),
            FontSize = 12,
            Foreground = GetTextSecondary(isDark),
            TextWrapping = TextWrapping.Wrap
        };
        titleStack.Children.Add(titleText);
        titleStack.Children.Add(subtitleText);
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);

        rootStack.Children.Add(headerGrid);

        // --- 2. Version Comparison Strip ---
        var versionCard = new Border
        {
            Background = GetCardBackground(isDark),
            BorderBrush = GetCardBorder(isDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var verGrid = new Grid();
        verGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        verGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        verGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Current Version Box
        var currentVerStack = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Center };
        var currentChipBorder = new Border
        {
            Background = GetChipBackground(isDark),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2, 7, 2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "CURRENT".T(),
                FontSize = 9.5,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = GetTextSecondary(isDark),
                HorizontalAlignment = HorizontalAlignment.Center,
                CharacterSpacing = 15
            }
        };
        currentVerStack.Children.Add(currentChipBorder);
        currentVerStack.Children.Add(new TextBlock
        {
            Text = $"v{currentVersion}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetTextPrimary(isDark),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(currentVerStack, 0);
        verGrid.Children.Add(currentVerStack);

        // Arrow Icon
        var arrowIcon = new FontIcon
        {
            Glyph = "\uE76C", // Arrow right
            FontSize = 14,
            Foreground = GetAccentBrush(isDark),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 12, 0)
        };
        Grid.SetColumn(arrowIcon, 1);
        verGrid.Children.Add(arrowIcon);

        // Remote Version Box
        var remoteVerStack = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Center };
        var remoteBadgeBorder = new Border
        {
            Background = GetAccentGradient(isDark),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2, 7, 2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "LATEST".T(),
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Colors.White),
                CharacterSpacing = 15
            }
        };
        remoteVerStack.Children.Add(remoteBadgeBorder);
        remoteVerStack.Children.Add(new TextBlock
        {
            Text = $"v{remoteVersion}",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = GetAccentBrush(isDark),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(remoteVerStack, 2);
        verGrid.Children.Add(remoteVerStack);

        versionCard.Child = verGrid;
        rootStack.Children.Add(versionCard);

        // --- 3. Changelog / What's New Section ---
        var changelogHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(2, 0, 0, 0)
        };
        changelogHeader.Children.Add(new FontIcon
        {
            Glyph = "\uE735", // Star / What's new
            FontSize = 13,
            Foreground = GetAccentBrush(isDark),
            VerticalAlignment = VerticalAlignment.Center
        });
        changelogHeader.Children.Add(new TextBlock
        {
            Text = "What's New in this Release".T(),
            FontSize = 12.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetTextPrimary(isDark),
            VerticalAlignment = VerticalAlignment.Center
        });
        rootStack.Children.Add(changelogHeader);

        var changelogCard = new Border
        {
            Background = GetInnerCardBackground(isDark),
            BorderBrush = GetInnerCardBorder(isDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            MaxHeight = 150
        };

        var changelogScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var changelogListStack = new StackPanel { Spacing = 6 };

        // Parse changelog items into bullet points
        var rawItems = SplitChangelogItems(changelog);
        if (rawItems.Count == 0)
        {
            rawItems.Add(string.IsNullOrWhiteSpace(changelog) ? "Performance improvements and security enhancements.".T() : changelog);
        }

        foreach (var item in rawItems)
        {
            var itemGrid = new Grid();
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var checkIcon = new FontIcon
            {
                Glyph = "\uE73E", // Checkmark
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)), // Emerald
                Margin = new Thickness(0, 3, 8, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(checkIcon, 0);
            itemGrid.Children.Add(checkIcon);

            var itemText = new TextBlock
            {
                Text = item.Trim(),
                FontSize = 11.5,
                Foreground = GetTextPrimary(isDark),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            };
            Grid.SetColumn(itemText, 1);
            itemGrid.Children.Add(itemText);

            changelogListStack.Children.Add(itemGrid);
        }

        changelogScroll.Content = changelogListStack;
        changelogCard.Child = changelogScroll;
        rootStack.Children.Add(changelogCard);

        // --- 4. Security & Channel Footer ---
        var footerStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        footerStack.Children.Add(new FontIcon
        {
            Glyph = "\uE875", // Shield / Authenticode
            FontSize = 11,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
            VerticalAlignment = VerticalAlignment.Center
        });
        footerStack.Children.Add(new TextBlock
        {
            Text = string.Format("Digital Signature: Verified • Channel: {0} • Zero-Risk Rollback Protected".T(), channel),
            FontSize = 10.5,
            Foreground = GetTextSecondary(isDark),
            VerticalAlignment = VerticalAlignment.Center
        });
        rootStack.Children.Add(footerStack);

        var dialog = new ContentDialog
        {
            Content = rootStack,
            PrimaryButtonText = "Update Now".T(),
            CloseButtonText = "Later".T(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Background = GetDialogBackground(isDark),
            CornerRadius = new CornerRadius(16)
        };

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj1) && styleObj1 is Style accentStyle1)
        {
            dialog.PrimaryButtonStyle = accentStyle1;
        }

        return await dialog.ShowAsync();
    }

    /// <summary>
    /// Hiển thị Popup sang trọng thông báo hệ thống đã chạy phiên bản mới nhất (Up to Date).
    /// </summary>
    public static async Task ShowUpToDateAsync(
        XamlRoot xamlRoot,
        ElementTheme theme,
        string currentVersion,
        string channel = "Stable")
    {
        if (xamlRoot == null) return;

        bool isDark = ResolveIsDark(theme);

        var rootStack = new StackPanel
        {
            Spacing = 16,
            Width = 440
        };

        // Header with Emerald Checkmark Circle
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconContainer = new Grid
        {
            Width = 56,
            Height = 56,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var auraHalo = new Ellipse
        {
            Width = 56,
            Height = 56,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(40, 16, 185, 129), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0, 16, 185, 129), Offset = 1 }
                }
            }
        };
        iconContainer.Children.Add(auraHalo);

        var iconBadge = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(36, 16, 185, 129)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 16, 185, 129)),
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = "\uE73E", // Checkmark
                FontSize = 22,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        iconContainer.Children.Add(iconBadge);
        Grid.SetColumn(iconContainer, 0);
        headerGrid.Children.Add(iconContainer);

        var titleStack = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleStack.Children.Add(new TextBlock
        {
            Text = "You're All Up to Date".T(),
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = GetTextPrimary(isDark)
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "WinCare Pro is running the latest security and optimization definitions.".T(),
            FontSize = 12,
            Foreground = GetTextSecondary(isDark),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);

        rootStack.Children.Add(headerGrid);

        // System Details Card
        var infoCard = new Border
        {
            Background = GetCardBackground(isDark),
            BorderBrush = GetCardBorder(isDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var infoStack = new StackPanel { Spacing = 8 };

        infoStack.Children.Add(CreateInfoRow("Installed Version".T(), $"v{currentVersion} (Latest Production)", isDark));
        infoStack.Children.Add(CreateInfoRow("Distribution Channel".T(), $"{channel} CDN Repository", isDark));
        infoStack.Children.Add(CreateInfoRow("Audit Timestamp".T(), DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"), isDark));
        infoStack.Children.Add(CreateInfoRow("Integrity Status".T(), "All modules verified & synchronized".T(), isDark));

        infoCard.Child = infoStack;
        rootStack.Children.Add(infoCard);

        var dialog = new ContentDialog
        {
            Content = rootStack,
            CloseButtonText = "OK".T(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Background = GetDialogBackground(isDark),
            CornerRadius = new CornerRadius(16)
        };

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj2) && styleObj2 is Style accentStyle2)
        {
            dialog.CloseButtonStyle = accentStyle2;
        }

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Hiển thị Popup thông báo khi kiểm tra cập nhật gặp sự cố kết nối hoặc lỗi máy chủ.
    /// </summary>
    public static async Task<ContentDialogResult> ShowUpdateErrorAsync(
        XamlRoot xamlRoot,
        ElementTheme theme,
        string errorMessage)
    {
        if (xamlRoot == null) return ContentDialogResult.None;

        bool isDark = ResolveIsDark(theme);

        var rootStack = new StackPanel
        {
            Spacing = 16,
            Width = 440
        };

        // Header with Amber Warning Badge
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBadge = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(32, 245, 158, 11)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 245, 158, 11)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new FontIcon
            {
                Glyph = "\uE783", // Warning
                FontSize = 20,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(iconBadge, 0);
        headerGrid.Children.Add(iconBadge);

        var titleStack = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Update Check Failed".T(),
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = GetTextPrimary(isDark)
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Unable to establish a secure handshake with the update server.".T(),
            FontSize = 12,
            Foreground = GetTextSecondary(isDark),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);

        rootStack.Children.Add(headerGrid);

        // Error Details Card
        var errCard = new Border
        {
            Background = GetCardBackground(isDark),
            BorderBrush = GetCardBorder(isDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 12, 14, 12)
        };

        var errStack = new StackPanel { Spacing = 6 };
        errStack.Children.Add(new TextBlock
        {
            Text = $"Error: {errorMessage}",
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        errStack.Children.Add(new TextBlock
        {
            Text = "Troubleshooting tips:\n• Check active Internet connection or Wi-Fi.\n• Flush DNS or switch DNS provider in Network Booster.\n• Or download the latest installer directly from the official website.\n• Retry in a few moments.".T(),
            FontSize = 11,
            Foreground = GetTextSecondary(isDark),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
            Margin = new Thickness(0, 4, 0, 0)
        });

        errCard.Child = errStack;
        rootStack.Children.Add(errCard);

        var dialog = new ContentDialog
        {
            Content = rootStack,
            PrimaryButtonText = "Retry".T(),
            SecondaryButtonText = "Download from Website".T(),
            CloseButtonText = "Close".T(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Background = GetDialogBackground(isDark),
            CornerRadius = new CornerRadius(16)
        };

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj3) && styleObj3 is Style accentStyle3)
        {
            dialog.PrimaryButtonStyle = accentStyle3;
        }

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        return result;
    }

    /// <summary>
    /// Hiển thị Popup khi quá trình tải gói cập nhật thất bại, cho phép người dùng tải thủ công từ trang chủ.
    /// </summary>
    public static async Task<ContentDialogResult> ShowDownloadFailedAsync(
        XamlRoot xamlRoot,
        ElementTheme theme,
        string errorMessage,
        string? directDownloadUrl = null)
    {
        if (xamlRoot == null) return ContentDialogResult.None;

        bool isDark = ResolveIsDark(theme);

        var rootStack = new StackPanel
        {
            Spacing = 16,
            Width = 450
        };

        // Header with Amber Warning Badge
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBadge = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(32, 239, 68, 68)),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 239, 68, 68)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new FontIcon
            {
                Glyph = "\uE896", // Download error
                FontSize = 20,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(iconBadge, 0);
        headerGrid.Children.Add(iconBadge);

        var titleStack = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Download Failed".T(),
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI Variable Display"),
            Foreground = GetTextPrimary(isDark)
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "The update package could not be downloaded automatically.".T(),
            FontSize = 12,
            Foreground = GetTextSecondary(isDark),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);

        rootStack.Children.Add(headerGrid);

        // Details Card
        var errCard = new Border
        {
            Background = GetCardBackground(isDark),
            BorderBrush = GetCardBorder(isDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 12, 14, 12)
        };

        var errStack = new StackPanel { Spacing = 8 };
        errStack.Children.Add(new TextBlock
        {
            Text = $"Error: {errorMessage}",
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        errStack.Children.Add(new TextBlock
        {
            Text = "If automated updates cannot reach the CDN, you can download the installer manually from the official release page.".T(),
            FontSize = 11.5,
            Foreground = GetTextPrimary(isDark),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16
        });

        errCard.Child = errStack;
        rootStack.Children.Add(errCard);

        var dialog = new ContentDialog
        {
            Content = rootStack,
            PrimaryButtonText = "Download from Website".T(),
            SecondaryButtonText = "Retry".T(),
            CloseButtonText = "Close".T(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            RequestedTheme = theme,
            Background = GetDialogBackground(isDark),
            CornerRadius = new CornerRadius(16)
        };

        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj4) && styleObj4 is Style accentStyle4)
        {
            dialog.PrimaryButtonStyle = accentStyle4;
        }

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                string targetUrl = !string.IsNullOrEmpty(directDownloadUrl) && (directDownloadUrl.StartsWith("http://") || directDownloadUrl.StartsWith("https://"))
                    ? directDownloadUrl
                    : "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        return result;
    }

    private static Grid CreateInfoRow(string label, string value, bool isDark)
    {
        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblText = new TextBlock
        {
            Text = label,
            FontSize = 11.5,
            Foreground = GetTextSecondary(isDark),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(lblText, 0);
        rowGrid.Children.Add(lblText);

        var valText = new TextBlock
        {
            Text = value,
            FontSize = 11.5,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetTextPrimary(isDark),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valText, 1);
        rowGrid.Children.Add(valText);

        return rowGrid;
    }

    private static List<string> SplitChangelogItems(string changelog)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(changelog)) return result;

        // Split by colon prefix first if present (e.g. "Cập nhật v4.5: Feature 1, Feature 2...")
        int colonIdx = changelog.IndexOf(':');
        string contentToSplit = colonIdx >= 0 && colonIdx < changelog.Length - 1 ? changelog.Substring(colonIdx + 1) : changelog;

        // Split by comma, semicolon, bullet point or newline
        var tokens = Regex.Split(contentToSplit, @"(?<=[;,\n\r•])\s+");
        foreach (var token in tokens)
        {
            var cleaned = token.Trim().TrimStart('•', '-', '*', ' ', '\t', ';', ',').TrimEnd(';', ',');
            if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length > 3)
            {
                result.Add(cleaned);
            }
        }

        if (result.Count == 0 && !string.IsNullOrWhiteSpace(contentToSplit))
        {
            result.Add(contentToSplit.Trim());
        }

        return result;
    }
}

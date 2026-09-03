using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;
using WinCarePro.Shared.Components;

namespace WinCarePro.Services.Implementations;

public class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;
    private readonly System.Threading.SemaphoreSlim _dialogSemaphore = new(1, 1);

    private XamlRoot? ActiveXamlRoot
    {
        get
        {
            if (_xamlRoot != null && _xamlRoot.IsHostVisible)
            {
                return _xamlRoot;
            }
            return (App.MainWindowInstance?.Content as FrameworkElement)?.XamlRoot;
        }
    }

    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    public async Task<CleaningAction> ShowLockingAppsDialogAsync(List<LockingAppInfo> apps)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return CleaningAction.CleanAnyway;

        await _dialogSemaphore.WaitAsync();
        try
        {
            // Allow time for previous dialog's closing transition to complete
            await Task.Delay(300);

            var panel = new StackPanel { Spacing = 12 };
            
            var textBlock = new TextBlock 
            { 
                Text = "The following applications are using temporary files:".T(),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(textBlock);

            var listPanel = new StackPanel { Spacing = 8, Margin = new Thickness(8, 4, 8, 8) };
            foreach (var app in apps)
            {
                var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
                
                FrameworkElement iconElement;
                if (app.HasIcon)
                {
                    iconElement = new Image
                    {
                        Source = app.IconImageSource,
                        Width = 20,
                        Height = 20,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                else
                {
                    iconElement = new FontIcon
                    {
                        Glyph = "\uE7BA",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                itemPanel.Children.Add(iconElement);

                itemPanel.Children.Add(new TextBlock 
                { 
                    Text = $"{app.Name} ({app.ProcessCount} {"processes".T()}) - {app.LockedSizeFormatted}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });

                listPanel.Children.Add(itemPanel);
            }
            panel.Children.Add(listPanel);

            var choiceText = new TextBlock 
            { 
                Text = "What would you like to do?".T(),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(choiceText);

            CleaningAction action = CleaningAction.Cancel;

            var restartButton = new Button
            {
                Content = "Clean After Restart".T(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 440,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var currentTheme = WinCarePro.Services.ThemeManager.Instance.CurrentTheme;
            bool isDark = currentTheme == ElementTheme.Dark ||
                          (currentTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);

            var dialogBg = isDark
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(248, 18, 20, 29))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(254, 255, 255, 255));
            var dialogBorder = isDark
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(38, 255, 255, 255))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));

            var dialog = new ContentDialog
            {
                Title = "Running Applications Detected".T(),
                Content = scrollViewer,
                PrimaryButtonText = "Close Apps & Clean".T(),
                SecondaryButtonText = "Clean Anyway".T(),
                CloseButtonText = "Cancel".T(),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot,
                RequestedTheme = currentTheme,
                Background = dialogBg,
                BorderBrush = dialogBorder,
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(1)
            };

            if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var styleObj) && styleObj is Style accentStyle)
            {
                dialog.PrimaryButtonStyle = accentStyle;
            }

            restartButton.Click += (s, e) =>
            {
                action = CleaningAction.ScheduleAfterRestart;
                dialog.Hide();
            };
            panel.Children.Add(restartButton);

            var dialogResult = await dialog.ShowAsync();

            if (dialogResult == ContentDialogResult.None && action == CleaningAction.ScheduleAfterRestart)
            {
                return CleaningAction.ScheduleAfterRestart;
            }

            return dialogResult switch
            {
                ContentDialogResult.Primary => CleaningAction.CloseAndClean,
                ContentDialogResult.Secondary => CleaningAction.CleanAnyway,
                _ => CleaningAction.Cancel
            };
        }
        finally
        {
            _dialogSemaphore.Release();
        }
    }

    public async Task<bool> ShowForceClosePromptAsync(string appName)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return false;

        return await ResultDialogHelper.ShowConfirmAsync(
            xamlRoot,
            "Force Close Application",
            string.Format("{0} did not close normally. Force close?".T(), appName),
            confirmText: "Force Close",
            cancelText: "Cancel",
            isDestructive: true);
    }

    public async Task ShowMessageAsync(string title, string content)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return;

        await ResultDialogHelper.ShowCustomResultDialogAsync(
            xamlRoot,
            ResultDialogType.Info,
            title,
            content,
            primaryButtonText: "OK");
    }

    public async Task<bool> ShowForceUninstallPromptAsync(string appName)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return false;

        return await ResultDialogHelper.ShowConfirmAsync(
            xamlRoot,
            "Uninstaller Failed or Cancelled",
            string.Format("The standard uninstaller for {0} could not be completed. Would you like to perform a Force Uninstall (wipe its residual files and registry entries)?".T(), appName),
            confirmText: "Force Uninstall",
            cancelText: "Cancel",
            isDestructive: true);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel", bool isDestructive = false)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return false;

        return await ResultDialogHelper.ShowConfirmAsync(
            xamlRoot,
            title,
            message,
            confirmText: confirmText,
            cancelText: cancelText,
            isDestructive: isDestructive);
    }

    public async Task ShowSuccessAsync(string title, string message, string? detailLog = null)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return;

        await ResultDialogHelper.ShowSuccessAsync(xamlRoot, title, message, detailLog: detailLog);
    }

    public async Task ShowWarningAsync(string title, string message, string? detailLog = null)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return;

        await ResultDialogHelper.ShowWarningAsync(xamlRoot, title, message, detailLog: detailLog);
    }

    public async Task ShowErrorAsync(string title, string message, string? detailLog = null)
    {
        var xamlRoot = ActiveXamlRoot;
        if (xamlRoot == null) return;

        await ResultDialogHelper.ShowErrorAsync(xamlRoot, title, message, detailLog: detailLog);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;

    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    public async Task<CleaningAction> ShowLockingAppsDialogAsync(List<LockingAppInfo> apps)
    {
        if (_xamlRoot == null) return CleaningAction.CleanAnyway;

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

        var dialog = new ContentDialog
        {
            Title = "Running Applications Detected".T(),
            Content = panel,
            PrimaryButtonText = "Close Apps & Clean".T(),
            SecondaryButtonText = "Clean Anyway".T(),
            CloseButtonText = "Cancel".T(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = _xamlRoot
        };

        restartButton.Click += (s, e) =>
        {
            action = CleaningAction.ScheduleAfterRestart;
            dialog.Hide();
        };
        panel.Children.Add(restartButton);

        var dialogResult = await dialog.ShowAsync();
        if (action == CleaningAction.ScheduleAfterRestart)
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

    public async Task<bool> ShowForceClosePromptAsync(string appName)
    {
        if (_xamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = "Force Close Application".T(),
            Content = string.Format("{0} did not close normally. Force close?".T(), appName),
            PrimaryButtonText = "Force Close".T(),
            CloseButtonText = "Cancel".T(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = _xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowMessageAsync(string title, string content)
    {
        if (_xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK".T(),
            XamlRoot = _xamlRoot
        };

        await dialog.ShowAsync();
    }
}

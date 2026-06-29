using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;

namespace WinCarePro.Views;

public sealed partial class ProcessPage : Page
{
    public ProcessViewModel ViewModel { get; }
    private ProcessInfo? _rightClickedProcess;
    private bool _isSyncingPriorityCombo;

    public ProcessPage()
    {
        ViewModel = App.Services.GetRequiredService<ProcessViewModel>();
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    internal string FormatPercent(double val) => $"{val:F1}%";

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }

    private async void OnRefreshProcessesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshProcessesAsync();
    }

    private async void OnRamBoostClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.OptimizeMemoryAsync();
    }

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string column)
        {
            ViewModel.ChangeSort(column);
        }
    }

    private void CloseDetails_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedProcess = null;
        ProcessList.SelectedItem = null;
    }

    private void ProcessList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcessList.SelectedItem is ProcessInfo process)
        {
            ViewModel.SelectedProcess = process;
            SyncPriorityCombo(process.PriorityClass);
        }
        else
        {
            ViewModel.SelectedProcess = null;
        }
    }

    private void SyncPriorityCombo(string priorityClass)
    {
        if (string.IsNullOrEmpty(priorityClass)) return;
        _isSyncingPriorityCombo = true;
        try
        {
            foreach (ComboBoxItem item in PriorityCombo.Items)
            {
                if (item.Tag is string tag && string.Equals(tag, priorityClass, StringComparison.OrdinalIgnoreCase))
                {
                    PriorityCombo.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _isSyncingPriorityCombo = false;
        }
    }

    private async void PriorityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingPriorityCombo) return;

        if (PriorityCombo.SelectedItem is ComboBoxItem item && item.Tag is string priorityStr)
        {
            if (ViewModel.SelectedProcess != null && ViewModel.SelectedProcess.PriorityClass != priorityStr)
            {
                await ViewModel.UpdateSelectedProcessPriorityAsync(priorityStr);
            }
        }
    }

    private async void OnSuspendClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SuspendSelectedProcessAsync();
    }

    private async void OnResumeClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ResumeSelectedProcessAsync();
    }

    private async void OnEndTaskClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProcess != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirm End Task",
                Content = $"Are you sure you want to terminate {ViewModel.SelectedProcess.Name}?",
                PrimaryButtonText = "End Process",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.EndProcessAsync(ViewModel.SelectedProcess.Id, ViewModel.SelectedProcess.Name);
            }
        }
    }

    private async void OnEndProcessTreeClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProcess != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirm End Process Tree",
                Content = $"Are you sure you want to terminate {ViewModel.SelectedProcess.Name} and all its child processes?",
                PrimaryButtonText = "End Process Tree",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.EndProcessTreeAsync(ViewModel.SelectedProcess.Id, ViewModel.SelectedProcess.Name);
            }
        }
    }

    private void ProcessRow_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (sender is Grid grid)
        {
            var flyout = FlyoutBase.GetAttachedFlyout(grid) as MenuFlyout;
            if (flyout != null)
            {
                _rightClickedProcess = grid.DataContext as ProcessInfo;
                Services.TranslationManager.Instance.Translate(flyout);
                if (args.TryGetPosition(grid, out var point))
                {
                    flyout.ShowAt(grid, point);
                }
                else
                {
                    flyout.ShowAt(grid);
                }
                args.Handled = true;
            }
        }
    }

    private async void OnEndTaskContextClick(object sender, RoutedEventArgs e)
    {
        if (_rightClickedProcess != null)
        {
            await ViewModel.EndProcessAsync(_rightClickedProcess.Id, _rightClickedProcess.Name);
        }
    }

    private async void OnEndProcessTreeContextClick(object sender, RoutedEventArgs e)
    {
        if (_rightClickedProcess != null)
        {
            await ViewModel.EndProcessTreeAsync(_rightClickedProcess.Id, _rightClickedProcess.Name);
        }
    }

    private void OnOpenLocationContextClick(object sender, RoutedEventArgs e)
    {
        if (_rightClickedProcess != null && !string.IsNullOrEmpty(_rightClickedProcess.FilePath) && File.Exists(_rightClickedProcess.FilePath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{_rightClickedProcess.FilePath}\"");
            }
            catch { }
        }
    }

    private void OnSearchOnlineContextClick(object sender, RoutedEventArgs e)
    {
        if (_rightClickedProcess != null)
        {
            try
            {
                string url = $"https://www.google.com/search?q={Uri.EscapeDataString(_rightClickedProcess.Name)}";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }
    }
}

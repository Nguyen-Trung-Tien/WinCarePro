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

    public ProcessPage()
    {
        ViewModel = App.Services.GetRequiredService<ProcessViewModel>();
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopMonitoring();
    }

    private async void OnRefreshProcessesClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshProcessesAsync();
    }

    private void ProcessRow_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (sender is Grid grid)
        {
            var flyout = FlyoutBase.GetAttachedFlyout(grid) as MenuFlyout;
            if (flyout != null)
            {
                _rightClickedProcess = grid.DataContext as ProcessInfo;
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
            await ViewModel.EndProcessAsync(_rightClickedProcess.Id);
        }
    }

    private async void OnEndProcessTreeContextClick(object sender, RoutedEventArgs e)
    {
        if (_rightClickedProcess != null)
        {
            await ViewModel.EndProcessTreeAsync(_rightClickedProcess.Id);
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

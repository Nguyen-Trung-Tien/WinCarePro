using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class ProcessPage : Page
{
    public ProcessViewModel ViewModel { get; }

    public ProcessPage()
    {
        InitializeComponent();
        ViewModel = new ProcessViewModel();
        this.Loaded += (s, e) => DataContext = ViewModel;
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshProcessesAsync();
    }

    private async void OnEndTaskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int pid)
        {
            await ViewModel.EndProcessAsync(pid);
        }
    }

    private async void OnEndTreeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int pid)
        {
            await ViewModel.EndProcessTreeAsync(pid);
        }
    }

    private void OnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string col)
        {
            ViewModel.ChangeSort(col);
        }
    }

    internal bool IsNot(bool val) => !val;

    internal static Brush GetCpuColor(double cpu)
    {
        if (cpu > 70.0) return new SolidColorBrush(Colors.Red);
        if (cpu > 20.0) return new SolidColorBrush(Colors.Orange);
        // Standard theme foreground (dark mode safe default color)
        return (Brush)Application.Current.Resources["TextControlForeground"];
    }

    internal static string FormatCpu(double cpu) => $"{cpu:F1}%";
}

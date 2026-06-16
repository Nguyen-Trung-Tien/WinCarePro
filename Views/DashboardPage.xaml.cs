using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.ViewModels;

namespace WinCarePro.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        // Set up local viewModel instance
        ViewModel = new DashboardViewModel();
        DataContext = ViewModel;
    }

    private async void OnScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunFullDiagnosticsAsync();
    }

    internal string GetStatusText(int score)
    {
        if (score >= 90) return "EXCELLENT - Your system is highly optimized and clean.";
        if (score >= 70) return "GOOD - Some areas can be optimized to reclaim storage.";
        return "NEEDS OPTIMIZATION - Heavy junk logs or updates required.";
    }

    internal bool IsNot(bool val) => !val;
    internal string FormatPercent(double val) => $"{val:F1}%";

    internal Visibility GetVisibility(int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal static string GetStatusIcon(bool healthy)
    {
        return healthy ? "\uE73E" : "\uE7BA"; // Checkmark or Warning glyph
    }

    internal static string GetStatusLabel(bool healthy)
    {
        return healthy ? "Optimized" : "Action Recommended";
    }

    internal static Brush GetStatusColor(bool healthy)
    {
        var color = healthy ? Colors.LightGreen : Colors.Orange;
        // Adjust for default light/dark backgrounds if necessary, using a clean system color
        return new SolidColorBrush(color);
    }
}

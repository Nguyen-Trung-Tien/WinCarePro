using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using Windows.UI;

namespace WinCarePro.Views;

public sealed partial class SystemOptimizerPage : Page
{
    public SystemOptimizerViewModel ViewModel { get; }

    public SystemOptimizerPage()
    {
        ViewModel = App.Services.GetRequiredService<SystemOptimizerViewModel>();
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplySelectedAsync();
    }

    private void OnReloadTweaksClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadTweaks();
    }

    internal bool IsNot(bool val) => !val;

    internal Brush GetBorderBrush(bool active)
    {
        if (active)
        {
            return new SolidColorBrush(Color.FromArgb(255, 232, 17, 35));
        }
        else
        {
            if (Application.Current.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var brushObj) && brushObj is Brush brush)
            {
                return brush;
            }
            return new SolidColorBrush(Color.FromArgb(255, 204, 204, 204));
        }
    }

    internal Brush GetCircleBg(bool active)
    {
        return active 
            ? new SolidColorBrush(Color.FromArgb(30, 232, 17, 35)) 
            : new SolidColorBrush(Color.FromArgb(20, 127, 86, 217));
    }

    internal Brush GetGlyphColor(bool active)
    {
        if (active)
        {
            return new SolidColorBrush(Color.FromArgb(255, 232, 17, 35));
        }
        else
        {
            if (Application.Current.Resources.TryGetValue("SystemAccentColorBrush", out var brushObj) && brushObj is Brush brush)
            {
                return brush;
            }
            if (Application.Current.Resources.TryGetValue("SystemAccentColor", out var colorObj) && colorObj is Color color)
            {
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
        }
    }
}

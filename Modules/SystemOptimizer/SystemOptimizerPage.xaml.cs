using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class SystemOptimizerPage : Page
{
    private DispatcherTimer? _ramTimer;

    public SystemOptimizerViewModel ViewModel { get; }

    public SystemOptimizerPage()
    {
        ViewModel = App.Services.GetRequiredService<SystemOptimizerViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Refresh values on page entry
        ViewModel.UpdateRamAndServices();
        ViewModel.LoadTweaks();

        // Auto-run AI Diagnostics scan for optimizer recommendations
        _ = ViewModel.RunAiScanAsync();

        // Setup periodic RAM update timer (1.5 seconds)
        if (_ramTimer == null)
        {
            _ramTimer = new DispatcherTimer();
            _ramTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _ramTimer.Tick += RamTimer_Tick;
        }
        _ramTimer.Start();
    }

    private async void OnRunAiScanClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.IsEnabled = false;
            try
            {
                await System.Threading.Tasks.Task.Delay(350);
                await ViewModel.RunAiScanAsync();

                if (App.MainWindowInstance is MainWindow mw)
                {
                    mw.ShowToastFromDb("AI Diagnostics Complete".T(), 
                        $"AI Health Score: {ViewModel.AiHealthScore}/100. {ViewModel.AiSummaryText}", "Success");
                }
            }
            catch { }
            finally
            {
                btn.IsEnabled = true;
            }
        }
        else
        {
            await ViewModel.RunAiScanAsync();
        }
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Stop timer when navigating away to conserve resources
        _ramTimer?.Stop();
    }

    private void RamTimer_Tick(object? sender, object e)
    {
        ViewModel.UpdateRamAndServices();
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        int applied = await ViewModel.ApplySelectedAsync();

        string msg = applied > 0 
            ? string.Format("Successfully applied {0} Windows system tweaks and purged memory cache for maximum responsiveness.".T(), applied)
            : "Your system tweaks and memory resources are already fully optimized!".T();

        ContentDialog dialog = new ContentDialog
        {
            Title = "System Optimization Complete".T(),
            Content = msg,
            CloseButtonText = "OK".T(),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }

    private void OnReloadTweaksClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadTweaks();
    }

    private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Confirm Restore".T(),
            Content = "Are you sure you want to restore default Windows settings for all tweaks?".T(),
            PrimaryButtonText = "Yes, Restore".T(),
            CloseButtonText = "Cancel".T(),
            XamlRoot = this.XamlRoot,
            RequestedTheme = ThemeManager.Instance.CurrentTheme
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RestoreDefaultsAsync();
        }
    }

    private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Pivot pivot && pivot.SelectedItem is PivotItem item)
        {
            string category = item.Tag as string ?? "All";
            ViewModel.FilterTweaks(category);
        }
    }

    private async void OnToggleTweakClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SystemTweak tweak)
        {
            await ViewModel.ToggleTweakAsync(tweak);
        }
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BoostRamAsync();
    }

    public bool IsNot(bool val) => !val;

    public string GetPercentageText(double val) => $"{val:F0}%";

    public Style? GetStatusBadgeStyle(bool isOptimized)
    {
        if (isOptimized)
        {
            return Application.Current.Resources.TryGetValue("StatusBadgeGoodStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
        else
        {
            return Application.Current.Resources.TryGetValue("StatusBadgeWarningStyle", out var styleObj) && styleObj is Style style 
                 ? style 
                 : null;
        }
    }

    private async void OnTurboToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            if (ts.IsOn != ViewModel.IsTurboActive)
            {
                await ViewModel.ToggleGamingTurboAsync();
                if (TurboStatusText != null)
                {
                    TurboStatusText.Text = ViewModel.GamingStatusMessage;
                }
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Core.Helpers;
using WinCarePro.Shared.Animations;

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

        this.Loaded += (s, e) => TranslationManager.Instance.Translate(this);
        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) => TranslationManager.Instance.LanguageChanged -= langHandler;
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

    private async void OnSmartTuneClick(object sender, RoutedEventArgs e)
    {
        var btn = SmartTuneBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 320);

        try
        {
            if (TweaksCard != null) WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(TweaksCard, Windows.UI.Color.FromArgb(220, 139, 92, 246));
        }
        catch { }

        int applied = 0;
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, null, null, null,
            "Auto-Tuning...", "Auto-Tune",
            async () =>
            {
                applied = await ViewModel.ApplySmartAutoTuneAsync();
            },
            minDurationMs: 800);

        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, Windows.UI.Color.FromArgb(255, 16, 185, 129));
            }
            var statCards = new System.Collections.Generic.List<FrameworkElement>();
            if (StatCard0 != null) statCards.Add(StatCard0);
            if (StatCard1 != null) statCards.Add(StatCard1);
            if (StatCard2 != null) statCards.Add(StatCard2);
            if (StatCard3 != null) statCards.Add(StatCard3);
            WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DCascadeWave(statCards, 60);
        }
        catch { }

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Smart Auto-Tune Complete".T(), 
                string.Format("Successfully tuned {0} system settings & optimized RAM.".T(), applied), "Success");
        }
    }

    private async void OnGamingProfileClick(object sender, RoutedEventArgs e)
    {
        var btn = (sender as Button) ?? (sender as FrameworkElement) ?? SmartTuneBtn;
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 320);

        try
        {
            if (TweaksCard != null) WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(TweaksCard, Windows.UI.Color.FromArgb(220, 245, 158, 11));
        }
        catch { }

        int applied = 0;
        if (sender is Button b)
        {
            await UiLoadingHelper.ExecuteWithLoadingAsync(
                b, null, null, null,
                "Activating Gaming...", "Gaming Mode",
                async () =>
                {
                    applied = await ViewModel.ApplyGamingProfileAsync();
                },
                minDurationMs: 800);
        }
        else
        {
            applied = await ViewModel.ApplyGamingProfileAsync();
        }

        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, Windows.UI.Color.FromArgb(255, 245, 158, 11));
            }
            if (StatCard3 != null) WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(StatCard3, Windows.UI.Color.FromArgb(255, 245, 158, 11));
        }
        catch { }

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Gaming Profile Active".T(), 
                string.Format("Gaming Turbo ON, network latency minimized, {0} tweaks applied.".T(), applied), "Success");
        }
    }

    private async void OnPrivacyProfileClick(object sender, RoutedEventArgs e)
    {
        var btn = (sender as Button) ?? (sender as FrameworkElement) ?? SmartTuneBtn;
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.06f, 320);

        try
        {
            if (TweaksCard != null) WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(TweaksCard, Windows.UI.Color.FromArgb(220, 16, 185, 129));
        }
        catch { }

        int applied = 0;
        if (sender is Button b)
        {
            await UiLoadingHelper.ExecuteWithLoadingAsync(
                b, null, null, null,
                "Hardening Privacy...", "Privacy Shield",
                async () =>
                {
                    applied = await ViewModel.ApplyPrivacyProfileAsync();
                },
                minDurationMs: 800);
        }
        else
        {
            applied = await ViewModel.ApplyPrivacyProfileAsync();
        }

        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, Windows.UI.Color.FromArgb(255, 16, 185, 129));
            }
        }
        catch { }

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("Privacy Shield Active".T(), 
                string.Format("Disabled background telemetry & logs ({0} tweaks applied).".T(), applied), "Success");
        }
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        var btn = ApplyTweaksBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);

        try
        {
            if (TweaksCard != null) WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(TweaksCard, Windows.UI.Color.FromArgb(220, 139, 92, 246));
        }
        catch { }

        int applied = 0;
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, ApplyTweaksRing, ApplyTweaksText, ApplyTweaksIcon,
            "Applying...", "Apply Tweaks",
            async () =>
            {
                applied = await ViewModel.ApplySelectedAsync();
            },
            minDurationMs: 1000);

        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, Windows.UI.Color.FromArgb(255, 16, 185, 129));
            }
            if (StatCard1 != null) WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(StatCard1);
        }
        catch { }

        string msg = applied > 0 
            ? string.Format("Successfully applied {0} Windows system tweaks and purged memory cache for maximum responsiveness.".T(), applied)
            : "Your selected system tweaks and memory resources are already fully optimized!".T();

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

    private async void OnRunAiScanClick(object sender, RoutedEventArgs e)
    {
        var btn = RunAiScanBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, null, null, AiScanIcon,
            "Scanning...", "Scan",
            async () =>
            {
                await ViewModel.RunAiScanAsync();
            },
            minDurationMs: 500);

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("AI Diagnostics Complete".T(), 
                $"AI Efficiency Score: {ViewModel.AiHealthScore}/100 ({ViewModel.EfficiencyGradeText}). {ViewModel.AiSummaryText}", "Success");
        }
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        var btn = BoostRamBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, BoostRamRing, BoostRamText, BoostRamIcon,
            "Purging RAM...", "Purge RAM Cache",
            async () =>
            {
                await ViewModel.BoostRamAsync();
            },
            minDurationMs: 800);
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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            ViewModel.SearchQuery = tb.Text;
        }
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAll();
    }

    private void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAll();
    }

    private void OnSelectUnappliedClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectUnoptimizedOnly();
    }

    private async void OnToggleTweakClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SystemTweak tweak)
        {
            await ViewModel.ToggleTweakAsync(tweak);
        }
    }

    private async void OnCleanDeliveryCacheClick(object sender, RoutedEventArgs e)
    {
        var btn = CleanCacheBtn ?? (sender as Button);
        long freed = 0;
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            btn, CleanCacheRing, CleanCacheText, CleanCacheIcon,
            "Purging Cache...", "Purge Delivery Cache",
            async () =>
            {
                freed = await ViewModel.CleanDeliveryCacheAsync();
            },
            minDurationMs: 800);

        if (App.MainWindowInstance is MainWindow mw && freed > 0)
        {
            double mb = freed / 1024.0 / 1024.0;
            mw.ShowToastFromDb("Delivery Cache Purged".T(), 
                string.Format("Successfully freed {0:F1} MB from Windows Delivery Optimization cache.".T(), mb), "Success");
        }
    }

    private async void OnTurboToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            if (ts.IsOn != ViewModel.IsTurboActive)
            {
                await ViewModel.ToggleGamingTurboAsync();
            }
        }
        else if (sender is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            if (lts.IsOn != ViewModel.IsTurboActive)
            {
                lts.IsLoading = true;
                try
                {
                    await ViewModel.ToggleGamingTurboAsync();
                }
                finally
                {
                    lts.IsLoading = false;
                }
            }
        }
    }

    public bool IsNot(bool val) => !val;
}

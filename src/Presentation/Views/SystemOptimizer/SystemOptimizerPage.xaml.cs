using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinCarePro.ViewModels;
using WinCarePro.Models;
using WinCarePro.Services;
using WinCarePro.Core.Helpers;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Shared.Animations;
using WinCarePro.Shared.Components;

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

        this.Loaded += (s, e) =>
        {
            TranslationManager.Instance.Translate(this);
            UpdateLoadingOverlayState();
        };
        var langHandler = new EventHandler((s, e) =>
        {
            this.DispatcherQueue?.TryEnqueue(() => TranslationManager.Instance.Translate(this));
        });
        TranslationManager.Instance.LanguageChanged += langHandler;
        this.Unloaded += (s, e) => TranslationManager.Instance.LanguageChanged -= langHandler;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsLoading))
            {
                UpdateLoadingOverlayState();
            }
        };
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        ViewModel.Initialize();

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
        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
            }
        }
        catch { }
        ViewModel.Cleanup();
    }

    private void RamTimer_Tick(object? sender, object e)
    {
        if (ViewModel.IsBusy || ViewModel.IsLoading || ViewModel.IsOperationActive) return;
        ViewModel.UpdateRamAndServices();
    }

    private void OnSelectRecommendedProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.06f, 300);
        int count = ViewModel.SelectPresetProfile("Recommended");
        HighlightPresetSelection(Windows.UI.Color.FromArgb(255, 6, 182, 212), "Recommended Auto-Tune".T(), count);
    }

    private void OnSelectPerformanceProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.06f, 300);
        int count = ViewModel.SelectPresetProfile("Performance");
        HighlightPresetSelection(Windows.UI.Color.FromArgb(255, 245, 158, 11), "Max Performance".T(), count);
    }

    private void OnSelectPrivacyProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.06f, 300);
        int count = ViewModel.SelectPresetProfile("Privacy");
        HighlightPresetSelection(Windows.UI.Color.FromArgb(255, 16, 185, 129), "Privacy Shield".T(), count);
    }

    private void OnSelectGamingProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.06f, 300);
        int count = ViewModel.SelectPresetProfile("Gaming");
        HighlightPresetSelection(Windows.UI.Color.FromArgb(255, 139, 92, 246), "Gaming Ultra".T(), count);
    }

    private void OnSelectEcoProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.06f, 300);
        int count = ViewModel.SelectPresetProfile("Eco");
        HighlightPresetSelection(Windows.UI.Color.FromArgb(255, 59, 130, 246), "Eco Balanced".T(), count);
    }

    private void HighlightPresetSelection(Windows.UI.Color glowColor, string presetName, int count)
    {
        try
        {
            if (TweaksCard != null)
            {
                WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, glowColor);
            }
            if (ApplyTweaksBtn != null)
            {
                FluidAnimationHelper.ApplyGlowSparkBurst(ApplyTweaksBtn, 1.08f, 350);
            }
        }
        catch { }

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb(string.Format("Preset: {0}".T(), presetName), 
                string.Format("Selected {0} tweaks. Click 'Apply Tweaks' to proceed.".T(), count), "Info");
        }
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        var btn = ApplyTweaksBtn ?? (sender as Button);
        if (btn != null) FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.08f, 350);

        int applied = 0;
        try
        {
            if (TweaksCard != null) WinCarePro.Core.Helpers.Animation3DHelper.Start3DScanEffect(TweaksCard, Windows.UI.Color.FromArgb(220, 139, 92, 246));

            await UiLoadingHelper.ExecuteWithLoadingAsync(
                btn, ApplyTweaksRing, ApplyTweaksText, ApplyTweaksIcon,
                "Applying...", "Apply Tweaks",
                async () =>
                {
                    applied = await ViewModel.ApplySelectedAsync();
                },
                minDurationMs: 1000);
        }
        finally
        {
            try
            {
                if (TweaksCard != null)
                {
                    WinCarePro.Core.Helpers.Animation3DHelper.Stop3DScanEffect(TweaksCard);
                    WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(TweaksCard, Windows.UI.Color.FromArgb(255, 16, 185, 129));
                }
                if (StatCard1 != null) WinCarePro.Core.Helpers.Animation3DHelper.Trigger3DOptimizeBurst(StatCard1);
                if (btn != null) WinCarePro.Core.Helpers.AnimationHelper.StartSuccessPopAnimation(btn, 1.10f, 400);
            }
            catch { }
        }

        string msg = applied > 0 
            ? string.Format("Successfully applied {0} Windows system tweaks and purged memory cache for maximum responsiveness.".T(), applied)
            : "Your selected system tweaks and memory resources are already fully optimized!".T();

        try
        {
            await ResultDialogHelper.ShowSuccessAsync(this.XamlRoot, "System Optimization Complete", msg);
        }
        catch { }
    }

    private async void OnRunAiScanClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunAiScanAsync();

        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb("AI Diagnostics Complete".T(), 
                $"AI Efficiency Score: {ViewModel.AiHealthScore}/100 ({ViewModel.EfficiencyGradeText}). {ViewModel.AiSummaryText}", "Success");
        }
    }

    private async void OnBoostRamClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BoostRamAsync();
    }

    private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
    {
        bool confirmed = await ResultDialogHelper.ShowConfirmAsync(
            this.XamlRoot,
            "Confirm Restore",
            "Are you sure you want to restore default Windows settings for all tweaks?".T(),
            confirmText: "Yes, Restore",
            cancelText: "Cancel");

        if (confirmed)
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

    private void OnMasterSelectAllChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb)
        {
            if (cb.IsChecked == true)
                ViewModel.SelectAll();
            else
                ViewModel.DeselectAll();
        }
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
        long freed = await ViewModel.CleanDeliveryCacheAsync();
        if (App.MainWindowInstance is MainWindow mw && freed > 0)
        {
            double mb = freed / 1024.0 / 1024.0;
            mw.ShowToastFromDb("Delivery Cache Purged".T(), 
                string.Format("Successfully freed {0:F1} MB from Windows Delivery Optimization cache.".T(), mb), "Success");
        }
    }

    private async void OnOptimizeRamClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(b, 1.1f, 300);
            await ViewModel.OptimizeRamAsync();
        }
        else
        {
            await ViewModel.OptimizeRamAsync();
        }
    }

    private void UpdateLoadingOverlayState()
    {
        if (LoadingOverlayGrid == null || FadeInLoading == null || FadeOutLoading == null) return;

        if (ViewModel.IsLoading)
        {
            LoadingOverlayGrid.Visibility = Visibility.Visible;
            FadeInLoading.Begin();
        }
        else
        {
            FadeOutLoading.Begin();
        }
    }

    private void FadeOutLoading_Completed(object? sender, object e)
    {
        if (!ViewModel.IsLoading && LoadingOverlayGrid != null)
        {
            LoadingOverlayGrid.Visibility = Visibility.Collapsed;
        }
    }

    public static Microsoft.UI.Xaml.Media.Brush GetTweakBadgeBg(bool isOptimized) =>
        isOptimized 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(30, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(20, 59, 130, 246));

    public static Microsoft.UI.Xaml.Media.Brush GetTweakBadgeBorder(bool isOptimized) =>
        isOptimized 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(48, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(32, 59, 130, 246));

    public static Microsoft.UI.Xaml.Media.Brush GetTweakBadgeFg(bool isOptimized) =>
        isOptimized 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246));

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.08f, 300);
        await ViewModel.RefreshAsync();
    }

    private void OnRefreshAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        _ = ViewModel.RefreshAsync();
    }

    private void OnFindAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        TweakSearchBox?.Focus(FocusState.Programmatic);
        TweakSearchBox?.SelectAll();
    }

    private void OnEscapeAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        OnResetFilterClick(this, new RoutedEventArgs());
    }

    private void OnResetFilterClick(object sender, RoutedEventArgs e)
    {
        if (TweakSearchBox != null)
        {
            TweakSearchBox.Text = string.Empty;
        }
        ViewModel.SearchQuery = string.Empty;
        ViewModel.FilterTweaks("All");
        if (CategoryPivot != null)
        {
            CategoryPivot.SelectedIndex = 0;
        }
    }

    private void OnSortSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string sortKey)
        {
            ViewModel.CurrentSort = sortKey;
        }
    }

    private async void OnExportReportClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) FluidAnimationHelper.ApplyGlowSparkBurst(fe, 1.08f, 300);

        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Text Report (*.txt)", new System.Collections.Generic.List<string>() { ".txt" });
            savePicker.FileTypeChoices.Add("Markdown File (*.md)", new System.Collections.Generic.List<string>() { ".md" });
            savePicker.SuggestedFileName = $"WinCarePro_OptimizerReport_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await ViewModel.ExportReportAsync(file.Path);
            }
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerPage.OnExportReportClick", ex);
            string defaultPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"WinCarePro_OptimizerReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await ViewModel.ExportReportAsync(defaultPath);
        }
    }

    public bool IsNot(bool val) => !val;
    public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
}

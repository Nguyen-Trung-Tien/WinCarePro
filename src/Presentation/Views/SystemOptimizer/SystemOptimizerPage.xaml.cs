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
using WinCarePro.Shared.Components;

namespace WinCarePro.Views;

public sealed partial class SystemOptimizerPage : Page
{
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
        };
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
        
        ViewModel.Initialize();

        // Refresh values on page entry on-demand (zero continuous background polling)
        ViewModel.UpdateRamAndServices();
        ViewModel.LoadTweaks();
        _ = ViewModel.LoadStartupItemsAsync();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Cleanup();
    }

    private void OnSelectRecommendedProfileClick(object sender, RoutedEventArgs e)
    {
        int count = ViewModel.SelectPresetProfile("Recommended");
        HighlightPresetSelection("Office & Productivity".T(), count);
    }

    private void OnSelectPerformanceProfileClick(object sender, RoutedEventArgs e)
    {
        int count = ViewModel.SelectPresetProfile("Performance");
        HighlightPresetSelection("Max Performance".T(), count);
    }

    private void OnSelectPrivacyProfileClick(object sender, RoutedEventArgs e)
    {
        int count = ViewModel.SelectPresetProfile("Privacy");
        HighlightPresetSelection("Privacy Shield".T(), count);
    }

    private void OnSelectGamingProfileClick(object sender, RoutedEventArgs e)
    {
        int count = ViewModel.SelectPresetProfile("Gaming");
        HighlightPresetSelection("Gaming Focus".T(), count);
    }

    private void OnSelectEcoProfileClick(object sender, RoutedEventArgs e)
    {
        int count = ViewModel.SelectPresetProfile("Eco");
        HighlightPresetSelection("Daily Balanced".T(), count);
    }

    private void HighlightPresetSelection(string presetName, int count)
    {
        if (App.MainWindowInstance is MainWindow mw)
        {
            mw.ShowToastFromDb(string.Format("Preset: {0}".T(), presetName), 
                string.Format("Selected {0} tweaks. Click 'Apply Tweaks' to proceed.".T(), count), "Info");
        }
    }

    private async void OnApplyTweaksClick(object sender, RoutedEventArgs e)
    {
        var btn = ApplyTweaksBtn ?? (sender as Button);
        int applied = 0;
        try
        {
            await UiLoadingHelper.ExecuteWithLoadingAsync(
                btn, ApplyTweaksRing, ApplyTweaksText, ApplyTweaksIcon,
                "Applying...", "Apply Tweaks",
                async () =>
                {
                    applied = await ViewModel.ApplySelectedAsync();
                },
                minDurationMs: 400);
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("SystemOptimizerPage.OnApplyTweaksClick", ex);
        }

        string msg = applied > 0 
            ? string.Format("Successfully applied {0} Windows system policy tweaks.".T(), applied)
            : "Your selected system tweaks are already fully applied!".T();

        try
        {
            await ResultDialogHelper.ShowSuccessAsync(this.XamlRoot, "System Optimization Complete", msg);
        }
        catch { }
    }

    private async void OnStartupItemToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ts.DataContext is StartupEntry entry)
        {
            await ViewModel.ToggleStartupItemAsync(entry, ts.IsOn);
        }
    }

    private async void OnToggleServiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ServiceStatusItem item)
        {
            await ViewModel.ToggleServiceStateAsync(item);
        }
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

    public static string GetServiceActionText(bool isRunning) => isRunning ? "Stop" : "Start";

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
        _ = ViewModel.LoadStartupItemsAsync();
    }

    private void OnRefreshAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        _ = ViewModel.RefreshAsync();
        _ = ViewModel.LoadStartupItemsAsync();
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

using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinCarePro.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace WinCarePro.Views;

public sealed partial class CleanerPage : Page
{
    public CleanerViewModel ViewModel { get; }
    private CancellationTokenSource? _pageCts;

    public CleanerPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CleanerViewModel>();
        DataContext = ViewModel;

        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        WinCarePro.Services.TranslationManager.Instance.Translate(this);
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Smart Caching: Reuse cached audit without hammering disk I/O on every tab switch
        if (ViewModel.HasCompletedScan && ViewModel.CleanerCategories.Count > 0)
        {
            return;
        }

        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = new CancellationTokenSource();

        try
        {
            await ViewModel.ScanAllAsync(_pageCts.Token);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[CleanerPage] Scan cancelled on page transition.");
        }
        catch (Exception ex)
        {
            Infrastructure.Logging.CrashLogger.LogException("CleanerPage.OnPageLoaded", ex);
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = null;

        this.Loaded -= OnPageLoaded;
        this.Unloaded -= OnPageUnloaded;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = null;
    }

    public Visibility ListVisibility(int count, bool isBusy) =>
        (count > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyStateVisibility(int count, bool isBusy) =>
        (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SkeletonVisibility(int count, bool isBusy) =>
        (count == 0 && isBusy) ? Visibility.Visible : Visibility.Collapsed;

    private void OnOpenDeepJunkCleanerClick(object sender, RoutedEventArgs e)
    {
        this.Frame?.Navigate(typeof(JunkPage), null, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
    }
}


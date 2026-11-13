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
        this.NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<CleanerViewModel>();
        DataContext = ViewModel;

        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Smart Lifecycle: Preserve cached scan results. Do NOT auto-trigger heavy scans on tab navigation.
        WinCarePro.Services.TranslationManager.Instance.Translate(this);
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
    }

    public Visibility ListVisibility(int count, bool isBusy) =>
        (count > 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyStateVisibility(int count, bool isBusy) =>
        (count == 0 && !isBusy) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SkeletonVisibility(int count, bool isBusy) =>
        (count == 0 && isBusy) ? Visibility.Visible : Visibility.Collapsed;
}


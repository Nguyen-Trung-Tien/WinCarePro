using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;
using WinCarePro.Shared.Animations;

namespace WinCarePro.Views;

public sealed partial class UserGuideView : UserControl
{
    private long _visibilityToken;
    private string _currentCategory = "All";
    private string _currentSearchText = string.Empty;

    public UserGuideView()
    {
        this.InitializeComponent();
        this.Loaded += UserGuideView_Loaded;
        this.Unloaded += UserGuideView_Unloaded;
    }

    private void UserGuideView_Loaded(object sender, RoutedEventArgs e)
    {
        TranslationManager.Instance.LanguageChanged += OnLanguageChanged;
        _visibilityToken = this.RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
        UpdateTranslations();
        ApplyStaggeredEntranceAnimation();
    }

    private void UserGuideView_Unloaded(object sender, RoutedEventArgs e)
    {
        TranslationManager.Instance.LanguageChanged -= OnLanguageChanged;
        if (_visibilityToken != 0)
        {
            this.UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityToken);
            _visibilityToken = 0;
        }
    }

    private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (this.Visibility == Visibility.Visible)
        {
            UpdateTranslations();
            ApplyStaggeredEntranceAnimation();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            UpdateTranslations();
        });
    }

    public void UpdateTranslations()
    {
        try
        {
            TranslationManager.Instance.Translate(this);
        }
        catch { }
    }

    private void ApplyStaggeredEntranceAnimation()
    {
        try
        {
            var visibleCards = new List<UIElement>();
            foreach (var child in CardsContainer.Children)
            {
                if (child is FrameworkElement fe && fe.Visibility == Visibility.Visible)
                {
                    visibleCards.Add(fe);
                    FluidAnimationHelper.EnableHoverDepthEffect(fe, 1.01f);
                }
            }

            if (visibleCards.Count > 0)
            {
                FluidAnimationHelper.ApplyStaggeredEntrance(visibleCards, 35);
            }
        }
        catch { }
    }

    private void OnGuideSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _currentSearchText = sender.Text?.Trim() ?? string.Empty;
        FilterCards();
    }

    private void OnCategoryFilterClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string category)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.05f, 250);
            _currentCategory = category;
            UpdateCategoryFilterStyles();
            FilterCards();
        }
    }

    private void UpdateCategoryFilterStyles()
    {
        var primaryStyle = (Style)Application.Current.Resources["VibrantPrimaryButtonStyle"];
        var secondaryStyle = (Style)Application.Current.Resources["StandardSecondaryButtonStyle"];

        FilterBtnAll.Style = _currentCategory == "All" ? primaryStyle : secondaryStyle;
        FilterBtnCare.Style = _currentCategory == "Care" ? primaryStyle : secondaryStyle;
        FilterBtnTuning.Style = _currentCategory == "Tuning" ? primaryStyle : secondaryStyle;
        FilterBtnSecurity.Style = _currentCategory == "Security" ? primaryStyle : secondaryStyle;
        FilterBtnSystem.Style = _currentCategory == "System" ? primaryStyle : secondaryStyle;
    }

    private void OnResetSearchClick(object sender, RoutedEventArgs e)
    {
        _currentSearchText = string.Empty;
        GuideSearchBox.Text = string.Empty;
        _currentCategory = "All";
        UpdateCategoryFilterStyles();
        FilterCards();
    }

    private void FilterCards()
    {
        int matchCount = 0;
        string query = _currentSearchText.ToLowerInvariant();

        foreach (var child in CardsContainer.Children)
        {
            if (child is FrameworkElement fe)
            {
                string tag = (fe.Tag as string ?? string.Empty).ToLowerInvariant();
                bool matchesCategory = _currentCategory == "All" || tag.Contains(_currentCategory.ToLowerInvariant());
                bool matchesSearch = string.IsNullOrEmpty(query) || tag.Contains(query) || SearchCardContent(fe, query);

                bool isVisible = matchesCategory && matchesSearch;
                fe.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                if (isVisible)
                {
                    matchCount++;
                }
            }
        }

        NoResultsCard.Visibility = matchCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool SearchCardContent(FrameworkElement card, string query)
    {
        try
        {
            if (card is Border border && border.Child is StackPanel sp)
            {
                return ContainsQueryInElement(sp, query);
            }
        }
        catch { }
        return false;
    }

    private bool ContainsQueryInElement(UIElement element, string query)
    {
        if (element is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
        {
            if (tb.Text.ToLowerInvariant().Contains(query))
                return true;
        }
        else if (element is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (ContainsQueryInElement(child, query))
                    return true;
            }
        }
        else if (element is Border b && b.Child != null)
        {
            return ContainsQueryInElement(b.Child, query);
        }
        return false;
    }

    private void OnLaunchModuleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string moduleTag)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.1f, 300);
            var mainPage = GetMainPage();
            if (mainPage != null)
            {
                mainPage.NavigateToPageExternal(moduleTag);
            }
        }
    }

    private void OnLaunchWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            FluidAnimationHelper.ApplyGlowSparkBurst(btn, 1.1f, 300);
            try
            {
                WinCarePro.Modules.DesktopWidget.DesktopWidgetWindow.ShowWindow();
            }
            catch { }
        }
    }

    private MainPage? GetMainPage()
    {
        if (App.MainWindowInstance is MainWindow mw && mw.Content is Grid rootGrid)
        {
            if (rootGrid.FindName("RootFrame") is Frame frame && frame.Content is MainPage mp)
            {
                return mp;
            }
        }
        return null;
    }
}

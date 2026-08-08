using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCarePro.Services;

namespace WinCarePro.Views;

public sealed partial class UserGuideView : UserControl
{
    private long _visibilityToken;

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
}

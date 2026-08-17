using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Services;

public enum AppLanguage
{
    English = 0,
    Vietnamese = 1
}

public partial class TranslationManager
{
    private static TranslationManager? _instance;
    public static TranslationManager Instance => _instance ??= new TranslationManager();

    private AppLanguage _currentLanguage = AppLanguage.English;
    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? LanguageChanged;

    private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _reverseTranslations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<string, string>> OriginalValues = new();
    private static readonly ConditionalWeakTable<DependencyObject, object> RegisteredControlsMap = new();
    private static readonly object DummyValue = new();

    private TranslationManager()
    {
        InitializeTranslations();
        InitializeUserGuideTranslations();
        BuildReverseTranslations();
        LoadLanguageFromSettings();
    }

    public void LoadLanguageFromSettings()
    {
        try
        {
            int index = WinCarePro.Services.Implementations.SettingsService.Instance.CurrentSettings.LanguageIndex;
            CurrentLanguage = index == 1 ? AppLanguage.Vietnamese : AppLanguage.English;
        }
        catch { }
    }

    private static string PreserveWhitespace(string original, string newText)
    {
        if (!string.IsNullOrEmpty(original) && (original.StartsWith(" ") || original.EndsWith(" ")))
        {
            int leading = original.Length - original.TrimStart().Length;
            int trailing = original.Length - original.TrimEnd().Length;
            return new string(' ', leading) + newText + new string(' ', trailing);
        }
        return newText;
    }

    public string T(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return GetTranslationForLanguage(text, CurrentLanguage);
    }

    public string GetTranslationForLanguage(string key, AppLanguage language)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        string trimmed = key.Trim();

        if (language == AppLanguage.English)
        {
            if (_translations.ContainsKey(trimmed)) return key;
            if (_reverseTranslations.TryGetValue(trimmed, out var englishKey))
            {
                return PreserveWhitespace(key, englishKey);
            }
            return key;
        }
        else // Vietnamese
        {
            if (_translations.TryGetValue(trimmed, out string? translated))
            {
                return PreserveWhitespace(key, translated);
            }

            // Dynamic Regex translation for storage sustainability
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Drive ([A-Z]): usage is at (\d+)%\. Estimated storage sustainability is over (\d+) days\.$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string res = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^Drive ([A-Z]): usage is at (\d+)%\. Estimated storage sustainability is over (\d+) days\.$", "Ổ $1: đang sử dụng $2%. Ước tính dung lượng bền vững hơn $3 ngày.", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for RAM Boosted logs
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^RAM Boosted: Optimized (\d+) processes, freed (\d+) bytes$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string res = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^RAM Boosted: Optimized (\d+) processes, freed (\d+) bytes$", "Giải phóng RAM: Đã tối ưu $1 tiến trình, giải phóng $2 bytes", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for Cleaned bytes logs
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Cleaned (\d+) bytes$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string res = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^Cleaned (\d+) bytes$", "Đã dọn dẹp $1 bytes", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for System updated version logs
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^System updated to version (.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string res = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^System updated to version (.+)$", "Hệ thống đã cập nhật lên phiên bản $1", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for Trạng thái: ... Condition
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Trạng thái:\s*Fair Condition$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return PreserveWhitespace(key, "Trạng thái: Khá");
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Trạng thái:\s*Good Condition$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return PreserveWhitespace(key, "Trạng thái: Tốt");
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Trạng thái:\s*Excellent Condition$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return PreserveWhitespace(key, "Trạng thái: Tuyệt vời");
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^Trạng thái:\s*Critical Condition$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return PreserveWhitespace(key, "Trạng thái: Cảnh báo");

            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^AI detected (\d+) active background processes\. Disabling unnecessary startup items can shave up to ([\d\.,]+) seconds off boot time\.$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                string res = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^AI detected (\d+) active background processes\. Disabling unnecessary startup items can shave up to ([\d\.,]+) seconds off boot time\.$", "AI phát hiện $1 tiến trình ngầm đang hoạt động. Tắt các mục khởi động không cần thiết có thể rút ngắn tới $2 giây boot.", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return PreserveWhitespace(key, res);
            }

            return key;
        }
    }

    private static string GetOriginalValue(DependencyObject obj, string propertyName, string currentValue)
    {
        if (!OriginalValues.TryGetValue(obj, out var dict))
        {
            dict = new Dictionary<string, string>();
            OriginalValues.Add(obj, dict);
        }

        string trimmedCandidate = currentValue?.Trim() ?? string.Empty;

        // Check if we already have a recorded original value
        if (dict.TryGetValue(propertyName, out var original))
        {
            string translationVi = Instance.GetTranslationForLanguage(original, AppLanguage.Vietnamese);
            
            bool isSame = string.Equals(trimmedCandidate, original, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(trimmedCandidate, translationVi, StringComparison.OrdinalIgnoreCase);
            
            if (isSame)
            {
                return original;
            }
        }

        string originalCandidate = trimmedCandidate;
        if (Instance._reverseTranslations.TryGetValue(trimmedCandidate, out var revKey))
        {
            originalCandidate = revKey;
        }

        if (!string.IsNullOrEmpty(currentValue) && (currentValue.StartsWith(" ") || currentValue.EndsWith(" ")))
        {
            int leading = currentValue.Length - currentValue.TrimStart().Length;
            int trailing = currentValue.Length - currentValue.TrimEnd().Length;
            originalCandidate = new string(' ', leading) + originalCandidate + new string(' ', trailing);
        }

        dict[propertyName] = originalCandidate;
        return originalCandidate;
    }

    public void BuildReverseTranslations()
    {
        lock (_translations)
        {
            _reverseTranslations.Clear();
            foreach (var kvp in _translations)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    _reverseTranslations[kvp.Value] = kvp.Key;
                }
            }
        }
    }

    private bool ShouldTranslate(DependencyObject? obj, string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        if (obj != null && OriginalValues.TryGetValue(obj, out var dict) && dict.Count > 0)
        {
            return true;
        }

        string trimmed = text.Trim();
        return _translations.ContainsKey(trimmed) || _reverseTranslations.ContainsKey(trimmed);
    }

    private bool ShouldTranslate(string? text)
    {
        return ShouldTranslate(null, text);
    }

    private readonly List<WeakReference<DependencyObject>> _registeredControls = new();

    private void RegisterControl(DependencyObject control)
    {
        lock (_registeredControls)
        {
            if (!RegisteredControlsMap.TryGetValue(control, out _))
            {
                _registeredControls.Add(new WeakReference<DependencyObject>(control));
                RegisteredControlsMap.Add(control, DummyValue);
            }
        }
    }

    public void ApplyLanguageChange()
    {
        // First, translate all currently registered controls
        lock (_registeredControls)
        {
            for (int i = _registeredControls.Count - 1; i >= 0; i--)
            {
                if (_registeredControls[i].TryGetTarget(out var control))
                {
                    TranslateSingleControl(control);
                }
                else
                {
                    _registeredControls.RemoveAt(i);
                }
            }
        }

        // Next, execute a visual tree walk on the active window content (if available)
        // to catch any unregistered, dynamic, or newly added elements.
        try
        {
            if (WinCarePro.App.MainWindowInstance != null)
            {
                Translate(WinCarePro.App.MainWindowInstance.Content);
            }
        }
        catch { }
    }

    public void TranslateSingleControl(DependencyObject parent)
    {
        bool translated = false;

        if (parent is TextBlock tb)
        {
            try
            {
                if (tb.Inlines != null && tb.Inlines.Count > 0)
                {
                    foreach (var inline in tb.Inlines)
                    {
                        if (inline is Run r && ShouldTranslate(r, r.Text))
                        {
                            string originalR = GetOriginalValue(r, "Text", r.Text);
                            r.Text = T(originalR);
                            translated = true;
                        }
                    }
                }
            }
            catch { }

            if (ShouldTranslate(tb, tb.Text))
            {
                string original = GetOriginalValue(tb, "Text", tb.Text);
                tb.Text = T(original);
                translated = true;
            }
        }
        else if (parent is Run run)
        {
            if (ShouldTranslate(run.Text))
            {
                string original = GetOriginalValue(run, "Text", run.Text);
                run.Text = T(original);
                translated = true;
            }
        }
        else if (parent is ContentControl cc && cc.Content is string ccContent)
        {
            if (ShouldTranslate(ccContent))
            {
                string original = GetOriginalValue(cc, "Content", ccContent);
                cc.Content = T(original);
                translated = true;
            }
        }
        else if (parent is HyperlinkButton hb && hb.Content is string hbContent)
        {
            if (ShouldTranslate(hbContent))
            {
                string original = GetOriginalValue(hb, "Content", hbContent);
                hb.Content = T(original);
                translated = true;
            }
        }
        else if (parent is CheckBox cb && cb.Content is string cbContent)
        {
            if (ShouldTranslate(cbContent))
            {
                string original = GetOriginalValue(cb, "Content", cbContent);
                cb.Content = T(original);
                translated = true;
            }
        }
        else if (parent is RadioButton rb && rb.Content is string rbContent)
        {
            if (ShouldTranslate(rbContent))
            {
                string original = GetOriginalValue(rb, "Content", rbContent);
                rb.Content = T(original);
                translated = true;
            }
        }
        else if (parent is ToggleSwitch ts)
        {
            if (ts.Header is string headerStr && ShouldTranslate(headerStr))
            {
                string originalHeader = GetOriginalValue(ts, "Header", headerStr);
                ts.Header = T(originalHeader);
                translated = true;
            }
            if (ts.OnContent is string onStr && ShouldTranslate(onStr))
            {
                string originalOn = GetOriginalValue(ts, "OnContent", onStr);
                ts.OnContent = T(originalOn);
                translated = true;
            }
            if (ts.OffContent is string offStr && ShouldTranslate(offStr))
            {
                string originalOff = GetOriginalValue(ts, "OffContent", offStr);
                ts.OffContent = T(originalOff);
                translated = true;
            }
        }
        else if (parent is WinCarePro.Shared.Components.LoadingToggleSwitch lts)
        {
            if (!string.IsNullOrEmpty(lts.HeaderText) && ShouldTranslate(lts.HeaderText))
            {
                string originalHeader = GetOriginalValue(lts, "HeaderText", lts.HeaderText);
                lts.HeaderText = T(originalHeader);
                translated = true;
            }
            if (!string.IsNullOrEmpty(lts.OnContent) && ShouldTranslate(lts.OnContent))
            {
                string originalOn = GetOriginalValue(lts, "OnContent", lts.OnContent);
                lts.OnContent = T(originalOn);
                translated = true;
            }
            if (!string.IsNullOrEmpty(lts.OffContent) && ShouldTranslate(lts.OffContent))
            {
                string originalOff = GetOriginalValue(lts, "OffContent", lts.OffContent);
                lts.OffContent = T(originalOff);
                translated = true;
            }
            if (!string.IsNullOrEmpty(lts.LoadingText) && ShouldTranslate(lts.LoadingText))
            {
                string originalLoading = GetOriginalValue(lts, "LoadingText", lts.LoadingText);
                lts.LoadingText = T(originalLoading);
                translated = true;
            }
        }
        else if (parent is TextBox txt)
        {
            if (!string.IsNullOrEmpty(txt.PlaceholderText) && ShouldTranslate(txt.PlaceholderText))
            {
                string originalPlaceholder = GetOriginalValue(txt, "PlaceholderText", txt.PlaceholderText);
                txt.PlaceholderText = T(originalPlaceholder);
                translated = true;
            }
            if (txt.Header is string headerStr && ShouldTranslate(headerStr))
            {
                string originalHeader = GetOriginalValue(txt, "Header", headerStr);
                txt.Header = T(originalHeader);
                translated = true;
            }
        }
        else if (parent is PasswordBox pwb)
        {
            if (!string.IsNullOrEmpty(pwb.PlaceholderText) && ShouldTranslate(pwb.PlaceholderText))
            {
                string originalPlaceholder = GetOriginalValue(pwb, "PlaceholderText", pwb.PlaceholderText);
                pwb.PlaceholderText = T(originalPlaceholder);
                translated = true;
            }
            if (pwb.Header is string headerStr && ShouldTranslate(headerStr))
            {
                string originalHeader = GetOriginalValue(pwb, "Header", headerStr);
                pwb.Header = T(originalHeader);
                translated = true;
            }
        }
        else if (parent is AutoSuggestBox asb)
        {
            if (!string.IsNullOrEmpty(asb.PlaceholderText) && ShouldTranslate(asb.PlaceholderText))
            {
                string originalPlaceholder = GetOriginalValue(asb, "PlaceholderText", asb.PlaceholderText);
                asb.PlaceholderText = T(originalPlaceholder);
                translated = true;
            }
            if (asb.Header is string headerStr && ShouldTranslate(headerStr))
            {
                string originalHeader = GetOriginalValue(asb, "Header", headerStr);
                asb.Header = T(originalHeader);
                translated = true;
            }
        }
        else if (parent is ComboBoxItem cbi && cbi.Content is string cbiContent)
        {
            if (ShouldTranslate(cbiContent))
            {
                string original = GetOriginalValue(cbi, "Content", cbiContent);
                cbi.Content = T(original);
                translated = true;
            }
        }
        else if (parent is ComboBox cbx)
        {
            if (cbx.Header is string headerStr && ShouldTranslate(headerStr))
            {
                string originalHeader = GetOriginalValue(cbx, "Header", headerStr);
                cbx.Header = T(originalHeader);
                translated = true;
            }
            foreach (var item in cbx.Items)
            {
                if (item is ComboBoxItem combi && combi.Content is string combiContent)
                {
                    if (ShouldTranslate(combiContent))
                    {
                        string originalCombi = GetOriginalValue(combi, "Content", combiContent);
                        combi.Content = T(originalCombi);
                        translated = true;
                    }
                }
            }
        }
        else if (parent is ListViewItem lvi && lvi.Content is string lviContent)
        {
            if (ShouldTranslate(lviContent))
            {
                string original = GetOriginalValue(lvi, "Content", lviContent);
                lvi.Content = T(original);
                translated = true;
            }
        }
        else if (parent is PivotItem pi)
        {
            if (pi.Header is string piHeader && ShouldTranslate(piHeader))
            {
                string original = GetOriginalValue(pi, "Header", piHeader);
                pi.Header = T(original);
                translated = true;
            }
        }
        else if (parent is NavigationView nv)
        {
            if (nv.SettingsItem is NavigationViewItem settingsItem && settingsItem.Content is string settingsContent)
            {
                if (ShouldTranslate(settingsContent))
                {
                    string original = GetOriginalValue(settingsItem, "Content", settingsContent);
                    settingsItem.Content = T(original);
                    translated = true;
                }
            }
        }
        else if (parent is NavigationViewItem nvi)
        {
            if (nvi.Content is string nviContent && ShouldTranslate(nviContent))
            {
                string original = GetOriginalValue(nvi, "Content", nviContent);
                nvi.Content = T(original);
                translated = true;
            }
        }
        else if (parent is NavigationViewItemHeader nvih)
        {
            if (nvih.Content is string nvihContent && ShouldTranslate(nvihContent))
            {
                string original = GetOriginalValue(nvih, "Content", nvihContent);
                nvih.Content = T(original);
                translated = true;
            }
        }
        else if (parent is MenuFlyout mf)
        {
            foreach (var item in mf.Items)
            {
                Translate(item);
            }
        }
        else if (parent is MenuFlyoutItem mfi)
        {
            if (!string.IsNullOrEmpty(mfi.Text) && ShouldTranslate(mfi.Text))
            {
                string original = GetOriginalValue(mfi, "Text", mfi.Text);
                mfi.Text = T(original);
                translated = true;
            }
        }
        else if (parent is MenuFlyoutSubItem mfsi)
        {
            if (!string.IsNullOrEmpty(mfsi.Text) && ShouldTranslate(mfsi.Text))
            {
                string original = GetOriginalValue(mfsi, "Text", mfsi.Text);
                mfsi.Text = T(original);
                translated = true;
            }
        }
        else if (parent is ContentDialog cd)
        {
            if (cd.Title is string titleStr && ShouldTranslate(titleStr))
            {
                string originalTitle = GetOriginalValue(cd, "Title", titleStr);
                cd.Title = T(originalTitle);
                translated = true;
            }
            if (!string.IsNullOrEmpty(cd.PrimaryButtonText) && ShouldTranslate(cd.PrimaryButtonText))
            {
                string originalPrimary = GetOriginalValue(cd, "PrimaryButtonText", cd.PrimaryButtonText);
                cd.PrimaryButtonText = T(originalPrimary);
                translated = true;
            }
            if (!string.IsNullOrEmpty(cd.SecondaryButtonText) && ShouldTranslate(cd.SecondaryButtonText))
            {
                string originalSecondary = GetOriginalValue(cd, "SecondaryButtonText", cd.SecondaryButtonText);
                cd.SecondaryButtonText = T(originalSecondary);
                translated = true;
            }
            if (!string.IsNullOrEmpty(cd.CloseButtonText) && ShouldTranslate(cd.CloseButtonText))
            {
                string originalClose = GetOriginalValue(cd, "CloseButtonText", cd.CloseButtonText);
                cd.CloseButtonText = T(originalClose);
                translated = true;
            }
        }
        else if (parent is TeachingTip tt)
        {
            if (!string.IsNullOrEmpty(tt.Title) && ShouldTranslate(tt.Title))
            {
                string originalTitle = GetOriginalValue(tt, "Title", tt.Title);
                tt.Title = T(originalTitle);
                translated = true;
            }
            if (!string.IsNullOrEmpty(tt.Subtitle) && ShouldTranslate(tt.Subtitle))
            {
                string originalSub = GetOriginalValue(tt, "Subtitle", tt.Subtitle);
                tt.Subtitle = T(originalSub);
                translated = true;
            }
            if (tt.ActionButtonContent is string actionStr && ShouldTranslate(actionStr))
            {
                string originalAction = GetOriginalValue(tt, "ActionButtonContent", actionStr);
                tt.ActionButtonContent = T(originalAction);
                translated = true;
            }
            if (tt.CloseButtonContent is string closeStr && ShouldTranslate(closeStr))
            {
                string originalClose = GetOriginalValue(tt, "CloseButtonContent", closeStr);
                tt.CloseButtonContent = T(originalClose);
                translated = true;
            }
        }
        else if (parent is InfoBar ib)
        {
            if (!string.IsNullOrEmpty(ib.Title) && ShouldTranslate(ib.Title))
            {
                string originalTitle = GetOriginalValue(ib, "Title", ib.Title);
                ib.Title = T(originalTitle);
                translated = true;
            }
            if (!string.IsNullOrEmpty(ib.Message) && ShouldTranslate(ib.Message))
            {
                string originalMsg = GetOriginalValue(ib, "Message", ib.Message);
                ib.Message = T(originalMsg);
                translated = true;
            }
        }
        else if (parent is Pivot pivot)
        {
            if (!RegisteredControlsMap.TryGetValue(pivot, out _))
            {
                pivot.SelectionChanged += (s, e) =>
                {
                    pivot.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        if (pivot.SelectedItem is DependencyObject selectedDep)
                        {
                            Translate(selectedDep);
                        }
                    });
                };
            }
        }
        else if (parent is Expander exp)
        {
            if (exp.Header is string expHeader && ShouldTranslate(expHeader))
            {
                string originalHeader = GetOriginalValue(exp, "Header", expHeader);
                exp.Header = T(originalHeader);
                translated = true;
            }
        }

        // Support ToolTip
        if (parent is DependencyObject dobj)
        {
            var toolTipValue = ToolTipService.GetToolTip(dobj);
            if (toolTipValue is string toolTipStr && ShouldTranslate(toolTipStr))
            {
                string originalToolTip = GetOriginalValue(dobj, "ToolTip", toolTipStr);
                ToolTipService.SetToolTip(dobj, T(originalToolTip));
                translated = true;
            }
        }

        if (translated)
        {
            RegisterControl(parent);
        }
    }

    public void Translate(DependencyObject? parent)
    {
        if (parent == null) return;
        TranslateInternal(parent, new HashSet<DependencyObject>());
    }

    private void TranslateInternal(DependencyObject? parent, HashSet<DependencyObject> visited)
    {
        if (parent == null || !visited.Add(parent)) return;

        TranslateSingleControl(parent);

        // Translate ContextFlyout if present
        if (parent is UIElement ui && ui.ContextFlyout != null)
        {
            TranslateInternal(ui.ContextFlyout, visited);
        }

        int count = 0;
        try
        {
            count = VisualTreeHelper.GetChildrenCount(parent);
        }
        catch { }

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child != null)
                    {
                        TranslateInternal(child, visited);
                    }
                }
                catch { }
            }
        }
        else
        {
            // Logical Tree Traversal fallback for WinUI 3 elements not yet realized in Visual Tree
            try
            {
                if (parent is ContentControl cc && cc.Content is DependencyObject ccDep)
                {
                    TranslateInternal(ccDep, visited);
                }
                else if (parent is Border border && border.Child != null)
                {
                    TranslateInternal(border.Child, visited);
                }
                else if (parent is Panel panel && panel.Children != null)
                {
                    foreach (var child in panel.Children)
                    {
                        TranslateInternal(child, visited);
                    }
                }
                else if (parent is Pivot pivot && pivot.Items != null)
                {
                    foreach (var item in pivot.Items)
                    {
                        if (item is DependencyObject depItem)
                        {
                            TranslateInternal(depItem, visited);
                        }
                    }
                }
                else if (parent is PivotItem pi && pi.Content is DependencyObject piDep)
                {
                    TranslateInternal(piDep, visited);
                }
                else if (parent is UserControl uc && uc.Content is DependencyObject ucDep)
                {
                    TranslateInternal(ucDep, visited);
                }
                else if (parent is Viewbox vb && vb.Child != null)
                {
                    TranslateInternal(vb.Child, visited);
                }
            }
            catch { }
        }
    }

}

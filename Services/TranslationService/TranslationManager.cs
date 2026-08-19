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
                ApplyLanguageChange();
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

    private static readonly System.Text.RegularExpressions.Regex DriveUsageRegex = new(@"^Drive ([A-Z]): usage is at (\d+)%\. Estimated storage sustainability is over (\d+) days\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex DriveUsageHealthyRegex = new(@"^Drive ([A-Z]): usage is healthy at (\d+)% \(([\d\.,]+)\s*GB free\)\. Storage sustainability is over (\d+) days\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex DriveUsageCritRegex = new(@"^Drive ([A-Z]): has critically low space \(([\d\.,]+)\s*GB free,\s*(\d+)% used\)\. AI recommends immediate disk cleanup\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex DriveUsageCapRegex = new(@"^Drive ([A-Z]): is at (\d+)% capacity \(([\d\.,]+)\s*GB free\)\. Consider freeing up large files\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex RamBoostedRegex = new(@"^RAM Boosted: Optimized (\d+) processes, freed (\d+) bytes$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex CleanedBytesRegex = new(@"^Cleaned (\d+) bytes$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex SystemUpdatedRegex = new(@"^System updated to version (.+)$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex AiProcessesRegex = new(@"^AI detected (\d+) active background processes\. Disabling unnecessary startup items can (?:shave up to ([\d\.,]+) seconds off|improve) boot time\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex BackgroundProcessesActiveRegex = new(@"^There are (\d+) background processes active\. System is operating normally\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex UninstallingAppRegex = new(@"^Uninstalling\s+(?:app:\s*)?(.+?)(?:\.\.\.|…)?$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex CleanedDirsRegex = new(@"^Cleaned (\d+) empty directories under (.+)$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex GamingTurboActiveRegex = new(@"^🚀 Gaming Turbo ACTIVE! Freed ([\d\.,]+) MB RAM across (\d+) processes\.$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex PresetRegex = new(@"^Preset:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex BootSavingsRegex = new(@"^-([\d\.,]+)s\s+Boot Time$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex DaysLeftRegex = new(@"^(\d+)\s+Days Left$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static string PreserveWhitespace(string original, string newText)
    {
        if (string.IsNullOrEmpty(original) || (!original.StartsWith(' ') && !original.EndsWith(' ')))
        {
            return newText;
        }
        int leading = original.Length - original.TrimStart().Length;
        int trailing = original.Length - original.TrimEnd().Length;
        return new string(' ', leading) + newText + new string(' ', trailing);
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
        string normalizedLf = trimmed.Replace("\r\n", "\n");
        string normalizedCrlf = trimmed.Replace("\r\n", "\n").Replace("\n", "\r\n");

        if (language == AppLanguage.English)
        {
            if (_translations.ContainsKey(trimmed) || _translations.ContainsKey(normalizedLf) || _translations.ContainsKey(normalizedCrlf)) return key;
            if (_reverseTranslations.TryGetValue(trimmed, out var englishKey) ||
                _reverseTranslations.TryGetValue(normalizedLf, out englishKey) ||
                _reverseTranslations.TryGetValue(normalizedCrlf, out englishKey))
            {
                return PreserveWhitespace(key, englishKey);
            }
            return key;
        }
        else // Vietnamese
        {
            if (_translations.TryGetValue(trimmed, out string? translated) ||
                _translations.TryGetValue(normalizedLf, out translated) ||
                _translations.TryGetValue(normalizedCrlf, out translated))
            {
                return PreserveWhitespace(key, translated);
            }

            // Dynamic Regex translation for storage sustainability
            if (DriveUsageHealthyRegex.IsMatch(trimmed))
            {
                string res = DriveUsageHealthyRegex.Replace(trimmed, "Ổ $1: mức sử dụng tốt ở mức $2% ($3 GB trống). Độ bền dung lượng ước tính trên $4 ngày.");
                return PreserveWhitespace(key, res);
            }

            if (DriveUsageCritRegex.IsMatch(trimmed))
            {
                string res = DriveUsageCritRegex.Replace(trimmed, "Ổ $1: dung lượng cực thấp ($2 GB trống, đã dùng $3%). AI khuyến nghị dọn dẹp ổ đĩa ngay.");
                return PreserveWhitespace(key, res);
            }

            if (DriveUsageCapRegex.IsMatch(trimmed))
            {
                string res = DriveUsageCapRegex.Replace(trimmed, "Ổ $1: đang ở mức $2% dung lượng ($3 GB trống). Hãy cân nhắc giải phóng các tệp lớn.");
                return PreserveWhitespace(key, res);
            }

            if (DriveUsageRegex.IsMatch(trimmed))
            {
                string res = DriveUsageRegex.Replace(trimmed, "Ổ $1: đang sử dụng $2%. Ước tính dung lượng bền vững hơn $3 ngày.");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for RAM Boosted logs
            if (RamBoostedRegex.IsMatch(trimmed))
            {
                string res = RamBoostedRegex.Replace(trimmed, "Giải phóng RAM: Đã tối ưu $1 tiến trình, giải phóng $2 bytes");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for Cleaned bytes logs
            if (CleanedBytesRegex.IsMatch(trimmed))
            {
                string res = CleanedBytesRegex.Replace(trimmed, "Đã dọn dẹp $1 bytes");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for System updated version logs
            if (SystemUpdatedRegex.IsMatch(trimmed))
            {
                string res = SystemUpdatedRegex.Replace(trimmed, "Hệ thống đã cập nhật lên phiên bản $1");
                return PreserveWhitespace(key, res);
            }

            // Dynamic translation for Uninstalling app
            if (trimmed.StartsWith("Uninstalling ", StringComparison.OrdinalIgnoreCase))
            {
                string rest = trimmed.Substring(13).Trim();
                if (rest.StartsWith("app:", StringComparison.OrdinalIgnoreCase))
                {
                    rest = rest.Substring(4).Trim();
                }
                rest = rest.TrimEnd('.', '…').Trim();
                return PreserveWhitespace(key, $"Đang gỡ cài đặt ứng dụng: {rest}...");
            }

            // Dynamic Regex translation for Cleaned empty directories
            if (CleanedDirsRegex.IsMatch(trimmed))
            {
                string res = CleanedDirsRegex.Replace(trimmed, "Đã dọn dẹp $1 thư mục rỗng trong $2");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex translation for Gaming Turbo Active
            if (GamingTurboActiveRegex.IsMatch(trimmed))
            {
                string res = GamingTurboActiveRegex.Replace(trimmed, "🚀 Gaming Turbo HOẠT ĐỘNG! Đã giải phóng $1 MB RAM trên $2 tiến trình.");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex for Preset chip
            if (PresetRegex.IsMatch(trimmed))
            {
                var match = PresetRegex.Match(trimmed);
                string presetVal = match.Groups[1].Value.Trim();
                string translatedVal = _translations.TryGetValue(presetVal, out var tVal) ? tVal : presetVal;
                return PreserveWhitespace(key, $"Cấu hình: {translatedVal}");
            }

            // Dynamic Regex for Boot Savings
            if (BootSavingsRegex.IsMatch(trimmed))
            {
                string res = BootSavingsRegex.Replace(trimmed, "-$1s Khởi Động");
                return PreserveWhitespace(key, res);
            }

            // Dynamic Regex for Days Left
            if (DaysLeftRegex.IsMatch(trimmed))
            {
                string res = DaysLeftRegex.Replace(trimmed, "Còn $1 Ngày");
                return PreserveWhitespace(key, res);
            }

            // Fast-path prefix check for Status Condition
            if (trimmed.StartsWith("Trạng thái:", StringComparison.OrdinalIgnoreCase))
            {
                if (trimmed.EndsWith("Fair Condition", StringComparison.OrdinalIgnoreCase)) return PreserveWhitespace(key, "Trạng thái: Khá");
                if (trimmed.EndsWith("Good Condition", StringComparison.OrdinalIgnoreCase)) return PreserveWhitespace(key, "Trạng thái: Tốt");
                if (trimmed.EndsWith("Excellent Condition", StringComparison.OrdinalIgnoreCase)) return PreserveWhitespace(key, "Trạng thái: Tuyệt vời");
                if (trimmed.EndsWith("Critical Condition", StringComparison.OrdinalIgnoreCase)) return PreserveWhitespace(key, "Trạng thái: Cảnh báo");
            }

            if (AiProcessesRegex.IsMatch(trimmed))
            {
                string res = AiProcessesRegex.Replace(trimmed, "AI phát hiện $1 tiến trình nền đang hoạt động. Tắt các mục khởi động không cần thiết có thể cải thiện thời gian khởi động.");
                return PreserveWhitespace(key, res);
            }

            if (BackgroundProcessesActiveRegex.IsMatch(trimmed))
            {
                string res = BackgroundProcessesActiveRegex.Replace(trimmed, "Có $1 tiến trình nền đang hoạt động. Hệ thống đang vận hành bình thường.");
                return PreserveWhitespace(key, res);
            }

            // Fallback for multiline blocks: translate each line individually
            if (trimmed.Contains('\n'))
            {
                var lines = trimmed.Split('\n');
                bool anyTranslated = false;
                var translatedLines = new List<string>(lines.Length);
                foreach (var line in lines)
                {
                    string rawLine = line.TrimEnd('\r');
                    string tLine = GetTranslationForLanguage(rawLine, language);
                    if (!string.Equals(tLine, rawLine, StringComparison.Ordinal))
                    {
                        anyTranslated = true;
                    }
                    translatedLines.Add(tLine);
                }
                if (anyTranslated)
                {
                    string joiner = trimmed.Contains("\r\n") ? "\r\n" : "\n";
                    return PreserveWhitespace(key, string.Join(joiner, translatedLines));
                }
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
        if (_translations.ContainsKey(trimmed) || _reverseTranslations.ContainsKey(trimmed))
            return true;

        if (DriveUsageHealthyRegex.IsMatch(trimmed) ||
            DriveUsageCritRegex.IsMatch(trimmed) ||
            DriveUsageCapRegex.IsMatch(trimmed) ||
            DriveUsageRegex.IsMatch(trimmed) ||
            RamBoostedRegex.IsMatch(trimmed) ||
            CleanedBytesRegex.IsMatch(trimmed) ||
            SystemUpdatedRegex.IsMatch(trimmed) ||
            CleanedDirsRegex.IsMatch(trimmed) ||
            GamingTurboActiveRegex.IsMatch(trimmed) ||
            PresetRegex.IsMatch(trimmed) ||
            BootSavingsRegex.IsMatch(trimmed) ||
            DaysLeftRegex.IsMatch(trimmed) ||
            AiProcessesRegex.IsMatch(trimmed) ||
            BackgroundProcessesActiveRegex.IsMatch(trimmed) ||
            trimmed.StartsWith("Drive ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Uninstalling ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Trạng thái:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Preset:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("AI detected", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool ShouldTranslate(string? text)
    {
        return ShouldTranslate(null, text);
    }

    private readonly List<WeakReference<DependencyObject>> _registeredControls = new();
    private readonly List<WeakReference<Window>> _registeredWindows = new();
    private readonly List<WeakReference<Page>> _registeredPages = new();

    public void RegisterWindow(Window window)
    {
        if (window == null) return;
        lock (_registeredWindows)
        {
            _registeredWindows.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == window);
            _registeredWindows.Add(new WeakReference<Window>(window));
        }

        if (window.Content != null)
        {
            Translate(window.Content);
        }
    }

    public void UnregisterWindow(Window window)
    {
        if (window == null) return;
        lock (_registeredWindows)
        {
            _registeredWindows.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == window);
        }
    }

    public void RegisterPage(Page page)
    {
        if (page == null) return;
        lock (_registeredPages)
        {
            _registeredPages.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == page);
            _registeredPages.Add(new WeakReference<Page>(page));
        }

        Translate(page);
    }

    public void UnregisterPage(Page page)
    {
        if (page == null) return;
        lock (_registeredPages)
        {
            _registeredPages.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == page);
        }
    }

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
        // 1. Translate all individually tracked controls
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

        // 2. Dispatch translation across all registered windows
        lock (_registeredWindows)
        {
            for (int i = _registeredWindows.Count - 1; i >= 0; i--)
            {
                if (_registeredWindows[i].TryGetTarget(out var win))
                {
                    try
                    {
                        win.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                        {
                            if (win.Content != null)
                            {
                                Translate(win.Content);
                            }
                        });
                    }
                    catch { }
                }
                else
                {
                    _registeredWindows.RemoveAt(i);
                }
            }
        }

        // 3. Dispatch translation across all active pages
        lock (_registeredPages)
        {
            for (int i = _registeredPages.Count - 1; i >= 0; i--)
            {
                if (_registeredPages[i].TryGetTarget(out var page))
                {
                    try
                    {
                        page.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                        {
                            Translate(page);
                        });
                    }
                    catch { }
                }
                else
                {
                    _registeredPages.RemoveAt(i);
                }
            }
        }

        // 4. Fallback for main window instance
        try
        {
            if (WinCarePro.App.MainWindowInstance != null && WinCarePro.App.MainWindowInstance.Content != null)
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

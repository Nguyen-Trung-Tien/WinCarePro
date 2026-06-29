using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinCarePro.Services;

public enum AppLanguage
{
    English = 0,
    Vietnamese = 1
}

public class TranslationManager
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
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<string, string>> OriginalValues = new();

    private TranslationManager()
    {
        InitializeTranslations();
        LoadLanguageFromSettings();
    }

    public void LoadLanguageFromSettings()
    {
        try
        {
            string raw = Database.DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("LanguageIndex", out var langProp))
                {
                    int index = langProp.GetInt32();
                    CurrentLanguage = index == 1 ? AppLanguage.Vietnamese : AppLanguage.English;
                }
            }
        }
        catch { }
    }

    public string T(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (CurrentLanguage == AppLanguage.English) return text;

        string trimmed = text.Trim();
        if (_translations.TryGetValue(trimmed, out string? translated))
        {
            // Preserve leading/trailing whitespace if present in original
            string result = text;
            if (text.StartsWith(" ") || text.EndsWith(" "))
            {
                int leading = text.Length - text.TrimStart().Length;
                int trailing = text.Length - text.TrimEnd().Length;
                result = new string(' ', leading) + translated + new string(' ', trailing);
            }
            else
            {
                result = translated;
            }
            return result;
        }

        return text;
    }

    private static string GetOriginalValue(DependencyObject obj, string propertyName, string currentValue)
    {
        if (!OriginalValues.TryGetValue(obj, out var dict))
        {
            dict = new Dictionary<string, string>();
            OriginalValues.Add(obj, dict);
        }

        if (!dict.TryGetValue(propertyName, out var original))
        {
            string originalCandidate = currentValue ?? string.Empty;
            string trimmedCandidate = originalCandidate.Trim();

            // Reverse map Vietnamese translation to original English key if already translated at initialization
            foreach (var kvp in Instance._translations)
            {
                if (string.Equals(kvp.Value, trimmedCandidate, StringComparison.OrdinalIgnoreCase))
                {
                    if (originalCandidate.StartsWith(" ") || originalCandidate.EndsWith(" "))
                    {
                        int leading = originalCandidate.Length - originalCandidate.TrimStart().Length;
                        int trailing = originalCandidate.Length - originalCandidate.TrimEnd().Length;
                        originalCandidate = new string(' ', leading) + kvp.Key + new string(' ', trailing);
                    }
                    else
                    {
                        originalCandidate = kvp.Key;
                    }
                    break;
                }
            }

            original = originalCandidate;
            dict[propertyName] = original;
        }

        return original;
    }

    private bool ShouldTranslate(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string trimmed = text.Trim();
        
        if (_translations.ContainsKey(trimmed)) return true;
        
        foreach (var val in _translations.Values)
        {
            if (string.Equals(val, trimmed, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    private readonly List<WeakReference<DependencyObject>> _registeredControls = new();

    private void RegisterControl(DependencyObject control)
    {
        lock (_registeredControls)
        {
            foreach (var wr in _registeredControls)
            {
                if (wr.TryGetTarget(out var target) && target == control)
                {
                    return;
                }
            }
            _registeredControls.Add(new WeakReference<DependencyObject>(control));
        }
    }

    public void ApplyLanguageChange()
    {
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
    }

    public void TranslateSingleControl(DependencyObject parent)
    {
        bool translated = false;

        if (parent is TextBlock tb)
        {
            if (ShouldTranslate(tb.Text))
            {
                string original = GetOriginalValue(tb, "Text", tb.Text);
                tb.Text = T(original);
                translated = true;
            }
        }
        else if (parent is Button btn && btn.Content is string btnContent)
        {
            if (ShouldTranslate(btnContent))
            {
                string original = GetOriginalValue(btn, "Content", btnContent);
                btn.Content = T(original);
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

        TranslateSingleControl(parent);

        // Translate ContextFlyout if present
        if (parent is UIElement ui && ui.ContextFlyout != null)
        {
            Translate(ui.ContextFlyout);
        }

        // Translate Items of ItemsControl (ListView items, ComboBox items, etc.)
        if (parent is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is DependencyObject depObj)
                {
                    Translate(depObj);
                }
            }
        }

        // Recurse down visual tree children
        int count = 0;
        try
        {
            count = VisualTreeHelper.GetChildrenCount(parent);
        }
        catch { }

        for (int i = 0; i < count; i++)
        {
            try
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null)
                {
                    Translate(child);
                }
            }
            catch { }
        }
    }

    private void InitializeTranslations()
    {
        // --- Sidebar Navigation Menu Items & App Header ---
        _translations["Dashboard"] = "Trang chủ";
        _translations["Junk Cleaner"] = "Dọn tệp rác";
        _translations["App Uninstaller"] = "Gỡ ứng dụng";
        _translations["Network Center"] = "Trung tâm mạng";
        _translations["System Repair"] = "Sửa lỗi hệ thống";
        _translations["Security Shield"] = "Khiên bảo mật";
        _translations["System Optimizer"] = "Tối ưu hệ thống";
        _translations["Startup & Services"] = "Khởi động & Dịch vụ";
        _translations["Process Manager"] = "Quản lý tiến trình";
        _translations["Disk Tools"] = "Công cụ ổ đĩa";
        _translations["Hardware Center"] = "Thông tin phần cứng";
        _translations["Registry Center"] = "Quản lý Registry";
        _translations["Software Updater"] = "Cập nhật phần mềm";
        _translations["Driver Updater"] = "Cập nhật Driver";
        _translations["CARE & CLEAN"] = "CHĂM SÓC & DỌN DẸP";
        _translations["SYSTEM TUNING"] = "TINH CHỈNH HỆ THỐNG";
        _translations["PC Health Secured"] = "Sức khỏe PC An toàn";
        _translations["Search tools, settings, apps... (Ctrl + F)"] = "Tìm kiếm công cụ, cài đặt, ứng dụng... (Ctrl + F)";
        _translations["Settings & Personalization"] = "Cài đặt & Cá nhân hóa";
        _translations["Settings"] = "Cài đặt";

        // --- System Dashboard Page ---
        _translations["System Dashboard"] = "Bảng điều khiển hệ thống";
        _translations["Real-time performance monitors, PC diagnostics diagnostics, and speed optimizer."] = "Giám sát hiệu suất thời gian thực, chẩn đoán PC và tối ưu tốc độ.";
        _translations["System Health Overview"] = "Tổng quan sức khỏe hệ thống";
        _translations["Health Score"] = "Điểm sức khỏe";
        _translations["A scan verifies diagnostic alerts, junk file volumes, startup overhead, and available security fixes. Optimize now to clean garbage files and improve loading speeds."] = "Quét để kiểm tra các cảnh báo chẩn đoán, dung lượng tệp rác, tiến trình khởi động và các bản sửa lỗi bảo mật. Tối ưu ngay để dọn sạch tệp rác và tăng tốc độ khởi động.";
        _translations["Scan System"] = "Quét hệ thống";
        _translations["Optimize Now"] = "Tối ưu ngay";
        _translations["CPU Processor Load"] = "Tải lượng vi xử lý CPU";
        _translations["Memory (RAM) Footprint"] = "Dung lượng bộ nhớ RAM";
        _translations["Graphics Card (GPU) Load"] = "Tải lượng card đồ họa GPU";
        _translations["System Disk Activity"] = "Hoạt động ổ đĩa hệ thống";
        _translations["Smart AI Advice"] = "Lời khuyên từ AI thông minh";
        _translations["Suggested actions based on scanning diagnostics:"] = "Đề xuất hành động dựa trên chẩn đoán hệ thống:";
        _translations["No recommendations. Run a full scan to evaluate your PC health."] = "Không có đề xuất nào. Vui lòng chạy quét toàn bộ để đánh giá sức khỏe PC.";
        _translations["Diagnostics Discoveries"] = "Phát hiện chẩn đoán";
        _translations["Category"] = "Danh mục";
        _translations["Check Name"] = "Tên kiểm tra";
        _translations["Description"] = "Mô tả";
        _translations["Status"] = "Trạng thái";
        _translations["System Status: Idle"] = "Trạng thái hệ thống: Đang rảnh";
        _translations["Status: Scanning Junk Files..."] = "Trạng thái: Đang quét tệp rác...";
        _translations["Status: Scanning Registry Issues..."] = "Trạng thái: Đang quét lỗi Registry...";
        _translations["Status: Checking Available Software Updates..."] = "Trạng thái: Đang kiểm tra cập nhật phần mềm...";
        _translations["Status: Evaluating Connection and Security Status..."] = "Trạng thái: Đang đánh giá kết nối và bảo mật...";
        _translations["Status: Calculating System Health Index..."] = "Trạng thái: Đang tính toán chỉ số sức khỏe hệ thống...";
        _translations["Ready to scan system"] = "Sẵn sàng quét hệ thống";
        _translations["Status: Optimizing - Cleaning Junk Files..."] = "Trạng thái: Đang tối ưu - Dọn dẹp tệp rác...";
        _translations["Status: Optimizing - Cleaning Windows Update Cache..."] = "Trạng thái: Đang tối ưu - Dọn dẹp bộ nhớ đệm Windows Update...";
        _translations["Status: Optimizing - Repairing Registry Issues..."] = "Trạng thái: Đang tối ưu - Sửa chữa lỗi Registry...";
        _translations["Status: Optimizing - Performing Active RAM Boost..."] = "Trạng thái: Đang tối ưu - Giải phóng bộ nhớ RAM...";
        _translations["Status: Optimizing - Flushing DNS Resolver Cache..."] = "Trạng thái: Đang tối ưu - Xóa bộ nhớ đệm DNS...";
        _translations["Status: Optimizing - Applying Speed & UI Tweaks..."] = "Trạng thái: Đang tối ưu - Áp dụng tinh chỉnh giao diện & tốc độ...";
        _translations["Optimization Complete! System is fully optimized."] = "Tối ưu hóa hoàn tất! Hệ thống đã được tối ưu hóa hoàn toàn.";
        _translations["PC Health Secured (RAM Boosted, Junk Cleaned)"] = "Sức khỏe PC An toàn (Đã tối ưu RAM, dọn tệp rác)";
        _translations["Connected"] = "Đã kết nối";
        _translations["Disconnected"] = "Mất kết nối";
        _translations["Optimized"] = "Đã tối ưu";
        _translations["Available"] = "Có sẵn";
        _translations["Healthy"] = "Khỏe mạnh";
        _translations["Action Required"] = "Cần xử lý";

        // --- Junk Cleaner Page ---
        _translations["Current Junk:"] = "Tệp rác hiện tại:";
        _translations["Safe cleanup files. Empty the recycle bin, Windows installers cache, and directx shaders."] = "Các tệp dọn dẹp an toàn. Làm trống thùng rác, bộ nhớ đệm trình cài đặt Windows và shader directx.";
        _translations["Scan Directories"] = "Quét thư mục";
        _translations["Clean Now"] = "Dọn dẹp ngay";
        _translations["Junk Categories"] = "Danh mục tệp rác";
        _translations["Cleanup Progress"] = "Tiến trình dọn dẹp";
        _translations["Live Operations Trace"] = "Theo dõi hoạt động trực tiếp";
        _translations["Ready to scan junk files"] = "Sẵn sàng quét tệp rác";
        _translations["Scan completed. Select items to clean."] = "Quét hoàn tất. Chọn các mục để dọn dẹp.";
        _translations["Scanning directories..."] = "Đang quét các thư mục...";
        _translations["Cleaning selected directories..."] = "Đang dọn dẹp các thư mục đã chọn...";
        _translations["Cleanup complete. Reclaimed"] = "Dọn dẹp hoàn tất. Đã giải phóng";
        _translations["Windows Temp Files"] = "Tệp tạm thời Windows";
        _translations["Temporary files generated by the Windows OS operating system."] = "Các tệp tạm thời được tạo ra bởi hệ điều hành Windows.";
        _translations["User Temp Files"] = "Tệp tạm thời người dùng";
        _translations["Local application cache files and user account temp data."] = "Các tệp bộ đệm ứng dụng cục bộ và dữ liệu tạm thời của người dùng.";
        _translations["Browser Cache"] = "Bộ nhớ đệm trình duyệt";
        _translations["Cached webpages, images, and offline resources from Microsoft Edge."] = "Trang web, hình ảnh và tài nguyên ngoại tuyến được lưu trong Edge.";
        _translations["System Log Files"] = "Tệp nhật ký hệ thống";
        _translations["Diagnostic logging traces and Windows event log reports."] = "Ghi nhật ký chẩn đoán và báo cáo sự kiện Windows.";
        _translations["Recycle Bin"] = "Thùng rác";
        _translations["Deleted files stored in your Recycle Bin."] = "Các tệp đã xóa được lưu trữ trong Thùng rác của bạn.";
        _translations["Update Installer Cache"] = "Bộ nhớ đệm cài đặt cập nhật";
        _translations["Leftover software update components and cached windows setup files."] = "Thành phần cập nhật phần mềm còn sót lại và tệp cài đặt Windows.";
        _translations["DirectX Shader Cache"] = "Bộ nhớ đệm DirectX Shader";
        _translations["Graphics driver compiled shaders cache for speeding up UI renders."] = "Bộ nhớ đệm đổ bóng được biên dịch để tăng tốc kết xuất giao diện.";
        _translations["Thumbnail Cache"] = "Bộ nhớ đệm hình thu nhỏ";
        _translations["Cached preview image files of system explorer folders."] = "Các tệp ảnh thu nhỏ xem trước được lưu trữ của các thư mục.";
        
        // v3.2 Junk Redesign additions
        _translations["Delivery Optimization Files"] = "Tệp tối ưu hóa phân phối";
        _translations["Cache files used for downloading and sharing Windows updates."] = "Bộ nhớ đệm được sử dụng để tải xuống và chia sẻ cập nhật Windows.";
        _translations["System Prefetch Files"] = "Tệp Prefetch hệ thống";
        _translations["System prefetch cache files created to speed up application startup."] = "Các tệp prefetch hệ thống được tạo ra để tăng tốc khởi động ứng dụng.";
        _translations["System Crash Dumps"] = "Tệp kết xuất sự cố hệ thống";
        _translations["Memory dump files and log traces created when system crash or error occurs."] = "Các tệp kết xuất bộ nhớ và nhật ký sự cố được tạo ra khi hệ thống gặp lỗi hoặc sập nguồn.";
        _translations["Top Largest Files"] = "Các tệp lớn nhất";
        _translations["Open Folder Location"] = "Mở vị trí thư mục";
        _translations["No files found or scanner is idle."] = "Không tìm thấy tệp hoặc trình quét đang rảnh.";
        _translations["Select a category to view its top files details."] = "Chọn một danh mục để xem chi tiết các tệp tin lớn nhất.";
        _translations["Scan system to find junk files."] = "Hãy quét hệ thống để tìm các tệp rác.";
        
        // Locking app additions
        _translations["Running Applications Locking Cache Files"] = "Ứng dụng đang chạy khóa tệp bộ nhớ đệm";
        _translations["Applications in use: {0}. These apps lock cache files and prevent them from being cleaned."] = "Ứng dụng đang mở: {0}. Các ứng dụng này khóa tệp cache và không thể dọn dẹp hoàn toàn.";
        _translations["Close Apps & Re-scan"] = "Đóng ứng dụng & Quét lại";
        _translations["Stopping locking applications..."] = "Đang đóng các ứng dụng khóa tệp...";
        _translations["Closing {0}..."] = "Đang đóng {0}...";
        _translations["Failed to close {0}: {1}"] = "Không thể đóng {0}: {1}";
        _translations["Locked / In Use"] = "Đang bị khóa / Đang sử dụng";
        _translations["Ready to Clean"] = "Sẵn sàng dọn dẹp";
        _translations["Cleanable Space"] = "Dung lượng có thể dọn";
        _translations["Locked"] = "Bị khóa";
        _translations["Running Applications Detected"] = "Phát hiện ứng dụng đang chạy";
        _translations["The following applications are using temporary files:"] = "Các ứng dụng sau đang sử dụng tệp tạm:";
        _translations["Do you want to close these applications to ensure all junk files are cleaned, or clean anyway?"] = "Bạn có muốn đóng các ứng dụng này để đảm bảo toàn bộ tệp rác được dọn dẹp sạch sẽ, hay tiếp tục dọn dẹp mà không đóng?";
        _translations["Close Apps & Clean"] = "Đóng ứng dụng & Dọn dẹp";
        _translations["Clean Anyway"] = "Vẫn dọn dẹp";
        _translations["Clean After Restart"] = "Dọn sau khi khởi động lại";
        _translations["Cancel"] = "Hủy bỏ";
        _translations["Force Close Application"] = "Cưỡng bức đóng ứng dụng";
        _translations["{0} did not close normally. Force close?"] = "{0} không thể đóng bình thường. Cưỡng bức đóng?";
        _translations["Cleanup scheduled successfully for next startup."] = "Đã hẹn lịch dọn dẹp thành công cho lần khởi động máy tiếp theo.";
        _translations["What would you like to do?"] = "Bạn muốn thực hiện hành động nào?";

        // --- App Uninstaller Page ---
        _translations["Search installed applications..."] = "Tìm kiếm ứng dụng đã cài đặt...";
        _translations["Scan Registry Apps"] = "Quét ứng dụng Registry";
        _translations["Uninstall"] = "Gỡ cài đặt";
        _translations["Leftovers Scanner Discovery"] = "Phát hiện tàn dư";
        _translations["Cancel & Back"] = "Hủy & Quay lại";
        _translations["Wipe Leftovers"] = "Xóa sạch tàn dư";
        _translations["Ready to scan applications"] = "Sẵn sàng quét các ứng dụng";
        _translations["Scan completed. Select applications to uninstall."] = "Quét hoàn tất. Hãy chọn ứng dụng để gỡ cài đặt.";
        _translations["Scanning installed application registries..."] = "Đang quét registry ứng dụng đã cài đặt...";
        _translations["Uninstalling applications..."] = "Đang gỡ cài đặt các ứng dụng...";
        _translations["Uninstall complete. Scanning for leftover registry keys and folder directories..."] = "Gỡ cài đặt hoàn tất. Đang quét các khóa registry và thư mục tàn dư...";
        _translations["Leftover analysis completed."] = "Phân tích tàn dư hoàn tất.";
        _translations["Cleaning leftover file system components and registries..."] = "Đang dọn dẹp các tệp và registry tàn dư...";
        _translations["Leftover cleanup completed successfully."] = "Dọn dẹp tàn dư hoàn tất thành công.";

        // v3.0 App Uninstaller Redesign additions
        _translations["Desktop App"] = "Ứng dụng Desktop";
        _translations["Store App"] = "Ứng dụng Store";
        _translations["Total Apps:"] = "Tổng số ứng dụng:";
        _translations["Total Size:"] = "Tổng dung lượng:";
        _translations["Desktop Apps:"] = "Ứng dụng Desktop:";
        _translations["Store Apps:"] = "Ứng dụng Store:";
        _translations["Sort by:"] = "Sắp xếp theo:";
        _translations["Filter:"] = "Bộ lọc:";
        _translations["Sort by Name (A-Z)"] = "Sắp xếp theo Tên (A-Z)";
        _translations["Sort by Name (Z-A)"] = "Sắp xếp theo Tên (Z-A)";
        _translations["Sort by Size (Large-Small)"] = "Sắp xếp theo Dung lượng (Lớn-Nhỏ)";
        _translations["Sort by Size (Small-Large)"] = "Sắp xếp theo Dung lượng (Nhỏ-Lớn)";
        _translations["Sort by Install Date (Newest)"] = "Sắp xếp theo Ngày cài đặt (Mới nhất)";
        _translations["Sort by Install Date (Oldest)"] = "Sắp xếp theo Ngày cài đặt (Cũ nhất)";
        _translations["All Types"] = "Tất cả các loại";
        _translations["Desktop Apps"] = "Ứng dụng Desktop";
        _translations["Store Apps"] = "Ứng dụng Windows Store";
        _translations["App Details"] = "Chi tiết ứng dụng";
        _translations["Version:"] = "Phiên bản:";
        _translations["Publisher:"] = "Nhà xuất bản:";
        _translations["Install Location:"] = "Vị trí cài đặt:";
        _translations["Registry Path:"] = "Đường dẫn Registry:";
        _translations["Uninstall String:"] = "Lệnh gỡ cài đặt:";
        _translations["Open Folder"] = "Mở thư mục";
        _translations["Open Registry"] = "Mở Registry";
        _translations["Search Online"] = "Tìm kiếm Online";
        _translations["Force Uninstall"] = "Gỡ cưỡng bức";
        _translations["Batch Actions"] = "Hành động hàng loạt";
        _translations["Standard Uninstall Selected"] = "Gỡ cài đặt mục đã chọn";
        _translations["Force Uninstall Selected"] = "Gỡ cưỡng bức mục đã chọn";
        _translations["Uninstalling {0}..."] = "Đang gỡ cài đặt {0}...";
        _translations["Uninstalling {0} ({1}/{2})..."] = "Đang gỡ cài đặt {0} ({1}/{2})...";
        _translations["Force uninstalling {0} ({1}/{2})..."] = "Đang gỡ cưỡng bức {0} ({1}/{2})...";
        _translations["Scanning leftovers for {0}..."] = "Đang quét tàn dư cho {0}...";
        _translations["Scanned {0} leftover items."] = "Đã phát hiện {0} tàn dư.";
        _translations["Batch uninstall completed successfully. No leftovers found."] = "Gỡ cài đặt hàng loạt hoàn tất thành công. Không tìm thấy tàn dư.";
        _translations["Cleaned {0} leftover files and registry entries."] = "Đã dọn sạch {0} tệp và khóa registry tàn dư.";
        _translations["Batch uninstallation encountered an error:"] = "Gỡ cài đặt hàng loạt gặp lỗi:";
        _translations["No application selected. Choose an app from the list to view its details."] = "Không có ứng dụng nào được chọn. Hãy chọn một ứng dụng từ danh sách để xem chi tiết.";
        _translations["Error opening folder:"] = "Lỗi khi mở thư mục:";
        _translations["Error opening registry:"] = "Lỗi khi mở registry:";
        _translations["Error searching online:"] = "Lỗi khi tìm kiếm trực tuyến:";
        _translations["Error deleting leftovers:"] = "Lỗi khi xóa tàn dư:";

        // --- Process Manager Page ---
        _translations["System CPU Load"] = "Tải lượng CPU hệ thống";
        _translations["Memory Usage"] = "Sử dụng bộ nhớ";
        _translations["Active Processes"] = "Tiến trình hoạt động";
        _translations["RAM Boost"] = "Tối ưu RAM";
        _translations["Search processes..."] = "Tìm kiếm tiến trình...";
        _translations["Hide System Processes"] = "Ẩn tiến trình hệ thống";
        _translations["High Resource Only"] = "Chỉ hiện ứng dụng tốn tài nguyên";
        _translations["Refresh List"] = "Làm mới danh sách";
        _translations["Process Name"] = "Tên tiến trình";
        _translations["End Task"] = "Kết thúc tác vụ";
        _translations["End Process Tree"] = "Kết thúc cây tiến trình";
        _translations["Open File Location"] = "Mở thư mục tệp";
        _translations["Search Process Online"] = "Tìm kiếm tiến trình trực tuyến";
        _translations["Process ID (PID)"] = "Mã tiến trình (PID)";
        _translations["Executable Path"] = "Đường dẫn tệp thực thi";
        _translations["Command Line Arguments"] = "Tham số dòng lệnh";
        _translations["Active Threads"] = "Luồng hoạt động";
        _translations["Handles Count"] = "Số lượng Handle";
        _translations["Process Start Time"] = "Thời gian khởi chạy";
        _translations["CPU Allocation Priority"] = "Mức độ ưu tiên CPU";
        _translations["Suspend"] = "Tạm dừng";
        _translations["Resume"] = "Tiếp tục";
        _translations["Confirm End Task"] = "Xác nhận kết thúc tác vụ";
        _translations["Are you sure you want to terminate {0}?"] = "Bạn có chắc chắn muốn kết thúc {0}?";
        _translations["Confirm End Process Tree"] = "Xác nhận kết thúc cây tiến trình";
        _translations["Are you sure you want to terminate {0} and all its child processes?"] = "Bạn có chắc chắn muốn kết thúc {0} và tất cả các tiến trình con của nó?";
        _translations["Monitoring {0} active processes."] = "Đang giám sát {0} tiến trình đang chạy.";
        _translations["Terminating process {0} (PID {1})..."] = "Đang kết thúc tiến trình {0} (PID {1})...";
        _translations["Process {0} terminated successfully."] = "Tiến trình {0} đã được kết thúc thành công.";
        _translations["Failed to terminate process {0} (Access Denied or Protected)."] = "Không thể kết thúc tiến trình {0} (Quyền truy cập bị từ chối hoặc Tiến trình được bảo vệ).";
        _translations["Terminating process tree for {0} (PID {1})..."] = "Đang kết thúc cây tiến trình cho {0} (PID {1})...";
        _translations["Process tree for {0} terminated successfully."] = "Cây tiến trình cho {0} đã được kết thúc thành công.";
        _translations["Failed to terminate process tree for {0}."] = "Không thể kết thúc cây tiến trình cho {0}.";
        _translations["Updating priority for PID {0} to {1}..."] = "Đang cập nhật độ ưu tiên cho PID {0} thành {1}...";
        _translations["Process priority updated successfully."] = "Cập nhật độ ưu tiên tiến trình thành công.";
        _translations["Failed to update process priority (Access Denied or Protected)."] = "Không thể cập nhật độ ưu tiên tiến trình (Quyền truy cập bị từ chối hoặc Tiến trình được bảo vệ).";
        _translations["Suspending process PID {0}..."] = "Đang tạm dừng tiến trình PID {0}...";
        _translations["Process suspended successfully."] = "Tiến trình đã được tạm dừng thành công.";
        _translations["Failed to suspend process (Access Denied or Protected)."] = "Không thể tạm dừng tiến trình (Quyền truy cập bị từ chối hoặc Tiến trình được bảo vệ).";
        _translations["Resuming process PID {0}..."] = "Đang tiếp tục tiến trình PID {0}...";
        _translations["Process resumed successfully."] = "Tiến trình đã được tiếp tục thành công.";
        _translations["Failed to resume process (Access Denied or Protected)."] = "Không thể tiếp tục tiến trình (Quyền truy cập bị từ chối hoặc Tiến trình được bảo vệ).";
        _translations["RAM Boost Complete: Freed {0:F1} MB by trimming working sets on {1} apps."] = "Tối ưu RAM hoàn tất: Giải phóng {0:F1} MB bằng cách thu gọn working set của {1} ứng dụng.";

        // --- Network Center Page ---
        _translations["Network Center"] = "Trung tâm mạng";
        _translations["Baud rate network parameters, active TCP ports monitoring, and DNS optimizer."] = "Thông số mạng băng thông, giám sát cổng TCP đang hoạt động và tối ưu hóa DNS.";
        _translations["Active Adapter Specs"] = "Thông số Adapter đang hoạt động";
        _translations["Speed & Bandwidth Test"] = "Kiểm tra tốc độ & Băng thông";
        _translations["Active TCP Port Listeners"] = "Cổng TCP đang lắng nghe";
        _translations["DNS Resolver Settings"] = "Cài đặt bộ phân giải DNS";
        _translations["Adapter Name:"] = "Tên Adapter:";
        _translations["Connection Type:"] = "Loại kết nối:";
        _translations["MAC Address:"] = "Địa chỉ MAC:";
        _translations["IPv4 Address:"] = "Địa chỉ IPv4:";
        _translations["Gateway IP:"] = "IP Cổng kết nối:";
        _translations["DNS Servers:"] = "Máy chủ DNS:";
        _translations["Test Ping Quality"] = "Kiểm tra chất lượng Ping";
        _translations["Flush DNS Cache"] = "Xóa bộ nhớ đệm DNS";
        _translations["Average Latency:"] = "Độ trễ trung bình:";
        _translations["Packet Loss Rate:"] = "Tỷ lệ mất gói tin:";
        _translations["Fast DNS Presets:"] = "Mẫu DNS nhanh:";
        _translations["Apply Custom DNS"] = "Áp dụng DNS tùy chọn";
        _translations["Google Public DNS (8.8.8.8)"] = "Google Public DNS (8.8.8.8)";
        _translations["Cloudflare Secure (1.1.1.1)"] = "Cloudflare Secure (1.1.1.1)";
        _translations["OpenDNS Family (208.67.222.222)"] = "OpenDNS Family (208.67.222.222)";
        _translations["Default ISP Configuration"] = "Cấu hình ISP mặc định";
        _translations["TCP Ports Map"] = "Bản đồ cổng TCP";
        _translations["Process"] = "Tiến trình";
        _translations["Local Address"] = "Địa chỉ cục bộ";
        _translations["Foreign Address"] = "Địa chỉ ngoài";
        _translations["Status Status"] = "Trạng thái";

        // --- System Repair Page ---
        _translations["System File Integrity Check"] = "Kiểm tra tính toàn vẹn tệp hệ thống";
        _translations["Scan and repair damaged Windows system files (SFC & DISM tools)."] = "Quét và sửa chữa các tệp hệ thống Windows bị hỏng (SFC & DISM).";
        _translations["Active System Audits Log"] = "Nhật ký kiểm tra hệ thống hoạt động";
        _translations["Repair Tool options"] = "Tùy chọn công cụ sửa chữa";
        _translations["Execute SFC Integrity Scan"] = "Thực thi quét SFC Integrity";
        _translations["DISM Restore Health Scan"] = "Quét khôi phục sức khỏe DISM";
        _translations["Rebuild Icon & Thumbnail Cache"] = "Dựng lại bộ đệm Icon & ảnh thu nhỏ";
        _translations["Re-Register Windows Store Apps"] = "Đăng ký lại ứng dụng Windows Store";
        _translations["Clear Print Spooler Queue"] = "Xóa hàng đợi lệnh in";
        _translations["Reset Network Sockets (Winsock)"] = "Đặt lại Network Sockets (Winsock)";
        _translations["Ready to execute repair routines"] = "Sẵn sàng thực thi các chương trình sửa lỗi";
        _translations["Starting System File Checker (SFC) scan..."] = "Bắt đầu quét System File Checker (SFC)...";
        _translations["SFC Scan completed. Integrity errors resolved."] = "Quét SFC hoàn tất. Các lỗi toàn vẹn đã được giải quyết.";
        _translations["Starting DISM Component Store Cleanup & Health Restore..."] = "Bắt đầu dọn dẹp Component Store & khôi phục sức khỏe DISM...";
        _translations["DISM Restore Health finished successfully."] = "Khôi phục sức khỏe DISM hoàn tất thành công.";
        _translations["Rebuilding Windows Icon and Thumbnail database cache..."] = "Đang dựng lại bộ nhớ đệm Icon và Thumbnail...";
        _translations["Re-registering all Windows Store applications package definitions..."] = "Đang đăng ký lại tất cả các gói ứng dụng Windows Store...";
        _translations["Stopping Print Spooler, clearing spool queue files, and restarting service..."] = "Đang dừng Print Spooler, xóa hàng đợi và khởi động lại dịch vụ...";
        _translations["Resetting TCP/IP Winsock catalog sockets definitions..."] = "Đang đặt lại định nghĩa TCP/IP Winsock...";

        // --- Security Shield Page ---
        _translations["Security & Privacy Audits"] = "Kiểm tra bảo mật & Quyền riêng tư";
        _translations["Local security configurations, system firewalls status, and telemetry settings tracking."] = "Cấu hình bảo mật cục bộ, trạng thái tường lửa và theo dõi cài đặt đo lường.";
        _translations["Anti-Malware Realtime Shield"] = "Lớp bảo vệ chống mã độc thời gian thực";
        _translations["System Firewall Protection"] = "Bảo vệ bằng tường lửa hệ thống";
        _translations["User Account Control (UAC) Checks"] = "Kiểm tra kiểm soát tài khoản người dùng (UAC)";
        _translations["Telemetry Logging & Privacy Alerts"] = "Nhật ký đo lường & Cảnh báo quyền riêng tư";
        _translations["Active Realtime protection shield monitors for malicious software executions."] = "Lớp bảo vệ thời gian thực giám sát các hành vi chạy phần mềm độc hại.";
        _translations["Local Windows Defender system firewall monitors incoming network threads."] = "Tường lửa Windows Defender giám sát các luồng mạng đi vào.";
        _translations["UAC validates admin permission elevation confirmation dialogue box prompts."] = "UAC xác thực hộp thoại yêu cầu quyền nâng cao của quản trị viên.";
        _translations["Telemetry logs diagnostics reporting data variables to remote cloud infrastructure."] = "Dữ liệu đo lường báo cáo các biến số chẩn đoán về máy chủ đám mây.";
        _translations["System Firewalls Status: Secured"] = "Trạng thái tường lửa: Được bảo vệ";
        _translations["System Firewalls Status: Action Required"] = "Trạng thái tường lửa: Cần xử lý";
        _translations["UAC Level Validation: Secured"] = "Kiểm tra mức UAC: Được bảo vệ";
        _translations["UAC Level Validation: Action Required"] = "Kiểm tra mức UAC: Cần xử lý";
        _translations["Realtime Shield Status: Secured"] = "Trạng thái bảo vệ thời gian thực: Được bảo vệ";
        _translations["Realtime Shield Status: Action Required"] = "Trạng thái bảo vệ thời gian thực: Cần xử lý";
        _translations["Telemetry Logs Status: Telemetry Enabled"] = "Dữ liệu đo lường: Đang bật gửi dữ liệu";
        _translations["Telemetry Logs Status: Secured (Private)"] = "Dữ liệu đo lường: Đã bảo mật (Riêng tư)";
        _translations["Scan Security"] = "Quét bảo mật";
        _translations["Apply Security Fixes"] = "Áp dụng sửa lỗi bảo mật";
        _translations["Ready to perform security audit scan"] = "Sẵn sàng quét kiểm tra bảo mật";
        _translations["Securing firewalls, fixing Defender settings, adjusting UAC, disabling telemetry logs..."] = "Đang bảo mật tường lửa, sửa cài đặt Defender, điều chỉnh UAC, tắt dữ liệu đo lường...";
        _translations["Security hardening completed successfully."] = "Tăng cường bảo mật hoàn tất thành công.";

        // --- System Optimizer Page ---
        _translations["Active Optimization Engine Tweaks"] = "Tinh chỉnh tối ưu hóa hệ thống hoạt động";
        _translations["Speed up file tree access, configure background scheduler services, and optimize memory load."] = "Tăng tốc truy cập tệp, cấu hình dịch vụ lập lịch nền và tối ưu bộ nhớ.";
        _translations["System Optimization registry settings tweaks"] = "Tinh chỉnh registry tối ưu hệ thống";
        _translations["Scan Optimize System Registry"] = "Quét & Tối ưu Registry hệ thống";
        _translations["Apply Selected Tweaks"] = "Áp dụng tinh chỉnh đã chọn";
        _translations["Ready to load system optimizer tweaks"] = "Sẵn sàng tải tinh chỉnh tối ưu hệ thống";
        _translations["Loading registry tweaks..."] = "Đang tải các tinh chỉnh registry...";
        _translations["Optimizing selected registry tweaks..."] = "Đang tối ưu các tinh chỉnh registry đã chọn...";
        _translations["System registry tweaks applied successfully."] = "Đã áp dụng các tinh chỉnh registry hệ thống thành công.";

        // --- Startup & Services Page ---
        _translations["Startup & Background Services"] = "Khởi động & Dịch vụ nền";
        _translations["Speed up Windows boot times by disabling redundant startup programs and system services."] = "Tăng tốc khởi động Windows bằng cách tắt ứng dụng khởi động và dịch vụ thừa.";
        _translations["Startup Programs"] = "Chương trình khởi động";
        _translations["Windows Services"] = "Dịch vụ Windows";
        _translations["Scan Startup & Services"] = "Quét Khởi động & Dịch vụ";
        _translations["Impact"] = "Mức độ ảnh hưởng";
        _translations["Command"] = "Lệnh";
        _translations["Registry Path"] = "Đường dẫn Registry";
        _translations["Startup Folder"] = "Thư mục Khởi động";
        _translations["Task Scheduler Task"] = "Nhiệm vụ Task Scheduler";
        _translations["Disable Selected"] = "Tắt các mục đã chọn";
        _translations["Enable Selected"] = "Bật các mục đã chọn";
        _translations["DisplayName"] = "Tên hiển thị";
        _translations["Startup Type"] = "Kiểu khởi động";
        _translations["Start Service"] = "Bắt đầu dịch vụ";
        _translations["Stop Service"] = "Dừng dịch vụ";
        _translations["Ready to load startup entries and services"] = "Sẵn sàng tải danh mục khởi động và dịch vụ";
        _translations["Loading startup entries and background services..."] = "Đang tải danh mục khởi động và dịch vụ nền...";
        _translations["Ready. Loaded startup applications and background services."] = "Sẵn sàng. Đã tải các ứng dụng khởi động và dịch vụ nền.";
        _translations["Status: Updating startup programs..."] = "Trạng thái: Đang cập nhật ứng dụng khởi động...";
        _translations["Status: Updating services..."] = "Trạng thái: Đang cập nhật dịch vụ...";
        _translations["Action completed successfully."] = "Hành động đã hoàn tất thành công.";

        // v3.2 Extensions
        _translations["Startup Applications"] = "Ứng dụng khởi động";
        _translations["Background Services"] = "Dịch vụ chạy ngầm";
        _translations["Scheduled Tasks"] = "Nhiệm vụ lên lịch";
        _translations["Startup programs registered in Windows. Disable high-impact apps to speed up boot delay times."] = "Ứng dụng khởi động đã đăng ký trong Windows. Hãy vô hiệu hóa các ứng dụng tác động cao để tăng tốc thời gian khởi động.";
        _translations["Active Background Services"] = "Dịch vụ nền đang hoạt động";
        
        _translations["Last Boot Duration"] = "Thời gian khởi động gần nhất";
        _translations["Startup Health Score"] = "Điểm sức khỏe khởi động";
        _translations["Smart Optimizer Insights"] = "Phân tích trình tối ưu";
        _translations["Undo Change"] = "Hoàn tác thay đổi";
        _translations["Quick Optimize"] = "Tối ưu nhanh";
        _translations["Microsoft Items"] = "Mục Microsoft";
        _translations["Third-Party Items"] = "Mục bên thứ ba";
        _translations["High Impact Only"] = "Chỉ tác động cao";
        _translations["Scan System"] = "Quét hệ thống";
        _translations["Ready"] = "Sẵn sàng";
        
        _translations["Application"] = "Ứng dụng";
        _translations["Publisher"] = "Nhà phát triển";
        _translations["State"] = "Trạng thái";
        _translations["Action"] = "Hành động";
        _translations["Service Display Name"] = "Tên hiển thị dịch vụ";
        _translations["Company"] = "Công ty";
        _translations["Status / Category"] = "Trạng thái / Danh mục";
        _translations["Service Control Commands"] = "Lệnh điều khiển dịch vụ";
        _translations["Task Name / Action"] = "Tên nhiệm vụ / Hành động";
        _translations["Author"] = "Tác giả";
        _translations["Last Execution Time"] = "Thời gian chạy gần nhất";
        _translations["Next Execution Time"] = "Thời gian chạy tiếp theo";
        
        _translations["Scanning startup configuration..."] = "Đang quét cấu hình khởi động...";
        _translations["Analyzing last system boot performance..."] = "Đang phân tích thời gian khởi động hệ thống...";
        _translations["Reading registry and folder startup applications..."] = "Đang đọc các ứng dụng khởi động từ registry và thư mục...";
        _translations["Scanning Windows background services..."] = "Đang quét các dịch vụ chạy nền của Windows...";
        _translations["Reading active scheduled maintenance tasks..."] = "Đang đọc các nhiệm vụ lên lịch bảo trì đang hoạt động...";
        _translations["Optimization recommendations:"] = "Khuyến nghị tối ưu hóa:";
        
        _translations["Excellent"] = "Xuất sắc";
        _translations["Good"] = "Tốt";
        _translations["Fair"] = "Khá";
        _translations["Needs Care"] = "Cần chăm sóc";
        _translations["Automatic"] = "Tự động";
        _translations["Manual"] = "Thủ công";
        _translations["Disabled"] = "Bị vô hiệu hóa";
        _translations["Running"] = "Đang chạy";
        _translations["Stopped"] = "Đã dừng";
        _translations["System"] = "Hệ thống";
        _translations["Third-Party"] = "Bên thứ ba";
        _translations["Critical"] = "Nguy kịch";
        _translations["High"] = "Cao";
        _translations["Medium"] = "Trung bình";
        _translations["Low"] = "Thấp";
        _translations["Never"] = "Không bao giờ";
        
        _translations["Permanently Remove Startup Entry"] = "Xóa vĩnh viễn mục khởi động";
        _translations["Disable System Service"] = "Vô hiệu hóa dịch vụ hệ thống";
        _translations["Delete Scheduled Task"] = "Xóa nhiệm vụ lên lịch";
        _translations["Remove"] = "Xóa bỏ";
        _translations["Disable Service"] = "Vô hiệu hóa dịch vụ";
        _translations["Delete Task"] = "Xóa nhiệm vụ";
        _translations["Cancel"] = "Hủy";
        _translations["Action Blocked: Disabling a system-critical service is restricted."] = "Hành động bị chặn: Vô hiệu hóa dịch vụ quan trọng của hệ thống bị hạn chế.";

        // --- Process Manager Page ---
        _translations["Active Processes List"] = "Danh sách tiến trình hoạt động";
        _translations["Monitor system threads load, active CPU footprints, and memory allocation tables."] = "Giám sát luồng hệ thống, tải lượng CPU và phân bổ bộ nhớ.";
        _translations["Active system task processes list"] = "Danh sách tiến trình hệ thống đang hoạt động";
        _translations["Reload Processes"] = "Tải lại tiến trình";
        _translations["Kill Selected Tasks"] = "Dừng các tác vụ đã chọn";
        _translations["Ready to monitor system processes"] = "Sẵn sàng giám sát tiến trình hệ thống";
        _translations["Loading active system processes..."] = "Đang tải danh sách tiến trình hoạt động...";
        _translations["Processes list loaded successfully."] = "Đã tải danh sách tiến trình thành công.";
        _translations["Terminating selected processes..."] = "Đang dừng các tiến trình đã chọn...";
        _translations["Selected processes terminated."] = "Các tiến trình đã chọn đã được dừng.";

        // --- Disk Tools Page ---
        _translations["Allocation Directory Weight (Double-click card to Zoom In)"] = "Trọng lượng phân bổ thư mục (Click đúp thẻ để phóng to)";
        _translations["Local storage volumes health, physical S.M.A.R.T attributes, and hardware controllers status."] = "Sức khỏe ổ đĩa lưu trữ cục bộ, thuộc tính S.M.A.R.T vật lý và trạng thái bộ điều khiển phần cứng.";
        _translations["Analysis Target Folder"] = "Thư mục đích phân tích";
        _translations["Analyze Folder"] = "Phân tích thư mục";
        _translations["Find identical files (MD5 / SHA-256) and execute one-click smart batch deletion."] = "Tìm các tệp giống hệt nhau (MD5 / SHA-256) và thực hiện xóa hàng loạt thông minh chỉ với một cú nhấp chuột.";
        _translations["Scan Duplicates"] = "Quét tệp trùng";
        _translations["Delete Pre-Selected"] = "Xóa các tệp đã chọn";
        _translations["Disk Overview"] = "Tổng quan đĩa";
        _translations["Space Analyzer"] = "Phân tích dung lượng";
        _translations["Duplicate Finder"] = "Tìm tệp trùng lặp";

        // --- Hardware Center Page ---
        _translations["Hardware Specifications"] = "Thông số kỹ thuật phần cứng";
        _translations["Physical sensors temperature monitors, Motherboard features, and Bios diagnostic components."] = "Giám sát nhiệt độ cảm biến vật lý, thông số Bo mạch chủ và chẩn đoán Bios.";
        _translations["Active Hardware Components Specs"] = "Thông số linh kiện phần cứng hoạt động";
        _translations["Central Processor Unit (CPU)"] = "Bộ vi xử lý trung tâm (CPU)";
        _translations["System Memory (RAM)"] = "Bộ nhớ hệ thống (RAM)";
        _translations["Motherboard & Bios firmware"] = "Bo mạch chủ & Bios firmware";
        _translations["Graphics Processing Unit (GPU)"] = "Bộ xử lý đồ họa (GPU)";
        _translations["Storage Disks & Partitioning"] = "Ổ đĩa lưu trữ & Phân vùng";
        _translations["Operating System details"] = "Chi tiết Hệ điều hành";
        _translations["Reload Specs"] = "Tải lại thông số";
        _translations["Ready to load hardware specifications"] = "Sẵn sàng tải thông số kỹ thuật phần cứng";
        _translations["Loading physical hardware components specifications..."] = "Đang tải thông số kỹ thuật linh kiện phần cứng...";
        _translations["Hardware specs loaded successfully."] = "Đã tải thông số kỹ thuật phần cứng thành công.";

        // --- Registry Center Page ---
        _translations["Registry Issues Scanner"] = "Quét lỗi Registry";
        _translations["Scan registry database branches (Shared DLLs, shortcuts, file extensions) and clean corruption."] = "Quét cơ sở dữ liệu Registry (DLL chung, phím tắt, đuôi tệp) và dọn dẹp lỗi.";
        _translations["Scan Registry Issues"] = "Quét lỗi Registry";
        _translations["Fix Selected Registry Issues"] = "Sửa lỗi Registry đã chọn";
        _translations["Registry Backups History"] = "Lịch sử sao lưu Registry";
        _translations["Create Registry Backup Hive"] = "Tạo bản sao lưu Registry";
        _translations["Restore Registry Backup Hive"] = "Khôi phục bản sao lưu Registry";
        _translations["Ready to perform registry analysis"] = "Sẵn sàng thực hiện phân tích registry";
        _translations["Scanning registry keys database hierarchy..."] = "Đang quét cơ sở dữ liệu registry...";
        _translations["Registry scan completed."] = "Quét registry hoàn tất.";
        _translations["Fixing selected registry database keys..."] = "Đang sửa đổi các khóa registry đã chọn...";
        _translations["Registry issues fixed successfully."] = "Đã sửa chữa lỗi registry thành công.";
        _translations["Creating registry hive backup files..."] = "Đang tạo các tệp sao lưu registry...";
        _translations["Registry backup created successfully."] = "Đã tạo bản sao lưu registry thành công.";
        _translations["Restoring system registry hive from backup file..."] = "Đang khôi phục registry từ tệp sao lưu...";
        _translations["Registry backup restored successfully. Please restart your PC to apply updates."] = "Bản sao lưu registry đã được khôi phục thành công. Vui lòng khởi động lại PC để áp dụng.";

        // --- Software Updater Page ---
        _translations["Software Updates Manager"] = "Quản lý cập nhật phần mềm";
        _translations["Scan installed software applications using Winget engine and update out-of-date tools."] = "Quét các ứng dụng phần mềm đã cài đặt bằng Winget và cập nhật các công cụ cũ.";
        _translations["Active installed software updates check list"] = "Danh sách kiểm tra bản cập nhật phần mềm hoạt động";
        _translations["Scan Available Updates"] = "Quét bản cập nhật";
        _translations["Upgrade Selected Applications"] = "Cập nhật ứng dụng đã chọn";
        _translations["Ready to scan software updates"] = "Sẵn sàng quét các bản cập nhật phần mềm";
        _translations["Scanning winget packages database..."] = "Đang quét danh sách gói phần mềm bằng winget...";
        _translations["Software updates scan completed."] = "Quét cập nhật phần mềm hoàn tất.";
        _translations["Upgrading selected application software package models..."] = "Đang cập nhật các gói phần mềm đã chọn...";
        _translations["Software packages upgraded successfully."] = "Đã cập nhật các gói phần mềm thành công.";

        // --- Driver Updater Page ---
        _translations["Driver Updates Manager"] = "Quản lý cập nhật Driver";
        _translations["Scan hardware controllers and display card drivers and apply security hotfixes updates."] = "Quét các driver bộ điều khiển phần cứng và card màn hình để cập nhật các bản vá.";
        _translations["Active hardware sensors and driver updates list"] = "Danh sách cập nhật cảm biến phần cứng và driver";
        _translations["Scan Available Drivers"] = "Quét Driver có sẵn";
        _translations["Update Selected Drivers"] = "Cập nhật Driver đã chọn";
        _translations["Ready to scan driver updates"] = "Sẵn sàng quét bản cập nhật driver";
        _translations["Querying PnP controllers database files..."] = "Đang truy vấn cơ sở dữ liệu bộ điều khiển PnP...";
        _translations["Driver updates scan completed."] = "Quét cập nhật driver hoàn tất.";
        _translations["Installing selected driver packages in the background..."] = "Đang cài đặt các gói driver đã chọn trong nền...";
        _translations["Driver updates installed successfully."] = "Đã cài đặt các bản cập nhật driver thành công.";

        // --- Software Updates Settings / Messages ---
        _translations["Checking for updates..."] = "Đang kiểm tra cập nhật...";
        _translations["New version {remoteVerStr} is available."] = "Phiên bản mới {remoteVerStr} đã có sẵn.";
        _translations["You are running the latest version (v{currentVersion.ToString(3)})."] = "Bạn đang chạy phiên bản mới nhất (v{currentVersion.ToString(3)}).";
        _translations["Downloading update..."] = "Đang tải bản cập nhật...";
        _translations["Launching installer..."] = "Đang chạy trình cài đặt...";
        _translations["Purge Complete"] = "Xóa sạch hoàn tất";
        _translations["Database logs and compiled report documents successfully purged."] = "Đã xóa sạch các bản ghi nhật ký cơ sở dữ liệu và báo cáo.";
        _translations["Purge Failed"] = "Xóa sạch thất bại";
        _translations["Accent Color Applied"] = "Đã áp dụng màu nhấn";
        _translations["System accent color successfully updated to {tag}."] = "Đã cập nhật màu nhấn hệ thống thành {tag}.";
        _translations["Diagnostics Trace logs"] = "Nhật ký theo dõi chẩn đoán";
        _translations["Language Saved / Ngôn ngữ đã lưu"] = "Ngôn ngữ đã lưu";
        _translations["Language setting has been updated. Please restart the application to apply the changes.\n\nCài đặt ngôn ngữ đã được cập nhật. Vui lòng khởi động lại ứng dụng để áp dụng thay đổi."] = "Cài đặt ngôn ngữ đã được cập nhật. Vui lòng khởi động lại ứng dụng để áp dụng thay đổi.";
        _translations["Language Saved"] = "Đã lưu ngôn ngữ";
        _translations["Language setting has been updated successfully."] = "Cài đặt ngôn ngữ đã được cập nhật thành công.";
        _translations["OK"] = "Đồng ý";
        _translations["Close"] = "Đóng";
        
        // --- Additional Dashboard and System strings ---
        _translations["PC Health Issues Detected"] = "Phát hiện vấn đề sức khỏe PC";
        _translations["High Memory Usage Bottleneck"] = "Nghẽn cổ chai do sử dụng bộ nhớ cao";
        _translations["Security Telemetry Logs Leak"] = "Rò rỉ nhật ký đo lường bảo mật";
        _translations["Outdated Software Packages"] = "Các gói phần mềm đã cũ";
        _translations["Startup applications delay"] = "Thời gian trễ ứng dụng khởi động";
        _translations["Disk Temp Alert"] = "Cảnh báo nhiệt độ ổ đĩa";
        _translations["Disk storage clutter volume"] = "Dung lượng lộn xộn bộ nhớ ổ đĩa";
        _translations["Registry Integrity Anomalies"] = "Bất thường tính toàn vẹn Registry";
        _translations["CPU temperature levels"] = "Mức độ nhiệt độ CPU";
        _translations["System Health is Secure"] = "Sức khỏe hệ thống An toàn";
        _translations["CPU Load Status Check"] = "Kiểm tra tải lượng CPU";
        _translations["Active CPU processor load is normal."] = "Tải lượng hoạt động của vi xử lý CPU bình thường.";
        _translations["High load bottleneck on CPU processor thread."] = "Nghẽn cổ chai tải lượng cao trên luồng CPU.";
        _translations["Hardware Sensor Temperature"] = "Nhiệt độ cảm biến phần cứng";
        _translations["CPU core temperature is normal."] = "Nhiệt độ lõi CPU bình thường.";
        _translations["Warning: CPU temperature levels exceed safety threshold."] = "Cảnh báo: Nhiệt độ CPU vượt quá ngưỡng an toàn.";
        _translations["Physical RAM Allocation"] = "Phân bổ RAM vật lý";
        _translations["Memory load footprint is normal."] = "Dung lượng bộ nhớ RAM chiếm dụng bình thường.";
        _translations["Warning: RAM load footprint exceeds safety threshold."] = "Cảnh báo: Bộ nhớ RAM chiếm dụng vượt quá ngưỡng an toàn.";
        _translations["Storage Drive Space"] = "Dung lượng ổ lưu trữ";
        _translations["Junk cache size is small and clean."] = "Kích thước tệp rác đệm nhỏ và sạch sẽ.";
        _translations["Junk files clutter is clean."] = "Tệp rác đã được dọn sạch.";
        _translations["Warning: Large junk files clutter size detected."] = "Cảnh báo: Phát hiện dung lượng tệp rác lớn.";
        _translations["Registry Integrity"] = "Tính toàn vẹn Registry";
        _translations["Registry database integrity checks are normal."] = "Kiểm tra tính toàn vẹn registry bình thường.";
        _translations["Registry errors detected."] = "Phát hiện lỗi registry.";
        _translations["Registry errors resolved."] = "Các lỗi registry đã được khắc phục.";
        _translations["Junk files successfully cleaned."] = "Các tệp rác đã được dọn sạch thành công.";
        _translations["Software Version Checks"] = "Kiểm tra phiên bản phần mềm";
        _translations["All software applications are up-to-date."] = "Tất cả ứng dụng phần mềm đều mới nhất.";
        _translations["Outdated software versions detected."] = "Phát hiện phiên bản phần mềm cũ.";
        _translations["Active Startup Programs"] = "Chương trình khởi động hoạt động";
        _translations["Startup boot configuration timeline is normal."] = "Cấu hình khởi động hệ thống bình thường.";
        _translations["Warning: High number of startup applications delayed boot time."] = "Cảnh báo: Số lượng lớn ứng dụng khởi động làm chậm thời gian boot.";
        _translations["Network Connection Latency"] = "Độ trễ kết nối mạng";
        _translations["DNS resolution and network latency are normal."] = "Phân giải DNS và độ trễ mạng bình thường.";
        _translations["Warning: High latency packet loss detected."] = "Cảnh báo: Phát hiện độ trễ cao và mất gói tin.";
        _translations["System Firewall Check"] = "Kiểm tra tường lửa hệ thống";
        _translations["Firewall protections are active."] = "Bảo vệ tường lửa đang hoạt động.";
        _translations["Firewall protections are inactive."] = "Bảo vệ tường lửa chưa hoạt động.";
        _translations["User Account Control Status"] = "Trạng thái kiểm soát tài khoản người dùng";
        _translations["UAC notification shield is active."] = "Thông báo UAC đang hoạt động.";
        _translations["UAC notification shield is disabled."] = "Thông báo UAC đã bị tắt.";
        _translations["Active Antivirus Shield"] = "Khiên diệt virus hoạt động";
        _translations["Defender shield protections are active."] = "Khiên bảo vệ Defender đang hoạt động.";
        _translations["Defender shield protections are disabled."] = "Khiên bảo vệ Defender đã bị tắt.";
        _translations["Telemetry Logging Privacy"] = "Quyền riêng tư nhật ký đo lường";
        _translations["Privacy settings are hardened."] = "Cấu hình quyền riêng tư đã được thắt chặt.";
        _translations["Warning: Telemetry logs tracking is enabled."] = "Cảnh báo: Tính năng theo dõi dữ liệu đo lường đang bật.";
        
        // Recommendations
        _translations["Clean junk files using Junk Cleaner tool to reclaim disk space."] = "Dọn dẹp tệp rác bằng công cụ Dọn tệp rác để giải phóng dung lượng đĩa.";
        _translations["Repair registry issues using Registry Center to resolve database conflicts."] = "Sửa lỗi registry bằng Quản lý Registry để giải quyết xung đột.";
        _translations["Update outdated applications using Software Updater to secure system apps."] = "Cập nhật ứng dụng cũ bằng Cập nhật phần mềm để bảo mật các ứng dụng.";
        _translations["Optimize your startup programs list using Startup Manager to speed up boot time."] = "Tối ưu danh sách khởi động bằng Khởi động & Dịch vụ để tăng tốc độ boot.";
        _translations["Harden firewall status configurations to protect your PC from remote threats."] = "Thắt chặt cấu hình tường lửa để bảo vệ PC khỏi các mối đe dọa từ xa.";
        _translations["Enable real-time security protections shield settings to prevent malware runs."] = "Bật bảo vệ thời gian thực để ngăn chặn thực thi mã độc.";
        _translations["Harden UAC notification permissions settings to request admin clearance on upgrades."] = "Thắt chặt quyền thông báo UAC để yêu cầu quyền quản trị khi nâng cấp.";
        _translations["Disable telemetry diagnostics tracking logs to secure private user account details."] = "Tắt tính năng theo dõi đo lường để bảo mật thông tin người dùng.";
        _translations["Clean Windows Update download cache files to reclaim storage drive footprint."] = "Dọn dẹp bộ nhớ đệm tải về Windows Update để giải phóng dung lượng đĩa.";
        _translations["Flush local DNS resolver cache registries to optimize latency and internet bandwidth."] = "Xóa bộ nhớ đệm phân giải DNS để tối ưu hóa độ trễ và băng thông.";
        _translations["Perform an active memory RAM boost to reclaim system memory and close ghost processes."] = "Thực hiện giải phóng RAM để thu hồi bộ nhớ hệ thống và đóng các tiến trình ma.";
        _translations["Apply optimizer registry settings to harden speed parameters and UI responsiveness."] = "Áp dụng cấu hình registry để tăng tốc các thông số hiệu suất và giao diện.";
        
        // System Optimizer Tweaks
        _translations["Menu Hover Delay Speedup"] = "Tăng tốc độ trễ rê chuột trên Menu";
        _translations["Reduces the wait time before menus expand on hover from 400ms to 50ms, making the Windows desktop interface feel much faster."] = "Giảm thời gian chờ trước khi mở rộng menu từ 400ms xuống 50ms, giúp giao diện Windows phản hồi nhanh hơn nhiều.";
        _translations["Auto-Close Hung Tasks on Shutdown"] = "Tự động đóng tác vụ bị treo khi tắt máy";
        _translations["Automatically terminates frozen programs during shutdown/restart instead of displaying the standard prompt delay."] = "Tự động chấm dứt các chương trình bị treo khi tắt máy hoặc khởi động lại thay vì hiển thị thông báo chờ tiêu chuẩn.";
        _translations["App Termination Shutdown Speedup"] = "Tăng tốc đóng ứng dụng khi tắt máy";
        _translations["Reduces wait time before terminating unresponsive apps during shutdown from 20 seconds to 2 seconds."] = "Giảm thời gian chờ trước khi đóng các ứng dụng không phản hồi khi tắt máy từ 20 giây xuống 2 giây.";
        _translations["Disable NTFS File Last Access Logs"] = "Vô hiệu hóa nhật ký truy cập tệp NTFS";
        _translations["Disables updating the last-access timestamp on files. Reduces disk write cycles on SSDs, extending lifespan and speed."] = "Vô hiệu hóa việc cập nhật dấu thời gian truy cập gần nhất của tệp. Giảm chu kỳ ghi đĩa trên ổ SSD, kéo dài tuổi thọ và tăng tốc độ.";
        _translations["Disable Network Packet Throttling"] = "Vô hiệu hóa giới hạn gói tin mạng";
        _translations["Disables default Windows network throttling for multimedia/gaming tasks, ensuring full network bandwidth usage."] = "Vô hiệu hóa giới hạn mạng mặc định của Windows cho các tác vụ đa phương tiện/chơi game, đảm bảo sử dụng đầy đủ băng thông mạng.";
        _translations["Prioritize Active UI Applications"] = "Ưu tiên ứng dụng giao diện đang hoạt động";
        _translations["Allocates 100% CPU resource priority to active foreground applications and games, disabling default system service reservations."] = "Phân bổ 100% mức ưu tiên tài nguyên CPU cho các ứng dụng và trò chơi đang hoạt động ở chế độ nền trước, vô hiệu hóa các đặt trước dịch vụ hệ thống mặc định.";
        _translations["UI Responsiveness"] = "Độ phản hồi UI";
        _translations["Disk & SSD"] = "Ổ đĩa & SSD";
        _translations["Scan failed:"] = "Quét thất bại:";
        _translations["Optimization failed:"] = "Tối ưu hóa thất bại:";
        _translations["Evaluation Complete. System Health is {0}/100"] = "Đánh giá hoàn tất. Sức khỏe hệ thống là {0}/100";

        // StartupPage
        _translations["Startup Applications"] = "Ứng dụng khởi động";
        _translations["Startup programs registered in Windows. Disable high-impact apps to speed up boot delay times."] = "Các chương trình khởi động cùng Windows. Hãy tắt ứng dụng ảnh hưởng cao để tăng tốc thời gian boot.";
        _translations["Reload Configuration"] = "Tải lại cấu hình";
        _translations["Active Background Services"] = "Dịch vụ chạy nền hoạt động";
        _translations["Startup / Control"] = "Khởi động / Điều khiển";
        _translations["Application"] = "Ứng dụng";
        _translations["Source"] = "Nguồn";
        _translations["State"] = "Trạng thái";
        _translations["Service Name"] = "Tên dịch vụ";
        _translations["Start"] = "Bắt đầu";
        _translations["Stop"] = "Dừng";

        // ProcessPage
        _translations["Search running processes..."] = "Tìm kiếm tiến trình đang chạy...";
        _translations["Refresh Thread Count"] = "Tải lại danh sách";
        _translations["Process Module (Right-click to control)"] = "Mô-đun tiến trình (Chuột phải để điều khiển)";
        _translations["CPU Usage"] = "Sử dụng CPU";
        _translations["Memory (RAM)"] = "Bộ nhớ (RAM)";
        _translations["Disk I/O"] = "Đọc/Ghi đĩa";
        _translations["End Task"] = "Dừng tác vụ";
        _translations["End Process Tree"] = "Dừng cây tiến trình";
        _translations["Open File Location"] = "Mở vị trí tệp";
        _translations["Search Process Online"] = "Tìm kiếm tiến trình trực tuyến";

        // RegistryPage
        _translations["Registry Center"] = "Quản lý Registry";
        _translations["Clean invalid paths, residual installer keys, and manage system registry backups."] = "Dọn dẹp đường dẫn không hợp lệ, khóa cài đặt dư thừa và quản lý sao lưu registry.";
        _translations["Detected Registry Issues"] = "Lỗi Registry phát hiện";
        _translations["Registry Backup & Restore"] = "Sao lưu & Khôi phục Registry";
        _translations["Create Full Backup"] = "Tạo bản sao lưu đầy đủ";
        _translations["Backup History"] = "Lịch sử sao lưu";
        _translations["Fix Issues"] = "Sửa lỗi";
        _translations["Scan Registry"] = "Quét Registry";

        // RepairPage
        _translations["SFC Component Verification"] = "Xác minh cấu phần SFC";
        _translations["Scans and restores system file indices against the local Windows components store."] = "Quét và khôi phục chỉ mục tệp hệ thống so với kho lưu trữ cấu phần Windows cục bộ.";
        _translations["SFC Scan"] = "Quét SFC";
        _translations["SFC Scan & Repair"] = "Quét & Sửa lỗi SFC";
        _translations["DISM OS Image Verification"] = "Xác minh ảnh hệ điều hành DISM";
        _translations["Validates OS image components against official cloud-based Windows Update feeds."] = "Xác thực các cấu phần ảnh hệ điều hành so với nguồn cấp Windows Update chính thức trên đám mây.";
        _translations["DISM Check"] = "Kiểm tra DISM";
        _translations["DISM Restore Health"] = "Khôi phục sức khỏe DISM";
        _translations["Scheduled Automations"] = "Tự động hóa theo lịch trình";
        _translations["Stop update loops, flush local download cache directories, and reconstruct database schemas."] = "Dừng vòng lặp cập nhật, xóa các thư mục đệm tải xuống cục bộ và dựng lại cơ sở dữ liệu.";
        _translations["Reset Windows Update Components"] = "Đặt lại cấu phần Windows Update";
        _translations["Services Restoration Registry"] = "Đăng ký khôi phục dịch vụ";
        _translations["Restore background core update services to standard boot setups."] = "Khôi phục các dịch vụ cập nhật cốt lõi chạy nền về cài đặt boot tiêu chuẩn.";
        _translations["Restore Selected Services"] = "Khôi phục các dịch vụ đã chọn";
        _translations["Operation Progress"] = "Tiến trình hoạt động";
        _translations["ANSI Syntax Log Console"] = "Bảng điều khiển nhật ký cú pháp ANSI";

        // SecurityPage
        _translations["Security & Privacy Shield"] = "Khiên bảo mật & Quyền riêng tư";
        _translations["Monitor local security components, verify hardware platform trust levels, and tune privacy profiles."] = "Giám sát các cấu phần bảo mật cục bộ, xác minh mức độ tin cậy của phần cứng và tinh chỉnh hồ sơ quyền riêng tư.";
        _translations["Security Center"] = "Trung tâm bảo mật";
        _translations["System Protection Level"] = "Mức độ bảo vệ hệ thống";
        _translations["Ensures active antivirus defenses, firewall protection, platform integrity policies, and prevents unauthorized startup exposures."] = "Đảm bảo các lá chắn diệt virus hoạt động, bảo vệ tường lửa, chính sách toàn vẹn nền tảng và ngăn chặn các mối nguy hại khởi động không rõ nguồn gốc.";
        _translations["Re-Scan Security"] = "Quét lại bảo mật";
        _translations["Defense Status Checklists"] = "Danh sách kiểm tra trạng thái phòng thủ";
        _translations["Antivirus Protection"] = "Bảo vệ chống virus";
        _translations["Windows Defender Firewall"] = "Tường lửa Windows Defender";
        _translations["UEFI Secure Boot"] = "Khởi động an toàn UEFI Secure Boot";
        _translations["TPM (Trusted Platform Module)"] = "Mô-đun nền tảng đáng tin cậy TPM";
        _translations["BitLocker Volume Encryption"] = "Mã hóa phân vùng BitLocker";
        _translations["Discovered Security Alerts"] = "Cảnh báo bảo mật phát hiện";
        _translations["No security risks or vulnerabilities detected. Your PC is secure."] = "Không phát hiện rủi ro bảo mật hoặc lỗ hổng nào. Máy tính của bạn an toàn.";
        _translations["Security Guidance"] = "Hướng dẫn bảo mật";
        _translations["• Windows Antivirus and Firewall block active malware processes and unsolicited remote port access."] = "• Antivirus và Tường lửa của Windows chặn các tiến trình mã độc hoạt động và truy cập cổng từ xa trái phép.";
        _translations["• TPM chips securely store passwords and keys, which is a prerequisite for Windows 11 hardware trust."] = "• Chip TPM lưu trữ mật khẩu và khóa một cách an toàn, là điều kiện tiên quyết để xác thực phần cứng Windows 11.";
        _translations["• Secure Boot prevents malicious rootkits from loading during early BIOS handoff cycles."] = "• Secure Boot ngăn chặn các rootkit độc hại tải trong chu kỳ bàn giao BIOS ban đầu.";
        _translations["Privacy Tuning"] = "Tinh chỉnh quyền riêng tư";
        _translations["Windows Privacy Toggles"] = "Bật tắt quyền riêng tư Windows";
        _translations["Personalized Ads (Advertising ID)"] = "Quảng cáo cá nhân hóa (Advertising ID)";
        _translations["Disable to block applications from tracking your app usage for targeted ads."] = "Tắt để ngăn các ứng dụng theo dõi mức độ sử dụng ứng dụng của bạn cho quảng cáo mục tiêu.";
        _translations["System Diagnostic Telemetry"] = "Dữ liệu đo lường chẩn đoán hệ thống";
        _translations["Restrict telemetry reports sent to Microsoft to Security-only levels."] = "Hạn chế báo cáo đo lường gửi đến Microsoft ở mức Chỉ bảo mật.";
        _translations["Windows Clipboard History"] = "Lịch sử Clipboard Windows";
        _translations["Saves multiple copied items to clipboard for later use. Disable to prevent caching copied credentials."] = "Lưu nhiều mục đã sao chép vào clipboard để sử dụng sau. Tắt để ngăn việc lưu bộ nhớ đệm thông tin xác thực đã sao chép.";
        _translations["Personalization & Typing Input Tracking"] = "Theo dõi đầu vào nhập liệu & Cá nhân hóa";
        _translations["Restrict Windows from logging typing history and personalization telemetry."] = "Hạn chế Windows ghi nhật ký lịch sử nhập liệu và đo lường cá nhân hóa.";
        _translations["Clear Activity Traces"] = "Xóa dấu vết hoạt động";
        _translations["Manually erase cached system activity logs and clipboard files:"] = "Xóa thủ công nhật ký hoạt động hệ thống và tệp clipboard trong bộ nhớ đệm:";
        _translations["Wipe Clipboard Cache"] = "Xóa bộ nhớ đệm Clipboard";
        _translations["Clear Recent Files & Run History"] = "Xóa tệp gần đây & Lịch sử Run";
        _translations["Privacy Guard Summary"] = "Tóm tắt bảo vệ quyền riêng tư";
        _translations["• WinCare Pro privacy settings configure official Windows Registry parameters. These apply instantly and do not run third party agent daemons."] = "• Cài đặt quyền riêng tư của WinCare Pro cấu hình các tham số Registry chính thức của Windows. Chúng áp dụng ngay lập tức và không chạy các trình nền đại lý bên thứ ba.";
        _translations["• Disabling telemetry restricts Windows updates from monitoring usage diagnostics, which may reduce background data usage."] = "• Tắt đo lường chẩn đoán sẽ hạn chế Windows Update theo dõi chẩn đoán sử dụng, điều này có thể giảm mức sử dụng dữ liệu nền.";
        _translations["• Traces clearing deletes temporary shortcuts inside Explorer Recent files history."] = "• Xóa dấu vết sẽ xóa các lối tắt tạm thời trong lịch sử tệp gần đây của Explorer.";

        // NetworkPage
        _translations["Network Center & Diagnostics"] = "Trung tâm & Chẩn đoán mạng";
        _translations["Check connectivity stability, test network performance, and repair network configuration adapters."] = "Kiểm tra tính ổn định của kết nối, kiểm tra hiệu suất mạng và sửa chữa các bộ điều hợp cấu hình mạng.";
        _translations["Refresh Diagnostics"] = "Tải lại chẩn đoán";
        _translations["Connectivity Diagnostics"] = "Chẩn đoán kết nối";
        _translations["Internet Status"] = "Trạng thái Internet";
        _translations["Gateway IP Address"] = "Địa chỉ IP Cổng kết nối";
        _translations["DNS Resolution"] = "Phân giải DNS";
        _translations["IP Address Stack"] = "Ngăn xếp địa chỉ IP";
        _translations["Signal & Speed Quality"] = "Chất lượng tín hiệu & Tốc độ";
        _translations["Latency"] = "Độ trễ";
        _translations["Packet Loss"] = "Mất gói tin";
        _translations["Internet Speed Test"] = "Kiểm tra tốc độ Internet";
        _translations["Test Speed"] = "Kiểm tra tốc độ";
        _translations["Diagnostics Terminal"] = "Bảng điều khiển chẩn đoán";
        _translations["Enter Domain or IP (e.g. google.com)"] = "Nhập tên miền hoặc IP (Ví dụ: google.com)";
        _translations["Ping"] = "Ping";
        _translations["Trace"] = "Trace";
        _translations["DNS Lookup"] = "DNS Lookup";
        _translations["Network Repair Toolkit"] = "Bộ công cụ sửa chữa mạng";
        _translations["Reset Winsock Catalog"] = "Đặt lại danh mục Winsock";
        _translations["Reset TCP/IP Stack"] = "Đặt lại ngăn xếp TCP/IP";
        _translations["Release / Renew IP"] = "Giải phóng / Làm mới IP";
        _translations["Restart Active Adapter"] = "Khởi động lại Adapter hoạt động";
        _translations["Reset Windows Firewall"] = "Đặt lại tường lửa Windows";
        _translations["Reset Proxy Settings"] = "Đặt lại cài đặt Proxy";
        _translations["Port Scanner"] = "Quét cổng";
        _translations["Host IP (e.g. localhost)"] = "IP Máy chủ (Ví dụ: localhost)";
        _translations["Target IP"] = "IP Đích";
        _translations["e.g. 80, 443, 21, 22"] = "Ví dụ: 80, 443, 21, 22";
        _translations["Ports to Scan"] = "Cổng cần quét";
        _translations["Scan Ports"] = "Quét cổng";
        _translations["DNS Optimizer"] = "Tối ưu hóa DNS";
        _translations["Active Connections"] = "Kết nối đang hoạt động";
        _translations["Process Name"] = "Tên tiến trình";
        _translations["MAC Address"] = "Địa chỉ MAC";
        _translations["Fastest"] = "Nhanh nhất";
        _translations["Protocol"] = "Giao thức";
        _translations["Local Address"] = "Địa chỉ cục bộ";
        _translations["Foreign Address"] = "Địa chỉ từ xa";
        _translations["State"] = "Trạng thái";
        _translations["PID"] = "PID";
        _translations["Search active connections..."] = "Tìm kiếm kết nối đang hoạt động...";
        _translations["Refresh Connections"] = "Làm mới kết nối";
        _translations["Active Adapters"] = "Bộ điều hợp hoạt động";
        _translations["Benchmark DNS"] = "Đánh giá DNS";
        _translations["Apply DNS"] = "Áp dụng DNS";
        _translations["Not Tested"] = "Chưa kiểm tra";
        _translations["Fastest DNS:"] = "DNS nhanh nhất:";
        _translations["Connection Quality"] = "Chất lượng kết nối";
        _translations["Diagnostics & Repair"] = "Chẩn đoán & Sửa chữa";
        _translations["DNS Benchmark"] = "Đo tốc độ DNS";
        _translations["Flush DNS Cache"] = "Xóa bộ nhớ đệm DNS";
        _translations["Active Ports"] = "Cổng hoạt động";

        // HardwarePage
        _translations["Hardware Monitoring Engine"] = "Công cụ giám sát phần cứng";
        _translations["Bare-metal hardware status, thermal sensors telemetry, and battery wear calculations."] = "Trạng thái phần cứng cơ bản, đo lường cảm biến nhiệt và tính toán độ chai pin.";
        _translations["CPU Diagnostic Array"] = "Mảng chẩn đoán CPU";
        _translations["Core Temperature"] = "Nhiệt độ lõi";
        _translations["Frequency Speed"] = "Tần số tốc độ";
        _translations["Operational Power"] = "Công suất hoạt động";
        _translations["Core Topology"] = "Cấu trúc lõi";
        _translations["GPU Diagnostic Array"] = "Mảng chẩn đoán GPU";
        _translations["Junction Temperature"] = "Nhiệt độ điểm nối";
        _translations["Processing Load"] = "Tải xử lý";
        _translations["Fan Speed Controller"] = "Bộ điều khiển tốc độ quạt";
        _translations["Memory & Storage Layout"] = "Bố cục Bộ nhớ & Lưu trữ";
        _translations["RAM Capacity Mode"] = "Chế độ dung lượng RAM";
        _translations["Physical Storage Controllers"] = "Bộ điều khiển lưu trữ vật lý";
        _translations["Read: 250 MB/s"] = "Đọc: 250 MB/s";
        _translations["Write: 180 MB/s"] = "Ghi: 180 MB/s";
        _translations["Battery Health Matrix (Laptops)"] = "Bảng sức khỏe Pin (Máy tính xách tay)";
        _translations["Charge Percentage"] = "Phần trăm sạc";
        _translations["Status Connection"] = "Trạng thái kết nối";
        _translations["Wear Level Index"] = "Chỉ số độ chai pin";
        _translations["Battery Lifecycle"] = "Vòng đời pin";
        _translations["Design Target"] = "Dung lượng thiết kế";
        _translations["Full Charge Capacity"] = "Dung lượng sạc đầy";
        _translations["Motherboard Hardware"] = "Phần cứng bo mạch chủ";
        _translations["Firmware (BIOS Version)"] = "Firmware (Phiên bản BIOS)";

        // SettingsPage
        _translations["General Configuration"] = "Cấu hình chung";
        _translations["Appearance & Theme"] = "Giao diện & Chủ đề";
        _translations["Auto Cleanup Scheduler"] = "Lập lịch dọn dẹp tự động";
        _translations["Monitoring Options"] = "Tùy chọn giám sát";
        _translations["Safety & Rollback"] = "An toàn & Khôi phục";
        _translations["Advanced Developer"] = "Nhà phát triển nâng cao";
        _translations["General Configurations"] = "Cấu hình chung";
        _translations["Language Selection"] = "Lựa chọn ngôn ngữ";
        _translations["Launch WinCare Pro automatically upon OS startup"] = "Tự động chạy WinCare Pro khi khởi động hệ điều hành";
        _translations["Check for updates automatically in the background"] = "Tự động kiểm tra bản cập nhật trong nền";
        _translations["Minimize application to System Tray instead of closing"] = "Thu nhỏ ứng dụng vào Khay hệ thống thay vì đóng";
        _translations["Software Updates"] = "Cập nhật phần mềm";
        _translations["Check if there is a newer version of WinCare Pro available."] = "Kiểm tra xem có phiên bản WinCare Pro mới hơn hay không.";
        _translations["Check for Updates"] = "Kiểm tra cập nhật";
        _translations["Local database logs size limit"] = "Giới hạn kích thước nhật ký cơ sở dữ liệu";
        _translations["Clean log rows and diagnostic assessment reports stored in SQLite."] = "Dọn dẹp nhật ký và báo cáo đánh giá chẩn đoán được lưu trong SQLite.";
        _translations["Purge Diagnostics History Logs"] = "Xóa nhật ký lịch sử chẩn đoán";
        _translations["Appearance & Theme Customization"] = "Tùy chỉnh giao diện & Chủ đề";
        _translations["Application Color Mode"] = "Chế độ màu sắc ứng dụng";
        _translations["Light Mode"] = "Chế độ sáng";
        _translations["Dark Mode"] = "Chế độ tối";
        _translations["Window Accent Colors Palette"] = "Bảng màu nhấn cửa sổ";
        _translations["Mica Acrylic Background Transparency Level"] = "Mức độ trong suốt nền Mica Acrylic";
        _translations["Enable UI fluid translation animations"] = "Bật hiệu ứng chuyển động mượt mà của giao diện";
        _translations["Auto Maintenance Task Schedulers"] = "Lập lịch tác vụ bảo trì tự động";
        _translations["Automated Cleanup Trigger Size (GB)"] = "Dung lượng kích hoạt dọn dẹp tự động (GB)";
        _translations["Trigger Smart Boost optimization if free RAM falls below 10%"] = "Kích hoạt tối ưu hóa Smart Boost nếu RAM trống dưới 10%";
        _translations["Maintenance Frequency"] = "Tần suất bảo trì";
        _translations["Daily run"] = "Chạy hàng ngày";
        _translations["Weekly scan"] = "Quét hàng tuần";
        _translations["Monthly execution"] = "Thực thi hàng tháng";
        _translations["Telemetry Diagnostics Options"] = "Tùy chọn chẩn đoán đo lường";
        _translations["Sensors Telemetry Update Interval"] = "Khoảng thời gian cập nhật đo lường cảm biến";
        _translations["0.5 seconds"] = "0.5 giây";
        _translations["1.0 seconds"] = "1.0 giây";
        _translations["2.0 seconds"] = "2.0 giây";
        _translations["5.0 seconds"] = "5.0 giây";
        _translations["Performance history duration logger"] = "Thời gian ghi nhật ký lịch sử hiệu suất";
        _translations["7 Days logs"] = "Nhật ký 7 ngày";
        _translations["30 Days logs"] = "Nhật ký 30 ngày";
        _translations["90 Days logs"] = "Nhật ký 90 ngày";
        _translations["Enable continuous low-overhead hardware sensors scanning thread"] = "Bật luồng quét cảm biến phần cứng liên tục tiêu tốn ít tài nguyên";
        
        // Monitoring & Alert section missing keys
        _translations["Monitoring & Alert"] = "Giám sát & Cảnh báo";
        _translations["Telemetry & Notifications Configuration"] = "Cấu hình Đo lường & Thông báo";
        _translations["Notification Center Policy"] = "Chính sách Trung tâm Thông báo";
        _translations["Show dynamic in-app Toast Notifications"] = "Hiển thị thông báo Toast trong ứng dụng";
        _translations["PC Health Rating Severity Trigger Limit"] = "Ngưỡng cảnh báo độ nghiêm trọng sức khỏe PC";
        _translations["Notify when system overall health score drops below threshold"] = "Thông báo khi điểm sức khỏe hệ thống giảm dưới ngưỡng";
        _translations["Notify when system automated cleaners finish runs"] = "Thông báo khi trình tự động dọn dẹp chạy xong";
        _translations["Notify when newer packages updates are available"] = "Thông báo khi có bản cập nhật gói mới hơn";
        _translations["Play subtle system audio alert upon notification arrival"] = "Phát âm thanh cảnh báo tinh tế khi có thông báo đến";
        _translations["Sensors Telemetry"] = "Đo lường Cảm biến";

        _translations["Rollback & Safety Registries"] = "Sao lưu an toàn & Đăng ký khôi phục";
        _translations["Automatically create a System Restore Point before major updates"] = "Tự động tạo Điểm khôi phục hệ thống trước các bản cập nhật lớn";
        _translations["Automatically backup system registry hive partitions"] = "Tự động sao lưu các phân vùng registry hệ thống";
        _translations["Modification Confirmation Alerts Level"] = "Mức độ cảnh báo xác nhận sửa đổi";
        _translations["Medium: Prompts warnings before major file tree mutations."] = "Trung bình: Nhắc nhở cảnh báo trước khi thay đổi cấu trúc tệp lớn.";
        _translations["Advanced & Sandbox Options"] = "Tùy chọn nâng cao & Hộp cát";
        _translations["Enable verbose debug logs trace files"] = "Bật tệp theo dõi nhật ký gỡ lỗi chi tiết";
        _translations["Plugin & Sandbox Modules Manager"] = "Quản lý mô-đun Plugin & Hộp cát";
        _translations["No custom plugins are currently loaded."] = "Hiện tại không có plugin tùy chỉnh nào được tải.";
        _translations["Browse Verified plugins"] = "Tìm kiếm plugin đã được xác minh";
        _translations["Enable experimental AI diagnostics auto-remediations (Pre-release)"] = "Bật tự động khắc phục chẩn đoán AI thử nghiệm (Bản thử nghiệm)";
        _translations["System Software Diagnostics Trace"] = "Theo dõi chẩn đoán phần mềm hệ thống";
        _translations["Show Diagnostics Log Viewer Modal"] = "Hiển thị cửa sổ xem nhật ký chẩn đoán";

        // ViewModels & Dynamic Texts
        _translations["Ready"] = "Sẵn sàng";
        _translations["Activating Game Boost... Halting updates and freeing RAM cache lines."] = "Đang kích hoạt Game Boost... Dừng cập nhật và giải phóng bộ nhớ đệm RAM.";
        _translations["Deactivating Game Boost... Re-enabling background services."] = "Đang tắt Game Boost... Kích hoạt lại các dịch vụ nền.";
        _translations["Activating Game Boost..."] = "Đang kích hoạt Game Boost...";
        _translations["Game Boost is inactive."] = "Game Boost chưa hoạt động.";
        _translations["Active. Halting wuauserv completed. Purged RAM files cache ({0} MB freed). Foreground priorities raised."] = "Đang hoạt động. Đã dừng wuauserv. Giải phóng bộ nhớ đệm RAM (Đã giải phóng {0} MB). Mức ưu tiên các ứng dụng được nâng cao.";
        _translations["Wizard Step 1: Analysing firmware components..."] = "Bước 1: Phân tích các thành phần firmware...";
        _translations["Wizard Step 2: Downloading driver binaries from verified manufacturer nodes..."] = "Bước 2: Tải xuống tệp cài đặt driver từ nguồn nhà sản xuất được xác minh...";
        _translations["Wizard Step 3: Installing driver payloads. Display flicker or audio dropouts may occur during firmware compilation."] = "Bước 3: Cài đặt driver. Màn hình có thể chớp nháy hoặc mất âm thanh tạm thời trong quá trình cài đặt.";
        _translations["Wizard Step 4: Verification of physical thread components complete."] = "Bước 4: Xác minh các thành phần phần cứng hoàn tất.";
        _translations["All physical driver installations successfully verified."] = "Tất cả driver phần cứng vật lý đã được cài đặt và xác minh thành công.";
        _translations["Driver update failed: {0}"] = "Cập nhật driver thất bại: {0}";
        _translations["No drivers selected for update."] = "Không có driver nào được chọn để cập nhật.";
        _translations["Update Available"] = "Có bản cập nhật";
        _translations["Up to date"] = "Mới nhất";
        _translations["Completed"] = "Đã hoàn thành";
        _translations["Failed"] = "Thất bại";
        _translations["Disk Tools ready.\n"] = "Công cụ ổ đĩa đã sẵn sàng.\n";
        _translations["Directory does not exist: {0}"] = "Thư mục không tồn tại: {0}";
        _translations["Starting disk usage analysis for: {0}..."] = "Bắt đầu phân tích dung lượng đĩa cho: {0}...";
        _translations["Analysis complete. Found {0} items."] = "Phân tích hoàn tất. Tìm thấy {0} mục.";
        _translations["Storage analysis error: {0}"] = "Lỗi phân tích lưu trữ: {0}";
        _translations["Searching duplicate files in: {0}..."] = "Tìm kiếm các tệp trùng lặp trong: {0}...";
        _translations["Scan complete. Found {0} duplicate groups."] = "Quét hoàn tất. Tìm thấy {0} nhóm trùng lặp.";
        _translations["Duplicate finder error: {0}"] = "Lỗi tìm tệp trùng lặp: {0}";
        _translations["Starting duplicate files cleanup..."] = "Bắt đầu dọn dẹp các tệp trùng lặp...";
        _translations["Cleaned {0} duplicate files, reclaiming {1} MB."] = "Đã xóa {0} tệp trùng lặp, giải phóng {1} MB.";
        _translations["Scanning registry for broken paths..."] = "Đang quét lỗi đường dẫn registry...";
        _translations["Scan complete. Found {0} issues."] = "Quét hoàn tất. Tìm thấy {0} lỗi.";
        _translations["Repairing selected registry issues..."] = "Đang sửa lỗi registry đã chọn...";
        _translations["Repaired {0} registry issues."] = "Đã sửa {0} lỗi registry.";
        _translations["Creating registry backup..."] = "Đang tạo bản sao lưu registry...";
        _translations["Registry backup created successfully."] = "Đã tạo bản sao lưu registry thành công.";
        _translations["Scan failed: {0}"] = "Quét thất bại: {0}";
        _translations["Repair failed: {0}"] = "Sửa lỗi thất bại: {0}";
        _translations["Backup failed: {0}"] = "Sao lưu thất bại: {0}";
        _translations["Restore defaults requires manual registry reset. Contact support."] = "Khôi phục cài đặt gốc yêu cầu đặt lại registry thủ công. Vui lòng liên hệ hỗ trợ.";
        _translations["Applying selected tweaks..."] = "Đang áp dụng các tinh chỉnh đã chọn...";
        _translations["Applied {0} tweaks successfully."] = "Đã áp dụng thành công {0} tinh chỉnh.";
        _translations["Auditing winget packages database..."] = "Đang đối chiếu cơ sở dữ liệu gói winget...";
        _translations["Updates scan completed. {0} packages available."] = "Quét cập nhật hoàn tất. Có sẵn {0} gói cập nhật.";
        _translations["Updating..."] = "Đang cập nhật...";
        _translations["Silent updating {0} ({1}/{2})..."] = "Đang cập nhật tự động {0} ({1}/{2})...";
        _translations["All background installations complete."] = "Tất cả cài đặt nền đã hoàn tất.";
        _translations["Updates failed: {0}"] = "Cập nhật thất bại: {0}";
        _translations["Wiped database and reports files"] = "Đã xóa sạch cơ sở dữ liệu và tệp báo cáo";
        _translations["Purge Complete"] = "Xóa sạch hoàn tất";
        _translations["Database logs and compiled report documents successfully purged."] = "Đã xóa sạch các bản ghi nhật ký cơ sở dữ liệu và tài liệu báo cáo.";
        _translations["Purge Failed"] = "Xóa sạch thất bại";
        _translations["Accent Color Applied"] = "Đã áp dụng màu nhấn";
        _translations["System accent color successfully updated to {0}."] = "Màu nhấn hệ thống đã được cập nhật thành {0}.";
        _translations["Diagnostics Trace logs"] = "Nhật ký theo dõi chẩn đoán";
        _translations["Language Saved / Ngôn ngữ đã lưu"] = "Đã lưu ngôn ngữ";
        _translations["Language Saved"] = "Đã lưu ngôn ngữ";
        _translations["Language setting has been updated successfully."] = "Cài đặt ngôn ngữ đã được cập nhật thành công.";
        _translations["OK"] = "Đồng ý";
        _translations["Close"] = "Đóng";
        _translations["Checking for updates..."] = "Đang kiểm tra cập nhật...";
        _translations["New version {0} is available."] = "Đã có phiên bản mới {0}.";
        _translations["You are running the latest version (v{0})."] = "Bạn đang chạy phiên bản mới nhất (v{0}).";
        _translations["Downloading update..."] = "Đang tải bản cập nhật...";
        _translations["Launching installer..."] = "Đang khởi động trình cài đặt...";
        _translations["Update Available"] = "Có bản cập nhật mới";
        _translations["Version {0} has been released (Current: {1}).\n\nWhat's New:\n{2}\n\nWould you like to download and install this update now?"] = "Phiên bản {0} đã được phát hành (Hiện tại: {1}).\n\nTính năng mới:\n{2}\n\nBạn có muốn tải xuống và cài đặt bản cập nhật này ngay bây giờ không?";
        _translations["Update Now"] = "Cập nhật ngay";
        _translations["Later"] = "Để sau";
        _translations["Creating System Restore Point..."] = "Đang tạo điểm khôi phục hệ thống...";
        _translations["Before WinCare Pro Update"] = "Trước khi cập nhật WinCare Pro";
        _translations["Download failed: {0}"] = "Tải xuống thất bại: {0}";
        _translations["Failed to check for updates: {0}"] = "Không thể kiểm tra cập nhật: {0}";
        _translations["Downloading update... {0}%"] = "Đang tải bản cập nhật... {0}%";
        _translations["AC Power"] = "Nguồn điện AC";
        _translations["Discharging"] = "Đang xả pin";
        _translations["Charging"] = "Đang sạc pin";
        _translations["Loading..."] = "Đang tải...";
        _translations["Checking..."] = "Đang kiểm tra...";
        _translations["No Internet"] = "Không có kết nối Internet";
        _translations["Reachable"] = "Có thể kết nối";
        _translations["Unreachable"] = "Không thể kết nối";
        _translations["Resolving"] = "Đang phân giải";
        _translations["IPv4: Active, IPv6: Active"] = "IPv4: Hoạt động, IPv6: Hoạt động";
        _translations["IPv4: Active, IPv6: Inactive"] = "IPv4: Hoạt động, IPv6: Không hoạt động";
        _translations["IPv4: Inactive, IPv6: Active"] = "IPv4: Không hoạt động, IPv6: Hoạt động";
        _translations["IPv4: Inactive, IPv6: Inactive"] = "IPv4: Không hoạt động, IPv6: Không hoạt động";
        _translations["IPv4: {0}, IPv6: {1}"] = "IPv4: {0}, IPv6: {1}";
        _translations["Active"] = "Hoạt động";
        _translations["Inactive"] = "Không hoạt động";
        _translations["Starting connectivity diagnosis..."] = "Bắt đầu chẩn đoán kết nối...";
        _translations["Estimating packet loss and latency quality..."] = "Ước tính chất lượng độ trễ và mất gói tin...";
        _translations["Diagnostics complete. Latency: {0}ms, Packet Loss: {1}%."] = "Chẩn đoán hoàn tất. Độ trễ: {0}ms, Mất gói tin: {1}%.";
        _translations["Diagnostics error: {0}"] = "Lỗi chẩn đoán: {0}";
        _translations["Ping test failed: {0}"] = "Kiểm tra Ping thất bại: {0}";
        _translations["Traceroute failed: {0}"] = "Traceroute thất bại: {0}";
        _translations["DNS Lookup failed: {0}"] = "DNS Lookup thất bại: {0}";
        _translations["Port scan failed: {0}"] = "Quét cổng thất bại: {0}";
        _translations["Speed test failed: {0}"] = "Kiểm tra tốc độ thất bại: {0}";
        _translations["Initiating repair action: {0}..."] = "Bắt đầu hành động sửa lỗi: {0}...";
        _translations["Repair operation succeeded."] = "Hoạt động sửa lỗi thành công.";
        _translations["Repair operation encountered errors."] = "Hoạt động sửa lỗi gặp sự cố.";
        _translations["Analyzing system security indicators..."] = "Đang phân tích các chỉ số bảo mật hệ thống...";
        _translations["Windows Firewall Active"] = "Tường lửa Windows đang hoạt động";
        _translations["Firewall Disabled or Misconfigured"] = "Tường lửa bị tắt hoặc cấu hình sai";
        _translations["Scan complete. Security Score: {0}/100"] = "Quét hoàn tất. Điểm bảo mật: {0}/100";
        _translations["Security analysis failed: {0}"] = "Phân tích bảo mật thất bại: {0}";
        _translations["Secure Boot Active (UEFI)"] = "Secure Boot hoạt động (UEFI)";
        _translations["Secure Boot Inactive or Unsupported"] = "Secure Boot không hoạt động hoặc không hỗ trợ";
        _translations["TPM v{0} Detected and Ready"] = "Đã phát hiện TPM v{0} và Sẵn sàng";
        _translations["TPM Security Chip Not Detected or Disabled"] = "Không phát hiện chip bảo mật TPM hoặc bị tắt";
        _translations["Clearing clipboard cache..."] = "Đang xóa bộ nhớ đệm clipboard...";
        _translations["Clipboard history successfully cleared."] = "Lịch sử clipboard đã được xóa thành công.";
        _translations["Failed to clear clipboard: {0}"] = "Không thể xóa clipboard: {0}";
        _translations["Clearing Recent items and Run history..."] = "Đang xóa các mục gần đây và lịch sử hộp thoại Run...";
        _translations["Recent items and Explorer Run history successfully cleared."] = "Các mục gần đây và lịch sử Explorer Run đã được xóa thành công.";
        _translations["Failed to clear recent files: {0}"] = "Không thể xóa các tệp gần đây: {0}";
        _translations["Windows Firewall is disabled! Please enable it to block malicious traffic."] = "Tường lửa Windows bị tắt! Vui lòng bật nó để chặn lưu lượng độc hại.";
        _translations["No active Antivirus protection detected. Enable Microsoft Defender."] = "Không phát hiện lớp bảo vệ Antivirus hoạt động. Hãy bật Microsoft Defender.";
        _translations["Secure Boot is disabled. Enable it in your system BIOS/UEFI for rootkit defense."] = "Secure Boot đã bị tắt. Hãy bật nó trong cài đặt BIOS/UEFI của hệ thống để phòng thủ rootkit.";
        _translations["Suspicious startup program: {0} runs shell command or runs from Temp!"] = "Chương trình khởi động đáng ngờ: {0} chạy lệnh shell hoặc chạy từ thư mục Temp!";
        _translations["Windows Defender Real-Time Protection is disabled in Policy!"] = "Lớp bảo vệ thời gian thực của Windows Defender đã bị tắt trong Chính sách!";
        _translations["Scanning registry for installed applications..."] = "Đang quét cơ sở dữ liệu registry cho ứng dụng cài đặt...";
        _translations["Uninstalling {0}..."] = "Đang gỡ cài đặt {0}...";
        _translations["Scanning leftovers for {0}..."] = "Đang quét các tệp tàn dư của {0}...";
        _translations["Scanned {0} leftover items."] = "Phát hiện {0} tệp tàn dư.";
        _translations["Successfully uninstalled. No leftovers found."] = "Đã gỡ cài đặt thành công. Không tìm thấy tệp tàn dư.";
        _translations["Uninstallation failed: {0}"] = "Gỡ cài đặt thất bại: {0}";
        _translations["Deleting leftover components..."] = "Đang xóa các tệp tàn dư...";
        _translations["Cleaned leftover files and registry entries."] = "Đã dọn sạch các tệp và registry tàn dư.";
        _translations["Error deleting leftovers: {0}"] = "Lỗi khi xóa tệp tàn dư: {0}";
        _translations["Leftover deletion cancelled."] = "Đã hủy xóa tệp tàn dư.";
        _translations["Executing {0} on service {1}..."] = "Đang thực hiện {0} đối với dịch vụ {1}...";
        _translations["Successfully sent {0} command to {1}."] = "Đã gửi thành công lệnh {0} đến {1}.";
        _translations["Failed to {0} service {1}."] = "Không thể {0} dịch vụ {1}.";
        _translations["Toggling {0}..."] = "Đang chuyển trạng thái {0}...";
        _translations["{0} toggled to {1}."] = "Đã chuyển {0} sang {1}.";
        _translations["Failed to toggle startup app {0}."] = "Không thể chuyển trạng thái ứng dụng khởi động {0}.";
        _translations["Enabled"] = "Đã bật";
        _translations["Disabled"] = "Đã tắt";
        _translations["Loading startup configurations..."] = "Đang tải cấu hình khởi động...";
        _translations["Startup data loaded successfully."] = "Đã tải cấu hình khởi động thành công.";
        _translations["Load failed: {0}"] = "Không thể tải dữ liệu: {0}";

        // --- Battery Status & Hardware specs ---
        _translations["AC Power (Charging)"] = "Nguồn điện AC (Đang sạc)";
        _translations["Fully Charged"] = "Đã sạc đầy";
        _translations["Low Battery"] = "Pin yếu";
        _translations["Critical Battery"] = "Pin yếu nghiêm trọng";
        _translations["Charging and High"] = "Đang sạc (Mức cao)";
        _translations["Charging and Low"] = "Đang sạc (Mức thấp)";
        _translations["Charging and Critical"] = "Đang sạc (Mức nguy kịch)";
        _translations["Undefined"] = "Không xác định";
        _translations["Partially Charged"] = "Đang sạc một phần";
        _translations["AC Power (No Battery)"] = "Nguồn điện AC (Không có pin)";
        _translations["Unlimited"] = "Vô hạn";
        _translations["Calculating..."] = "Đang tính toán...";
        _translations["Good"] = "Tốt";
        _translations["Cores"] = "Nhân";
        _translations["Threads"] = "Luồng";

        // --- Network Center & Repair ---
        _translations["Network Console Ready.\n"] = "Bảng điều khiển mạng Sẵn sàng.\n";
        _translations["Network Console Ready."] = "Bảng điều khiển mạng Sẵn sàng.";
        _translations["Windows Repair Center Console Ready.\n"] = "Bảng điều khiển Trung tâm Sửa lỗi Windows Sẵn sàng.\n";
        _translations["Windows Repair Center Console Ready."] = "Bảng điều khiển Trung tâm Sửa lỗi Windows Sẵn sàng.";
        _translations["SFC command execution failed: {0}"] = "Thực thi lệnh SFC thất bại: {0}";
        _translations["DISM execution failed: {0}"] = "Thực thi DISM thất bại: {0}";
        _translations["Windows Update repair execution failed: {0}"] = "Thực thi sửa lỗi Windows Update thất bại: {0}";
        _translations["Services restoration failed: {0}"] = "Khôi phục dịch vụ thất bại: {0}";
        _translations["Running"] = "Đang chạy";
        _translations["Stopped"] = "Đã dừng";
        _translations["Automatic"] = "Tự động";
        _translations["Manual"] = "Thủ công";

        // --- Process Monitor ---
        _translations["Refreshing process tree..."] = "Đang làm mới cây tiến trình...";
        _translations["Monitoring {0} active processes."] = "Đang giám sát {0} tiến trình hoạt động.";
        _translations["Refresh failed: {0}"] = "Làm mới thất bại: {0}";
        _translations["Terminating process PID {0}..."] = "Đang dừng tiến trình PID {0}...";
        _translations["Process terminated successfully."] = "Đã dừng tiến trình thành công.";
        _translations["Failed to terminate process (Access Denied)."] = "Không thể dừng tiến trình (Quyền bị từ chối).";
        _translations["Terminating process tree for PID {0}..."] = "Đang dừng cây tiến trình cho PID {0}...";
        _translations["Process tree terminated successfully."] = "Đã dừng cây tiến trình thành công.";
        _translations["Failed to terminate process tree."] = "Không thể dừng cây tiến trình.";

        // --- Security Shield ---
        _translations["Ready to scan security status"] = "Sẵn sàng quét trạng thái bảo mật";
        _translations["On"] = "Bật";
        _translations["Off"] = "Tắt";

        // --- General ---
        _translations["Failed: {0}"] = "Thất bại: {0}";

        // --- Exit Overlay & Power off ---
        _translations["Shutting Down"] = "Đang tắt ứng dụng";
        _translations["Closing database connections and freeing resources..."] = "Đang đóng kết nối cơ sở dữ liệu và giải phóng tài nguyên...";

        // --- Notifications Page & Pivot Headers ---
        _translations["Notifications & Activity Log"] = "Thông báo & Nhật ký hoạt động";
        _translations["Review system notifications, update alerts, and detailed operation history logs."] = "Xem các thông báo hệ thống, cảnh báo cập nhật và nhật ký lịch sử hoạt động chi tiết.";
        _translations["System Alerts"] = "Cảnh báo hệ thống";
        _translations["Activity Log"] = "Nhật ký hoạt động";
        _translations["System Notifications"] = "Thông báo hệ thống";
        _translations["Critical alerts, warnings, and system status updates."] = "Các cảnh báo quan trọng, cảnh báo thường và cập nhật trạng thái hệ thống.";
        _translations["Clear Alerts"] = "Xóa cảnh báo";
        _translations["All clear! No new notifications."] = "Hệ thống sạch sẽ! Không có thông báo mới.";
        _translations["Search logs by action or status..."] = "Tìm kiếm nhật ký theo hành động hoặc trạng thái...";
        _translations["Filter by Module"] = "Lọc theo Module";
        _translations["All Modules"] = "Tất cả các Module";
        _translations["Refresh"] = "Làm mới";
        _translations["Clear Logs"] = "Xóa nhật ký";
        _translations["Export Logs"] = "Xuất nhật ký";

        // Missing level filters & headers
        _translations["All Levels"] = "Tất cả các mức độ";
        _translations["Search notifications..."] = "Tìm kiếm thông báo...";
        _translations["Info"] = "Thông tin";
        _translations["Warning"] = "Cảnh báo";
        _translations["Critical"] = "Nghiêm trọng";
        _translations["Action / Operation"] = "Hành động / Thao tác";
        _translations["Module"] = "Phân hệ (Module)";
        _translations["Status"] = "Trạng thái";
        _translations["Time Logged"] = "Thời gian ghi nhận";

        // Telemetry & settings logs
        _translations["[*] System diagnostics trace initialized..."] = "[ * ] Tiến trình theo dõi chẩn đoán hệ thống đã được khởi tạo...";
        _translations["[*] Registered local SQLite connection..."] = "[ * ] Đã đăng ký kết nối SQLite cục bộ...";
        _translations["[*] Background scheduling task parsed..."] = "[ * ] Tác vụ lập lịch chạy nền đã được phân tích...";
        _translations["[*] Telemetry sensor monitoring thread spawned..."] = "[ * ] Luồng giám sát cảm biến đo lường từ xa đã được tạo...";
        _translations["[*] Safety policies integrity check: PASS"] = "[ * ] Kiểm tra tính toàn vẹn của chính sách an toàn: ĐẠT";
        _translations["[*] No CPU bottlenecks or memory leaks detected."] = "[ * ] Không phát hiện thấy nghẽn cổ chai CPU hoặc rò rỉ bộ nhớ.";
        _translations["[!] Warning: Winget update repository has outdated packages."] = "[ ! ] Cảnh báo: Kho lưu trữ cập nhật Winget có các gói đã cũ.";
        _translations["[*] Diagnostic log purge: waiting user input..."] = "[ * ] Dọn dẹp nhật ký chẩn đoán: đang chờ người dùng nhập...";

        // Software updates database notifications
        _translations["Software Updated"] = "Đã cập nhật phần mềm";
        _translations["WinCare Pro has been successfully updated to version {0}. Review the changelog to see all enhancements."] = "WinCare Pro đã được cập nhật thành công lên phiên bản {0}. Hãy xem nhật ký thay đổi để biết tất cả các cải tiến.";
        _translations["System updated to version {0}"] = "Hệ thống đã cập nhật lên phiên bản {0}";

        // About & Developer settings section
        _translations["About & Developer"] = "Thông tin & Nhà phát triển";
        _translations["About WinCare Pro"] = "Thông tin về WinCare Pro";
        _translations["Author:"] = "Tác giả:";
        _translations["Contact Support:"] = "Liên hệ hỗ trợ:";
        _translations["Source Code Repository"] = "Kho lưu trữ mã nguồn";
        _translations["Contributions, bug reports, and suggestions are welcome on GitHub."] = "Mọi đóng góp, báo cáo lỗi và đề xuất đều được chào đón trên GitHub.";

        // Dashboard & Bottleneck localizations
        _translations["Real-time performance monitors, AI diagnostics, and tiered optimization."] = "Theo dõi hiệu suất thời gian thực, chẩn đoán AI và tối ưu hóa phân tầng.";
        _translations["Bottleneck Detection"] = "Phát hiện nghẽn cổ chai";
        _translations["Our weighted detection checks CPU (40%), RAM (30%), and Disk (30%) parameters to warn you of active performance constraints."] = "Hệ thống kiểm tra trọng số các thông số CPU (40%), RAM (30%) và Ổ đĩa (30%) để cảnh báo cho bạn về các giới hạn hiệu suất hoạt động.";
        _translations["CPU sustained high load"] = "Tải CPU ở mức cao liên tục";
        _translations["RAM footprint capacity saturated"] = "Dung lượng bộ nhớ RAM đã bão hòa";
        _translations["Disk active I/O saturation"] = "Hoạt động ghi/đọc (I/O) ổ đĩa đã bão hòa";
        _translations["Bottleneck: "] = "Nghẽn cổ chai: ";
        _translations["System Status: Stable"] = "Trạng thái hệ thống: Ổn định";
        _translations["System Status: Idle"] = "Trạng thái hệ thống: Đang rảnh";
        _translations["EXCELLENT - Your system is highly optimized and clean."] = "TUYỆT VỜI - Hệ thống của bạn đã được tối ưu hóa và sạch sẽ.";
        _translations["GOOD - Some areas can be optimized to reclaim storage."] = "TỐT - Một số khu vực có thể được tối ưu hóa để thu hồi dung lượng lưu trữ.";
        _translations["NEEDS OPTIMIZATION - Heavy junk logs or updates required."] = "CẦN TỐI ƯU HÓA - Cần dọn dẹp các tệp nhật ký rác nặng hoặc cập nhật phần mềm.";
        _translations["Uptime"] = "Thời gian hoạt động";
        _translations["Network"] = "Mạng";
        _translations["Junk Size"] = "Dung lượng rác";
        _translations["Apps"] = "Ứng dụng";
        _translations["Optimize (Recommended)"] = "Tối ưu hóa (Khuyên dùng)";
        _translations["Safe Mode (Temp & DNS)"] = "Chế độ an toàn (Tệp tạm & DNS)";
        _translations["Recommended Mode (Caches & Startup)"] = "Chế độ khuyên dùng (Bộ nhớ đệm & Khởi động)";
        _translations["Advanced Mode (Registry & Tweaks)"] = "Chế độ nâng cao (Registry & Tinh chỉnh)";
        _translations["Undo Last Optimization"] = "Hoàn tác tối ưu hóa gần nhất";

        // Startup Changelog Log notifications
        _translations["System Updated to Version {0}"] = "Hệ thống đã cập nhật lên Phiên bản {0}";
        _translations["WinCare Pro has been successfully updated."] = "WinCare Pro đã được cập nhật thành công.";
        _translations["What's New:"] = "Có gì mới:";
        _translations["Responsive Layout: Dynamic collapse on narrower viewports."] = "Bố cục Đáp ứng: Tự động thu gọn trên các màn hình hẹp hơn.";
        _translations["Skeleton Loader: Beautiful entry shimmer layouts during database scan."] = "Trình tải Khung xương: Hiệu ứng lấp lánh đẹp mắt khi quét cơ sở dữ liệu.";
        _translations["Stability Fixes: Upgraded SQLite concurrent engines with WAL journal mode."] = "Sửa lỗi Ổn định: Nâng cấp công cụ SQLite đồng thời với chế độ ghi WAL.";
        _translations["Theme Consistency: Optimized contrast ratios for elements in Light Mode."] = "Nhất quán Chủ đề: Tối ưu hóa độ tương phản cho các phần tử ở Chế độ Sáng.";

        // --- Notification DB Items ---
        _translations["Software Update Available"] = "Có bản cập nhật phần mềm";
        _translations["A new version v{0} of WinCare Pro is available for download."] = "Có một phiên bản mới v{0} của WinCare Pro sẵn sàng để tải về.";
        _translations["System Health Alert"] = "Cảnh báo sức khỏe hệ thống";
        _translations["Your PC health score is low ({0}/100). Please run an optimization scan."] = "Điểm sức khỏe máy tính của bạn đang thấp ({0}/100). Vui lòng chạy quét tối ưu hóa.";
        _translations["PC diagnostics completed. Health score is {0}/100."] = "Hoàn tất chẩn đoán PC. Điểm sức khỏe là {0}/100.";
        _translations["System Scan Completed"] = "Đã hoàn tất quét hệ thống";
        _translations["Optimization Completed"] = "Đã hoàn tất tối ưu hóa";
        _translations["System has been optimized to peak performance (Health Score: 100/100)."] = "Hệ thống đã được tối ưu hóa đạt hiệu suất tối đa (Điểm sức khỏe: 100/100).";

        // --- System Optimizer & Tweaks ---
        _translations["System Optimization Tweaks"] = "Tinh chỉnh Tối ưu Hệ thống";
        _translations["All"] = "Tất cả";
        _translations["Performance"] = "Hiệu năng";
        _translations["Gaming & GPU"] = "Trò chơi & GPU";
        _translations["System & Disk"] = "Hệ thống & Ổ đĩa";
        _translations["Privacy & Logs"] = "Riêng tư & Nhật ký";
        _translations["Restore Windows Defaults"] = "Khôi phục Mặc định Windows";
        _translations["Optimize All"] = "Tối ưu Tất cả";
        _translations["Applying selected tweaks..."] = "Đang áp dụng các tinh chỉnh đã chọn...";
        _translations["Applied {0} tweaks successfully."] = "Đã áp dụng thành công {0} tinh chỉnh.";
        _translations["Reverted {0} tweaks successfully."] = "Đã khôi phục thành công {0} tinh chỉnh.";
        _translations["Are you sure you want to restore default Windows settings for all tweaks?"] = "Bạn có chắc chắn muốn khôi phục cài đặt mặc định của Windows cho tất cả tinh chỉnh không?";
        _translations["Confirm Restore"] = "Xác nhận Khôi phục";
        _translations["Yes, Restore"] = "Có, Khôi phục";
        _translations["Cancel"] = "Hủy bỏ";
        _translations["RAM Booster"] = "Tối ưu bộ nhớ RAM";
        _translations["Purge RAM Cache"] = "Giải phóng RAM";
        _translations["Boost RAM Now"] = "Tăng tốc RAM ngay";
        _translations["Auto-Boost RAM"] = "Tự động tối ưu RAM";
        _translations["Game Boost Mode"] = "Chế độ tăng tốc Game";
        _translations["Halts updates, telemetry, indexer, and SysMain during gaming, restoring them when off."] = "Dừng cập nhật, telemetry, indexer và SysMain khi chơi game, khôi phục lại khi tắt.";
        _translations["Game Boost Status"] = "Trạng thái Game Boost";
        _translations["Active. Background services halted. Priorities raised."] = "Đang hoạt động. Dịch vụ nền đã dừng. Mức ưu tiên được nâng cao.";
        _translations["Inactive. Background services restored."] = "Không hoạt động. Dịch vụ nền đã khôi phục.";
        _translations["Reclaimed {0} MB of physical memory."] = "Đã thu hồi {0} MB bộ nhớ vật lý.";
        _translations["Active RAM Cache"] = "Bộ nhớ đệm RAM";
        _translations["Total RAM"] = "Tổng RAM";
        _translations["Available RAM"] = "RAM khả dụng";
        _translations["Used RAM"] = "RAM đã dùng";
        _translations["Physical RAM usage"] = "Sử dụng RAM vật lý";
        _translations["Scan Status"] = "Trạng thái Quét";
        _translations["Check Registry"] = "Kiểm tra Registry";
        _translations["Current: {0} | Recommended: {1}"] = "Hiện tại: {0} | Khuyến nghị: {1}";
        _translations["Status: Ready"] = "Trạng thái: Sẵn sàng";

        // Tweak Names & Descriptions
        _translations["Menu Hover Delay Speedup"] = "Tăng tốc Độ trễ Di chuột Menu";
        _translations["Reduces menu expand delay on hover from 400ms to 50ms, speeding up Windows navigation."] = "Giảm độ trễ mở rộng menu khi di chuột từ 400ms xuống 50ms, tăng tốc điều hướng Windows.";
        _translations["Auto-Close Hung Tasks on Shutdown"] = "Tự đóng Tác vụ Treo khi Tắt máy";
        _translations["Automatically terminates frozen programs during shutdown/restart without prompt delay."] = "Tự động chấm dứt các chương trình bị đóng băng khi tắt/khởi động lại máy mà không cần chờ.";
        _translations["App Termination Shutdown Speedup"] = "Tăng tốc Đóng Ứng dụng khi Tắt máy";
        _translations["Reduces wait time before terminating unresponsive apps during shutdown from 20s to 2s."] = "Giảm thời gian chờ trước khi đóng các ứng dụng không phản hồi khi tắt máy từ 20 giây xuống 2 giây.";
        _translations["Disable NTFS File Last Access Logs"] = "Tắt Nhật ký Truy cập Cuối tệp NTFS";
        _translations["Disables writing last-access timestamp on files. Extends SSD lifespan by reducing disk write cycles."] = "Tắt ghi dấu thời gian truy cập cuối cùng trên các tệp. Kéo dài tuổi thọ SSD bằng cách giảm chu kỳ ghi đĩa.";
        _translations["Disable Network Packet Throttling"] = "Tắt Giới hạn Gói tin Mạng";
        _translations["Disables network packet throttling for multimedia/gaming tasks, ensuring full network bandwidth usage."] = "Tắt giới hạn gói tin mạng đối với các tác vụ đa phương tiện/chơi game, đảm bảo sử dụng băng thông mạng tối đa.";
        _translations["Prioritize Active UI Applications"] = "Ưu tiên Ứng dụng Giao diện Hoạt động";
        _translations["Allocates 100% CPU priority to active foreground games/applications, bypassing system reservations."] = "Phân bổ 100% tài nguyên CPU cho các trò chơi/ứng dụng đang chạy trên màn hình, bỏ qua các đặt trước của hệ thống.";
        _translations["Enable Windows Game Mode"] = "Bật Chế độ Game Windows";
        _translations["Enables Windows Game Mode to prioritize CPU, GPU, and RAM resources for gaming and suspend background updates."] = "Bật Chế độ chơi game của Windows để ưu tiên tài nguyên CPU, GPU và RAM cho việc chơi game và tạm dừng cập nhật nền.";
        _translations["Enable Hardware Accelerated GPU Scheduling"] = "Bật Lập lịch GPU Tăng tốc Phần cứng (HAGS)";
        _translations["Reduces graphic rendering latency and improves gaming performance by allowing direct GPU memory scheduling." ] = "Giảm độ trễ dựng hình ảnh và cải thiện hiệu năng chơi game bằng cách cho phép lập lịch bộ nhớ GPU trực tiếp.";
        _translations["Disable Telemetry & Diagnostic Data"] = "Tắt Thu thập Dữ liệu Telemetry & Chẩn đoán";
        _translations["Disables background Windows telemetry data gathering, freeing CPU, memory, and network resources."] = "Tắt thu thập dữ liệu chẩn đoán từ xa (telemetry) của Windows chạy ẩn, giải phóng tài nguyên CPU, bộ nhớ và mạng.";
        _translations["Disable Cortana Background Assistant"] = "Tắt Trợ lý Ảo Cortana Chạy ẩn";
        _translations["Stops Cortana background assistant from running, freeing system memory and CPU cycles."] = "Dừng trợ lý ảo Cortana chạy ẩn dưới nền, giải phóng bộ nhớ hệ thống và chu kỳ CPU.";
        _translations["Disable Windows Error Reporting"] = "Tắt Báo cáo Lỗi Windows";
        _translations["Disables sending error logs and reports to Microsoft, saving background resources and speed."] = "Tắt gửi nhật ký lỗi và báo cáo lỗi cho Microsoft, tiết kiệm tài nguyên chạy ẩn và tăng tốc.";
        _translations["Disable Search Indexer Backoff"] = "Tắt Tự động Giảm tốc Bộ Chỉ mục Tìm kiếm";
        _translations["Prevents search indexing from slowing down or backing off when the system is in active use."] = "Ngăn bộ chỉ mục tìm kiếm chạy chậm lại hoặc tạm dừng khi hệ thống đang được sử dụng tích cực.";
        _translations["Optimize Window Animations"] = "Tối ưu hóa Hiệu ứng Chuyển động Cửa sổ";
        _translations["Disables minimize and maximize window transition animations, making UI navigation feel instant."] = "Tắt hiệu ứng chuyển động khi thu nhỏ/phóng to cửa sổ, giúp việc điều hướng giao diện có cảm giác tức thì.";

        // UI Labels & Descriptions
        _translations["System Optimization Panel"] = "Bảng điều khiển Tối ưu hệ thống";
        _translations["Optimize system responsiveness, release memory resources, and boost game performance safely."] = "Tối ưu hóa độ phản hồi hệ thống, giải phóng bộ nhớ và tăng tốc game an toàn.";
        _translations["Halts background telemetry/services"] = "Dừng các dịch vụ chạy ngầm/telemetry";
        _translations["Toggle Engine"] = "Bật/Tắt Trình tăng tốc";
        _translations["Services Monitored:"] = "Dịch vụ được giám sát:";
        _translations["Purge program working sets"] = "Giải phóng phân vùng bộ nhớ tiến trình";
        _translations["Physical usage"] = "Sử dụng vật lý";
        _translations["Total RAM:"] = "Tổng dung lượng RAM:";
        _translations["Available RAM:"] = "Dung lượng RAM trống:";
        _translations["Used RAM:"] = "RAM đã sử dụng:";
        _translations["System Tweaks Summary:"] = "Tóm tắt tinh chỉnh hệ thống:";
        _translations["Auto-Boost is active. Will purge cache when RAM > 85%."] = "Tự động tối ưu đang hoạt động. Sẽ giải phóng bộ đệm khi RAM vượt quá 85%.";
        _translations["Total Tweaks:"] = "Tổng số tinh chỉnh:";
        _translations["Optimized:"] = "Đã tối ưu:";
        _translations["Available:"] = "Khả dụng:";
        _translations["Live Execution Log:"] = "Nhật ký thực thi trực tiếp:";
        _translations["Apply"] = "Áp dụng";
        _translations["Revert"] = "Khôi phục";
        _translations["Enabled"] = "Đã bật";
        _translations["Disabled"] = "Đã tắt";
        _translations["High Priority (0)"] = "Ưu tiên cao (0)";
        _translations["Normal (20)"] = "Bình thường (20)";
        _translations["Default (10)"] = "Mặc định (10)";

        // Live Console Log Mappings
        _translations["System Optimizer panel initialized."] = "Bảng điều khiển Tối ưu hệ thống đã được khởi tạo.";
        _translations["Auto-Boost: Memory load exceeds threshold (85%). Initiating purge."] = "Tự động tối ưu: Dung lượng bộ nhớ vượt ngưỡng (85%). Đang khởi động giải phóng.";
        _translations["RAM Booster: Purging process working sets and file cache..."] = "RAM Booster: Đang giải phóng bộ nhớ tiến trình và bộ đệm tệp...";
        _translations["RAM Booster completed. Purged {0} processes and freed {1} MB."] = "Hoàn tất RAM Booster. Đã giải phóng {0} tiến trình và giải phóng {1} MB.";
        _translations["Registry Sweep: Initiating application of selected adjustments."] = "Quét Registry: Đang bắt đầu áp dụng các tinh chỉnh đã chọn.";
        _translations["Registry Sweep: Applying tweak: {0} (Path: {1})"] = "Quét Registry: Đang áp dụng tinh chỉnh: {0} (Đường dẫn: {1})";
        _translations["Registry Sweep: Successfully applied: {0}"] = "Quét Registry: Đã áp dụng thành công: {0}";
        _translations["Registry Sweep Warning: Failed to apply: {0}"] = "Cảnh báo quét Registry: Không thể áp dụng: {0}";
        _translations["Registry Sweep completed. Successfully adjusted {0} settings."] = "Hoàn tất quét Registry. Đã tinh chỉnh thành công {0} cài đặt.";
        _translations["Registry Sweep Error: {0}"] = "Lỗi quét Registry: {0}";
        _translations["Registry Restore: Reverting all optimized tweaks to standard Windows settings."] = "Khôi phục Registry: Đang hoàn tác tất cả tinh chỉnh đã tối ưu về cài đặt mặc định của Windows.";
        _translations["Registry Restore: Reverting tweak: {0} (Path: {1})"] = "Khôi phục Registry: Đang hoàn tác tinh chỉnh: {0} (Đường dẫn: {1})";
        _translations["Registry Restore: Successfully reverted: {0}"] = "Khôi phục Registry: Đã hoàn tác thành công: {0}";
        _translations["Registry Restore Warning: Failed to revert: {0}"] = "Cảnh báo khôi phục Registry: Không thể hoàn tác: {0}";
        _translations["Registry Restore completed. Reverted {0} tweaks back to standard Windows defaults."] = "Hoàn tất khôi phục Registry. Đã khôi phục {0} tinh chỉnh về mặc định Windows.";
        _translations["Registry Restore Error: {0}"] = "Lỗi khôi phục Registry: {0}";
        _translations["Tweak Toggle: Reverting {0}"] = "Bật/Tắt tinh chỉnh: Đang hoàn tác {0}";
        _translations["Tweak Toggle: Reverted {0} successfully."] = "Bật/Tắt tinh chỉnh: Đã hoàn tác {0} thành công.";
        _translations["Tweak Toggle: Applying {0}"] = "Bật/Tắt tinh chỉnh: Đang áp dụng {0}";
        _translations["Tweak Toggle: Applied {0} successfully."] = "Bật/Tắt tinh chỉnh: Đã áp dụng {0} thành công.";
        _translations["Tweak Toggle Error: {0}"] = "Lỗi Bật/Tắt tinh chỉnh: {0}";
        _translations["Registry Filter: Visual list updated for category '{0}' (Items shown: {1})."] = "Bộ lọc Registry: Đã cập nhật danh sách hiển thị cho danh mục '{0}' (Hiển thị: {1} mục).";
        _translations["Game Boost: Activating gaming focus engine."] = "Game Boost: Đang kích hoạt công cụ tối ưu hóa chơi game.";
        _translations["Game Boost: Querying service status for '{0}'"] = "Game Boost: Đang truy vấn trạng thái dịch vụ '{0}'";
        _translations["Game Boost: Terminating background daemon: {0}"] = "Game Boost: Đang dừng dịch vụ chạy ngầm: {0}";
        _translations["Game Boost: Daemon {0} stopped successfully."] = "Game Boost: Dịch vụ {0} đã dừng thành công.";
        _translations["Game Boost Warning: Could not stop daemon '{0}' ({1})."] = "Cảnh báo Game Boost: Không thể dừng dịch vụ '{0}' ({1}).";
        _translations["Game Boost: RAM Flush completed. Freed {0} MB."] = "Game Boost: Hoàn tất giải phóng RAM. Đã giải phóng {0} MB.";
        _translations["Game Boost Engine is now active."] = "Trình tăng tốc Game hiện đang hoạt động.";
        _translations["Game Boost: Deactivating gaming focus engine."] = "Game Boost: Đang tắt công cụ tối ưu hóa chơi game.";
        _translations["Game Boost: Restarting service: {0}"] = "Game Boost: Đang khởi động lại dịch vụ: {0}";
        _translations["Game Boost: Service {0} is now running."] = "Game Boost: Dịch vụ {0} hiện đang chạy.";
        _translations["Game Boost Warning: Could not restart service '{0}' ({1})."] = "Cảnh báo Game Boost: Không thể khởi động lại dịch vụ '{0}' ({1}).";
        _translations["Game Boost Engine deactivated. System services restored to windows defaults."] = "Đã tắt trình tăng tốc Game. Dịch vụ hệ thống đã được khôi phục về mặc định Windows.";

        // --- Network Center & Diagnostics Modernized ---
        _translations["Network Center & Diagnostics"] = "Trung tâm chẩn đoán & Quản lý mạng";
        _translations["Monitor connection quality, query true DNS latency, view active connections, and apply advanced repairs."] = "Giám sát chất lượng kết nối, đo độ trễ DNS thực tế, hiển thị các kết nối đang hoạt động và sửa lỗi nâng cao.";
        _translations["Refresh Diagnostics"] = "Chẩn đoán lại";
        _translations["Connection Quality"] = "Chất lượng kết nối";
        _translations["DNS Optimizer"] = "Tối ưu hóa DNS";
        _translations["Active Connections"] = "Kết nối hoạt động";
        _translations["Diagnostics & Repairs"] = "Chẩn đoán & Sửa lỗi";
        _translations["Download"] = "Tải xuống";
        _translations["Upload"] = "Tải lên";
        _translations["Ping Latency"] = "Độ trễ Ping";
        _translations["Jitter Telemetry"] = "Độ biến động Jitter";
        _translations["Public IP Address"] = "Địa chỉ IP công cộng";
        _translations["Active DNS Server"] = "Máy chủ DNS hoạt động";
        _translations["Internet Speed Test (Multi-threaded)"] = "Đo tốc độ Internet (Đa luồng)";
        _translations["DOWNLOAD"] = "TẢI XUỐNG";
        _translations["UPLOAD"] = "TẢI LÊN";
        _translations["Multiple High-Speed CDNs (Range-based)"] = "Đo qua nhiều CDN tốc độ cao";
        _translations["Run Speed Test"] = "Đo tốc độ mạng";
        _translations["Avg Ping Latency"] = "Độ trễ Ping TB";
        _translations["Packet Loss Rate"] = "Tỷ lệ mất gói";
        _translations["Link Quality Score"] = "Điểm chất lượng mạng";
        _translations["Active Adapters"] = "Adapter đang hoạt động";
        _translations["DNS Query Latency Benchmark & Optimization"] = "Đo & Tối ưu hóa độ trễ truy vấn DNS";
        _translations["Performs live query resolutions of google.com, cloudflare.com, and microsoft.com to compute the most accurate response speeds."] = "Thực hiện truy vấn thực tế tới google.com, cloudflare.com và microsoft.com để đo tốc độ phản hồi chính xác nhất.";
        _translations["Start DNS Benchmark"] = "Bắt đầu đo DNS";
        _translations["DNS Name"] = "Tên DNS";
        _translations["IP Addresses"] = "Địa chỉ IP";
        _translations["Avg Resolution"] = "Độ trễ trung bình";
        _translations["Reliability"] = "Độ tin cậy";
        _translations["Apply DNS"] = "Áp dụng DNS";
        _translations["DNS Query Diagnostics"] = "Chẩn đoán truy vấn DNS";
        _translations["Recommended DNS"] = "DNS khuyên dùng";
        _translations["Domain Name System (DNS) maps domain names to IP addresses. Having low DNS resolution query latencies is critical for instant web-browsing response speeds, gaming latency stability, and preventing loading delay spikes."] = "Hệ thống tên miền (DNS) ánh xạ tên miền thành địa chỉ IP. Độ trễ truy vấn DNS thấp là cực kỳ quan trọng để tải web tức thì, chơi game mượt mà và tránh bị giật lag.";
        _translations["* Note: The benchmark tests standard UDP DNS resolving (Port 53) directly. Applying the DNS settings will bind all active local network adapters to the selected static servers."] = "* Lưu ý: Quá trình kiểm tra thực hiện truy vấn trực tiếp cổng UDP 53 của DNS. Áp dụng DNS sẽ thiết lập máy chủ DNS tĩnh cho tất cả card mạng đang hoạt động.";
        _translations["Refresh Connections"] = "Tải lại kết nối";
        _translations["Total Ports:"] = "Tổng số cổng:";
        _translations["Established:"] = "Đã thiết lập:";
        _translations["Listening:"] = "Đang lắng nghe:";
        _translations["Protocol"] = "Giao thức";
        _translations["Process / PID"] = "Tiến trình / PID";
        _translations["State"] = "Trạng thái";
        _translations["Diagnostics Terminal Console"] = "Bảng điều khiển chẩn đoán";
        _translations["Enter Domain or IP (e.g. google.com)"] = "Nhập tên miền hoặc IP (VD: google.com)";
        _translations["Ping"] = "Kiểm tra Ping";
        _translations["Trace"] = "Theo dõi tuyến";
        _translations["DNS Lookup"] = "Tra cứu DNS";
        _translations["Network Repair Toolkit"] = "Bộ công cụ sửa chữa mạng";
        _translations["Flush DNS Cache"] = "Xóa bộ nhớ đệm DNS";
        _translations["Low Risk"] = "Rủi ro thấp";
        _translations["Clears client DNS resolver database to resolve hostname parsing errors."] = "Xóa cơ sở dữ liệu phân giải tên miền để sửa các lỗi phân giải tên miền.";
        _translations["Reset Winsock Catalog"] = "Khôi phục Winsock";
        _translations["Requires Restart"] = "Cần khởi động lại";
        _translations["Rebuilds network sockets drivers catalog to fix corrupted socket APIs."] = "Xây dựng lại danh mục driver socket mạng để sửa các lỗi kết nối socket.";
        _translations["Reset TCP/IP Stack"] = "Khôi phục TCP/IP";
        _translations["Requires Admin"] = "Cần quyền Admin";
        _translations["Re-initializes TCP/IP parameters to resolve configuration discrepancies."] = "Khởi tạo lại các thông số cấu hình TCP/IP để sửa lỗi kết nối.";
        _translations["Optimize Advanced TCP Settings"] = "Tối ưu hóa cài đặt TCP";
        _translations["Optimizes TCP Auto-Tuning values for throughput optimization."] = "Tối ưu hóa cấu hình TCP Auto-Tuning để tăng băng thông truyền tải.";
        _translations["Disable Green/EEE Power Saving"] = "Tắt tiết kiệm điện card mạng";
        _translations["Temp Disconnect"] = "Mất kết nối tạm thời";
        _translations["Disables Ethernet Energy Saving properties to prevent sudden latency spikes."] = "Tắt thuộc tính tiết kiệm điện của Ethernet để tránh bị giật lag độ trễ đột ngột.";
        _translations["Reset Hosts File"] = "Đặt lại tệp Hosts";
        _translations["Clears registry blocks and resets hosts file back to Microsoft defaults."] = "Xóa các chặn hệ thống và đặt lại tệp cấu hình hosts về mặc định Microsoft.";
        _translations["Release / Renew IP"] = "Giải phóng / Làm mới IP";
        _translations["Releases current IPv4 address lease and requests a fresh allocation from DHCP."] = "Giải phóng địa chỉ IPv4 hiện tại và yêu cầu cấp phát IP mới từ DHCP.";
        _translations["Restart Active Adapter"] = "Khởi động lại card mạng";
        _translations["Disables and re-enables all active local network card interfaces."] = "Tắt và bật lại tất cả các giao diện card mạng đang hoạt động.";
        _translations["Search connections (e.g. chrome, ESTABLISHED, 80)..."] = "Tìm kiếm kết nối (VD: chrome, ESTABLISHED, 80)...";

        // --- C# code translations ---
        _translations["Starting connectivity diagnosis..."] = "Bắt đầu chẩn đoán kết nối mạng...";
        _translations["Connected"] = "Đã kết nối";
        _translations["No Internet"] = "Không có Internet";
        _translations["Reachable"] = "Có thể kết nối";
        _translations["Unreachable"] = "Không thể kết nối";
        _translations["Resolving"] = "Đang phân giải";
        _translations["Failed"] = "Thất bại";
        _translations["Estimating packet loss, latency, and jitter quality..."] = "Đang đánh giá tỷ lệ mất gói, độ trễ và jitter...";
        _translations["Poor"] = "Yếu";
        _translations["Moderate"] = "Trung bình";
        _translations["Good"] = "Tốt";
        _translations["Diagnostics complete. Latency: {0}ms, Jitter: {1}ms, Packet Loss: {2}%."] = "Chẩn đoán hoàn tất. Độ trễ: {0}ms, Jitter: {1}ms, Mất gói: {2}%.";
        _translations["Diagnostics error: {0}"] = "Lỗi chẩn đoán: {0}";
        _translations["Ping test failed: {0}"] = "Kiểm tra Ping thất bại: {0}";
        _translations["Traceroute failed: {0}"] = "Traceroute thất bại: {0}";
        _translations["DNS Lookup failed: {0}"] = "Tra cứu DNS thất bại: {0}";
        _translations["Port scan failed: {0}"] = "Quét cổng thất bại: {0}";
        _translations["Starting speed test..."] = "Bắt đầu đo tốc độ mạng...";
        _translations["Running download speed benchmark..."] = "Đang đo tốc độ tải xuống...";
        _translations["Running upload speed benchmark..."] = "Đang đo tốc độ tải lên...";
        _translations["Speed test complete. Download: {0} Mbps, Upload: {1} Mbps, Latency: {2} ms, Jitter: {3} ms."] = "Đo tốc độ hoàn tất. Tải xuống: {0} Mbps, Tải lên: {1} Mbps, Độ trễ: {2} ms, Jitter: {3} ms.";
        _translations["Speed Test Completed"] = "Đo tốc độ hoàn tất";
        _translations["Download: {0} Mbps, Upload: {1} Mbps."] = "Tải xuống: {0} Mbps, Tải lên: {1} Mbps.";
        _translations["Speed Test Failed"] = "Đo tốc độ thất bại";
        _translations["Initiating DNS query resolution benchmark..."] = "Đang bắt đầu đo tốc độ truy vấn DNS...";
        _translations["DNS Benchmark Completed"] = "Đo DNS hoàn tất";
        _translations["Fastest server: {0}"] = "Máy chủ nhanh nhất: {0}";
        _translations["Successfully applied DNS: {0}"] = "Đã áp dụng DNS thành công: {0}";
        _translations["DNS Server Updated"] = "Đã cập nhật Máy chủ DNS";
        _translations["Active interface configured to use {0}."] = "Card mạng hoạt động đã được cấu hình dùng {0}.";
        _translations["Failed to apply DNS settings (Requires administrator privilege)."] = "Không thể áp dụng DNS (Yêu cầu quyền Administrator).";
        _translations["DNS Setup Failed"] = "Thiết lập DNS thất bại";
        _translations["Administrative privileges required."] = "Yêu cầu quyền Administrator.";
        _translations["Initiating repair action: {0}..."] = "Bắt đầu thực thi sửa lỗi: {0}...";
        _translations["Repair operation succeeded."] = "Sửa lỗi mạng thành công.";
        _translations["Network Repair"] = "Sửa chữa mạng";
        _translations["Operation '{0}' completed successfully."] = "Thao tác '{0}' hoàn thành thành công.";
        _translations["Repair operation encountered errors."] = "Sửa lỗi gặp một số lỗi.";
        _translations["Operation '{0}' failed or requires Administrator elevation."] = "Thao tác '{0}' thất bại hoặc cần quyền Administrator.";
    }
}

public static class TranslationExtensions
{
    public static string T(this string? text)
    {
        return TranslationManager.Instance.T(text);
    }
}

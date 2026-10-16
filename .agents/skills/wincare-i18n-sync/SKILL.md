---
name: wincare-i18n-sync
description: >-
  Strict internationalization (i18n) and dual-language synchronization rules for WinCare Pro. Covers registering keys in TranslationManager.Translations.cs (vi-VN and en-US), XAML TranslationConverter binding, eliminating hardcoded strings, key naming taxonomy, and verifying translation completeness. Use whenever adding or editing UI labels, tooltips, placeholders, or dialog messages.
---

# 🌐 WinCare Pro — Quy Chuẩn Đa Ngôn Ngữ & Đồng Bộ Từ Điển (i18n Sync)

WinCare Pro hỗ trợ 100% song ngữ Việt - Anh (**vi-VN** và **en-US**). Toàn bộ chuỗi hiển thị trên giao diện người dùng **bắt buộc** phải được nạp qua hệ thống `TranslationManager`.

---

## 🚫 1. Bảng Đối Chiếu Quy Tắc Đa Ngôn Ngữ Bắt Buộc

| Tình huống | ❌ CẤM TUYỆT ĐỐI | ✅ BẮT BUỘC SỬ DỤNG |
| :--- | :--- | :--- |
| **Chuỗi text trong XAML** | `<TextBlock Text="Bắt đầu quét"/>` | `<TextBlock Text="{Binding 'Mod_Junk_StartScan', Converter={StaticResource TranslationConverter}}"/>` |
| **Chuỗi thông báo trong ViewModel** | `StatusMessage = "Đang tối ưu hệ thống...";` | `StatusMessage = TranslationManager.Instance.Get("Mod_Opt_OptimizingStatus");` |
| **Khai báo từ điển** | Chỉ thêm key vào `vi-VN` và bỏ quên `en-US` | Đồng bộ 100% key ở **cả 2 từ điển** trong `TranslationManager.Translations.cs` |
| **Nối chuỗi hiển thị có tham số** | `$"Đã dọn dẹp {mb} MB rác"` | `TranslationManager.Instance.Format("Mod_Junk_CleanedSuccess", mb)` |
| **Đặt tên key tùy tiện** | `btn1`, `txt_title`, `ScanText` | Tuân theo chuẩn phân nhóm: `Mod_{Module}_{TênKey}` hoặc `Common_{TênKey}` |

---

## 🏷️ 2. Quy Chuẩn Đặt Tên Translation Key (Key Taxonomy)

Mọi key phải tuân theo cấu trúc phân cấp rõ ràng:

1. **Chuỗi dùng chung toàn hệ thống (Common):**
   - Tiền tố: `Common_`
   - Ví dụ: `Common_Ok`, `Common_Cancel`, `Common_Close`, `Common_Save`, `Common_Refresh`, `Common_Error`, `Common_Warning`, `Common_Success`.

2. **Chuỗi theo từng phân hệ giao diện (Modules):**
   - Tiền tố: `Mod_{Module}_`
   - Ví dụ:
     - Dashboard: `Mod_Dashboard_CpuUsage`, `Mod_Dashboard_RamStatus`, `Mod_Dashboard_QuickScan`
     - Junk Cleaner: `Mod_Junk_ScanTitle`, `Mod_Junk_CleanSuccess`, `Mod_Junk_EmptyTrash`
     - Registry: `Mod_Reg_DeepScan`, `Mod_Reg_BackupWarning`, `Mod_Reg_IssuesFound`
     - Network: `Mod_Net_FlushDns`, `Mod_Net_PingSuccess`, `Mod_Net_SpeedTest`
     - Settings: `Mod_Settings_Language`, `Mod_Settings_ThemeDark`, `Mod_Settings_Autostart`

3. **Chuỗi thông báo lỗi & cảnh báo hệ thống (Errors & Dialogs):**
   - Tiền tố: `Err_` hoặc `Dialog_`
   - Ví dụ: `Err_AccessDenied`, `Err_PathNotFound`, `Dialog_ConfirmDeleteTitle`.

---

## 📖 3. Quy Trình Khai Báo Song Ngữ Chuẩn

Khi thêm chuỗi giao diện mới, mở tập tin [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs) và bổ sung đồng thời vào hai từ điển:

```csharp
// 1. Trong từ điển Tiếng Việt ("vi-VN"):
["Mod_Junk_ScanCompleted"] = "Quét hoàn tất! Tìm thấy {0} tệp rác ({1}).",
["Mod_Junk_CleanWarning"] = "Các tệp tạm sẽ bị xóa vĩnh viễn khỏi ổ đĩa.",

// 2. Trong từ điển Tiếng Anh ("en-US"):
["Mod_Junk_ScanCompleted"] = "Scan completed! Found {0} junk files ({1}).",
["Mod_Junk_CleanWarning"] = "Temporary files will be permanently deleted from disk.",
```

---

## 🎨 4. Ràng Buộc Trong XAML (Data Binding)

Sử dụng `TranslationConverter` đã được khai báo Resource toàn cục trong [App.xaml](file:///d:/WinCare/App.xaml):

### TextBlock tĩnh:
```xml
<TextBlock Text="{Binding 'Mod_Dashboard_Title', Converter={StaticResource TranslationConverter}}"
           Style="{StaticResource SubtitleTextBlockStyle}" />
```

### Button có Content và ToolTip:
```xml
<Button Content="{Binding 'Common_Refresh', Converter={StaticResource TranslationConverter}}"
        ToolTipService.ToolTip="{Binding 'Mod_Dashboard_RefreshTooltip', Converter={StaticResource TranslationConverter}}"
        Command="{Binding RefreshCommand}" />
```

### TextBox Placeholder:
```xml
<TextBox PlaceholderText="{Binding 'Mod_Settings_SearchPlaceholder', Converter={StaticResource TranslationConverter}}"
         Text="{Binding SearchQuery, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

---

## 🧪 5. Kiểm Tra Tự Động & Chống Sót Key

Trước khi bàn giao mã nguồn:
1. Sử dụng `grep_search` kiểm tra file XAML mới xem có chữ tiếng Việt / tiếng Anh viết cứng (`Text="..."` không qua Converter) không.
2. Kiểm tra bộ từ điển `vi-VN` và `en-US` đảm bảo:
   $$\text{Count}(vi\text{-}VN) == \text{Count}(en\text{-}US)$$
3. Chạy kiểm thử translation nếu có trong bộ test suite:
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~Translation" --logger "console;verbosity=minimal"
   ```

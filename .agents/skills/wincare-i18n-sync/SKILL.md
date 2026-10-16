---
name: wincare-i18n-sync
description: >-
  Strict internationalization (i18n) and dual-language synchronization rules for WinCare Pro. Covers registering keys in TranslationManager.Translations.cs (vi-VN and en-US), XAML TranslationConverter binding, eliminating hardcoded strings, key naming taxonomy, and verifying translation completeness. Use whenever adding or editing UI labels, tooltips, placeholders, or dialog messages.
---

# 🌐 WinCare Pro — Internationalization & Dictionary Synchronization Standards (i18n Sync)

WinCare Pro mandates 100% dual-language coverage for Vietnamese and English (**vi-VN** and **en-US**). All user interface display strings **MUST** be loaded dynamically through `TranslationManager`.

---

## 🚫 1. Internationalization Enforcement Matrix

| Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **XAML Text Literals** | `<TextBlock Text="Start Scan"/>` | `<TextBlock Text="{Binding 'Mod_Junk_StartScan', Converter={StaticResource TranslationConverter}}"/>` |
| **ViewModel Display Strings** | `StatusMessage = "Optimizing system...";` | `StatusMessage = TranslationManager.Instance.Get("Mod_Opt_OptimizingStatus");` |
| **Dictionary Definitions** | Adding a key to `vi-VN` while omitting `en-US` | Synchronize 100% of keys across **both dictionaries** in `TranslationManager.Translations.cs` |
| **Parameterized String Formatting** | `$"Cleaned {mb} MB of junk"` | `TranslationManager.Instance.Format("Mod_Junk_CleanedSuccess", mb)` |
| **Arbitrary Key Naming** | `btn1`, `txt_title`, `ScanText` | Strictly follow hierarchical taxonomy: `Mod_{Module}_{KeyName}` or `Common_{KeyName}` |

---

## 🏷️ 2. Translation Key Taxonomy Standards

All translation keys MUST follow a strict hierarchical structure:

1. **Shared System Strings (Common):**
   - Prefix: `Common_`
   - Canonical examples: `Common_Ok`, `Common_Cancel`, `Common_Close`, `Common_Save`, `Common_Refresh`, `Common_Error`, `Common_Warning`, `Common_Success`.

2. **Feature Module Strings (Modules):**
   - Prefix: `Mod_{Module}_`
   - Canonical examples:
     - Dashboard: `Mod_Dashboard_CpuUsage`, `Mod_Dashboard_RamStatus`, `Mod_Dashboard_QuickScan`
     - Junk Cleaner: `Mod_Junk_ScanTitle`, `Mod_Junk_CleanSuccess`, `Mod_Junk_EmptyTrash`
     - Registry: `Mod_Reg_DeepScan`, `Mod_Reg_BackupWarning`, `Mod_Reg_IssuesFound`
     - Network: `Mod_Net_FlushDns`, `Mod_Net_PingSuccess`, `Mod_Net_SpeedTest`
     - Settings: `Mod_Settings_Language`, `Mod_Settings_ThemeDark`, `Mod_Settings_Autostart`

3. **System Errors & Dialog Strings (Errors & Dialogs):**
   - Prefix: `Err_` or `Dialog_`
   - Canonical examples: `Err_AccessDenied`, `Err_PathNotFound`, `Dialog_ConfirmDeleteTitle`.

---

## 📖 3. Standard Dual-Dictionary Registration Workflow

When introducing new UI strings, open [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs) and register them simultaneously in both language dictionaries:

```csharp
// 1. In Vietnamese dictionary ("vi-VN"):
["Mod_Junk_ScanCompleted"] = "Quét hoàn tất! Tìm thấy {0} tệp rác ({1}).",
["Mod_Junk_CleanWarning"] = "Các tệp tạm sẽ bị xóa vĩnh viễn khỏi ổ đĩa.",

// 2. In English dictionary ("en-US"):
["Mod_Junk_ScanCompleted"] = "Scan completed! Found {0} junk files ({1}).",
["Mod_Junk_CleanWarning"] = "Temporary files will be permanently deleted from disk.",
```

---

## 🎨 4. XAML Data Binding Patterns

Utilize the global `TranslationConverter` defined in [App.xaml](../../App.xaml):

### Static TextBlock:
```xml
<TextBlock Text="{Binding 'Mod_Dashboard_Title', Converter={StaticResource TranslationConverter}}"
           Style="{StaticResource SubtitleTextBlockStyle}" />
```

### Button with Content and ToolTip:
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

## 🧪 5. Automated Verification & Missing Key Prevention

Before finalizing code changes:
1. Execute `grep_search` across newly modified XAML files to detect hardcoded text literals (`Text="..."` without Converter).
2. Validate dictionary cardinality:
   $$\text{Count}(vi\text{-}VN) == \text{Count}(en\text{-}US)$$
3. Execute translation test suite:
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~Translation" --logger "console;verbosity=minimal"
   ```

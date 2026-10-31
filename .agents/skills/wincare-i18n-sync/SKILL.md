---
name: wincare-i18n-sync
description: >-
  Strict internationalization (i18n) and dual-language synchronization rules for WinCare Pro. Covers registering keys in TranslationManager.Translations.cs (vi-VN and en-US), XAML TranslationConverter binding, eliminating hardcoded strings, key naming taxonomy, and verifying translation completeness. Use whenever adding or editing UI labels, tooltips, placeholders, or dialog messages.
---

# 🌐 WinCare Pro — Dual-Language Internationalization (i18n Synchronization)

WinCare Pro features full native bilingual capabilities across Vietnamese (`vi-VN`) and English (`en-US`). Hardcoded strings are strictly forbidden in both XAML and C# presentation tiers.

## 📁 Skill Assets & Helpers
- **Verification Script:** [`scripts/verify-i18n-parity.ps1`](./scripts/verify-i18n-parity.ps1)
- **Reference Examples:**
  - Programmatic Translation: [`examples/TranslationCodeExample.cs`](./examples/TranslationCodeExample.cs)
  - XAML Translation Binding: [`examples/TranslationBindingExample.xaml`](./examples/TranslationBindingExample.xaml)
- **Taxonomy & Guidelines:**
  - Key Naming Taxonomy: [`resources/i18n-taxonomy.md`](./resources/i18n-taxonomy.md)

---

## 🏛️ 1. Translation Invariants

1. **Zero Hardcoded Display Strings:**
   ```xml
   <!-- ❌ STRICTLY FORBIDDEN -->
   <TextBlock Text="System Optimizer"/>

   <!-- ✅ MANDATORY PATTERN -->
   <TextBlock Text="{Binding Source='System Optimizer', Converter={StaticResource TranslationConverter}}"/>
   ```
2. **Synchronized Dictionary Registration:**
   All keys MUST be registered in [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs):
   ```csharp
   _translations["Your English Key"] = "Nội dung tiếng Việt tương ứng";
   ```
3. **Automated Verification:**
   Before submitting code with UI additions, always run:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .agents/skills/wincare-i18n-sync/scripts/verify-i18n-parity.ps1
   ```

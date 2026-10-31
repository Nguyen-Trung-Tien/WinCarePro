# 🌐 WinCare Pro — Translation Key Taxonomy & Standards

## Taxonomy Conventions
- **Navigation Items:** `Nav_{Section}_{Page}` (e.g., `Nav_Overview`, `Nav_Optimization`) or canonical English title (e.g., `Dashboard`, `System Optimizer`).
- **Module Controls:** `Mod_{Feature}_{Action}` (e.g., `Mod_DiskClean_Start`, `Mod_Registry_Backup`).
- **Status & Diagnostics:** `Status_{State}` (e.g., `Idle`, `Running`, `Completed`, `Failed`).
- **System Metrics:** `Metric_{Name}` (e.g., `Metric_Cpu`, `Metric_Ram`, `Metric_Storage`).

## Registration Rules
1. Add new key-value pair to `TranslationManager.Translations.cs`:
   ```csharp
   _translations["Your English Title or Key"] = "Tiêu đề tiếng Việt";
   ```
2. The reverse translation (`_reverseTranslations`) is automatically constructed by `BuildReverseTranslations()` at runtime.
3. Verify parity using:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .agents/skills/wincare-i18n-sync/scripts/verify-i18n-parity.ps1
   ```


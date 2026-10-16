---
name: wincare-ui-aura
description: >-
  Guidelines for building WinUI 3 Fluent 2.0 Aura Glass interfaces, styling cards, binding Tabular numerals, hooking WeakReferenceMessenger, avoiding memory leaks via Unloaded events, and registering dual vi-VN/en-US translations. Use when editing or creating XAML views and ViewModels.
---

# 🎨 WinCare Pro — Aura Glass UI/UX & WinUI 3 Design Standards

WinCare Pro employs the **Aura Glass** design aesthetic built upon Fluent 2.0 (Windows App SDK / WinUI 3), integrating Mica Alt backdrops, subtle corner rounding, fluid micro-interactions, and 100% dual-language coverage.

---

## 💎 1. Aura Glass Design System Core Principles

1. **Backdrop & Translucency:**
   - Implement Mica Alt or Acrylic styling with subtle opacity gradients.
   - Content cards utilize `Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"` coupled with translucent borders `BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`.
2. **Consistent Corner Radii:**
   - Primary cards & containers: `CornerRadius="8"`
   - Interactive buttons & inputs: `CornerRadius="4"` or `CornerRadius="6"`
   - Badges & status chips: `CornerRadius="12"` (pill shape)
3. **Typography & Tabular Numerals for Telemetry:**
   - ALL dynamic numerical metrics (CPU %, RAM MB, Storage capacity GB/MB, Latency ms/s) **MUST** configure tabular numeral alignment:
     ```xml
     <TextBlock Text="{Binding FormattedMemory}" 
                FontFamily="Segoe UI Variable Display"
                Typography.NumeralAlignment="Tabular"
                FontWeight="SemiBold"/>
     ```
   - Tabular figure rendering enforces fixed character widths, eliminating layout jitter during high-frequency telemetry updates.

---

## 🌐 2. Mandatory Dual-Language Integration (i18n & Zero Hardcoding)

- **FORBIDDEN:** Direct string literals in XAML:
  ```xml
  <!-- ❌ FORBIDDEN: Unlocalizable literal violating Rule 09 -->
  <Button Content="Start Scan"/>
  <TextBlock Text="System Cleaned Successfully!"/>
  ```
- **MANDATORY:** Dynamic data binding via `TranslationConverter`:
  ```xml
  <!-- ✅ MANDATORY: Dynamic runtime localization -->
  <Button Content="{Binding Source='Btn_StartScan', Converter={StaticResource TranslationConverter}}"/>
  <TextBlock Text="{Binding Source='Msg_CleanSuccess', Converter={StaticResource TranslationConverter}}"/>
  ```
- All translation keys MUST be registered simultaneously in both `vi-VN` and `en-US` dictionaries within [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs).

---

## 🔄 3. UI Thread Safety & Zero-Crash Concurrency

- All asynchronous routines mutating observable state (e.g., assigning `Text` properties, appending items to `ObservableCollection`) **MUST** be dispatched via `RunOnUI()`:
  ```csharp
  RunOnUI(() =>
  {
      ScanResults.Add(newItem);
      ProgressValue = currentPercentage;
  });
  ```
- Sync-over-async invocations (`task.Result`, `task.Wait()`) are STRICTLY FORBIDDEN. ALWAYS use `await`.

---

## 🧹 4. Memory Discipline & Leak Elimination

- **Detach Event Handlers on Navigation:**
  Inside the `Unloaded` view lifecycle event or explicit cleanup methods, detach 100% of subscribed event handlers (`-=`):
  ```csharp
  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
      ViewModel.TelemetryUpdated -= OnTelemetryUpdated;
      Unloaded -= OnUnloaded;
  }
  ```
- **Suspend Visual Animations & Canvas Renders:**
  Upon navigation away from a page, halt active animation timers and continuous canvas loops to maintain CPU Idle $\le 0.3\%$ and Foreground RAM $\le 85\text{ MB}$.
- **Decoupled ViewModel Messaging:**
  Employ `WeakReferenceMessenger.Default` for cross-module communication to eliminate strong reference cycles and ensure clean Garbage Collection.

---
name: wincare-ui-aura
description: >-
  Guidelines for building WinUI 3 Fluent 2.0 Aura Glass interfaces, styling cards, binding Tabular numerals, hooking WeakReferenceMessenger, avoiding memory leaks via Unloaded events, and registering dual vi-VN/en-US translations. Use when editing or creating XAML views and ViewModels.
---

# 🎨 WinCare Pro — Aura Glass (Fluent 2.0) UI Design System

WinCare Pro features a premium Fluent 2.0 design system codenamed **Aura Glass**, powered by WinUI 3 with Mica Alt backdrops, subtle acrylic transparency, 8px corner radii, and strict Tabular typography.

## 📁 Skill Assets & Helpers
- **UI Linter Script:** [`scripts/verify-ui-conventions.ps1`](./scripts/verify-ui-conventions.ps1)
- **Reference Examples:**
  - Aura Page XAML: [`examples/SampleAuraPage.xaml`](./examples/SampleAuraPage.xaml)
  - Memory Safe Code-Behind: [`examples/SampleAuraPage.xaml.cs`](./examples/SampleAuraPage.xaml.cs)
- **Resource Dictionaries:**
  - Aura Glass Card Styles: [`resources/AuraGlassResourceDictionary.xaml`](./resources/AuraGlassResourceDictionary.xaml)

---

## 🏛️ 1. Aura Glass Design Invariants

1. **Card Radius:** All primary cards MUST have `CornerRadius="8"`.
2. **Typography for Metrics:** All numeric telemetry displays (CPU, RAM, Disk, Temperature, FPS) MUST apply:
   ```xml
   Typography.NumeralAlignment="Tabular"
   ```
   This prevents horizontal UI jittering during real-time value updates.
3. **No Hardcoded Display Strings:** ALL visible text must bind through `TranslationConverter`:
   ```xml
   <TextBlock Text="{Binding Source='Nav_Optimization', Converter={StaticResource TranslationConverter}}"/>
   ```
4. **Lifecycle & Memory Discipline:**
   - Detach all event handlers (`Loaded`, `Unloaded`, `PointerMoved`, etc.) in the `Unloaded` event.
   - Pause or stop canvas/composition animations when navigating away from the page.

---

## 📐 2. Standard Card Pattern

```xml
<Border Background="{ThemeResource LayerFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Padding="16">
    <!-- Card Content -->
</Border>
```

---
name: wincare-ui-aura
description: >-
  Guidelines for building WinUI 3 Fluent 2.0 Aura Glass interfaces, styling cards, binding Tabular numerals, hooking WeakReferenceMessenger, avoiding memory leaks via Unloaded events, and registering dual vi-VN/en-US translations. Use when editing or creating XAML views and ViewModels.
---

# 🎨 WinCare Pro — Aura Glass (Fluent 2.0) UI Design System

WinCare Pro features a premium Fluent 2.0 design system codenamed **Aura Glass** (inspired by Raycast, Linear, and CleanMyMac X), powered by WinUI 3 with Deep Obsidian canvas (`#0D0F14`), static Ambient Mesh Glow (`#00D2FF` & `#7B2CBF`), Precision Glass cards with Specular Top-Edge Rim borders (`SpecularGlassBorderBrush`), Custom Circular Ring Gauges, and GPU Composition physics.

## 📁 Skill Assets & Helpers
- **Core Styles Dictionary:** [`src/Presentation/Styles/AppStyles.xaml`](../../../src/Presentation/Styles/AppStyles.xaml)
- **Fluid Animation Helper:** [`src/Presentation/Styles/Animations/FluidAnimationHelper.cs`](../../../src/Presentation/Styles/Animations/FluidAnimationHelper.cs)
- **UI Linter Script:** [`scripts/verify-ui-conventions.ps1`](./scripts/verify-ui-conventions.ps1)
- **Reference Examples:**
  - Aura Page XAML: [`examples/SampleAuraPage.xaml`](./examples/SampleAuraPage.xaml)
  - Memory Safe Code-Behind: [`examples/SampleAuraPage.xaml.cs`](./examples/SampleAuraPage.xaml.cs)
- **Resource Dictionaries:**
  - Aura Glass Card Styles: [`resources/AuraGlassResourceDictionary.xaml`](./resources/AuraGlassResourceDictionary.xaml)

---

## 🏛️ 1. Aura Glass Design Invariants

1. **Canvas & Ambient Lighting:**
   - Base background: Deep Obsidian `#0D0F14` (`AppDeepObsidianBackground`).
   - Ambient Mesh Glow: Layer static radial gradient glow (`Opacity="0.12"`, Cyan `#00D2FF` to Indigo `#7B2CBF`) via `AppAmbientMeshGlowBrush`.
2. **Card Geometry & Specular Borders:**
   - Card Background: High-density obsidian glass `#161922` (`GlassCardBackgroundBrush`).
   - Specular Top-Edge Rim: MUST declare `BorderBrush="{StaticResource SpecularGlassBorderBrush}"` (`LinearGradientBrush` with `#45FFFFFF` at top fading to `#0F1218` at bottom).
   - Radius: `CornerRadius="10"` or `"12"`.
3. **Data Visualization (Zero Generic ProgressBars):**
   - Telemetry (CPU, RAM, Disk, Health Score) MUST render via **Custom Circular Ring Gauges** (`Microsoft.UI.Xaml.Shapes.Path` with round end-caps and dynamic load aura glow).
4. **Themed Icon Badges:**
   - Feature blocks MUST declare rounded `IconBadge` (`CornerRadius="10"`), translucent themed gradient background, and sharp glyph.
5. **GPU Composition Physics:**
   - Cards: Attach `FluidAnimationHelper.ApplyCardHoverElevation(card, -3f)` (150ms cubic-bezier lift).
   - Buttons: Attach `FluidAnimationHelper.ApplyButtonPressPhysics(button)` (SpringVector3, DampingRatio 0.6f, Period 100ms).
   - Primary CTA: Use `PrimaryGradientButtonStyle` (`#0072FF` -> `#00C6FF`) with shimmer sweep overlay.
6. **Typography for Metrics:** All numeric telemetry displays (CPU, RAM, Disk, Temperature, FPS) MUST apply:
   ```xml
   Typography.NumeralAlignment="Tabular"
   ```
   This prevents horizontal UI jittering during real-time value updates.
7. **No Hardcoded Display Strings:** ALL visible text must bind through `TranslationConverter`:
   ```xml
   <TextBlock Text="{Binding Source='Nav_Optimization', Converter={StaticResource TranslationConverter}}"/>
   ```
8. **Lifecycle & Memory Discipline:**
   - Detach all event handlers (`Loaded`, `Unloaded`, `PointerMoved`, etc.) in the `Unloaded` event.
   - Pause or stop canvas/composition animations when navigating away from the page.

---

## 📐 2. Precision Glass Card Pattern

```xml
<Border Background="{StaticResource GlassCardBackgroundBrush}"
        BorderBrush="{StaticResource SpecularGlassBorderBrush}"
        BorderThickness="1"
        CornerRadius="12"
        Padding="16">
    <!-- Card Content with Themed Icon Badge & Tabular Numerals -->
</Border>
```

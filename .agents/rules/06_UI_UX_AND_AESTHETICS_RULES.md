# 🎨 06. Aura Glass UI/UX & Aesthetics Rules

> [⬅️ 05. C# 13 Coding Standards](05_CODING_STANDARDS_AND_CONVENTIONS.md) • [🏠 Rules Index](README.md) • **Rule 06** • [Next Rule: 07. Testing & QA ➡️](07_TESTING_AND_QA_RULES.md)

---

## 💎 1. Aura Glassmorphic Fluent 2.0 Design System

All screens and XAML presentation components MUST adhere to modern Fluent 2.0 aesthetics:

1. **Backdrop Materials & Elevation Depth:**
   - **Deep Obsidian Canvas:** Use deep dark backdrop `#0D0F14` (`AppDeepObsidianBackground`) combined with **MicaAlt**.
   - **Ambient Mesh Glow:** Layer a subtle static ambient radial mesh glow (`Opacity="0.12"`, Cyan `#00D2FF` fading into Indigo Purple `#7B2CBF` via `AppAmbientMeshGlowBrush`) to provide deep cinematic spatial depth.
   - Use **Desktop Acrylic** for floating overlays (Desktop HUD Widget, Flyouts, context menus).
2. **Precision Glass Cards & Specular Luminous Borders:**
   - Card background: High-density obsidian glass `#161922` with subtle translucency (`GlassCardBackgroundBrush`).
   - Specular Top-Edge Rim: Card borders MUST use `LinearGradientBrush` (`SpecularGlassBorderBrush`) from `#45FFFFFF` at top to `#0F1218` at bottom.
   - Information containers and cards MUST apply rounded geometry: `CornerRadius="10"` or `"12"`.

---

## 📊 2. Visual Data Graphics & Iconography

1. **Circular Ring Gauges (Zero Generic ProgressBars):**
   - For CPU, RAM, Disk, and Health Score: Linear ProgressBars are strictly forbidden. Telemetry MUST render via **Custom Circular Ring Gauges** (`Microsoft.UI.Xaml.Shapes.Path` with `StrokeStartLineCap="Round"` and `StrokeEndLineCap="Round"`).
   - Underneath the active ring: Declare an ambient **Aura Glow** (`GaugeGlowGoodBrush`, `GaugeGlowWarningBrush`, `GaugeGlowCriticalBrush`) that dynamically shifts color (Emerald < 65%, Amber 65-85%, Ruby > 85%).
2. **Themed Icon Badges:**
   - Monochromatic raw font icons are forbidden. Feature groups MUST use an **Icon Badge** (`CornerRadius="10"`) with a themed translucent gradient background and sharp colored glyph.
3. **Tabular Numerals:**
   - All real-time telemetry numbers MUST specify `Typography.NumeralAlignment="Tabular"` to eliminate layout jitter during high-frequency telemetry refreshes.

---

## 🎬 3. Composition Motion & GPU Physics

1. **Interactive Hover Lift & Spotlight:**
   - Cards MUST attach `FluidAnimationHelper.ApplyCardHoverElevation(card, -3f)`: on hover, the card smoothly lifts `Offset.Y = -3` in `150ms` (`CubicBezierEasingFunction`), while the specular border highlights.
2. **Spring Natural Motion Button Physics:**
   - Interactive buttons MUST attach `FluidAnimationHelper.ApplyButtonPressPhysics(button)` using `SpringVector3NaturalMotionAnimation` (`DampingRatio = 0.6f`, `Period = 100ms`).
3. **Primary CTA Shimmer Sweep:**
   - Hero action buttons MUST use `PrimaryGradientButtonStyle` (`#0072FF` to `#00C6FF` gradient) with a specular shimmer light sweep overlay across the surface on hover.
4. **Smooth Page Transitions:**
   - All navigation pages MUST configure `EntranceNavigationTransitionInfo` and `EntranceThemeTransition` with `FromVerticalOffset="24"` for a smooth upward entrance glide.

---

## 👁️ 4. Accessibility & High Contrast (WCAG AAA)

1. **Text Contrast Ratios:**
   - Foreground-to-background contrast MUST achieve at least $4.5:1$ for body copy and $3:1$ for large display headers per WCAG 2.1.
2. **Full Keyboard Navigation:**
   - All interactive controls (buttons, inputs, selectable list rows) MUST render a clear focus visual (`FocusVisualPrimaryBrush`) on `Tab` and directional arrow key events.
   - Button styles in `AppStyles.xaml` and `App.xaml` MUST specify `UseSystemFocusVisuals="True"` and `FocusVisualPrimaryThickness="2"`.
3. **Live Regions & Screen Readers:**
   - Status indicators (e.g., `ScanStatus`) MUST specify `AutomationProperties.LiveSetting="Polite"`.
   - All icon-only buttons MUST declare `AutomationProperties.Name` and descriptive tooltips via `ToolTipService.ToolTip`.

---

## 🎨 5. Theme Studio Palette Governance

Ad-hoc raw hex values are STRICTLY FORBIDDEN in XAML files. All brushes MUST bind to centralized Theme Studio resource keys:

| Semantic Role | Resource Key | State Indication |
| :--- | :--- | :--- |
| **Primary Accent** | `SystemAccentColor` / `AuraAccentBrush` | Core brand identity, primary call-to-action buttons |
| **Success / Safe** | `SuccessStatusBrush` (`#2EA043`) | Optimal health score, protected service, completed task |
| **Warning / Attention** | `WarningStatusBrush` (`#D29922`) | Elevated RAM, low disk space threshold, accumulated cache |
| **Danger / Critical** | `DangerStatusBrush` (`#F85149`) | Security vulnerability, S.M.A.R.T disk fault, critical error |

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

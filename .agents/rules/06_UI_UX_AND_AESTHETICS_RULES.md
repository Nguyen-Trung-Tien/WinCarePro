# 🎨 06. Aura Glass UI/UX & Aesthetics Rules

> [⬅️ 05. C# 13 Coding Standards](05_CODING_STANDARDS_AND_CONVENTIONS.md) • [🏠 Rules Index](README.md) • **Rule 06** • [Next Rule: 07. Testing & QA ➡️](07_TESTING_AND_QA_RULES.md)

---

## 💎 1. Aura Glassmorphic Fluent 2.0 Design System

All screens and XAML presentation components MUST adhere to modern Fluent 2.0 aesthetics:

1. **Backdrop Materials & Elevation Depth:**
   - Use **Mica** or **MicaAlt** for primary window backdrops on Windows 11.
   - Use **Desktop Acrylic** for floating overlays (Desktop HUD Widget, Flyouts, context menus).
2. **Corner Radii & Subtle Borders:**
   - Information containers and cards MUST apply rounded geometry: `CornerRadius="8"` or `"12"`.
   - Card perimeters MUST declare `BorderThickness="1"` with subtle translucency (`rgba(255, 255, 255, 0.08)` in Dark Theme).

---

## 🎬 2. Composition Motion & Micro-Interactions

1. **Timing & Easing Curves:**
   - Page transitions and card expansions MUST NOT exceed $250\text{ ms}$.
   - ALWAYS use smooth cubic-bezier easing (`EaseInOut` or `FastOutSlowIn`).
2. **Staggered Entrance Loading:**
   - Collection rendering (e.g., detected junk files or uninstallable packages) MUST animate sequentially with a $30\text{ ms}$ stagger per item.
3. **Shimmer Skeleton Loading:**
   - Display a translucent animated shimmer skeleton while scanning or retrieving data rather than an isolated static `ProgressRing`.

---

## 👁️ 3. Accessibility & High Contrast (WCAG AAA)

1. **Text Contrast Ratios:**
   - Foreground-to-background contrast MUST achieve at least $4.5:1$ for body copy and $3:1$ for large display headers per WCAG 2.1.
2. **Full Keyboard Navigation:**
   - All interactive controls (buttons, inputs, selectable list rows) MUST render a clear focus visual (`FocusVisualPrimaryBrush`) on `Tab` and directional arrow key events.
3. **Tooltips on Icon Actions:**
   - Icon-only command buttons MUST declare descriptive tooltips via `ToolTipService.ToolTip`.

---

## 🎨 4. Theme Studio Palette Governance

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

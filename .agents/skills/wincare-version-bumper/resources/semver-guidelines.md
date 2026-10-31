# 🏷️ WinCare Pro — Semantic Versioning & Codename Guidelines

WinCare Pro adheres to **Semantic Versioning 2.0.0** (`MAJOR.MINOR.PATCH`) adapted for desktop software release cycles:

$$\mathbf{Version} = \mathbf{MAJOR}.\mathbf{MINOR}.\mathbf{PATCH}$$

## 1. Release Classification
- **MAJOR (X.0.0):**
  - Breaking architectural changes (e.g., migration to .NET 10, complete overhaul of Native Interop, new UI paradigm).
  - New release codename assigned (e.g., `v4.x Nova` $\rightarrow$ `v5.x Polaris`).
- **MINOR (5.X.0):**
  - Major new feature additions, new utility modules, or significant engine enhancements.
  - Backward-compatible database schema additions.
- **PATCH (5.1.X):**
  - Targeted bug fixes, performance optimizations, memory leak remediations, or security patches.
  - Zero schema breaking changes.

## 2. Codename Taxonomy
- Codename is held in `AppConstants.Codename` and displayed in `SystemBadgeText`.
- Current Codename: **Polaris** (WinCare Pro 5.x Suite).


---
name: wincare-token-guard
description: >-
  AI context window and token optimization runbook for WinCare Pro. Enforces targeted grep_search and view_file slicing (30-60 lines), zero write_to_file overwrites of large files, minimal diff anchors, and quiet terminal outputs. Use when planning refactorings or file edits across large codebases.
---

# 📉 WinCare Pro — Token Economy & Precision Guard (Token Guard)

AI pair programmers MUST optimize prompt and completion tokens to prevent context dilution, eliminate hallucinations, and sustain low latency.

## 📁 Skill Assets & Helpers
- **Narrow Slicer Script:** [`scripts/token-slice-helper.ps1`](./scripts/token-slice-helper.ps1)
- **Reference Examples:**
  - Minimal vs Bloated Diff Anchors: [`examples/MinimalDiffAnchorsExample.md`](./examples/MinimalDiffAnchorsExample.md)
- **Core Rules:**
  - Token Economy Directives: [`resources/token-economy-rules.md`](./resources/token-economy-rules.md)

---

## 🏛️ 1. Core Protocols

1. **Grep Before Read:** Always use `grep_search` to pinpoint exact line numbers before viewing files.
2. **Narrow Slicing:** Read strictly 30 to 60 lines around target symbols. Never load entire files > 150 lines.
3. **Localized Patching:** Never overwrite full files with `write_to_file`. Apply minimal-anchor diffs.
4. **Quiet Output:** Use `-v q --nologo` for builds and minimal logger for tests.

---
name: wincare-token-guard
description: >-
  AI context window and token optimization runbook for WinCare Pro. Enforces targeted grep_search and view_file slicing (30-60 lines), zero write_to_file overwrites of large files, minimal diff anchors, and quiet terminal outputs. Use when planning refactorings or file edits across large codebases.
---

# 🧠 WinCare Pro — Quy Chuẩn Tiết Kiệm Token & Thao Tác Chính Xác (Token Guard)

Quy chuẩn này là cẩm nang hướng dẫn AI Agent thao tác với chi phí token thấp nhất và độ chính xác cao nhất trên codebase WinCare Pro Suite.

---

## 🎯 1. Chiến Lược 3 Bước Tiết Kiệm Context Window

### Bước 1: Định Vị Không Đọc Quét Tràn Lan
- **Không bao giờ** gọi `view_file` cả file > 150 dòng (ví dụ `MainWindow.xaml.cs`, `App.xaml.cs`, `DbManager.cs`).
- **Luôn** dùng `grep_search` với `Includes: ["*.cs"]` hoặc `["*.xaml"]` để xác định chính xác số dòng chứa phương thức / biến.
- Sau đó dùng `view_file` chỉ lấy lát cắt hẹp xung quanh mục tiêu:
  ```json
  { "AbsolutePath": "d:/WinCare/...", "StartLine": 120, "EndLine": 160 }
  ```

### Bước 2: Vá Cục Bộ (In-Place Minimal Anchors)
- **Tuyệt đối CẤM** dùng `write_to_file` với `Overwrite = true` để ghi đè toàn bộ file mã nguồn cũ.
- **Bắt buộc** dùng `replace_file_content` hoặc `multi_replace_file_content`.
- Mỏ neo context (`TargetContent`) chỉ cần 2-4 dòng duy nhất để định vị chính xác vị trí thay thế, không copy nguyên cả khối 50 dòng nếu chỉ sửa 2 dòng.

### Bước 3: Triệt Tiêu Log Rác Terminal (Terminal Suppression)
- Không chạy lệnh mặc định in hàng chục dòng log:
  ```powershell
  # ❌ Lãng phí token
  dotnet test
  dotnet build
  
  # ✅ Tối ưu, súc tích
  dotnet build WinCarePro.csproj -c Debug -v q --nologo
  dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TênTest" --logger "console;verbosity=minimal"
  ```

---

## 🚫 2. Danh Sách Đỏ Các Anti-Patterns Thường Gặp Của AI

1. **Ảo giác API (API Hallucination):** Tự bịa tham số hoặc phương thức chưa từng tồn tại trong `SafePathGuard` hay `ProcessRunner`. Tra cứu ngay tại [docs/00_AI_SYMBOL_INDEX.md](file:///d:/WinCare/docs/00_AI_SYMBOL_INDEX.md) trước khi gọi.
2. **Nuốt lỗi (Silent Catch):** Để `catch { }` rỗng. Mọi exception phải qua `CrashLogger.LogError(ex)` và trả về `OperationResult.Fail(ex)`.
3. **Quên `finally` giải phóng trạng thái:** Quên đặt `IsBusy = false` trong `finally`, làm giao diện bị đơ nút bấm mãi mãi nếu xảy ra lỗi.
4. **Copy-paste lại toàn bộ mã nguồn vào đoạn chat:** Không paste lại cả file hàng trăm dòng vào tin nhắn phản hồi khi diff đã thể hiện đầy đủ. Tóm tắt trực tiếp: Nguyên nhân - Vị trí sửa - Kết quả test.

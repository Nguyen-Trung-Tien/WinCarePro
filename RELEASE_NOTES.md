# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro v5.1.1

---

## 🚀 WinCare Pro v5.1.1 (Codename: Polaris) — Tối Ưu Hóa Toàn Diện, Native Interop [LibraryImport] & Cinematic Composition Animations

> **Phiên bản:** v5.1.1 (Codename: Polaris) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Trạng thái:** Bản Phát Hành Chính Thức (Official Release) · **Chứng nhận:** 100% Tests PASS · 0 Warnings / 0 Errors

**WinCare Pro v5.1.1 (Codename: Polaris)** là bản cập nhật nâng cấp và tối ưu hóa hệ thống toàn diện, củng cố kiến trúc Zero-Trust, chuyển đổi 100% Native Interop sang C# 13 `[LibraryImport]`, tăng tốc phản hồi giao diện với Windows Composition Animations và củng cố độ bền bỉ Zero-Crash:

### 🌟 Điểm Nhấn Đột Phá Trong v5.1.1:

1. **⚡ 100% Di Chuyển Sang `[LibraryImport]` (Zero P/Invoke Overhead):**
   - Loại bỏ hoàn toàn `[DllImport]` cũ, chuyển đổi sang source-generated `[LibraryImport]` trên toàn bộ hệ thống (`ThemeManager`, `NotificationService`, `LockingAppService`, `ProcessService`, `App.xaml.cs`).
   - Sử dụng `stackalloc char[2048]` và `Span<char>` an toàn trong `QueryFullProcessImageNameW`, giảm triệt để GC allocation và áp lực cấp phát bộ nhớ.

2. **🛡️ Tăng Cường Bảo Mật Zero-Trust & Registry Armor:**
   - Hoàn thiện `RegistryBackupEngine.BackupKey` & `BackupKeyAsync` đáp ứng triệt để quy chuẩn `SafeRegistryGuard + RegistryBackupEngine.BackupKey()`.
   - Chuẩn hóa toàn bộ CLI execution với `ProcessRunner.RunAsync` và `ArgumentList` tách bạch (bao gồm `netstat.exe` và `reg.exe`), triệt tiêu 100% nguy cơ Command Injection.
   - Bảo vệ phân luồng đa nhiệm an toàn: bổ sung semaphore timeout và chống deadlock trong `AuditLogService`.

3. **🎨 Nâng Cấp Fluent 2.0 Aura Glass & Cinematic Composition Animations:**
   - Bổ sung hiệu ứng Micro-Interactions: `ApplyCardHoverElevation` (nâng thẻ mượt mà với cubic bezier) và `ApplyButtonPressPhysics` (lực đàn hồi cơ học Spring physics khi nhấn nút).
   - Khắc phục lỗi rò rỉ sự kiện và mất liên kết ViewModel khi chuyển trang: tự động tái đăng ký và hủy đăng ký `PropertyChanged` / `LanguageChanged` trong `OnNavigatedTo` và `Unloaded`.

---

<div align="center">
  <sub>WinCare Pro Suite v5.1.1 Polaris • Phát triển bởi <b>Nguyễn Trung Tiến</b></sub>
</div>

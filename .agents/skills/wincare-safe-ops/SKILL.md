---
name: wincare-safe-ops
description: >-
  Mandatory safety procedures and rules for OS operations in WinCare Pro, including file deletions (SafePathGuard), registry edits/deletions (SafeRegistryGuard and backup), CLI execution (ProcessRunner with ArgumentList), service protection (ServiceSafetyService), and DPAPI encryption (CryptoHelper). Use whenever writing code that touches files, registry, CLI processes, or Windows services.
---

# 🛡️ WinCare Pro — Quy Chuẩn Thao Tác Hệ Điều Hành An Toàn (Safe OS Operations)

Hệ thống WinCare Pro can thiệp sâu vào Windows 10/11 x64. Mọi AI Agent và lập trình viên **bắt buộc** áp dụng Zero-Trust Safeguards trước khi chạm vào tệp tin, registry, tiến trình và dịch vụ Windows.

---

## 🚫 1. Bảng Đối Chiếu Thao Tác An Toàn Bắt Buộc

| Loại thao tác | ❌ Tuyệt đối CẤM | ✅ Bắt buộc sử dụng |
| :--- | :--- | :--- |
| **Xóa Tệp/Thư Mục** | `File.Delete(p)`, `Directory.Delete(p)` trực tiếp | `if (SafePathGuard.IsSafeToDelete(p)) { File.Delete(p); }` |
| **Sửa/Xóa Registry** | `regKey.DeleteSubKeyTree(k)` | Kiểm tra `SafeRegistryGuard.IsSafeToDeleteKey(root, subKey)` & Sao lưu trước qua `RegistryBackupEngine.BackupKey(...)` |
| **Gọi lệnh Shell** | `Process.Start("cmd.exe", "/c " + cmd)` | `ProcessRunner.RunAsync("app.exe", new[] { arg1, arg2 }, ct)` |
| **Can thiệp Service** | Gọi `sc stop <name>` bừa bãi | Kiểm tra `ServiceSafetyService.IsProtectedService(name)` trước |
| **Dữ liệu nhạy cảm** | Lưu plain text password/token/key | Mã hóa bằng `CryptoHelper.Protect(plainText)` qua Windows DPAPI |
| **Truy cập SQLite** | Đọc/ghi đa luồng trực tiếp | Luôn bọc trong `lock (_dbLock)` tại `DbManager.cs` |

---

## 📁 2. Quy Chuẩn Tệp Tin (File Safety via `SafePathGuard`)

Trước khi xóa bất kỳ file hoặc thư mục nào:
1. Kiểm tra:
   ```csharp
   if (!SafePathGuard.IsSafeToDelete(targetPath))
   {
       AppLogger.LogWarning($"[SafePathGuard] Blocked deletion attempt of protected path: {targetPath}");
       return OperationResult.Fail($"Path '{targetPath}' is protected by SafePathGuard.");
   }
   ```
2. `SafePathGuard` tự động chặn:
   - Thư mục gốc (`C:\`, `D:\`,...).
   - Thư mục Windows (`C:\Windows`, `System32`, `SysWOW64`, `WinSxS`).
   - Dữ liệu người dùng quan trọng (`Desktop`, `Documents`, `Pictures`, `Music`, `Videos`, `Downloads`).
   - Thư mục hệ thống (`Program Files`, `ProgramData`).
   - Reparse Points / Junctions / Symbolic links (ngăn ngừa Directory Traversal qua symlink attack).

---

## 🗝️ 3. Quy Chuẩn Registry (Registry Safety via `SafeRegistryGuard`)

1. **Sao lưu trước khi chỉnh sửa:**
   ```csharp
   var backupResult = await _backupEngine.BackupKeyAsync(rootHive, subKeyPath, ct);
   if (!backupResult.Success)
   {
       return OperationResult.Fail("Registry backup failed. Operation aborted for safety.");
   }
   ```
2. **Kiểm tra độ sâu và Whitelist:**
   ```csharp
   if (!SafeRegistryGuard.IsSafeToDeleteKey(rootHive, subKeyPath))
   {
       return OperationResult.Fail($"Registry key '{subKeyPath}' is protected.");
   }
   ```
3. `SafeRegistryGuard` bảo vệ:
   - Ngăn xóa Root Hives (`HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`).
   - Bắt buộc độ sâu khóa $\ge 3$ (Ví dụ: `Software\Microsoft\Windows\CurrentVersion\Run`).
   - Bảo vệ toàn bộ khóa hệ thống cốt lõi (BCD00000000, SAM, SECURITY, SYSTEM, HARDWARE).

---

## ⚡ 4. Quy Chuẩn CLI & Chống Command Injection (`ProcessRunner`)

- **CẤM:** Ghép chuỗi lệnh:
  ```csharp
  // ❌ NGUY HIỂM: Dễ bị command injection nếu arg chứa ký tự đặc biệt & | ; ` $
  Process.Start("cmd.exe", $"/c ping {host}");
  ```
- **BẮT BUỘC:** Sử dụng `ProcessRunner` kèm `ArgumentList`:
  ```csharp
  // ✅ AN TOÀN: Arguments được cô lập thành đối số riêng biệt
  var args = new[] { "-n", "4", host };
  var result = await ProcessRunner.RunAsync("ping.exe", args, ct);
  if (!result.Success)
  {
      AppLogger.LogError($"Command failed: {result.StandardError}");
  }
  ```
- Làm sạch chuỗi trước khi chuyển vào qua [InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs).

---

## 🔒 5. Bảo Vệ Windows Services (`ServiceSafetyService`)

- Trước khi dừng (`Stop`), vô hiệu hóa (`Disable`) bất kỳ dịch vụ nào:
  ```csharp
  if (ServiceSafetyService.IsProtectedService(serviceName))
  {
      return OperationResult.Fail($"Cannot modify protected Windows core service: {serviceName}");
  }
  ```
- Danh sách 45+ dịch vụ sống còn được bảo vệ vĩnh viễn: `RpcSs`, `DcomLaunch`, `WinDefend`, `CryptSvc`, `EventLog`, `PlugPlay`, `SamSs`, `LSM`, `wuauserv` (chế độ an toàn),...

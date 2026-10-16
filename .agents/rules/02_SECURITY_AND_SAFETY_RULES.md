# 🔒 02. Quy Chuẩn An Toàn & Bảo Mật Tuyệt Đối (Security & Safety Rules)

> [⬅️ 01. Kiến Trúc & Phân Tầng](01_ARCHITECTURE_AND_DESIGN_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 02** • [Quy Chuẩn Tiếp Theo: 03. Đa Luồng & Bất Đồng Bộ ➡️](03_CONCURRENCY_AND_THREADING_RULES.md)

---

## 🚫 1. Chống Tấn Công Command Injection (Quy Tắc Tuyệt Đối `ProcessRunner`)

WinCare Pro tương tác với các công cụ dòng lệnh của hệ điều hành (`winget.exe`, `sfc.exe`, `dism.exe`, `reg.exe`, `powercfg.exe`, `netsh.exe`).

### 🔴 QUY TẮC BẮT BUỘC:
1. **KHÔNG BAO GIỜ** sử dụng phép ghép chuỗi thô để tạo câu lệnh thực thi (Raw String Concatenation).
2. **LUÔN LUÔN** sử dụng [ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs) và truyền danh sách tham số thông qua `ProcessStartInfo.ArgumentList`.
3. Mọi đầu vào từ người dùng hoặc tên gói phần mềm từ bên thứ ba phải được lọc qua [InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs).

### ❌ Code Bị Cấm (Lỗ hổng bảo mật nghiêm trọng):
```csharp
// NGUY HIỂM: Dễ bị chèn lệnh độc hại như "app & shutdown -s -t 0"
var process = Process.Start("cmd.exe", "/c winget upgrade " + userAppId);
```

### ✅ Code Chuẩn Mực Production:
```csharp
var psi = new ProcessStartInfo
{
    FileName = "winget.exe",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};
// Mỗi tham số được cô lập an toàn trong mảng đối số:
psi.ArgumentList.Add("upgrade");
psi.ArgumentList.Add("--id");
psi.ArgumentList.Add(InputSanitizer.Sanitize(userAppId));
psi.ArgumentList.Add("--silent");
```

---

## 🛡️ 2. Bảo Vệ Tệp Hệ Thống & Tránh Xóa Nhầm (`SafePathGuard`)

Mọi hành động xóa tệp hoặc thư mục trong toàn bộ ứng dụng (đặc biệt trong `JunkCleanerEngine`, `UninstallEngine`, `DiskEngine`) **bắt buộc** phải gọi `SafePathGuard.IsSafeToDelete(path)` trước khi thực hiện `File.Delete()` hoặc `Directory.Delete()`.

### Danh sách đen cấm xóa (Blacklisted Targets):
- Thư mục gốc ổ đĩa: `C:\`, `D:\`, `E:\`
- Thư mục hệ thống: `C:\Windows`, `C:\Windows\System32`, `C:\Windows\SysWOW64`, `C:\Windows\WinSxS`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\ProgramData`
- Thư mục dữ liệu người dùng cá nhân: `Desktop`, `Documents`, `Pictures`, `Music`, `Videos`, `Downloads`
- Tệp nhạy cảm & Dữ liệu đăng nhập: `Login Data`, `Web Data`, `Cookies`, `Local State`, `SAM`, `SYSTEM`, `SECURITY`

### Quy tắc xóa thư mục không đệ quy:
- Khi thực hiện `Directory.Delete(path, recursive: false)`, phải xác minh thư mục hoàn toàn rỗng. Nếu còn tệp tin hoặc thư mục con, phải từ chối xóa để tránh mất mát dữ liệu không kiểm soát.

### Quy tắc xử lý liên kết (Reparse Point & Symlink Junctions):
- **Tuyệt đối không đệ quy vào Junction Points:** Khi duyệt thư mục tạm, nếu gặp `FileAttributes.ReparsePoint`, phải bỏ qua việc duyệt sâu vào bên trong để ngăn chặn tấn công liên kết chéo dẫn tới thư mục ngoài ý muốn.

---

## 🧬 3. Phòng Vệ Khóa & Giá Trị Registry Cốt Lõi (`SafeRegistryGuard`)

1. **Cấm xóa Root Hives:** Tuyệt đối ngăn chặn xóa các Root Hives: `HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`.
2. **Cấm xóa tiền tố hệ thống sống còn:** Bảo vệ các nhánh `HKLM\SYSTEM\CurrentControlSet\Control` và `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`.
3. **Độ sâu khóa tối thiểu:** Mọi thao tác xóa khóa con phải có độ sâu đường dẫn tối thiểu từ 3 cấp trở lên (`SegmentCount >= 3`).
4. **Bảo vệ giá trị khởi động sống còn:** Cấm sửa đổi hoặc xóa giá trị `Shell` (mặc định `explorer.exe`) và `Userinit` (mặc định `userinit.exe`).

---

## 🛡️ 4. Bảo Vệ Dịch Vụ Cốt Lõi Windows (`ServiceSafetyService`)

1. **Danh sách trắng bảo vệ (Service Whitelist):**
   - Nghiêm cấm vô hiệu hóa hoặc dừng các dịch vụ thiết yếu: `RpcSs`, `DcomLaunch`, `SamSs`, `gpsvc`, `ProfSvc`, `BFE`, `WinDefend`, `CryptSvc`, `EventLog`, `PlugPlay`, `RpcEptMapper`.
2. **Kiểm tra trước khi thay đổi trạng thái:**
   - Mọi thao tác tinh chỉnh trong `StartupEngine` hoặc `SystemOptimizerEngine` phải gọi `ServiceSafetyService.IsProtectedService(serviceName)`. Nếu là dịch vụ được bảo vệ, phải từ chối hành động và trả về cảnh báo an toàn.

---

## 🔐 4. Xác Thực Chữ Ký Số & Mã Hóa Dữ Liệu

1. **WinVerifyTrust Interop:**
   - Các gói cập nhật phần mềm do `SoftwareUpdaterEngine` tải về phải được xác minh tính hợp lệ của chữ ký số (Authenticode Signature) qua Win32 API `WinVerifyTrust` trước khi được phép chạy cài đặt ngầm.
2. **Mã hóa dữ liệu nhạy cảm bằng Windows DPAPI:**
   - Các thông tin cấu hình nhạy cảm của người dùng (Token, API Key, cấu hình riêng tư) lưu trong CSDL SQLite phải được mã hóa qua [CryptoHelper.cs](file:///d:/WinCare/Core/Helpers/CryptoHelper.cs) sử dụng `ProtectedData.Protect` với `DataProtectionScope.CurrentUser`.
   - Tuyệt đối không lưu mật khẩu hoặc thông tin bí mật ở dạng chuỗi rõ (Plain-text).

---

## 🔄 5. Nguyên Tắc An Toàn & Khôi Phục (Snapshot Before Action)

Trước khi thực hiện bất kỳ sửa đổi Registry, vô hiệu hóa dịch vụ hoặc tinh chỉnh hệ thống nào:
1. Phải ghi nhận trạng thái cũ vào bảng `Snapshots` trong CSDL thông qua `DbManager.SaveSnapshot()` hoặc xuất bản sao lưu `.reg` thông qua `RegistryBackupEngine`.
2. Cung cấp khả năng hoàn tác 1-Click thông qua `UndoManagerService`.

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

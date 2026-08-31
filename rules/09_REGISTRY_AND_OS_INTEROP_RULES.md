# 🧬 09. Quy Chuẩn Tương Tác Windows Registry & Win32 Interop (Registry & OS Interop Rules)

> [⬅️ 08. Đóng Gói & CI/CD](08_RELEASE_PACKAGING_AND_CICD_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 09** • [Quy Chuẩn Tiếp Theo: 10. Quản Lý Sự Cố & Ghi Log ➡️](10_INCIDENT_MANAGEMENT_AND_LOGGING_RULES.md)

---

## 🗂️ 1. Quy Chuẩn Thao Tác Windows Registry An Toàn

WinCare Pro can thiệp vào Registry để tối ưu hệ thống, chỉnh sửa Context Menu, quản lý ứng dụng khởi động và thu dọn tàn dư.

### 🔴 CÁC QUY TẮC BẮT BUỘC:
1. **Bắt buộc sao lưu trước khi ghi/xóa:**
   - Trước khi sửa đổi bất kỳ Key/Value nào trong Registry, **bắt buộc** gọi `RegistryBackupEngine.BackupKey()` để xuất bản sao lưu `.reg` an toàn.
2. **Xác thực quyền & Nhánh Registry (Hive Safety):**
   - Chỉ ghi vào `RegistryHive.CurrentUser` (`HKCU`) hoặc `RegistryHive.LocalMachine` (`HKLM`).
   - Tuyệt đối không xóa toàn bộ một SubKey nhánh gốc hệ thống (`HKCR`, `HKU`, `HKCC`).
3. **Phân biệt 32-bit & 64-bit Registry Views (`RegistryView`):**
   - Khi quét ứng dụng đã cài đặt hoặc khóa khởi động, luôn duyệt cả hai chế độ `RegistryView.Registry64` và `RegistryView.Registry32` (`WOW6432Node`) để không bỏ sót thông tin.

```csharp
// Chuẩn mở khóa Registry an toàn đa kiến trúc:
using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
using var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: false);
```

---

## 🔌 2. Quy Chuẩn P/Invoke & Win32 Native Interop

1. **Khai báo `[LibraryImport]` hoặc `[DllImport]` chuẩn:**
   - Luôn chỉ định rõ `SetLastError = true` và `CharSet = CharSet.Unicode`.
   - Bắt buộc kiểm tra mã lỗi qua `Marshal.GetLastWin32Error()` hoặc `Marshal.GetLastPInvokeErrorMessage()` khi hàm API trả về mã lỗi (`FALSE` hoặc `INVALID_HANDLE_VALUE`).
2. **Quản lý Handle Win32 (SafeHandle & CloseHandle):**
   - Mọi Handle tiến trình hoặc token (`IntPtr`) lấy từ `OpenProcess`, `OpenProcessToken`, `CreateFile` **bắt buộc** phải được đóng qua `CloseHandle` hoặc bọc trong lớp kế thừa `SafeHandle` để triệt tiêu hiện tượng rò rỉ tài nguyên nhân hệ điều hành (Kernel Handle Leak).
3. **Cấu hình Marshal An Toàn:**
   - Các cấu trúc Win32 struct (như `MEMORYSTATUSEX`, `WINTRUST_DATA`, `SHELLEXECUTEINFO`) bắt buộc phải khởi tạo trường kích thước `dwLength = (uint)Marshal.SizeOf<STRUCT>()` trước khi truyền vào hàm gốc.

---

## 📡 3. Quy Chuẩn Truy Vấn WMI / CIM (`System.Management`)

1. **Giải phóng đối tượng WMI:**
   - Mọi truy vấn `ManagementObjectSearcher` và `ManagementObjectCollection` phải được bọc trong khối `using` để giải phóng kết nối COM WMI ngầm.
2. **Timeout & Fallback:**
   - Dịch vụ WMI của Windows (`Winmgmt`) đôi khi bị treo hoặc phản hồi chậm trên các máy tính lỗi. Mọi truy vấn WMI phải có cơ chế Timeout hoặc chạy trong `Task.Run` với `CancellationToken`. Nếu WMI lỗi, phải fallback về phương thức đọc Registry hoặc Performance Counter tương đương.

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

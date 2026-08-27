# 🔒 05. Tiêu Chuẩn Bảo Mật & Cơ Chế An Toàn (Security & Safety Architecture)

WinCare Pro là ứng dụng can thiệp sâu vào hệ điều hành Windows, do đó tiêu chuẩn an toàn **Zero-Bug Hardened** được đặt lên mức ưu tiên cao nhất nhằm loại bỏ triệt để nguy cơ làm hỏng hệ điều hành, rò rỉ dữ liệu hoặc bị lợi dụng tấn công.

---

## 🛡️ 1. Mô Hình Phòng Thủ Đa Tầng (Defense-in-Depth Model)

```mermaid
graph TD
    classDef safe fill:#064e3b,stroke:#059669,stroke-width:2px,color:#fff;
    classDef input fill:#1e1b4b,stroke:#6366f1,stroke-width:2px,color:#fff;
    classDef core fill:#451a03,stroke:#d97706,stroke-width:2px,color:#fff;
    classDef os fill:#701a75,stroke:#ec4899,stroke-width:2px,color:#fff;

    Input["Yêu cầu từ Người Dùng / Giao Diện"]:::input
    Sanitizer["1. InputSanitizer (Lọc Ký Tự Đặc Biệt & Đường Dẫn)"]:::safe
    SafeGuard["2. SafePathGuard (Kiểm Tra Danh Sách Đen & Junction Point)"]:::safe
    SvcSafety["3. ServiceSafetyService (Bảo Vệ Dịch Vụ Cốt Lõi Windows)"]:::safe
    Runner["4. ProcessRunner (ArgumentList - Chống Command Injection)"]:::safe
    OS["5. Thực Thi An Toàn Trên Windows OS"]:::os

    Input --> Sanitizer
    Sanitizer --> SafeGuard
    SafeGuard --> SvcSafety
    SvcSafety --> Runner
    Runner --> OS
```

---

## 🚫 2. Chống Tấn Công Command Injection (`ProcessRunner`)

- **Vị trí tệp:** [Core/Helpers/ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs)
- **Vấn đề an ninh:** Việc nối chuỗi câu lệnh thô (ví dụ: `cmd.exe /c "tool.exe " + userInput`) có nguy cơ bị tấn công chèn mã (Command Injection) nếu người dùng hoặc tên phần mềm chứa các ký tự điều khiển shell như `&`, `|`, `;`, `powershell -enc`, `&&`.
- **Giải pháp trong WinCare Pro:**
  - Tuyệt đối **không** dùng thuộc tính `Arguments` dạng chuỗi ghép.
  - Sử dụng cấu trúc danh sách tham số phân tách an toàn `ProcessStartInfo.ArgumentList`. Mỗi tham số được hệ điều hành Windows truyền trực tiếp vào mảng `argv` của tiến trình con mà không thông qua trình thông dịch shell trung gian.

```csharp
// Chuẩn bảo mật trong WinCare Pro:
var psi = new ProcessStartInfo
{
    FileName = "winget.exe",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};
// Thêm từng tham số độc lập vào ArgumentList
psi.ArgumentList.Add("upgrade");
psi.ArgumentList.Add("--id");
psi.ArgumentList.Add(sanitizedPackageId); // An toàn 100% trước ký tự & hoặc |
psi.ArgumentList.Add("--silent");
```

---

## 🛡️ 3. Ngăn Chặn Xóa Nhầm & Bảo Vệ Dữ Liệu Cá Nhân (`SafePathGuard`)

- **Vị trí tệp:** [Core/Helpers/SafePathGuard.cs](file:///d:/WinCare/Core/Helpers/SafePathGuard.cs)
- **Cơ chế bảo vệ 3 tầng:**

### 3.1. Danh Sách Đen Thư Mục Hệ Thống Cốt Lõi (System Protected Roots)
Tuyệt đối chặn xóa hoặc quét can thiệp vào các thư mục:
- `C:\Windows`, `C:\Windows\System32`, `C:\Windows\SysWOW64`
- `C:\Windows\WinSxS` (Chỉ được dọn qua DISM chính thống)
- `C:\Boot`, `C:\Recovery`, `C:\System Volume Information`
- `C:\Program Files\Windows Defender`

### 3.2. Bảo Vệ Tệp Cơ Sở Dữ Liệu & Thông Tin Đăng Nhập Người Dùng (Credential Shield)
Khi quét dọn tệp rác trình duyệt hoặc ứng dụng, `SafePathGuard` tự động loại trừ các tệp chứa session/cookies/tokens:
- `Login Data`, `Login Data For Account` (Mật khẩu trình duyệt)
- `Web Data` (Dữ liệu tự điền autofill)
- `Cookies`, `Cookies-journal`
- `Local State` (Chứa khóa giải mã DPAPI Master Key của trình duyệt Chromium)
- Tệp SAM, SYSTEM, SECURITY trong `%windir%\System32\config`

### 3.3. Phát Hiện và Bỏ Qua Junction Points / Symlinks
Ngăn chặn kỹ thuật tấn công liên kết chéo (Directory Traversal via Reparse Points):
```csharp
public static bool IsReparsePoint(string path)
{
    var dirInfo = new DirectoryInfo(path);
    return (dirInfo.Attributes & FileAttributes.ReparsePoint) != 0;
}
```
Nếu phát hiện thư mục là Junction Point trỏ tới vị trí khác, động cơ dọn dẹp sẽ lập tức bỏ qua, không xóa đệ quy.

---

## ⚙️ 4. Bảo Vệ Dịch Vụ Cốt Lõi Của Windows (`ServiceSafetyService`)

- **Vị trí tệp:** [Infrastructure/Security/ServiceSafetyService.cs](file:///d:/WinCare/Infrastructure/Security/ServiceSafetyService.cs)
- **Vấn đề:** Nếu người dùng hoặc thuật toán dọn dẹp vô tình tắt các dịch vụ nền tối quan trọng, Windows có thể bị màn hình xanh (BSOD), mất mạng hoặc không thể đăng nhập.
- **Danh sách trắng dịch vụ bất khả xâm phạm (System Core Whitelist):**

| Tên Dịch Vụ (ServiceName) | Tên Hiển Thị | Hậu Quả Nếu Bị Vô Hiệu Hóa |
| :--- | :--- | :--- |
| `RpcSs` | Remote Procedure Call | Sập toàn bộ hệ thống Windows |
| `DcomLaunch` | DCOM Server Process Launcher | Không thể mở bất kỳ ứng dụng nào |
| `SamSs` | Security Accounts Manager | Không thể đăng nhập tài khoản |
| `ProfSvc` | User Profile Service | Lỗi nạp hồ sơ người dùng |
| `gpsvc` | Group Policy Client | Lỗi chính sách bảo mật |
| `BFE` | Base Filtering Engine | Tường lửa ngừng hoạt động |
| `WinDefend` | Microsoft Defender Antivirus | Mất lá chắn diệt virus |
| `CryptSvc` | Cryptographic Services | Không thể cài đặt driver và update |
| `EventLog` | Windows Event Log | Mất nhật ký lỗi hệ thống |

`ServiceSafetyService.IsProtectedService(string serviceName)` sẽ trả về `true` và chặn mọi yêu cầu thay đổi trạng thái của các dịch vụ này.

---

## 🔍 5. Kiểm Tra Chữ Ký Số Tiến Trình (WinTrust API Interop)

- **Vị trí tệp:** [Engines/Repair/SecurityPrivacyEngine.cs](file:///d:/WinCare/Engines/Repair/SecurityPrivacyEngine.cs)
- Hệ thống gọi hàm API Win32 gốc `WinVerifyTrust` từ thư viện `wintrust.dll` để xác thực:
  1. Tệp thực thi (`.exe`, `.dll`, `.sys`) có chứng chỉ số hợp lệ không.
  2. Tệp có bị chỉnh sửa mã nhị phân (Tampered/Corrupted) sau khi ký hay không.
  3. Chứng chỉ có thuộc nhà phát hành đáng tin cậy (Microsoft Corporation, Google, Apple...) hay không.

---

## 🛡️ 6. Quản Lý Quyền Nâng Cao (UAC Elevation & Manifest)

- Ứng dụng khai báo quyền trong [app.manifest](file:///d:/WinCare/app.manifest):
  ```xml
  <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
  ```
- Đảm bảo toàn bộ quyền can thiệp phần cứng (S.M.A.R.T, SFC, DISM, Registry HKLM, Network Interface Reset) được thực thi mà không gặp lỗi cấp quyền `Access Denied`.

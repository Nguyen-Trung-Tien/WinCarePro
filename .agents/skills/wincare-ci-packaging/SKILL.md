---
name: wincare-ci-packaging
description: >-
  Build, publish, and packaging guidelines for WinCare Pro. Covers compiling with dotnet build, running publish.bat for Self-Contained x64 distribution, building the Inno Setup installer via publish_installer.bat, versioning via AppConstants, and pre-release smoke test verification. Use whenever compiling, publishing, releasing, or verifying build outputs.
---

# 📦 WinCare Pro — Đóng Gói, Phát Hành & Tự Động Hóa Bản Build (CI/Packaging)

Tài liệu này chuẩn hóa quy trình biên dịch, đóng gói phân phối và tạo bộ cài đặt Production cho **WinCare Pro Suite v5.0.0 (Polaris)** trên nền tảng Windows 10/11 x64.

---

## ⚡ 1. Các Lệnh Vận Hành Chuẩn (Command Reference)

| Mục tiêu | Lệnh thực thi | Ghi chú |
| :--- | :--- | :--- |
| **Biên dịch kiểm tra cú pháp** | `dotnet build WinCarePro.csproj -c Debug -v q --nologo` | Chạy nhanh, triệt tiêu log thừa (Rule 11) |
| **Chạy toàn bộ bài kiểm thử** | `dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --logger "console;verbosity=minimal"` | Yêu cầu đạt 100% Passed (465 tests) |
| **Đóng gói Self-Contained** | `.\publish.bat` | Tạo thư mục chạy trực tiếp x64 không cần cài .NET runtime rời |
| **Tạo bộ cài đặt Installer** | `.\publish_installer.bat` | Biên dịch Inno Setup Script tạo file Setup.exe |

---

## 🏷️ 2. Quy Chuẩn Phiên Bản Duy Nhất (Single Source of Truth - SSOT)

Mọi thông số phiên bản phần mềm **bắt buộc** chỉ được định nghĩa tại một nơi duy nhất: [Core/AppConstants.cs](file:///d:/WinCare/Core/AppConstants.cs).

```csharp
public static class AppConstants
{
    public const string AppName = "WinCare Pro";
    public const string Version = "5.0.0";
    public const string BuildNumber = "2026.10";
    public const string Codename = "Polaris";
    public const string Copyright = "© 2026 WinCare Corporation";
}
```

Tuyệt đối không hardcode số phiên bản rời rạc trong các View XAML hoặc chuỗi hiển thị khác.

---

## 🚀 3. Quy Trình Đóng Gói Self-Contained x64 (`publish.bat`)

Khi chạy `publish.bat`, script thực hiện các bước:
1. Dọn dẹp thư mục `bin\`, `obj\` cũ.
2. Publish .NET 10.0 x64 Self-Contained:
   ```powershell
   dotnet publish WinCarePro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ./publish/
   ```
3. Sao chép các tệp tài nguyên:
   - Thư mục `Assets/` (icons, images, logos).
   - Thư mục `Assets/Aura/` (shaders, backdrop textures).
   - Thư viện native WinAppSDK và SQLite3.

---

## 🛠️ 4. Quy Trình Đóng Gói Bộ Cài Đặt Inno Setup (`publish_installer.bat`)

File installer được tạo thông qua Inno Setup Compiler (`ISCC.exe`):
1. Yêu cầu quyền Administrator khi cài đặt để đăng ký Windows Services và Task Scheduler.
2. Tự động kiểm tra phiên bản Windows (yêu cầu tối thiểu Windows 10 Build 19041 x64).
3. Đăng ký thông tin gỡ cài đặt sạch sẽ trong Windows Registry (`Uninstall` hive).
4. Tạo shortcut ngoài Desktop và trong Start Menu.

---

## ✅ 5. Bản Kiểm Kê Trước Khi Phát Hành (Pre-Release Checklist)

Trước khi bàn giao bản build cho người dùng hoặc release lên server:
- [ ] **1. Build sạch:** `dotnet build -c Release` không phát sinh lỗi hoặc cảnh báo mới.
- [ ] **2. 100% Test Passed:** Toàn bộ 465 test xUnit chạy thành công `0 Failed`.
- [ ] **3. Đồng bộ i18n:** Kiểm tra các chuỗi mới có đủ cả `vi-VN` và `en-US`.
- [ ] **4. Native DLLs:** Xác nhận file `Microsoft.ui.xaml.dll`, `sqlite3.dll` có mặt trong thư mục `./publish/`.
- [ ] **5. Khởi động lạnh (Cold Start):** Thời gian khởi động ứng dụng trên máy thật $\le 350\text{ ms}$, RAM tiêu thụ ban đầu $\le 85\text{ MB}$.

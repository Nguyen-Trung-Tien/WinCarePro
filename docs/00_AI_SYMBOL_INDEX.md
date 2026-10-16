# 🧭 WinCare Pro — AI Symbol Index & Architecture Map (Bản Đồ Ký Hiệu & Kiến Trúc)
> **Mục tiêu:** Cung cấp bản đồ tra cứu ký hiệu tức thì $O(1)$ cho AI Agent.  
> **Nguyên tắc:** Ngăn chặn tuyệt đối ảo giác chữ ký (Zero-Hallucination), triệt tiêu việc phải đọc quét hàng chục file mã nguồn.

---

## 🏛️ 1. Cấu Trúc Phân Tầng & Bảng Tra Cứu Ký Hiệu Cốt Lõi (Core Index)

### 🛡️ Khiên Bảo Vệ Hệ Thống (Core Safety Guards & Interop)
| Lớp / Ký hiệu | Đường dẫn tập tin | Chức năng & Phương thức chính |
| :--- | :--- | :--- |
| `SafePathGuard` | [Core/Helpers/SafePathGuard.cs](file:///d:/WinCare/Core/Helpers/SafePathGuard.cs) | `bool IsSafeToDelete(string path)`: Chống xóa thư mục gốc, hệ thống, Windows, Reparse Points/Symlinks. |
| `SafeRegistryGuard` | [Core/Helpers/SafeRegistryGuard.cs](file:///d:/WinCare/Core/Helpers/SafeRegistryGuard.cs) | `bool IsSafeToDeleteKey(RegistryKey root, string subKey)`: Ngăn chặn xóa Root Hive, độ sâu $\ge 3$, danh sách whitelist an toàn. |
| `ServiceSafetyService` | [Core/Helpers/ServiceSafetyService.cs](file:///d:/WinCare/Core/Helpers/ServiceSafetyService.cs) | `bool IsProtectedService(string serviceName)`: Bảo vệ 45+ Windows Service sống còn (`RpcSs`, `WinDefend`, `CryptSvc`,...). |
| `ProcessRunner` | [Core/Helpers/ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs) | `Task<ProcessResult> RunAsync(string exe, IEnumerable<string> args, CancellationToken ct)`: Chống Command Injection, dùng `ArgumentList`. |
| `CryptoHelper` | [Core/Helpers/CryptoHelper.cs](file:///d:/WinCare/Core/Helpers/CryptoHelper.cs) | `string Protect(string plainText)`, `string Unprotect(string cipherText)`: Mã hóa bảo mật bằng Windows DPAPI. |
| `InputSanitizer` | [Core/Helpers/InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs) | `string SanitizeFileName(string input)`, `string SanitizeCliArg(string input)`: Chuẩn hóa và làm sạch chuỗi đầu vào. |

### 🧩 Mô Hình Cơ Sở & Kết Quả (Base Models & Helpers)
| Lớp / Ký hiệu | Đường dẫn tập tin | Chức năng & Phương thức chính |
| :--- | :--- | :--- |
| `OperationResult`, `OperationResult<T>` | [Core/Models/OperationResult.cs](file:///d:/WinCare/Core/Models/OperationResult.cs) | `Ok()`, `Ok(T data)`, `Fail(string msg)`, `Fail(Exception ex)`: Mẫu kết quả chuẩn hóa mọi phương thức có nguy cơ ngoại lệ. |
| `ViewModelBase` | [Core/Helpers/ViewModelBase.cs](file:///d:/WinCare/Core/Helpers/ViewModelBase.cs) | Kế thừa `ObservableObject`. Chứa `RunOnUI(Action)`, `IsBusy`, `OperationState`, `StatusMessage`. Bắt buộc cho mọi ViewModel. |
| `AppConstants` | [Core/AppConstants.cs](file:///d:/WinCare/Core/AppConstants.cs) | Single Source of Truth (SSOT) cho tên app, phiên bản (`5.0.0`), codename (`Polaris`), build date, bản quyền. |
| `UndoManager` | [Core/Helpers/UndoManager.cs](file:///d:/WinCare/Core/Helpers/UndoManager.cs) | Quản lý lịch sử Undo, khôi phục registry backup và file backup an toàn. |

### 🗄️ Hạ Tầng & Dữ Liệu (Infrastructure & Data)
| Lớp / Ký hiệu | Đường dẫn tập tin | Chức năng & Phương thức chính |
| :--- | :--- | :--- |
| `DbManager` | [Infrastructure/Database/DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs) | Quản lý SQLite WAL. Mọi query bắt buộc bọc trong `lock (_dbLock)`. Chứa `SaveAuditRecord()`, `GetHistory()`, `SaveSetting()`. |
| `CrashLogger` | [Infrastructure/Logging/CrashLogger.cs](file:///d:/WinCare/Infrastructure/Logging/CrashLogger.cs) | `LogError(Exception ex, string context)`: Ghi nhận sự cố và dump lỗi không nuốt ngoại lệ. |
| `AppLogger` | [Infrastructure/Logging/AppLogger.cs](file:///d:/WinCare/Infrastructure/Logging/AppLogger.cs) | Ghi nhận telemetry và log nghiệp vụ thường quy theo cấp độ (Info, Warn, Error). |

### 🌐 Dịch Vụ Hệ Thống (Services)
| Lớp / Ký hiệu | Đường dẫn tập tin | Chức năng & Phương thức chính |
| :--- | :--- | :--- |
| `TranslationManager` | [Services/TranslationService/TranslationManager.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.cs) | Singleton i18n đa ngôn ngữ. Quản lý từ điển qua [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs). |
| `ThemeManager` | [Services/ThemeService/ThemeManager.cs](file:///d:/WinCare/Services/ThemeService/ThemeManager.cs) | Quản lý giao diện Aura Glass (Light, Dark, Windows Default, Mica Alt backdrop). |
| `IconCacheService` | [Services/IconCacheService.cs](file:///d:/WinCare/Services/IconCacheService.cs) | Cache icon ứng dụng Win32/UWP tránh nghẽn I/O và rò rỉ GDI handle. |
| `NavigationService` | [Services/NavigationService.cs](file:///d:/WinCare/Services/NavigationService.cs) | Điều hướng trang giữa các phân hệ WinUI 3. |
| `UpdateManager` | [Services/UpdateManager.cs](file:///d:/WinCare/Services/UpdateManager.cs) | Kiểm tra cập nhật ứng dụng tự động, xác thực payload qua `UpdateSecurityValidator`. |

---

## ⚙️ 2. Danh Mục Động Cơ Nghiệp Vụ (Engines Index)

Tất cả các Engine độc lập 100% với WinUI 3, nhận `CancellationToken ct = default`, trả về `OperationResult<T>`:

| Động Cơ (Engine) | Đường dẫn tập tin | Nghiệp vụ cốt lõi |
| :--- | :--- | :--- |
| `JunkCleanerEngine` | [Engines/JunkCleanerEngine.cs](file:///d:/WinCare/Engines/JunkCleanerEngine.cs) | Quét và dọn rác hệ thống (Temp, Prefetch, Log, Dump), bọc qua `SafePathGuard`. |
| `RegistryEngine` | [Engines/RegistryEngine.cs](file:///d:/WinCare/Engines/RegistryEngine.cs) | Quét và tối ưu Registry, sao lưu tự động qua `RegistryBackupEngine`, bảo vệ qua `SafeRegistryGuard`. |
| `SystemOptimizerEngine`| [Engines/SystemOptimizerEngine.cs](file:///d:/WinCare/Engines/SystemOptimizerEngine.cs)| Tinh chỉnh hiệu năng OS, quản lý Visual Effects, cấu hình mạng và dịch vụ. |
| `UninstallEngine` | [Engines/UninstallEngine.cs](file:///d:/WinCare/Engines/UninstallEngine.cs) | Quét danh sách phần mềm cài đặt, gỡ cài đặt sạch sâu kèm quét tàn dư file/reg. |
| `NetworkCenterEngine` | [Engines/NetworkCenterEngine.cs](file:///d:/WinCare/Engines/NetworkCenterEngine.cs) | Đo tốc độ mạng, flush DNS, reset TCP/IP stack, tối ưu MTU qua `ProcessRunner`. |
| `StartupEngine` | [Engines/StartupEngine.cs](file:///d:/WinCare/Engines/StartupEngine.cs) | Quản lý tác vụ khởi động cùng Windows (Registry Run, Startup Folder, Task Scheduler). |
| `SystemRepairEngine` | [Engines/SystemRepairEngine.cs](file:///d:/WinCare/Engines/SystemRepairEngine.cs) | Sửa lỗi hệ thống, kích hoạt SFC (`sfc /scannow`), DISM (`DISM /Online /Cleanup-Image /RestoreHealth`). |
| `SecurityCenterEngine`| [Engines/SecurityCenterEngine.cs](file:///d:/WinCare/Engines/SecurityCenterEngine.cs)| Kiểm tra trạng thái Windows Defender, Firewall, UAC, BitLocker, Windows Update. |
| `AiWinCareEngine` | [Engines/AiWinCareEngine.cs](file:///d:/WinCare/Engines/AiWinCareEngine.cs) | Lõi phân tích thông minh, dự đoán nguy cơ suy giảm hiệu năng và đề xuất tối ưu. |
| `AiDiagnosticsEngine` | [Engines/AiDiagnosticsEngine.cs](file:///d:/WinCare/Engines/AiDiagnosticsEngine.cs)| Chẩn đoán chi tiết log hệ thống và phần cứng thông qua mô hình phân tích logic. |

---

## 🖥️ 3. Danh Mục 14 Phân Hệ Giao Diện (Modules: ViewModel & View)

| Phân hệ (Module) | ViewModel | View (XAML) |
| :--- | :--- | :--- |
| **Dashboard** | [Modules/Dashboard/DashboardViewModel.cs](file:///d:/WinCare/Modules/Dashboard/DashboardViewModel.cs) | [Modules/Dashboard/DashboardView.xaml](file:///d:/WinCare/Modules/Dashboard/DashboardView.xaml) |
| **Junk Cleaner** | [Modules/JunkCleaner/JunkCleanerViewModel.cs](file:///d:/WinCare/Modules/JunkCleaner/JunkCleanerViewModel.cs) | [Modules/JunkCleaner/JunkCleanerView.xaml](file:///d:/WinCare/Modules/JunkCleaner/JunkCleanerView.xaml) |
| **Registry** | [Modules/Registry/RegistryViewModel.cs](file:///d:/WinCare/Modules/Registry/RegistryViewModel.cs) | [Modules/Registry/RegistryView.xaml](file:///d:/WinCare/Modules/Registry/RegistryView.xaml) |
| **System Optimizer**| [Modules/SystemOptimizer/SystemOptimizerViewModel.cs](file:///d:/WinCare/Modules/SystemOptimizer/SystemOptimizerViewModel.cs) | [Modules/SystemOptimizer/SystemOptimizerView.xaml](file:///d:/WinCare/Modules/SystemOptimizer/SystemOptimizerView.xaml) |
| **Startup Manager** | [Modules/StartupManager/StartupManagerViewModel.cs](file:///d:/WinCare/Modules/StartupManager/StartupManagerViewModel.cs) | [Modules/StartupManager/StartupManagerView.xaml](file:///d:/WinCare/Modules/StartupManager/StartupManagerView.xaml) |
| **Uninstall** | [Modules/Uninstall/UninstallViewModel.cs](file:///d:/WinCare/Modules/Uninstall/UninstallViewModel.cs) | [Modules/Uninstall/UninstallView.xaml](file:///d:/WinCare/Modules/Uninstall/UninstallView.xaml) |
| **System Repair** | [Modules/Repair/SystemRepairViewModel.cs](file:///d:/WinCare/Modules/Repair/SystemRepairViewModel.cs) | [Modules/Repair/SystemRepairView.xaml](file:///d:/WinCare/Modules/Repair/SystemRepairView.xaml) |
| **Security Center**| [Modules/Security/SecurityCenterViewModel.cs](file:///d:/WinCare/Modules/Security/SecurityCenterViewModel.cs) | [Modules/Security/SecurityCenterView.xaml](file:///d:/WinCare/Modules/Security/SecurityCenterView.xaml) |
| **Network Center** | [Modules/Network/NetworkCenterViewModel.cs](file:///d:/WinCare/Modules/Network/NetworkCenterViewModel.cs) | [Modules/Network/NetworkCenterView.xaml](file:///d:/WinCare/Modules/Network/NetworkCenterView.xaml) |
| **AI Assistant** | [Modules/AiAssistant/AiAssistantViewModel.cs](file:///d:/WinCare/Modules/AiAssistant/AiAssistantViewModel.cs) | [Modules/AiAssistant/AiAssistantView.xaml](file:///d:/WinCare/Modules/AiAssistant/AiAssistantView.xaml) |
| **Desktop Widget** | [Modules/DesktopWidget/DesktopWidgetViewModel.cs](file:///d:/WinCare/Modules/DesktopWidget/DesktopWidgetViewModel.cs) | [Modules/DesktopWidget/DesktopWidgetWindow.xaml](file:///d:/WinCare/Modules/DesktopWidget/DesktopWidgetWindow.xaml) |
| **Notifications** | [Modules/Notifications/NotificationCenterViewModel.cs](file:///d:/WinCare/Modules/Notifications/NotificationCenterViewModel.cs)| [Modules/Notifications/NotificationCenterView.xaml](file:///d:/WinCare/Modules/Notifications/NotificationCenterView.xaml)|
| **Settings** | [Modules/Settings/SettingsViewModel.cs](file:///d:/WinCare/Modules/Settings/SettingsViewModel.cs) | [Modules/Settings/SettingsView.xaml](file:///d:/WinCare/Modules/Settings/SettingsView.xaml) |
| **Updates** | [Modules/Updates/UpdateViewModel.cs](file:///d:/WinCare/Modules/Updates/UpdateViewModel.cs) | [Modules/Updates/UpdateView.xaml](file:///d:/WinCare/Modules/Updates/UpdateView.xaml) |

---

## 🧪 4. Danh Mục Kiểm Thử (Unit Tests Index)

Các bài test kiểm thử nằm tại [WinCarePro.Tests/](file:///d:/WinCare/WinCarePro.Tests):
- **Quy chuẩn SQLite Test:** Các test tác động database bắt buộc dùng thuộc tính:
  ```csharp
  [Collection(nameof(DatabaseTestCollection))]
  ```
  nhằm ngăn chặn va chạm truy cập file SQLite `lock (_dbLock)` khi chạy song song.
- **Lệnh chạy kiểm thử có định hướng (AI Standard):**
  ```powershell
  dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TênModule" --logger "console;verbosity=minimal"
  ```

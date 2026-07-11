# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.3 — Tái cấu trúc toàn diện & Tối ưu hóa kiến trúc mã nguồn

> **Phát hành:** 11/07/2026 · **Loại:** Bản cập nhật kiến trúc & Tối ưu hóa hiệu năng (Refactoring & Architecture Update) · **Phiên bản trước:** v3.4.2 (Đã loại bỏ)

Bản cập nhật **v3.4.3** tập trung vào việc tái cấu trúc cấu trúc mã nguồn toàn diện của WinCare Pro, phân tách cấu trúc tệp tin lớn thành các lớp phân phần (partial classes) và chuẩn hóa dữ liệu mô hình (Data Models) riêng biệt. Điều này giúp nâng cao đáng kể khả năng bảo trì, tốc độ phản hồi chẩn đoán hệ thống và sự ổn định của ứng dụng.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.4.3 (Key Features)

#### 1. 🏗️ Tái cấu trúc và Mô-đun hóa mã nguồn (Comprehensive Codebase Refactoring)
* **Tách nhỏ cấu trúc dữ liệu:** Xóa bỏ tệp tin nguyên khối `DataModels.cs` lỗi thời và chuyển đổi sang các mô hình dữ liệu chuyên biệt dưới thư mục `Core/Models/` (bao gồm `ProcessInfo`, `HardwareSpecs`, `JunkModels`, `DriverInfo`, `SoftwareUpdateInfo`...).
* **Phân rã ViewModels & Code-behind:** Phân chia các lớp giao diện lớn như `MainWindow.xaml.cs`, `DashboardViewModel.cs`, và `NetworkViewModel.cs` thành các tệp lớp phân phần (partial class) để tối ưu hóa quản lý mã nguồn.
* **Tách hệ thống bản dịch:** Di chuyển toàn bộ dữ liệu bản dịch lớn ra khỏi lớp logic cốt lõi của `TranslationManager.cs` sang tệp cấu hình bản dịch chuyên dụng `TranslationManager.Translations.cs`.

#### 2. ⚡ Tối ưu hóa & Chuyên biệt hóa các Động cơ giám sát (Engine & Performance Optimizations)
* **Giám sát Mạng (Network Engine):** Tách biệt logic và bổ sung các mô-đun mới như đo tốc độ internet (`NetworkEngine.SpeedTest.cs`), chẩn đoán sửa lỗi DNS (`NetworkEngine.DnsRepair.cs`, `NetworkEngine.DnsBenchmark.cs`), và sửa lỗi card mạng (`NetworkEngine.Repair.cs`).
* **Trình gỡ cài đặt ứng dụng (Uninstall Engine):** Phân mảnh mã nguồn gỡ cài đặt thành các phần quét ứng dụng nâng cao (`UninstallEngine.Scanning.cs`) và xử lý tàn dư tập tin (`UninstallEngine.Leftovers.cs`).
* **Tiện ích hệ thống (WmiHelper):** Bổ sung tiện ích `WmiHelper.cs` giúp việc truy vấn cơ sở dữ liệu WMI của Windows diễn ra mượt mà và an toàn hơn, hạn chế tối đa rò rỉ tài nguyên hệ thống.

#### 3. 🛡️ Tối ưu hóa bộ nhớ và độ ổn định (Stability & Memory Cleanup)
* **Dọn dẹp tài nguyên:** Cải tiến hiệu suất truy vấn tiến trình nền và giải phóng tài nguyên CPU/RAM khi chuyển đổi giữa các tab chức năng.
* **Cải thiện Logging & Cache:** Nâng cấp cơ chế ghi nhật ký hoạt động (`AuditLogService.cs`) và bộ nhớ đệm biểu tượng ứng dụng (`IconCacheService.cs`).

---

### 🩹 Chi tiết kỹ thuật & Thay đổi cấu hình
* Cập nhật phiên bản toàn hệ thống lên `3.4.3` tại các tệp tin cấu hình (`WinCarePro.csproj`, `Package.appxmanifest`, `update.json`, `MainWindow.xaml`, `setup.iss`).

---
<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
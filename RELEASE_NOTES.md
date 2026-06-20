# 📝 Nhật ký Phát hành (Release Notes) - WinCare Pro v1.0.2

Chào mừng bạn đến với phiên bản **WinCare Pro v1.0.2**! Đây là một bản cập nhật quan trọng mang đến sự lột xác về mặt thị giác với hệ thống nhận diện thương hiệu mới, giao diện người dùng (UI) được tinh chỉnh theo phong cách cao cấp, cùng hàng loạt cải tiến cốt lõi về hiệu năng và độ ổn định của hệ thống.

---

## 🚀 Điểm nổi bật (Highlights)

* **Thiết kế lại Logo & Bộ nhận diện:** Thay đổi toàn bộ các biểu tượng ứng dụng sang phong cách tối giản, hiện đại và tối ưu hiển thị sắc nét trên thanh Taskbar và Desktop của Windows mà không bị nhòe hay tối màu.
* **Tối ưu hóa hiệu năng theo dõi CPU/RAM:** Loại bỏ hoàn toàn cơ chế truy vấn WMI chậm chạp, chuyển sang sử dụng trực tiếp các hàm API gốc của Windows (`GetSystemTimes`, `GlobalMemoryStatusEx`), giúp giảm tải CPU của chính ứng dụng xuống gần 0%.
* **Quy trình tối ưu hóa một chạm toàn diện:** Nâng cấp tính năng **Optimize Now** trên màn hình chính thực hiện đồng thời 6 bước tối ưu hóa: dọn dẹp rác, xóa bộ nhớ đệm Windows Update, sửa lỗi Registry, giải phóng RAM thông minh, dọn bộ đệm DNS và áp dụng tinh chỉnh hệ thống.
* **Tải và nâng cấp ứng dụng ổn định hơn:** Khắc phục triệt để các lỗi treo vô hạn khi quét nâng cấp phần mềm bằng cách áp dụng cơ chế giới hạn thời gian (Timeout) và chạy nâng cấp ứng dụng hệ thống với quyền Quản trị viên (Administrator).

---

## 🔍 Chi tiết các thay đổi (Detailed Changelog)

### 🎨 1. Giao diện & Trải nghiệm Người dùng (UI/UX)
* **Cập nhật Assets Đồ họa:**
  * Thiết kế lại icon ứng dụng chính (`AppIcon.ico`) tối giản và cao cấp hơn, dung lượng tệp được tối ưu hóa từ 138 KB xuống còn 83 KB.
  * Cập nhật đồng bộ các hình ảnh màn hình chào (`SplashScreen`), logo ô vuông (`Square150x150`, `Square44x44`), và Store Logo để đảm bảo độ sắc nét trên mọi mật độ điểm ảnh (HiDPI / Scale 200%).
* **Cải tiến Hệ thống Theme & Style (`App.xaml`):**
  * Thêm dải màu Gradient thương hiệu (`PrimaryAccentGradient`) và hiệu ứng viền kính mờ (`GlassBorderBrush`).
  * Định nghĩa các dải màu hiển thị trạng thái sức khỏe hệ thống: Tốt (Xanh lá), Cảnh báo (Vàng), Nguy hiểm (Đỏ).
  * Tạo bộ màu chuyên biệt cho từng thành phần phần cứng (CPU: Vàng, RAM: Xanh dương, GPU: Tím, Disk: Xanh lá).
  * Bổ sung các kiểu khung thẻ bo góc hiện đại (`ModernCardStyle`, `GradientBorderCardStyle`, `GlowCardStyle`).
  * Thiết kế giao diện Terminal giả lập chuyên nghiệp cho nhật ký hệ thống (`DarkTerminalStyle`).
* **Làm mới Bố cục các Trang chức năng:**
  * Đồng bộ hóa và nâng cấp giao diện của toàn bộ 13 trang chức năng trong ứng dụng, bao gồm: `DashboardPage`, `DiskPage`, `HardwarePage`, `JunkPage`, `LogsPage`, `NetworkPage`, `RegistryBackupPage`, `RepairPage`, `SecurityPrivacyPage`, `SettingsPage`, `StartupPage`, `SystemOptimizerPage`, và `UpdaterPage`.

### ⚡ 2. Tối ưu hóa Hiệu năng (Performance Optimization)
* **Đọc chỉ số hệ thống thời gian thực:**
  * Sử dụng P/Invoke gọi hàm `GetSystemTimes` để tính toán chính xác phần trăm sử dụng CPU.
  * Sử dụng struct `MEMORYSTATUSEX` và hàm `GlobalMemoryStatusEx` để đọc thông số RAM thay thế cho truy vấn WMI.
* **Chạy tác vụ nền không gây đơ ứng dụng (Asynchronous Execution):**
  * Chuyển toàn bộ tiến trình quét và dọn dẹp rác (`JunkCleanerEngine`) vào luồng chạy ngầm riêng biệt (`Task.Run`), giúp thanh tiến trình chạy mượt mà và giao diện không bị hiện tượng "Not Responding".
  * Áp dụng cơ chế tương tự cho các thao tác tối ưu hóa hệ thống khác.

### 🛠️ 3. Cải tiến Công cụ Hệ thống & Sửa lỗi (Core Engines & Bug Fixes)
* **Trình quản lý tiến trình (`ProcessService.cs`):**
  * Sử dụng hàm Win32 API `QueryFullProcessImageName` với cờ `PROCESS_QUERY_LIMITED_INFORMATION`.
  * Nhờ đó, ứng dụng có thể lấy được đường dẫn thực thi và tên nhà phát hành của các tiến trình hệ thống hoặc tiến trình chạy quyền cao mà không bị lỗi từ chối truy cập (Access Denied).
* **Công cụ cập nhật phần mềm (`SoftwareUpdaterEngine.cs`):**
  * Thiết lập giới hạn thời gian chờ tối đa (Timeout) để tránh ứng dụng bị treo vô hạn nếu tiến trình Winget hoặc file Setup cài đặt ngầm bị kẹt:
    * Quét Winget: Hạn giờ 15 giây.
    * Nâng cấp Winget: Hạn giờ 120 giây.
    * Trình cài đặt Setup: Hạn giờ 180 giây.
  * Cấu hình tham số nâng cấp ứng dụng Winget chạy ẩn và yêu cầu quyền Administrator (`Verb = "runas"`, `UseShellExecute = true`), giúp nâng cấp thành công các phần mềm hệ thống.
* **Bộ sửa lỗi Windows Update (`SystemEngine.cs`):**
  * Thêm bước dọn dẹp và đổi tên thư mục dữ liệu chứng thực chữ ký số mạng `C:\Windows\System32\catroot2` thành `catroot2.old` song song với thư mục `SoftwareDistribution`, nâng cao tỷ lệ sửa lỗi Windows Update thành công.
* **Bảo mật & Quyền riêng tư (`SecurityPrivacyEngine.cs`):**
  * Sửa lỗi logic ngược khi đọc/ghi giá trị Registry `RestrictImplicitConsent` thuộc tính năng chặn theo dõi hành vi nhập liệu của Microsoft (`InputPersonalization`). Giờ đây, trạng thái bật/tắt hiển thị chính xác theo thiết lập người dùng.

### 📦 4. Đóng gói & Phát hành (Deployment)
* **Cập nhật phiên bản đồng bộ:**
  * Nâng phiên bản ứng dụng lên `1.0.2` trong tệp cấu hình dự án `WinCarePro.csproj`.
  * Đồng bộ thông tin phiên bản trong kịch bản đóng gói Inno Setup `setup.iss`.
  * Cập nhật siêu dữ liệu trong tệp kiểm tra cập nhật tự động `update.json`.
* **Tối ưu hóa Script tự động build:**
  * Tinh chỉnh cú pháp tệp `publish_installer.bat` và `publish.bat` giúp lập trình viên đóng gói nhanh ứng dụng chỉ bằng 1 cú nhấp chuột.

---

## 💾 Hướng dẫn Cập nhật lên v1.0.2

1. Khởi động ứng dụng **WinCare Pro** hiện tại của bạn.
2. Di chuyển đến mục **Cài đặt (Settings)** hoặc **Cập nhật (Update)**.
3. Hệ thống sẽ tự động phát hiện phiên bản mới `1.0.2` thông qua tệp `update.json`.
4. Nhấn **Cập nhật ngay** để tải xuống bộ cài đặt `WinCareProSetup.exe` và tiến hành nâng cấp tự động.
5. *Hoặc* bạn có thể tải trực tiếp file cài đặt mới nhất từ trang [Releases của WinCarePro](https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/tag/v1.0.2).

---
*Cảm ơn bạn đã tin dùng WinCare Pro! Chúng tôi luôn nỗ lực mang lại trải nghiệm chăm sóc máy tính Windows tốt nhất cho bạn.*

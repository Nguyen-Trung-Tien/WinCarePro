# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.9 — Tái cấu trúc Architecture, Nâng cấp Fluent UI & Hiệu năng Hệ thống

> **Phát hành:** 02/08/2026 · **Loại:** Bản nâng cấp Kiến trúc, Giao diện & Tối ưu Hiệu năng (Major Architecture & UI/UX Update)

Bản cập nhật **v3.4.9** mang đến bước tiến lớn về kiến trúc ứng dụng với thiết kế UI mô-đun hóa hoàn chỉnh, nâng cấp trải nghiệm thị giác Fluent UI 3D với hiệu ứng kính mờ (Mica/Acrylic), tối ưu hóa bộ đo tốc độ mạng Network SpeedTest, cải tiến công cụ SQLite DBManager và tăng cường tính ổn định toàn bộ hệ thống.

---

### ✨ Các tính năng & cải tiến nổi bật trong phiên bản v3.4.9 (Key Features)

* **🖥️ Kiến trúc UI Mô-đun hóa & Giao diện Fluent UI 3D Hiện đại:**
  * Tái cấu trúc toàn bộ ứng dụng theo mô hình trang chuyên biệt (`Dashboard`, `Junk Cleaner`, `App Uninstall`, `Disk Tools`, `System Optimizer`, `Registry`, `Security`, `Startup`, `Repair`, `Network`, `Updater`, `Settings`).
  * Tích hợp cơ chế chuyển đổi chủ đề (Dark / Light Theme Tokens) mượt mà, phản hồi tức thì và không bị nháy giao diện.
  * Tối ưu hóa hiệu ứng kính mờ Win32 Backdrop (Mica / Acrylic) cùng đường nét thiết kế chuẩn Windows 11.

* **🚀 Công cụ Đo tốc độ Mạng (Network SpeedTest) & Tiến trình Hệ thống:**
  * Nâng cấp `NetworkEngine.SpeedTest` với cơ chế thử nghiệm băng thông kép (Dual-endpoint), xử lý phản hồi chính xác tốc độ Ping, Download và Upload.
  * Tích hợp cơ chế tự động chuyển đổi máy chủ (Fallback mechanism) giúp đo đạc ổn định ngay cả khi mất kết nối máy chủ chính.
  * Tối ưu hóa `ProcessService` theo dõi mức sử dụng tài nguyên tiến trình nền nhẹ nhàng, tiết kiệm CPU và RAM.

* **💾 Tăng cường Độ tin cậy Cơ sở Dữ liệu SQLite (DbManager):**
  * Kích hoạt chế độ WAL (Write-Ahead Logging) nâng cao hiệu năng đọc/ghi nhật ký lịch sử ứng dụng.
  * Tích hợp bộ kiểm thử tự động `DbManagerRegressionTests` phòng ngừa lỗi xung đột dữ liệu đa luồng.

* **🛡️ Bộ cài đặt & Đóng gói Cập nhật Tự động (Installer & Auto-Deploy):**
  * Tối ưu hóa kịch bản Inno Setup (`setup.iss`) & bộ biên dịch tự động (`publish_installer.bat`).
  * Tự động phát hiện và đóng các tiến trình `WinCarePro.exe` đang chạy trước khi cập nhật, đảm bảo quá trình nâng cấp mượt mà không gây đè tệp.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v4.0.0 — Đột Phá Giao Diện Aura Glass, AI Health Copilot Engine & Single-Instance HUD Widget

> **Phát hành:** 04/08/2026 · **Loại:** Bản nâng cấp Toàn diện Giao diện, Trải nghiệm Chuyển động & Tính năng Đột phá (Major Evolution Release)

Bản cập nhật **v4.0.0** đánh dấu bước chuyển mình toàn diện của WinCare Pro với thiết kế giao diện **Aura Glassmorphic Fluent 2.0**, hệ thống chuyển động **Visual Composition 120 FPS**, Trợ lý **AI Health Copilot Engine** dự đoán rủi ro hệ thống, Chế độ **Gaming Turbo 2.0 Mode** tối ưu bộ nhớ RAM tức thì và cửa sổ **Single-Instance Desktop HUD Widget** ghim trên cùng màn hình.

---

### ✨ Các tính năng & cải tiến nổi bật trong phiên bản v4.0.0 (Key Features)

* **🤖 Trợ lý AI Health Copilot Engine (Predictive AI Analytics):**
  * Tích hợp thuật toán AI chẩn đoán sức khỏe PC, dự đoán chính xác số ngày còn lại trước khi ổ C: bị cạn kiệt dung lượng và ước tính số giây khởi động giảm bớt được.
  * Tự động đưa ra các khuyến nghị tối ưu thông minh theo từng cấp độ rủi ro (Critical, High, Medium).

* **⚡ Chế độ Gaming Turbo 2.0 Suite:**
  * Giải phóng bộ nhớ RAM vật lý (Working Set) và nâng ưu tiên tài nguyên CPU cho các ứng dụng / game chỉ với 1-Click.
  * Kích hoạt chế độ năng lượng *Ultimate Performance* tự động.

* **🪟 Cửa sổ Desktop HUD Widget Đơn Thể (Single-Instance TopMost HUD):**
  * Thiết kế cửa sổ Widget mờ kính nhỏ gọn ghim nổi trên màn hình (`IsAlwaysOnTop = true`) hiển thị live CPU, RAM và lưu lượng Mạng (Download/Upload speed).
  * Chuẩn hóa nút bấm ghim và xử lý mở duy nhất 1 cửa sổ đơn thể (Single-Instance Focus) giúp chống trùng lặp màn hình.

* **🌐 Tối Ưu Đa Ngôn Ngữ 100% (Bidirectional Multi-Language Engine):**
  * Nâng cấp cơ chế dịch thuật hai chiều **(English ↔ Vietnamese)** trên toàn bộ 13 phân hệ trang. Dịch chuyển mượt mà 100% theo thời gian thực không cần khởi động lại.

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
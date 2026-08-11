# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v4.1.0 — Phân Hệ System Repair, AI WinCare Engine, Trợ Lý Disk Analyzer & Hạ Tầng CI/CD Tự Động

> **Phát hành:** 11/08/2026 · **Loại:** Bản nâng cấp Chức năng Chuyên sâu (Feature & Reliability Evolution Release)

Bản cập nhật **v4.1.0** mang đến những cải tiến vượt bậc về khả năng chẩn đoán, dọn dẹp và tự động sửa lỗi hệ thống của WinCare Pro. Phiên bản này chính thức ra mắt bộ phân hệ **Sửa lỗi Windows (System Repair Suite)**, phân tích bộ nhớ & tìm tệp trùng lặp (**Disk Analyzer & Duplicate Engine**), trình dọn dẹp đa luồng **JunkCleaner**, trợ lý chẩn đoán **AI WinCare Engine** nâng cấp, trình quản lý **Context Menu** vỏ Windows, cùng hệ thống hướng dẫn **UserGuide** tương tác và hạ tầng tích hợp liên tục **CI/CD Pipelines**.

---

### ✨ Các tính năng & cải tiến đột phá trong phiên bản v4.1.0 (Key Features)

* **🛠️ Phân hệ Sửa Lỗi Hệ Thống Chuyên Sâu (System Repair Suite):**
  * Tích hợp bộ công cụ tự động chẩn đoán và khắc phục sự cố hệ điều hành Windows (`RepairViewModel` / `RepairPage`).
  * Tự động quét và khôi phục các tệp hệ thống hỏng hóc thông qua lệnh SFC (System File Checker) và DISM Component Store Repair.
  * Sửa chữa các lỗi Registry phổ biến, sự cố mất kết nối mạng và khôi phục các dịch vụ hệ thống bị ngưng hoạt động.

* **🤖 Trợ lý AI WinCare Engine Nâng Cấp (Predictive AI Health Analytics):**
  * Động cơ chẩn đoán AI WinCare (`AiHealthEngine`) phân tích telemetry thời gian thực, đo lường áp lực bộ nhớ và cảnh báo sớm nguy cơ cạn kiệt dung lượng đĩa.
  * Tự động xuất báo cáo tối ưu hóa thông minh kèm đề xuất hành động khắc phục cụ thể theo từng mức độ rủi ro.

* **💾 Phân Tích Ổ Đĩa & Tìm Tệp Trùng Lặp (Disk Analyzer & Duplicate Search Engine):**
  * Phân hệ quản lý và phân tích cấu hình đĩa (`DiskEngine` / `DiskPage`) với biểu đồ trực quan hóa dung lượng lưu trữ.
  * Tích hợp thuật toán quét và loại bỏ tệp trùng lặp (`StorageDuplicateModels`), kết hợp tối ưu hóa ổ cứng SSD/HDD (TRIM / Defrag) giúp thu hồi tối đa dung lượng bộ nhớ.

* **🧹 Dọn Dẹp Rác Đa Luồng & Tối Ưu Tiến Trình (Multi-Threaded Junk Cleaner):**
  * Thuật toán dọn dẹp bộ đệm ứng dụng, tệp tạm hệ thống và bộ nhớ đệm trình duyệt đa luồng siêu tốc.
  * Tích hợp bộ hỗ trợ tải giao diện `UiLoadingHelper` và trình ghi nhật ký sự cố `CrashLogger` chống giật lag UI khi quét dung lượng lớn.

* **🪟 Cửa Sổ Desktop HUD Widget Đơn Thể (Single-Instance TopMost HUD):**
  * Cửa sổ nổi mini `DesktopWidgetWindow` mờ kính Aura Glass, hiển thị thời gian thực chỉ số CPU, RAM, Network I/O.
  * Hỗ trợ dọn dẹp nhanh và kích hoạt Turbo Mode trực tiếp từ cửa sổ Widget với cơ chế quản lý Single-Instance tuyệt đối an toàn.

* **🖱️ Quản Lý Shell Extension & Context Menu Windows:**
  * Trang `ContextMenuPage` cho phép người dùng bật/tắt và loại bỏ các mục thừa trong menu chuột phải (Windows Explorer Context Menu) giúp mở file/folder mượt mà hơn.

* **⚡ Bộ Đo Tốc Độ Mạng Đa Luồng (Multi-Threaded Network SpeedTest):**
  * Động cơ đo tốc độ mạng đa luồng (`NetworkEngine.SpeedTest.cs`), đồng bộ hóa chỉ số Ping, Download, Upload chính xác, xử lý triệt để hiện tượng trễ/bất đồng bộ trạng thái quét.

* **📖 Trình Hướng Dẫn Tương Tác & Tối Ưu Đa Ngôn Ngữ (Interactive User Guide System):**
  * Tích hợp giao diện hướng dẫn người dùng trực quan (`UserGuideView`) kèm tài liệu hướng dẫn chi tiết `USER_GUIDE.md`.
  * Mở rộng từ điển dịch thuật song ngữ Anh - Việt (`TranslationManager`), đáp ứng 100% thao tác chuyển đổi ngôn ngữ tức thì.

* **⚙️ Bộ Linh Kiện UI & Hạ Tầng Tự Động Hóa CI/CD:**
  * Thêm các thành phần giao diện mới: `LoadingToggleSwitch`, `SkeletonLoader`, `CircularProgressMeter` tạo phản hồi chuyển động mịn 120 FPS.
  * Tích hợp tự động hóa xây dựng và phát hành GitHub Actions workflows (`ci.yml`, `release.yml`).
  * Đảm bảo độ tin cậy hệ thống với **100% Unit Tests Pass (WinCarePro.Tests)**.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
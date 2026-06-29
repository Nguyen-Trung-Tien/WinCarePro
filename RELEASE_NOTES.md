# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.3.0 — Nâng cấp toàn diện & Tối ưu hóa Hệ thống chuyên sâu

> **Phát hành:** 29/06/2026 · **Loại:** Phiên bản nâng cấp lớn (Major Feature Upgrade) · **Phiên bản trước:** v3.2.0

Bản cập nhật **v3.3.0** đánh dấu một cột mốc quan trọng, hoàn thiện bộ công cụ bảo trì hệ thống toàn diện của **WinCare Pro** với sự nâng cấp mạnh mẽ từ lõi xử lý đến giao diện trực quan Fluent Design. Chúng tôi tập trung cải thiện trải nghiệm người dùng, mở rộng các chức năng chẩn đoán thông minh, dọn dẹp chuyên sâu, và tối ưu hóa hiệu năng hệ thống tuyệt đối.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.3.0 (Key Features)

#### 1. 🧹 Trình Dọn Rác Hệ Thống Chuyên Sâu (Junk Cleaner)
* **Lõi quét cải tiến:** Hỗ trợ quét và dọn dẹp an toàn các tệp tin tạm (System Temp), Nhật ký hệ thống (Windows Log), tệp lỗi bộ nhớ (Memory Dumps), bộ nhớ đệm tối ưu hóa phân phối (Delivery Optimization files) và bộ nhớ đệm hình ảnh (Thumbnail Cache).
* **Biểu đồ dung lượng trực quan:** Tích hợp biểu đồ hình tròn (Pie Chart) hiển thị tỉ lệ phần trăm dung lượng của từng loại rác trên ổ đĩa.
* **Hiệu ứng mượt mà:** Bổ sung thanh tiến trình động và hiệu ứng chuyển đổi mượt mà giữa các trạng thái quét và dọn dẹp.

#### 2. 🔌 Trình Quản Lý & Cập Nhật Driver (Driver Updater & Wizard)
* **Quét Driver phần cứng:** Tự động phát hiện phiên bản driver của các thiết bị phần cứng, firmware hệ thống và thông tin bộ điều khiển bo mạch chủ (Motherboard controllers).
* **Driver Wizard chuyên nghiệp:** Quy trình 3 bước khép kín (Quét firmware phần cứng, tải xuống gói cài đặt nhị phân, và tự động triển khai nâng cấp) giúp việc cập nhật driver an toàn và dễ dàng hơn bao giờ hết.

#### 3. 🛡️ Trung Tâm Bảo Mật & Quyền Riêng Tư (Security Center & Privacy Tuning)
* **Giám sát trạng thái an toàn:** Kiểm tra thời gian thực trạng thái bảo vệ của Windows Defender, Tường lửa (Firewall), và cấp độ kiểm soát tài khoản người dùng UAC.
* **Cấu hình bảo mật nâng cao:** Hỗ trợ tinh chỉnh các quyền riêng tư hệ thống để ngăn chặn rò rỉ thông tin cá nhân và tối ưu hóa bảo mật cục bộ.

#### 4. 🚀 Trình Gỡ Ứng Dụng Nâng Cao (App Uninstaller)
* **Gỡ cài đặt hàng loạt (Batch Uninstall):** Hỗ trợ chọn nhiều ứng dụng để thực hiện gỡ cài đặt đồng thời, tiết kiệm thời gian.
* **Gỡ cài đặt cưỡng bức (Force Uninstall):** Gỡ bỏ triệt để các ứng dụng Windows Store cứng đầu bằng các lệnh PowerShell nâng cao.
* **Làm sạch tàn dư (Leftovers Purger):** Tự động tìm kiếm và xóa sạch các thư mục rác trong `ProgramFiles`, `AppData`, `ProgramData` và các khóa Registry hỏng sau khi gỡ cài đặt phần mềm.

#### 5. ⚙️ Bộ Máy Sửa Lỗi Hệ Thống (System Repair)
* **Quét lỗi nâng cao:** Tích hợp trực tiếp công cụ quét tệp tin hệ thống SFC (`sfc /scannow`) và công cụ sửa lỗi ảnh đĩa DISM (`DISM.exe /Online /Cleanup-Image /RestoreHealth`).
* **UI chia luồng trực quan:** Hiển thị chi tiết từng giai đoạn quét (Chẩn đoán, Khắc phục, Hoàn tất) cùng thanh tiến trình phần trăm cụ thể.

#### 6. 🌐 Giám Sát Mạng Thời Gian Thực (Network Center)
* **Đo lường băng thông:** Hiển thị lưu lượng tải xuống (Download) và tải lên (Upload) qua biểu đồ đường (Line Chart) thời gian thực.
* **Quản lý tiến trình sử dụng mạng:** Liệt kê chi tiết các ứng dụng đang chiếm dụng băng thông nhiều nhất.
* **Chẩn đoán mạng nhanh:** Hỗ trợ đo Ping, kiểm tra Packet Loss, giải phóng/làm mới IP và làm sạch bộ nhớ đệm DNS (Flush DNS) chỉ với một nút bấm.

#### 7. 📊 Bảng Điều Khiển & Quản Lý Tiến Trình (Dashboard & Process Manager)
* **Chỉ số CPU & RAM:** Theo dõi tải hệ thống thời gian thực với các biểu đồ trực quan.
* **Điểm sức khỏe AI (Composite Health Score):** Bộ máy phân tích tự động chấm điểm sức khỏe hệ thống (0 - 100) và đưa ra các khuyến nghị tối ưu hóa thông minh.
* **Quản lý tiến trình chuyên nghiệp:** Liệt kê chi tiết CPU, RAM, số lượng Thread, Handle của từng tiến trình đang chạy và cho phép đóng băng hoặc tắt nhanh các tác vụ bị treo (End Task).

#### 8. ⚡ Tinh Chỉnh Tối Ưu Hóa Hiệu Năng (System Optimizer)
* **Tối ưu hóa Windows Explorer:** Áp dụng các tinh chỉnh dịch vụ để Explorer hoạt động phản hồi nhanh hơn.
* **Chế độ chơi game & làm việc:** Tối ưu hóa cấu hình bộ nhớ đệm hệ thống và các dịch vụ ngầm để đạt hiệu năng phần cứng tối đa.

#### 9. 📂 Quản Lý Khởi Động & Dịch Vụ Hệ Thống (Startup & Services)
* **Tối ưu tốc độ khởi động:** Phân tích ảnh hưởng của các phần mềm tự chạy cùng Windows, hỗ trợ bật/tắt dễ dàng.
* **Quản lý dịch vụ Windows:** Tắt bỏ các dịch vụ Windows ngầm không cần thiết để giải phóng dung lượng RAM.

#### 10. 💾 Sao Lưu Registry & Phân Tích Ổ Cứng (Disk Tools & Registry Backup)
* **Thông số S.M.A.R.T:** Đọc thông tin sức khỏe ổ cứng HDD/SSD để cảnh báo hỏng hóc.
* **Tìm kiếm tệp trùng lặp:** Quét và liệt kê các tệp trùng tên/dung lượng để giải phóng ổ đĩa.
* **Sao lưu Registry:** Tạo các điểm khôi phục nhanh (Restore Point) cho Windows Registry giúp khôi phục hệ thống an toàn khi xảy ra sự cố.

#### 11. ⚙️ Cài Đặt Hệ Thống & Chuyển Đổi Ngôn Ngữ (Settings & Dynamic Translation)
* **Chuyển ngữ động:** Hỗ trợ thay đổi ngôn ngữ giao diện (Tiếng Việt / Tiếng Anh) ngay lập tức mà không cần khởi động lại ứng dụng nhờ module `TranslationManager`.
* **Giao diện sáng/tối:** Hỗ trợ chuyển đổi Theme (Sáng/Tối/Mặc định hệ thống) và thiết lập tần suất quét dọn tự động.

---

### 🩹 Các lỗi đã khắc phục và cải tiến hiệu năng (Bug Fixes & Tweaks)
* Khắc phục hoàn toàn lỗi crash do xung đột luồng khi chuyển trang nhanh trên Dashboard và các thẻ giám sát tài nguyên.
* Khắc phục lỗi hiển thị thống kê Quick Stats bị đặt lại về 0 trong tab App Uninstaller khi quay lại trang.
* Tối ưu hóa bộ nhớ RAM và CPU khi ứng dụng chạy nền giám sát hệ thống.

---
<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.5 — Nâng cấp tính năng & Quản lý nâng cao

> **Phát hành:** 18/07/2026 · **Loại:** Bản cập nhật tính năng & Hiệu năng hệ thống (Feature & Performance Update)

Bản cập nhật **v3.4.5** giới thiệu các tính năng quản lý menu chuột phải hoàn toàn mới, tích hợp cơ chế Bảo mật DNS qua HTTPS (DoH), nâng cấp hệ thống chẩn đoán phần cứng (GPU, CPU, SSD/HDD) và cải tiến toàn diện độ ổn định bằng các tác vụ bất đồng bộ.

---

### ✨ Các tính năng & cải tiến nổi bật trong phiên bản v3.4.5 (Key Features)

#### 1. 🖱️ Quản lý Menu chuột phải (Context Menu Manager)
* **Tối ưu hóa menu chuột phải:** Bổ sung trang quản lý menu chuột phải (Context Menu) cho phép dễ dàng bật/tắt các mục menu thuộc loại All Files, Desktop Background, Folders trực tiếp qua Registry.
* **Nhận diện thông minh:** Tự động giải mã tên hiển thị thân thiện từ Class ID (CLSID) trong registry để người dùng dễ kiểm soát.

#### 2. 🛡️ Bảo mật DNS qua HTTPS (DNS-over-HTTPS - DoH)
* **Bảo mật kết nối:** Tích hợp cấu hình Secure DNS (DoH) trực tiếp trong Network Center, cho phép mã hóa các truy vấn tên miền của hệ thống.
* **Nhiều nhà cung cấp phổ biến:** Hỗ trợ kích hoạt nhanh các nhà cung cấp DoH uy tín như Cloudflare, Google, AdGuard, NextDNS hoặc đặt lại cấu hình DNS tự động.

#### 3. 📊 Chẩn đoán Phần cứng & Hệ thống Nâng cao
* **Giám sát GPU thực tế:** Bổ sung theo dõi hiệu suất sử dụng GPU thực tế thời gian thực thông qua Performance Counters của Windows.
* **Phát hiện thắt cổ chai CPU (Throttling):** Kiểm tra xung nhịp xử lý thực tế so với xung nhịp tối đa để cảnh báo tình trạng giảm hiệu năng do nhiệt độ hoặc năng lượng.
* **Kiểm tra sức khỏe ổ cứng (SSD/HDD Health):** Phân tích dữ liệu SMART và các chỉ số sức khỏe của Windows Storage (`PredictFailure`, `HealthStatus`) để đưa ra phần trăm độ bền ổ đĩa chính xác.
* **Lọc Driver lỗi thời:** Cơ chế kiểm tra driver mới chỉ phát hiện và cảnh báo các driver phần cứng hệ thống quan trọng (Non-Generic/Non-Microsoft) có ngày phát hành cũ hơn 180 ngày.

#### 4. ⚡ Nâng cấp hiệu năng & Khả năng phục hồi của Trình gỡ cài đặt
* **Giao diện không bị đóng băng:** Tối ưu hóa toàn bộ các tác vụ chạy công cụ hệ thống (DISM, SFC, Restore Point, Network Repair) sang chế độ bất đồng bộ (`ProcessRunner`) để giao diện luôn mượt mà.
* **Gỡ cài đặt cưỡng bức (Force Uninstall):** Khi tiến trình gỡ cài đặt tiêu chuẩn gặp lỗi, ứng dụng sẽ cung cấp tùy chọn Force Uninstall để dọn dẹp triệt để ứng dụng (kể cả Microsoft Store) và các tệp rác còn sót lại.
* **Đồng bộ thao tác:** Cải tiến tính năng chọn tất cả ("Select All") cho danh sách ứng dụng và danh sách tệp rác thừa.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
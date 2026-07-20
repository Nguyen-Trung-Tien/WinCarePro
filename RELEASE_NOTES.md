# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.6 — Nâng cấp tính năng & Quản lý nâng cao

> **Phát hành:** 20/07/2026 · **Loại:** Bản cập nhật tính năng & Hiệu năng hệ thống (Feature & Performance Update)

Bản cập nhật **v3.4.6** giới thiệu các tính năng quản lý menu chuột phải hoàn toàn mới, tích hợp cơ chế Bảo mật DNS qua HTTPS (DoH), nâng cấp hệ thống chẩn đoán phần cứng (GPU, CPU, SSD/HDD) và cải tiến toàn diện độ ổn định bằng các tác vụ bất đồng bộ.

---

### ✨ Các tính năng & cải tiến nổi bật trong phiên bản v3.4.6 (Key Features)

* **🖱️ Quản lý Menu chuột phải (Context Menu Manager):** Cho phép dễ dàng bật/tắt các mục menu chuột phải hệ thống cho All Files, Desktop Background, Folders trực tiếp qua Registry, tự động giải mã tên hiển thị thân thiện từ Class ID (CLSID) trong Registry để người dùng dễ kiểm soát.
* **🛡️ Bảo mật DNS qua HTTPS (DNS-over-HTTPS - DoH):** Tích hợp cấu hình Secure DNS (DoH) trực tiếp trong Network Center, cho phép mã hóa các truy vấn tên miền của hệ thống với các nhà cung cấp phổ biến như Cloudflare, Google, AdGuard, NextDNS.
* **📊 Chẩn đoán Phần cứng & Hệ thống Nâng cao:** 
  * Bổ sung theo dõi hiệu suất sử dụng GPU thực tế thời gian thực thông qua Performance Counters của Windows.
  * Phát hiện thắt cổ chai CPU (Throttling) bằng cách kiểm tra xung nhịp xử lý thực tế so với xung nhịp tối đa.
  * Phân tích dữ liệu SMART và các chỉ số sức khỏe ổ cứng (SSD/HDD Health) của Windows Storage để đưa ra phần trăm độ bền ổ đĩa chính xác.
  * Cơ chế kiểm tra driver mới chỉ phát hiện và cảnh báo các driver phần cứng hệ thống quan trọng (Non-Generic/Non-Microsoft) cũ hơn 180 ngày.
* **⚡ Nâng cấp hiệu năng & Trình gỡ cài đặt:** 
  * Tối ưu hóa toàn bộ các tác vụ chạy công cụ hệ thống (DISM, SFC, Restore Point, Network Repair) sang chế độ bất đồng bộ (`ProcessRunner`) để giao diện luôn mượt mà.
  * Hỗ trợ gỡ cài đặt cưỡng bức (Force Uninstall) để dọn dẹp triệt để ứng dụng (kể cả Microsoft Store) và các tệp rác còn sót lại khi tiến trình gỡ cài đặt tiêu chuẩn gặp lỗi.
  * Cải tiến tính năng chọn tất cả ("Select All") cho danh sách ứng dụng và danh sách tệp rác thừa.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
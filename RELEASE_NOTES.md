# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.7 — Tối ưu hóa Kiến trúc & Nâng cao An toàn Hệ thống

> **Phát hành:** 27/07/2026 · **Loại:** Bản cập nhật Kiến trúc, An toàn & Hiệu năng (Architecture & Safety Update)

Bản cập nhật **v3.4.7** tập trung vào việc tái cấu trúc kiến trúc hệ thống chuẩn Dependency Injection (DI), tăng cường cơ chế bảo vệ và sao lưu registry tự động, tối ưu hóa công cụ Game Boost an toàn và nâng cao độ ổn định tổng thể của ứng dụng.

---

### ✨ Các tính năng & cải tiến nổi bật trong phiên bản v3.4.7 (Key Features)

* **🏗️ Tối ưu hóa & Chuẩn hóa Kiến trúc Dependency Injection (DI):** 
  * Chuẩn hóa toàn bộ các ViewModel và Engine hệ thống thông qua DI Container, giúp quản lý tài nguyên hiệu quả và nâng cao khả năng mở rộng.
  * Tối ưu hóa bộ đệm kết nối mạng (`HttpClient Singleton`), ngăn ngừa hiện tượng cạn kiệt tài nguyên socket khi kiểm tra cập nhật và chẩn đoán mạng.

* **🛡️ An toàn Hệ thống & Tự động Sao lưu Registry:**
  * Cơ chế tự động tạo bản sao lưu Registry an toàn trước khi sửa lỗi hệ thống, giúp người dùng dễ dàng khôi phục khi cần thiết.
  * Bảo vệ dịch vụ cập nhật hệ thống quan trọng (Windows Update) khi kích hoạt chế độ Game Boost, tránh ảnh hưởng đến tính an ninh của hệ thống.

* **⚡ Cải tiến Luồng Xử lý & Dọn dẹp Bộ nhớ:**
  * Tối ưu hóa luồng xử lý quét và dọn dẹp tệp trùng lặp trên đĩa đĩa (`Disk Tools`), đảm bảo giao diện phản hồi chính xác và mượt mà.
  * Tập trung hóa bộ công cụ định dạng dung lượng dữ liệu dùng chung (`FormatHelper`), giúp hiển thị thông số thống nhất trên toàn ứng dụng.
  * Loại bỏ các mã dữ liệu giả lập, đảm bảo thông tin hiển thị chính xác theo thực tế hệ thống.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
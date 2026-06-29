# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.3.1 — Vá lỗi phím tắt & Cải tiến bộ tự động cập nhật

> **Phát hành:** 29/06/2026 · **Loại:** Bản vá bảo trì & Nâng cấp phụ (Maintenance & Minor Update) · **Phiên bản trước:** v3.3.0

Bản cập nhật **v3.3.1** tập trung sửa lỗi tương tác giao diện và tối ưu hóa hệ thống kiểm thử bộ cập nhật ứng dụng, mang lại trải nghiệm mượt mà, ổn định hơn cho người dùng.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.3.1 (Key Features)

#### 1. ⌨️ Khắc phục xung đột phím tắt tìm kiếm (Ctrl+F)
* **Chuyển sang PreviewKeyDown:** Khắc phục triệt để lỗi xung đột phím tắt tìm kiếm. Thay vì sử dụng danh sách `KeyboardAccelerators` của WinUI 3 (vốn gặp lỗi hiển thị Tooltip không mong muốn và tự động kích hoạt ngoài ý muốn), ứng dụng đã chuyển sang lắng nghe và bắt sự kiện `PreviewKeyDown` trực tiếp từ container chính của giao diện.
* **Ổn định hơn:** Ngăn chặn các sự cố giao diện bị đơ hoặc lỗi hành vi phím tắt khi người dùng nhập liệu nhanh trong các trường văn bản.

#### 2. 🔌 Hỗ trợ cập nhật ứng dụng từ đường dẫn cục bộ (Local File Path)
* **Giao thức file:///:** Trình cập nhật ứng dụng (`SoftwareUpdaterEngine`) nay đã hỗ trợ kiểm tra và tải xuống tệp cài đặt từ đường dẫn tệp tin cục bộ (`file:///...`).
* **Tiện lợi cho kiểm thử:** Cho phép lập trình viên và kiểm thử viên kiểm tra quy trình hoạt động của trình tự động cập nhật ngay trên máy tính cá nhân mà không cần tải lên GitHub Releases hoặc máy chủ web.

#### 3. 📚 Cập nhật và Đồng bộ hóa Tài liệu Hệ thống
* **Tối ưu README.md:** Đồng bộ hóa toàn bộ mô tả 14 phân hệ chức năng tương ứng trực quan với mã nguồn hiện tại của hệ thống.
* **Nút tải về trực tiếp:** Cập nhật liên kết nút tải về tại README.md trỏ trực tiếp đến gói cài đặt `WinCareProSetup.exe` của phiên bản mới nhất v3.3.1.

---

### 🩹 Chi tiết kỹ thuật & Sửa lỗi (Technical Details & Fixes)
* Điều chỉnh logic sao chép tập tin trong `SettingsPage.xaml.cs` để hỗ trợ sao chép an toàn từ đường dẫn cục bộ thay vì gọi HTTP client tải xuống khi phát hiện giao thức file.
* Giải phóng tài nguyên stream đúng cách và tránh hiện tượng khóa file khi sao chép tệp cài đặt tạm thời.

---
<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
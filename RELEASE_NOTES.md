# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro v5.0.0

---

## 🚀 WinCare Pro v5.0.0 (Codename: Polaris) — Bước Tiến Đột Phá: AI Dự Báo Lưu Trữ, Bảo Mật Cô Lập Nhân & Giám Sát Nhận Biết Nguồn Pin (Polaris Major Release)

> **Phiên bản:** v5.0.0 (Codename: Polaris) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Trạng thái:** Bản Phát Hành Chính Thức (Official Release) · **Chứng nhận:** 100% Tests PASS · 0 Warnings / 0 Errors

**WinCare Pro v5.0.0 (Codename: Polaris)** là bước phát triển nhảy vọt của WinCare Pro, nâng tầm ứng dụng từ một trình dọn dẹp hệ thống trở thành một **Bộ Tiện Ích Chẩn Đoán, Bảo Mật & Quản Trị Hệ Thống Chuyên Nghiệp Cao Cấp** trên Windows 10 & 11:

### 🌟 Điểm Nhấn Đột Phá Trong v5.0.0 (Polaris):

1. **🛡️ Kiểm Tra Bảo Mật Nền Tảng & Cô Lập Nhân (Platform Security & Kernel Isolation):**
   - **Hypervisor-Protected Code Integrity (HVCI):** Kiểm tra tính toàn vẹn bộ nhớ được bảo vệ bởi ảo hóa phần cứng (Memory Integrity), ngăn chặn triệt để mã độc chèn vào nhân hệ điều hành.
   - **Microsoft Defender Tamper Protection:** Giám sát trạng thái chống can thiệp của bộ phòng vệ Windows Defender, cảnh báo người dùng khi các thiết lập an ninh bị phần mềm lạ can thiệp.
   - **RDP Network Level Authentication (NLA):** Phát hiện và khuyến nghị bật xác thực cấp mạng cho Remote Desktop nhằm ngăn chặn các đợt tấn công dò mật khẩu từ xa.

2. **🔋 Giám Sát Nền Nhận Biết Nguồn Pin (Battery-Aware Background Watchdog):**
   - **Cơ chế tiết kiệm pin thông minh:** Khi laptop ngắt sạc và mức pin dưới 20%, hệ thống tự động hoãn các tác vụ bảo trì định kỳ có phụ tải cao để tối đa hóa thời lượng sử dụng của người dùng.
   - **Cảnh báo dung lượng ổ đĩa khẩn cấp:** Giám sát dung lượng ổ đĩa hệ thống định kỳ mỗi 60 giây và kích hoạt thông báo Toast kèm đường dẫn xử lý khi dung lượng dưới 5GB.
   - **Nhật ký tài nguyên SQLite WAL không phình to:** Ghi nhận lịch sử tài nguyên CPU/RAM/Disk và tự động dọn dẹp các bản ghi cũ quá 7 ngày.

3. **⚡ Chuẩn Hóa Zero-Mock Telemetry Tuyệt Đối:**
   - **100% dữ liệu thật từ Kernel & Win32 APIs:** Loại bỏ hoàn toàn mọi đoạn mã sinh số giả lập ngẫu nhiên (`Random`) trong toàn bộ tầng giám sát và ViewModel. Dữ liệu CPU, RAM, Disk, và Network phản ánh chính xác 100% hoạt động thực tế của máy tính.

---

<div align="center">
  <sub>WinCare Pro Suite v4.9.3 Orion • Phát triển bởi <b>Nguyễn Trung Tiến</b></sub>
</div>

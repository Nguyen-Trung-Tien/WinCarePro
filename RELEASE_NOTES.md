# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro

---

## 🚀 WinCare Pro v5.1.2 (Codename: Polaris) — Top-Center Dynamic Capsule HUD & UX Elevation

> **Phiên bản:** v5.1.2 (Codename: Polaris) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Ngày phát hành:** 31/10/2026 · **Trạng thái:** Bản Phát Hành Chính Thức (Official Release) · **Chứng nhận:** 100% Tests PASS · 0 Warnings / 0 Errors

**WinCare Pro v5.1.2 (Codename: Polaris)** là bản phát hành nâng cấp trải nghiệm người dùng mang tính đột phá, chuyển đổi toàn bộ cơ chế thông báo sang **Top-Center Floating Island Toast (Golden Focal Area)**, tối ưu hóa thị giác với thiết kế viên nhộng Dynamic Capsule, chuyển động vật lý trục dọc mượt mà và đồng bộ hóa phiên bản nguyên tử trên toàn hệ thống:

### 🌟 Điểm Nhấn Đột Phá Trong v5.1.2:

1. **🎯 Kiến Trúc Thông Báo Top-Center Dynamic Capsule (Golden Focal Area):**
   - **Tầm nhìn tự nhiên:** Di chuyển hệ thống thông báo từ góc dưới phải (Bottom-Right) lên vị trí trung tâm phía trên màn hình (Top-Center), loại bỏ hoàn toàn nguy cơ bị Taskbar hệ thống, Windows Action Center che khuất hoặc hiện tượng mù thị giác ngoại vi (peripheral blindness).
   - **Chống xung đột TitleBar:** Thiết lập vùng đệm chính xác `Margin="0,58,0,0"` ngay dưới thanh tiêu đề 56px và nâng mức hiển thị lên `Canvas.ZIndex="3000"`. Cơ chế `IsHitTestVisible` thông minh đảm bảo 100% không cản trở thao tác kéo thả di chuyển cửa sổ ứng dụng và tương tác trên thanh tìm kiếm `SearchBox`.

2. **🎨 Ngôn Ngữ Thiết Kế Aura Capsule & Chiều Sâu Nổi Khối:**
   - **Kiểu dáng Dynamic Capsule:** Bo cong viền mềm mại `CornerRadius="20"`, mở rộng bề ngang lên `388px` tạo cảm giác cân xứng, sang trọng và hiện đại như Dynamic Island.
   - **Specular Line & Elevation Depth:** Bổ sung hiệu ứng chiều sâu nổi khối Windows 11 với `Translation="0,8,28"` và đường viền phản chiếu ánh sáng Acrylic specular phía trên `CornerRadius="20,20,0,0"`.
   - **Micro Progress Bar:** Thanh đếm ngược thời gian tự đóng siêu mỏng `2px` bo cong ôm sát mép đáy (`CornerRadius="0,0,20,20"`), giúp người dùng theo dõi trực quan trạng thái thông báo mà không gây xao nhãng.

3. **💫 Chuyển Động Trục Y Mượt Mà & Hiệu Năng 120 FPS:**
   - **Chuyển động rơi tự nhiên (Slide-Down Entry):** Thay thế chuyển động trượt ngang cũ bằng hiệu ứng rơi dọc từ trên mép màn hình xuống với gia tốc tự nhiên `CubicEaseOut` (`Y: -40 -> 0`, thời lượng 320ms).
   - **Thu hồi nhanh (Slide-Up Exit):** Khi hết thời gian hoặc bấm Dismiss, Toast lướt nhẹ ngược lên trên với `CubicEaseIn` (`Y: 0 -> -25`, thời lượng 180ms) và mờ dần rất nhanh.
   - **Object Pooling tối ưu bộ nhớ:** Duy trì cấu trúc tái sử dụng đối tượng (`_toastPool`), triệt tiêu việc cấp phát bộ nhớ rác (GC Allocation) và đảm bảo tốc độ phản hồi 120 FPS.

4. **🔄 Đồng Bộ Hóa Phiên Bản Hệ Thống Nguyên Tử (Atomic Version Sync):**
   - Đồng bộ hóa 14 điểm chạm trên toàn bộ dự án: `AppConstants` (SSOT), `WinCarePro.csproj`, `app.manifest`, `Package.appxmanifest`, `setup.iss`, `update.json`, `MainWindow.xaml`, `CHANGELOG.md`, `README.md`, `AGENTS.md` và `WinCarePro.Tests`.
   - Vượt qua 100% bộ kiểm thử tự động `AppVersionManagementTests` và bảo đảm quy chuẩn biên dịch không cảnh báo (`0 Warning, 0 Error`).

---

<div align="center">
  <sub>WinCare Pro Suite v5.1.2 Polaris • Phát triển bởi <b>Nguyễn Trung Tiến</b></sub>
</div>

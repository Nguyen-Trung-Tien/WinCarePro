# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro

---

## 🚀 WinCare Pro v5.2.0 (Codename: Polaris) — Aura Fluidic 3.0 & Intelligent Diagnostic Sentinel Suite

> **Phiên bản:** v5.2.0 (Codename: Polaris) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Ngày phát hành:** 06/11/2026 · **Trạng thái:** Bản Phát Hành Chính Thức (Official Release) · **Chứng nhận:** 100% Tests PASS (482/482) · 0 Warnings / 0 Errors

**WinCare Pro v5.2.0 (Codename: Polaris)** là bản phát hành nâng cấp toàn diện và mạnh mẽ nhất của hệ sinh thái WinCare Pro Suite, đánh dấu bước chuyển mình đột phá sang **Kiến Trúc Aura Fluidic 3.0**, tích hợp động cơ **AI Diagnostic Sentinel Engine** tự động hóa phát hiện và khắc phục sự cố, tái cấu trúc toàn diện **Trung Tâm Bảo Mật Thế Hệ Mới (Next-Gen Security Center)** và hệ thống quản lý ứng dụng chuyên sâu:

### 🌟 Điểm Nhấn Đột Phá Trong v5.2.0:

1. **🎨 Kiến Trúc Aura Fluidic 3.0 & Trải Nghiệm Người Dùng Đồng Nhất:**
   - **Nâng cấp đồng bộ 11 phân hệ:** Hoàn thiện ngôn ngữ thiết kế Fluent 2.0 Aura Glass trên toàn bộ giao diện: Dashboard, Bảo mật, Dọn dẹp rác, Tối ưu Registry, Gỡ cài đặt, Quản lý mạng, Sửa chữa hệ thống, Tối ưu hóa, Thông báo, Trợ lý AI và Cài đặt.
   - **Hệ thống thẻ chẩn đoán mật độ cao:** Bố cục thẻ thông số thông minh bo góc chuẩn 8-12px, hiệu ứng đổ bóng nổi khối Z-axis đa tầng, font chữ Tabular Figures cố định cho mọi chỉ số telemetry giúp triệt tiêu hiện tượng giật rung giao diện khi cập nhật số liệu.
   - **Kỷ luật bộ nhớ và chuyển động 120 FPS:** Tối ưu hóa Composition API, triệt tiêu rò rỉ bộ nhớ nhờ giải phóng toàn diện sự kiện (`-=`) khi chuyển trang. Duy trì nghiêm ngặt mức chiếm dụng: Foreground RAM $\le 85\text{ MB}$, Background RAM $\le 15\text{ MB}$, CPU Idle $\le 0.3\%$.

2. **🤖 Động Cơ AI Diagnostic Sentinel & Khắc Phục Tự Động:**
   - **Phân tích ngữ cảnh đa chiều:** Động cơ `AiWinCareEngine` tự động quét và phân tích sâu tình trạng CPU, RAM, ổ đĩa, tiến trình treo, lỗ hổng mạng và thiết lập hệ điều hành để đề xuất giải pháp tối ưu.
   - **Kịch bản sửa lỗi 1-Chạm an toàn:** Tự động tạo kịch bản khắc phục sự cố thông minh, giải thích cặn kẽ nguyên nhân và cho phép người dùng kích hoạt 1-Click Repair dưới sự bảo vệ nghiêm ngặt của `SafePathGuard` và `SafeRegistryGuard`.
   - **Trải nghiệm tương tác AI trực quan:** Giao diện hội thoại Aura Glass hiện đại với bong bóng chat thông minh, hỗ trợ câu hỏi gợi ý nhanh và hiển thị chi tiết tiến trình chẩn đoán.

3. **🛡️ Trung Tâm Bảo Mật Thế Hệ Mới & Phòng Thủ Lỗ Hổng:**
   - **Rà soát lỗ hổng đa vector:** Kiểm tra toàn diện tình trạng an ninh của Microsoft Defender, Windows Firewall trên từng hồ sơ mạng (Domain, Private, Public), cấu hình xác thực cấp mạng RDP NLA và các cổng kết nối nhạy cảm.
   - **Kernel Isolation & Tamper Protection:** Giám sát thời gian thực tính toàn vẹn bộ nhớ mức ảo hóa (HVCI) và bảo vệ chống can thiệp trái phép, bảo vệ hệ điều hành trước mã độc cấp nhân (Kernel-level exploits).
   - **Thẻ điểm an ninh hệ thống:** Cung cấp chỉ số Security Health Score tổng quan và danh sách khuyến nghị xử lý được sắp xếp theo mức độ rủi ro từ cao xuống thấp.

4. **🔔 Trung Tâm Hoạt Động & Nhịp Đập Telemetry (Activity Hub):**
   - **Live Telemetry Pulse:** Theo dõi nhịp tim hệ thống và mức tải xử lý theo thời gian thực ngay trong trung tâm thông báo.
   - **Bộ lọc danh mục thông minh:** Phân loại cảnh báo chi tiết theo danh mục (Bảo mật, Dọn rác, Hiệu năng, Cập nhật), hỗ trợ đánh dấu đã đọc hoặc dọn dẹp hàng loạt nhật ký hoạt động chỉ với 1 cú nhấp.

5. **📦 Quản Lý Vòng Đời Ứng Dụng Chuyên Sâu & Cây Tiến Trình:**
   - **Nhóm tiến trình theo cây cha-con (`ProcessGroupInfo`):** Nhận diện chính xác toàn bộ cây tiến trình của phần mềm, đảm bảo đóng an toàn và triệt để trước khi thực hiện gỡ cài đặt.
   - **Quét sâu tàn dư:** Tự động truy quét các tập tin rác còn sót lại trong AppData/ProgramData và khóa Registry mồ côi sau khi phần mềm bị xóa, tự động tạo điểm khôi phục qua `RegistryBackupEngine`.

6. **✅ Mở Rộng Kiểm Thử Tự Động & Độ Tin Cậy Tuyệt Đối:**
   - **482 xUnit Test Cases hoàn thành xuất sắc (100% Passed):** Bổ sung 17 bài kiểm thử chuyên sâu cho chẩn đoán AI, bảo mật gói cài đặt, giám sát tiến trình và dọn dẹp hệ thống; đảm bảo 0 Warning và 0 Error trên toàn dự án.

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

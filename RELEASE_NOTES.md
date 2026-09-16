# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro v4.9.3

---

## 🚀 WinCare Pro v4.9.3 (Codename: Orion) — Bản Sửa Lỗi Cập Nhật SHA-256 & Gia Cố Toàn Diện Hệ Thống (SHA-256 Update Fix & System Hardening Release)

> **Phiên bản:** v4.9.3 (Codename: Orion) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Trạng thái:** Bản Phát Hành Sẵn Sàng (Release Ready) · **Chứng nhận:** 438/438 Tests PASS · 0 Warnings / 0 Errors

**WinCare Pro v4.9.3 (Codename: Orion)** tập trung khắc phục triệt để lỗi mã SHA-256 trong chu trình tự động cập nhật, tự động hóa tính và đồng bộ hóa manifest, loại bỏ hoàn toàn các nguy cơ crash tiềm ẩn khi khởi động, tối ưu hóa toàn diện hiệu năng và xử lý đa luồng WinUI 3.

### 🛡️ Điểm Cải Tiến & Vá Lỗi Nổi Bật trong v4.9.3:

1. **🔐 Khắc Phục Triệt Để Lỗi Mã SHA-256 Khi Cập Nhật:**
   - **Tự động đồng bộ hóa SHA-256:** Tự động tính toán mã hash SHA-256 chính xác của file cài đặt `WinCareProSetup.exe` và ghi trực tiếp vào `update.json` và CI/CD release workflow qua script `scripts/update_sha256.ps1` tích hợp trong `publish_installer.bat`.
   - **Tự động Commit & Push trong GitHub Actions:** Bổ sung cơ chế tự động commit `update.json` về nhánh `main` và upload trực tiếp `update.json` lên GitHub Release Assets trong `.github/workflows/release.yml`, loại bỏ hoàn toàn hiện tượng hash trên `main` bị lệch so với hash runner tạo ra.
   - **Cơ chế Tự Phục Hồi Checksum Đồng Hành (Authoritative Companion Checksum Fallback):** Khi tải bản cập nhật, ứng dụng truy vấn song song file `.sha256` phát hành cùng gói setup trên GitHub Releases (`TryFetchCompanionSha256Async`). Nếu hash trong `update.json` bị lệch hoặc chậm cập nhật CDN, ứng dụng sẽ xác thực với checksum chính thức này mà không làm gián đoạn cập nhật.
   - **Phòng vệ Tải Dở Dang (Truncated Download Guard):** Bổ sung kiểm tra `totalRead == totalBytes` trước khi hash. Nếu mạng bị ngắt giữa chừng, hệ thống báo lỗi mạng gián đoạn rõ ràng để người dùng thử lại thay vì báo nhầm "lỗi SHA-256 mismatch".
   - **Làm sạch mã băm (Hash Normalization):** Bổ sung hàm `NormalizeHash()` loại bỏ tiền tố (`sha256:`, `0x`), khoảng trắng thừa, và chuẩn hóa hex 64 ký tự.
   - **Cơ chế Hash-Pinned Integrity:** Cho phép xác thực an toàn đối với các bản phát hành nguồn mở từ GitHub Releases chính thức kết hợp mã băm 256-bit, tránh việc tự xóa file cài đặt khi chưa có chứng chỉ số thương mại.
   - **Đồng bộ kênh Beta & Stable:** Đồng bộ hóa logic kiểm tra cập nhật giữa `MainWindow` và `SettingsPage`, đảm bảo tải đúng URL và so khớp đúng mã SHA-256.

2. **💥 Triệt Tiêu Các Nguy Cơ Crash Hệ Thống (Zero-Crash Architecture):**
   - **Đóng gói đầy đủ runtime WinUI 3:** Bổ sung `System.Diagnostics.EventLog` và chuyển đổi `PublishSingleFile=false` trong `publish.bat`, `publish_installer.bat` và release workflow, đảm bảo nạp đủ 468 assemblies runtime.
   - **Bảo vệ ServiceController & Boot Time:** Bọc khối try-catch an toàn trong `StartupEngine.cs` và các ViewModel, ngăn chặn hiện tượng tê liệt dữ liệu Startup hoặc phát sinh `UnobservedTaskException`.
   - **Chống crash mạng:** Phòng vệ rớt mạng đột ngột trong quá trình kiểm tra cập nhật tại `SettingsPage`.

3. **🏗️ Chuẩn Hóa MVVM & Đa Luồng WinUI 3:**
   - Chuẩn hóa phương thức `RunOnUI()` trong `ViewModelBase`, loại bỏ 6 cảnh báo biên dịch CS0108.
   - Bổ sung khối `finally` đảm bảo cờ `IsBusy` và `IsScanning` luôn được giải phóng sau khi hoàn tất hoặc hủy tác vụ.

4. **🧪 100% Unit Test Passed:**
   - 438 / 438 bài kiểm thử xUnit chạy thành công 100% không cảnh báo, không lỗi.

---

## 🌟 Tổng Hợp Các Chức Năng Cốt Lõi Của WinCare Pro (Core Suite Capabilities)

* **🧠 Trợ Lý AI WinCare & Chẩn Đoán Sức Khỏe:**
  * Quét và đánh giá 8 phương diện sức khỏe của máy tính (RAM, Ổ đĩa, Tệp rác, Ứng dụng khởi động, Bảo mật, Mạng, Tàn dư hệ thống).
  * Dự báo thông minh số ngày còn lại trước khi ổ đĩa cài đặt Windows bị đầy dung lượng.
  * Sửa chữa nhanh các vấn đề được phát hiện chỉ bằng một cú nhấp chuột.
  * Hỗ trợ xuất báo cáo chẩn đoán chi tiết ra màn hình chính.

* **🧹 Dọn Rác Toàn Diện & Gỡ Cài Đặt Sạch Sẽ:**
  * Dọn sạch bộ nhớ đệm, tệp tạm Windows, lịch sử trình duyệt (Chrome, Edge, Firefox, Brave) và các tệp nhật ký dư thừa.
  * Gỡ bỏ hoàn toàn ứng dụng truyền thống (Win32) và ứng dụng Microsoft Store (UWP), tự động quét sâu và xóa sạch các thư mục tàn dư trong AppData và Registry.

* **⚡ Tối Ưu Hóa Hiệu Năng & Tinh Chỉnh Hệ Thống:**
  * Giải phóng bộ nhớ đệm RAM vật lý ngay lập tức, mang lại độ mượt mà tức thì cho các ứng dụng và game.
  * Tinh chỉnh độ nhạy menu, tối ưu băng thông mạng và giảm độ trễ phản hồi của hệ điều hành.

* **🔧 Quản Lý Driver Phần Cứng & Cập Nhật Phần Mềm:**
  * Kiểm tra danh sách thiết bị phần cứng và trạng thái hoạt động của các trình điều khiển (driver).
  * Sao lưu toàn bộ driver ra thư mục an toàn để dễ dàng phục hồi khi cần thiết.
  * Tự động quét và phát hiện phiên bản mới của các phần mềm bên thứ ba đã cài đặt trên máy, hỗ trợ cập nhật nhanh chóng.

* **🪟 Tiện Ích Màn Hình Desktop HUD Mini Widget & Giao Diện Kính Mờ:**
  * Tiện ích nổi mini hiển thị tức thời mức sử dụng CPU, RAM và Tốc độ mạng ngay trên màn hình.
  * Giao diện kính mờ Aura Glass 2.0 (Mica/Acrylic) siêu mượt 120 FPS với các chủ đề Tối, Sáng và Cyberpunk Neon.
  * Thanh tìm kiếm cài đặt toàn cục thông minh trên Header giúp truy cập nhanh tới mọi mục chức năng.
  * Hỗ trợ song ngữ hoàn hảo 100% Tiếng Việt và Tiếng Anh, chuyển đổi ngôn ngữ tức thì.

---

## 📋 Bảng Danh Mục Các Phân Hệ Chức Năng (Functional Capabilities Matrix)

| Phân hệ chức năng | Mô tả chức năng & Lợi ích đối với người dùng |
| :--- | :--- |
| **📊 Bảng Điều Khiển (Dashboard)** | Theo dõi thông số phần cứng thời gian thực, hiển thị điểm sức khỏe tổng thể và nút tăng tốc nhanh 1-chạm. |
| **🤖 Trợ Lý AI WinCare** | Chẩn đoán đa chiều, cảnh báo dung lượng ổ đĩa, tự động khắc phục sự cố và xuất báo cáo sức khỏe chi tiết. |
| **🧹 Dọn Rác (Junk Cleaner)** | Làm sạch tệp tạm hệ thống, bộ nhớ đệm trình duyệt và các dữ liệu rác với cơ chế chống xóa nhầm tệp quan trọng. |
| **📦 Gỡ Cài Đặt (App Uninstaller)** | Gỡ bỏ sạch sẽ ứng dụng Desktop và Store, tự động tìm và xóa sạch tệp thừa cùng khóa Registry còn sót lại. |
| **🌐 Trung Tâm Mạng (Network)** | Đo tốc độ mạng Speed Test ổn định, đổi máy chủ DNS bảo mật 1-chạm và công cụ tự động khôi phục kết nối. |
| **🛠️ Sửa Lỗi Hệ Thống (Repair)** | Tự động quét và phục hồi tệp hệ thống Windows bị hỏng (SFC), khôi phục kho ảnh (DISM) và sửa lỗi Windows Update. |
| **🛡️ Khiên Bảo Mật (Security)** | Kiểm tra trạng thái Tường lửa, Windows Defender, bảo vệ quyền riêng tư và dọn dẹp dấu vết hoạt động cá nhân. |
| **⚡ Tối Ưu Hệ Thống (Optimizer)** | Tinh chỉnh hiệu năng Windows, giải phóng bộ nhớ RAM vật lý tức thì và thiết lập cấu hình vận hành tối ưu. |
| **🖱️ Menu Chuột Phải (Context Menu)** | Quản lý và tắt các mục mở rộng dư thừa trên menu chuột phải của File Explorer giúp mở menu tức thì. |
| **🚀 Khởi Động & Dịch Vụ (Startup)** | Quản lý các ứng dụng khởi động cùng Windows, đánh giá mức độ ảnh hưởng và rút ngắn thời gian khởi động máy tính. |
| **💾 Ổ Đĩa & Lưu Trữ (Disk Center)** | Theo dõi sức khỏe ổ cứng S.M.A.R.T, phân tích trực quan dung lượng thư mục và tìm kiếm tệp tin trùng lặp. |
| **🗄️ Quản Trị Registry (Registry)** | Phát hiện và dọn dẹp các mục Registry lỗi thời, tự động sao lưu an toàn trước khi thực hiện thay đổi. |
| **🔄 Cập Nhật Phần Mềm (Updater)** | Tự động tìm kiếm bản cập nhật mới cho các ứng dụng đã cài đặt và hỗ trợ cập nhật an toàn. |
| **🔧 Driver Phần Cứng (Hardware)** | Kiểm tra tình trạng hoạt động của toàn bộ driver linh kiện và sao lưu dự phòng toàn diện. |
| **🪟 Tiện Ích Nổi (Desktop Widget)** | Cửa sổ HUD nhỏ gọn trên màn hình chính theo dõi tài nguyên liên tục mà không chiếm dụng không gian làm việc. |
| **⚙️ Cài Đặt & Giao Diện (Settings)** | Tùy biến chủ đề hiển thị, chuyển đổi ngôn ngữ Việt - Anh tức thì và quản lý dữ liệu ứng dụng. |

---

<div align="center">
  <sub>WinCare Pro Suite v4.9.3 Orion • Phát triển bởi <b>Nguyễn Trung Tiến</b></sub>
</div>
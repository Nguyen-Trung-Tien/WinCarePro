# 📝 Nhật ký Phát hành (Release Notes) — WinCare Pro v4.9.0

---

## 🚀 WinCare Pro v4.9.0 (Codename: Nova) — Bộ Ứng Dụng Tối Ưu, Chăm Sóc & Bảo Mật Windows Toàn Diện

> **Phiên bản:** v4.9.0 (Codename: Nova) · **Nền tảng:** Windows 10 (Build 19041+) & Windows 11 (x64) · **Trạng thái:** Bản Phát Hành Chính Thức (Official Production Release) · **Chứng nhận:** 300/300 Tests PASS · 0 Warnings / 0 Errors

**WinCare Pro v4.9.0 (Codename: Nova)** là phiên bản nâng cấp sản xuất đỉnh cao (Production Hardening & Safety Architecture), củng cố toàn diện độ an toàn cấp doanh nghiệp, bảo vệ tuyệt đối hệ thống Windows khỏi các nguy cơ xóa nhầm hoặc xung đột luồng, đồng thời tối ưu hóa vòng đời tác vụ ngầm và phản hồi giao diện WinUI 3 đạt mức hoàn thiện 100%.

---

## ✨ Điểm Mới & Cải Tiến Nổi Bật Dành Cho Người Dùng (What's New in v4.9.0)

### 1. 🛡️ Khiên Bảo Vệ Registry Đa Lớp (SafeRegistryGuard Enterprise Barrier)
* **Bảo vệ toàn vẹn Registry hệ thống:** Tích hợp bộ lọc an toàn `SafeRegistryGuard`, ngăn chặn tuyệt đối mọi hành vi xóa nhầm hoặc sửa đổi các nhánh gốc Windows (`HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`) và các nhánh cấu hình sống còn (`SYSTEM\CurrentControlSet`, `Winlogon`, `Image File Execution Options`).
* **Bảo vệ khóa khởi động và thông số boot:** Các giá trị nhạy cảm của hệ thống như `Shell`, `Userinit`, `AppInit_DLLs` được khóa an toàn, ngăn chặn mã độc hoặc thao tác dọn dẹp nhầm lẫn làm hỏng phiên đăng nhập Windows.
* **Yêu cầu phân cấp nghiêm ngặt:** Khóa Registry chỉ được phép xóa khi có độ sâu từ 3 phân cấp trở lên và thuộc về ứng dụng cụ thể.

### 2. 🔒 Bảo Vệ Dịch Vụ Cốt Lõi Hệ Điều Hành (ServiceSafetyService Fail-Safe)
* **Chống dừng / vô hiệu hóa nhầm dịch vụ Windows:** Tích hợp cơ chế kiểm soát dịch vụ bảo vệ tuyệt đối các dịch vụ lõi như `RpcSs` (Remote Procedure Call), `WinDefend` (Microsoft Defender), `SamSs` (Security Accounts Manager), `PlugPlay`, `RpcEptMapper`, `DcomLaunch`.
* **Cảnh báo và tự động ghi log an toàn:** Bất kỳ thao tác nào cố tình đưa dịch vụ cốt lõi về trạng thái `Disabled` hoặc `Stop` đều bị chặn tức thì và ghi nhận vào nhật ký kiểm toán hệ thống.

### 3. ⚡ Quản Lý Vòng Đời Tác Vụ & Điều Phối Giao Diện An Toàn (CancellationToken & UI Dispatcher)
* **Hủy tác vụ an toàn, tức thời:** Toàn bộ các thao tác quét rác, quét tàn dư, sửa Registry, chẩn đoán AI và kiểm tra mạng đều được kiểm soát bởi `CancellationToken`. Người dùng có thể hủy hoặc chuyển trang bất kỳ lúc nào mà không lo ứng dụng bị treo.
* **Tự động dọn dẹp khi rời trang (`OnNavigatedFrom`):** Khi người dùng chuyển trang, ViewModel tự động hủy tác vụ cũ, giải phóng trạng thái `IsBusy` và ngắt kết nối sự kiện ngầm, đảm bảo khi quay lại trang luôn sẵn sàng 100%.
* **Đồng bộ luồng giao diện WinUI 3 hoàn hảo:** Mọi thông báo ngầm từ cơ sở dữ liệu SQLite và tiến độ xử lý đều được điều phối chuẩn xác qua `App.MainDispatcherQueue.TryEnqueue()`, triệt tiêu hoàn toàn nguy cơ lỗi luồng chéo (COMException).

### 4. 📁 Củng Cố An Toàn Tệp Tin & Thư Mục (Enhanced SafePathGuard)
* **Xóa thư mục không đệ quy:** Quy trình dọn dẹp thư mục chuyển sang cơ chế xóa an toàn không đệ quy (`Delete(false)`), đảm bảo chỉ xóa thư mục khi đã thực sự rỗng và bảo vệ toàn vẹn các liên kết tượng trưng (Junction/Symlink).
* **Khóa bảo vệ `C:\ProgramData`:** Bổ sung `C:\ProgramData` và các thư mục bảo mật Windows vào danh sách cấm xóa tuyệt đối.

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
  <sub>WinCare Pro Suite v4.9.0 Nova • Phát triển bởi <b>Nguyễn Trung Tiến</b></sub>
</div>
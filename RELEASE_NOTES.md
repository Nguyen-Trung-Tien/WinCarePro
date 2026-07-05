# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.1 — Tối ưu hóa giao diện, Hiệu năng & Bản địa hóa

> **Phát hành:** 05/07/2026 · **Loại:** Bản cập nhật hiệu năng & Sửa lỗi (Performance & Bug Fixes) · **Phiên bản trước:** v3.4.0

Bản cập nhật **v3.4.1** tập trung tối ưu hóa hiệu năng khởi động ứng dụng, cải tiến cơ chế đồng bộ giao diện sáng/tối và sửa lỗi hiển thị phiên bản ở phần About & Developer.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.4.1 (Key Features)

#### 1. 🎨 Tối ưu hóa Chủ đề & Hiệu ứng trong suốt (Theme & Transparency Syncing)
* **Đồng bộ hóa giao diện:** Đồng bộ hóa hoàn hảo trạng thái nút chuyển đổi giao diện giữa Header và trang Cài đặt (Settings).
* **Mica & Acrylic Background:** Khắc phục triệt để lỗi hiển thị độ trong suốt của hình nền Mica/Acrylic khi thay đổi chủ đề, mang lại trải nghiệm thị giác nhất quán.

#### 2. 🌐 Hoàn thiện hệ thống Đa ngôn ngữ (Localization Enhancements)
* **Bản dịch toàn diện:** Rà soát và dịch thuật toàn bộ các chuỗi ký tự cứng còn sót lại sang Tiếng Việt và Tiếng Anh.
* **Chuyển đổi tức thì:** Khắc phục lỗi hiển thị ngôn ngữ không đồng bộ khi chuyển đổi giữa các tab chức năng.

#### 3. ⚡ Cải tiến điều hướng & Hiệu năng khởi chạy (Navigation & Startup Performance)
* **Khởi động nhanh:** Cải tiến luồng tải tài nguyên giúp ứng dụng khởi chạy nhanh hơn và mượt mà hơn.
* **Điều hướng ổn định:** Tối ưu hóa bộ nhớ khi chuyển đổi giữa các màn hình Dashboard, Cleaner, Startup, Settings...

#### 4. 🛠️ Sửa lỗi hiển thị thông tin phiên bản (About & Developer Version Fix)
* **Cập nhật thông tin About & Developer:** Khắc phục lỗi hiển thị phiên bản cũ `v1.2.0` tại trang Cài đặt (Settings), cập nhật chính xác lên phiên bản `v3.4.1 (Stable Release)`.

---

### 🩹 Chi tiết kỹ thuật & Thay đổi cấu hình
* Cập nhật phiên bản toàn hệ thống lên `3.4.1` tại các tệp tin cấu hình (`WinCarePro.csproj`, `Package.appxmanifest`, `update.json`, `MainWindow.xaml`, `setup.iss`).

---

## 🚀 WinCare Pro v3.4.0 — Tái cấu trúc mã nguồn & Tối ưu hóa hiệu năng

> **Phát hành:** 04/07/2026 · **Loại:** Bản cập nhật tính năng & Tối ưu hóa cấu trúc (Feature Update & Code Refactoring) · **Phiên bản trước:** v3.3.1

Bản cập nhật **v3.4.0** là bước tiến quan trọng trong việc chuẩn hóa cấu trúc dự án theo mô hình kiến trúc sạch (Clean Architecture/MVVM) và cải thiện hiệu năng toàn diện của các phân hệ cốt lõi.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.4.0 (Key Features)

#### 1. 📂 Tái cấu trúc thư mục dự án (Clean Architecture & Directory Restructuring)
* **Quy hoạch mã nguồn:** Quy hoạch lại toàn bộ mã nguồn vào các phân vùng chức năng rõ ràng:
  * `Core/`: Chứa các thành phần trợ giúp (Helpers) và mô hình dữ liệu (Models) dùng chung.
  * `Modules/`: Gom các View và ViewModel của từng màn hình thành một cụm chức năng khép kín (như Dashboard, Disk, Updates, ProcessManager...).
  * `Infrastructure/`: Nơi xử lý bộ nhớ đệm (Caching), cơ sở dữ liệu (Database), ghi nhật ký (Logging) và bảo mật hệ thống.
  * `Services/`: Các dịch vụ nghiệp vụ chính được thiết kế dạng Interface/Implementation tách biệt.
  * `Shared/`: Chứa các Component tùy biến và Converter dùng chung trên giao diện.
* **Vệ sinh tài liệu:** Loại bỏ tài liệu kiến trúc cũ đã lỗi thời (`docs/ARCHITECTURE.md`) để đồng bộ với cấu trúc mới.

#### 2. ⚡ Tối ưu hóa phân hệ Quản lý Tiến trình (Process Manager Stability)
* **Khắc phục lỗi nạp tiến trình:** Sử dụng cơ chế `SemaphoreSlim` (`_querySemaphore`) để đồng bộ hóa việc truy vấn tài nguyên hệ thống, ngăn ngừa triệt để tình trạng xung đột truy cập tài nguyên do gọi đồng thời, qua đó chấm dứt hiện tượng giao diện bị đơ/đóng băng khi tải danh sách tiến trình.

#### 3. 🖥️ Tinh chỉnh trải nghiệm đóng ứng dụng (Exit & Shutdown Experience)
* **Đóng nhanh không độ trễ:** Nút đóng (Close) mặc định trên cửa sổ chính nay sẽ bỏ qua hoạt động hiển thị màn hình chờ tắt máy (Shutdown overlay), giải phóng khay hệ thống (Tray Icon) ngay lập tức và thu nhỏ hoặc đóng ứng dụng tức thì theo cấu hình người dùng.
* **Hoạt ảnh tắt máy chuyên biệt:** Nút nguồn đỏ tắt máy (Shutdown) được thiết kế hoạt ảnh tắt máy mượt mà riêng, đảm bảo thoát hoàn toàn ứng dụng ra khỏi nền ngay cả khi đang bật chế độ "Chạy ngầm" (Run in background).

#### 4. 🌐 Cải tiến Trung tâm Mạng (Network Center Layout & Anti-Flicker)
* **Trải nghiệm mượt mà:** Khắc phục triệt để tình trạng nháy giao diện (flicker) khi cập nhật dữ liệu lưu lượng mạng theo thời gian thực; tối ưu hóa layout hiển thị giúp phản hồi linh hoạt hơn trên các độ phân giải màn hình khác nhau.

#### 5. 🩹 Tối ưu hệ thống tự động cập nhật (Updater Reliability)
* **Phòng ngừa treo tiến trình:** Tinh chỉnh cơ chế so sánh Task bất đồng bộ khi gọi `process.WaitForExitAsync()` thông qua biến cục bộ thay vì tạo mới instance mỗi lần kiểm tra trong `Task.WhenAny`, loại bỏ hoàn toàn nguy cơ tranh chấp tài nguyên (race conditions) khi nâng cấp ứng dụng.

---

### 🩹 Chi tiết kỹ thuật & Thay đổi cấu hình
* Cập nhật `.gitignore` loại bỏ các thư mục làm việc tạm thời `agents/` và `plants/`.
* Nâng cấp phiên bản toàn hệ thống lên `3.4.0` tại các tệp tin cấu hình dự án (`WinCarePro.csproj`, `Package.appxmanifest`, `update.json`, `MainWindow.xaml`, `setup.iss`).

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
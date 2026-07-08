# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.2 — Sửa lỗi tương thích & Tăng cường tính ổn định

> **Phát hành:** 08/07/2026 · **Loại:** Bản cập nhật sửa lỗi & Tối ưu hóa (Bug Fixes & Stability Updates) · **Phiên bản trước:** v3.4.1

Bản cập nhật **v3.4.2** tập trung khắc phục một số lỗi tương thích hệ thống trên các phiên bản Windows cũ, cải tiến cơ chế quản lý vòng đời tiến trình giám sát nền và tối ưu hóa xử lý lỗi ngoại lệ trong các mô-đun hệ thống để mang lại hiệu suất sử dụng ổn định nhất.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.4.2 (Key Features)

#### 1. ⚙️ Khắc phục lỗi tương thích trên Windows cũ (OS Compatibility Fix)
* **Uninstall Module:** Khắc phục triệt để lỗi gây tắt ứng dụng đột ngột khi cố gắng lấy danh sách UWP apps (`AppListEntries`) trên các phiên bản Windows cũ hơn Windows 10 Build 19041 nhờ bổ sung cơ chế kiểm tra `OperatingSystem.IsWindowsVersionAtLeast`.

#### 2. 📊 Sửa lỗi biểu đồ Dashboard (Dashboard Chart Fix)
* **LiveCharts2 Integration:** Sửa lỗi cảnh báo kết xuất biểu đồ giám sát hiệu năng CPU/RAM bằng cách cập nhật thuộc tính cấu hình tìm kiếm sang `FindingStrategy` tương thích tốt hơn.

#### 3. ⚡ Quản lý tài nguyên & Luồng nền an toàn (Thread Safety & Resource Lifecycle)
* **Hardware & Process Monitors:** Nâng cấp cơ chế đồng bộ luồng nền bằng cách áp dụng khóa nguyên tử `Interlocked` tránh khởi tạo trùng lặp tiến trình giám sát, đồng thời đảm bảo việc cập nhật dữ liệu tiến trình (Process Page) luôn được đẩy về luồng UI (`DispatcherQueue`) một cách an toàn.
* **Network Monitor:** Tối ưu hóa giải phóng tài nguyên tiến trình nền ngay khi chuyển trang hoặc đóng giao diện mạng.

#### 4. 🛡️ Tăng cường xử lý ngoại lệ (Graceful Exception Fallback)
* **Settings & Network:** Bổ sung xử lý lỗi `FileNotFoundException` khi gọi API HttpClient kiểm tra phiên bản mới hoặc gọi API truy xuất thông tin DNS/Network Adapter. Đảm bảo ứng dụng vẫn hoạt động bình thường kể cả khi một số thư viện mạng của hệ thống không thể tải thành công.

---

### 🩹 Chi tiết kỹ thuật & Thay đổi cấu hình
* Cập nhật phiên bản toàn hệ thống lên `3.4.2` tại các tệp tin cấu hình (`WinCarePro.csproj`, `Package.appxmanifest`, `update.json`, `MainWindow.xaml`, `setup.iss`).

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
<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
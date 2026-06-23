# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v2.0.5 (Bản cập nhật tối ưu & Sửa lỗi dịch thuật/tiêu đề)

Chào mừng bạn đến với **WinCare Pro v2.0.5**! Bản cập nhật này sửa các lỗi giao diện quan trọng và tối ưu hóa hệ thống chuyển đổi ngôn ngữ động, giúp nâng cao trải nghiệm sử dụng.

### 🌟 Các cải tiến & Khắc phục lỗi (Improvements & Fixes)
* **Hệ thống dịch thuật động (Dynamic Translation Manager):** Tích hợp `TranslationManager` quản lý trực tiếp bộ dịch trong ứng dụng. Giờ đây khi bạn chuyển đổi ngôn ngữ (Tiếng Anh ⇆ Tiếng Việt) tại mục Cài đặt, toàn bộ giao diện, các bảng điều khiển, nút bấm, menu Sidebar và hộp thoại sẽ cập nhật ngay lập tức mà không cần phải khởi động lại ứng dụng.
* **Sửa lỗi tiêu đề trang hiển thị sai (Page Header Title Fix):** Sửa triệt để lỗi tiêu đề thanh Header của ứng dụng luôn hiển thị cố định là "Dashboard" bất kể người dùng đang chọn trang chức năng nào hoặc ẩn đi theo cài đặt. Các tiêu đề trang giờ đây hiển thị chính xác theo tên của từng trang chức năng đang chọn (ví dụ: "Dọn tệp rác", "Thông tin phần cứng",...) bằng ngôn ngữ tương ứng.
* **Tối ưu hóa hiệu năng & Rò rỉ tài nguyên:** 
  * Cải tiến vòng lặp quét và xử lý của các dịch vụ chạy ngầm (`ProcessService`, `JunkCleanerEngine`, `DiskEngine`, `StartupEngine`, `SystemOptimizerEngine`) để loại bỏ hoàn toàn các lỗi rò rỉ bộ nhớ (Memory Leaks) và treo luồng khi chuyển đổi nhanh giữa các trang chức năng.
  * Cấu trúc lại các ViewModels giúp cập nhật trạng thái UI mượt mà hơn.
* **Đồng bộ màu sắc & Nền sáng (Light Mode Syncing):** Đồng bộ hóa và cải thiện hiển thị của các nút bấm trên thanh tiêu đề tùy chỉnh (custom titlebar) khi sử dụng giao diện nền sáng (Light Mode).

### 💾 Hướng dẫn Cập nhật lên v2.0.5
1. Mở ứng dụng **WinCare Pro** hiện tại của bạn.
2. Di chuyển đến mục **Cài đặt (Settings)**.
3. Nhấp chọn **Check for Updates**.
4. Ứng dụng sẽ phát hiện phiên bản mới `2.0.5` thông qua cấu hình `update.json` tự động.
5. Nhấn chọn **Update Now** để tự động tải về và nâng cấp thông qua Silent Installer.
6. *Hoặc* tải trực tiếp file cài đặt mới nhất tại mục **Releases** trên GitHub: [Releases của WinCarePro](https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/tag/v2.0.5).

---

## 🚀 WinCare Pro v2.0.0 (Bản nâng cấp lớn)

Chào mừng bạn đến với **WinCare Pro v2.0.0**! Đây là một cột mốc quan trọng, tái cấu trúc toàn diện kiến trúc phần mềm, khôi phục các module dọn dẹp Registry, Bảo mật & Riêng tư, Quản lý Driver và đặc biệt là tích hợp **Chẩn đoán Hệ thống bằng Trí tuệ Nhân tạo (AI Diagnostics)**.

### 🌟 Điểm nổi bật (Highlights)
* **Trợ lý Chẩn đoán Hệ thống bằng AI (AI Diagnostics):** Tích hợp động cơ chẩn đoán thông minh, tự động phân tích sức khỏe hệ thống, dung lượng đĩa, RAM và các lỗ hổng bảo mật để đưa ra đề xuất tối ưu hóa cá nhân hóa.
* **Trung tâm Bảo mật & Riêng tư (Security & Privacy):** Bảo vệ thông tin cá nhân, dọn dẹp các tệp tin theo dõi và tối ưu hóa cài đặt bảo mật của Windows.
* **Dọn dẹp & Tối ưu Registry (Registry Cleaner):** Quét và sửa các lỗi Registry phân mảnh, khóa Registry không hợp lệ để tăng độ ổn định của hệ thống.
* **Quản lý Driver & Thiết bị phần cứng (Hardware Drivers):** Quét danh sách driver hiện tại, hiển thị trạng thái và đề xuất cập nhật các driver lỗi thời.
* **Thông số phần cứng chi tiết (Hardware Specs):** Cung cấp thông tin chi tiết cấu hình CPU, GPU, RAM, Bo mạch chủ, BIOS và hệ thống.
* **Tái cấu trúc Kiến trúc (Refactoring MVVM):** Tổ chức lại các ViewModels (ví dụ: `JunkViewModel`, `DiskViewModel`, `DriverViewModel`, `HardwareViewModel`, `RegistryViewModel`, `SecurityViewModel`) giúp ứng dụng mượt mà, phản hồi nhanh và giảm tiêu hao tài nguyên.

### 💾 Hướng dẫn Cập nhật lên v2.0.0
1. Mở ứng dụng **WinCare Pro** hiện tại của bạn.
2. Di chuyển đến mục **Cài đặt (Settings)**.
3. Nhấp chọn **Check for Updates**.
4. Ứng dụng sẽ phát hiện phiên bản mới `2.0.0` thông qua tệp `update.json` đã cấu hình.
5. Nhấp chọn **Update Now** để tự động tải về và chạy Silent Install cài đè phiên bản mới mà không cần thao tác thêm.
6. *Hoặc* tải trực tiếp file cài đặt từ mục **Releases** trên GitHub: [Releases của WinCarePro](https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/tag/v2.0.0).

---

## 🚀 WinCare Pro v1.0.3 (Phiên bản trước)

Chào mừng bạn đến với phiên bản **WinCare Pro v1.0.3**! Đây là một bản cập nhật đột phá mang tính tối giản hệ thống, tối ưu hóa trải nghiệm người dùng, sửa các lỗi crash nghiêm trọng và bổ sung tính năng nâng cấp tự động hoàn toàn không cần tương tác.

---

## 🚀 Điểm nổi bật (Highlights)

* **Trình gỡ cài đặt ứng dụng chuyên nghiệp (App Uninstaller):** Bổ sung tính năng gỡ cài đặt ứng dụng tận gốc. Không chỉ chạy trình gỡ cài đặt mặc định, WinCare Pro còn quét và dọn sạch các tệp tin cấu hình dư thừa, thư mục rác trong AppData/ProgramData, và các khóa Registry còn sót lại của ứng dụng.
* **Cập nhật tự động chạy ngầm (Silent Auto-Update):** Quy trình cập nhật phiên bản mới thông qua GitHub Releases giờ đây diễn ra hoàn toàn tự động. Khi bấm cập nhật, ứng dụng sẽ tải bản cài và tự động chạy cài đè ngầm (`Silent Install`), đồng thời tự đóng phiên bản cũ mà người dùng không cần bấm qua các bước cài đặt thủ công.
* **Tinh gọn ứng dụng & Giao diện tối giản:** Loại bỏ các tính năng bảo trì ít sử dụng bao gồm: "Security & Privacy", "Registry & Backup", "Hardware Specs", và "Log & Report" giúp ứng dụng nhẹ hơn, tập trung sâu vào các tính năng dọn dẹp và tối ưu hóa hệ thống cốt lõi.
* **Khắc phục lỗi Crash ở Network Center:** Sửa lỗi ứng dụng bị đóng đột ngột khi người dùng chuyển đổi qua lại giữa các trang chức năng trong Trung tâm Mạng (Network Center).

---

## 🔍 Chi tiết các thay đổi (Detailed Changelog)

### 🛠️ 1. Tính năng mới: Gỡ cài đặt chuyên nghiệp
* **Quét và gỡ cài đặt tận gốc (`UninstallEngine.cs` & `UninstallPage`):**
  * Liệt kê đầy đủ các ứng dụng đã cài đặt trên hệ thống (bao gồm cả Desktop Apps và Windows Store Apps).
  * Tìm kiếm và xóa sạch các tệp tin dư thừa trong `C:\Program Files`, `C:\Program Files (x86)`, `%AppData%`, và `%LocalAppData%`.
  * Quét Registry và loại bỏ các khóa đăng ký rác liên quan đến ứng dụng đã gỡ để tránh làm phân mảnh hệ thống.
  * Hiển thị dung lượng chi tiết của từng ứng dụng giúp người dùng dễ dàng dọn dẹp bộ nhớ ổ cứng.

### 📦 2. Hệ thống cập nhật tự động chạy ngầm (Silent Updater)
* **Cải tiến đối số cài đặt (`SettingsPage.xaml.cs`):**
  * Tích hợp các tham số tự động cài đặt ngầm vào tiến trình kích hoạt bộ cài: `/SILENT /SP- /NOICONS /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS`.
  * Tự động tắt ứng dụng hiện tại để giải phóng file thực thi `WinCarePro.exe` cho quá trình ghi đè phiên bản mới.
* **Cập nhật tệp cấu hình đóng gói (`setup.iss`):**
  * Bổ sung thuộc tính `CloseApplications=force` trong mục `[Setup]` để bộ cài tự động buộc đóng các file liên kết đang chạy ngầm của phiên bản cũ, triệt tiêu lỗi khóa file khi cập nhật.

### 🎨 3. Tinh gọn Giao diện & Menu Điều hướng
* **Tối giản hóa UI:**
  * Xóa bỏ hoàn toàn mã nguồn các trang và ViewModel không cần thiết: `SecurityPrivacyPage`, `RegistryBackupPage`, `HardwarePage`, và `LogsPage`.
  * Làm gọn menu Sidebar của ứng dụng để người dùng tập trung vào các công cụ cốt lõi: Dashboard, Dọn rác, Quản lý tiến trình, Khởi động, Quản lý đĩa, Gỡ ứng dụng, và Cài đặt.

### ⚡ 4. Sửa lỗi & Tối ưu độ ổn định
* **Sửa lỗi crash tại Network Center:**
  * Khắc phục triệt để hiện tượng xung đột tài nguyên/luồng xử lý gây crash ứng dụng khi chuyển trang con trong tab Network Center.

---

## 💾 Hướng dẫn Cập nhật lên v1.0.3

1. Mở ứng dụng **WinCare Pro** hiện tại của bạn.
2. Di chuyển đến mục **Cài đặt (Settings)**.
3. Nhấp chọn **Check for Updates**.
4. Ứng dụng sẽ phát hiện phiên bản mới `1.0.3` thông qua tệp `update.json`.
5. Nhấp chọn **Update Now**. Quá trình tải xuống và tự động cài đè ngầm sẽ diễn ra. Ứng dụng sẽ tự động đóng và nâng cấp lên bản mới.
6. *Hoặc* bạn có thể tải trực tiếp file cài đặt mới nhất từ trang [Releases của WinCarePro](https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/tag/v1.0.3).

---
*Cảm ơn bạn đã tin dùng WinCare Pro! Chúng tôi luôn nỗ lực mang lại trải nghiệm chăm sóc máy tính Windows tốt nhất cho bạn.*

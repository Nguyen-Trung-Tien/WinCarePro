# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v4.0.0 — Đột Phá Giao Diện Aura Glass, AI Health Copilot Engine, Single-Instance HUD Widget & Registry Undo System

> **Phát hành:** 06/08/2026 · **Loại:** Bản nâng cấp Đại thế hệ (Major Generation Evolution Release)

Bản cập nhật **v4.0.0** đánh dấu bước chuyển mình toàn diện nhất của WinCare Pro kể từ trước đến nay. Phiên bản mới mang đến ngôn ngữ thiết kế **Aura Glassmorphic Fluent 2.0**, hệ thống chuyển động **Visual Composition 120 FPS**, Trợ lý **AI Health Copilot Engine** chẩn đoán và dự đoán rủi ro hệ thống, cửa sổ nổi **Desktop HUD Widget** đơn thể (Single-Instance), cơ chế **Hoàn tác Registry (Undo/Rollback System)** an toàn tuyệt đối, cùng hàng loạt cải tiến về hiệu năng và giao diện phản hồi linh hoạt (Responsive UI).

---

### ✨ Các tính năng & cải tiến đột phá trong phiên bản v4.0.0 (Key Features)

* **🤖 Trợ lý AI Health Copilot Engine (Predictive AI Analytics):**
  * Tích hợp thuật toán AI chẩn đoán sức khỏe PC nâng cao (`AiDiagnosticsEngine`), phân tích dữ liệu phần cứng và tự động chấm điểm **Composite Health Score (0 - 100)**.
  * **Dự đoán thông minh:** Dự đoán chính xác số ngày còn lại trước khi ổ C: bị cạn kiệt dung lượng và ước tính số giây khởi động hệ thống có thể tối ưu thêm.
  * Tự động phân loại khuyến nghị bảo trì theo các cấp độ rủi ro trực quan (**Critical**, **High**, **Medium**).

* **🪟 Cửa sổ Desktop HUD Widget Đơn Thể (Single-Instance TopMost HUD):**
  * Cửa sổ Widget mini mờ kính sang trọng ghim nổi trên màn hình (`IsAlwaysOnTop = true`), cho phép theo dõi thời gian thực chỉ số CPU, RAM và tốc độ Mạng (Download/Upload speed).
  * Kích hoạt cơ chế kiểm soát đơn thể **Single-Instance Focus**: ngăn ngừa tạo trùng lặp cửa sổ khi mở từ Dashboard hoặc khay hệ thống (System Tray).

* **🛡️ Quản trị Registry & Hệ thống Hoàn tác An toàn (Registry Rollback System):**
  * Tích hợp cơ chế **Undo / Rollback System** cho phép sao lưu và khôi phục trạng thái Registry tức thì trước khi áp dụng bất kỳ tinh chỉnh nào.
  * Cung cấp các nút truy cập nhanh an toàn tới **Registry Editor** (`regedit`) và **System Restore** (`rstrui`) để tạo điểm khôi phục hệ thống chỉ với 1-Click.

* **⚡ Chế độ Gaming Turbo 2.0 Suite & Giải phóng RAM Tức thì:**
  * Công cụ **RAM Booster** dọn dẹp vùng nhớ làm việc (Working Set) của các tiến trình, giải phóng dung lượng RAM vật lý ngay lập tức mà không gây gián đoạn ứng dụng.
  * Kích hoạt chế độ năng lượng **Ultimate Performance** và ưu tiên tài nguyên CPU cho các ứng dụng / game mượt mà hơn.

* **🌐 Giám sát Mạng Responsive & Secure DNS (DoH Engine):**
  * Giao diện **Network Page** được tái cấu trúc hoàn toàn với khả năng tự động co giãn linh hoạt (Adaptive Responsive Layout) phù hợp với mọi kích thước màn hình.
  * Nâng cấp bộ đo tốc độ mạng **SpeedTest** kép (Dual-endpoint benchmarking) đo lường chính xác Ping, Download và Upload.
  * Tích hợp cấu hình **Secure DNS over HTTPS (DoH)** trực tiếp từ UI hỗ trợ các nhà cung cấp uy tín (Cloudflare, Google, AdGuard, NextDNS).

* **📂 Quản lý Khởi động (Startup) & Dịch vụ Hệ thống (Services):**
  * Tích hợp trang quản lý khởi động chuyên biệt với đánh giá mức độ ảnh hưởng (Impact Rating) đến thời gian boot máy.
  * Phân hệ quản lý Services cho phép kiểm soát, bật/tắt các dịch vụ ngầm an toàn kèm khả năng tìm kiếm và lọc dữ liệu thông minh.

* **🔄 Cập nhật Phần mềm Bên Thứ Ba (Software Updater Engine):**
  * Tích hợp động cơ quét ứng dụng cũ `SoftwareUpdaterEngine` hỗ trợ kiểm tra phiên bản mới thông qua **Windows Package Manager (winget)** hoặc trình tải trực tiếp.
  * Lưu trữ cấu hình tự động cập nhật ngầm với cơ chế lưu đệm chống ghi trùng lặp (Debouncing persistence).

* **🌐 Tối Ưu Đa Ngôn Ngữ 100% (Bi-directional Multi-Language Engine):**
  * Nâng cấp cơ chế dịch thuật hai chiều **English ↔ Tiếng Việt** mượt mà 100% trên toàn bộ 13 phân hệ trang. Chuyển đổi giao diện ngay lập tức mà không cần khởi động lại ứng dụng.

* **🧪 Đảm bảo Chất lượng & Kiểm thử Tự động (100% Unit Test Pass):**
  * Xây dựng bộ kiểm thử tự động toàn diện (`WinCarePro.Tests`) phủ kín các engine cốt lõi: AI Diagnostics, System Optimizer, Junk Cleaner, Startup Engine, Network Engine, Settings Persistence và SQLite DbManager.
  * Kết quả kiểm thử đạt **100% Pass (67/67 unit tests)**, bảo đảm độ tin cậy tối đa cho hệ thống.

---

<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
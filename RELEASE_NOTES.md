# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.4.4 — Nâng cấp hệ thống & Đồng bộ trải nghiệm người dùng

> **Phát hành:** 14/07/2026 · **Loại:** Bản cập nhật tính năng & Ổn định hệ thống (Feature & Stability Update) · **Phiên bản trước:** v3.4.3 (Đã loại bỏ)

Bản cập nhật **v3.4.4** tập trung vào việc nâng cấp trải nghiệm người dùng, cải tiến cơ chế sao lưu Registry để đảm bảo an toàn tối đa cho hệ thống, tối ưu hóa giao diện Trung tâm mạng (Network Center) linh hoạt theo kích thước hiển thị, và nâng cao độ tin cậy của quá trình gỡ cài đặt ứng dụng Microsoft Store.

---

### ✨ Các cải tiến nổi bật trong phiên bản v3.4.4 (Key Features)

#### 1. 🛡️ Nâng cấp Động cơ Sao lưu Registry (Registry Backup Engine)
* **Xuất thêm các khóa hệ thống HKLM:** Ngoài việc sao lưu nhánh HKCU an toàn, hệ thống hiện tự động xuất và bổ sung thêm các khóa Registry HKLM quan trọng liên quan đến tinh chỉnh hệ thống (như FileSystem, SystemProfile, GraphicsDrivers) vào file sao lưu chung, giúp quá trình khôi phục đầy đủ hơn.

#### 2. ⚡ Tối ưu hóa Trình gỡ cài đặt (Uninstall Engine)
* **Giới hạn thời gian chờ (Timeout):** Bổ sung giới hạn thời gian chờ 30 giây cho tiến trình PowerShell khi gỡ cài đặt ứng dụng Microsoft Store, ngăn chặn hoàn toàn hiện tượng ứng dụng bị treo vô hạn nếu hệ thống phản hồi chậm hoặc tiến trình ngầm bị kẹt.

#### 3. 🎨 Đồng bộ hóa Chủ đề & Tối ưu hóa giao diện (Theme & UI Improvements)
* **Đồng bộ hóa màu sắc chủ đề:** Cập nhật lớp `ThemeManager` để tự động đổi màu đồng bộ các cọ vẽ màu đơn (`PrimaryAccentBrush`, `PrimaryAccentLightBrush`, `PrimaryAccentBorderBrush`) khi người dùng thay đổi chủ đề hoặc màu chủ đạo của ứng dụng.
* **Tối ưu hóa bố cục Trung tâm Mạng:** Tinh chỉnh thiết kế của trang Network Page để tự động thay đổi cách sắp xếp các nút bấm, ô kiểm tra trên các độ phân giải màn hình khác nhau (Medium và Narrow layout), tránh bị che khuất hoặc tràn văn bản bằng thuộc tính rút gọn thông minh (`TextTrimming="CharacterEllipsis"`).

#### 4. 🌐 Cải thiện Bản dịch (Localization Update)
* **Bổ sung dịch thuật mô tả chi tiết:** Thêm các bản dịch và chú giải công cụ (Tooltip) tiếng Việt đầy đủ cho các tùy chọn cài đặt nâng cao trong trang Settings, giúp người dùng dễ dàng hiểu rõ tính năng trước khi áp dụng.

---

### 🩹 Chi tiết kỹ thuật & Thay đổi cấu hình
* Cập nhật phiên bản toàn hệ thống lên `3.4.4` tại các tệp tin cấu hình (`WinCarePro.csproj`, `Package.appxmanifest`, `update.json`, `MainWindow.xaml`, `setup.iss`).

---
<div align="center">
  <sub>Bản quyền phát hành thuộc về <b>Nguyễn Trung Tiến</b></sub>
</div>
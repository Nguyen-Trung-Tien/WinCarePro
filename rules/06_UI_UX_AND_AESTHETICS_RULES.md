# 🎨 06. Quy Chuẩn Giao Diện Aura Glass & Trải Nghiệm Người Dùng (UI/UX Rules)

> [⬅️ 05. Chuẩn Mã Nguồn C# 13](05_CODING_STANDARDS_AND_CONVENTIONS.md) • [🏠 Mục Lục Rules](README.md) • **Rule 06** • [Quy Chuẩn Tiếp Theo: 07. Kiểm Thử & QA ➡️](07_TESTING_AND_QA_RULES.md)

---

## 💎 1. Ngôn Ngữ Thiết Kế Aura Glassmorphic Fluent 2.0

Mọi màn hình và thành phần giao diện người dùng phải tuân thủ các nguyên tắc thiết kế hiện đại của WinUI 3:

1. **Hiệu ứng Kính Mờ & Chiều Sâu (Backdrop Materials):**
   - Sử dụng **Mica** hoặc **MicaAlt** làm nền cửa sổ chính trên Windows 11.
   - Sử dụng **Desktop Acrylic** cho các cửa sổ dạng lớp nổi (như Desktop HUD Widget, Flyouts, Menu chuột phải).
2. **Bo tròn góc & Viền Gradient:**
   - Các Card chứa thông tin phải áp dụng bo góc mềm mại `CornerRadius="8"` hoặc `"12"`.
   - Viền Card sử dụng độ dày mỏng `BorderThickness="1"` với màu trong suốt nhẹ (`rgba(255, 255, 255, 0.08)` trong Dark Theme).

---

## 🎬 2. Quy Chuẩn Hoạt Cảnh & Chuyển Động (Composition Animations)

1. **Tốc độ phản hồi và nhịp độ (Timing & Easing):**
   - Hoạt cảnh chuyển trang hoặc mở rộng Card không được vượt quá $250\text{ ms}$.
   - Sử dụng hàm gia tốc mượt mà Cubic-Bezier (`EaseInOut` hoặc `FastOutSlowIn`).
2. **Staggered Entrance Loading:**
   - Khi tải danh sách (như danh sách tệp rác hoặc phần mềm cần gỡ), các hàng phần tử phải xuất hiện so le tuần tự (Staggered Animation với độ trễ $30\text{ ms}$ mỗi hàng) để tạo cảm giác sống động.
3. **Shimmer Skeleton Loading:**
   - Khi dữ liệu đang được quét hoặc tải ngầm, hiển thị khung xương mờ có hiệu ứng quét sáng (Shimmer) thay vì chỉ dùng một vòng quay tải đơn điệu (`ProgressRing`).

---

## 👁️ 3. Tiêu Chuẩn Tiếp Cận & Độ Tương Phản (Accessibility & WCAG AAA)

1. **Tương phản văn bản:**
   - Tỷ lệ tương phản màu giữa chữ và nền phải đạt tối thiểu $4.5:1$ cho văn bản thường và $3:1$ cho tiêu đề lớn theo chuẩn WCAG 2.1.
2. **Hỗ trợ điều hướng bàn phím (Tab Navigation):**
   - Mọi nút bấm, hộp thoại nhập liệu và danh sách chọn phải có trạng thái lấy nét trực quan (`FocusVisualPrimaryBrush`) khi di chuyển bằng phím `Tab` hoặc phím mũi tên.
3. **Tooltip & Mô tả hành động:**
   - Các nút bấm dạng biểu tượng (Icon-only buttons) bắt buộc phải có `ToolTipService.ToolTip` giải thích rõ hành động tương ứng.

---

## 🎨 4. Quy Chuẩn Bảng Màu Theme Studio

Không sử dụng các màu sắc thô không đồng nhất. Mọi màu sắc phải trỏ tới hệ thống Resource Keys định nghĩa trong Theme Studio:

| Vai trò | Resource Key | Ý nghĩa trạng thái |
| :--- | :--- | :--- |
| **Màu nhấn** | `SystemAccentColor` / `AuraAccentBrush` | Màu thương hiệu chủ đạo, nút bấm hành động chính |
| **Thành công / An toàn** | `SuccessStatusBrush` (`#2EA043`) | Điểm sức khỏe cao, dịch vụ an toàn, hoàn tất tối ưu |
| **Cảnh báo** | `WarningStatusBrush` (`#D29922`) | Bộ nhớ RAM cao, ổ cứng sắp đầy, file tạm tích tụ |
| **Nguy hiểm / Rủi ro** | `DangerStatusBrush` (`#F85149`) | Lỗ hổng bảo mật, lỗi ổ cứng S.M.A.R.T, malware alert |

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

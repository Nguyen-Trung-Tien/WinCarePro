---
name: wincare-ui-aura
description: >-
  Guidelines for building WinUI 3 Fluent 2.0 Aura Glass interfaces, styling cards, binding Tabular numerals, hooking WeakReferenceMessenger, avoiding memory leaks via Unloaded events, and registering dual vi-VN/en-US translations. Use when editing or creating XAML views and ViewModels.
---

# 🎨 WinCare Pro — Quy Chuẩn UI/UX Aura Glass & Thiết Kế WinUI 3

WinCare Pro áp dụng ngôn ngữ thiết kế **Aura Glass** dựa trên chuẩn Fluent 2.0 (Windows App SDK / WinUI 3) với hiệu ứng Mica Alt, bo tròn viền thẻ, chuyển động mượt mà và hỗ trợ đa ngôn ngữ hoàn toàn.

---

## 💎 1. Các Nguyên Tắc Thẩm Mỹ Cốt Lõi (Aura Glass Design System)

1. **Backdrop & Độ Trong Suốt:**
   - Sử dụng nền Mica Alt hoặc Acrylic với độ mờ tinh tế.
   - Các card nội dung dùng `Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"` kết hợp viền mờ `BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`.
2. **Bo Góc Nhất Quán:**
   - Card chính / Panel: `CornerRadius="8"`
   - Nút bấm / Input box: `CornerRadius="4"` hoặc `CornerRadius="6"`
   - Badge / Chip trạng thái: `CornerRadius="12"` (dạng viên thuốc bo tròn)
3. **Typography & Tabular Figures Cho Telemetry:**
   - Mọi con số chỉ số hệ thống (CPU %, RAM MB, Dung lượng GB/MB, Thời gian ms/giây) **bắt buộc** bật:
     ```xml
     <TextBlock Text="{Binding FormattedMemory}" 
                FontFamily="Segoe UI Variable Display"
                Typography.NumeralAlignment="Tabular"
                FontWeight="SemiBold"/>
     ```
   - Tabular figures giúp các con số có cùng bề ngang, ngăn chặn hiện tượng giật rung giao diện khi giá trị cập nhật liên tục.

---

## 🌐 2. Đa Ngôn Ngữ Bắt Buộc (i18n & Zero Hardcode)

- **CẤM:** Viết chuỗi văn bản cứng vào XAML:
  ```xml
  <!-- ❌ CẤM: Không thể dịch, vi phạm Rule 09 -->
  <Button Content="Bắt đầu quét"/>
  <TextBlock Text="Đã dọn dẹp thành công!"/>
  ```
- **BẮT BUỘC:** Ràng buộc qua `TranslationConverter`:
  ```xml
  <!-- ✅ ĐÚNG: Tự động đổi ngôn ngữ runtime -->
  <Button Content="{Binding Source='Btn_StartScan', Converter={StaticResource TranslationConverter}}"/>
  <TextBlock Text="{Binding Source='Msg_CleanSuccess', Converter={StaticResource TranslationConverter}}"/>
  ```
- Mọi khóa bản dịch (`Key`) phải được khai báo song song trong cả 2 từ điển `vi-VN` và `en-US` tại [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs).

---

## 🔄 3. Cơ Chế Luồng Giao Diện (Thread Safety & Zero Crash)

- Mọi tác vụ ngầm khi cần cập nhật dữ liệu hiển thị (ví dụ: gán thuộc tính `Text`, thêm phần tử vào `ObservableCollection`) **bắt buộc** phải bọc trong `RunOnUI()`:
  ```csharp
  RunOnUI(() =>
  {
      ScanResults.Add(newItem);
      ProgressValue = currentPercentage;
  });
  ```
- Tuyệt đối không dùng `task.Result` hoặc `task.Wait()` gây deadlock giao diện. Luôn dùng `await`.

---

## 🧹 4. Ngăn Ngừa Rò Rỉ Bộ Nhớ (Memory Leak Prevention)

- **Gỡ bỏ Event Handlers khi đóng trang:**
  Trong sự kiện `Unloaded` của View XAML hoặc phương thức dọn dẹp, phải gỡ bỏ toàn bộ event (`-=`) đã đăng ký:
  ```csharp
  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
      ViewModel.TelemetryUpdated -= OnTelemetryUpdated;
      Unloaded -= OnUnloaded;
  }
  ```
- **Dừng Animation / 3D Render:**
  Khi chuyển trang (Navigate), dừng các timer vẽ animation hoặc hiệu ứng xoay lặp để đưa CPU Idle $\le 0.3\%$ và RAM Foreground $\le 85\text{ MB}$.
- **Giao tiếp ViewModel qua Messenger:**
  Dùng `WeakReferenceMessenger.Default` để gửi nhận thông báo liên phân hệ, tránh tạo tham chiếu chéo gây giữ rác Garbage Collector.

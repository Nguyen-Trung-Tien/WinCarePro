# ⚡ 04. Quy Chuẩn Hiệu Năng & Quản Lý Bộ Nhớ (Performance & Memory Rules)

> [⬅️ 03. Đa Luồng & Bất Đồng Bộ](03_CONCURRENCY_AND_THREADING_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 04** • [Quy Chuẩn Tiếp Theo: 05. Chuẩn Mã Nguồn C# 13 ➡️](05_CODING_STANDARDS_AND_CONVENTIONS.md)

---

## 🏎️ 1. Ngân Sách Khởi Động & Tiêu Thụ Tài Nguyên (Performance Budgets)

Mọi bản build Production của WinCare Pro phải đáp ứng các chỉ tiêu khắt khe sau:

| Chỉ số hiệu năng | Ngưỡng giới hạn (Budget) | Phương pháp đo lường |
| :--- | :--- | :--- |
| **Cold Start (Khởi động lạnh)** | $\le 350\text{ ms}$ | Từ lúc gọi `.exe` đến khi xuất hiện khung giao diện chính |
| **RAM khi chạy tiền cảnh** | $\le 85\text{ MB}$ | Đang mở Dashboard và hiển thị dữ liệu live |
| **RAM khi thu nhỏ xuống khay hệ thống (System Tray)** | $\le 15\text{ MB}$ | Sau khi kích hoạt cơ chế `TrimProcessMemory()` |
| **Mức chiếm dụng CPU khi Idle** | $\le 0.3\%$ | Khi ứng dụng chỉ giám sát nền |
| **Độ mượt hoạt cảnh giao diện** | $120\text{ FPS}$ (hoặc tần số quét tối đa của màn hình) | Windows Composition Layer Render |

---

## 🧹 2. Cơ Chế Thu Gọn RAM Nền (`TrimProcessMemory`)

Khi ứng dụng được thu nhỏ xuống khay hệ thống (System Tray), hệ thống **bắt buộc** thực hiện quy trình giải phóng tài nguyên:

```csharp
public static void TrimProcessMemory()
{
    try
    {
        // 1. Kích hoạt thu gom rác thế hệ 2 đầy đủ
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        // 2. Thu hồi Working Set bộ nhớ vật lý qua Win32 API
        using var process = Process.GetCurrentProcess();
        NativeMethods.EmptyWorkingSet(process.Handle);
    }
    catch (Exception ex)
    {
        CrashLogger.LogWarning("TrimProcessMemory failed", ex);
    }
}
```

---

## 🚰 3. Chống Rò Rỉ Bộ Nhớ XAML (Memory Leak Prevention)

1. **Hủy đăng ký sự kiện (Event Unsubscription):**
   - Mọi sự kiện đăng ký với các đối tượng sống lâu (Singleton Services, `Window`, `DispatcherTimer`, `NetworkChange.NetworkAddressChanged`) **bắt buộc** phải được hủy đăng ký trong sự kiện `Unloaded` của View hoặc hàm `Dispose()` của ViewModel.
2. **Sử dụng `WeakReferenceMessenger`:**
   - Khi gửi thông điệp giữa các ViewModel hoặc Service, luôn sử dụng cơ chế liên kết yếu (Weak Reference) của `CommunityToolkit.Mvvm` để tránh việc đối tượng nhận tin không được GC thu hồi.
3. **Giải phóng tài nguyên không quản lý (`IDisposable`):**
   - Mọi đối tượng thực thi `IDisposable` (như `SqliteConnection`, `FileStream`, `Process`, `BitmapImage`) phải được bọc trong khối `using var` hoặc gọi `Dispose()` tường minh.

---

## 🖼️ 4. Quy Chuẩn Bộ Nhớ Đệm I/O & Icon (`IconCacheService`)

- **Không đọc đĩa lặp lại:** Tuyệt đối không gọi `SHGetFileInfo` hoặc `ExtractIcon` nhiều lần cho cùng một tệp thực thi.
- **Bộ nhớ đệm trong RAM:** Sử dụng `ConcurrentDictionary<string, ImageSource>` với giới hạn kích thước (LRU eviction nếu cần) để phục vụ việc hiển thị danh sách ứng dụng khởi động và tiến trình mượt mà $O(1)$.

---

## 📊 5. Chống Giật Số Giao Diện Telemetry (`Tabular Numerals`)

Để tránh hiện tượng các thẻ hiển thị CPU%, RAM%, Tốc độ mạng bị nhảy kích thước liên tục (Layout Jitter / Stutter) khi cập nhật dữ liệu mỗi giây:
- **Bắt buộc** khai báo thuộc tính phông chữ `Typography.NumeralAlignment="Tabular"` trên các TextBlock hiển thị số liệu kỹ thuật.

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

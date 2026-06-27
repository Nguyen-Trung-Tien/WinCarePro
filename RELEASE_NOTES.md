# 📝 Nhật ký Phát hành (Release Notes)

---

## 🚀 WinCare Pro v3.1.0 — Nâng cấp trình gỡ cài đặt ứng dụng (App Uninstaller Upgrade)

> **Phát hành:** 27/06/2026 · **Loại:** Nâng cấp tính năng (Feature Enhancement) · **Phiên bản trước:** v3.0.1

Bản cập nhật **v3.1.0** mang đến giao diện thiết kế mới hiện đại theo phong cách Master-Detail trực quan, bổ sung các chức năng gỡ cài đặt hàng loạt (Batch Uninstall), gỡ cài đặt cưỡng bức (Force Uninstall) cho cả ứng dụng Desktop truyền thống lẫn Windows Store, quét sâu để phát hiện và làm sạch các tệp tin/registry tàn dư sau khi gỡ bỏ ứng dụng.

### ✨ Các cải tiến nổi bật (Key Features)

#### 1. Giao diện Master-Detail hiện đại và trực quan hơn
* Bổ sung **4 thẻ thống kê nhanh (Quick Stats)** ở phía trên cùng: Tổng số ứng dụng, Số ứng dụng Desktop, Số ứng dụng Windows Store, Tổng dung lượng ổ đĩa bị chiếm dụng.
* Thiết kế Master-Detail giúp tối ưu hóa không gian hiển thị: danh sách ứng dụng hiển thị ngắn gọn ở cột trái, bảng thông tin chi tiết (specifications) hiển thị ở cột phải khi chọn ứng dụng.
* Bổ sung nhãn phân loại (badges) rõ ràng giữa **Desktop App** và **Store App**.
* Tích hợp bộ lọc phân loại ứng dụng nhanh chóng (Tất cả, Desktop, Windows Store) và chức năng sắp xếp nâng cao (theo tên A-Z/Z-A, theo dung lượng, hoặc theo ngày cài đặt).

#### 2. Tính năng Gỡ cài đặt nâng cao và Xử lý tàn dư (Leftovers Scanner)
* **Gỡ cài đặt hàng loạt (Batch Uninstall):** Hỗ trợ chọn hộp kiểm (checkbox) cạnh các ứng dụng và thực hiện gỡ cài đặt đồng thời thông qua thanh công cụ nổi (floating action bar) ở phía dưới cùng.
* **Gỡ cài đặt cưỡng bức (Force Uninstall):** Hỗ trợ gỡ bỏ triệt để các ứng dụng Windows Store không có trình gỡ cài đặt mặc định thông qua các câu lệnh PowerShell nâng cao.
* **Mở thư mục cài đặt & Registry:** Tích hợp nút chức năng mở trực tiếp thư mục cài đặt của ứng dụng trong Explorer, hoặc mở đường dẫn khóa registry của ứng dụng trong Regedit.
* **Tìm kiếm trực tuyến (Search Online):** Tích hợp nút tìm kiếm nhanh các thông tin gỡ cài đặt của ứng dụng trên trình duyệt web mặc định.
* **Quét và Dọn dẹp tàn dư (Leftovers Purger):** Sau khi ứng dụng được gỡ bỏ, hệ thống sẽ tự động quét các thư mục hệ thống tiêu chuẩn (`ProgramFiles`, `AppData`, `ProgramData`), danh sách shortcut Start Menu và Registry Hive (`HKLM`, `HKCU`) để tìm kiếm các thư mục/khóa tàn dư và cho phép người dùng xóa sạch chúng.

#### 3. Bản địa hóa Tiếng Việt đầy đủ (Vietnamese Localization)
* Bổ sung đầy đủ các chuỗi dịch nghĩa cho giao diện gỡ cài đặt mới, thống kê chi tiết và quá trình quét dọn tàn dư.

---

## 🩹 WinCare Pro v3.0.1 — Bản vá ổn định (Hotfix "Stability Patch")

> **Phát hành:** 24/06/2026 · **Loại:** Hotfix / Bugfix · **Phiên bản trước:** v3.0.0

Bản cập nhật **v3.0.1** tập trung hoàn toàn vào việc khắc phục các lỗi **crash / thoát đột ngột** xảy ra khi người dùng **click chuyển tab nhanh** giữa các trang trong menu điều hướng. Không có thay đổi về tính năng hay giao diện.

### 🔴 Lỗi được khắc phục (Critical Fixes)

#### 1. Race condition gây crash khi chuyển tab Dashboard ← → bất kỳ tab nào
* **Nguyên nhân gốc:** `DashboardViewModel.StartResourceMonitor()` sử dụng `bool _isRunning` đơn giản không đủ để ngăn 2 vòng lặp background chạy đồng thời. Khi người dùng navigate ra rồi vào nhanh, 2 thread cùng gọi `CpuSeriesValues.Add()` / `RemoveAt()` trên cùng `ObservableCollection` → **CollectionModifiedException → crash**.
* **Fix áp dụng (`DashboardViewModel.cs`):**
  * Thay `bool _isRunning` bằng `CancellationTokenSource` + `Interlocked.CompareExchange` để đảm bảo **chỉ duy nhất 1 monitoring loop** chạy tại bất kỳ thời điểm nào.
  * `Task.Delay(delayMs, token)` thay vì `Task.Delay(delayMs)` — thread hủy ngay lập tức khi navigate away, không phải đợi hết interval.
  * Thêm `?.` null-safe cho tất cả `_dispatcherQueue.TryEnqueue()` calls.

#### 2. `HardwareViewModel` — DispatcherQueue bị gọi sau khi đã bị thu hồi
* **Nguyên nhân gốc:** `StartSensorMonitoring()` vẫn đang `await Task.Delay(2000)` khi `StopMonitoring()` được gọi. Sau khi delay xong, thread tiếp tục gọi `_dispatcherQueue.TryEnqueue()` trên một Page đã bị hủy → **InvalidOperationException → crash**.
* **Fix áp dụng (`HardwareViewModel.cs`):**
  * Thay `bool _isRunning` bằng `CancellationTokenSource`.
  * Thêm `if (token.IsCancellationRequested) break;` trước mỗi lần dispatch.
  * `_dispatcherQueue?.TryEnqueue()` với null-safe operator.
  * `Task.Delay(2000, token)` với `catch (TaskCanceledException) { break; }`.

#### 3. `ProcessViewModel` — monitoring loop không dừng gracefully
* **Nguyên nhân gốc:** `StartRunningProcessesMonitor()` là `async Task` chạy bằng `_ = Task...` nhưng dùng `while (_isRunning)` — khi `StopMonitoring()` set `_isRunning = false`, thread đang trong `await Task.Run(GetRunningProcessesAsync)` không hay biết → tiếp tục dispatch `ApplyFilterAndSort()` vào collection đã bị detach.
* **Fix áp dụng (`ProcessViewModel.cs`):**
  * Chuyển sang `CancellationTokenSource` + truyền `token` vào `Task.Run(..., token)`.
  * `_dispatcherQueue?.TryEnqueue()` với null-safe và kiểm tra token trước khi update collection.

#### 4. `NetworkViewModel` — Event handler bị đăng ký nhiều lần (Memory leak + double dispatch)
* **Nguyên nhân gốc:** `NetworkPage` có `NavigationCacheMode = Required` (page được giữ trong bộ nhớ) nhưng `OnNavigatedTo` luôn gọi `ViewModel.Initialize()` → `_engine.OutputReceived += OnOutputReceived` bị gọi nhiều lần → mỗi message được xử lý N lần → `ConsoleOutput` tăng theo cấp số nhân → **OutOfMemoryException** khi dùng lâu.
* **Fix áp dụng (`NetworkViewModel.cs`):**
  * Thêm `_engine.OutputReceived -= OnOutputReceived;` **trước** `+=` trong `Initialize()` để đảm bảo idempotent subscription.

#### 5. `JunkViewModel` — Tương tự double-subscribe khi re-navigate
* **Fix áp dụng (`JunkViewModel.cs`):**
  * Thêm unsubscribe trước subscribe trong `Initialize()` (cùng pattern với NetworkViewModel).

#### 6. `DiskViewModel` — `engine.OutputReceived` không bao giờ được unsubscribe
* **Nguyên nhân gốc:** `_engine.OutputReceived += LogText` được gọi trong constructor nhưng không có đối ứng `-=` khi navigate away → memory leak và log text nhân đôi khi quay lại trang.
* **Fix áp dụng (`DiskViewModel.cs` + `DiskPage.xaml.cs`):**
  * Tách ra thành `SubscribeEvents()` / `UnsubscribeEvents()` được gọi từ `OnNavigatedTo` / `OnNavigatedFrom`.
  * `SubscribeEvents()` luôn unsubscribe trước khi subscribe để đảm bảo an toàn.

#### 7. `SecurityViewModel` — `_dispatcherQueue.TryEnqueue()` không null-safe
* **Nguyên nhân gốc:** Nhiều WMI Task đang chạy dở (async), nếu navigate away nhanh → `_dispatcherQueue` có thể null khi task hoàn thành → **NullReferenceException**.
* **Fix áp dụng (`SecurityViewModel.cs`):**
  * Thay tất cả `_dispatcherQueue.TryEnqueue()` bằng `_dispatcherQueue?.TryEnqueue()`.

### 🟡 Cải tiến phòng ngừa (Preventive Improvements)

| Trang | Thay đổi |
|-------|----------|
| `SecurityPage.xaml.cs` | Thêm `NavigationCacheMode = Required` + `OnNavigatedFrom` handler |
| `UninstallPage.xaml.cs` | Thêm `NavigationCacheMode = Required` — tránh registry scan lặp lại mỗi lần navigate |
| `StartupPage.xaml.cs` | Thêm `NavigationCacheMode = Required` |
| `RepairPage.xaml.cs` | Thêm `NavigationCacheMode = Required` |
| `SystemOptimizerPage.xaml.cs` | Thêm `NavigationCacheMode = Required` |

> **Lý do:** Với `NavigationCacheMode = Required`, WinUI giữ Page instance trong bộ nhớ. Khi người dùng quay lại trang, `OnNavigatedTo` được gọi lại nhưng constructor **không** chạy lại → ViewModel không bị tạo mới → không có background thread tích lũy.

### ✅ Kết quả kiểm tra

```
dotnet build → Build succeeded | 0 Error(s) | 6 Warning(s) (pre-existing)
dotnet run   → Build succeeded | App launched successfully
```

---
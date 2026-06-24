# 📝 Nhật ký Phát hành (Release Notes)

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
---
name: wincare-concurrency-guard
description: >-
  WinUI 3 threading and concurrency rules for WinCare Pro. Covers RunOnUI dispatching, safe async/await, RelayCommands with cancellation, background worker thread safety, avoiding sync-over-async deadlocks, and WeakReferenceMessenger message passing. Use whenever writing async methods, background tasks, or UI-updating code.
---

# ⚡ WinCare Pro — Quy Chuẩn Đa Luồng & Bất Đồng Bộ An Toàn (Concurrency Guard)

Môi trường **Windows App SDK (WinUI 3)** có mô hình Single-Threaded Apartment (STA) nghiêm ngặt cho UI Thread. Mọi vi phạm về luồng đều gây ra ngoại lệ `COMException (0x8001010E RPC_E_WRONG_THREAD)` hoặc gây đóng băng (freeze) giao diện người dùng.

---

## 🚫 1. Bảng Đối Chiếu Thao Tác Đa Luồng Bắt Buộc

| Tình huống | ❌ CẤM TUYỆT ĐỐI | ✅ BẮT BUỘC SỬ DỤNG |
| :--- | :--- | :--- |
| **Cập nhật UI từ Background Thread** | `Items.Add(item);` hoặc `StatusText = "...";` | `RunOnUI(() => { Items.Add(item); StatusText = "..."; });` |
| **Chờ tác vụ Async hoàn tất** | `var res = myTask.Result;` hoặc `myTask.Wait();` | `var res = await myTask;` (Không Sync-over-Async) |
| **Khai báo phương thức bất đồng bộ** | `public async void DoWork()` (trừ event handler) | `public async Task DoWorkAsync(CancellationToken ct = default)` |
| **Hủy bỏ tác vụ dài (Long-running)** | Bỏ qua `CancellationToken`, chạy ngầm không dừng | Nhận `CancellationToken ct`, kiểm tra `ct.ThrowIfCancellationRequested()` |
| **Thu hồi trạng thái bận (Busy State)** | Quên reset khi có Exception | Luôn đặt trong khối `finally { IsBusy = false; OperationState = OperationState.Idle; }` |
| **Giao tiếp giữa các ViewModel** | Giữ tham chiếu trực tiếp `_otherViewModel.DoSomething()` | Gửi thông điệp phân tán qua `WeakReferenceMessenger.Default.Send(...)` |

---

## 🧵 2. Quy Chuẩn `RunOnUI` & WinUI 3 DispatcherQueue

### Nguyên tắc vàng:
Mọi thuộc tính kích hoạt thông báo `INotifyPropertyChanged` hiển thị trên giao diện hoặc bất kỳ thao tác nào thay đổi `ObservableCollection<T>` khi đang ở luồng nền (ThreadPool, Task.Run, Timer, Background Engine Callback) **bắt buộc** phải bọc trong `RunOnUI()`.

```csharp
// Trong ViewModel kế thừa ViewModelBase:
public async Task LoadTelemetryAsync(CancellationToken ct)
{
    try
    {
        IsBusy = true;
        OperationState = OperationState.Running;

        // Xử lý tính toán nặng trên ThreadPool
        var report = await Task.Run(() => _telemetryEngine.CollectMetrics(ct), ct);

        // Đẩy kết quả về UI Thread an toàn
        RunOnUI(() =>
        {
            CpuUsage = report.CpuPercent;
            RamUsage = report.RamUsedMb;
            ProcessList.Clear();
            foreach (var proc in report.TopProcesses)
            {
                ProcessList.Add(proc);
            }
        });
    }
    catch (OperationCanceledException)
    {
        RunOnUI(() => StatusMessage = "Tác vụ đã được hủy bỏ.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(LoadTelemetryAsync));
        RunOnUI(() => StatusMessage = "Lỗi khi nạp dữ liệu telemetry.");
    }
    finally
    {
        RunOnUI(() =>
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        });
    }
}
```

---

## 🛑 3. Triệt Tiêu Deadlock (Anti-Sync-over-Async)

### ❌ Sai lầm chết người:
```csharp
// Gây DEADLOCK tức thì nếu gọi từ UI Thread!
var data = _engine.ScanAsync().Result;
_engine.CleanAsync().Wait();
```

### ✅ Chuẩn hóa bất đồng bộ:
1. Mọi phương thức I/O, Win32, Registry hoặc WMI có thời gian thực thi $> 50\text{ ms}$ phải được thiết kế dạng `Task` / `Task<T>`.
2. Hậu tố phương thức bắt buộc có chữ `Async` (ví dụ: `ScanJunkAsync`, `RepairFilesAsync`).
3. Luôn `await` từ đầu đến cuối chuỗi gọi (Async All The Way).

---

## 🎮 4. Mô Hình `[RelayCommand]` & Hủy Tác Vụ Chuẩn

Sử dụng bộ công cụ `CommunityToolkit.Mvvm`:

```csharp
[ObservableProperty]
private bool _isBusy;

[ObservableProperty]
private OperationState _operationState = OperationState.Idle;

// Tự động sinh ScanCommand và ScanCancelCommand
[RelayCommand(IncludeCancelCommand = true)]
private async Task ScanAsync(CancellationToken ct)
{
    try
    {
        IsBusy = true;
        OperationState = OperationState.Running;
        StatusMessage = "Đang quét hệ thống...";

        var result = await _engine.ExecuteScanAsync(progress: new Progress<int>(percent =>
        {
            RunOnUI(() => ScanProgress = percent);
        }), ct);

        if (result.Success)
        {
            RunOnUI(() => StatusMessage = "Quét hoàn tất.");
        }
    }
    catch (OperationCanceledException)
    {
        RunOnUI(() => StatusMessage = "Người dùng đã dừng quét.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(ScanAsync));
        RunOnUI(() => StatusMessage = "Đã xảy ra sự cố trong quá trình quét.");
    }
    finally
    {
        RunOnUI(() =>
        {
            IsBusy = false;
            OperationState = OperationState.Idle;
        });
    }
}
```

---

## 📡 5. Giao Tiếp Lỏng Qua `WeakReferenceMessenger`

Để ngăn ngừa rò rỉ bộ nhớ (Memory Leaks do giữ tham chiếu cứng) giữa các trang:

### Đăng ký nhận tin nhắn (trong ViewModel khởi tạo):
```csharp
WeakReferenceMessenger.Default.Register<SystemOptimizedMessage>(this, (r, m) =>
{
    RunOnUI(() =>
    {
        RefreshDashboard();
    });
});
```

### Hủy đăng ký khi dọn dẹp hoặc Unloaded:
```csharp
public override void Cleanup()
{
    WeakReferenceMessenger.Default.UnregisterAll(this);
    base.Cleanup();
}
```

### Phát tin nhắn (Broadcast):
```csharp
WeakReferenceMessenger.Default.Send(new SystemOptimizedMessage(freedBytes));
```

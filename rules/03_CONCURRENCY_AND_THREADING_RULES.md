# ⚡ 03. Quy Chuẩn Đa Luồng & Bất Đồng Bộ (Concurrency & Threading Rules)

> [⬅️ 02. An Toàn & Bảo Mật](02_SECURITY_AND_SAFETY_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 03** • [Quy Chuẩn Tiếp Theo: 04. Hiệu Năng & Quản Lý RAM ➡️](04_PERFORMANCE_AND_MEMORY_RULES.md)

---

## 🖥️ 1. Quy Tắc UI Dispatcher Thread (Zero-Crash Threading)

WinUI 3 (Windows App SDK) yêu cầu mọi cập nhật lên thuộc tính giao diện hoặc danh sách dữ liệu hiển thị (`ObservableCollection`) phải được thực thi trên **UI Thread**.

### 🔴 QUY TẮC BẮT BUỘC:
1. **Không bao giờ cập nhật UI từ Background Thread:** Tuyệt đối không thay đổi thuộc tính `[ObservableProperty]` hoặc thao tác trên `ObservableCollection` trực tiếp bên trong `Task.Run()` mà không chuyển luồng.
2. **Luôn sử dụng `RunOnUI` trong ViewModel:**

```csharp
// Trong ViewModelBase.cs:
protected void RunOnUI(Action action)
{
    if (App.MainWindow?.DispatcherQueue == null) return;

    if (App.MainWindow.DispatcherQueue.HasThreadAccess)
    {
        action();
    }
    else
    {
        App.MainWindow.DispatcherQueue.TryEnqueue(() => action());
    }
}
```

### ✅ Ví dụ chuẩn mực trong ViewModel:
```csharp
[RelayCommand]
public async Task ScanAsync()
{
    IsScanning = true; // Thuộc tính trên UI Thread
    
    // Đẩy toàn bộ tác vụ I/O nặng xuống luồng nền
    var result = await Task.Run(() => _junkEngine.ScanJunkItemsAsync());
    
    // Cập nhật kết quả an toàn lên UI Thread
    RunOnUI(() =>
    {
        JunkItems.Clear();
        foreach (var item in result.Data)
        {
            JunkItems.Add(item);
        }
        IsScanning = false;
    });
}
```

---

## 🛑 2. Quy Chuẩn Hủy Tác Vụ Hợp Tác (`CancellationToken`)

Mọi phương thức bất đồng bộ tốn thời gian (quét đĩa, tìm file trùng lặp, ping mạng, quét cập nhật) **bắt buộc** phải nhận tham số `CancellationToken ct = default` và kiểm tra định kỳ `ct.ThrowIfCancellationRequested()` hoặc truyền trực tiếp vào các lệnh I/O.

### Quy tắc kiểm tra nhịp độ (Pacing):
```csharp
public async Task<OperationResult<List<JunkItem>>> ScanDirectoryAsync(
    string folderPath, 
    IProgress<ScanProgress>? progress, 
    CancellationToken ct = default)
{
    var list = new List<JunkItem>();
    
    foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
    {
        // 1. Kiểm tra yêu cầu hủy từ người dùng
        ct.ThrowIfCancellationRequested();
        
        // 2. Thực hiện kiểm tra an toàn và tính toán
        if (SafePathGuard.IsSafeToDelete(file))
        {
            list.Add(new JunkItem(file));
        }
    }
    
    return OperationResult<List<JunkItem>>.Ok(list);
}
```

### 🔴 Quy Chuẩn Vòng Đời Điều Hướng (Navigation Lifecycle & Cleanup):
1. **Quản lý CancellationTokenSource:** Mọi ViewModel có tác vụ dài hạn phải duy trì `CancellationTokenSource? _cts`.
2. **Hủy khi rời trang (OnNavigatedFrom / Cleanup):** Khi người dùng chuyển trang hoặc nhấn nút Hủy, ViewModel/Page **bắt buộc** phải gọi `_cts?.Cancel()` và `_cts?.Dispose()`.
3. **Triệt tiêu Orphaned Tasks:** Tuyệt đối không để task nền tiếp tục chạy ngầm và gọi `RunOnUI` cập nhật dữ liệu sau khi View đã bị hủy hoặc điều hướng sang trang khác.

---

## 💾 3. Quy Chuẩn Khóa Đồng Bộ CSDL (`_dbLock` & SQLite WAL)

Mặc dù SQLite ở chế độ **Write-Ahead Logging (WAL)** hỗ trợ nhiều luồng đọc đồng thời, thao tác ghi (INSERT, UPDATE, DELETE) vẫn yêu cầu quyền ghi độc quyền để tránh lỗi `SQLite busy (database is locked)`.

### 🔴 QUY TẮC BẮT BUỘC TRONG `DbManager`:
1. **Khóa cục bộ toàn năng:** Mọi phương thức truy vấn hoặc thực thi lệnh CSDL phải nằm trong khối `lock (_dbLock)`.
2. **Tránh Deadlock:** Không gọi lồng nhau các hàm bên ngoài (như gọi sự kiện giao diện hoặc gọi HTTP request) trong khi đang nắm giữ `lock (_dbLock)`.

```csharp
private readonly object _dbLock = new();

public void InsertLog(string action, string module, string status)
{
    lock (_dbLock)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Logs (Action, Module, Status) VALUES ($act, $mod, $stat)";
        cmd.Parameters.AddWithValue("$act", action);
        cmd.Parameters.AddWithValue("$mod", module);
        cmd.Parameters.AddWithValue("$stat", status);
        cmd.ExecuteNonQuery();
    }
}
```

---

## 🚫 4. Cấm Sử Dụng Sync-Over-Async (`.Result` hoặc `.Wait()`)

- ❌ **CẤM:** Gọi `task.Result` hoặc `task.Wait()` trên UI Thread — đây là nguyên nhân số 1 gây Deadlock trong môi trường Windows UI.
- ✅ **LUÔN DÙNG:** Từ khóa `await` trong phương thức có chữ ký `async Task`.
- ❌ **CẤM `async void`:** Ngoại trừ các bộ xử lý sự kiện UI XAML (Event Handlers), toàn bộ các phương thức bất đồng bộ phải trả về `Task` hoặc `Task<T>`.

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

---
name: wincare-error-resilience
description: >-
  Zero-Silent Catch and error resilience patterns for WinCare Pro. Covers OperationResult/OperationResult<T> usage, CrashLogger exception tracking, preventing empty catches, graceful degradation for Win32/WMI failures, and user-facing error reporting via InfoBar/NotificationCenter. Use whenever handling exceptions, designing API results, or writing fault-tolerant system code.
---

# 🛡️ WinCare Pro — Quản Lý Lỗi & Chống Nuốt Ngoại Lệ (Error Resilience)

WinCare Pro can thiệp vào nhiều phân hệ nhạy cảm của Windows (Registry, File System, Win32 API, Services, WMI). Lỗi I/O, Access Denied hoặc Win32 Error là điều tất yếu. Hệ thống **tuyệt đối cấm** nuốt ngoại lệ (silent catch) và bắt buộc trả về kết quả qua mẫu `OperationResult`.

---

## 🚫 1. Bảng Đối Chiếu Xử Lý Ngoại Lệ Bắt Buộc

| Tình huống | ❌ CẤM TUYỆT ĐỐI | ✅ BẮT BUỘC SỬ DỤNG |
| :--- | :--- | :--- |
| **Bắt ngoại lệ** | `catch { }` hoặc `catch (Exception) { /* no-op */ }` | Luôn log qua `CrashLogger.LogError(ex)` và trả về `OperationResult.Fail(ex)` |
| **Kiểu trả về của Engine** | Trả về `void` hoặc ném thẳng Exception không kiểm soát | Trả về `OperationResult` hoặc `OperationResult<T>` |
| **Lỗi quyền hạn (UnauthorizedAccess)** | Để ứng dụng sập (Crash unhandled) | Bắt cụ thể `UnauthorizedAccessException`, thông báo cần quyền Administrator |
| **Lỗi P/Invoke hoặc Win32 API** | Không kiểm tra mã trả về của Win32 | Kiểm tra `Marshal.GetLastWin32Error()` hoặc mã `HRESULT` |
| **Hiển thị lỗi trên UI** | Bật `MessageBox.Show()` WinForms cũ kỹ | Dùng `InfoBar`, `ContentDialog` hoặc `NotificationCenter` theo chuẩn Fluent 2.0 |

---

## 📦 2. Mẫu Kết Quả Chuẩn Hóa (`OperationResult<T>`)

Mọi phương thức trong tầng `Engines` và `Infrastructure` phải bọc kết quả qua [OperationResult.cs](file:///d:/WinCare/Core/Models/OperationResult.cs):

### Phương thức không có giá trị trả về:
```csharp
public async Task<OperationResult> FlushDnsAsync(CancellationToken ct = default)
{
    try
    {
        var procResult = await ProcessRunner.RunAsync("ipconfig.exe", new[] { "/flushdns" }, ct);
        if (procResult.ExitCode == 0)
        {
            AppLogger.LogInfo("DNS cache flushed successfully.");
            return OperationResult.Ok();
        }

        return OperationResult.Fail($"ipconfig failed with exit code {procResult.ExitCode}: {procResult.StdErr}");
    }
    catch (OperationCanceledException)
    {
        return OperationResult.Fail("Operation was canceled by user.");
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, nameof(FlushDnsAsync));
        return OperationResult.Fail(ex);
    }
}
```

### Phương thức có dữ liệu trả về (`OperationResult<T>`):
```csharp
public OperationResult<DriveHealthInfo> GetDiskHealth(string driveLetter)
{
    try
    {
        if (string.IsNullOrWhiteSpace(driveLetter))
            return OperationResult<DriveHealthInfo>.Fail("Invalid drive letter.");

        var info = QuerySmartAttributes(driveLetter);
        return OperationResult<DriveHealthInfo>.Ok(info);
    }
    catch (Exception ex)
    {
        CrashLogger.LogError(ex, $"{nameof(GetDiskHealth)}: {driveLetter}");
        return OperationResult<DriveHealthInfo>.Fail(ex);
    }
}
```

---

## 📝 3. Phân Cấp Ghi Log (Logging Strategy)

Toàn hệ thống phân định rõ ràng giữa hai cơ chế log:

1. **`CrashLogger.LogError(Exception ex, string context)`**:
   - Sử dụng khi: Có Exception không mong muốn, ngoại lệ I/O, lỗi CSDL, lỗi Win32, hoặc crash ngầm.
   - Vị trí lưu: Thư mục `%AppData%\WinCare\Logs\crash_*.log` kèm stack trace chi tiết.

2. **`AppLogger.LogInfo` / `AppLogger.LogWarning`**:
   - Sử dụng khi: Ghi nhận sự kiện nghiệp vụ thông thường (bắt đầu quét, quét xong 150MB rác, người dùng hủy thao tác, phát hiện service bị bảo vệ).

---

## 🛡️ 4. Chiến Lược Giảm Cấp Mềm Dẻo (Graceful Degradation)

Khi một API hệ thống cấp thấp không thể thực thi (do Windows thiếu tính năng, tài khoản người dùng chuẩn không phải Administrator, hoặc WMI bị hỏng repository):

1. **Không làm chết toàn bộ quy trình:** Thử cơ chế fallback thay thế (ví dụ: WMI hỏng $\rightarrow$ đọc qua Registry hoặc Performance Counters).
2. **Cung cấp cảnh báo cụ thể cho người dùng:** 
   ```csharp
   if (!IsAdministrator())
   {
       RunOnUI(() =>
       {
           WarningMessage = TranslationManager.Instance.Get("Err_AdminRequiredForThisAction");
           ShowAdminShieldBadge = true;
       });
       return OperationResult.Fail("Administrator privileges required.");
   }
   ```
3. **Giữ ứng dụng tiếp tục hoạt động:** Các phân hệ khác không liên quan vẫn phải tương tác bình thường.

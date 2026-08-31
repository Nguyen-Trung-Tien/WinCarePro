# 💻 05. Quy Chuẩn Lập Trình C# 13 & .NET 10 (Coding Standards & Conventions)

> [⬅️ 04. Hiệu Năng & Quản Lý RAM](04_PERFORMANCE_AND_MEMORY_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 05** • [Quy Chuẩn Tiếp Theo: 06. Giao Diện & UI/UX ➡️](06_UI_UX_AND_AESTHETICS_RULES.md)

---

## 🏗️ 1. Mẫu Kết Quả Hoạt Động Bắt Buộc (`OperationResult<T>`)

Mọi phương thức trong tầng Engine và Service thực hiện các thao tác có khả năng thất bại (I/O, Registry, Win32 API, Network) **bắt buộc** trả về kiểu `OperationResult` hoặc `OperationResult<T>` thay vì ném ra ngoại lệ thô (Uncaught Exceptions).

```csharp
// Chuẩn cấu trúc kết quả:
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    public static OperationResult Ok(string message = "") => new() { Success = true, Message = message };
    public static OperationResult Fail(string error, Exception? ex = null, string code = "") => 
        new() { Success = false, Message = error, Exception = ex, ErrorCode = code };
}
```

---

## 🚫 2. Xử Lý Ngoại Lệ & Nhật Ký Sự Cố (Zero-Silent Catch Policy)

1. **Nghiêm cấm nuốt lỗi rỗng (No Empty Catch Blocks):**
   - ❌ **CẤM:** `catch { }` hoặc `catch (Exception) { /* do nothing */ }`.
   - Mọi ngoại lệ bắt được phải được ghi nhận vào `CrashLogger` hoặc `AuditLogService` hoặc trả về qua `OperationResult.Fail()`.
2. **Ghi log có cấu trúc:**
   - Sử dụng `AuditLogService.LogAction(action, module, status, details)` để lưu lại các hành động quan trọng của người dùng vào CSDL.

---

## 🌐 3. Quy Chuẩn Đa Ngôn Ngữ (i18n & No Hardcoded Strings)

1. **Tuyệt đối không gán cứng chuỗi hiển thị:**
   - Mọi thông điệp giao diện, tiêu đề trang, nhãn nút bấm, thông báo Toast **bắt buộc** phải sử dụng mã định danh dịch thuật (Translation Key).
2. **Cú pháp trong XAML & C#:**
   - Trong XAML: Sử dụng `x:Uid` hoặc `Text="{Binding Key, Converter={StaticResource TranslationConverter}}"`
   - Trong C#: Sử dụng `TranslationManager.Instance.GetString("Key")` hoặc extension method `"Key".Translate()`.
3. **Bổ sung đủ cặp khóa dịch:**
   - Mỗi Key mới phải được thêm đồng thời vào cả 2 từ điển `vi-VN` và `en-US` trong [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs).

---

## 🔤 4. Quy Ước Đặt Tên & Cấu Trúc Mã Nguồn (Naming Conventions)

| Đối tượng | Quy chuẩn | Ví dụ |
| :--- | :--- | :--- |
| **Class, Struct, Record** | PascalCase | `JunkCleanerEngine`, `SafePathGuard` |
| **Interface** | Tiền tố `I` + PascalCase | `IServiceSafetyService`, `IDbManager` |
| **Method (Hàm)** | PascalCase (Thêm hậu tố `Async` nếu là bất đồng bộ) | `ScanJunkAsync()`, `EmptyWorkingSet()` |
| **Private Field** | Tiền tố `_` + camelCase | `private readonly object _dbLock;` |
| **Property** | PascalCase | `public double CpuUsagePercentage { get; set; }` |
| **Constants** | PascalCase hoặc UPPER_SNAKE | `MaxHistoryEntries`, `DEFAULT_TIMEOUT_MS` |
| **File Name** | Khớp chính xác với tên Class chính trong file | `SystemOptimizerEngine.cs` |

---

## 🛡️ 5. An Toàn Kiểu Null (Nullable Reference Types)

Dự án kích hoạt chế độ `<Nullable>enable</Nullable>`.
- Luôn kiểm tra `null` đối với các biến đầu vào hoặc sử dụng cú pháp an toàn `?.` / `??`.
- Không sử dụng toán tử ép kiểu phủ định `!` (Null-forgiving operator) trừ khi có chứng minh toán học bất biến chắc chắn rằng biến không thể `null`.

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

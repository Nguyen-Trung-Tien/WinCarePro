---
name: wincare-db-wal
description: >-
  SQLite 3 WAL Mode persistence and synchronization rules for WinCare Pro. Covers DbManager usage, mandatory lock (_dbLock) synchronization, parameterized queries, schema migrations, avoiding lock contention, and preventing test collisions via DatabaseTestCollection. Use whenever accessing, querying, or modifying the SQLite database.
---

# 🗄️ WinCare Pro — Quy Chuẩn CSDL SQLite WAL & Đồng Thời An Toàn (Db WAL Guard)

WinCare Pro sử dụng **SQLite 3** ở chế độ **Write-Ahead Logging (WAL)** làm CSDL cục bộ lưu trữ cấu hình, lịch sử quét dọn, nhật ký can thiệp hệ thống và điểm khôi phục Undo. 

Do SQLite trong môi trường desktop là Single-Writer, mọi thao tác đọc/ghi **bắt buộc** phải tuân theo cơ chế đồng bộ hóa tập trung qua [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs).

---

## 🚫 1. Bảng Đối Chiếu Quy Tắc Thao Tác CSDL Bắt Buộc

| Tình huống | ❌ CẤM TUYỆT ĐỐI | ✅ BẮT BUỘC SỬ DỤNG |
| :--- | :--- | :--- |
| **Truy cập SQLite** | Mở connection riêng lẻ ngoài `DbManager` | Luôn gọi qua `DbManager.Instance` hoặc DI service |
| **Đồng bộ đa luồng** | Thực thi query mà không có lock | Bọc 100% trong `lock (_dbLock) { /* SQLite operations */ }` |
| **Xử lý bên trong lock** | Chạy logic nặng, I/O mạng, cập nhật UI trong lock | Chỉ thực hiện các câu lệnh SQLite thuần túy trong lock |
| **Truy vấn SQL** | Ghép chuỗi `$"SELECT * FROM Audit WHERE Path = '{p}'"` | Bắt buộc dùng Parameter: `cmd.Parameters.AddWithValue("$path", p)` |
| **Unit Test CSDL** | Tạo test class CSDL chạy song song tự do | Gắn attribute `[Collection(nameof(DatabaseTestCollection))]` |

---

## 🔒 2. Mô Thức Khóa Đồng Bộ Tập Trung (`lock (_dbLock)`)

Mọi phương thức trong `DbManager` phải tuân thủ nghiêm ngặt mô thức sau:

```csharp
public OperationResult SaveAuditRecord(AuditRecord record)
{
    if (record == null) return OperationResult.Fail("Audit record cannot be null.");

    // Chuẩn bị dữ liệu bên NGOÀI lock
    var timestamp = DateTime.UtcNow.ToString("o");

    lock (_dbLock)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AuditRecords (Module, Action, TargetPath, Details, Timestamp, Success)
                VALUES ($mod, $act, $target, $details, $ts, $success);";

            cmd.Parameters.AddWithValue("$mod", record.Module);
            cmd.Parameters.AddWithValue("$act", record.Action);
            cmd.Parameters.AddWithValue("$target", record.TargetPath ?? string.Empty);
            cmd.Parameters.AddWithValue("$details", record.Details ?? string.Empty);
            cmd.Parameters.AddWithValue("$ts", timestamp);
            cmd.Parameters.AddWithValue("$success", record.Success ? 1 : 0);

            cmd.ExecuteNonQuery();
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            CrashLogger.LogError(ex, $"{nameof(DbManager)}.{nameof(SaveAuditRecord)}");
            return OperationResult.Fail(ex);
        }
    }
}
```

---

## ⚡ 3. Chống Nghẽn Luồng (Zero-Blocking Inside Lock)

### ❌ Anti-Pattern gây treo ứng dụng:
```csharp
lock (_dbLock)
{
    // CẤM: Gọi I/O đĩa hoặc I/O mạng bên trong lock SQLite!
    var fileSize = new FileInfo(path).Length; 
    var hash = ComputeFileSha256(path); 
    
    // CẤM: Đẩy cập nhật giao diện trong khi đang giữ lock CSDL!
    RunOnUI(() => StatusText = "Đang lưu CSDL...");
    
    cmd.ExecuteNonQuery();
}
```

### ✅ Best Practice:
1. Tính toán, băm SHA256, kiểm tra file tồn tại **trước** khi vào `lock (_dbLock)`.
2. Vào lock, thực thi truy vấn SQLite nhanh gọn trong vài phần triệu giây.
3. Rời khỏi lock, sau đó mới cập nhật UI qua `RunOnUI()`.

---

## 🛡️ 4. Chống Tấn Công SQL Injection

Mọi tham số do người dùng nhập hoặc trích xuất từ hệ thống (đường dẫn, tên phần mềm, Registry key) **tuyệt đối không được nội suy chuỗi trực tiếp vào câu lệnh SQL**.

- Luôn dùng cú pháp tiền tố `$`, `@` hoặc `:` theo chuẩn `Microsoft.Data.Sqlite`.
- Sử dụng `cmd.Parameters.AddWithValue("$paramName", value ?? DBNull.Value)`.

---

## 🧪 5. Kiểm Thử CSDL An Toàn (DatabaseTestCollection)

Để ngăn ngừa lỗi `SQLite Error 5: 'database is locked'` khi xUnit chạy đa luồng:

```csharp
using Xunit;
using WinCarePro.Tests.Fixtures;

namespace WinCarePro.Tests.Database;

[Collection(nameof(DatabaseTestCollection))]
public class DbManagerTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DbManagerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SaveAndRetrieveAuditRecord_ShouldSucceed()
    {
        // Thực thi test CSDL trong môi trường tuần tự an toàn
    }
}
```

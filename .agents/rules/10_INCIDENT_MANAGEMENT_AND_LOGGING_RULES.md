# 📋 10. Quy Chuẩn Quản Lý Sự Cố, Ghi Log & Bảo Vệ Riêng Tư (Incident & Logging Rules)

> [⬅️ 09. Tương Tác Registry & Win32](09_REGISTRY_AND_OS_INTEROP_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 10** • [Về Đầu: 01. Kiến Trúc & Phân Tầng ➡️](01_ARCHITECTURE_AND_DESIGN_RULES.md)

---

## 📝 1. Chiến Lược Ghi Log Phân Cấp (Structured Logging Strategy)

Hệ thống duy trì 2 kênh ghi nhật ký độc lập theo mục đích sử dụng:

| Kênh Log | Vị trí lưu | Mục đích | Cơ chế bảo vệ |
| :--- | :--- | :--- | :--- |
| **Audit Log (Kiểm toán)** | CSDL SQLite bảng `Logs` | Ghi nhận các hành động dọn rác, tối ưu, gỡ app của người dùng | Truy vấn qua `AuditLogService`, hỗ trợ tìm kiếm theo Module |
| **Crash Logger (Sự cố)** | `%AppData%\WinCarePro\CrashLogs\` | Ghi nhận các ngoại lệ未xử lý (`UnhandledException`), StackTrace | Tự động tạo tệp theo ngày giờ, nén và giới hạn kích thước |

---

## 🔒 2. Quy Tắc Bảo Vệ Dữ Liệu Cá Nhân Trong Log (Zero-PII Leakage)

1. **Tuyệt đối không ghi thông tin nhạy cảm vào Log:**
   - ❌ **CẤM:** Ghi mật khẩu, token, khóa giải mã hoặc nội dung tệp tin cá nhân của người dùng vào tệp nhật ký.
2. **Ẩn danh đường dẫn người dùng (Path Sanitization):**
   - Khi ghi đường dẫn tệp vào log, các đường dẫn nhạy cảm có thể chứa tên tài khoản Windows (ví dụ: `C:\Users\JohnDoe\AppData\...`) phải được chuẩn hóa về định dạng biến môi trường `%USERPROFILE%\AppData\...` hoặc `%TEMP%` để tránh lộ danh tính khi người dùng gửi file log nhờ hỗ trợ kỹ thuật.

---

## 🗄️ 3. Cơ Chế Xoay Vòng & Giới Hạn Kích Thước Log (Log Rotation & Retention)

Để tránh việc tệp log phình to vô hạn chiếm dụng ổ cứng của người dùng:

1. **Giới hạn kích thước tệp log:**
   - Mỗi tệp log tối đa **5 MB**. Khi vượt quá kích thước này, tự động đóng tệp và tạo tệp mới với hậu tố số thứ tự.
2. **Chính sách lưu trữ (Retention Policy):**
   - Thư mục `CrashLogs/` chỉ giữ tối đa **30 ngày** hoặc tối đa **10 tệp log gần nhất**.
   - Khi ứng dụng khởi động, `CrashLogger.CleanupOldLogs()` sẽ tự động dọn dẹp các tệp log cũ hơn 30 ngày.
3. **Giới hạn bảng `Logs` trong SQLite:**
   - Bảng `Logs` trong CSDL được tự động cắt tỉa giữ lại tối đa **1,000 bản ghi** hoạt động gần nhất để đảm bảo tốc độ truy vấn luôn đạt dưới $10\text{ ms}$.

---

## 🚨 4. Quy Chuẩn Xử Lý Sự Cố Crash Ứng Dụng (Global Exception Hooks)

Tại [App.xaml.cs](file:///d:/WinCare/App.xaml.cs), hệ thống **bắt buộc** phải đăng ký đầy đủ 3 bộ móc ngoại lệ toàn cục:

```csharp
// 1. Ngoại lệ luồng chính & miền ứng dụng
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    CrashLogger.LogFatal("AppDomain Unhandled Exception", e.ExceptionObject as Exception);
};

// 2. Ngoại lệ Task chạy nền không được quan sát
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    CrashLogger.LogError("TaskScheduler Unobserved Task Exception", e.Exception);
    e.SetObserved(); // Ngăn chặn crash tiến trình nếu lỗi không nghiêm trọng
};

// 3. Ngoại lệ trên UI Thread XAML
this.UnhandledException += (s, e) =>
{
    CrashLogger.LogFatal("XAML UI Unhandled Exception", e.Exception);
    e.Handled = true; // Cố gắng giữ ứng dụng tiếp tục hoạt động nếu có thể
};
```

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

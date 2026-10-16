---
name: wincare-test-verifier
description: >-
  Token-efficient unit testing and regression verification workflow for WinCare Pro. Covers running targeted tests, handling SQLite DatabaseTestCollection fixtures, interpreting test failures, and ensuring 100% test passing before completion. Use when running tests, diagnosing test failures, or validating changes in WinCarePro.Tests.
---

# 🧪 WinCare Pro — Quy Trình Kiểm Thử & Xác Thực Hồi Quy (Test Verifier)

WinCare Pro hiện có **465 bài kiểm thử xUnit** bảo vệ toàn diện hệ thống. Tài liệu này hướng dẫn cách chạy test chuẩn, tiết kiệm token tối đa và xử lý đồng thời SQLite an toàn.

---

## ⚡ 1. Lệnh Kiểm Thử Mục Tiêu Tiết Kiệm Token (Targeted Execution)

### ❌ Anti-Pattern Cần Tránh:
- Chạy `dotnet test` toàn bộ 465 test lặp đi lặp lại sau mỗi chỉnh sửa nhỏ. Điều này in ra hàng trăm dòng log vô ích, đốt từ 5.000 đến 15.000 token output không cần thiết.

### ✅ Best Practice Chuẩn AI:
1. **Chỉ chạy test cho module đang làm việc:**
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TênLớpTest" --logger "console;verbosity=minimal"
   ```
   *Ví dụ:*
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~JunkCleanerEngineTests" --logger "console;verbosity=minimal"
   ```
2. **Kiểm tra biên dịch cú pháp nhanh trước khi chạy test:**
   ```powershell
   dotnet build WinCarePro.csproj -c Debug -v q --nologo
   ```
3. **Chỉ chạy toàn bộ test suite khi hoàn tất công việc (Final Smoke Test):**
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj -c Debug --logger "console;verbosity=minimal"
   ```

---

## 🗄️ 2. Quy Chuẩn Đồng Thời SQLite Trong Test (`DatabaseTestCollection`)

Do SQLite hoạt động ở chế độ Single-Writer WAL với `lock (_dbLock)`, nếu xUnit chạy song song nhiều test class cùng thao tác CSDL cục bộ sẽ phát sinh lỗi `database is locked` hoặc va chạm dữ liệu.

### Quy Tắc Bắt Buộc:
Mọi test class có gọi đến `DbManager` hoặc các Engine ghi nhận Audit/History **bắt buộc** phải gắn attribute:
```csharp
[Collection(nameof(DatabaseTestCollection))]
public class MyFeatureDbTests : IClassFixture<MyFixture>
{
    // ...
}
```
Attribute này chỉ thị cho xUnit xếp các test class vào cùng một nhóm tuần tự, loại bỏ hoàn toàn hiện tượng race condition khi test.

---

## 🔍 3. Quy Trình Chẩn Đoán Khi Test Thất Bại (Failure RCA)

1. **Đọc đúng thông điệp lỗi:**
   - Xem Assert mong đợi giá trị gì và thực tế nhận giá trị gì.
   - Kiểm tra stack trace dòng phát sinh ngoại lệ.
2. **Không tự suy đoán hoặc sửa mò:**
   - Dùng `grep_search` kiểm tra phương thức thực tế và giá trị hằng số liên quan.
   - Dùng `view_file` xem lát cắt hẹp xung quanh dòng fail (30-50 dòng).
3. **Áp dụng bản vá tối thiểu:**
   - Dùng `replace_file_content` sửa nguyên nhân gốc rễ.
   - Chạy lại đúng bài test đơn lẻ đó:
     ```powershell
     dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TênClass.TênHàmTest" --logger "console;verbosity=minimal"
     ```
4. **Cam kết bàn giao:**
   - 100% Passed (465/465 Tests), 0 Failed, 0 Skipped.

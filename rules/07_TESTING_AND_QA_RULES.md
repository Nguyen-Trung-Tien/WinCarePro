# 🧪 07. Quy Chuẩn Kiểm Thử Tự Động & Đảm Bảo Chất Lượng (QA & Testing Rules)

> [⬅️ 06. Giao Diện & UI/UX](06_UI_UX_AND_AESTHETICS_RULES.md) • [🏠 Mục Lục Rules](README.md) • **Rule 07** • [Quy Chuẩn Tiếp Theo: 08. Đóng Gói & CI/CD ➡️](08_RELEASE_PACKAGING_AND_CICD_RULES.md)

---

## 🏆 1. Chính Sách Không Lỗi Tuyệt Đối (Zero-Bug Policy)

Dự án duy trì tỷ lệ kiểm thử thành công **100% Passed (230/230 Tests)**. Không một bản build nào được phép xuất xưởng nếu có bất kỳ bài test nào bị `Failed`.

```powershell
# Lệnh kiểm thử bắt buộc trước khi merge code:
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --verbosity normal
```

---

## 🛡️ 2. Quy Tắc Cô Lập Kiểm Thử (Test Isolation & Safety)

1. **Không can thiệp vào máy tính thật của Developer:**
   - Các bài kiểm thử CSDL **bắt buộc** sử dụng cơ sở dữ liệu tạm thời trong RAM (`Data Source=:memory:`) hoặc file SQLite độc lập trong thư mục `TestTemp/`, tự động dọn sạch sau khi test kết thúc.
   - Các thao tác Win32 nguy hiểm (như dừng tiến trình thật, can thiệp Registry `HKLM`, chạy `sfc /scannow`) phải được bọc qua Mock Object (`Moq`) hoặc kiểm tra trên tệp giả lập.
2. **Kiểm thử độc lập đa luồng:**
   - Các bài test không được phụ thuộc vào thứ tự chạy (Order-independent). Không dùng biến tĩnh toàn cục có thể gây xung đột trạng thái giữa các bài test chạy song song.

---

## 🎯 3. Yêu Cầu Phạm Vi Kiểm Thử Cho Tính Năng Mới (Coverage Requirements)

Khi lập trình viên thêm một Engine hoặc tính năng mới, **bắt buộc** phải viết kèm bộ test xUnit trong `WinCarePro.Tests`:

| Phân loại tính năng | Yêu cầu bài test tối thiểu |
| :--- | :--- |
| **Thuật toán quét & dọn dẹp** | Test nhận diện tệp, test bỏ qua file đang khóa, test kiểm tra an toàn `SafePathGuard`. |
| **Chẩn đoán & Chấm điểm** | Test các trường hợp biên: 0% RAM, 100% CPU, ổ C: đầy, danh sách rỗng. |
| **Bảo mật & CSDL** | Test chống SQL Injection, test di chuyển Schema Version, test mã hóa DPAPI. |
| **Dịch thuật đa ngôn ngữ** | Test kiểm tra không thiếu bất kỳ Key dịch nào giữa `vi-VN` và `en-US`. |

---

## 🔍 4. Cấu Trúc Bài Test Chuẩn Mực (Arrange - Act - Assert)

Mỗi phương thức kiểm thử phải tuân theo cấu trúc AAA rõ ràng và sử dụng `FluentAssertions`:

```csharp
[Fact]
public void SafePathGuard_ShouldBlockDeletion_WhenTargetIsSystem32()
{
    // 1. Arrange (Chuẩn bị dữ liệu)
    string dangerousPath = @"C:\Windows\System32\drivers";

    // 2. Act (Thực thi hành động)
    bool isSafe = SafePathGuard.IsSafeToDelete(dangerousPath);

    // 3. Assert (Xác nhận kết quả với FluentAssertions)
    isSafe.Should().BeFalse("Thư mục System32 là thành phần sống còn của hệ điều hành, không được phép xóa.");
}
```

---

<div align="center">
  <sub>[🏠 Mục Lục Rules](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>

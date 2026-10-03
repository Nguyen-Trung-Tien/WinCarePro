# 🧠 WinCare Pro — Quy Chuẩn 11: Tối Ưu Token & Kỹ Thuật Lập Trình Chính Xác Cho AI Agent
> **Tập tin quy chuẩn:** `rules/11_AI_AGENT_EFFICIENCY_AND_PRECISION_RULES.md`  
> **Phạm vi áp dụng:** Toàn bộ AI Agents (Antigravity/Gemini), Trợ lý lập trình tự động & Nhà phát triển  
> **Mục tiêu cốt lõi:** Tối thiểu hóa lượng token tiêu thụ (Context & Output Token Economy), loại trừ ảo giác (Zero-Hallucination), tuân thủ 100% kiến trúc và bảo đảm tính chính xác tuyệt đối cho hệ thống WinCare Pro Suite.

---

## 🏛️ 1. Nguyên Tắc Cốt Lõi (Core Directive)

Khi phát triển hoặc bảo trì mã nguồn trong WinCare Pro, AI Agent **bắt buộc** phải tuân theo nguyên tắc:

$$\mathbf{Precision\ Engineering} + \mathbf{Token\ Economy} \Longrightarrow \mathbf{Zero\ Downtime} + \mathbf{Optimal\ Cost}$$

1. **Không phung phí Context Window:** Đọc chính xác phần mã cần can thiệp; không bao giờ nạp toàn bộ file hàng nghìn dòng vào ngữ cảnh mà không có lý do.
2. **Không ghi đè mù quáng (Zero-Overwrite):** Chỉ sửa đúng vị trí cần thay đổi bằng diff/patch, không xuất lại toàn bộ file nguồn.
3. **Tuân thủ thiết kế bản địa (Zero-Reinventing):** Luôn tận dụng triệt để các Guard, Helper và Service có sẵn của WinCare Pro thay vì tự viết lại logic ngoại vi.
4. **Vòng lặp tự kiểm chứng (Self-Correction Gate):** Mọi thay đổi mã nguồn phải được chứng minh tính chính xác thông qua biên dịch 0 cảnh báo và vượt qua 100% bài kiểm thử hồi quy.

---

## 📉 2. Chiến Lược Tiết Kiệm Token (Token Economy Protocols)

```mermaid
graph TD
    classDef step fill:#0f172a,stroke:#3b82f6,stroke-width:2px,color:#fff;
    classDef bad fill:#450a0a,stroke:#ef4444,stroke-width:2px,color:#fff;
    classDef good fill:#064e3b,stroke:#10b981,stroke-width:2px,color:#fff;

    Start["Yêu cầu can thiệp mã nguồn"]:::step
    Search["Bước 1: grep_search định vị biểu tượng / dòng lỗi"]:::step
    Start --> Search

    CheckFile{"File > 150 dòng?"}:::step
    Search --> CheckFile

    CheckFile -- Có --> Slice["✅ Dùng view_file với StartLine/EndLine cụ thể (30-80 dòng)"]:::good
    CheckFile -- Không --> ViewAll["Xem toàn bộ file nhỏ"]:::good

    Edit["Bước 2: Chỉnh sửa mã nguồn"]:::step
    Slice --> Edit
    ViewAll --> Edit

    BadEdit["❌ CẤM: write_to_file (Overwrite = true) toàn file"]:::bad
    GoodEdit["✅ BẮT BUỘC: replace_file_content / multi_replace_file_content"]:::good
    Edit -.-> BadEdit
    Edit --> GoodEdit

    Verify["Bước 3: Xác thực"]:::step
    GoodEdit --> Verify

    BadTest["❌ CẤM: Chạy full test suite cho mỗi chỉnh sửa nhỏ"]:::bad
    GoodTest["✅ BẮT BUỘC: dotnet test --filter \"TênTest\" cho module liên quan"]:::good
    Verify -.-> BadTest
    Verify --> GoodTest
```

### 2.1 Định Vị Chính Xác Thay Vì Đọc Tràn Lan (Targeted Slicing)
- **CẤM:** Gọi `view_file` xem toàn bộ file kích thước lớn (như [App.xaml](file:///d:/WinCare/App.xaml), [MainWindow.xaml.cs](file:///d:/WinCare/MainWindow.xaml.cs), [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs),...) gây lãng phí từ vài nghìn đến hàng chục nghìn token đầu vào (prompt tokens).
- **BẮT BUỘC:** 
  1. Sử dụng `grep_search` để tìm tên hàm, biến, hoặc chuỗi thông báo lỗi.
  2. Dùng `view_file` với phạm vi cắt lát hẹp (`StartLine` và `EndLine`), chỉ đọc từ 30 đến tối đa 80 dòng xung quanh khu vực cần can thiệp.
  3. Không đọc lại các file đã có trong ngữ cảnh đối thoại gần nhất trừ khi file vừa được chỉnh sửa.

### 2.2 Vá Cục Bộ Thay Vì Ghi Đè Toàn File (In-Place Patching)
- **CẤM:** Dùng `write_to_file` với `Overwrite = true` đối với các file mã nguồn hiện có trong dự án. Việc ghi đè toàn bộ tệp sẽ:
  - Tiêu tốn lượng lớn output tokens vô ích.
  - Dễ gây mất các hàm phụ trợ, mất câu lệnh `using`, hoặc làm biến dạng cấu trúc thụt lề (indentation).
  - Gây xung đột và diff khổng lồ trong hệ thống quản lý phiên bản Git.
- **BẮT BUỘC:**
  - Dùng `replace_file_content` cho các khối chỉnh sửa liền mạch (single contiguous block).
  - Dùng `multi_replace_file_content` khi cần sửa nhiều khối độc lập trong cùng một file.

### 2.3 Thực Thi Terminal Có Bộ Lọc & Hạn Chế Phun Log Dài (Focused Execution)
- **CẤM:** Chạy toàn bộ 465 bài kiểm thử (`dotnet test`) lặp đi lặp lại sau mỗi chỉnh sửa nhỏ.
- **BẮT BUỘC:**
  - Khi đang phát triển hoặc gỡ lỗi một phương thức/lớp, chỉ chạy bộ test có mục tiêu bằng cờ `--filter`:
    ```powershell
    # Kiểm thử riêng phân hệ an toàn file
    dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~SafePathGuardTests"

    # Kiểm thử riêng phân hệ an toàn Registry
    dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~SafeRegistryGuardTests"
    ```
  - Chỉ chạy toàn bộ bộ kiểm thử (`dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj`) một lần duy nhất tại bước kiểm duyệt chất lượng trước khi bàn giao.
  - Giới hạn dòng log khi dùng lệnh shell (ví dụ: `git log -n 5`, `git status -s`).

### 2.4 Phong Cách Phản Hồi Súc Tích (Concise Communication)
- Không in lại toàn bộ code đã sửa trong lời giải thích nếu diff đã thể hiện rõ.
- Tập trung vào: **Mục tiêu thay đổi**, **Vị trí can thiệp (Link tới mã nguồn)**, và **Kết quả xác thực kiểm thử**.

---

## 🎯 3. Bộ Quy Tắc Code Chức Năng Chính Xác Cho WinCare Pro

Để bảo đảm mọi chức năng được sinh ra hoạt động chính xác 100%, không phát sinh lỗi tiềm ẩn hoặc xung đột hệ điều hành, AI Agent phải tuân thủ nghiêm ngặt các quy tắc kỹ thuật sau:

### 3.1 Tuân Thủ Tuyệt Đối Ranh Giới 4 Tầng
```
Presentation (Views/XAML) ──> ViewModels ──> Engines ──> Infrastructure/Core
```
- **Không xâm lấn WinUI vào Tầng Thấp:** Tuyệt đối cấm `using Microsoft.UI.Xaml;`, `Button`, `SolidColorBrush`, `Window` trong các thư mục `Engines/`, `Core/`, `Infrastructure/`.
- **ViewModel Phải Độc Lập:** Kế thừa từ [ViewModelBase.cs](file:///d:/WinCare/Core/Helpers/ViewModelBase.cs), giao tiếp qua `WeakReferenceMessenger`, không giữ con trỏ trực tiếp tới Page hay Window.

### 3.2 Tái Sử Dụng Khiên Bảo Vệ Hệ Thống (Mandatory Reuse of Core Guards)
Trước khi viết bất kỳ thao tác nhạy cảm nào, Agent bắt buộc phải kiểm tra và sử dụng các thư viện bảo vệ đã được thiết kế sẵn:

| Tác vụ nhạy cảm | Lớp bảo vệ bắt buộc | Mã nguồn mẫu chuẩn xác |
| :--- | :--- | :--- |
| **Gọi lệnh Shell / Process** | [ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs) | `await ProcessRunner.RunAsync("dism.exe", new[] { "/Online", "/Cleanup-Image", "/ScanHealth" }, ct);` |
| **Lọc tham số CLI** | [InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs) | `var cleanArg = InputSanitizer.Sanitize(rawInput);` |
| **Xóa File / Thư mục** | [SafePathGuard.cs](file:///d:/WinCare/Core/Helpers/SafePathGuard.cs) | `if (SafePathGuard.IsSafeToDelete(targetPath)) { File.Delete(targetPath); }` |
| **Sửa / Xóa Registry** | [SafeRegistryGuard.cs](file:///d:/WinCare/Core/Helpers/SafeRegistryGuard.cs) & `RegistryBackupEngine` | `if (SafeRegistryGuard.IsSafeToDeleteKey(regKey)) { await RegistryBackupEngine.BackupKeyAsync(regKey); ... }` |
| **Thao Tác Windows Service** | `ServiceSafetyService` | `if (!ServiceSafetyService.IsProtectedService(svcName)) { /* Thao tác dừng/tắt */ }` |
| **Mã Hóa Dữ Liệu Nhạy Cảm** | [CryptoHelper.cs](file:///d:/WinCare/Core/Helpers/CryptoHelper.cs) | `var encrypted = CryptoHelper.Protect(plainText);` |
| **Cập Nhật Giao Diện UI** | `ViewModelBase.RunOnUI()` | `RunOnUI(() => { Items.Add(newItem); StatusText = "Xong"; });` |
| **Truy Cập Cơ Sở Dữ Liệu SQLite** | [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs) | `lock (_dbLock) { /* Thực hiện truy vấn SQLite WAL */ }` |

### 3.3 Chuẩn Mực Bắt Ngoại Lệ & Trạng Thái Bận (Zero-Silent Catch & State Hygiene)
1. **Cấm nuốt lỗi thầm lặng:**
   ```csharp
   // ❌ CẤM TUYỆT ĐỐI
   catch { }

   // ✅ CHUẨN MỰC
   catch (Exception ex)
   {
       CrashLogger.LogError(ex);
       return OperationResult<bool>.Fail($"Không thể hoàn tất thao tác: {ex.Message}");
   }
   ```
2. **Luôn giải phóng trạng thái trong `finally`:**
   ```csharp
   try
   {
       IsBusy = true;
       OperationState = OperationState.Running;
       // Logic nghiệp vụ...
   }
   finally
   {
       IsBusy = false;
       OperationState = OperationState.Idle;
   }
   ```

### 3.4 Đồng Bộ Đa Ngôn Ngữ i18n Chuẩn Xác
- **Cấm Hardcode Chuỗi Text Trên UI:** Mọi chuỗi hiển thị phải đi qua `TranslationConverter` hoặc `TranslationManager.Instance.GetString(key)`.
- Khi bổ sung tính năng mới có chuỗi text mới, **bắt buộc** thêm đồng thời cả 2 từ điển `vi-VN` và `en-US` vào [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs). Không được phép bỏ sót một trong hai ngôn ngữ.

---

## ⚡ 4. Quy Trình Vận Hành Tiêu Chuẩn Cho Agent (AI SOP)

Khi nhận một tác vụ từ người dùng:

```powershell
# Bước 1: Định vị nhanh qua ripgrep (tiết kiệm token)
grep_search(Query: "TargetMethod", SearchPath: "d:\WinCare")

# Bước 2: Đọc lát cắt mã nguồn chính xác
view_file(AbsolutePath: "...", StartLine: 120, EndLine: 180)

# Bước 3: Áp dụng thay đổi dạng diff (tiết kiệm output token)
replace_file_content(...)

# Bước 4: Kiểm tra cục bộ qua unit test theo module
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TenModule"

# Bước 5: Kiểm tra nghiệm thu chất lượng (Gate)
dotnet build WinCarePro.csproj -c Debug                         # Phải đạt 0 Errors, 0 Warnings
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj            # Phải đạt 100% Passed (465/465 Tests)
```

---

## 🚫 5. Bảng Nhận Diện Anti-Patterns Thường Gặp Của AI

| Hành vi của AI | ❌ Tác hại | ✅ Giải pháp chuẩn hóa |
| :--- | :--- | :--- |
| **Đọc toàn bộ file 500-1000 dòng** | Tràn context, tốn input tokens, giảm độ tập trung của AI. | Dùng `grep_search` tìm biểu tượng $\rightarrow$ `view_file` lát cắt 40-70 dòng. |
| **Viết lại toàn bộ file bằng `write_to_file`** | Tốn output tokens, dễ sót hàm cũ, phá hỏng định dạng file. | Luôn dùng `replace_file_content` hoặc `multi_replace_file_content`. |
| **Tự sinh câu lệnh `Process.Start("cmd.exe", ...)`** | Nguy cơ Command Injection nghiêm trọng, vi phạm Rule 02. | Dùng `ProcessRunner.RunAsync("...", args, ct)`. |
| **Hardcode trực tiếp string Tiếng Việt vào XAML** | Phá vỡ tính năng chuyển đổi ngôn ngữ Anh - Việt. | Khai báo key trong `TranslationManager.Translations.cs` và bind qua `TranslationConverter`. |
| **Quên khối `finally` cho cờ `IsBusy`** | UI bị treo vĩnh viễn ở trạng thái bận nếu có ngoại lệ phát sinh. | Luôn đặt `IsBusy = false` trong khối `finally`. |
| **Chạy `dotnet test` toàn bộ suite cho mỗi lỗi nhỏ** | Tốn thời gian chờ, tạo output terminal quá dài làm tràn ngữ cảnh. | Dùng `--filter` khoanh vùng test của class đang sửa. |

---

<div align="center">
  <sub>Quy chuẩn 11 được ban hành nhằm nâng cao năng suất kỹ thuật số của AI Agent trong hệ sinh thái <b>WinCare Pro Suite</b></sub>
</div>

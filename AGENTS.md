# 🤖 WinCare Pro Suite — AI Agent Operational Directive
> **Phiên bản:** v4.9.2 (Codename: Orion) | **Nền tảng:** Windows 10/11 x64  
> **Công nghệ lõi:** .NET 10.0 • C# 13 • Windows App SDK (WinUI 3) • SQLite 3 WAL • CommunityToolkit.Mvvm  
> **Tài liệu tham chiếu chuyên sâu:** [10 Bộ Quy Chuẩn Kỹ Thuật](file:///d:/WinCare/rules/README.md) • [11 Chương Tài Liệu Kỹ Thuật](file:///d:/WinCare/docs/README.md)

---

## 🧭 1. Thứ Bậc Ưu Tiên Tuyệt Đối
$$\mathbf{Safety} > \mathbf{Correctness} > \mathbf{Security} > \mathbf{Stability} > \mathbf{Performance} > \mathbf{Maintainability} > \mathbf{UX} > \mathbf{Aesthetics}$$

1. **Safety:** Tuyệt đối không làm hỏng OS, không xóa nhầm dữ liệu người dùng, không tắt service sống còn.
2. **Correctness:** Dữ liệu dung lượng, telemetry và chẩn đoán phải chuẩn xác, không mock giả mạo.
3. **Security:** Zero-Trust. Không ghép chuỗi CLI, cô lập đối số qua `ArgumentList`, mã hóa nhạy cảm bằng DPAPI.
4. **Stability:** Zero-Crash. Luôn dùng `RunOnUI`, xử lý triệt để I/O & Win32, quản lý `CancellationToken`.
5. **Performance:** RAM nền $\le 15\text{ MB}$, Foreground $\le 85\text{ MB}$, Cold Start $\le 350\text{ ms}$, CPU Idle $\le 0.3\%$.
6. **Maintainability & UX:** Phân tầng MVVM nghiêm ngặt, chuẩn C# 13, giao diện Aura Glass (Fluent 2.0).

---

## 🏛️ 2. Kiến Trúc 4 Tầng (4-Tier Modular Architecture)
Chi nhánh phụ thuộc một chiều từ ngoài vào trong:  
$$\text{Presentation (Views/XAML)} \longrightarrow \text{ViewModel} \longrightarrow \text{Engines (Business Logic)} \longrightarrow \text{Infrastructure / Core}$$

- **Presentation:** Chỉ tương tác với ViewModel qua Data Binding & RelayCommands. Tuyệt đối không gọi thẳng Engine/Infra.
- **ViewModel:** Kế thừa [ViewModelBase.cs](file:///d:/WinCare/Core/Helpers/ViewModelBase.cs), bọc mọi cập nhật UI trong `RunOnUI()`. Giao tiếp chéo qua `WeakReferenceMessenger`, không giữ tham chiếu ViewModel khác.
- **Engines:** Xử lý nghiệp vụ độc lập. **CẤM** chứa bất kỳ WinUI type nào (`Button`, `Window`, `SolidColorBrush`,...).
- **Infrastructure / Core:** Lưu trữ SQLite [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs), bảo vệ file/reg ([SafePathGuard.cs](file:///d:/WinCare/Core/Helpers/SafePathGuard.cs), [SafeRegistryGuard.cs](file:///d:/WinCare/Core/Helpers/SafeRegistryGuard.cs)), P/Invoke, mã hóa DPAPI.

---

## 📋 3. Mười Quy Chuẩn Kỹ Thuật Bắt Buộc (Inviolable Rules)

1. **Kiến Trúc & DI:** View $\rightarrow$ ViewModel $\rightarrow$ Engine $\rightarrow$ Core. Đăng ký Singleton (`DbManager`, `ThemeManager`, `TranslationManager`) hoặc Transient (`*Engine`, `*ViewModel`) tại [App.xaml.cs](file:///d:/WinCare/App.xaml.cs).
2. **Khiên Bảo Vệ File & Registry:**
   - Xóa file/thư mục: Bắt buộc gọi `SafePathGuard.IsSafeToDelete(path)`. Cấm xóa thư mục gốc, hệ thống, tệp người dùng, Reparse Points.
   - Sửa/Xóa Registry: Bắt buộc gọi `SafeRegistryGuard.IsSafeToDeleteKey(key)` và sao lưu trước qua `RegistryBackupEngine.BackupKey()`. Cấm xóa Root Hives, độ sâu $\ge 3$.
3. **An Toàn CLI (Chống Injection):** Cấm ghép chuỗi `cmd.exe /c "..."`. Dùng [ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs) và đưa tham số vào `ArgumentList`. Lọc input qua [InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs).
4. **Bảo Vệ Windows Services & DPAPI:** Cấm tắt các dịch vụ lõi (`RpcSs`, `DcomLaunch`, `WinDefend`, `CryptSvc`,...). Dùng `ServiceSafetyService.IsProtectedService()`. Mã hóa dữ liệu nhạy cảm bằng [CryptoHelper.cs](file:///d:/WinCare/Core/Helpers/CryptoHelper.cs) (DPAPI).
5. **Đa Luồng & WinUI 3 Threading:** Mọi cập nhật UI / `ObservableCollection` phải bọc trong `RunOnUI(() => { ... })`. Cấm Sync-over-Async (`.Result`, `.Wait()`). Cấm `async void` (trừ UI Event Handlers).
6. **Quản Lý Tác Vụ & Hủy Bỏ:** Tác vụ async nặng phải nhận `CancellationToken ct = default` và kiểm tra `ThrowIfCancellationRequested()`. Luôn có khối `finally { IsBusy = false; OperationState = OperationState.Idle; }`.
7. **Đồng Thời SQLite (WAL Mode):** Mọi thao tác đọc/ghi SQLite trong [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs) bắt buộc bọc trong `lock (_dbLock)`. Không gọi logic ngoại vi trong lock.
8. **Mẫu Kết Quả & Zero-Silent Catch:** Mọi hàm có nguy cơ ngoại lệ phải trả về `OperationResult` hoặc `OperationResult<T>`. Tuyệt đối cấm `catch { }` nuốt lỗi; bắt buộc ghi log vào [CrashLogger.cs](file:///d:/WinCare/Infrastructure/Logging/CrashLogger.cs).
9. **Đa Ngôn Ngữ Bắt Buộc (i18n):** Cấm hardcode chuỗi text trên UI. Mọi text phải khai báo đầy đủ cả 2 từ điển `vi-VN` và `en-US` trong [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs).
10. **Tối Ưu Bộ Nhớ & Tài Nguyên:** Gỡ bỏ event (`-=`) khi `Unloaded`/`Dispose()`. Dừng animation 3D khi chuyển trang. Cache icon qua `IconCacheService`. Dùng `Typography.NumeralAlignment="Tabular"` cho telemetry.

---

## 🛑 4. Bảng Đối Chiếu Anti-Patterns Thường Gặp

| Tình huống | ❌ CẤM TUYỆT ĐỐI | ✅ BẮT BUỘC SỬ DỤNG |
| :--- | :--- | :--- |
| **Gọi lệnh Shell** | `Process.Start("cmd.exe", "/c " + arg)` | `ProcessRunner.RunAsync("app.exe", new[] { arg })` |
| **Xóa Tệp Tin** | `File.Delete(path)` | `if (SafePathGuard.IsSafeToDelete(path)) File.Delete(path);` |
| **Xóa/Sửa Registry** | `regKey.DeleteSubKeyTree(name)` | `if (SafeRegistryGuard.IsSafeToDeleteKey(k)) ...` (có Backup) |
| **Cập nhật UI từ Thread** | `Items.Add(item);` | `RunOnUI(() => Items.Add(item));` |
| **Xử lý Ngoại lệ** | `catch { }` | `catch (Exception ex) { CrashLogger.LogError(ex); return OperationResult.Fail(ex); }` |
| **Chờ Tác Vụ Async** | `var r = task.Result;` | `var r = await task;` |
| **Chuỗi Hiển Thị UI** | `<TextBlock Text="Quét rác"/>` | `<TextBlock Text="{Binding Key, Converter={StaticResource TranslationConverter}}"/>` |
| **Dọn Trạng Thái Busy** | Bỏ qua hoặc quên reset khi throw | `try { IsBusy = true; ... } finally { IsBusy = false; }` |
| **Truy Cập SQLite** | Mở query trực tiếp đa luồng | `lock (_dbLock) { /* query */ }` |

---

## 🛠️ 5. Quy Trình Triển Khai (SOPs Tinh Gọn)

### A. Thêm Phân Hệ / Tính Năng Mới
1. **Model:** Khai báo DTO tại `Core/Models/`, sử dụng `OperationResult<T>`.
2. **Engine:** Tạo tại `Engines/<Category>/`, bọc Guard an toàn, nhận `CancellationToken`.
3. **DI:** Đăng ký Engine & ViewModel trong `App.xaml.cs`.
4. **ViewModel:** Tạo tại `Modules/<Feature>/`, kế thừa `ViewModelBase`, bọc UI bằng `RunOnUI()`, xử lý `finally`.
5. **View:** Tạo XAML với Aura Glass, bo góc, Tabular figures, liên kết qua TranslationConverter.
6. **i18n:** Thêm cặp key vào cả `vi-VN` và `en-US` trong `TranslationManager.Translations.cs`.
7. **Test & Verify:** Viết unit test xUnit trong `WinCarePro.Tests/` và chạy `dotnet test`.

### B. Sửa Lỗi (Bug Fixing)
1. Định vị tầng lỗi (View, ViewModel, Engine, Core/Infra).
2. Áp dụng chuẩn an toàn tương ứng (Rule 02/03/05/09).
3. Chạy kiểm thử hồi quy (`dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj`).
4. Bổ sung ít nhất 1 bài test kiểm chứng lỗi không tái phát.

---

## ⚡ 6. Lệnh Vận Hành & Bản Kiểm Kê Hoàn Tất

```powershell
dotnet build WinCarePro.csproj -c Debug                         # Biên dịch kiểm tra lỗi
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --verbosity normal # Chạy bộ test (Mục tiêu: 100% Passed)
.\publish.bat                                                  # Đóng gói Self-Contained x64
.\publish_installer.bat                                        # Tạo bộ cài đặt Inno Setup
```

### ✅ Checklist Tự Rà Soát Trước Khi Bàn Giao:
- [ ] **1. Thứ bậc ưu tiên:** Bảo đảm `Safety > Correctness > Security > Stability > Performance`.
- [ ] **2. Phân tầng chuẩn:** View $\rightarrow$ ViewModel $\rightarrow$ Engine $\rightarrow$ Core; không lẫn WinUI vào Engine.
- [ ] **3. An toàn Zero-Trust:** Dùng `ProcessRunner`, `SafePathGuard`, `SafeRegistryGuard`, DPAPI, không command injection.
- [ ] **4. Đa luồng Zero-Crash:** 100% cập nhật UI qua `RunOnUI()`, không sync-over-async.
- [ ] **5. CancellationToken & Finally:** Luôn giải phóng `IsBusy` và cờ trạng thái trong `finally`.
- [ ] **6. SQLite WAL Safe:** Mọi tương tác SQLite đều bọc `lock (_dbLock)`.
- [ ] **7. Zero-Silent Catch:** Không có khối `catch` rỗng; trả về `OperationResult` hoặc ghi log.
- [ ] **8. Đầy đủ i18n:** Đồng bộ đủ cặp key trên cả `vi-VN` và `en-US`.
- [ ] **9. Bộ nhớ sạch:** Hủy đăng ký event XAML, dọn dẹp visual 3D, giải phóng `IDisposable`.
- [ ] **10. 100% Unit Test Passed:** Tất cả bài test xUnit chạy thành công (`0 Failed`), không phát sinh build warning mới.

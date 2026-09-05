# ⚙️ 03. Tầng Động Cơ Nghiệp Vụ (Engines & Business Logic)

> [⬅️ 02. Chi Tiết 16 Phân Hệ](02_CORE_MODULES_DETAILED.md) • [🏠 Mục Lục Docs](README.md) • **Chương 03** • [Trang Kế Tiếp: 04. Cơ Sở Dữ Liệu & Lưu Trữ ➡️](04_DATABASE_AND_STORAGE.md)

Tài liệu này đi sâu vào kiến trúc, cấu trúc phương thức, thuật toán và cách hoạt động của toàn bộ các lớp Động cơ (Engines) trong thư mục `Engines/`.

---

## 🏗️ Phân Loại Tầng Engines

Các Engines được chia thành 4 nhóm nghiệp vụ chính:

```
Engines/
├── Diagnostics/         # Phân tích, chẩn đoán AI, chấm điểm và thông tin hệ thống
├── Monitoring/          # Giám sát phần cứng, tiến trình, mạng và đo băng thông
├── Optimization/        # Thuật toán dọn rác, dọn RAM, phân tích ổ đĩa, quản lý khởi động
└── Repair/              # Gỡ bỏ ứng dụng, sao lưu Registry, bảo mật, driver và context menu
```

---

## 1. 🔍 Nhóm Động Cơ Chẩn Đoán (Diagnostics Engines)

### 1.1. `AiDiagnosticsEngine`
- **File:** [Engines/Diagnostics/AiDiagnosticsEngine.cs](file:///d:/WinCare/Engines/Diagnostics/AiDiagnosticsEngine.cs)
- **Mục đích:** Bộ máy phân tích Heuristic không phụ thuộc vào đám mây (Cloud-free / Local Heuristic).
- **Thuật toán cốt lõi:**
  - `RunFullDiagnosticAsync(IProgress<DiagnosticProgress> progress, CancellationToken ct)`: Chạy song song 8 luồng chẩn đoán độc lập:
    1. *Memory Pressure*: Kiểm tra tỷ lệ RAM sử dụng > 85% và tỷ lệ Paging File cao.
    2. *Storage Warning*: Kiểm tra dung lượng trống ổ đĩa hệ thống (`C:`) < 15%.
    3. *Junk Volume*: Đánh giá lượng file rác tích tụ trong các thư mục tạm.
    4. *Process Hogging*: Phát hiện các tiến trình không phải hệ thống chiếm dụng CPU liên tục.
    5. *Security Flaws*: Kiểm tra trạng thái Windows Defender, Firewall và UAC.
    6. *Network Latency*: Kiểm tra độ trễ mạng và DNS responsiveness.
    7. *Startup Congestion*: Đánh giá số lượng ứng dụng khởi động loại High Impact.
    8. *Residual Folders*: Tìm kiếm các thư mục AppData mồ côi của các phần mềm đã gỡ.
  - `ExecuteFixActionAsync(DiagnosticItem item)`: Thực thi giải pháp sửa chữa tự động tương ứng cho từng loại vấn đề phát hiện được.

### 1.2. `AiWinCareScoringEngine`
- **File:** [Engines/Diagnostics/AiWinCareScoringEngine.cs](file:///d:/WinCare/Engines/Diagnostics/AiWinCareScoringEngine.cs)
- **Mục đích:** Tính toán chỉ số sức khỏe tổng hợp (Composite System Health Score) từ 0 đến 100 điểm.
- **Công thức tính điểm trọng số:**
  $$\text{Score} = 100 - (\mathbf{W}_{ram} \cdot P_{ram} + \mathbf{W}_{disk} \cdot P_{disk} + \mathbf{W}_{sec} \cdot P_{sec} + \mathbf{W}_{junk} \cdot P_{junk} + \mathbf{W}_{boot} \cdot P_{boot})$$
  - Trong đó: Trọng số Bảo mật ($\mathbf{W}_{sec} = 0.30$), Trọng số Ổ đĩa ($\mathbf{W}_{disk} = 0.25$), Trọng số RAM ($\mathbf{W}_{ram} = 0.20$), Trọng số Rác ($\mathbf{W}_{junk} = 0.15$), Trọng số Khởi động ($\mathbf{W}_{boot} = 0.10$).

### 1.3. `PredictiveAnalysisEngine`
- **File:** [Engines/Diagnostics/PredictiveAnalysisEngine.cs](file:///d:/WinCare/Engines/Diagnostics/PredictiveAnalysisEngine.cs)
- **Mục đích:** Dự báo thời điểm ổ cứng hệ thống đầy và nguy cơ suy giảm tuổi thọ SSD.
- **Phương thức chính:**
  - `ForecastDiskFullDays(string driveLetter)`: Dựa trên lịch sử ghi dữ liệu trong CSDL SQLite để tính tốc độ tiêu thụ GB/ngày và ngoại suy số ngày còn lại.

### 1.4. `SystemEngine`
- **File:** [Engines/Diagnostics/SystemEngine.cs](file:///d:/WinCare/Engines/Diagnostics/SystemEngine.cs)
- **Mục đích:** Tương tác với các công cụ bảo trì gốc của Windows qua [ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs).
- **Phương thức chính:**
  - `RunSfcScanAsync(IProgress<string> outputProgress)`: Gọi `sfc.exe /scannow` bất đồng bộ và parse tiến độ %.
  - `RunDismRestoreHealthAsync(IProgress<string> outputProgress)`: Gọi `DISM.exe /Online /Cleanup-Image /RestoreHealth`.
  - `RunDismComponentCleanupAsync()`: Dọn dẹp kho `WinSxS` bằng `/StartComponentCleanup /ResetBase`.
  - `RepairWindowsUpdateAsync()`: Khởi động lại dịch vụ Update và làm sạch `%windir%\SoftwareDistribution`.

---

## 2. 📊 Nhóm Động Cơ Giám Sát (Monitoring Engines)

### 2.1. `ProcessService`
- **File:** [Engines/Monitoring/ProcessService.cs](file:///d:/WinCare/Engines/Monitoring/ProcessService.cs)
- **Mục đích:** Đọc danh sách và thông số hiệu năng của tất cả các tiến trình đang chạy.
- **Phương thức chính:**
  - `GetRunningProcessesAsync()`: Lấy danh sách kèm PID, Memory Working Set, CPU Usage %, Tên tiến trình, Đường dẫn thực thi và Icon (được cache qua `IconCacheService`).
  - `KillProcessAsync(int processId)`: Kết thúc tiến trình an toàn bằng Win32 `OpenProcess` & `TerminateProcess`.
  - `SetProcessPriority(int processId, ProcessPriorityClass priority)`: Thay đổi độ ưu tiên CPU (sử dụng trong Gaming Turbo).

### 2.2. `NetworkEngine` (Modular Partial Classes)
- **File chính:** [Engines/Monitoring/NetworkEngine.cs](file:///d:/WinCare/Engines/Monitoring/NetworkEngine.cs)
- **Các thành phần mở rộng:**
  - [NetworkEngine.SpeedTest.cs](file:///d:/WinCare/Engines/Monitoring/NetworkEngine.SpeedTest.cs): Tải các tệp mẫu kích thước cố định qua `HttpClient` với bộ đệm stream để tính toán throughput Mbps thực tế và jitter.
  - [NetworkEngine.DnsBenchmark.cs](file:///d:/WinCare/Engines/Monitoring/NetworkEngine.DnsBenchmark.cs): Gửi các gói truy vấn UDP DNS Socket tới danh sách máy chủ DNS công cộng và đo thời gian phản hồi (Round-Trip Time ms).
  - [NetworkEngine.Repair.cs](file:///d:/WinCare/Engines/Monitoring/NetworkEngine.Repair.cs): Gọi tuần tự các lệnh `ipconfig /flushdns`, `netsh winsock reset`, `netsh int ip reset`, `ipconfig /renew`.

---

## 3. ⚡ Nhóm Động Cơ Tối Ưu Hóa (Optimization Engines)

### 3.1. `JunkCleanerEngine`
- **File:** [Engines/Optimization/JunkCleanerEngine.cs](file:///d:/WinCare/Engines/Optimization/JunkCleanerEngine.cs)
- **Mục đích:** Tìm kiếm và dọn dẹp các tệp tin tạm, cache trên toàn bộ ổ đĩa.
- **Cơ chế duyệt tệp:**
  - Sử dụng `Directory.EnumerateFiles` kết hợp `SafePathGuard` để duyệt đệ quy.
  - Kiểm tra đuôi tệp và đường dẫn xem có nằm trong danh sách đen không trước khi gọi `File.Delete`.
  - Tự động bỏ qua lỗi `UnauthorizedAccessException` hoặc `IOException` (khi file đang được dùng) mà không gián đoạn toàn bộ tiến trình quét.

### 3.2. `SystemOptimizerEngine`
- **File:** [Engines/Optimization/SystemOptimizerEngine.cs](file:///d:/WinCare/Engines/Optimization/SystemOptimizerEngine.cs)
- **Mục đích:** Tinh chỉnh Windows Registry, bộ nhớ RAM và cấu hình hiệu năng.
- **Phương thức chính:**
  - `FlushMemoryWorkingSet()`: Duyệt qua tất cả các tiến trình người dùng và gọi hàm Win32 API `EmptyWorkingSet(hProcess)` từ thư viện `psapi.dll`, giải phóng dung lượng RAM vật lý chưa được giải phóng.
  - `ApplySystemTweaksAsync(List<SystemTweak> tweaks)`: Ghi các giá trị DWORD/String vào `HKEY_CURRENT_USER` hoặc `HKEY_LOCAL_MACHINE`.
  - `EnableUltimatePerformancePowerPlan()`: Chạy lệnh `powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61`.

### 3.3. `DiskEngine`
- **File:** [Engines/Optimization/DiskEngine.cs](file:///d:/WinCare/Engines/Optimization/DiskEngine.cs)
- **Phương thức chính:**
  - `GetDriveInfoList()`: Lấy thông tin dung lượng tổng, dung lượng trống, định dạng ổ đĩa (NTFS/FAT32).
  - `GetSmartAttributesAsync(string driveLetter)`: Đọc S.M.A.R.T qua WMI `MSStorageDriver_FailurePredictData` và Win32 `DeviceIoControl`.
  - `FindDuplicateFilesAsync(string searchPath, CancellationToken ct)`: Thuật toán quét 2 bước: Bước 1 gom nhóm theo kích thước file (Length); Bước 2 tính mã băm SHA-256 đối với các file cùng kích thước để xác định trùng lặp chính xác 100%.

### 3.4. `StartupEngine`
- **File:** [Engines/Optimization/StartupEngine.cs](file:///d:/WinCare/Engines/Optimization/StartupEngine.cs)
- **Phương thức chính:**
  - `GetStartupItemsAsync()`: Đọc các mục khởi động từ 4 vị trí:
    1. `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
    2. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
    3. Thư mục `shell:startup` (`%AppData%\Microsoft\Windows\Start Menu\Programs\Startup`)
    4. Thư mục `shell:common startup` (`%ProgramData%\Microsoft\Windows\Start Menu\Programs\Startup`)
  - `ToggleStartupItem(StartupItem item, bool isEnabled)`: Bật/tắt bằng cách di chuyển khóa sang nhánh `RunDisabled` hoặc đổi thuộc tính file shortcut.
  - **Cơ chế phòng vệ:** Tích hợp kiểm tra [SafeRegistryGuard.cs](file:///d:/WinCare/Core/Helpers/SafeRegistryGuard.cs) và [ServiceSafetyService.cs](file:///d:/WinCare/Infrastructure/Security/ServiceSafetyService.cs) để ngăn chặn sửa đổi các dịch vụ hoặc khóa hệ điều hành thiết yếu.

---

## 4. 🛠️ Nhóm Động Cơ Sửa Chữa & Bảo Mật (Repair Engines)

### 4.1. `UninstallEngine`
- **Files:** [UninstallEngine.cs](file:///d:/WinCare/Engines/Repair/UninstallEngine.cs), [UninstallEngine.Scanning.cs](file:///d:/WinCare/Engines/Repair/UninstallEngine.Scanning.cs), [UninstallEngine.Leftovers.cs](file:///d:/WinCare/Engines/Repair/UninstallEngine.Leftovers.cs)
- **Quy trình gỡ bỏ 3 bước (3-Step Uninstallation Flow):**
  1. *Khởi chạy Uninstaller gốc:* Gọi chuỗi lệnh `UninstallString` hoặc `QuietUninstallString` lấy từ Registry.
  2. *Chờ tiến trình kết thúc:* Giám sát PID của trình gỡ cài đặt với Timeout an toàn.
  3. *Deep Leftover Scanning:* Duyệt đệ quy tìm các thư mục và khóa Registry chứa Tên phần mềm hoặc Tên nhà phát hành (Publisher), hiển thị cây tàn dư cho người dùng xác nhận trước khi xóa vĩnh viễn.

### 4.2. `SecurityPrivacyEngine`
- **File:** [Engines/Repair/SecurityPrivacyEngine.cs](file:///d:/WinCare/Engines/Repair/SecurityPrivacyEngine.cs)
- **Phương thức chính:**
  - `GetSecurityStatusAsync()`: Kiểm tra trạng thái Defender qua WMI `root\SecurityCenter2` và Registry.
  - `ToggleTelemetry(bool disable)`: Cấu hình chính sách Group Policy / Registry `AllowTelemetry = 0` trong `HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection`.
  - `VerifyDigitalSignature(string filePath)`: Sử dụng hàm Win32 API `WinVerifyTrust` từ `wintrust.dll` để kiểm tra chứng chỉ số và tính toàn vẹn của tệp thực thi.

### 4.3. `SoftwareUpdaterEngine`
- **File:** [Engines/Repair/SoftwareUpdaterEngine.cs](file:///d:/WinCare/Engines/Repair/SoftwareUpdaterEngine.cs)
- **Phương thức chính:**
  - `ScanForUpdatesAsync()`: Thực thi lệnh `winget upgrade --include-unknown` với output parser phân tích danh sách: Tên phần mềm, ID, Phiên bản hiện tại, Phiên bản mới nhất.
  - `UpdatePackageAsync(string packageId, IProgress<string> logProgress)`: Thực thi `winget upgrade --id <packageId> --silent --accept-package-agreements --accept-source-agreements`.

### 4.4. `RegistryBackupEngine`
- **File:** [Engines/Repair/RegistryBackupEngine.cs](file:///d:/WinCare/Engines/Repair/RegistryBackupEngine.cs)
- **Phương thức chính:**
  - `BackupKey(RegistryHive hive, string subKeyPath, string destinationRegFile)`: Gọi `reg.exe export` qua ProcessRunner để tạo file `.reg` tiêu chuẩn.
  - `RestoreBackup(string regFilePath)`: Gọi `reg.exe import` để khôi phục trạng thái.

### 4.5. `HardwareDriverEngine`
- **File:** [Engines/Repair/HardwareDriverEngine.cs](file:///d:/WinCare/Engines/Repair/HardwareDriverEngine.cs)
- **Phương thức chính:**
  - `GetInstalledDriversAsync()`: Truy vấn WMI `Win32_PnPSignedDriver` để lấy danh sách driver thiết bị, phiên bản, ngày phát hành và nhà sản xuất.
  - `BackupDriversAsync(string backupDirectory)`: Sử dụng lệnh `dism.exe /online /export-driver /destination:<path>` để sao lưu toàn bộ driver của bên thứ 3.

---

> [⬅️ 02. Chi Tiết 16 Phân Hệ](02_CORE_MODULES_DETAILED.md) • [🏠 Mục Lục Docs](README.md) • **Chương 03** • [Trang Kế Tiếp: 04. Cơ Sở Dữ Liệu & Lưu Trữ ➡️](04_DATABASE_AND_STORAGE.md)

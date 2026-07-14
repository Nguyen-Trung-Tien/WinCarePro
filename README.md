# 🚀 WinCare Pro Suite

<div align="center">
  <img src="Assets/Square150x150Logo.scale-200.png" alt="WinCare Pro Logo" width="120" height="120" style="border-radius: 24%; box-shadow: 0 10px 25px rgba(127, 86, 217, 0.3); margin-bottom: 20px;" />

  <h3>Hệ Thống Tối Ưu Hóa, Dọn Dẹp & Sửa Lỗi Windows Toàn Diện</h3>
  <p align="center">
    Một bộ công cụ bảo trì máy tính mã nguồn mở, gọn nhẹ, hiện đại được xây dựng dựa trên giao diện <b>Fluent Design (WinUI 3 / Windows App SDK)</b> và sức mạnh của <b>.NET 10.0 & SQLite</b>.
  </p>

  <p align="center">
    <a href="https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v3.4.4/WinCareProSetup.exe">
      <img src="https://img.shields.io/badge/Download-Latest%20Release%20v3.4.4-blueviolet?style=for-the-badge&logo=windows&logoColor=white&color=7F56D9" alt="Download WinCare Pro" />
    </a>
  </p>

  <p align="center">
    <img src="https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square&logo=.net&logoColor=white&color=7F56D9" alt=".NET 10.0" />
    <img src="https://img.shields.io/badge/UI_Framework-WinUI_3-0078D4?style=flat-square&logo=windows" alt="WinUI 3" />
    <img src="https://img.shields.io/badge/Database-SQLite_3-003B57?style=flat-square&logo=sqlite&logoColor=white" alt="SQLite 3" />
    <img src="https://img.shields.io/badge/Architecture-MVVM%20(Modular)-008080?style=flat-square" alt="MVVM Pattern" />
    <img src="https://img.shields.io/badge/OS_Support-Windows_10_%2F_11-0078D6?style=flat-square&logo=windows" alt="Windows 10/11" />
  </p>
</div>

---

## 📖 Tổng quan dự án

**WinCare Pro** là một bộ ứng dụng chăm sóc hệ điều hành Windows thế hệ mới. Với giao diện Mica trong suốt, hiệu ứng chuyển động mượt mà và thiết kế Fluent Design nguyên bản của Windows 11, WinCare Pro cung cấp giải pháp tất-cả-trong-một giúp chẩn đoán, dọn dẹp, bảo mật, chắp vá hệ thống và giám sát tài nguyên phần cứng theo thời gian thực.

Ứng dụng được thiết kế tối giản nhưng mạnh mẽ, không chứa quảng cáo hay tiến trình chạy ngầm làm chậm máy, mang lại hiệu suất tối đa cho trải nghiệm máy tính của bạn.

---

## 📐 Kiến trúc & Mô hình hoạt động (Modular MVVM)

Mã nguồn của WinCare Pro được tổ chức theo mô hình **MVVM (Model-View-ViewModel)** chuẩn hóa và mô-đun hóa cao. Các tệp dữ liệu dùng chung (Data Models), hệ thống dịch thuật và các lớp điều khiển View/ViewModel lớn đều đã được phân rã thành các tệp phân phần chuyên biệt (partial classes) để tối ưu hóa hiệu năng biên dịch và quản lý mã nguồn.

```mermaid
graph TD
    %% Styling
    classDef ui fill:#f9f,stroke:#333,stroke-width:2px;
    classDef vm fill:#bbf,stroke:#333,stroke-width:2px;
    classDef engine fill:#dfd,stroke:#333,stroke-width:2px;
    classDef data fill:#fdd,stroke:#333,stroke-width:2px;

    subgraph UI [Lớp Giao Diện - View Layer]
        MainPage[MainPage.xaml]:::ui
        DashboardPage[DashboardPage.xaml]:::ui
        JunkPage[JunkPage.xaml]:::ui
        DiskPage[DiskPage.xaml]:::ui
        NetworkPage[NetworkPage.xaml]:::ui
        AppPage[Uninstall/AppPage.xaml]:::ui
        RepairPage[RepairPage.xaml]:::ui
        SecurityPage[SecurityPage.xaml]:::ui
        StartupPage[StartupPage.xaml]:::ui
        ProcessPage[ProcessPage.xaml]:::ui
        HardwarePage[HardwarePage.xaml]:::ui
        RegistryPage[RegistryPage.xaml]:::ui
        UpdaterPage[UpdaterPage.xaml]:::ui
        DriverPage[DriverPage.xaml]:::ui
        SettingsPage[SettingsPage.xaml]:::ui
    end

    subgraph Logic [Lớp Giao Tiếp - ViewModel Layer]
        DashboardVM[DashboardViewModel.cs<br/>.Diagnostics.cs / .Monitoring.cs]:::vm
        JunkVM[JunkCleanerViewModel.cs]:::vm
        DiskVM[DiskViewModel.cs]:::vm
        NetworkVM[NetworkViewModel.cs<br/>.Adapters.cs / .DnsRepair.cs / .Tools.cs]:::vm
        UninstallVM[UninstallViewModel.cs]:::vm
        SecurityVM[SecurityViewModel.cs]:::vm
        StartupVM[StartupViewModel.cs]:::vm
        ProcessVM[ProcessViewModel.cs]:::vm
        HardwareVM[HardwareViewModel.cs]:::vm
        RegistryVM[RegistryViewModel.cs]:::vm
        UpdaterVM[UpdaterViewModel.cs]:::vm
        DriverVM[DriverViewModel.cs]:::vm
    end

    subgraph CoreEngine [Bộ Máy Xử Lý - Service & Engine Layer]
        AiDiagnostics[AiDiagnosticsEngine]:::engine
        JunkCleaner[JunkCleanerEngine]:::engine
        DiskEngine[DiskEngine]:::engine
        NetEngine[NetworkEngine.cs<br/>.SpeedTest.cs / .DnsBenchmark.cs / .Repair.cs]:::engine
        SysOptimizer[SystemOptimizerEngine]:::engine
        RegistryBackup[RegistryBackupEngine]:::engine
        SoftwareUpdater[SoftwareUpdaterEngine]:::engine
        DriverEngine[HardwareDriverEngine]:::engine
        UninstallEngine[UninstallEngine.cs<br/>.Scanning.cs / .Leftovers.cs]:::engine
    end

    subgraph DataOS [Dữ Liệu & Hệ Điều Hành - Data & OS Layer]
        SqliteDB[(SQLite DB - wincaredb.db)]:::data
        WmiHelper[WmiHelper.cs / Windows WMI]:::data
        RegistryStore[Windows Registry]:::data
        WinAPI[Windows System APIs / Shell]:::data
        TranslationMgr[TranslationManager.cs<br/>.Translations.cs / .Extensions.cs]:::data
    end

    %% Connections
    MainPage --> DashboardPage
    DashboardPage <--> DashboardVM
    JunkPage <--> JunkVM
    DiskPage <--> DiskVM
    NetworkPage <--> NetworkVM
    AppPage <--> UninstallVM
    
    DashboardVM --> AiDiagnostics
    JunkVM --> JunkCleaner
    DiskVM --> DiskEngine
    NetworkVM --> NetEngine
    UninstallVM --> UninstallEngine

    AiDiagnostics --> SqliteDB
    JunkCleaner --> WinAPI
    DiskEngine --> WmiHelper
    NetEngine --> WinAPI
    UninstallEngine --> WinAPI & RegistryStore
    TranslationMgr -.-> UI
```

---

## ✨ 14 Phân hệ chức năng cốt lõi (Core Modules)

WinCare Pro cung cấp 14 công cụ chuyên nghiệp, truy cập trực tiếp qua thanh điều hướng (Sidebar):

### 1. 📊 Bảng điều khiển (Dashboard)
* **Giám sát thời gian thực:** Theo dõi biểu đồ động đo hiệu suất CPU, RAM, dung lượng đĩa và hoạt động I/O của hệ thống.
* **Điểm sức khỏe AI (Composite Health Score):** Sử dụng các thuật toán phân tích nhanh để đánh giá tình trạng PC theo thang điểm từ `0` đến `100` và đưa ra đề xuất xử lý tối ưu.

### 2. 🧹 Dọn rác hệ thống (Junk Cleaner)
* **Lõi quét đa luồng:** Dọn dẹp sạch sẽ các tệp tin rác hệ thống (Temp Files), Nhật ký hoạt động (Logs), Tệp đổ bộ nhớ lỗi (Memory Dumps), và bộ nhớ đệm trình duyệt.
* **Phân tích lưu trữ trực quan:** Hiển thị chi tiết tỷ lệ các nhóm tệp tin rác bằng biểu đồ tròn (Pie Chart) trực quan.

### 3. 🚀 Trình gỡ ứng dụng (App Uninstaller)
* **Gỡ cài đặt hàng loạt (Batch Uninstall):** Hỗ trợ chọn nhiều ứng dụng cùng lúc để gỡ cài đặt tự động.
* **Quét dọn tàn dư chuyên sâu:** Tự động phát hiện và xóa bỏ sạch sẽ các khóa Registry thừa, thư mục rác (Leftovers) còn sót lại của ứng dụng.
* **Buộc gỡ ứng dụng UWP:** Gỡ bỏ các ứng dụng Microsoft Store cài sẵn cứng đầu bằng lệnh PowerShell an toàn.

### 4. 🌐 Giám sát mạng (Network Center)
* **Theo dõi băng thông:** Biểu đồ đường thời gian thực đo lường chính xác tốc độ Upload và Download.
* **Giám sát kết nối:** Thống kê chi tiết danh sách tiến trình đang kết nối mạng và chiếm dụng băng thông.
* **Bộ công cụ mạng nâng cao:** Ping Test, Packet Loss check, Flush DNS, Renew IP, Benchmarking DNS và Speed Test (Đo tốc độ mạng).

### 5. 🛠️ Sửa lỗi hệ thống (System Repair)
* **Khôi phục tập tin cốt lõi:** Chạy trực tiếp công cụ chẩn đoán hệ thống SFC (`sfc /scannow`) và công cụ sửa lỗi ổ đĩa DISM (`RestoreHealth`).
* **Trực quan hóa tiến trình:** Hiển thị chi tiết từng bước kiểm tra hệ thống và phần trăm tiến độ cụ thể.

### 6. 🛡️ Khiên bảo mật (Security Shield)
* **Quản trị bảo mật:** Giám sát trạng thái hoạt động của Windows Defender, Tường lửa (Firewall) và quyền kiểm soát tài khoản người dùng UAC.
* **Bảo vệ quyền riêng tư (Privacy Tweaks):** Cho phép bật/tắt các quyền thu thập dữ liệu ngầm, quyền truy cập Camera/Microphone/Vị trí của ứng dụng.

### 7. ⚡ Tinh chỉnh hiệu năng (System Optimizer)
* **Giải phóng RAM vật lý (RAM Booster):** Dọn dẹp Working Sets của các chương trình để giải phóng RAM trống ngay lập tức.
* **Tối ưu hóa hệ thống:** Cấu hình tinh chỉnh Windows Explorer, tăng tốc phản hồi phản hồi ứng dụng và kích hoạt chế độ chơi game (Game Mode).

### 8. 📂 Khởi động & Dịch vụ (Startup & Services)
* **Quản lý khởi động:** Liệt kê các chương trình tự khởi động cùng Windows, đánh giá mức độ ảnh hưởng đến thời gian boot máy và cho phép bật/tắt.
* **Quản trị Services:** Theo dõi và kiểm soát hoạt động của các dịch vụ hệ thống chạy ngầm.

### 9. 📊 Quản lý tiến trình (Process Manager)
* **Thông tin chi tiết:** Thống kê dung lượng RAM, CPU, số luồng (Threads) và Handles của từng tiến trình đang chạy.
* **Kiểm soát tác vụ:** Hỗ trợ đóng băng (Suspend) hoặc dừng cưỡng bức (End Task) các tiến trình bị treo.

### 10. 💾 Công cụ ổ đĩa (Disk Tools)
* **Sức khỏe ổ đĩa (SMART Info):** Đọc thông số nhiệt độ, tỷ lệ lỗi và tình trạng sức khỏe thực tế của ổ đĩa cứng (SSD/HDD).
* **Phân tích dung lượng (Storage Analyzer):** Quét thư mục bất kỳ để tìm ra các tệp tin và thư mục đang chiếm dụng dung lượng lớn nhất.
* **Tìm tệp trùng lặp (Duplicate Finder):** Tìm kiếm và dọn dẹp các tệp tin bị trùng nội dung giúp giải phóng dung lượng đĩa.

### 11. 💻 Thông tin phần cứng (Hardware Center)
* **Đặc tả chi tiết:** Hiển thị toàn bộ thông tin phần cứng bao gồm: CPU (Xung nhịp, nhân/luồng), GPU, RAM, Bo mạch chủ, BIOS và hệ điều hành.

### 12. 🔧 Quản trị Registry (Registry Center)
* **Dọn dẹp Registry:** Quét và sửa các khóa đăng ký lỗi, đường dẫn ứng dụng hỏng hoặc registry rác.
* **Khôi phục cấu hình:** Sao lưu Registry tự động và hỗ trợ tạo điểm khôi phục (Restore Point) hệ thống.

### 13. 🔄 Cập nhật phần mềm (Software Updater)
* **Quản lý ứng dụng bên thứ ba:** Kiểm tra phiên bản mới của các ứng dụng đã cài đặt trên máy.
* **Cập nhật nhanh chóng:** Tải và cài đặt tự động thông qua Windows Package Manager (winget) hoặc liên kết trực tiếp.

### 14. 🔌 Cập nhật Driver (Driver Updater)
* **Quét Driver thiết bị:** Tự động phát hiện các Driver phần cứng đã lỗi thời hoặc còn thiếu.
* **Thuật sĩ cài đặt:** Hướng dẫn quy trình 3 bước trực quan giúp tải xuống và cập nhật driver an toàn.

---

## 🔔 Tiện ích hệ thống bổ sung
* **Trung tâm thông báo (Notification Center):** Nơi lưu trữ lịch sử hoạt động bảo trì, dọn dẹp và hiển thị các gợi ý bảo vệ máy tính thời gian thực.
* **Cài đặt & Đa ngôn ngữ (Settings):** Hỗ trợ đổi giao diện Màu Sáng / Tối (Light/Dark Theme), chuyển đổi tức thì giữa **Tiếng Việt** và **Tiếng Anh**, cấu hình tự động cập nhật ngầm.

---

## 🛠️ Công nghệ & Dependencies chính

Ứng dụng được tối ưu hóa sâu với các công nghệ phần mềm mới nhất của Microsoft:

| Thư viện / Công nghệ | Phiên bản | Vai trò trong hệ thống |
| :--- | :--- | :--- |
| **.NET SDK** | `10.0` | Môi trường thực thi thế hệ mới, tối ưu hóa bộ nhớ và tốc độ xử lý. |
| **Windows App SDK** | `2.2.0` | Thư viện WinUI 3 mang lại giao diện Fluent Design mượt mà trên Windows 10/11. |
| **CommunityToolkit.Mvvm** | `8.2.2` | Bộ công cụ chuẩn hóa cấu trúc MVVM, tách biệt giao diện và logic nghiệp vụ. |
| **Microsoft.Data.Sqlite** | `10.0.9` | Cơ sở dữ liệu SQLite cục bộ lưu trữ lịch sử hoạt động gọn nhẹ và an toàn. |
| **System.Management** | `10.0.9` | Truy vấn WMI lấy thông số phần cứng chính xác của máy tính. |
| **TaskScheduler** | `2.12.2` | Đăng ký tác vụ bảo trì tự động với hệ điều hành. |

---

## 📥 Hướng dẫn Cài đặt & Sử dụng

### Cách 1: Sử dụng bộ cài đặt đóng gói sẵn (Khuyên dùng cho người dùng)
1. Truy cập vào phần [Releases](https://github.com/Nguyen-Trung-Tien/WinCarePro/releases) hoặc nhấn nút **Download** ở đầu trang README để tải về file **`WinCareProSetup.exe`**.
2. Mở file exe vừa tải về để bắt đầu cài đặt (Yêu cầu quyền Administrator để cài đặt các dịch vụ hệ thống bổ trợ).

### Cách 2: Tự biên dịch từ mã nguồn (Dành cho nhà phát triển)

#### **Yêu cầu hệ thống:**
* **Hệ điều hành:** Windows 10 (Build 19041 trở lên) hoặc Windows 11.
* **IDE:** Visual Studio 2022 (được tích hợp gói *Desktop development with .NET*).
* **SDK:** .NET 10.0 SDK trở lên.

#### **Các bước biên dịch:**
1. Tải mã nguồn về máy:
   ```bash
   git clone https://github.com/Nguyen-Trung-Tien/WinCarePro.git
   cd WinCarePro
   ```
2. Khôi phục các gói NuGet dependencies:
   ```bash
   dotnet restore
   ```
3. Khởi chạy ứng dụng dưới chế độ Debug:
   ```bash
   dotnet run
   ```

---

## 📦 Công cụ đóng gói & Phát hành chuyên nghiệp

Thư mục gốc chứa các kịch bản tự động hóa quá trình đóng gói và phát hành ứng dụng:

* **Tạo bản Portable (`publish.bat`):** Biên dịch ứng dụng thành một tệp thực thi duy nhất đã được nén và tối ưu hóa (`PublishSingleFile=true`, `PublishReadyToRun=true`). Bản này có thể chạy trực tiếp trên bất kỳ máy tính Windows nào tại đường dẫn `.\PublishOutput\WinCarePro.exe`.
* **Tạo bộ cài Setup (`publish_installer.bat`):** Sử dụng **Inno Setup 6** kết hợp với kịch bản [setup.iss](file:///d:/WinCare/setup.iss) để đóng gói toàn bộ ứng dụng và runtime tự cấp (Self-contained) thành tệp cài đặt chuyên nghiệp `.\PublishOutput\WinCareProSetup.exe`.

---

## 📂 Cấu trúc thư mục nguồn của Dự án

```text
WinCare/
│
├── Assets/                 # Tài nguyên đồ họa, hình ảnh và icon Fluent của ứng dụng
├── Core/                   # Thư mục cốt lõi chứa các Helpers và mô hình dữ liệu (Models) dùng chung
│   ├── Helpers/            # Các tiện ích hệ thống (như WmiHelper.cs)
│   └── Models/             # Các Data Models phân tách (như ProcessInfo.cs, DriverInfo.cs...)
├── Engines/                # Động cơ xử lý logic cốt lõi (Diagnostics, Monitoring, Optimization, Repair)
├── Infrastructure/         # Xử lý Caching, Database (SQLite), Logging, Scheduling và Security
├── Modules/                # Tập hợp các View (xaml) và ViewModel (cs) phân theo trang chức năng MVVM
├── Services/               # Các dịch vụ hệ thống bổ trợ (Dịch thuật, thông báo, dọn dẹp...)
├── Shared/                 # Các UI Components tùy biến và Converter giao diện dùng chung
│
├── App.xaml / App.xaml.cs  # Điểm khởi chạy cấu hình và thiết lập điều hướng toàn cục
├── MainWindow.xaml / .cs   # Cửa sổ chính chứa khung điều hướng và thanh tiêu đề
├── WinCarePro.csproj       # File cấu hình cấu trúc dự án và các NuGet Dependencies
├── app.manifest            # Tệp cấu hình các đặc quyền bảo mật và thực thi của Windows
├── publish.bat             # Batch script đóng gói ứng dụng di động Portable
├── publish_installer.bat   # Batch script tự động build bộ cài đặt Inno Setup
└── setup.iss               # Kịch bản biên dịch bộ cài đặt Inno Setup
```

---

## 📝 Giấy phép (License) & Đóng góp ý kiến
Nếu bạn phát hiện lỗi hoặc có bất kỳ ý kiến đóng góp phát triển ứng dụng tốt hơn, vui lòng tạo một **Issue** hoặc gửi **Pull Request** trực tiếp trên kho lưu trữ mã nguồn này. Xem thêm [Nhật ký Phát hành (RELEASE_NOTES.md)](file:///d:/WinCare/RELEASE_NOTES.md) để biết chi tiết các thay đổi trong phiên bản mới nhất v3.4.4.

---
<div align="center">
  <sub>Được phát triển và thiết kế bởi <b>Nguyễn Trung Tiến</b></sub>
</div>

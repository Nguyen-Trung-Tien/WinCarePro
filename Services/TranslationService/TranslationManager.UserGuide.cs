using System;
using System.Collections.Generic;

namespace WinCarePro.Services;

/// <summary>
/// Separate Partial Class containing all User Guide and Feature Manual Translations.
/// Modify this file to edit or add detailed guide descriptions for WinCare Pro modules.
/// </summary>
public partial class TranslationManager
{
    private void InitializeUserGuideTranslations()
    {
        // --- General Guide Section Headers ---
        _translations["Feature Guide & Manual"] = "Hướng dẫn sử dụng";
        _translations["Feature Guide & System Manual"] = "Hướng dẫn sử dụng";
        _translations["Comprehensive documentation explaining the purpose, detailed features, step-by-step usage, and safety guidelines for each WinCare Pro module."] = "Tài liệu chi tiết mô tả mục đích, tính năng nổi bật, hướng dẫn thao tác từng bước và mức độ an toàn của từng phân hệ WinCare Pro.";

        _translations["Purpose & Overview"] = "Mục đích & Nguyên lý hoạt động";
        _translations["Key Capabilities"] = "Tính năng chi tiết";
        _translations["Step-by-Step Guide"] = "Hướng dẫn sử dụng từng bước";
        _translations["Safety Rating"] = "Mức độ an toàn";
        _translations["100% Safe (No System Impact)"] = "100% An toàn (Không rủi ro hệ thống)";
        _translations["Safe (Recommended)"] = "An toàn (Khuyên dùng)";
        _translations["Requires Administrator Privileges"] = "Yêu cầu Quyền QTV (Administrator)";

        // --- 1. Dashboard & System Health Monitor ---
        _translations["1. Dashboard & System Health Monitor"] = "1. Tổng Quan & Chẩn Đoán Sức Khỏe Hệ Thống";
        _translations["Provides real-time system monitoring for CPU, RAM, GPU, and Disk utilization, along with continuous Health Score tracking and one-click Quick Boost optimization."] = "Cung cấp giao diện giám sát hiệu năng CPU, RAM, GPU, Ổ đĩa theo thời gian thực, đo điểm số sức khỏe hệ thống và nút Quick Boost tối ưu nhanh 1-Click.";
        _translations["• Real-time animated hardware telemetry meters.\n• Instant RAM & CPU Working Set flushing via Quick Boost.\n• Dynamic system health status alert indicators."] = "• Đồng hồ đo thông số phần cứng động theo thời gian thực.\n• Nút Quick Boost giải phóng RAM và làm sạch làm việc CPU tức thì.\n• Cảnh báo trạng thái sức khỏe hệ thống theo mã màu trực quan.";
        _translations["1. Open Dashboard from the top of the left navigation pane.\n2. Observe hardware loads and current Health Score.\n3. Click [Quick Boost] to instantly reclaim RAM and optimize system processes."] = "1. Mở trang Tổng quan từ đầu thanh điều hướng bên trái.\n2. Theo dõi mức sử dụng phần cứng và Điểm số Sức khỏe hiện tại.\n3. Nhấn nút [Quick Boost] để giải phóng RAM và tối ưu tiến trình ngay lập tức.";

        // --- 2. AI Assistant & Diagnostics ---
        _translations["2. AI Assistant & Diagnostics"] = "2. Trợ Lý WinCare AI & Chẩn Đoán Hệ Thống";
        _translations["Leverages intelligent AI algorithms to comprehensively scan the Windows system, analyze real-time health indicators, detect accumulated junk files, RAM bottlenecks, slowing background services, and potential security risks."] = "Sử dụng thuật toán AI thông minh để quét toàn diện hệ thống Windows, phân tích các chỉ số sức khỏe, phát hiện tệp rác tích tụ, xung đột bộ nhớ RAM, dịch vụ gây chậm máy và nguy cơ tiềm ẩn.";
        _translations["• Real-time Health Score calculation based on system telemetry.\n• Predictive analysis for C: drive storage and boot speed trends.\n• One-click automated recommendations for system optimizations."] = "• Tính toán Điểm số Sức khỏe (Health Score) theo thời gian thực.\n• Dự đoán dung lượng ổ C: và tốc độ boot máy trong tương lai.\n• Tự động đưa ra danh sách đề xuất khắc phục tối ưu 1-Click.";
        _translations["1. Navigate to the AI Assistant module from the main sidebar.\n2. Click [Run AI Diagnostics] to start deep system analysis.\n3. Review findings and click [Apply Recommended Tweaks] to automatically optimize."] = "1. Mở phân hệ Trợ lý AI từ menu chính bên trái.\n2. Nhấn nút [Run AI Diagnostics] để bắt đầu phân tích.\n3. Xem kết quả chẩn đoán và nhấn [Apply Recommended Tweaks] để tự động tối ưu hóa.";

        // --- 3. Junk Cleaner & Debris ---
        _translations["3. Junk Cleaner & Debris"] = "3. Dọn Dẹp Tệp Rác & Tệp Tạm System";
        _translations["Deeply scans all storage drives to locate and wipe redundant junk files created by Windows OS, web browsers, and third-party applications during daily usage."] = "Quét sâu toàn bộ ổ đĩa để tìm và xóa sạch các tệp rác thừa do hệ điều hành Windows và các phần mềm ứng dụng tạo ra trong quá trình sử dụng.";
        _translations["• Cleans Windows temporary files (%TEMP%, C:\\Windows\\Temp, Prefetch, Log files).\n• Wipes web browser cache (Chrome, Edge, Firefox, Brave).\n• Clears Windows Update download cache and installer temporary files."] = "• Làm sạch bộ nhớ tạm Windows (%TEMP%, C:\\Windows\\Temp, Prefetch, Log).\n• Dọn dẹp cache trình duyệt web (Google Chrome, Edge, Firefox, Brave).\n• Quét bộ đệm cập nhật Windows Update cache và Installer temp files.";
        _translations["1. Select [Junk Cleaner] on the main navigation panel.\n2. Click [Scan Directories] to scan for removable system clutter.\n3. Check target categories and click [Clean Now] to free up disk space."] = "1. Chọn mục [Junk Cleaner] trên thanh điều hướng.\n2. Nhấn nút [Scan Directories] để tìm kiếm tệp rác.\n3. Đánh dấu các danh mục muốn dọn và nhấn [Clean Now].";

        // --- 4. App Uninstaller & Leftovers ---
        _translations["4. App Uninstaller & Leftovers"] = "4. Gỡ Ứng Dụng & Dọn Tàn Dư Triệt Để";
        _translations["Uninstalls installed applications and default Windows Bloatware, followed by deep residual registry and directory cleanup."] = "Gỡ bỏ phần mềm đã cài đặt và ứng dụng mặc định Windows (Bloatware), sau đó tự động truy quét dọn sạch các thư mục rác và Registry Key thừa còn sót lại.";
        _translations["• Batch uninstall multiple applications simultaneously.\n• Deep leftover scanner purges AppData, ProgramData, and Registry traces.\n• Complete removal of unused Windows Store packages."] = "• Gỡ bỏ hàng loạt ứng dụng cùng lúc.\n• Quét dọn tàn dư tận gốc trong AppData, ProgramData và Registry.\n• Gỡ bỏ triệt để các ứng dụng Windows Store mặc định không dùng đến.";
        _translations["1. Launch [App Uninstaller] module.\n2. Select software applications to remove.\n3. Click [Uninstall] and choose [Wipe Leftovers] to clean all residual data."] = "1. Mở phân hệ [App Uninstaller].\n2. Tìm và chọn ứng dụng muốn gỡ bỏ.\n3. Nhấn [Uninstall] và chọn [Wipe Leftovers] để dọn sạch hoàn toàn tàn dư.";

        // --- 5. Network Center ---
        _translations["5. Network Center"] = "5. Trung Tâm Tối Ưu Mạng & Kết Nối";
        _translations["Diagnoses internet connection health, tests bandwidth speeds, and resolves network connectivity issues or latency spikes."] = "Chẩn đoán chất lượng kết nối Internet, kiểm tra tốc độ băng thông và sửa các sự cố không vào được mạng hoặc giật lag khi chơi game online.";
        _translations["• Precision Speed Test for Download, Upload, and Latency/Ping metrics.\n• One-click Flush DNS Resolver Cache.\n• TCP Auto-Tuning optimization and Secure DoH (DNS over HTTPS) toggles."] = "• Speedtest đo tốc độ Download, Upload và Ping chuẩn xác.\n• Xóa bộ nhớ đệm DNS (Flush DNS Resolver cache).\n• Tinh chỉnh cấu hình TCP Auto-Tuning và DoH (DNS over HTTPS) bảo mật.";
        _translations["1. Open [Network Center].\n2. Click [Run Speed Test] to analyze latency and bandwidth.\n3. Click [Flush DNS Cache] or [Reset TCP/IP Stack] to resolve network issues."] = "1. Truy cập trang [Network Center].\n2. Nhấn [Run Speed Test] để kiểm tra chất lượng đường truyền.\n3. Nhấn [Flush DNS Cache] hoặc [Reset TCP/IP Stack] nếu gặp sự cố mạng.";

        // --- 6. System Repair Center ---
        _translations["6. System Repair Center"] = "6. Trung Tâm Khắc Phục Sửa Lỗi Windows";
        _translations["Integrates advanced Windows diagnostic and repair utilities to automatically repair corrupt system files, component store corruption, and Windows Update errors."] = "Tích hợp bộ công cụ chẩn đoán và khắc phục sự cố hệ điều hành chuyên sâu giúp tự động sửa chữa các tệp hệ thống bị hỏng và lỗi Windows Update.";
        _translations["• SFC Repair (sfc /scannow): Scans and restores corrupted Windows system binaries.\n• DISM Component Restore: Repairs damaged Windows Component Store packages.\n• Windows Update Repair: Resets update cache and stuck service states.\n• Network Repair: Resets Winsock catalog and TCP/IP stack."] = "• SFC Scan (sfc /scannow): Kiểm tra và sửa tệp hệ thống Windows bị hỏng.\n• DISM Restore: Sửa chữa kho linh kiện linh hồn Windows (Component Store).\n• Repair Windows Update: Reset bộ nhớ đệm và dịch vụ cập nhật bị kẹt.\n• Network Repair: Reset Winsock Catalog và TCP/IP stack.";
        _translations["1. Go to [System Repair] section.\n2. Select [SFC Repair] or [DISM Restore] based on system issues.\n3. Allow process to complete and restart Windows if prompted."] = "1. Truy cập mục [System Repair].\n2. Chọn nút [SFC Repair] hoặc [DISM Restore] tùy theo lỗi hệ thống gặp phải.\n3. Chờ quá trình sửa lỗi hoàn tất và khởi động lại máy nếu được yêu cầu.";

        // --- 7. Security Shield & Privacy ---
        _translations["7. Security Shield & Privacy"] = "7. Bảo Mật & Bảo Vệ Quyền Riêng Tư";
        _translations["Protects personal privacy by purging activity histories, temporary clipboard data, and disabling Windows telemetry tracking."] = "Bảo vệ riêng tư cá nhân bằng cách xóa sạch lịch sử hoạt động, vết vết sử dụng ứng dụng và vô hiệu hóa các trình theo dõi Telemetry của Windows.";
        _translations["• Wipes sensitive data stored in clipboard cache (Wipe Clipboard).\n• Clears Recent Files and application launch history.\n• Disables telemetry and diagnostic tracking services."] = "• Xóa dữ liệu nhạy cảm lưu trong bộ nhớ tạm Clipboard (Wipe Clipboard).\n• Dọn dẹp lịch sử mở tệp gần đây (Recent Files & Run History).\n• Tắt các tính năng thu thập dữ liệu Telemetry & Diagnostic tracking mặc định.";
        _translations["1. Open [Security Shield] section.\n2. Click [Wipe Clipboard Cache] to secure clipboard contents.\n3. Click [Clear Recent Files] to remove personal activity traces."] = "1. Mở trang [Security Shield].\n2. Nhấn [Wipe Clipboard Cache] để bảo vệ dữ liệu bộ nhớ tạm.\n3. Nhấn [Clear Recent Files] để xóa lịch sử hoạt động cá nhân.";

        // --- 8. System Optimizer & RAM Booster ---
        _translations["8. System Optimizer & RAM Booster"] = "8. Tối Ưu Hệ Thống & Tăng Tốc RAM";
        _translations["Applies Microsoft-compliant Registry tweaks to optimize CPU scheduling, disable unnecessary services, and reclaim active physical RAM memory."] = "Áp dụng các tinh chỉnh Registry chuẩn Microsoft nhằm tối ưu hóa lập lịch CPU, vô hiệu hóa dịch vụ không cần thiết và dọn dẹp vùng nhớ RAM vật lý.";
        _translations["• Gaming Turbo 2.0 Mode: Flushes process working sets and prioritizes CPU for active games.\n• Windows Registry latency tweaks for ultra-responsive system performance.\n• Automatic creation of System Restore Points prior to applying tweaks."] = "• Chế độ Gaming Turbo 2.0: Ép dọn bộ nhớ RAM (Working Set flushing) và ưu tiên CPU cho Game/Đồ họa.\n• Tinh chỉnh hệ thống Windows Registry tối ưu tốc độ phản hồi.\n• Tự động tạo điểm khôi phục System Restore Point trước khi tối ưu.";
        _translations["1. Open the [System Optimizer] page.\n2. Toggle [Enable Gaming Turbo] for instant gaming acceleration.\n3. Select desired system tweaks and click [Apply Tweaks]."] = "1. Truy cập mục [System Optimizer].\n2. Nhấn [BẬT TURBO NGAY] để tăng tốc tức thì cho chơi game.\n3. Tích chọn các tinh chỉnh hệ thống mong muốn và nhấn [Apply Tweaks].";

        // --- 9. Context Menu Manager ---
        _translations["9. Context Menu Manager"] = "9. Quản Lý Menu Ngữ Cảnh Chuột Phải";
        _translations["Manages shortcuts and background shell extensions displayed in the Windows Explorer right-click Context Menu for cleaner, faster File Explorer operation."] = "Quản lý các lệnh và extension hiển thị trong Menu chuột phải (Context Menu) giúp Windows Explorer mở nhanh vượt trội và luôn gọn gàng.";
        _translations["• Easily enable or disable third-party context menu entries.\n• Removes legacy or broken shell extension links.\n• Prevents Explorer freeze when right-clicking files or folders."] = "• Bật/Tắt nhanh các mục chuột phải của phần mềm bên thứ 3.\n• Loại bỏ các liên kết shell extension bị hỏng hoặc dư thừa.\n• Tránh hiện tượng đơ lag Windows Explorer khi nhấn chuột phải.";
        _translations["1. Navigate to [Context Menu] from the sidebar.\n2. Review registered shell menu items.\n3. Toggle off items you do not use to speed up File Explorer."] = "1. Mở trang [Context Menu] từ thanh điều hướng.\n2. Xem danh sách các lệnh chuột phải đang hoạt động.\n3. Tắt các mục không sử dụng để menu chuột phải mở tức thì.";

        // --- 10. Startup & Services Manager ---
        _translations["10. Startup & Services Manager"] = "10. Quản Lý Khởi Động & Dịch Vụ Windows";
        _translations["Controls software configured to start automatically with Windows and manages background system services to deliver rapid boot times."] = "Kiểm soát tất cả phần mềm tự động bật cùng Windows và quản lý trạng thái các dịch vụ nền (Services) giúp máy tính khởi động nhanh vượt trội.";
        _translations["• Analyzes startup impact on boot performance (High/Medium/Low Impact).\n• Toggle enable/disable status for background applications.\n• Built-in Safety Guard protects core Microsoft system services from accidental disabling."] = "• Đánh giá mức độ ảnh hưởng của ứng dụng đến thời gian boot (High/Medium/Low Impact).\n• Tắt/Bật nhanh các phần mềm chạy ngầm không cần thiết.\n• Hệ thống Safety Guard bảo vệ 14 dịch vụ cốt lõi của Microsoft không bị vô hiệu hóa nhầm.";
        _translations["1. Open the [Startup & Services] module.\n2. Review auto-starting items and disable unnecessary High Impact entries.\n3. Click [Boost Boot Speed] to let AI optimize boot sequence."] = "1. Mở trang [Startup & Services].\n2. Xem danh sách ứng dụng khởi động và tắt những app có mức ảnh hưởng High Impact.\n3. Nhấn [Boost Boot Speed] để hệ thống tự động tối ưu danh sách khởi động.";

        // --- 11. Disk Tools & Storage Sustainability ---
        _translations["11. Disk Tools & Storage Sustainability"] = "11. Công Cụ Đĩa & Dự Đoán Dung Lượng Bền Vững";
        _translations["Analyzes disk space consumption, predicts storage sustainability timelines, and provides drive optimization tools."] = "Phân tích chi tiết dung lượng đĩa cứng, dự đoán thời gian bền vững lưu trữ của ổ C: và cung cấp bộ công cụ tối ưu hóa ổ đĩa.";
        _translations["• Disk space analyzer with folder size visualization.\n• AI-driven Storage Sustainability timeline prediction.\n• TRIM optimization for SSDs and defragmentation trigger for HDDs."] = "• Trực quan hóa dung lượng thư mục và tệp lớn chiếm đĩa.\n• Dự đoán số ngày bền vững bộ nhớ còn lại bằng thuật toán AI.\n• Lệnh tối ưu TRIM cho ổ SSD và chống phân mảnh cho ổ HDD.";
        _translations["1. Select [Disk Tools] from menu.\n2. View storage breakdown and sustainability report.\n3. Run TRIM or Clean Drive to reclaim space."] = "1. Chọn phân hệ [Disk Tools].\n2. Xem báo cáo phân tích dung lượng và dự đoán số ngày còn lại.\n3. Nhấn chạy TRIM hoặc Dọn đĩa để tối ưu hiệu năng đĩa.";

        // --- 12. Registry Center & Registry Cleaner ---
        _translations["12. Registry Center & Registry Cleaner"] = "12. Trung Tâm Dọn Dẹp & Khôi Phục Registry";
        _translations["Scans for invalid registry keys, orphaned file extensions, and broken installation entries while backing up HKLM and HKCU partitions for absolute safety."] = "Quét tìm các khóa Registry bị lỗi, liên kết tệp hỏng, đường dẫn ứng dụng cũ và tự động sao lưu phân vùng Registry an toàn tuyệt đối.";
        _translations["• Deep Registry error scanner targeting file associations & CLSIDs.\n• Automatic 1-Click Registry Backup before cleaning operations.\n• Instant roll-back restore for created registry backups."] = "• Quét sâu lỗi Registry liên quan đến liên kết tệp & CLSID.\n• Tự động Sao lưu Registry 1-Click trước khi tiến hành dọn dẹp.\n• Khôi phục tức thì về trạng thái cũ nếu gặp sự cố.";
        _translations["1. Open [Registry Center].\n2. Click [Scan Registry] to detect invalid registry keys.\n3. Click [Clean Selected] (automatic backup will be created)."] = "1. Truy cập mục [Registry Center].\n2. Nhấn nút [Scan Registry] để tìm các khóa rác bị hỏng.\n3. Nhấn [Clean Selected] (hệ thống tự động sao lưu trước khi dọn).";

        // --- 13. Software Updater & Package Manager ---
        _translations["13. Software Updater & Package Manager"] = "13. Quản Lý & Cập Nhật Phần Mềm";
        _translations["Scans installed third-party applications for available security patches and software updates, enabling silent 1-click batch updates."] = "Quét danh sách ứng dụng đã cài đặt trên máy, phát hiện phiên bản mới và hỗ trợ cập nhật hàng loạt 1-Click nhanh chóng.";
        _translations["• Automatic background checks for outdated software packages.\n• Silent installer execution without manual wizard clicking.\n• Direct integration with official winget package repositories."] = "• Tự động phát hiện ứng dụng lỗi thời trong nền.\n• Cài đặt cập nhật chạy ngầm không cần nhấn Next thủ công.\n• Tích hợp kho phần mềm winget chính thức từ Microsoft.";
        _translations["1. Select [Software Updater].\n2. Click [Check for Updates] to scan installed apps.\n3. Click [Update All] to upgrade software automatically."] = "1. Mở mục [Software Updater].\n2. Nhấn [Check for Updates] để kiểm tra phần mềm cũ.\n3. Nhấn [Update All] để tự động cập nhật lên bản mới nhất.";

        // --- 14. Desktop Mini Widget & System Tray Bar ---
        _translations["14. Desktop Mini Widget & System Tray Bar"] = "14. Tiện Ích Desktop Widget & Thanh Khay Hệ Thống";
        _translations["Offers a compact floating desktop toolbar displaying live CPU/RAM usage meters, quick boost trigger, and system tray notification controls."] = "Cung cấp thanh công cụ mini nổi trên Desktop hiển thị thông số CPU/RAM thời gian thực, nút Quick Boost nhanh và icon khay hệ thống (System Tray).";
        _translations["• Compact floating widget with customizable opacity.\n• 1-Click Quick RAM boost directly from desktop without opening main window.\n• Minimize to System Tray support for low resource overhead."] = "• Thanh tiện ích nổi gọn nhẹ, tùy chỉnh độ trong suốt.\n• Giải phóng RAM 1-Click trực tiếp từ màn hình chính Desktop.\n• Chế độ ẩn xuống khay hệ thống tiết kiệm tài nguyên.";
        _translations["1. Enable [Desktop Widget] from Settings or System Tray menu.\n2. Position widget anywhere on screen.\n3. Click the rocket icon on widget to flush RAM anytime."] = "1. Bật [Desktop Widget] từ Settings hoặc menu chuột phải khay hệ thống.\n2. Đặt thanh tiện ích ở vị trí mong muốn trên màn hình.\n3. Nhấn icon tên lửa trên widget để tăng tốc RAM bất kỳ lúc nào.";
    }
}

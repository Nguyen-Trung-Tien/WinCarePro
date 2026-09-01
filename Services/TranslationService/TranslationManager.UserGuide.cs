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
        // --- General Guide Section Headers, Badges & Search ---
        _translations["Feature Guide & Manual"] = "Hướng dẫn sử dụng";
        _translations["Feature Guide & System Manual"] = "Hướng Dẫn Tính Năng & Cẩm Nang Hệ Thống";
        _translations["Comprehensive documentation, detailed feature capabilities, step-by-step guides & safety guidelines."] = "Tài liệu chi tiết mô tả mục đích, tính năng nổi bật, hướng dẫn thao tác từng bước và quy tắc an toàn.";
        _translations["Comprehensive documentation explaining the purpose, detailed features, step-by-step usage, and safety guidelines for each WinCare Pro module."] = "Tài liệu toàn diện giải thích mục đích, tính năng chi tiết, hướng dẫn từng bước và quy tắc an toàn cho từng phân hệ WinCare Pro.";
        _translations["100% Safe Guidelines"] = "Quy Tắc 100% An Toàn";
        _translations["14 Core Modules"] = "14 Phân Hệ Cốt Lõi";
        _translations["15 Core Modules"] = "15 Phân Hệ Cốt Lõi";
        _translations["Search guide (e.g., RAM, DNS, SFC, junk)..."] = "Tìm kiếm hướng dẫn (ví dụ: RAM, DNS, SFC, rác)...";
        _translations["Search guide (e.g., RAM, DNS, SFC, game, junk)..."] = "Tìm kiếm hướng dẫn (ví dụ: RAM, DNS, SFC, game, rác)...";
        _translations["Comprehensive documentation, step-by-step feature guides, and best practices."] = "Tài liệu toàn diện, hướng dẫn tính năng từng bước và các chuẩn mực thực hành tốt nhất.";
        _translations["All Modules"] = "Tất Cả";
        _translations["Care"] = "Chăm Sóc";
        _translations["Tuning"] = "Tối Ưu";
        _translations["Security"] = "Bảo Mật";
        _translations["System"] = "Hệ Thống";
        _translations["Care & Clean"] = "Chăm Sóc";
        _translations["Tuning & Gaming"] = "Tối Ưu";
        _translations["Security & Repair"] = "Bảo Mật";
        _translations["System & Tools"] = "Hệ Thống";

        _translations["No matching modules found"] = "Không tìm thấy phân hệ phù hợp";
        _translations["Try searching for another keyword like 'clean', 'speed', 'ram', 'game', 'restore', or select 'All Modules'."] = "Hãy thử tìm kiếm với từ khóa khác như 'clean', 'ram', 'speed', 'game', 'sửa lỗi', hoặc chọn 'Tất cả phân hệ'.";
        _translations["Clear Search & Show All"] = "Xóa tìm kiếm & Hiện tất cả";

        _translations["Purpose & Overview"] = "Mục đích & Nguyên lý";
        _translations["Key Capabilities"] = "Tính năng nổi bật";
        _translations["Step-by-Step Guide"] = "Hướng dẫn sử dụng từng bước";
        _translations["Safety Rating"] = "Mức độ an toàn";
        _translations["100% Safe (No System Impact)"] = "100% An toàn (Không rủi ro hệ thống)";
        _translations["Safe (Recommended)"] = "An toàn (Khuyên dùng)";
        _translations["Requires Admin Privileges"] = "Yêu cầu Quyền QTV (Administrator)";
        _translations["Requires Administrator Privileges"] = "Yêu cầu Quyền QTV (Administrator)";
        _translations["Launch Module"] = "Mở Tính Năng";
        _translations["Toggle HUD Widget"] = "Bật/Tắt HUD Widget";

        // --- 1. Dashboard & System Health Monitor ---
        _translations["1. Dashboard & System Health Monitor"] = "1. Tổng Quan & Chẩn Đoán Sức Khỏe Hệ Thống";
        _translations["Live Telemetry • Quick RAM Boost • Kernel Diagnostics"] = "Đo lường trực tiếp • Tăng tốc RAM nhanh • Chẩn đoán Kernel";
        _translations["Provides real-time system monitoring for CPU, RAM, GPU, and Disk utilization, along with continuous Health Score tracking and one-click Quick Boost optimization."] = "Cung cấp giao diện giám sát hiệu năng CPU, RAM, GPU, Ổ đĩa theo thời gian thực, đo điểm số sức khỏe hệ thống và nút Quick Boost tối ưu nhanh 1-Click.";
        _translations["• Real-time animated hardware telemetry meters.\n• Instant RAM & CPU Working Set flushing via Quick Boost.\n• Dynamic system health status alert indicators."] = "• Đồng hồ đo thông số phần cứng động theo thời gian thực.\n• Nút Quick Boost giải phóng RAM và làm sạch bộ nhớ làm việc CPU tức thì.\n• Cảnh báo trạng thái sức khỏe hệ thống theo mã màu trực quan.";
        _translations["• Real-time animated hardware telemetry meters.&#x0a;• Instant RAM & CPU Working Set flushing via Quick Boost.&#x0a;• Dynamic system health status alert indicators."] = "• Đồng hồ đo thông số phần cứng động theo thời gian thực.\n• Nút Quick Boost giải phóng RAM và làm sạch bộ nhớ làm việc CPU tức thì.\n• Cảnh báo trạng thái sức khỏe hệ thống theo mã màu trực quan.";
        _translations["1. Open Dashboard from the top of the left navigation pane."] = "1. Mở trang Tổng quan từ đầu thanh điều hướng bên trái.";
        _translations["2. Observe hardware loads and current Health Score."] = "2. Theo dõi mức sử dụng phần cứng và Điểm số Sức khỏe hiện tại.";
        _translations["3. Click [Quick Boost] to instantly reclaim RAM and optimize system processes."] = "3. Nhấn nút [Quick Boost] để giải phóng RAM và tối ưu tiến trình ngay lập tức.";

        // --- 2. AI Assistant & Diagnostics ---
        _translations["2. AI Assistant & Diagnostics"] = "2. Trợ Lý WinCare AI & Chẩn Đoán Hệ Thống";
        _translations["Intelligent Health Engine • Predictive Storage • Kernel Analysis"] = "Động cơ chẩn đoán AI • Dự đoán dung lượng • Phân tích Kernel";
        _translations["Leverages intelligent AI algorithms to comprehensively scan Windows, analyze health indicators, detect accumulated junk, bottlenecks, and security risks."] = "Sử dụng thuật toán AI thông minh để quét toàn diện Windows, phân tích chỉ số sức khỏe, phát hiện tệp rác tích tụ, điểm nghẽn và nguy cơ bảo mật.";
        _translations["Leverages intelligent AI algorithms to comprehensively scan the Windows system, analyze real-time health indicators, detect accumulated junk files, RAM bottlenecks, slowing background services, and potential security risks."] = "Sử dụng thuật toán AI thông minh để quét toàn diện hệ thống Windows, phân tích các chỉ số sức khỏe, phát hiện tệp rác tích tụ, xung đột bộ nhớ RAM, dịch vụ gây chậm máy và nguy cơ tiềm ẩn.";
        _translations["• Real-time Health Score calculation based on system telemetry.\n• Predictive analysis for C: drive storage and boot speed trends.\n• One-click automated recommendations for system optimizations."] = "• Tính toán Điểm số Sức khỏe (Health Score) theo thời gian thực.\n• Dự đoán dung lượng ổ C: và tốc độ boot máy trong tương lai.\n• Tự động đưa ra danh sách đề xuất khắc phục tối ưu 1-Click.";
        _translations["• Real-time Health Score calculation based on system telemetry.&#x0a;• Predictive analysis for C: drive storage and boot speed trends.&#x0a;• One-click automated recommendations for system optimizations."] = "• Tính toán Điểm số Sức khỏe (Health Score) theo thời gian thực.\n• Dự đoán dung lượng ổ C: và tốc độ boot máy trong tương lai.\n• Tự động đưa ra danh sách đề xuất khắc phục tối ưu 1-Click.";
        _translations["1. Navigate to the AI Assistant module from the main sidebar."] = "1. Mở phân hệ Trợ lý AI từ menu chính bên trái.";
        _translations["2. Click [Run AI Diagnostics] to start deep system analysis."] = "2. Nhấn nút [Run AI Diagnostics] để bắt đầu phân tích hệ thống.";
        _translations["3. Review findings and click [Apply Recommended Tweaks] to automatically optimize."] = "3. Xem kết quả chẩn đoán và nhấn [Apply Recommended Tweaks] để tự động tối ưu hóa.";

        // --- 3. Junk Cleaner & Debris ---
        _translations["3. Junk Cleaner & System Debris"] = "3. Dọn Dẹp Tệp Rác & Bộ Nhớ Tạm Hệ Thống";
        _translations["3. Junk Cleaner & Debris"] = "3. Dọn Dẹp Tệp Rác & Tệp Tạm System";
        _translations["Temp Cache • Browser Cache • Windows Update Debris • Memory Dumps"] = "Bộ nhớ tạm • Cache trình duyệt • Rác Windows Update • Bản ghi bộ nhớ";
        _translations["Deeply scans storage drives to locate and wipe redundant junk files created by Windows OS, web browsers, and third-party applications during daily usage."] = "Quét sâu toàn bộ ổ đĩa để tìm và xóa sạch các tệp rác thừa do hệ điều hành Windows, trình duyệt web và ứng dụng tạo ra trong quá trình sử dụng.";
        _translations["Deeply scans all storage drives to locate and wipe redundant junk files created by Windows OS, web browsers, and third-party applications during daily usage."] = "Quét sâu toàn bộ ổ đĩa để tìm và xóa sạch các tệp rác thừa do hệ điều hành Windows, trình duyệt web và ứng dụng tạo ra trong quá trình sử dụng.";
        _translations["• Cleans Windows temporary files (%TEMP%, Prefetch, Log files).\n• Wipes browser caches (Chrome, Edge, Firefox, Brave).\n• Clears Windows Update download cache and installer artifacts."] = "• Làm sạch bộ nhớ tạm Windows (%TEMP%, Prefetch, tệp Log).\n• Dọn dẹp cache trình duyệt (Chrome, Edge, Firefox, Brave).\n• Quét bộ đệm tải về Windows Update và tệp cài đặt tạm.";
        _translations["• Cleans Windows temporary files (%TEMP%, Prefetch, Log files).&#x0a;• Wipes browser caches (Chrome, Edge, Firefox, Brave).&#x0a;• Clears Windows Update download cache and installer artifacts."] = "• Làm sạch bộ nhớ tạm Windows (%TEMP%, Prefetch, tệp Log).\n• Dọn dẹp cache trình duyệt (Chrome, Edge, Firefox, Brave).\n• Quét bộ đệm tải về Windows Update và tệp cài đặt tạm.";
        _translations["1. Select [Junk Cleaner] on the main navigation panel."] = "1. Chọn mục [Junk Cleaner] trên thanh điều hướng chính.";
        _translations["2. Click [Scan Directories] to scan for removable system clutter."] = "2. Nhấn nút [Scan Directories] để quét tìm tệp rác có thể xóa.";
        _translations["3. Check target categories and click [Clean Now] to free up disk space."] = "3. Đánh dấu các danh mục muốn dọn và nhấn [Clean Now] để giải phóng ổ đĩa.";

        // --- 4. App Uninstaller & Leftovers ---
        _translations["4. App Uninstaller & Leftovers Purge"] = "4. Gỡ Ứng Dụng & Dọn Tàn Dư Triệt Để";
        _translations["4. App Uninstaller & Leftovers"] = "4. Gỡ Ứng Dụng & Dọn Tàn Dư Triệt Để";
        _translations["Batch Uninstall • Leftover File & Registry Sweeper • Bloatware Removal"] = "Gỡ hàng loạt • Quét tàn dư Registry & Thư mục • Gỡ Bloatware";
        _translations["Uninstalls installed applications and default Windows Bloatware, followed by deep residual registry and directory cleanup."] = "Gỡ bỏ phần mềm đã cài đặt và ứng dụng mặc định Windows (Bloatware), sau đó tự động truy quét dọn sạch các thư mục rác và Registry Key thừa còn sót lại.";
        _translations["• Batch uninstall multiple applications simultaneously.\n• Deep leftover scanner purges AppData, ProgramData, and Registry traces.\n• Complete removal of unused Windows Store packages."] = "• Gỡ bỏ hàng loạt ứng dụng cùng lúc.\n• Quét dọn tàn dư tận gốc trong AppData, ProgramData và Registry.\n• Gỡ bỏ triệt để các ứng dụng Windows Store mặc định không dùng đến.";
        _translations["• Batch uninstall multiple applications simultaneously.&#x0a;• Deep leftover scanner purges AppData, ProgramData, and Registry traces.&#x0a;• Complete removal of unused Windows Store packages."] = "• Gỡ bỏ hàng loạt ứng dụng cùng lúc.\n• Quét dọn tàn dư tận gốc trong AppData, ProgramData và Registry.\n• Gỡ bỏ triệt để các ứng dụng Windows Store mặc định không dùng đến.";
        _translations["1. Launch [App Uninstaller] module."] = "1. Mở phân hệ [App Uninstaller].";
        _translations["2. Select software applications to remove."] = "2. Tìm và chọn ứng dụng muốn gỡ bỏ.";
        _translations["3. Click [Uninstall] and choose [Wipe Leftovers] to clean all residual data."] = "3. Nhấn [Uninstall] và chọn [Wipe Leftovers] để dọn sạch hoàn toàn tàn dư.";

        // --- 5. Network Center ---
        _translations["5. Network Center & Speed Diagnostics"] = "5. Trung Tâm Tối Ưu Mạng & Đo Tốc Độ";
        _translations["5. Network Center"] = "5. Trung Tâm Tối Ưu Mạng & Kết Nối";
        _translations["Bandwidth Speed Test • Flush DNS Cache • TCP Auto-Tuning"] = "Đo tốc độ mạng • Xóa bộ nhớ đệm DNS • Tinh chỉnh TCP Auto-Tuning";
        _translations["Diagnoses internet connection health, tests bandwidth speeds, and resolves network connectivity issues or latency spikes."] = "Chẩn đoán chất lượng kết nối Internet, kiểm tra tốc độ băng thông và khắc phục các sự cố giật lag hoặc mất kết nối mạng.";
        _translations["• Precision Speed Test for Download, Upload, and Latency/Ping metrics.\n• One-click Flush DNS Resolver Cache.\n• TCP Auto-Tuning optimization and Secure DoH (DNS over HTTPS) toggles."] = "• Speedtest đo tốc độ Download, Upload và Ping chuẩn xác.\n• Xóa bộ nhớ đệm DNS (Flush DNS Resolver Cache) tức thì.\n• Tinh chỉnh cấu hình TCP Auto-Tuning và DoH (DNS over HTTPS) bảo mật.";
        _translations["• Precision Speed Test for Download, Upload, and Latency/Ping metrics.&#x0a;• One-click Flush DNS Resolver Cache.&#x0a;• TCP Auto-Tuning optimization and Secure DoH (DNS over HTTPS) toggles."] = "• Speedtest đo tốc độ Download, Upload và Ping chuẩn xác.\n• Xóa bộ nhớ đệm DNS (Flush DNS Resolver Cache) tức thì.\n• Tinh chỉnh cấu hình TCP Auto-Tuning và DoH (DNS over HTTPS) bảo mật.";
        _translations["1. Open [Network Center]."] = "1. Mở trang [Network Center].";
        _translations["2. Click [Run Speed Test] to analyze latency and bandwidth."] = "2. Nhấn [Run Speed Test] để phân tích độ trễ và băng thông mạng.";
        _translations["3. Click [Flush DNS Cache] or [Reset TCP/IP Stack] to resolve network issues."] = "3. Nhấn [Flush DNS Cache] hoặc [Reset TCP/IP Stack] để sửa sự cố mạng.";

        // --- 6. System Repair Center ---
        _translations["6. System Repair Center"] = "6. Trung Tâm Khắc Phục & Sửa Lỗi Windows";
        _translations["SFC Integrity Check • DISM Health Restore • Windows Update Auto-Fix"] = "Kiểm tra toàn vẹn SFC • Khôi phục DISM • Tự động sửa lỗi Windows Update";
        _translations["Integrates advanced Windows diagnostic and repair utilities to automatically repair corrupt system files, component store corruption, and Windows Update errors."] = "Tích hợp bộ công cụ chẩn đoán và khắc phục sự cố hệ điều hành chuyên sâu giúp tự động sửa chữa các tệp hệ thống bị hỏng và lỗi Windows Update.";
        _translations["• SFC Repair (sfc /scannow): Scans and restores corrupted Windows binaries.\n• DISM Component Restore: Repairs damaged Component Store packages.\n• Windows Update Repair: Resets update cache and stuck services."] = "• Quét SFC (sfc /scannow): Kiểm tra và phục hồi tệp hệ thống Windows bị hỏng.\n• Khôi phục DISM: Sửa chữa kho linh kiện Windows Component Store.\n• Sửa Windows Update: Reset bộ nhớ đệm và các dịch vụ cập nhật bị kẹt.";
        _translations["• SFC Repair (sfc /scannow): Scans and restores corrupted Windows binaries.&#x0a;• DISM Component Restore: Repairs damaged Component Store packages.&#x0a;• Windows Update Repair: Resets update cache and stuck services."] = "• Quét SFC (sfc /scannow): Kiểm tra và phục hồi tệp hệ thống Windows bị hỏng.\n• Khôi phục DISM: Sửa chữa kho linh kiện Windows Component Store.\n• Sửa Windows Update: Reset bộ nhớ đệm và các dịch vụ cập nhật bị kẹt.";
        _translations["1. Go to [System Repair] section."] = "1. Truy cập mục [System Repair].";
        _translations["2. Select [SFC Repair] or [DISM Restore] based on system issues."] = "2. Chọn nút [SFC Repair] hoặc [DISM Restore] tùy theo lỗi hệ thống gặp phải.";
        _translations["3. Allow process to complete and restart Windows if prompted."] = "3. Chờ quá trình sửa lỗi hoàn tất và khởi động lại máy nếu được yêu cầu.";

        // --- 7. Security Shield & Privacy ---
        _translations["7. Security Shield & Privacy Hardening"] = "7. Bảo Mật & Tăng Cường Quyền Riêng Tư";
        _translations["7. Security Shield & Privacy"] = "7. Bảo Mật & Bảo Vệ Quyền Riêng Tư";
        _translations["Clipboard Sanitizer • Recent Files Purge • Telemetry Shield"] = "Làm sạch Clipboard • Xóa lịch sử tệp gần đây • Chống thu thập Telemetry";
        _translations["Protects personal privacy by purging activity histories, temporary clipboard data, and disabling Windows telemetry tracking."] = "Bảo vệ riêng tư cá nhân bằng cách xóa sạch lịch sử hoạt động, vết sử dụng ứng dụng và vô hiệu hóa các trình theo dõi Telemetry của Windows.";
        _translations["• Wipes sensitive data stored in clipboard cache (Wipe Clipboard).\n• Clears Recent Files and application launch history.\n• Disables telemetry and diagnostic tracking services."] = "• Xóa dữ liệu nhạy cảm lưu trong bộ nhớ tạm Clipboard (Wipe Clipboard).\n• Dọn dẹp lịch sử mở tệp gần đây và nhật ký khởi chạy ứng dụng.\n• Tắt các dịch vụ thu thập dữ liệu Telemetry & Diagnostic tracking.";
        _translations["• Wipes sensitive data stored in clipboard cache (Wipe Clipboard).&#x0a;• Clears Recent Files and application launch history.&#x0a;• Disables telemetry and diagnostic tracking services."] = "• Xóa dữ liệu nhạy cảm lưu trong bộ nhớ tạm Clipboard (Wipe Clipboard).\n• Dọn dẹp lịch sử mở tệp gần đây và nhật ký khởi chạy ứng dụng.\n• Tắt các dịch vụ thu thập dữ liệu Telemetry & Diagnostic tracking.";
        _translations["1. Open [Security Shield] section."] = "1. Mở trang [Security Shield].";
        _translations["2. Click [Wipe Clipboard Cache] to secure clipboard contents."] = "2. Nhấn [Wipe Clipboard Cache] để bảo vệ dữ liệu bộ nhớ tạm.";
        _translations["3. Click [Clear Recent Files] to remove personal activity traces."] = "3. Nhấn [Clear Recent Files] để xóa dấu vết hoạt động cá nhân.";

        // --- 8. System Optimizer & Gaming Turbo ---
        _translations["8. System Optimizer & Gaming Turbo 2.0"] = "8. Tối Ưu Hệ Thống & Tăng Tốc Gaming Turbo 2.0";
        _translations["8. System Optimizer & RAM Booster"] = "8. Tối Ưu Hệ Thống & Tăng Tốc RAM";
        _translations["Hyper-Turbo Gaming Engine • Latency Minimizer • Registry Acceleration"] = "Động cơ Gaming Turbo • Giảm độ trễ Latency • Tăng tốc Registry";
        _translations["Applies Microsoft-compliant Registry tweaks to optimize CPU scheduling, disable unnecessary services, and reclaim active physical RAM memory."] = "Áp dụng các tinh chỉnh Registry chuẩn Microsoft nhằm tối ưu hóa lập lịch CPU, vô hiệu hóa dịch vụ không cần thiết và giải phóng bộ nhớ RAM vật lý.";
        _translations["• Gaming Turbo 2.0 Mode: Flushes process working sets and prioritizes CPU.\n• Windows Registry latency tweaks for ultra-responsive performance.\n• Automatic creation of System Restore Points before applying tweaks."] = "• Chế độ Gaming Turbo 2.0: Ép dọn RAM (Working Set flushing) và ưu tiên CPU cho Game.\n• Tinh chỉnh Windows Registry giảm độ trễ tối đa cho phản hồi siêu tốc.\n• Tự động tạo điểm khôi phục System Restore Point trước khi tối ưu.";
        _translations["• Gaming Turbo 2.0 Mode: Flushes process working sets and prioritizes CPU.&#x0a;• Windows Registry latency tweaks for ultra-responsive performance.&#x0a;• Automatic creation of System Restore Points before applying tweaks."] = "• Chế độ Gaming Turbo 2.0: Ép dọn RAM (Working Set flushing) và ưu tiên CPU cho Game.\n• Tinh chỉnh Windows Registry giảm độ trễ tối đa cho phản hồi siêu tốc.\n• Tự động tạo điểm khôi phục System Restore Point trước khi tối ưu.";
        _translations["1. Open the [System Optimizer] or [Gaming Turbo] page."] = "1. Mở trang [System Optimizer] hoặc phân hệ [Gaming Turbo].";
        _translations["1. Open the [System Optimizer] page."] = "1. Mở trang [System Optimizer].";
        _translations["2. Toggle [Enable Gaming Turbo] for instant gaming acceleration."] = "2. Bật [Gaming Turbo] để tăng tốc tức thì cho trải nghiệm chơi game mượt mà.";
        _translations["3. Select desired system tweaks and click [Apply Tweaks]."] = "3. Tích chọn các tinh chỉnh hệ thống mong muốn và nhấn [Apply Tweaks].";

        // --- 9. Context Menu Manager ---
        _translations["9. Context Menu Manager"] = "9. Quản Lý Menu Ngữ Cảnh Chuột Phải";
        _translations["Explorer Shell Extension Cleaner • Right-Click Menu Speedup"] = "Dọn dẹp tiện ích vỏ Explorer • Tăng tốc mở Menu chuột phải";
        _translations["Manages shortcuts and background shell extensions displayed in the Windows Explorer right-click Context Menu for cleaner, faster File Explorer operation."] = "Quản lý các lệnh và extension hiển thị trong Menu chuột phải (Context Menu) giúp File Explorer mở nhanh vượt trội và luôn gọn gàng.";
        _translations["• Easily enable or disable third-party context menu entries.\n• Removes legacy or broken shell extension links.\n• Prevents Explorer freeze when right-clicking files."] = "• Bật/Tắt nhanh các mục chuột phải của phần mềm bên thứ 3.\n• Loại bỏ các liên kết shell extension bị hỏng hoặc dư thừa.\n• Tránh hiện tượng đơ lag File Explorer khi nhấn chuột phải.";
        _translations["• Easily enable or disable third-party context menu entries.&#x0a;• Removes legacy or broken shell extension links.&#x0a;• Prevents Explorer freeze when right-clicking files."] = "• Bật/Tắt nhanh các mục chuột phải của phần mềm bên thứ 3.\n• Loại bỏ các liên kết shell extension bị hỏng hoặc dư thừa.\n• Tránh hiện tượng đơ lag File Explorer khi nhấn chuột phải.";
        _translations["1. Navigate to [Context Menu] from the sidebar."] = "1. Mở trang [Context Menu] từ thanh điều hướng bên trái.";
        _translations["2. Review registered shell menu items."] = "2. Xem danh sách các lệnh chuột phải đang hoạt động.";
        _translations["3. Toggle off items you do not use to speed up File Explorer."] = "3. Tắt các mục không sử dụng để menu chuột phải mở tức thì.";

        // --- 10. Startup & Services Manager ---
        _translations["10. Startup & Services Manager"] = "10. Quản Lý Khởi Động & Dịch Vụ Windows";
        _translations["Boot Acceleration • Startup Impact Analyzer • Safe Service Control"] = "Tăng tốc khởi động • Đánh giá ảnh hưởng Boot • Kiểm soát Dịch vụ an toàn";
        _translations["Controls software configured to start automatically with Windows and manages background system services to deliver rapid boot times."] = "Kiểm soát tất cả phần mềm tự động bật cùng Windows và quản lý trạng thái các dịch vụ nền (Services) giúp máy tính khởi động nhanh vượt trội.";
        _translations["• Analyzes startup impact on boot performance (High/Medium/Low Impact).\n• Toggle enable/disable status for background applications.\n• Built-in Safety Guard protects core Microsoft system services."] = "• Đánh giá mức độ ảnh hưởng của ứng dụng đến thời gian boot (Cao/Vừa/Thấp).\n• Bật/Tắt nhanh các phần mềm khởi động cùng hệ thống.\n• Hệ thống Safety Guard bảo vệ các dịch vụ cốt lõi của Microsoft không bị tắt nhầm.";
        _translations["• Analyzes startup impact on boot performance (High/Medium/Low Impact).&#x0a;• Toggle enable/disable status for background applications.&#x0a;• Built-in Safety Guard protects core Microsoft system services."] = "• Đánh giá mức độ ảnh hưởng của ứng dụng đến thời gian boot (Cao/Vừa/Thấp).\n• Bật/Tắt nhanh các phần mềm khởi động cùng hệ thống.\n• Hệ thống Safety Guard bảo vệ các dịch vụ cốt lõi của Microsoft không bị tắt nhầm.";
        _translations["1. Open the [Startup & Services] module."] = "1. Mở trang [Startup & Services].";
        _translations["2. Review auto-starting items and disable unnecessary High Impact entries."] = "2. Xem danh sách ứng dụng khởi động và tắt những app có mức ảnh hưởng High Impact.";
        _translations["3. Click [Boost Boot Speed] to let AI optimize boot sequence."] = "3. Nhấn [Boost Boot Speed] để hệ thống tự động tối ưu danh sách khởi động.";

        // --- 11. Disk Tools & Storage Sustainability ---
        _translations["11. Disk Tools & Storage Sustainability"] = "11. Công Cụ Đĩa & Dự Đoán Độ Bền Lưu Trữ";
        _translations["Space Analyzer • SSD TRIM Optimization • Predictive Life Metrics"] = "Phân tích dung lượng • Tối ưu TRIM cho SSD • Dự đoán tuổi thọ bền vững";
        _translations["Analyzes disk space consumption, predicts storage sustainability timelines, and provides drive optimization tools."] = "Phân tích chi tiết dung lượng đĩa cứng, dự đoán thời gian bền vững lưu trữ của ổ C: và cung cấp bộ công cụ tối ưu hóa ổ đĩa.";
        _translations["• Disk space analyzer with folder size visualization.\n• AI-driven Storage Sustainability timeline prediction.\n• TRIM optimization for SSDs and defragmentation trigger for HDDs."] = "• Trực quan hóa dung lượng thư mục và tệp lớn chiếm đĩa.\n• Dự đoán số ngày bền vững bộ nhớ còn lại bằng thuật toán AI.\n• Lệnh tối ưu TRIM cho ổ SSD và chống phân mảnh cho ổ HDD.";
        _translations["• Disk space analyzer with folder size visualization.&#x0a;• AI-driven Storage Sustainability timeline prediction.&#x0a;• TRIM optimization for SSDs and defragmentation trigger for HDDs."] = "• Trực quan hóa dung lượng thư mục và tệp lớn chiếm đĩa.\n• Dự đoán số ngày bền vững bộ nhớ còn lại bằng thuật toán AI.\n• Lệnh tối ưu TRIM cho ổ SSD và chống phân mảnh cho ổ HDD.";
        _translations["1. Select [Disk Tools] from menu."] = "1. Chọn phân hệ [Disk Tools] từ menu.";
        _translations["2. View storage breakdown and sustainability report."] = "2. Xem báo cáo phân tích dung lượng và dự đoán số ngày còn lại.";
        _translations["3. Run TRIM or Clean Drive to reclaim space."] = "3. Nhấn chạy TRIM hoặc Dọn đĩa để tối ưu hiệu năng đĩa.";

        // --- 12. Registry Center & Cleaner ---
        _translations["12. Registry Center & Cleaner"] = "12. Trung Tâm Dọn Dẹp & Khôi Phục Registry";
        _translations["12. Registry Center & Registry Cleaner"] = "12. Trung Tâm Dọn Dẹp & Khôi Phục Registry";
        _translations["Invalid Keys Scanner • 1-Click Rollback Backup • Orphaned Traces"] = "Quét khóa Registry hỏng • Sao lưu 1-Click an toàn • Dọn tàn dư mồ côi";
        _translations["Scans for invalid registry keys, orphaned file extensions, and broken installation entries while backing up HKLM and HKCU partitions for absolute safety."] = "Quét tìm các khóa Registry bị lỗi, liên kết tệp hỏng, đường dẫn ứng dụng cũ và tự động sao lưu phân vùng Registry an toàn tuyệt đối.";
        _translations["• Deep Registry error scanner targeting file associations & CLSIDs.\n• Automatic 1-Click Registry Backup before cleaning operations.\n• Instant roll-back restore for created registry backups."] = "• Quét sâu lỗi Registry liên quan đến liên kết tệp & CLSID.\n• Tự động Sao lưu Registry 1-Click trước khi tiến hành dọn dẹp.\n• Khôi phục tức thì về trạng thái cũ nếu gặp sự cố.";
        _translations["• Deep Registry error scanner targeting file associations & CLSIDs.&#x0a;• Automatic 1-Click Registry Backup before cleaning operations.&#x0a;• Instant roll-back restore for created registry backups."] = "• Quét sâu lỗi Registry liên quan đến liên kết tệp & CLSID.\n• Tự động Sao lưu Registry 1-Click trước khi tiến hành dọn dẹp.\n• Khôi phục tức thì về trạng thái cũ nếu gặp sự cố.";
        _translations["1. Open [Registry Center]."] = "1. Mở trang [Registry Center].";
        _translations["2. Click [Scan Registry] to detect invalid registry keys."] = "2. Nhấn nút [Scan Registry] để tìm các khóa rác bị hỏng.";
        _translations["3. Click [Clean Selected] (automatic backup will be created)."] = "3. Nhấn [Clean Selected] (hệ thống tự động sao lưu trước khi dọn).";

        // --- 13. Software Updater & Package Manager ---
        _translations["13. Software Updater & Package Manager"] = "13. Quản Lý & Cập Nhật Phần Mềm";
        _translations["Silent App Updates • Winget Integration • Security Patch Scanning"] = "Cập nhật chạy ngầm • Tích hợp Winget • Quét bản vá bảo mật";
        _translations["Scans installed third-party applications for available security patches and software updates, enabling silent 1-click batch updates."] = "Quét danh sách ứng dụng đã cài đặt trên máy, phát hiện phiên bản mới và hỗ trợ cập nhật hàng loạt 1-Click nhanh chóng.";
        _translations["• Automatic background checks for outdated software packages.\n• Silent installer execution without manual wizard clicking.\n• Direct integration with official winget package repositories."] = "• Tự động phát hiện ứng dụng lỗi thời trong nền.\n• Cài đặt cập nhật chạy ngầm không cần nhấn Next thủ công.\n• Tích hợp kho phần mềm winget chính thức từ Microsoft.";
        _translations["• Automatic background checks for outdated software packages.&#x0a;• Silent installer execution without manual wizard clicking.&#x0a;• Direct integration with official winget package repositories."] = "• Tự động phát hiện ứng dụng lỗi thời trong nền.\n• Cài đặt cập nhật chạy ngầm không cần nhấn Next thủ công.\n• Tích hợp kho phần mềm winget chính thức từ Microsoft.";
        _translations["1. Select [Software Updater]."] = "1. Mở mục [Software Updater].";
        _translations["2. Click [Check for Updates] to scan installed apps."] = "2. Nhấn [Check for Updates] để kiểm tra phần mềm cũ.";
        _translations["3. Click [Update All] to upgrade software automatically."] = "3. Nhấn [Update All] để tự động cập nhật lên bản mới nhất.";

        // --- 14. Desktop Mini Widget & System Tray Bar ---
        _translations["14. Desktop Mini Widget & System Tray Bar"] = "14. Tiện Ích Desktop Widget & Thanh Khay Hệ Thống";
        _translations["Floating HUD Overlay • 1-Click Desktop RAM Boost • Always-on-Top"] = "Thanh HUD nổi màn hình • Tăng tốc RAM 1-Click trên Desktop • Ghim trên cùng";
        _translations["Offers a compact floating desktop toolbar displaying live CPU/RAM usage meters, quick boost trigger, and system tray notification controls."] = "Cung cấp thanh công cụ mini nổi trên Desktop hiển thị thông số CPU/RAM thời gian thực, nút Quick Boost nhanh và icon khay hệ thống (System Tray).";
        _translations["• Compact floating widget with customizable opacity.\n• 1-Click Quick RAM boost directly from desktop without opening main window.\n• Minimize to System Tray support for low resource overhead."] = "• Thanh tiện ích nổi gọn nhẹ, tùy chỉnh độ trong suốt.\n• Giải phóng RAM 1-Click trực tiếp từ màn hình chính Desktop.\n• Chế độ ẩn xuống khay hệ thống tiết kiệm tài nguyên.";
        _translations["• Compact floating widget with customizable opacity.&#x0a;• 1-Click Quick RAM boost directly from desktop without opening main window.&#x0a;• Minimize to System Tray support for low resource overhead."] = "• Thanh tiện ích nổi gọn nhẹ, tùy chỉnh độ trong suốt.\n• Giải phóng RAM 1-Click trực tiếp từ màn hình chính Desktop.\n• Chế độ ẩn xuống khay hệ thống tiết kiệm tài nguyên.";
        _translations["1. Enable [Desktop Widget] from Settings or System Tray menu."] = "1. Bật [Desktop Widget] từ Settings hoặc menu chuột phải khay hệ thống.";
        _translations["2. Position widget anywhere on screen."] = "2. Đặt thanh tiện ích ở vị trí mong muốn trên màn hình.";
        _translations["3. Click the rocket icon on widget to flush RAM anytime."] = "3. Nhấn icon lò phản ứng trên widget để tăng tốc RAM bất kỳ lúc nào.";

        // --- 15. FAQ & Safe Operation Guidelines ---
        _translations["15. FAQ & Safe Operation Principles"] = "15. Hỏi Đáp & Nguyên Tắc Vận Hành An Toàn";
        _translations["System Restore Protection • Safe Deletion Standards • Reversible Optimization"] = "Bảo vệ điểm khôi phục • Tiêu chuẩn xóa an toàn • Tối ưu có thể hoàn tác";
        _translations["Zero Risk Guarantee"] = "Cam Kết Không Rủi Ro";
        _translations["WinCare Pro strictly adheres to official Microsoft APIs and Windows Service architectures. All operations create backups or restore points prior to execution."] = "WinCare Pro tuân thủ nghiêm ngặt các API chính thức của Microsoft và kiến trúc dịch vụ Windows. Mọi thao tác đều tự động tạo sao lưu hoặc điểm khôi phục trước khi thực thi.";
        _translations["Need Technical Support?"] = "Cần Hỗ Trợ Kỹ Thuật?";
        _translations["Visit the GitHub repository or submit an issue via Settings > About WinCare Pro for rapid technical guidance."] = "Ghé thăm kho mã nguồn GitHub hoặc gửi yêu cầu hỗ trợ qua Cài đặt > Giới thiệu WinCare Pro để được giải đáp nhanh chóng.";
    }
}

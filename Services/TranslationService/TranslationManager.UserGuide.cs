using System;
using System.Collections.Generic;

namespace WinCarePro.Services;

/// <summary>
/// Separate Partial Class containing all User Guide & Feature Manual Translations.
/// Modify this file to edit or add detailed guide descriptions for WinCare Pro modules.
/// </summary>
public partial class TranslationManager
{
    private void InitializeUserGuideTranslations()
    {
        // --- General Guide Section Headers ---
        _translations["Feature Guide & Manual"] = "Hướng Dẫn Sử Dụng & Mô Tả Chức Năng";
        _translations["Feature Guide & System Manual"] = "Hướng Dẫn Sử Dụng & Mô Tả Chức Năng Hệ Thống";
        _translations["Comprehensive documentation explaining the purpose, detailed features, step-by-step usage, and safety guidelines for each WinCare Pro module."] = "Tài liệu chi tiết mô tả mục đích, tính năng nổi bật, hướng dẫn thao tác từng bước và mức độ an toàn của từng phân hệ WinCare Pro.";

        _translations["Purpose & Overview"] = "Mục đích & Nguyên lý hoạt động";
        _translations["Key Capabilities"] = "Tính năng chi tiết";
        _translations["Step-by-Step Guide"] = "Hướng dẫn sử dụng từng bước";
        _translations["Safety Rating"] = "Mức độ an toàn";
        _translations["100% Safe (No System Impact)"] = "100% An toàn (Không rủi ro hệ thống)";
        _translations["Safe (Recommended)"] = "An toàn (Khuyên dùng)";
        _translations["Requires Administrator Privileges"] = "Yêu cầu Quyền QTV (Administrator)";

        // --- 1. AI Assistant & Diagnostics ---
        _translations["1. AI Assistant & Diagnostics"] = "1. Trợ Lý WinCare AI & Chẩn Đoán Hệ Thống";
        _translations["AI Assistant Overview Text"] = "Sử dụng thuật toán AI thông minh để quét toàn diện hệ thống Windows, phân tích các chỉ số sức khỏe, phát hiện tệp rác tích tụ, xung đột bộ nhớ RAM, dịch vụ gây chậm máy và nguy cơ tiềm ẩn.";
        _translations["AI Assistant Capabilities Text"] = "• Tính toán Điểm số Sức khỏe (Health Score) theo thời gian thực.\n• Dự đoán dung lượng ổ C: và tốc độ boot máy trong tương lai.\n• Tự động đưa ra danh sách đề xuất khắc phục tối ưu 1-Click.";
        _translations["AI Assistant Usage Text"] = "1. Mở phân hệ Trợ lý AI từ menu chính bên trái.\n2. Nhấn nút [Run AI Diagnostics] để bắt đầu phân tích.\n3. Xem kết quả chẩn đoán và nhấn [Apply Recommended Tweaks] để tự động tối ưu hóa.";

        // --- 2. Junk Cleaner & Debris ---
        _translations["2. Junk Cleaner & Debris"] = "2. Dọn Dẹp Tệp Rác & Tệp Tạm System";
        _translations["Junk Cleaner Overview Text"] = "Quét sâu toàn bộ ổ đĩa để tìm và xóa sạch các tệp rác thừa do hệ điều hành Windows và các phần mềm ứng dụng tạo ra trong quá trình sử dụng.";
        _translations["Junk Cleaner Capabilities Text"] = "• Làm sạch bộ nhớ tạm Windows (%TEMP%, C:\\Windows\\Temp, Prefetch, Log).\n• Dọn dẹp cache trình duyệt web (Google Chrome, Edge, Firefox, Brave).\n• Quét bộ đệm cập nhật Windows Update cache và Installer temp files.";
        _translations["Junk Cleaner Usage Text"] = "1. Chọn mục [Junk Cleaner] trên thanh điều hướng.\n2. Nhấn nút [Scan Directories] để tìm kiếm tệp rác.\n3. Đánh dấu các danh mục muốn dọn và nhấn [Clean Now].";

        // --- 3. System Optimizer & RAM Booster ---
        _translations["3. System Optimizer & RAM Booster"] = "3. Tối Ưu Hệ Thống & Tăng Tốc RAM";
        _translations["System Optimizer Overview Text"] = "Áp dụng các tinh chỉnh Registry chuẩn Microsoft nhằm tối ưu hóa lập lịch CPU, vô hiệu hóa dịch vụ không cần thiết và dọn dẹp vùng nhớ RAM vật lý.";
        _translations["System Optimizer Capabilities Text"] = "• Chế độ Gaming Turbo 2.0: Ép dọn bộ nhớ RAM (Working Set flushing) và ưu tiên CPU cho Game/Đồ họa.\n• Tinh chỉnh hệ thống Windows Registry tối ưu tốc độ phản hồi.\n• Tự động tạo điểm khôi phục System Restore Point trước khi tối ưu.";
        _translations["System Optimizer Usage Text"] = "1. Truy cập mục [System Optimizer].\n2. Nhấn [BẬT TURBO NGAY] để tăng tốc tức thì cho chơi game.\n3. Tích chọn các tinh chỉnh hệ thống mong muốn và nhấn [Apply Tweaks].";

        // --- 4. Startup & Services Manager ---
        _translations["4. Startup & Services Manager"] = "4. Quản Lý Khởi Động & Dịch Vụ Windows";
        _translations["Startup Manager Overview Text"] = "Kiểm soát tất cả phần mềm tự động bật cùng Windows và quản lý trạng thái các dịch vụ nền (Services) giúp máy tính khởi động nhanh vượt trội.";
        _translations["Startup Manager Capabilities Text"] = "• Đánh giá mức độ ảnh hưởng của ứng dụng đến thời gian boot (High/Medium/Low Impact).\n• Tắt/Bật nhanh các phần mềm chạy ngầm không cần thiết.\n• Hệ thống Safety Guard bảo vệ 14 dịch vụ cốt lõi của Microsoft không bị vô hiệu hóa nhầm.";
        _translations["Startup Manager Usage Text"] = "1. Mở trang [Startup & Services].\n2. Xem danh sách ứng dụng khởi động và tắt những app có mức ảnh hưởng High Impact.\n3. Nhấn [Boost Boot Speed] để hệ thống tự động tối ưu danh sách khởi động.";

        // --- 5. System Repair Center ---
        _translations["5. System Repair Center"] = "5. Trung Tâm Khắc Phục Sửa Lỗi Windows";
        _translations["System Repair Overview Text"] = "Tích hợp bộ công cụ chẩn đoán và khắc phục sự cố hệ điều hành chuyên sâu giúp tự động sửa chữa các tệp hệ thống bị hỏng và lỗi Windows Update.";
        _translations["System Repair Capabilities Text"] = "• SFC Scan (sfc /scannow): Kiểm tra và sửa tệp hệ thống Windows bị hỏng.\n• DISM Restore: Sửa chữa kho linh kiện linh hồn Windows (Component Store).\n• Repair Windows Update: Reset bộ nhớ đệm và dịch vụ cập nhật bị kẹt.\n• Network Repair: Reset Winsock Catalog và TCP/IP stack.";
        _translations["System Repair Usage Text"] = "1. Truy cập mục [System Repair].\n2. Chọn nút [SFC Repair] hoặc [DISM Restore] tùy theo lỗi hệ thống gặp phải.\n3. Chờ quá trình sửa lỗi hoàn tất và khởi động lại máy nếu được yêu cầu.";

        // --- 6. App Uninstaller & Leftovers ---
        _translations["6. App Uninstaller & Leftovers"] = "6. Gỡ Ứng Dụng & Dọn Tàn Dư Triệt Để";
        _translations["App Uninstaller Overview Text"] = "Gỡ bỏ phần mềm đã cài đặt và ứng dụng mặc định Windows (Bloatware), sau đó tự động truy quét dọn sạch các thư mục rác và Registry Key thừa còn sót lại.";
        _translations["App Uninstaller Capabilities Text"] = "• Gỡ bỏ hàng loạt ứng dụng cùng lúc.\n• Quét dọn tàn dư tận gốc trong AppData, ProgramData và Registry.\n• Gỡ bỏ triệt để các ứng dụng Windows Store mặc định không dùng đến.";
        _translations["App Uninstaller Usage Text"] = "1. Mở phân hệ [App Uninstaller].\n2. Tìm và chọn ứng dụng muốn gỡ bỏ.\n3. Nhấn [Uninstall] và chọn [Wipe Leftovers] để dọn sạch hoàn toàn tàn dư.";

        // --- 7. Network Center ---
        _translations["7. Network Center"] = "7. Trung Tâm Tối Ưu Mạng & Kết Nối";
        _translations["Network Center Overview Text"] = "Chẩn đoán chất lượng kết nối Internet, kiểm tra tốc độ băng thông và sửa các sự cố không vào được mạng hoặc giật lag khi chơi game online.";
        _translations["Network Center Capabilities Text"] = "• Speedtest đo tốc độ Download, Upload và Ping chuẩn xác.\n• Xóa bộ nhớ đệm DNS (Flush DNS Resolver cache).\n• Tinh chỉnh cấu hình TCP Auto-Tuning và DoH (DNS over HTTPS) bảo mật.";
        _translations["Network Center Usage Text"] = "1. Truy cập trang [Network Center].\n2. Nhấn [Run Speed Test] để kiểm tra chất lượng đường truyền.\n3. Nhấn [Flush DNS Cache] hoặc [Reset TCP/IP Stack] nếu gặp sự cố mạng.";

        // --- 8. Security Shield & Privacy ---
        _translations["8. Security Shield & Privacy"] = "8. Bảo Mật & Bảo Vệ Quyền Riêng Tư";
        _translations["Security Shield Overview Text"] = "Bảo vệ riêng tư cá nhân bằng cách xóa sạch lịch sử hoạt động, vết vết sử dụng ứng dụng và vô hiệu hóa các trình theo dõi Telemetry của Windows.";
        _translations["Security Shield Capabilities Text"] = "• Xóa dữ liệu nhạy cảm lưu trong bộ nhớ tạm Clipboard (Wipe Clipboard).\n• Dọn dẹp lịch sử mở tệp gần đây (Recent Files & Run History).\n• Tắt các tính năng thu thập dữ liệu Telemetry & Diagnostic tracking mặc định.";
        _translations["Security Shield Usage Text"] = "1. Mở trang [Security Shield].\n2. Nhấn [Wipe Clipboard Cache] để bảo vệ dữ liệu bộ nhớ tạm.\n3. Nhấn [Clear Recent Files] để xóa lịch sử hoạt động cá nhân.";

        // --- 9. Disk Tools & Context Menu ---
        _translations["9. Disk Tools & Context Menu"] = "9. Công Cụ Đĩa & Quản Lý Menu Ngữ Cảnh";
        _translations["Disk Tools Overview Text"] = "Tối ưu hóa đĩa cứng và quản lý danh sách các mục hiển thị trong Menu chuột phải (Context Menu) giúp Windows Explorer luôn gọn gàng và mở tức thì.";
        _translations["Disk Tools Capabilities Text"] = "• Bật/Tắt các lệnh hiển thị trong menu chuột phải Windows Explorer.\n• Quét dọn lỗi Registry và phân tích dung lượng ổ đĩa.\n• Tối ưu chống phân mảnh ổ đĩa cứng.";
        _translations["Disk Tools Usage Text"] = "1. Chọn mục [Context Menu] hoặc [Disk Tools].\n2. Tắt các mục chuột phải không cần thiết để menu mở nhanh hơn.\n3. Tiến hành quét dọn registry để sửa các liên kết tệp hỏng.";
    }
}

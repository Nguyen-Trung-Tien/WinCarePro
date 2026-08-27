# 📖 11. Sổ Tay Hướng Dẫn Sử Dụng Người Dùng (User Manual - Tiếng Việt)

Chào mừng bạn đến với **WinCare Pro Suite v4.3.0** — Hệ thống chăm sóc, dọn dẹp, bảo mật và tăng tốc Windows toàn diện thế hệ mới!

---

## 📥 1. Cài Đặt & Khởi Động Lần Đầu

### 1.1. Yêu Cầu Cấu Hình Máy Tính
- **Hệ điều hành:** Windows 10 (Phiên bản 2004 / Build 19041 trở lên) hoặc Windows 11 (64-bit).
- **Bộ vi xử lý (CPU):** Intel Core i3 / AMD Ryzen 3 trở lên.
- **Bộ nhớ RAM:** Tối thiểu 2GB (Khuyên dùng 4GB trở lên).
- **Dung lượng ổ cứng trống:** 150 MB.

### 1.2. Các Bước Cài Đặt
1. Tải bộ cài đặt `WinCareProSetup.exe` từ trang phát hành chính thức.
2. Nhấp đúp chuột vào file cài đặt. Khi hộp thoại **User Account Control (UAC)** xuất hiện, chọn **Yes** để cấp quyền Administrator.
3. Làm theo hướng dẫn trên màn hình và chọn **Finish** để khởi chạy WinCare Pro.

---

## 🧭 2. Hướng Dẫn Thao Tác Chi Tiết Từng Phân Hệ

### 2.1. 📊 Giám Sát & Xem Điểm Sức Khỏe (Dashboard)
- **Màn hình chính:** Hiển thị trực quan mức độ sử dụng CPU, RAM, Ổ cứng và Mạng.
- **Điểm sức khỏe (Health Score):**
  - **90 - 100 Điểm (Màu Xanh lá):** Máy tính ở trạng thái tuyệt vời.
  - **70 - 89 Điểm (Màu Vàng):** Máy tính có một số tệp rác hoặc tiến trình chạy ngầm cần tối ưu.
  - **Dưới 70 Điểm (Màu Đỏ):** Hệ thống đang chịu tải cao, ổ đĩa gần đầy hoặc có rủi ro bảo mật.
- **Nút "Tối Ưu Nhanh 1-Click":** Tự động giải phóng RAM và làm sạch các tệp tạm thời.

---

### 2.2. 🤖 Chẩn Đoán Bằng Trợ Lý AI (AI WinCare Assistant)
- **Bước 1:** Chọn mục **AI WinCare** trên thanh điều hướng bên trái.
- **Bước 2:** Nhấn nút **"Chạy Chẩn Đoán AI" (Run AI Diagnostics)**. Hệ thống sẽ tiến hành phân tích 8 phương diện sức khỏe của máy trong khoảng 5 - 10 giây.
- **Bước 3:** Xem danh sách các vấn đề được phát hiện và nhấn nút **"Khắc Phục" (Fix)** bên cạnh từng mục hoặc chọn **"Sửa Toàn Bộ"** để AI tự động tối ưu.

---

### 2.3. 🧹 Dọn Rác & Giải Phóng Bộ Nhớ Đệm (Junk Cleaner)
- **Bước 1:** Chọn mục **Dọn Tệp Rác (Junk Cleaner)**.
- **Bước 2:** Tích chọn các danh mục muốn quét (Tệp tạm Windows, Bộ nhớ đệm trình duyệt, Nhật ký lỗi, Thùng rác).
- **Bước 3:** Nhấn **"Quét Ngay" (Scan)** để xem danh sách và tổng dung lượng rác có thể giải phóng.
- **Bước 4:** Nhấn **"Dọn Dẹp" (Clean)**. Cơ chế an toàn `SafePathGuard` sẽ tự động bảo vệ các tệp mật khẩu trình duyệt và tệp hệ điều hành của bạn.

---

### 2.4. 📦 Gỡ Bỏ Phần Mềm & Dọn Tàn Dư (App Uninstaller)
- **Bước 1:** Chọn mục **Gỡ Cài Đặt (Uninstall)**.
- **Bước 2:** Tìm kiếm phần mềm bạn muốn gỡ bỏ trong danh sách.
- **Bước 3:** Nhấn nút **"Gỡ Cài Đặt" (Uninstall)** để chạy trình gỡ gốc.
- **Bước 4:** Sau khi gỡ xong, WinCare Pro sẽ tự động kích hoạt chế độ **Quét Tàn Dư (Deep Scan)** tìm các thư mục thừa trong AppData và các khóa Registry còn sót lại. Nhấn **"Xóa Tàn Dư"** để hoàn tất việc dọn dẹp triệt để.

---

### 2.5. 🌐 Kiểm Tra & Tối Ưu Mạng (Network Center)
- **Đo tốc độ Internet:** Nhấn **"Kiểm Tra Tốc Độ" (SpeedTest)** để đo Ping, tốc độ Download và Upload.
- **Đổi DNS 1-Click:** Chọn máy chủ DNS mong muốn (Cloudflare 1.1.1.1, Google 8.8.8.8, AdGuard chặn quảng cáo) và nhấn **"Áp Dụng DNS"**.
- **Sửa lỗi mất mạng:** Nếu không vào được mạng, nhấn nút **"Khôi Phục Mạng (Reset Network)"** để xóa bộ đệm DNS và làm mới cấu hình TCP/IP.

---

### 2.6. 🛠️ Sửa Lỗi Hệ Thống Windows (System Repair)
- **Sửa tệp hệ thống bị hỏng (SFC Scan):** Nhấn **"Quét & Sửa SFC"** (`sfc /scannow`).
- **Khôi phục kho thành phần Windows (DISM):** Nhấn **"Khôi Phục DISM"** để tải và sửa chữa các thành phần hệ thống gốc từ Microsoft.
- **Sửa lỗi Windows Update:** Nhấn **"Sửa Lỗi Update"** nếu máy tính bị kẹt khi tải các bản cập nhật Windows.

---

### 2.7. 🎮 Tăng Tốc Chơi Game (Gaming Turbo 2.0)
- **Bước 1:** Chọn mục **Gaming Turbo**.
- **Bước 2:** Bật công tắc **"Kích Hoạt Chế Độ Game" (Game Mode ON)**.
- **Hệ thống sẽ tự động:**
  - Kích hoạt chế độ năng lượng **Ultimate Performance**.
  - Giải phóng tối đa bộ nhớ RAM trống.
  - Tạm dừng các tác vụ nền gây giật lag (stuttering) trong game.
  - Tự động khôi phục về trạng thái bình thường sau khi bạn tắt chế độ.

---

### 2.8. 🪟 Cửa Sổ Nổi Desktop HUD Widget
- Mở menu **Cài Đặt** hoặc nhấn vào biểu tượng **Widget** trên thanh tiêu đề để bật cửa sổ mini.
- Cửa sổ nổi hiển thị liên tục thông số CPU, RAM, Tốc độ mạng ở góc màn hình Desktop.
- Bạn có thể nhấn giữ chuột trái để kéo thả Widget đến bất kỳ vị trí thuận tiện nào trên màn hình.

---

## ❓ 3. Câu Hỏi Thường Gặp (FAQ & Troubleshooting)

**Q1: WinCare Pro có làm xóa nhầm tài liệu cá nhân của tôi không?**  
*Trả lời:* Hoàn toàn không. Ứng dụng tích hợp hệ thống bảo vệ độc quyền `SafePathGuard`, tuyệt đối không quét hoặc can thiệp vào các thư mục tài liệu cá nhân (`Documents`, `Desktop`, `Pictures`, `Videos`) và chặn xóa các tệp mật khẩu trình duyệt.

**Q2: Tại sao ứng dụng cần quyền Quản Trị Viên (Run as Administrator)?**  
*Trả lời:* Các tính năng chuyên sâu như dọn dẹp file hệ thống, sửa lỗi tệp Windows qua SFC/DISM, đổi DNS card mạng và tối ưu RAM yêu cầu quyền quản trị cấp hệ thống của Windows để thực thi an toàn.

**Q3: Tôi có thể khôi phục lại các cài đặt cũ nếu không ưng ý không?**  
*Trả lời:* Có. Trước mọi thay đổi Registry hoặc tinh chỉnh dịch vụ, WinCare Pro đều tự động tạo một điểm sao lưu (Snapshot / Backup). Bạn chỉ cần vào mục **Lịch Sử & Hoàn Tác (Undo / Snapshots)** để quay lại trạng thái trước đó bất cứ lúc nào.

# Hệ thống Xếp hàng Tự động (Smart Queue System - SQS)

Dự án Đồ án Môn học IoT / Lập trình ứng dụng phân tán. Đây là một hệ thống xếp hàng tự động đa nền tảng, cho phép khách hàng lấy số từ Kiosk, theo dõi số trực tuyến qua Mobile App, hiển thị lên TV, và tích hợp với mô phỏng phần cứng Arduino qua Proteus để bấm số.

## 🌟 Kiến trúc Hệ thống (Tech Stack)

Hệ thống được thiết kế theo mô hình Client-Server với các thành phần sau:
- **Backend**: ASP.NET Core 10 (RESTful API & SignalR) - Quản lý logic gọi số (FIFO), xác thực JWT, phát sóng sự kiện Real-time và giao tiếp COM Port.
- **Database**: MySQL 8.0 (qua EF Core 9) - Lưu trữ thông tin User, Dịch vụ, Quầy, và Lịch sử Ticket.
- **Web Frontend**: ReactJS (Vite) - 4 giao diện riêng biệt: Kiosk (Lấy số), Display (TV tổng), Staff (Bàn làm việc), Admin (Thống kê KPI).
- **Desktop Client**: WinForms (.NET 10) - Ứng dụng dành riêng cho Staff, tích hợp SignalR Client và kết nối trực tiếp cổng COM cứng.
- **Mobile App**: Flutter (Android/iOS) - Ứng dụng cho khách hàng tra cứu số phiếu đang chờ từ xa.
- **IoT Hardware (Mô phỏng)**: Arduino Uno + Proteus - LCD hiển thị số đang gọi, Nút bấm cứng để gọi số, và Còi báo hiệu.
- **DevOps**: Docker Compose - Đóng gói Database và Backend chỉ bằng 1 lệnh duy nhất.

## 🚀 Tính năng nổi bật
- **Real-time Synchronization**: Sử dụng SignalR để đồng bộ hóa hàng chờ trên tất cả các màn hình (TV, Mobile, Staff Web/WinForms) ngay lập tức (độ trễ < 100ms).
- **Premium UI/UX**: Giao diện Web được thiết kế theo phong cách Glassmorphism hiện đại, Dark mode, kèm theo âm thanh và micro-animation bắt mắt.
- **Hardware Integration**: Nút cứng (phần cứng) có thể ra lệnh gọi số cho phần mềm, và phần mềm có thể cập nhật thông tin hiển thị lên LCD của phần cứng.
- **Smart Sequence Management**: Thuật toán quản lý sequence thread-safe bằng `IsolationLevel.Serializable` trong DB, tự động reset STT về 001 mỗi ngày.

## ⚙️ Hướng dẫn Khởi chạy (Chạy Demo)

### Cách 1: Chạy bằng Docker (Khuyên dùng cho Backend)
1. Cài đặt Docker Desktop.
2. Mở terminal tại thư mục gốc của project.
3. Chạy lệnh: `docker-compose up -d`
*(Hệ thống sẽ tự động pull MySQL, Backend ASP.NET và chạy DB Seed data).*

### Cách 2: Chạy Thủ công (Dành cho Development & IoT)
**1. Database (MySQL):**
- Import file `docs/init_database.sql` vào MySQL.
- Sửa ConnectionString trong `SQS.API/appsettings.json`.

**2. Backend (ASP.NET 10):**
```bash
cd SQS.API
dotnet run
```
*(Backend sẽ chạy ở `http://localhost:5000`)*

**3. Frontend (ReactJS):**
```bash
cd sqs-web
npm install
npm run dev
```
*(Truy cập `http://localhost:3000`)*

**4. Ứng dụng WinForms:**
```bash
cd SQS.WinForms
dotnet run
```

**5. Ứng dụng Mobile (Flutter):**
```bash
cd sqs_mobile
flutter run
```

## 🔌 Cấu hình Mô phỏng Phần cứng (Arduino + Proteus)
1. Cài đặt Virtual Serial Port Emulator (VSPE), tạo kết nối Pair (VD: COM3 <-> COM4).
2. Sửa file `appsettings.json` trong thư mục `SQS.API`, bật `Enabled: true` và chọn `PortName: "COM3"`.
3. Trong mạch mô phỏng Proteus, cài đặt Module COMPIM là `COM4` và baud rate `9600`.
4. Nạp file mã hex của `sqs_arduino/sqs_arduino.ino` vào mạch Arduino trên Proteus.
5. Nhấn Play. Khi bấm nút trên Proteus, hệ thống sẽ tự động gọi khách hàng!

---
*Developed and designed with 🤍 by: **Tam thái tử***

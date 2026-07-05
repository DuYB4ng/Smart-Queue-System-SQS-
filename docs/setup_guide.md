# 🚀 Hướng dẫn Cài đặt & Chạy SQS

## Yêu cầu môi trường

| Công cụ | Version | Link |
|---------|---------|------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com |
| Node.js | 18+ | https://nodejs.org |
| Flutter SDK | 3.x | https://flutter.dev |
| Docker Desktop | Latest | https://docker.com |
| Proteus | 8.x | Thương mại |
| com0com | Latest | https://sourceforge.net/projects/com0com/ |
| Visual Studio | 2022 | Cho WinForms |

## Bước 1: Cài com0com (COM ảo)

1. Tải & cài **com0com** từ SourceForge
2. Mở **Setup Command Prompt** của com0com
3. Tạo cặp COM:
   ```
   install PortName=COM3 PortName=COM4
   ```
4. Kiểm tra trong Device Manager → Ports

## Bước 2: Chạy Backend + DB (Docker)

```bash
# Clone và chạy
cd IoT/
docker-compose up -d

# Kiểm tra
curl http://localhost:5000/health
# Adminer DB: http://localhost:8080
```

## Bước 3: Chạy ReactJS Web

```bash
cd sqs-web/
npm install
npm run dev
# → http://localhost:3000
#   /kiosk    — Kiosk lấy số
#   /display  — Màn hình TV
#   /staff    — Giao diện nhân viên
#   /admin    — Dashboard admin
```

## Bước 4: Chạy Flutter App

```bash
cd sqs_flutter/
flutter pub get
flutter run
```

## Bước 5: Chạy WinForms

1. Mở `SQS.WinForms/SQS.WinForms.sln` bằng Visual Studio
2. Chạy (F5)
3. Cấu hình COM Port trong Settings → chọn `COM4`

## Bước 6: Chạy Proteus Simulation

1. Mở `SQS.Arduino/sqs_proteus.pdsprj`
2. Compile Arduino sketch (`sqs_display.ino`)
3. Upload vào Proteus
4. Chạy simulation
5. Virtual Terminal kết nối `COM3`

## Tài khoản mặc định (seed data)

| Role | Email | Mật khẩu |
|------|-------|---------|
| Admin | admin@sqs.edu.vn | Admin@123 |
| Staff | staff1@sqs.edu.vn | Staff@123 |
| Customer | sv001@sqs.edu.vn | Student@123 |

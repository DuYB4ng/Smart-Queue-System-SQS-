# 🏫 Hệ thống Xếp Hàng Tự Động — Smart Queue System (SQS)
**Môn học IoT | Bối cảnh: Trường Đại học**

> Cập nhật lần cuối: 2026-07-05

Hệ thống giải quyết ùn tắc, chờ đợi tại các phòng ban trường học (đăng ký học phần, nộp hồ sơ, thanh toán học phí, tư vấn tuyển sinh, nhận bằng/giấy tờ). Gồm 5 nền tảng: Backend (ASP.NET), Web (ReactJS - Kiosk + Display), Mobile (Flutter), Desktop (WinForms), Phần cứng (Arduino/Proteus).

---

## ✅ Quyết định thiết kế đã xác nhận

| # | Vấn đề | Quyết định |
|---|--------|------------|
| Q1 | Số thứ tự ticket | **Dùng chung 1 chuỗi số theo ngày** (001, 002, 003... → reset mỗi ngày) |
| Q2 | Màn hình tổng | **React route `/display`** — chạy trên TV/màn hình lớn, real-time qua SignalR |
| Q3 | Xác thực | **ASP.NET Identity** (email + mật khẩu) |
| Q4 | Triển khai | **Docker Compose** (API + MySQL + Adminer) |
| Q5 | Bối cảnh | **Trường Đại học** (đăng ký học phần, nộp hồ sơ, học phí, tư vấn, nhận bằng) |
| Q6 | Mô hình quầy | **Nhiều quầy, phân theo loại dịch vụ** (quầy A phục vụ dịch vụ A...) |

---

## 🗄️ Database Schema (MySQL)

### Sơ đồ quan hệ

```
users (1) ──────< customers (1-1)
users (1) ──────< staffs    (1-1)
users (1) ──────< admins    (1-1)

services (N) >──────< counters (N)   [qua counter_services]
counters  (1) ──────< tickets  (N)
services  (1) ──────< tickets  (N)
staffs    (1) ──────< tickets  (N)
customers (1) ──────< tickets  (N)   [nullable — khách vãng lai]

services (1) ──────< daily_sequence (N)
```

### Chi tiết bảng

| Bảng | Các cột chính | Ghi chú |
|------|---------------|---------|
| `users` | id, name, email, password_hash, birthday, address, role ENUM | Bảng gốc chung |
| `customers` | user_id (FK, PK) | Quan hệ 1-1 với users |
| `staffs` | user_id (FK, PK), position, kpi INT DEFAULT 0 | Quan hệ 1-1 với users |
| `admins` | user_id (FK, PK) | Quan hệ 1-1 với users |
| `services` | id, name, code VARCHAR(5), description | VD: code="DK" (Đăng ký học phần) |
| `counters` | id, name, is_active BOOL | VD: "Quầy 1", "Quầy 2" |
| `counter_services` | counter_id (FK), service_id (FK) | Phân công dịch vụ cho quầy (N-N) |
| `tickets` | id, ticket_number CHAR(3), id_customer (nullable), guest_name (nullable), id_service, id_counter (nullable), id_staff (nullable), date DATE, created_at DATETIME, status ENUM | Phiên xếp hàng chính |
| `daily_sequence` | id, date DATE, last_number INT | Bộ đếm số thứ tự theo ngày (UNIQUE: date) |

### Quy tắc số thứ tự
- **Format**: `001` → `002` → ... → `999`
- **Reset**: Mỗi ngày về `001`
- **Bảng `daily_sequence`**: Khi tạo ticket, dùng `SELECT FOR UPDATE` trên bảng này để tránh race condition

---

## 🏗️ Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────┐
│                   CLIENT LAYER                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐ │
│  │ReactJS   │ │ReactJS   │ │WinForms  │ │Flutter │ │
│  │/kiosk    │ │/display  │ │Desktop   │ │Mobile  │ │
│  │(Khách)   │ │(TV tổng) │ │(Staff)   │ │(SV App)│ │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └───┬────┘ │
└───────┼────────────┼────────────┼────────────┼──────┘
        │            │            │            │
        └────────────┴──────┬─────┴────────────┘
                            │  REST API + SignalR (WebSocket)
              ┌─────────────▼─────────────┐
              │     ASP.NET Web API       │
              │  ┌─────────────────────┐  │
              │  │   SignalR Hub       │  │
              │  │   QueueHub.cs       │  │
              │  └─────────────────────┘  │
              │  ┌─────────────────────┐  │
              │  │  SerialPortService  │──┼──→ COM Port → Arduino (Proteus)
              │  └─────────────────────┘  │
              └─────────────┬─────────────┘
                            │ EF Core
              ┌─────────────▼─────────────┐
              │       MySQL 8.0           │
              └───────────────────────────┘
```

---

## 📁 Cấu trúc thư mục toàn dự án

```
IoT/
├── docs/                          ← Tài liệu & kế hoạch
│   ├── implementation_plan.md     ← File này
│   ├── database_schema.md
│   ├── api_spec.md
│   └── setup_guide.md
│
├── SQS.API/                       ← Backend ASP.NET Web API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── TicketsController.cs
│   │   ├── StaffController.cs
│   │   ├── ServicesController.cs
│   │   ├── CountersController.cs
│   │   └── AdminController.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Customer.cs
│   │   ├── Staff.cs
│   │   ├── Admin.cs
│   │   ├── Service.cs
│   │   ├── Counter.cs
│   │   ├── CounterService.cs
│   │   ├── Ticket.cs
│   │   └── DailySequence.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── Tickets/
│   │   └── Users/
│   ├── Hubs/
│   │   └── QueueHub.cs            ← SignalR
│   ├── Services/
│   │   ├── TicketService.cs
│   │   ├── SequenceService.cs
│   │   └── SerialPortService.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Migrations/
│   ├── Dockerfile
│   └── Program.cs
│
├── sqs-web/                       ← Frontend ReactJS
│   ├── src/
│   │   ├── pages/
│   │   │   ├── KioskPage.jsx      ← /kiosk
│   │   │   ├── DisplayPage.jsx    ← /display (TV tổng)
│   │   │   ├── StaffPage.jsx      ← /staff
│   │   │   ├── AdminDashboard.jsx ← /admin
│   │   │   └── LoginPage.jsx
│   │   ├── components/
│   │   │   ├── TicketCard.jsx
│   │   │   ├── QueueDisplay.jsx
│   │   │   └── KPITable.jsx
│   │   └── services/
│   │       └── signalRService.js
│   └── package.json
│
├── sqs_flutter/                   ← Mobile App Flutter
│   └── lib/
│       ├── screens/
│       │   ├── login_screen.dart
│       │   ├── home_screen.dart
│       │   ├── ticket_screen.dart
│       │   └── profile_screen.dart
│       └── services/
│           ├── api_service.dart
│           └── signalr_service.dart
│
├── SQS.WinForms/                  ← Desktop App
│   ├── Forms/
│   │   ├── MainForm.cs
│   │   ├── LoginForm.cs
│   │   └── SettingsForm.cs
│   └── Services/
│       ├── ApiService.cs
│       ├── SignalRService.cs
│       └── SerialPortService.cs
│
├── SQS.Arduino/                   ← IoT Simulation
│   ├── sqs_display.ino            ← Arduino sketch
│   └── sqs_proteus.pdsprj         ← File Proteus
│
├── docker-compose.yml             ← Toàn bộ infrastructure
└── README.md
```

---

## 🔌 API Specification

### Auth
| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| POST | `/api/auth/login` | `{ email, password }` | `{ token, user }` |
| POST | `/api/auth/register` | `{ name, email, password, birthday, address }` | `{ token, user }` |

### Tickets (Phiên xếp hàng)
| Method | Endpoint | Body | Role | Mô tả |
|--------|----------|------|------|-------|
| POST | `/api/tickets` | `{ serviceId, guestName? }` | Customer/Guest | Lấy số |
| GET | `/api/tickets/{id}/status` | — | Customer | Xem trạng thái |
| DELETE | `/api/tickets/{id}` | — | Customer | Hủy số |
| GET | `/api/tickets/queue` | `?serviceId=` | All | Danh sách đang chờ |

### Staff Operations
| Method | Endpoint | Body | Role | Mô tả |
|--------|----------|------|------|-------|
| POST | `/api/staff/call-next` | `{ counterId }` | Staff | Gọi số tiếp theo |
| POST | `/api/staff/complete/{id}` | — | Staff | Hoàn thành → +1 KPI |
| POST | `/api/staff/skip/{id}` | — | Staff | Bỏ qua (Canceled) |

### Admin
| Method | Endpoint | Role | Mô tả |
|--------|----------|------|-------|
| GET | `/api/admin/dashboard` | Admin | Thống kê tổng hợp |
| GET | `/api/admin/staff` | Admin | Danh sách Staff + KPI |
| PUT | `/api/admin/staff/{id}/kpi` | Admin | Reset/Sửa KPI |

---

## 📡 SignalR Events

### Server → All Clients
| Event | Khi nào | Payload |
|-------|---------|---------|
| `TicketCalled` | Staff gọi số | `{ ticketNumber, counterName, serviceName }` |
| `QueueUpdated` | Thay đổi bất kỳ | `{ serviceId, waitingCount }` |

### Server → Specific Client
| Event | Khi nào | Payload |
|-------|---------|---------|
| `TicketStatusChanged` | Ticket của bạn thay đổi | `{ ticketId, newStatus }` |

---

## 🔧 COM Port Protocol (Arduino ↔ Backend)

### Backend → Arduino (gửi lệnh)
```
CALL:001\n          → Hiển thị số 001 lên LCD, buzzer 3 tiếng
RESET\n             → Xóa màn hình LCD
```

### Arduino → Backend (phản hồi & nút bấm)
```
ACK:OK\n            → Xác nhận đã nhận lệnh
BTN:NEXT\n          → Nút bấm vật lý → Trigger gọi số tiếp theo
```

### Cấu hình COM
- **Baud rate**: 9600
- **Cổng COM ảo**: Tạo bằng **com0com** (Windows)
  - Arduino/Proteus dùng: `COM3`
  - Backend/WinForms dùng: `COM4`

---

## 🐳 Docker Compose

```yaml
services:
  sqs-db:
    image: mysql:8.0
    ports: ["3306:3306"]
    volumes: ["mysql_data:/var/lib/mysql"]
    environment:
      MYSQL_DATABASE: sqs_db
      MYSQL_ROOT_PASSWORD: sqs@2026

  sqs-api:
    build: ./SQS.API
    ports: ["5000:80"]
    depends_on: [sqs-db]
    environment:
      ConnectionStrings__Default: "Server=sqs-db;Database=sqs_db;..."

  sqs-adminer:
    image: adminer
    ports: ["8080:8080"]
```

---

## 📋 Kế hoạch thực thi (12 Phases)

| Phase | Task | Nền tảng | Output |
|-------|------|----------|--------|
| **1** | Thiết kế & viết DB schema | MySQL | `init_database.sql` |
| **2** | Khởi tạo ASP.NET project, Models, EF Core | Backend | Cấu trúc project |
| **3** | Auth API (ASP.NET Identity) | Backend | Login/Register |
| **4** | Ticket API + Logic gọi số + Sequence | Backend | Core business logic |
| **5** | SignalR Hub — real-time | Backend | `QueueHub.cs` |
| **6** | Serial Port Service | Backend/WinForms | Giao tiếp COM |
| **7** | Docker Compose setup | DevOps | Môi trường chạy |
| **8** | ReactJS: Kiosk + Display + Staff + Admin | Web | 4 giao diện |
| **9** | WinForms: Staff UI + COM | Desktop | Ứng dụng Staff |
| **10** | Flutter: Customer App | Mobile | App sinh viên |
| **11** | Arduino Sketch + Proteus | IoT | Mô phỏng phần cứng |
| **12** | Integration test + Documentation | All | README + Hướng dẫn |

---

## ⚠️ Lưu ý kỹ thuật

1. **Race condition số thứ tự**: Dùng `SELECT ... FOR UPDATE` hoặc DB transaction khi tăng `daily_sequence.last_number`
2. **com0com**: Cần cài driver này trước khi test COM port
3. **CORS**: Cấu hình cho phép ReactJS (`localhost:3000`) và WinForms gọi API
4. **Flutter SignalR**: Dùng package `signalr_netcore` trên pub.dev

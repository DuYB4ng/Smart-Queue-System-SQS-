# 📊 Database Schema — SQS

> Tài liệu chi tiết schema MySQL cho Smart Queue System
> Script SQL: [`init_database.sql`](./init_database.sql)

---

## Sơ đồ quan hệ (ERD)

```
users (1) ──────── customers (1-1)  id_customer (nullable)
users (1) ──────── staffs    (1-1)                          ←── tickets.id_staff
users (1) ──────── admins    (1-1)

services  ─────<  counter_services  >──── counters
services  (1) ──────────────────────────< tickets (N)
counters  (1) ──────────────────────────< tickets (N)
customers (1, nullable) ─────────────── tickets (N)

daily_sequence : 1 row/ngày — bộ đếm số thứ tự chung
```

---

## Chi tiết các bảng

### `users` — Bảng gốc

| Cột | Kiểu | Ràng buộc | Mô tả |
|-----|------|-----------|-------|
| id | INT UNSIGNED | PK, AUTO_INCREMENT | |
| name | NVARCHAR(100) | NOT NULL | Họ tên |
| email | VARCHAR(150) | UNIQUE, NOT NULL | Email đăng nhập |
| password_hash | VARCHAR(255) | NOT NULL | BCrypt hash |
| birthday | DATE | NULL | |
| address | NVARCHAR(255) | NULL | |
| role | ENUM | NOT NULL | `Customer / Staff / Admin` |
| created_at | DATETIME | DEFAULT NOW() | |
| updated_at | DATETIME | ON UPDATE NOW() | |
| is_active | TINYINT(1) | DEFAULT 1 | Soft delete |

### `customers` — Quan hệ 1-1 với users

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| user_id | INT UNSIGNED | PK + FK → users.id |

### `staffs` — Quan hệ 1-1 với users

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| user_id | INT UNSIGNED | PK + FK → users.id |
| position | NVARCHAR(100) | Chức vụ, DEFAULT 'Nhân viên' |
| kpi | INT UNSIGNED | Tổng phiên hoàn thành, DEFAULT 0 |

### `admins` — Quan hệ 1-1 với users

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| user_id | INT UNSIGNED | PK + FK → users.id |

### `services` — Danh mục dịch vụ

| Code | Tên dịch vụ | Mô tả |
|------|-------------|-------|
| `DK` | Đăng ký học phần | Đăng ký, hủy, điều chỉnh học phần |
| `HS` | Nộp hồ sơ | Xin học bổng, miễn giảm học phí... |
| `HP` | Thanh toán học phí | Đóng học phí, ký túc xá |
| `TV` | Tư vấn tuyển sinh | Ngành học, điểm chuẩn... |
| `BG` | Nhận bằng & giấy tờ | Bằng tốt nghiệp, bảng điểm... |

### `counters` — Quầy phục vụ

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| id | INT UNSIGNED | PK |
| name | NVARCHAR(100) | VD: "Quầy 1 — Đăng ký học phần" |
| location | NVARCHAR(200) | VD: "Phòng 101, Tầng 1" |
| is_active | TINYINT(1) | DEFAULT 1 |

### `counter_services` — Phân công (N-N)

| Cột | Ghi chú |
|-----|---------|
| counter_id | FK → counters.id |
| service_id | FK → services.id |

**Mặc định:** Quầy 1 ↔ DK, Quầy 2 ↔ HS, Quầy 3 ↔ HP, Quầy 4 ↔ TV, Quầy 5 ↔ BG

### `tickets` — Phiên xếp hàng ⭐ (Bảng chính)

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| id | INT UNSIGNED | PK |
| ticket_number | CHAR(3) | "001"..."999", reset mỗi ngày |
| id_customer | INT UNSIGNED | NULL (khách vãng lai) |
| guest_name | NVARCHAR(100) | NULL (nếu đã đăng nhập) |
| id_service | INT UNSIGNED | FK → services.id |
| id_counter | INT UNSIGNED | NULL → gán khi Staff gọi |
| id_staff | INT UNSIGNED | NULL → gán khi Staff gọi |
| ticket_date | DATE | NOT NULL, ngày tạo phiên |
| created_at | DATETIME | NOT NULL |
| called_at | DATETIME | NULL → set khi gọi số |
| completed_at | DATETIME | NULL → set khi hoàn thành |
| status | ENUM | `Waiting / Calling / Completed / Canceled` |

> **Constraint**: `id_customer IS NOT NULL OR guest_name IS NOT NULL`

### `daily_sequence` — Bộ đếm số thứ tự

| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| id | INT UNSIGNED | PK |
| seq_date | DATE | UNIQUE — 1 row/ngày |
| last_number | SMALLINT UNSIGNED | 0→999, tăng dần trong ngày |

> **Thread-safe**: Stored Procedure `sp_get_next_ticket_number` dùng `SELECT FOR UPDATE`

---

## Luồng trạng thái Ticket

```
  [Tạo phiên]
      │
      ▼
  ┌─────────┐       Staff gọi số       ┌─────────┐
  │ Waiting │ ─────────────────────── ▶│ Calling │
  └─────────┘                          └─────────┘
      │                                     │
      │ Customer hủy                        ├── Staff hoàn thành
      ▼                                     │        ▼
  ┌──────────┐                         ┌───────────┐
  │ Canceled │ ◀─── Staff bỏ qua ──── │ Completed │
  └──────────┘   (chỉ khi Calling)    └───────────┘
```

---

## Indexes

```sql
-- Gọi số: tìm Waiting cũ nhất theo service
idx_tickets_service_status_created  (id_service, status, created_at)

-- Khách theo dõi vé của mình
idx_tickets_customer_date           (id_customer, ticket_date)

-- Dashboard thống kê theo ngày
idx_tickets_date_status             (ticket_date, status)

-- Màn hình tổng (đang Calling)
idx_tickets_status_counter          (status, id_counter)
```

---

## Views

| View | Mục đích |
|------|---------|
| `v_waiting_queue` | Hàng đợi hiện tại + vị trí xếp hàng |
| `v_staff_kpi_today` | KPI của Staff hôm nay vs tổng |
| `v_currently_calling` | Số đang được gọi → Màn hình Display |

---

## Tài khoản mặc định (Seed Data)

| Role | Email | Mật khẩu |
|------|-------|---------|
| Admin | admin@sqs.edu.vn | `Admin@123` |
| Staff 1 | staff1@sqs.edu.vn | `Admin@123` |
| Staff 2 | staff2@sqs.edu.vn | `Admin@123` |
| Staff 3–5 | staff3-5@sqs.edu.vn | `Admin@123` |
| Customer | sv001@sqs.edu.vn | `Admin@123` |
| Customer | sv002@sqs.edu.vn | `Admin@123` |
| Customer | sv003@sqs.edu.vn | `Admin@123` |

> ⚠️ Hash `$2a$10$TzP9...` trong SQL là BCrypt của `Admin@123`.
> Khi ASP.NET Identity verify password, cần dùng `PasswordHasher` cùng loại.
> Phase 2 sẽ xử lý việc đồng bộ hasher.

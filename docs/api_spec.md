# 📡 API Specification — SQS

> Base URL: `http://localhost:5000/api`
> Auth: Bearer Token (ASP.NET Identity)

## Authentication

### POST `/auth/login`
```json
// Request
{ "email": "sv001@sqs.edu.vn", "password": "Student@123" }

// Response 200
{ "token": "eyJ...", "user": { "id": 1, "name": "Nguyễn Văn A", "role": "Customer" } }
```

### POST `/auth/register`
```json
// Request
{ "name": "Nguyễn Văn A", "email": "sv001@sqs.edu.vn", "password": "Student@123", "birthday": "2003-01-15", "address": "TP.HCM" }
```

---

## Tickets

### POST `/tickets` — Lấy số
```json
// Request (Customer đã đăng nhập)
{ "serviceId": 1 }

// Request (Kiosk — khách vãng lai)
{ "serviceId": 1, "guestName": "Nguyễn Văn B" }

// Response 201
{
  "ticketId": 42,
  "ticketNumber": "007",
  "serviceName": "Đăng ký học phần",
  "estimatedWait": 3,    // số người đang chờ trước
  "createdAt": "2026-07-05T09:15:00"
}
```

### GET `/tickets/{id}/status`
```json
// Response 200
{
  "ticketId": 42,
  "ticketNumber": "007",
  "status": "Waiting",
  "position": 3,          // vị trí trong hàng đợi
  "calledAt": null
}
```

### DELETE `/tickets/{id}` — Hủy số
```json
// Response 200
{ "message": "Đã hủy thành công" }
```

### GET `/tickets/queue?serviceId=1` — Danh sách đang chờ
```json
// Response 200
{
  "serviceId": 1,
  "serviceName": "Đăng ký học phần",
  "waiting": [
    { "ticketNumber": "005", "createdAt": "..." },
    { "ticketNumber": "006", "createdAt": "..." }
  ],
  "currentCalling": "004"
}
```

---

## Staff Operations

### POST `/staff/call-next`
```json
// Request
{ "counterId": 1 }

// Response 200
{
  "ticketId": 40,
  "ticketNumber": "005",
  "customerName": "Nguyễn Văn A",
  "serviceName": "Đăng ký học phần"
}
// → Trigger SignalR: TicketCalled
// → Gửi "CALL:005\n" qua COM Port
```

### POST `/staff/complete/{ticketId}`
```json
// Response 200
{ "message": "Hoàn thành", "staffKpi": 15 }
// → Trigger SignalR: QueueUpdated
```

### POST `/staff/skip/{ticketId}`
```json
// Response 200
{ "message": "Đã bỏ qua" }
// → Trigger SignalR: QueueUpdated
```

---

## Admin

### GET `/admin/dashboard`
```json
// Response 200
{
  "today": {
    "totalTickets": 87,
    "completed": 72,
    "canceled": 8,
    "waiting": 7
  },
  "byService": [
    { "serviceCode": "DK", "serviceName": "Đăng ký học phần", "count": 30 }
  ]
}
```

### GET `/admin/staff`
```json
// Response 200
[
  { "staffId": 2, "name": "Trần Thị B", "position": "Nhân viên", "kpi": 72, "counterId": 1 }
]
```

---

## SignalR Hub

**Endpoint**: `http://localhost:5000/hubs/queue`

### Client subscribe events

```javascript
connection.on("TicketCalled", (data) => {
  // data: { ticketNumber, counterName, serviceName }
  // → Màn hình Display cập nhật số đang gọi
  // → Phát âm thanh
});

connection.on("QueueUpdated", (data) => {
  // data: { serviceId, waitingCount }
  // → Cập nhật số người đang chờ
});

connection.on("TicketStatusChanged", (data) => {
  // data: { ticketId, newStatus }
  // → Thông báo riêng cho khách hàng
});
```

import 'package:flutter/material.dart';

/// Model cho 1 vé khi lấy danh sách "Vé của tôi" (GET /api/tickets/my).
/// Khớp với DTO `TicketStatusResponse` trong Tickets/TicketDtos.cs của backend:
///
/// ```json
/// { "ticketId": 12, "ticketNumber": "0012", "customerName": "Nguyễn A",
///   "ticketType": "WalkIn", "serviceName": "Đăng ký học phần",
///   "status": "Waiting", "queuePosition": 3, "counterName": null,
///   "calledAt": null, "createdAt": "2026-07-10T08:30:00Z",
///   "appointmentDate": null }
/// ```
class TicketInfo {
  final int ticketId;
  final String? ticketNumber;
  final String? customerName;
  final String ticketType; // "WalkIn" | "Appointment"
  final String serviceName;
  final String status; // "Waiting" | "Calling" | "Completed" | "Canceled"
  final int? queuePosition;
  final String? counterName;
  final DateTime? calledAt;
  final DateTime createdAt;
  final DateTime? appointmentDate;

  const TicketInfo({
    required this.ticketId,
    this.ticketNumber,
    this.customerName,
    required this.ticketType,
    required this.serviceName,
    required this.status,
    this.queuePosition,
    this.counterName,
    this.calledAt,
    required this.createdAt,
    this.appointmentDate,
  });

  factory TicketInfo.fromJson(Map<String, dynamic> json) {
    Object? read(String a, String b) => json[a] ?? json[b];
    DateTime? tryDate(Object? v) =>
        v == null ? null : DateTime.tryParse(v.toString());

    return TicketInfo(
      ticketId: (read('ticketId', 'TicketId') as num?)?.toInt() ?? 0,
      ticketNumber: read('ticketNumber', 'TicketNumber')?.toString(),
      customerName: read('customerName', 'CustomerName')?.toString(),
      ticketType: (read('ticketType', 'TicketType') ?? 'WalkIn').toString(),
      serviceName: (read('serviceName', 'ServiceName') ?? '').toString(),
      status: (read('status', 'Status') ?? 'Waiting').toString(),
      queuePosition: (read('queuePosition', 'QueuePosition') as num?)?.toInt(),
      counterName: read('counterName', 'CounterName')?.toString(),
      calledAt: tryDate(read('calledAt', 'CalledAt')),
      createdAt:
          tryDate(read('createdAt', 'CreatedAt')) ?? DateTime.now(),
      appointmentDate: tryDate(read('appointmentDate', 'AppointmentDate')),
    );
  }

  /// Phân loại để chia 2 nhóm trong UI: "Hôm nay" vs "Ngày khác".
  /// WalkIn luôn thuộc nhóm hôm nay; Appointment so theo appointmentDate.
  bool isToday(DateTime now) {
    if (ticketType == 'WalkIn') {
      return _sameDay(createdAt, now);
    }
    final appt = appointmentDate;
    if (appt == null) return _sameDay(createdAt, now);
    return _sameDay(appt, now);
  }

  static bool _sameDay(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;
}

/// Helper mô tả trạng thái vé -> (label tiếng Việt, màu, icon).
class TicketStatusStyle {
  final String label;
  final Color color;
  final IconData icon;

  const TicketStatusStyle(this.label, this.color, this.icon);

  static TicketStatusStyle from(String status) {
    switch (status) {
      case 'Waiting':
        return const TicketStatusStyle(
            'Đang chờ', Color(0xFFE8A33D), Icons.access_time_rounded);
      case 'Calling':
        return const TicketStatusStyle(
            'Đang gọi', Color(0xFF22C55E), Icons.campaign_rounded);
      case 'Completed':
        return const TicketStatusStyle(
            'Hoàn thành', Color(0xFF0E6E64), Icons.check_circle_rounded);
      case 'Canceled':
        return const TicketStatusStyle(
            'Đã hủy', Color(0xFFEF4444), Icons.cancel_rounded);
      default:
        return const TicketStatusStyle(
            'Không rõ', Color(0xFF6B7280), Icons.help_outline);
    }
  }
}

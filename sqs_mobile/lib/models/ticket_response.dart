/// Response khi tạo vé Walk-in (POST /api/tickets | /api/tickets/guest).
/// Khớp với DTO `CreateTicketResponse` trong Tickets/TicketDtos.cs của backend:
///
/// ```json
/// { "ticketId": 12, "ticketNumber": "0012", "ticketType": "WalkIn",
///   "serviceName": "Đăng ký học phần", "serviceCode": "DK",
///   "estimatedWait": 3, "createdAt": "2026-07-10T08:30:00Z" }
/// ```
class CreateTicketResponse {
  final int ticketId;
  final String? ticketNumber;
  final String ticketType;
  final String serviceName;
  final String serviceCode;
  final int? estimatedWait;
  final String? createdAt;

  const CreateTicketResponse({
    required this.ticketId,
    this.ticketNumber,
    required this.ticketType,
    required this.serviceName,
    required this.serviceCode,
    this.estimatedWait,
    this.createdAt,
  });

  factory CreateTicketResponse.fromJson(Map<String, dynamic> json) {
    Object? read(String a, String b) => json[a] ?? json[b];
    return CreateTicketResponse(
      ticketId: (read('ticketId', 'TicketId') as num?)?.toInt() ?? 0,
      ticketNumber: read('ticketNumber', 'TicketNumber')?.toString(),
      ticketType: (read('ticketType', 'TicketType') ?? 'WalkIn').toString(),
      serviceName: (read('serviceName', 'ServiceName') ?? '').toString(),
      serviceCode: (read('serviceCode', 'ServiceCode') ?? '').toString(),
      estimatedWait: (read('estimatedWait', 'EstimatedWait') as num?)?.toInt(),
      createdAt: read('createdAt', 'CreatedAt')?.toString(),
    );
  }
}

/// Response khi tạo vé hẹn (POST /api/tickets/appointment[.../guest]).
/// Khớp với DTO `AppointmentResponse` trong Tickets/TicketDtos.cs của backend:
///
/// ```json
/// { "ticketId": 15, "serviceName": "...", "appointmentDate": "2026-07-11T...",
///   "studentId": null, "phoneNumber": null, "note": null,
///   "status": "Waiting", "createdAt": "..." }
/// ```
class AppointmentResponse {
  final int ticketId;
  final String serviceName;
  final DateTime appointmentDate;
  final String? studentId;
  final String? phoneNumber;
  final String? note;
  final String status;
  final String? createdAt;

  const AppointmentResponse({
    required this.ticketId,
    required this.serviceName,
    required this.appointmentDate,
    this.studentId,
    this.phoneNumber,
    this.note,
    required this.status,
    this.createdAt,
  });

  factory AppointmentResponse.fromJson(Map<String, dynamic> json) {
    Object? read(String a, String b) => json[a] ?? json[b];
    final rawDate = read('appointmentDate', 'AppointmentDate')?.toString();
    return AppointmentResponse(
      ticketId: (read('ticketId', 'TicketId') as num?)?.toInt() ?? 0,
      serviceName: (read('serviceName', 'ServiceName') ?? '').toString(),
      appointmentDate:
          rawDate != null ? DateTime.tryParse(rawDate) ?? DateTime.now() : DateTime.now(),
      studentId: read('studentId', 'StudentId')?.toString(),
      phoneNumber: read('phoneNumber', 'PhoneNumber')?.toString(),
      note: read('note', 'Note')?.toString(),
      status: (read('status', 'Status') ?? 'Waiting').toString(),
      createdAt: read('createdAt', 'CreatedAt')?.toString(),
    );
  }
}

/// Enum gói loại vé để UI dễ switch, đồng bộ 2 giá trị của backend
/// (TicketType.WalkIn | Appointment).
enum TicketType { walkIn, appointment }

extension TicketTypeX on TicketType {
  String get label => this == TicketType.walkIn ? 'WalkIn' : 'Appointment';
}

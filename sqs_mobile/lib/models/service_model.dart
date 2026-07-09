/// Model cho 1 dịch vụ, khớp với response của `GET /api/services`
/// trên backend (xem ServicesController.cs).
///
/// Backend trả về JSON dạng:
/// ```json
/// { "id": 1, "name": "Đăng ký học phần", "code": "DK",
///   "description": "...", "counters": [ ... ] }
/// ```
/// Mobile chỉ cần id/name/code/description để hiển thị + gửi serviceId.
class Service {
  final int id;
  final String name;
  final String code;
  final String? description;

  const Service({
    required this.id,
    required this.name,
    required this.code,
    this.description,
  });

  factory Service.fromJson(Map<String, dynamic> json) {
    // Backend dùng PascalCase (default của .NET), nên đọc theo cả 2 kiểu
    // để an toàn nếu sau này đổi serializer sang camelCase.
    return Service(
      id: (json['id'] ?? json['Id']) as int,
      name: (json['name'] ?? json['Name'] ?? '').toString(),
      code: (json['code'] ?? json['Code'] ?? '').toString(),
      description: (json['description'] ?? json['Description'])?.toString(),
    );
  }
}

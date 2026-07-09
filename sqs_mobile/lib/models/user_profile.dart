/// Thông tin profile của user, khớp với `UserDto` của backend
/// (DTOs/Auth/AuthResponse.cs). GET /api/users/me trả đúng object này.
///
/// ```json
/// { "id": 1, "name": "Nguyễn Văn A", "email": "a@x.com",
///   "role": "Customer", "birthday": "2003-05-10T00:00:00Z",
///   "address": "Hà Nội" }
/// ```
class UserProfile {
  final int id;
  final String name;
  final String email;
  final String role; // "Customer" | "Staff" | "Admin"
  final DateTime? birthday;
  final String? address;

  const UserProfile({
    required this.id,
    required this.name,
    required this.email,
    required this.role,
    this.birthday,
    this.address,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    Object? read(String a, String b) => json[a] ?? json[b];
    final rawBirthday = read('birthday', 'Birthday');
    return UserProfile(
      id: (read('id', 'Id') as num?)?.toInt() ?? 0,
      name: (read('name', 'Name') ?? '').toString(),
      email: (read('email', 'Email') ?? '').toString(),
      role: (read('role', 'Role') ?? 'Customer').toString(),
      birthday: rawBirthday == null
          ? null
          : DateTime.tryParse(rawBirthday.toString()),
      address: read('address', 'Address')?.toString(),
    );
  }
}

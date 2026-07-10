import 'dart:convert';
import 'package:http/http.dart' as http;

import '../models/user_profile.dart';
import 'auth_service.dart'; // để dùng lại AuthException + baseUrl pattern

/// Service gọi các endpoint profile của user:
///  - GET    /api/users/me          -> UserProfile
///  - PUT    /api/users/me          -> UserProfile (name, birthday, address)
///  - PUT    /api/users/me/password -> 204 No Content
///
/// Dùng chung baseUrl + cách xử lý lỗi với AuthService.
class UserService {
  static const String baseUrl = 'http://192.168.1.7:5000/api';

  final String _token;

  UserService(this._token);

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $_token',
      };

  /// Lấy thông tin profile hiện tại.
  Future<UserProfile> getProfile() async {
    final res = await http.get(
      Uri.parse('$baseUrl/users/me'),
      headers: _headers,
    );
    if (res.statusCode == 200) {
      return UserProfile.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
    }
    throw AuthException(_extractMessage(res, 'Không tải được thông tin.'));
  }

  /// Cập nhật họ tên / ngày sinh / địa chỉ.
  /// Backend chỉ chấp nhận 3 field này (UpdateProfileRequest).
  Future<UserProfile> updateProfile({
    required String name,
    DateTime? birthday,
    String? address,
  }) async {
    final res = await http.put(
      Uri.parse('$baseUrl/users/me'),
      headers: _headers,
      body: jsonEncode({
        'name': name,
        if (birthday != null) 'birthday': birthday.toIso8601String(),
        if (address != null) 'address': address,
      }),
    );
    if (res.statusCode == 200) {
      return UserProfile.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
    }
    throw AuthException(_extractMessage(res, 'Cập nhật thất bại.'));
  }

  /// Đổi mật khẩu. Backend trả 204 No Content khi thành công;
  /// 401 nếu CurrentPassword sai.
  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmNewPassword,
  }) async {
    final res = await http.put(
      Uri.parse('$baseUrl/users/me/password'),
      headers: _headers,
      body: jsonEncode({
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmNewPassword': confirmNewPassword,
      }),
    );
    // 200 hoặc 204 đều OK.
    if (res.statusCode == 200 || res.statusCode == 204) return;
    // 401 -> sai mật khẩu hiện tại, có message từ backend.
    throw AuthException(_extractMessage(res, 'Đổi mật khẩu thất bại.'));
  }

  /// Gộp 2 dạng lỗi backend: { "message": "..." } | ValidationProblemDetails.
  String _extractMessage(http.Response res, String fallback) {
    try {
      final body = jsonDecode(res.body);
      if (body is Map) {
        if (body['message'] != null) return body['message'].toString();
        final errors = body['errors'];
        if (errors is Map && errors.isNotEmpty) {
          return errors.values
              .expand((v) => (v is List) ? v.map((e) => '$e') : ['$v'])
              .join('\n');
        }
      }
    } catch (_) {}
    return fallback;
  }
}

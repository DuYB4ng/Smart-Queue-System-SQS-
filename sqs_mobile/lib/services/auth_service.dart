import 'dart:convert';
import 'package:flutter/foundation.dart' show debugPrint;
import 'package:http/http.dart' as http;

/// Kết quả trả về sau khi login/register thành công.
/// Khớp với response của POST /auth/login trong docs/api_spec.md
class AuthResult {
  final String token;
  final int userId;
  final String name;
  final String role;

  AuthResult({
    required this.token,
    required this.userId,
    required this.name,
    required this.role,
  });

  factory AuthResult.fromJson(Map<String, dynamic> json) {
    final user = json['user'] as Map<String, dynamic>;
    return AuthResult(
      token: json['token'] as String,
      userId: user['id'] as int,
      name: user['name'] as String,
      role: user['role'] as String,
    );
  }
}

/// Exception cho lỗi nghiệp vụ (email trùng, sai mật khẩu...) để UI
/// hiển thị đúng thông báo thay vì "Exception: ..." mặc định.
class AuthException implements Exception {
  final String message;
  AuthException(this.message);
  @override
  String toString() => message;
}

class AuthService {
  // Đồng bộ với baseUrl trong TicketTrackerPage (main.dart).
  // 10.0.2.2 = localhost khi chạy trên Android Emulator.
  static const String baseUrl = 'http://10.0.2.2:5000/api';

  Future<AuthResult> login({
    required String email,
    required String password,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );

    if (response.statusCode == 200) {
      return AuthResult.fromJson(jsonDecode(response.body));
    }
    throw AuthException(_extractMessage(response, 'Email hoặc mật khẩu không đúng.'));
  }

  Future<void> register({
    required String name,
    required String email,
    required String password,
    required String confirmPassword,
    required DateTime birthday,
    required String address,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'name': name,
        'email': email,
        'password': password,
        // Backend RegisterRequest có [Compare("Password")] trên ConfirmPassword
        // -> BẮT BUỘC gửi field này, thiếu là ModelState.IsValid = false ngay.
        'confirmPassword': confirmPassword,
        'birthday': birthday.toIso8601String().split('T').first,
        'address': address,
      }),
    );

    debugPrint('REGISTER STATUS: ${response.statusCode}'); // TODO: xóa sau khi hết debug
    debugPrint('REGISTER BODY: ${response.body}'); // TODO: xóa sau khi hết debug

    if (response.statusCode == 200 || response.statusCode == 201) {
      return;
    }
    throw AuthException(_extractMessage(response, 'Đăng ký thất bại. Vui lòng thử lại.'));
  }

  /// Backend trả lỗi ở 2 dạng:
  /// - { "message": "..." } cho Conflict(409)/500 (ném từ AuthService)
  /// - ValidationProblemDetails { "errors": { "Password": ["..."] , ... } }
  ///   cho BadRequest(400) do [Required]/[RegularExpression] fail.
  /// Hàm này gộp cả 2 dạng thành 1 chuỗi message dễ đọc.
  String _extractMessage(http.Response response, String fallback) {
    try {
      final body = jsonDecode(response.body);
      if (body is Map) {
        if (body['message'] != null) return body['message'] as String;
        final errors = body['errors'];
        if (errors is Map && errors.isNotEmpty) {
          return errors.values
              .expand((v) => (v is List) ? v.map((e) => e.toString()) : [v.toString()])
              .join('\n');
        }
      }
    } catch (_) {}
    return fallback;
  }
}
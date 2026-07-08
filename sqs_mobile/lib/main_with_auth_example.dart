// VÍ DỤ tích hợp Login/Register vào main.dart hiện có.
// Không ghi đè main.dart gốc — copy phần cần thiết vào file của bạn.

import 'package:flutter/material.dart';
import 'services/auth_service.dart';
import 'theme/app_theme.dart';
import 'screens/login_screen.dart';
// import 'main.dart' show TicketTrackerPage; // màn hình hiện có của bạn

void main() {
  runApp(const SQSMobileApp());
}

class SQSMobileApp extends StatelessWidget {
  const SQSMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SQS Mobile',
      theme: buildAppTheme(),
      home: const AuthGate(),
      debugShowCheckedModeBanner: false,
    );
  }
}

/// Quyết định hiển thị LoginScreen hay màn hình chính (TicketTrackerPage)
/// dựa trên việc đã đăng nhập hay chưa.
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  AuthResult? _authResult;
  bool _asGuest = false;

  @override
  Widget build(BuildContext context) {
    if (_authResult != null || _asGuest) {
      // TODO: thay bằng TicketTrackerPage(authResult: _authResult) sau khi
      // bạn thêm tham số nhận token vào TicketTrackerPage trong main.dart.
      return const Placeholder(); // <-- return TicketTrackerPage(...) ở đây
    }

    return LoginScreen(
      onLoginSuccess: (result) => setState(() => _authResult = result),
      onContinueAsGuest: () => setState(() => _asGuest = true),
    );
  }
}
import 'package:flutter/material.dart';

import 'theme/app_theme.dart';
import 'services/auth_service.dart';
import 'screens/login_screen.dart';
import 'navigation_screen.dart';

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

/// Cổng vào app: chưa đăng nhập -> LoginScreen, đã đăng nhập (hoặc chọn
/// "khách") -> MainNavigationScreen. Giữ state ở đây (session-only, mất khi
/// tắt app) — nếu cần nhớ đăng nhập lâu dài, thêm shared_preferences sau.
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  AuthResult? _authResult;
  bool _asGuest = false;

  void _handleLogout() {
    setState(() {
      _authResult = null;
      _asGuest = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_authResult != null || _asGuest) {
      return MainNavigationScreen(
        authResult: _authResult,
        onLogout: _handleLogout,
      );
    }

    return LoginScreen(
      onLoginSuccess: (result) => setState(() => _authResult = result),
      onContinueAsGuest: () => setState(() => _asGuest = true),
    );
  }
}

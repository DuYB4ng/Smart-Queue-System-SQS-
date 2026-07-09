import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../services/auth_service.dart';

class ProfileScreen extends StatelessWidget {
  final AuthResult? authResult;
  final VoidCallback onLogout;

  const ProfileScreen({
    super.key,
    required this.authResult,
    required this.onLogout,
  });

  bool get _isLoggedIn => authResult != null;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông tin cá nhân'),
        backgroundColor: AppColors.bgPrimary,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 16),
              Center(
                child: Container(
                  width: 96,
                  height: 96,
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(
                    gradient: AppColors.accentGradient,
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    _isLoggedIn && authResult!.name.isNotEmpty
                        ? authResult!.name[0].toUpperCase()
                        : '?',
                    style: const TextStyle(
                      fontSize: 36,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                _isLoggedIn ? authResult!.name : 'Khách',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                _isLoggedIn ? 'Tài khoản đã đăng nhập' : 'Đang dùng ở chế độ khách',
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 32),

              // TODO: nếu AuthResult có thêm field (email, mã sinh viên...),
              // thêm các dòng _InfoRow tương ứng ở đây.

              const Spacer(),
              OutlinedButton.icon(
                onPressed: () => _confirmLogout(context),
                icon: const Icon(Icons.logout),
                label: Text(_isLoggedIn ? 'Đăng xuất' : 'Rời chế độ khách'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.danger,
                  side: const BorderSide(color: AppColors.danger),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _confirmLogout(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc chắn muốn đăng xuất?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Hủy'),
          ),
          TextButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              onLogout();
            },
            child: const Text('Đăng xuất', style: TextStyle(color: AppColors.danger)),
          ),
        ],
      ),
    );
  }
}
import 'package:flutter/material.dart';
import 'theme/app_theme.dart';
import 'services/auth_service.dart';
import 'screens/take_number_screen.dart';
import 'screens/my_tickets_screen.dart';
import 'screens/profile_screen.dart';

class MainNavigationScreen extends StatefulWidget {
  final AuthResult? authResult;
  final VoidCallback onLogout;

  const MainNavigationScreen({
    super.key,
    required this.authResult,
    required this.onLogout,
  });

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final screens = [
      // Tab 0: Lấy số
      TakeNumberScreen(authResult: widget.authResult),

      // Tab 1: Vé của tôi (danh sách vé + trạng thái realtime)
      MyTicketsScreen(
        authResult: widget.authResult,
        onLogout: widget.onLogout,
      ),

      // Tab 2: Thông tin cá nhân
      ProfileScreen(
        authResult: widget.authResult,
        onLogout: widget.onLogout,
      ),
    ];

    return Scaffold(
      body: IndexedStack(index: _currentIndex, children: screens),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
        type: BottomNavigationBarType.fixed,
        selectedItemColor: AppColors.primary,
        unselectedItemColor: AppColors.textSecondary,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.confirmation_number_outlined),
            label: 'Lấy số',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.history_outlined),
            label: 'Trạng thái',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person_outline),
            label: 'Cá nhân',
          ),
        ],
      ),
    );
  }
}
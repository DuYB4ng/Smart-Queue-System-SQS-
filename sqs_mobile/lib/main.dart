import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:signalr_netcore/signalr_client.dart';

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
/// "khách") -> TicketTrackerPage. Giữ state ở đây (session-only, mất khi
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

class TicketTrackerPage extends StatefulWidget {
  final AuthResult? authResult;
  final VoidCallback onLogout;

  const TicketTrackerPage({
    super.key,
    required this.authResult,
    required this.onLogout,
  });

  @override
  State<TicketTrackerPage> createState() => _TicketTrackerPageState();
}

class _TicketTrackerPageState extends State<TicketTrackerPage> {
  final TextEditingController _ticketIdController = TextEditingController();

  HubConnection? _hubConnection;
  bool _isConnected = false;

  Map<String, dynamic>? _ticketData;
  String _statusMessage = 'Nhập Ticket ID của bạn (ví dụ: 1, 2, ...)';
  bool _isLoading = false;

  final String _baseUrl = 'http://10.0.2.2:5000'; // 10.0.2.2 is localhost for Android Emulator

  bool get _isLoggedIn => widget.authResult != null;

  /// Header dùng cho các API cần xác thực (khi bạn nối thêm
  /// GET /tickets/{id}/status, POST /tickets, v.v.)
  Map<String, String> get _authHeaders => {
        'Content-Type': 'application/json',
        if (_isLoggedIn) 'Authorization': 'Bearer ${widget.authResult!.token}',
      };

  @override
  void initState() {
    super.initState();
    _initSignalR();
  }

  Future<void> _initSignalR() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/queue')
        .withAutomaticReconnect()
        .build();

    _hubConnection?.onclose(({error}) {
      setState(() => _isConnected = false);
      debugPrint("SignalR Connection Closed");
    });

    _hubConnection?.on('TicketStatusChanged', _handleTicketStatusChanged);

    try {
      await _hubConnection?.start();
      setState(() => _isConnected = true);
      debugPrint("SignalR Connected!");
    } catch (e) {
      debugPrint("SignalR Connection Error: $e");
    }
  }

  void _handleTicketStatusChanged(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments.first as Map<String, dynamic>;

    // Only update if it's the ticket we're currently tracking
    if (_ticketData != null && data['ticketId'] == _ticketData!['ticketId']) {
      setState(() {
        _ticketData!['status'] = data['newStatus'];

        if (data['newStatus'] == 'Calling') {
          _statusMessage = data['message'] ?? 'Đến quầy ${data['counterName']} ngay!';
        } else if (data['newStatus'] == 'Completed') {
          _statusMessage = 'Đã phục vụ xong. Cảm ơn bạn!';
        } else if (data['newStatus'] == 'Canceled') {
          _statusMessage = data['message'] ?? 'Phiên đã bị hủy.';
        }
      });
    }
  }

  Future<void> _trackTicket() async {
    final ticketIdStr = _ticketIdController.text.trim();
    if (ticketIdStr.isEmpty) return;

    final ticketId = int.tryParse(ticketIdStr);
    if (ticketId == null) {
      setState(() => _statusMessage = 'Ticket ID phải là một số hợp lệ.');
      return;
    }

    setState(() {
      _isLoading = true;
      _statusMessage = 'Đang tìm kiếm...';
    });

    try {
      // Unsubscribe from previous group if any
      if (_ticketData != null && _isConnected) {
        await _hubConnection?.invoke('LeaveGroup', args: ['ticket-${_ticketData!['ticketId']}']);
      }

      if (_isConnected) {
        await _hubConnection?.invoke('JoinGroup', args: ['ticket-$ticketId']);

        setState(() {
          _ticketData = {
            'ticketId': ticketId,
            'status': 'Waiting (Tracking)',
          };
          _statusMessage = 'Đang theo dõi Ticket #$ticketId.\nHệ thống sẽ thông báo khi đến lượt bạn.';
          _isLoading = false;
        });
      } else {
        setState(() {
          _statusMessage = 'Không thể kết nối đến máy chủ thời gian thực.';
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _statusMessage = 'Có lỗi xảy ra: $e';
        _isLoading = false;
      });
    }
  }

  @override
  void dispose() {
    _hubConnection?.stop();
    _ticketIdController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          _isLoggedIn ? 'Xin chào, ${widget.authResult!.name}' : 'Theo dõi Xếp hàng (Khách)',
          style: const TextStyle(fontWeight: FontWeight.bold),
          overflow: TextOverflow.ellipsis,
        ),
        backgroundColor: AppColors.bgPrimary,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
        actions: [
  Padding(
    padding: const EdgeInsets.only(right: 16),
    child: Icon(
      _isConnected ? Icons.wifi : Icons.wifi_off,
      color: _isConnected ? AppColors.success : AppColors.danger,
    ),
  ),
],
      ),
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              width: 96,
              height: 96,
              alignment: Alignment.center,
              decoration: const BoxDecoration(
                gradient: AppColors.accentGradient,
                shape: BoxShape.circle,
              ),
              margin: const EdgeInsets.only(bottom: 8),
              child: const Icon(Icons.confirmation_number_rounded, size: 44, color: Colors.white),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _ticketIdController,
              keyboardType: TextInputType.number,
              style: const TextStyle(color: AppColors.textPrimary),
              decoration: InputDecoration(
                labelText: 'Nhập Ticket ID của bạn',
                prefixIcon: const Icon(Icons.search, color: AppColors.textSecondary),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.arrow_forward),
                  onPressed: _isLoading ? null : _trackTicket,
                ),
              ),
              onSubmitted: (_) => _isLoading ? null : _trackTicket(),
            ),
            const SizedBox(height: 32),

            if (_ticketData != null) ...[
              Container(
                padding: const EdgeInsets.all(24.0),
                decoration: BoxDecoration(
                  color: AppColors.cardBg,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: AppColors.border),
                ),
                child: Column(
                  children: [
                    Text(
                      'TICKET ID: ${_ticketData!['ticketId']}',
                      style: const TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                        color: AppColors.accentSecondary,
                      ),
                    ),
                    const Divider(height: 32, color: AppColors.border),
                    _buildStatusIcon(),
                    const SizedBox(height: 16),
                    Text(
                      _statusMessage,
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 18, color: AppColors.textPrimary),
                    ),
                  ],
                ),
              )
            ] else ...[
              Text(
                _statusMessage,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary, fontSize: 16),
              ),
            ]
          ],
        ),
      ),
    );
  }

  Widget _buildStatusIcon() {
    final status = _ticketData?['status'] ?? '';

    if (status == 'Calling') {
      return const Column(
        children: [
          Icon(Icons.campaign, size: 64, color: Colors.orange),
          SizedBox(height: 8),
          Text(
            'ĐẾN LƯỢT CỦA BẠN!',
            style: TextStyle(color: Colors.orange, fontWeight: FontWeight.bold, fontSize: 20),
          ),
        ],
      );
    } else if (status == 'Completed') {
      return const Icon(Icons.check_circle, size: 64, color: AppColors.success);
    } else if (status == 'Canceled') {
      return const Icon(Icons.cancel, size: 64, color: AppColors.danger);
    } else {
      return const CircularProgressIndicator(color: AppColors.accentPrimary);
    }
  }
}
import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:signalr_netcore/signalr_client.dart';

import '../theme/app_theme.dart';
import '../services/auth_service.dart';
import '../models/ticket_info.dart';

/// Tab "Trạng thái" — danh sách "Vé của tôi".
///
/// Giống MyTicketsPage của web: hiện toàn bộ vé của user, chia 2 nhóm
///   - Hôm nay: vé WalkIn tạo hôm nay + vé Appointment hẹn hôm nay
///   - Ngày khác: vé Appointment cho ngày khác
///
/// Realtime qua SignalR: lắng nghe `TicketStatusChanged` rồi refetch;
/// backup bằng poll 10s (như web) trong khi chưa nhận được event.
class MyTicketsScreen extends StatefulWidget {
  final AuthResult? authResult;
  final VoidCallback onLogout;

  const MyTicketsScreen({
    super.key,
    required this.authResult,
    required this.onLogout,
  });

  @override
  State<MyTicketsScreen> createState() => _MyTicketsScreenState();
}

class _MyTicketsScreenState extends State<MyTicketsScreen> {
  static const String _baseUrl = 'http://10.0.2.2:5000/api';
  static const String _hubUrl = 'http://10.0.2.2:5000/hubs/queue';

  List<TicketInfo> _tickets = [];
  bool _isLoading = true;
  String? _errorMessage;

  HubConnection? _hub;
  Timer? _pollTimer;

  // signalr_netcore dùng enum PascalCase (port từ C#): HubConnectionState.Connected.
  bool get _isHubConnected => _hub?.state == HubConnectionState.Connected;

  bool get _isLoggedIn => widget.authResult != null;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_isLoggedIn) 'Authorization': 'Bearer ${widget.authResult!.token}',
      };

  @override
  void initState() {
    super.initState();
    _fetchTickets();
    _initSignalR();
    // Poll 10s như web — backup cho trường hợp SignalR lag.
    _pollTimer = Timer.periodic(const Duration(seconds: 10), (_) => _fetchTickets());
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _hub?.stop();
    super.dispose();
  }

  // ---------------------------------------------------------------------------
  // DATA
  // ---------------------------------------------------------------------------

  Future<void> _fetchTickets() async {
    // Endpoint này cần [Authorize] -> khách không có vé của mình.
    if (!_isLoggedIn) {
      setState(() {
        _isLoading = false;
        _errorMessage = null;
      });
      return;
    }

    try {
      final res = await http.get(
        Uri.parse('$_baseUrl/tickets/my'),
        headers: _headers,
      );

      if (res.statusCode == 200) {
        final list = jsonDecode(res.body) as List;
        final tickets = list
            .map((e) => TicketInfo.fromJson(e as Map<String, dynamic>))
            .toList();
        // Mới nhất lên đầu.
        tickets.sort((a, b) => b.createdAt.compareTo(a.createdAt));
        setState(() {
          _tickets = tickets;
          _isLoading = false;
          _errorMessage = null;
        });
      } else {
        setState(() {
          _errorMessage = 'Không tải được vé (${res.statusCode}).';
          _isLoading = false;
        });
      }
    } catch (_) {
      setState(() {
        _errorMessage = 'Không thể kết nối đến máy chủ.';
        _isLoading = false;
      });
    }
  }

  Future<void> _cancelTicket(TicketInfo t) async {
    try {
      final res =
          await http.delete(Uri.parse('$_baseUrl/tickets/${t.ticketId}'), headers: _headers);
      if (res.statusCode == 200 || res.statusCode == 204) {
        _fetchTickets();
      } else {
        _toast(_extractMessage(res, 'Hủy vé thất bại (${res.statusCode}).'));
      }
    } catch (_) {
      _toast('Không thể kết nối đến máy chủ.');
    }
  }

  // ---------------------------------------------------------------------------
  // SIGNALR
  // ---------------------------------------------------------------------------

  Future<void> _initSignalR() async {
    _hub = HubConnectionBuilder()
        .withUrl(_hubUrl)
        .withAutomaticReconnect()
        .build();

    _hub?.on('TicketStatusChanged', _onStatusChanged);
    _hub?.on('QueueUpdated', _onQueueUpdated);

    try {
      await _hub?.start();
    } catch (e) {
      debugPrint('SignalR start error: $e');
    }
  }

  /// Khi 1 vé đổi trạng thái -> refetch toàn bộ (đơn giản, đúng dữ liệu).
  void _onStatusChanged(List<Object?>? args) => _fetchTickets();

  /// Khi hàng đợi thay đổi (vị trí trong hàng thay đổi) -> refetch.
  void _onQueueUpdated(List<Object?>? args) => _fetchTickets();

  // ---------------------------------------------------------------------------
  // HELPERS
  // ---------------------------------------------------------------------------

  String _extractMessage(http.Response res, String fallback) {
    try {
      final body = jsonDecode(res.body);
      if (body is Map && body['message'] != null) {
        return body['message'].toString();
      }
    } catch (_) {}
    return fallback;
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(msg)));
  }

  String _fmtTime(DateTime t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  String _fmtDate(DateTime t) =>
      '${t.day.toString().padLeft(2, '0')}/${t.month.toString().padLeft(2, '0')}/${t.year}';

  // ---------------------------------------------------------------------------
  // BUILD
  // ---------------------------------------------------------------------------

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          _isLoggedIn ? 'Vé của ${widget.authResult!.name}' : 'Vé của tôi',
          overflow: TextOverflow.ellipsis,
        ),
          actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16),
            child: Icon(
              _isHubConnected ? Icons.wifi : Icons.wifi_off,
              size: 20,
              color: _isHubConnected ? AppColors.success : AppColors.danger,
            ),
          ),
        ],
      ),
      body: SafeArea(child: _buildBody()),
    );
  }

  Widget _buildBody() {
    // Khách chưa đăng nhập -> không có "Vé của tôi".
    if (!_isLoggedIn) return _buildGuestView();

    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null && _tickets.isEmpty) {
      return _StateView(
        icon: Icons.cloud_off,
        message: _errorMessage!,
        action: ElevatedButton(
          onPressed: () {
            setState(() => _isLoading = true);
            _fetchTickets();
          },
          child: const Text('Thử lại'),
        ),
      );
    }

    if (_tickets.isEmpty) {
      return _StateView(
        icon: Icons.inbox_outlined,
        message: 'Bạn chưa có vé nào.\nHãy chuyển sang tab "Lấy số" để bắt đầu!',
      );
    }

    // Chia 2 nhóm: hôm nay / ngày khác.
    final now = DateTime.now();
    final today =
        _tickets.where((t) => t.isToday(now)).toList();
    final others =
        _tickets.where((t) => !t.isToday(now)).toList();

    return RefreshIndicator(
      onRefresh: _fetchTickets,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          if (today.isNotEmpty) ...[
            _GroupHeader(title: 'Hôm nay (${today.length})'),
            ...today.map((t) => _TicketCard(ticket: t)),
            const SizedBox(height: 8),
          ],
          if (others.isNotEmpty) ...[
            _GroupHeader(title: 'Ngày khác (${others.length})'),
            ...others.map((t) => _TicketCard(ticket: t)),
          ],
        ],
      ),
    );
  }

  Widget _buildGuestView() {
    return _StateView(
      icon: Icons.lock_outline,
      message:
          'Bạn đang ở chế độ khách.\nĐăng nhập để xem "Vé của tôi".',
    );
  }
}

// =============================================================================
// WIDGETS CON
// =============================================================================

/// Header chia nhóm "Hôm nay" / "Ngày khác".
class _GroupHeader extends StatelessWidget {
  final String title;
  const _GroupHeader({required this.title});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 12, bottom: 8),
      child: Text(
        title,
        style: const TextStyle(
          fontSize: 13,
          fontWeight: FontWeight.w800,
          color: AppColors.textSecondary,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

/// 1 card vé — layout giống MyTicketsPage trên web:
/// badge loại + thời gian | tên dịch vụ | số thứ tự/ngày hẹn | trạng thái + nút hủy.
class _TicketCard extends StatelessWidget {
  final TicketInfo ticket;
  const _TicketCard({required this.ticket});

  @override
  Widget build(BuildContext context) {
    final style = TicketStatusStyle.from(ticket.status);
    final isAppointment = ticket.ticketType == 'Appointment';
    final state = context.findAncestorStateOfType<_MyTicketsScreenState>()!;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.cardBg,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Dòng 1: badge loại + thời gian tạo
          Row(
            children: [
              _TypeBadge(appointment: isAppointment),
              const SizedBox(width: 8),
              Text(
                isAppointment
                    ? 'Hẹn ${state._fmtDate(ticket.appointmentDate ?? ticket.createdAt)}'
                    : state._fmtTime(ticket.createdAt),
                style: const TextStyle(
                    fontSize: 12.5, color: AppColors.textSecondary),
              ),
              const Spacer(),
              Text('#${ticket.ticketId}',
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.textSecondary)),
            ],
          ),
          const SizedBox(height: 12),

          // Dòng 2: tên dịch vụ
          Text(
            ticket.serviceName,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 12),

          // Dòng 3: số thứ tự (WalkIn) hoặc ngày hẹn chi tiết (Appointment)
          if (isAppointment)
            _InfoRow(
              icon: Icons.event_rounded,
              text: state._fmtDate(ticket.appointmentDate ?? ticket.createdAt),
            )
          else
            Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  ticket.ticketNumber ?? '---',
                  style: const TextStyle(
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                    color: AppColors.primary,
                    height: 1,
                  ),
                ),
                const SizedBox(width: 8),
                const Padding(
                  padding: EdgeInsets.only(bottom: 4),
                  child: Text('Số thứ tự',
                      style: TextStyle(
                          fontSize: 12, color: AppColors.textSecondary)),
                ),
              ],
            ),
          const SizedBox(height: 12),

          // Dòng 4: trạng thái + chi tiết vị trí / quầy
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: style.color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                Icon(style.icon, color: style.color, size: 18),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _statusDetail(ticket, style),
                    style: TextStyle(
                        color: style.color, fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
          ),

          // Dòng 5: nút hủy (chỉ khi Waiting)
          if (ticket.status == 'Waiting') ...[
            const SizedBox(height: 12),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                onPressed: () => _confirmCancel(context, state),
                icon: const Icon(Icons.close, size: 18),
                label: const Text('Hủy số'),
                style: TextButton.styleFrom(foregroundColor: AppColors.danger),
              ),
            ),
          ],
        ],
      ),
    );
  }

  String _statusDetail(TicketInfo t, TicketStatusStyle style) {
    switch (t.status) {
      case 'Waiting':
        if (t.ticketType == 'Appointment') return style.label;
        final pos = t.queuePosition;
        return pos != null && pos > 0
            ? '${style.label} · Còn $pos người trước bạn'
            : 'Sắp đến lượt bạn';
      case 'Calling':
        return t.counterName != null
            ? '${style.label} · Vui lòng đến ${t.counterName}'
            : style.label;
      default:
        return style.label;
    }
  }

  void _confirmCancel(BuildContext context, _MyTicketsScreenState state) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hủy số'),
        content: Text(
            'Hủy vé #${ticket.ticketId} "${ticket.serviceName}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Không'),
          ),
          TextButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              state._cancelTicket(ticket);
            },
            child: const Text('Hủy số',
                style: TextStyle(color: AppColors.danger)),
          ),
        ],
      ),
    );
  }
}

class _TypeBadge extends StatelessWidget {
  final bool appointment;
  const _TypeBadge({required this.appointment});

  @override
  Widget build(BuildContext context) {
    final color = appointment ? AppColors.primaryLight : AppColors.warning;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(appointment ? Icons.event_outlined : Icons.bolt_rounded,
              size: 12, color: color),
          const SizedBox(width: 4),
          Text(
            appointment ? 'Hẹn trước' : 'Lấy ngay',
            style: TextStyle(
                fontSize: 11, fontWeight: FontWeight.w700, color: color),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String text;
  const _InfoRow({required this.icon, required this.text});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 16, color: AppColors.textSecondary),
        const SizedBox(width: 6),
        Text(text,
            style: const TextStyle(
                fontSize: 13.5, color: AppColors.textPrimary)),
      ],
    );
  }
}

/// Trạng thái rỗng / lỗi / khách.
class _StateView extends StatelessWidget {
  final IconData icon;
  final String message;
  final Widget? action;
  const _StateView({required this.icon, required this.message, this.action});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 64, color: AppColors.textSecondary),
            const SizedBox(height: 16),
            Text(
              message,
              textAlign: TextAlign.center,
              style:
                  const TextStyle(color: AppColors.textSecondary, fontSize: 15),
            ),
            if (action != null) ...[
              const SizedBox(height: 20),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}

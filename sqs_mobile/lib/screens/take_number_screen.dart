import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../theme/app_theme.dart';
import '../services/auth_service.dart';
import '../models/service_model.dart';
import '../models/ticket_response.dart';

/// Tab "Lấy số" — cho phép khách hàng:
///  - Chọn dịch vụ (lấy từ GET /api/services)
///  - Lấy số ngay (Walk-in)      -> POST /api/tickets | /api/tickets/guest
///  - Đặt hẹn trước (Appointment) -> POST /api/tickets/appointment[.../guest]
///
/// Luồng đồng bộ với web: KioskPage (walk-in) + RegisterTicketPage (cả 2 loại).
class TakeNumberScreen extends StatefulWidget {
  final AuthResult? authResult;

  const TakeNumberScreen({super.key, required this.authResult});

  @override
  State<TakeNumberScreen> createState() => _TakeNumberScreenState();
}

class _TakeNumberScreenState extends State<TakeNumberScreen> {
  // 10.0.2.2 = localhost khi chạy trên Android Emulator trỏ tới máy host.
  static const String _baseUrl = 'http://10.0.2.2:5000/api';

  // --- State dữ liệu ---
  List<Service> _services = const [];
  Service? _selectedService;

  // --- State form ---
  TicketType _ticketType = TicketType.walkIn;
  final _guestNameController = TextEditingController();
  final _studentIdController = TextEditingController();
  final _phoneController = TextEditingController();
  final _noteController = TextEditingController();
  DateTime? _appointmentDate;

  // --- State UI ---
  bool _isLoadingServices = true;
  bool _isSubmitting = false;
  String? _errorMessage;

  // --- State kết quả ---
  // Chỉ 1 trong 2 != null tại một thời điểm.
  CreateTicketResponse? _ticketResult;
  AppointmentResponse? _appointmentResult;

  bool get _isLoggedIn => widget.authResult != null;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_isLoggedIn) 'Authorization': 'Bearer ${widget.authResult!.token}',
      };

  @override
  void initState() {
    super.initState();
    _fetchServices();
  }

  @override
  void dispose() {
    _guestNameController.dispose();
    _studentIdController.dispose();
    _phoneController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  // ---------------------------------------------------------------------------
  // API CALLS
  // ---------------------------------------------------------------------------

  /// Lấy danh sách dịch vụ đang hoạt động.
  /// Endpoint: GET /api/services (public, không cần auth).
  Future<void> _fetchServices() async {
    setState(() {
      _isLoadingServices = true;
      _errorMessage = null;
    });
    try {
      final res = await http.get(Uri.parse('$_baseUrl/services'));
      if (res.statusCode == 200) {
        final list = jsonDecode(res.body) as List;
        setState(() {
          _services = list
              .map((e) => Service.fromJson(e as Map<String, dynamic>))
              .toList();
          _isLoadingServices = false;
        });
      } else {
        setState(() {
          _errorMessage = 'Không tải được danh sách dịch vụ (${res.statusCode}).';
          _isLoadingServices = false;
        });
      }
    } catch (_) {
      setState(() {
        _errorMessage = 'Không thể kết nối đến máy chủ.';
        _isLoadingServices = false;
      });
    }
  }

  /// Gửi request tạo vé. Phân nhánh theo TicketType + login/guest,
  /// đúng 4 endpoint của TicketsController.
  Future<void> _submit() async {
    // --- Validate ---
    if (_selectedService == null) {
      _error('Vui lòng chọn dịch vụ.');
      return;
    }
    final guestName = _guestNameController.text.trim();
    if (!_isLoggedIn && guestName.isEmpty) {
      _error('Vui lòng nhập tên của bạn.');
      return;
    }
    if (_ticketType == TicketType.appointment && _appointmentDate == null) {
      _error('Vui lòng chọn ngày hẹn.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final response = _ticketType == TicketType.walkIn
          ? await _createWalkIn(guestName)
          : await _createAppointment(guestName);

      if (!mounted) return;
      setState(() {
        _isSubmitting = false;
        if (_ticketType == TicketType.walkIn) {
          _ticketResult = response as CreateTicketResponse;
        } else {
          _appointmentResult = response as AppointmentResponse;
        }
      });
    } on _ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.message;
        _isSubmitting = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _errorMessage = 'Không thể kết nối đến máy chủ.';
        _isSubmitting = false;
      });
    }
  }

  /// Walk-in: chọn endpoint theo login/guest.
  ///  - Đã login: POST /api/tickets         -> body { serviceId }
  ///  - Khách   : POST /api/tickets/guest   -> body { serviceId, guestName }
  Future<CreateTicketResponse> _createWalkIn(String guestName) async {
    final endpoint = _isLoggedIn ? '/tickets' : '/tickets/guest';
    final body = <String, dynamic>{'serviceId': _selectedService!.id};
    if (!_isLoggedIn) body['guestName'] = guestName;

    return CreateTicketResponse.fromJson(await _postJson(endpoint, body));
  }

  /// Appointment: chọn endpoint theo login/guest.
  ///  - Đã login: POST /api/tickets/appointment
  ///  - Khách   : POST /api/tickets/appointment/guest (cần thêm guestName)
  Future<AppointmentResponse> _createAppointment(String guestName) async {
    final endpoint =
        _isLoggedIn ? '/tickets/appointment' : '/tickets/appointment/guest';
    final body = <String, dynamic>{
      'serviceId': _selectedService!.id,
      // Backend lưu DateTime; gửi ISO 8601 an toàn nhất.
      'appointmentDate': _appointmentDate!.toIso8601String(),
      if (_studentIdController.text.trim().isNotEmpty)
        'studentId': _studentIdController.text.trim(),
      if (_phoneController.text.trim().isNotEmpty)
        'phoneNumber': _phoneController.text.trim(),
      if (_noteController.text.trim().isNotEmpty)
        'note': _noteController.text.trim(),
      if (!_isLoggedIn) 'guestName': guestName,
    };

    return AppointmentResponse.fromJson(await _postJson(endpoint, body));
  }

  /// Helper POST trả Map JSON hoặc ném _ApiException kèm message backend.
  Future<Map<String, dynamic>> _postJson(
      String endpoint, Map<String, dynamic> body) async {
    final res = await http.post(
      Uri.parse('$_baseUrl$endpoint'),
      headers: _headers,
      body: jsonEncode(body),
    );

    if (res.statusCode == 200 || res.statusCode == 201) {
      return jsonDecode(res.body) as Map<String, dynamic>;
    }
    throw _ApiException(_extractMessage(res, 'Thao tác thất bại (${res.statusCode}).'));
  }

  // Gộp message lỗi từ backend (2 dạng: {message} hoặc ValidationProblemDetails).
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

  void _error(String msg) => setState(() => _errorMessage = msg);

  void _reset() {
    setState(() {
      _ticketResult = null;
      _appointmentResult = null;
      _selectedService = null;
      _ticketType = TicketType.walkIn;
      _appointmentDate = null;
      _errorMessage = null;
      _guestNameController.clear();
      _studentIdController.clear();
      _phoneController.clear();
      _noteController.clear();
    });
  }

  // ---------------------------------------------------------------------------
  // BUILD
  // ---------------------------------------------------------------------------

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Lấy số')),
      body: SafeArea(
        child: _hasResult
            ? _buildResultView()
            : RefreshIndicator(
                onRefresh: _fetchServices,
                child: _buildFormView(),
              ),
      ),
    );
  }

  bool get _hasResult =>
      _ticketResult != null || _appointmentResult != null;

  // --- FORM VIEW -----------------------------------------------------------

  Widget _buildFormView() {
    if (_isLoadingServices) {
      return const Center(child: CircularProgressIndicator());
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Header(),
          const SizedBox(height: 20),

          if (_errorMessage != null) ...[
            _ErrorBanner(message: _errorMessage!),
            const SizedBox(height: 16),
          ],

          // 1. Chọn dịch vụ
          _SectionTitle(text: '1. Chọn dịch vụ'),
          const SizedBox(height: 10),
          ..._services.map((s) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: _ServiceCard(
                  service: s,
                  selected: _selectedService?.id == s.id,
                  onTap: () => setState(() => _selectedService = s),
                ),
              )),

          const SizedBox(height: 20),

          // 2. Chọn loại vé
          _SectionTitle(text: '2. Chọn loại'),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _TypeCard(
                  icon: Icons.access_time_rounded,
                  label: 'Lấy số ngay',
                  selected: _ticketType == TicketType.walkIn,
                  onTap: () => setState(() => _ticketType = TicketType.walkIn),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _TypeCard(
                  icon: Icons.event_rounded,
                  label: 'Đặt hẹn trước',
                  selected: _ticketType == TicketType.appointment,
                  onTap: () => setState(
                      () => _ticketType = TicketType.appointment),
                ),
              ),
            ],
          ),

          const SizedBox(height: 20),

          // 3. Thông tin bổ sung (guest name / appointment fields)
          _buildExtraFields(),

          const SizedBox(height: 24),

          // Nút submit
          ElevatedButton(
            onPressed: _isSubmitting ? null : _submit,
            child: _isSubmitting
                ? const SizedBox(
                    height: 22,
                    width: 22,
                    child: CircularProgressIndicator(
                        strokeWidth: 2.5, color: Colors.white),
                  )
                : Text(_ticketType == TicketType.walkIn
                    ? 'Lấy số'
                    : 'Đặt lịch hẹn'),
          ),
        ],
      ),
    );
  }

  /// Các field tuỳ theo tình huống:
  ///  - Guest luôn cần tên.
  ///  - Appointment cần thêm ngày hẹn (+ MSSV/SĐT/ghi chú tuỳ chọn).
  Widget _buildExtraFields() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Tên khách — chỉ bắt buộc khi chưa đăng nhập.
        if (!_isLoggedIn) ...[
          _SectionTitle(text: '3. Thông tin của bạn'),
          const SizedBox(height: 10),
          TextField(
            controller: _guestNameController,
            style: const TextStyle(color: AppColors.textPrimary),
            decoration: const InputDecoration(
              labelText: 'Họ và tên',
              prefixIcon:
                  Icon(Icons.person_outline, color: AppColors.textSecondary),
            ),
          ),
          const SizedBox(height: 16),
        ],

        // Appointment-only fields.
        if (_ticketType == TicketType.appointment) ...[
          if (_isLoggedIn) const _SectionTitle(text: '3. Thông tin hẹn'),
          if (_isLoggedIn) const SizedBox(height: 10),
          InkWell(
            onTap: _pickAppointmentDate,
            borderRadius: BorderRadius.circular(14),
            child: InputDecorator(
              decoration: const InputDecoration(
                labelText: 'Ngày hẹn',
                prefixIcon:
                    Icon(Icons.calendar_today, color: AppColors.textSecondary),
              ),
              child: Text(
                _appointmentDate == null
                    ? 'Chọn ngày hẹn'
                    : _formatDate(_appointmentDate!),
                style: TextStyle(
                  color: _appointmentDate == null
                      ? AppColors.textSecondary
                      : AppColors.textPrimary,
                ),
              ),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _studentIdController,
            style: const TextStyle(color: AppColors.textPrimary),
            decoration: const InputDecoration(
              labelText: 'Mã sinh viên (tuỳ chọn)',
              prefixIcon:
                  Icon(Icons.badge_outlined, color: AppColors.textSecondary),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _phoneController,
            keyboardType: TextInputType.phone,
            style: const TextStyle(color: AppColors.textPrimary),
            decoration: const InputDecoration(
              labelText: 'Số điện thoại (tuỳ chọn)',
              prefixIcon:
                  Icon(Icons.phone_outlined, color: AppColors.textSecondary),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _noteController,
            maxLines: 2,
            style: const TextStyle(color: AppColors.textPrimary),
            decoration: const InputDecoration(
              labelText: 'Ghi chú (tuỳ chọn)',
              prefixIcon:
                  Icon(Icons.note_outlined, color: AppColors.textSecondary),
            ),
          ),
        ],
      ],
    );
  }

  Future<void> _pickAppointmentDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _appointmentDate ?? now,
      firstDate: now, // không cho chọn ngày quá khứ
      lastDate: DateTime(now.year + 1),
    );
    if (picked != null) setState(() => _appointmentDate = picked);
  }

  String _formatDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/'
      '${d.month.toString().padLeft(2, '0')}/${d.year}';

  // --- RESULT VIEW ---------------------------------------------------------

  Widget _buildResultView() {
    if (_ticketResult != null) return _buildWalkInResult(_ticketResult!);
    return _buildAppointmentResult(_appointmentResult!);
  }

  Widget _buildWalkInResult(CreateTicketResponse t) {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.check_circle,
                color: AppColors.success, size: 72),
            const SizedBox(height: 16),
            const Text(
              'Lấy số thành công!',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                color: AppColors.primary,
              ),
            ),
            const SizedBox(height: 24),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 20),
              decoration: BoxDecoration(
                color: AppColors.boxBg,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                children: [
                  const Text('Số thứ tự của bạn',
                      style: TextStyle(color: AppColors.textSecondary)),
                  const SizedBox(height: 8),
                  Text(
                    t.ticketNumber ?? '---',
                    style: const TextStyle(
                      fontSize: 48,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text('Dịch vụ: ${t.serviceName}',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary)),
                  const SizedBox(height: 8),
                  Text(
                    t.estimatedWait != null && t.estimatedWait! > 0
                        ? 'Đang có ${t.estimatedWait} người chờ trước bạn'
                        : 'Sắp đến lượt bạn',
                    style: const TextStyle(color: AppColors.warning),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _reset,
              child: const Text('Lấy số khác'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAppointmentResult(AppointmentResponse a) {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.event_available,
                color: AppColors.success, size: 72),
            const SizedBox(height: 16),
            const Text(
              'Đặt hẹn thành công!',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                color: AppColors.primary,
              ),
            ),
            const SizedBox(height: 24),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 20),
              decoration: BoxDecoration(
                color: AppColors.boxBg,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                children: [
                  const Text('Ngày hẹn',
                      style: TextStyle(color: AppColors.textSecondary)),
                  const SizedBox(height: 8),
                  Text(
                    _formatDate(a.appointmentDate),
                    style: const TextStyle(
                      fontSize: 30,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text('Dịch vụ: ${a.serviceName}',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary)),
                  if (a.note != null && a.note!.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Text('Ghi chú: ${a.note}',
                        style: const TextStyle(color: AppColors.textSecondary)),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _reset,
              child: const Text('Đặt hẹn khác'),
            ),
          ],
        ),
      ),
    );
  }
}

// =============================================================================
// WIDGETS CON (private)
// =============================================================================

class _Header extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          width: 88,
          height: 88,
          alignment: Alignment.center,
          margin: const EdgeInsets.only(bottom: 16),
          decoration: const BoxDecoration(
            gradient: AppColors.accentGradient,
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.confirmation_number_rounded,
              size: 40, color: Colors.white),
        ),
        const Text(
          'Lấy số xếp hàng',
          textAlign: TextAlign.center,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
      ],
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String text;
  const _SectionTitle({required this.text});

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: const TextStyle(
        fontSize: 15,
        fontWeight: FontWeight.w700,
        color: AppColors.textPrimary,
      ),
    );
  }
}

class _ServiceCard extends StatelessWidget {
  final Service service;
  final bool selected;
  final VoidCallback onTap;

  const _ServiceCard({
    required this.service,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: selected
              ? AppColors.primary.withValues(alpha: 0.08)
              : AppColors.boxBg,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? AppColors.primary : AppColors.border,
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Icon(
              selected ? Icons.radio_button_checked : Icons.radio_button_off,
              color: selected ? AppColors.primary : AppColors.textSecondary,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    service.name,
                    style: const TextStyle(
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  if (service.description != null &&
                      service.description!.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        service.description!,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontSize: 12.5, color: AppColors.textSecondary),
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                service.code,
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: AppColors.primary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TypeCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _TypeCard({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 12),
        decoration: BoxDecoration(
          color: selected
              ? AppColors.primary.withValues(alpha: 0.08)
              : AppColors.boxBg,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? AppColors.primary : AppColors.border,
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Column(
          children: [
            Icon(icon,
                color: selected ? AppColors.primary : AppColors.textSecondary),
            const SizedBox(height: 8),
            Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: selected ? AppColors.primary : AppColors.textPrimary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  final String message;
  const _ErrorBanner({required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: AppColors.danger.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.danger.withValues(alpha: 0.4)),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline, color: AppColors.danger, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(message,
                style:
                    const TextStyle(color: AppColors.danger, fontSize: 13.5)),
          ),
        ],
      ),
    );
  }
}

/// Exception nội bộ mang message sạch từ backend để UI hiển thị.
class _ApiException implements Exception {
  final String message;
  _ApiException(this.message);
  @override
  String toString() => message;
}

import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../theme/app_theme.dart';
import '../services/auth_service.dart';

class TakeNumberScreen extends StatefulWidget {
  final AuthResult? authResult;

  const TakeNumberScreen({super.key, required this.authResult});

  @override
  State<TakeNumberScreen> createState() => _TakeNumberScreenState();
}

class _TakeNumberScreenState extends State<TakeNumberScreen> {
  final String _baseUrl = 'http://10.0.2.2:5000';

  // TODO: thay bằng danh sách dịch vụ thật, lấy từ API (GET /services)
  final List<String> _services = const [
    'Đăng ký học phần',
    'Xác nhận sinh viên',
    'Cấp bảng điểm',
  ];

  String? _selectedService;
  bool _isLoading = false;
  String? _errorMessage;
  Map<String, dynamic>? _ticketResult;

  Map<String, String> get _authHeaders => {
        'Content-Type': 'application/json',
        if (widget.authResult != null)
          'Authorization': 'Bearer ${widget.authResult!.token}',
      };

  Future<void> _takeNumber() async {
    if (_selectedService == null) {
      setState(() => _errorMessage = 'Vui lòng chọn dịch vụ');
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      // TODO: sửa endpoint + body theo đúng API backend của bạn
      final response = await http.post(
        Uri.parse('$_baseUrl/tickets'),
        headers: _authHeaders,
        body: jsonEncode({'serviceName': _selectedService}),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        setState(() {
          _ticketResult = data;
          _isLoading = false;
        });
      } else {
        setState(() {
          _errorMessage = 'Lấy số thất bại (${response.statusCode})';
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'Không thể kết nối đến máy chủ.';
        _isLoading = false;
      });
    }
  }

  void _reset() {
    setState(() {
      _ticketResult = null;
      _selectedService = null;
      _errorMessage = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lấy số'),
        backgroundColor: AppColors.bgPrimary,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: _ticketResult != null ? _buildResultView() : _buildFormView(),
        ),
      ),
    );
  }

  Widget _buildFormView() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: 16),
        Container(
          width: 88,
          height: 88,
          alignment: Alignment.center,
          margin: const EdgeInsets.only(bottom: 24),
          decoration: const BoxDecoration(
            gradient: AppColors.accentGradient,
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.confirmation_number_rounded,
              size: 40, color: Colors.white),
        ),
        const Text(
          'Chọn dịch vụ cần lấy số',
          textAlign: TextAlign.center,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 20),

        if (_errorMessage != null) ...[
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            decoration: BoxDecoration(
              color: AppColors.danger.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(_errorMessage!,
                style: const TextStyle(color: AppColors.danger)),
          ),
          const SizedBox(height: 16),
        ],

        ..._services.map((service) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _ServiceOption(
                label: service,
                selected: _selectedService == service,
                onTap: () => setState(() => _selectedService = service),
              ),
            )),

        const SizedBox(height: 12),
        ElevatedButton(
          onPressed: _isLoading ? null : _takeNumber,
          child: _isLoading
              ? const SizedBox(
                  height: 22,
                  width: 22,
                  child: CircularProgressIndicator(
                      strokeWidth: 2.5, color: Colors.white),
                )
              : const Text('Lấy số'),
        ),
      ],
    );
  }

  Widget _buildResultView() {
    final data = _ticketResult!;
    // TODO: đổi tên field cho khớp response thật của API
    final number = data['number']?.toString() ?? data['ticketId']?.toString() ?? '---';
    final service = data['serviceName']?.toString() ?? _selectedService ?? '';
    final peopleAhead = data['peopleAhead']?.toString() ?? '0';

    return Center(
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.check_circle, color: AppColors.success, size: 72),
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
                    number,
                    style: const TextStyle(
                      fontSize: 48,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text('Dịch vụ: $service',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary)),
                  const SizedBox(height: 8),
                  Text('Đang có $peopleAhead người chờ trước bạn',
                      style: const TextStyle(color: AppColors.warning)),
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
}

class _ServiceOption extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _ServiceOption({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary.withValues(alpha: 0.08) : AppColors.boxBg,
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
            Text(label, style: const TextStyle(color: AppColors.textPrimary)),
          ],
        ),
      ),
    );
  }
}
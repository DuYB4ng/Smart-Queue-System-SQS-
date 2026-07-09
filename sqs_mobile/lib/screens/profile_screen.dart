import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../services/auth_service.dart';
import '../services/user_service.dart';
import '../models/user_profile.dart';
import 'change_password_screen.dart';

/// Tab "Cá nhân".
/// - Khách (chưa login): chỉ hiện nút đăng xuất / rời chế độ khách.
/// - Đã login: xem + sửa (họ tên, ngày sinh, địa chỉ), link sang Đổi mật khẩu.
class ProfileScreen extends StatefulWidget {
  final AuthResult? authResult;
  final VoidCallback onLogout;

  const ProfileScreen({
    super.key,
    required this.authResult,
    required this.onLogout,
  });

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  bool get _isLoggedIn => widget.authResult != null;
  UserService? get _userService => _isLoggedIn
      ? UserService(widget.authResult!.token)
      : null;

  UserProfile? _profile;
  bool _isLoading = true;
  bool _isEditing = false;
  String? _errorMessage;

  // Controllers cho chế độ sửa.
  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  DateTime? _birthday;

  @override
  void initState() {
    super.initState();
    if (_isLoggedIn) _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final p = await _userService!.getProfile();
      setState(() {
        _profile = p;
        _isLoading = false;
      });
    } on AuthException catch (e) {
      setState(() {
        _errorMessage = e.message;
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _errorMessage = 'Không thể kết nối đến máy chủ.';
        _isLoading = false;
      });
    }
  }

  void _startEdit() {
    final p = _profile!;
    _nameController.text = p.name;
    _addressController.text = p.address ?? '';
    _birthday = p.birthday;
    setState(() {
      _isEditing = true;
      _errorMessage = null;
    });
  }

  Future<void> _saveEdit() async {
    if (_nameController.text.trim().length < 2) {
      setState(() => _errorMessage = 'Họ tên phải từ 2 ký tự.');
      return;
    }
    setState(() => _errorMessage = null);

    try {
      final updated = await _userService!.updateProfile(
        name: _nameController.text.trim(),
        birthday: _birthday,
        address: _addressController.text.trim(),
      );
      setState(() {
        _profile = updated;
        _isEditing = false;
      });
      _toast('Đã lưu thay đổi');
    } on AuthException catch (e) {
      setState(() => _errorMessage = e.message);
    } catch (_) {
      setState(() => _errorMessage = 'Không thể kết nối đến máy chủ.');
    }
  }

  Future<void> _pickBirthday() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _birthday ?? DateTime(now.year - 18),
      firstDate: DateTime(1950),
      lastDate: now,
    );
    if (picked != null) setState(() => _birthday = picked);
  }

  String _fmtDate(DateTime? d) => d == null
      ? '—'
      : '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  void _toast(String msg) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));

  // -------------------------------------------------------------------------
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Thông tin cá nhân')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _fetchProfile,
          child: _isLoggedIn ? _buildLoggedIn() : _buildGuest(),
        ),
      ),
    );
  }

  // --- GUEST ---------------------------------------------------------------
  Widget _buildGuest() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      physics: const AlwaysScrollableScrollPhysics(),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 24),
          _Avatar(name: 'Khách'),
          const SizedBox(height: 16),
          const Text(
            'Khách',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          const Text(
            'Bạn đang dùng ở chế độ khách.\nĐăng nhập để xem & quản lý thông tin.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 32),
          OutlinedButton.icon(
            onPressed: widget.onLogout,
            icon: const Icon(Icons.login),
            label: const Text('Đăng nhập'),
            style: OutlinedButton.styleFrom(foregroundColor: AppColors.primary),
          ),
        ],
      ),
    );
  }

  // --- LOGGED IN -----------------------------------------------------------
  Widget _buildLoggedIn() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      physics: const AlwaysScrollableScrollPhysics(),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Avatar(name: _profile?.name ?? widget.authResult!.name),
          const SizedBox(height: 12),
          Text(
            _profile?.name ?? widget.authResult!.name,
            textAlign: TextAlign.center,
            style: const TextStyle(
                fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Text(
            _profile?.email ?? widget.authResult!.name,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 24),

          if (_errorMessage != null) ...[
            _ErrorBanner(message: _errorMessage!),
            const SizedBox(height: 16),
          ],

          _isEditing ? _buildEditForm() : _buildViewForm(),

          const SizedBox(height: 16),
          _buildActionButtons(),
        ],
      ),
    );
  }

  /// Chế độ xem — Email khoá (chỉ đọc).
  Widget _buildViewForm() {
    return _Card(
      children: [
        _RowInfo(
            icon: Icons.person_outline, label: 'Họ và tên', value: _profile?.name ?? '—'),
        _RowInfo(
            icon: Icons.mail_outline,
            label: 'Email',
            value: _profile?.email ?? '—'),
        _RowInfo(
            icon: Icons.cake_outlined,
            label: 'Ngày sinh',
            value: _fmtDate(_profile?.birthday)),
        _RowInfo(
            icon: Icons.location_on_outlined,
            label: 'Địa chỉ',
            value: _profile?.address?.isNotEmpty == true
                ? _profile!.address!
                : '—'),
        _RowInfo(
            icon: Icons.verified_user_outlined,
            label: 'Vai trò',
            value: _roleLabel(_profile?.role ?? 'Customer')),
      ],
    );
  }

  /// Chế độ sửa — không cho sửa Email (backend không hỗ trợ đổi email).
  Widget _buildEditForm() {
    return _Card(
      children: [
        const Padding(
          padding: EdgeInsets.only(bottom: 8),
          child: Text('Chỉnh sửa thông tin',
              style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary)),
        ),
        TextField(
          controller: _nameController,
          style: const TextStyle(color: AppColors.textPrimary),
          decoration: const InputDecoration(
            labelText: 'Họ và tên',
            prefixIcon:
                Icon(Icons.person_outline, color: AppColors.textSecondary),
          ),
        ),
        const SizedBox(height: 12),
        InkWell(
          onTap: _pickBirthday,
          borderRadius: BorderRadius.circular(14),
          child: InputDecorator(
            decoration: const InputDecoration(
              labelText: 'Ngày sinh',
              prefixIcon:
                  Icon(Icons.cake_outlined, color: AppColors.textSecondary),
            ),
            child: Text(
              _birthday == null ? 'Chọn ngày sinh' : _fmtDate(_birthday),
              style: TextStyle(
                color: _birthday == null
                    ? AppColors.textSecondary
                    : AppColors.textPrimary,
              ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _addressController,
          maxLines: 2,
          style: const TextStyle(color: AppColors.textPrimary),
          decoration: const InputDecoration(
            labelText: 'Địa chỉ',
            prefixIcon: Icon(Icons.location_on_outlined,
                color: AppColors.textSecondary),
          ),
        ),
      ],
    );
  }

  Widget _buildActionButtons() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Nút sửa/lưu
        if (_isEditing) ...[
          ElevatedButton(
            onPressed: _saveEdit,
            child: const Text('Lưu thay đổi'),
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: () => setState(() {
              _isEditing = false;
              _errorMessage = null;
            }),
            child: const Text('Huỷ'),
          ),
        ] else ...[
          OutlinedButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined),
            label: const Text('Sửa thông tin'),
          ),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => ChangePasswordScreen(
                  token: widget.authResult!.token,
                ),
              ),
            ),
            icon: const Icon(Icons.lock_outline),
            label: const Text('Đổi mật khẩu'),
          ),
        ],
        const SizedBox(height: 16),
        const Divider(),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: () => _confirmLogout(context),
          icon: const Icon(Icons.logout),
          label: const Text('Đăng xuất'),
          style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
        ),
      ],
    );
  }

  String _roleLabel(String role) {
    switch (role) {
      case 'Admin':
        return 'Quản trị viên';
      case 'Staff':
        return 'Nhân viên';
      default:
        return 'Khách hàng';
    }
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
              widget.onLogout();
            },
            child: const Text('Đăng xuất',
                style: TextStyle(color: AppColors.danger)),
          ),
        ],
      ),
    );
  }
}

// =============================================================================
// WIDGETS CON
// =============================================================================

class _Avatar extends StatelessWidget {
  final String name;
  const _Avatar({required this.name});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        width: 96,
        height: 96,
        alignment: Alignment.center,
        decoration: const BoxDecoration(
          gradient: AppColors.accentGradient,
          shape: BoxShape.circle,
        ),
        child: Text(
          name.isNotEmpty ? name[0].toUpperCase() : '?',
          style: const TextStyle(
            fontSize: 36,
            fontWeight: FontWeight.bold,
            color: Colors.white,
          ),
        ),
      ),
    );
  }
}

class _Card extends StatelessWidget {
  final List<Widget> children;
  const _Card({required this.children});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.cardBg,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: children,
      ),
    );
  }
}

class _RowInfo extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  const _RowInfo(
      {required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, color: AppColors.textSecondary, size: 22),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label,
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.textSecondary)),
                const SizedBox(height: 2),
                Text(value,
                    style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary)),
              ],
            ),
          ),
        ],
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

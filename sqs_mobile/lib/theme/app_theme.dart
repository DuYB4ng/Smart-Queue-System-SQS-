import 'package:flutter/material.dart';

/// Bảng màu đồng bộ với SQS Customer (light theme):
/// - Primary: xanh teal đậm (logo, tiêu đề, số thứ tự "004", nút chính)
/// - Success: xanh lá (icon check thành công)
/// - Warning: cam (text "Đang có 0 người chờ trước bạn")
/// - Nền: trắng / xám rất nhạt
class AppColors {
  static const bgPrimary = Color(0xFFF5F7F8);      // nền ngoài, hơi xám-xanh
  static const bgSecondary = Color(0xFFEFF2F3);     // nền phụ
  static const cardBg = Colors.white;               // nền card chính
  static const boxBg = Color(0xFFF1F2F4);           // box xám chứa "Số thứ tự"

  static const primary = Color(0xFF0E6E64);         // teal đậm chủ đạo
  static const primaryDark = Color(0xFF0B5C53);     // teal đậm hơn (nút)
  static const primaryLight = Color(0xFF13897B);    // teal nhạt hơn

  static const accentPrimary = primary;
static const accentSecondary = primaryLight;

  static const success = Color(0xFF22C55E);         // icon check xanh lá
  static const warning = Color(0xFFE8A33D);         // text cam "đang chờ"
  static const danger = Color(0xFFEF4444);

  static const textPrimary = Color(0xFF1F2937);     // chữ đen/đậm
  static const textSecondary = Color(0xFF6B7280);   // chữ xám phụ
  static const border = Color(0xFFE5E7EB);

  static const accentGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [primary, primaryLight],
  );
}

ThemeData buildAppTheme() {
  final base = ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    scaffoldBackgroundColor: AppColors.bgPrimary,
    colorScheme: ColorScheme.fromSeed(
      seedColor: AppColors.primary,
      brightness: Brightness.light,
    ).copyWith(
      primary: AppColors.primary,
      secondary: AppColors.primaryLight,
      surface: AppColors.cardBg,
      error: AppColors.danger,
    ),
  );

  return base.copyWith(
    appBarTheme: const AppBarTheme(
      backgroundColor: Colors.white,
      foregroundColor: AppColors.primary,
      elevation: 0.5,
      centerTitle: false,
      titleTextStyle: TextStyle(
        color: AppColors.primary,
        fontSize: 20,
        fontWeight: FontWeight.w800,
      ),
      iconTheme: IconThemeData(color: AppColors.primary),
    ),
    textTheme: base.textTheme.apply(
      bodyColor: AppColors.textPrimary,
      displayColor: AppColors.textPrimary,
    ),
    cardTheme: CardThemeData(
      color: AppColors.cardBg,
      elevation: 2,
      shadowColor: Colors.black.withValues(alpha: 0.06),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: AppColors.boxBg,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: AppColors.border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: AppColors.border),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: AppColors.danger),
      ),
      labelStyle: const TextStyle(color: AppColors.textSecondary),
      hintStyle: const TextStyle(color: AppColors.textSecondary),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.primaryDark,
        foregroundColor: Colors.white,
        minimumSize: const Size.fromHeight(52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        elevation: 0,
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(foregroundColor: AppColors.primary),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: AppColors.primary,
        side: const BorderSide(color: AppColors.primary),
        minimumSize: const Size.fromHeight(52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
      ),
    ),
    iconTheme: const IconThemeData(color: AppColors.primary),
    dividerTheme: const DividerThemeData(color: AppColors.border, thickness: 1),
  );
}
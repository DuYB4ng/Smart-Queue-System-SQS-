using System.ComponentModel.DataAnnotations;

namespace SQS.API.DTOs.Users;

/// <summary>Request body cho PUT /api/users/{id} — cập nhật hồ sơ cá nhân.</summary>
public class UpdateProfileRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2-100 ký tự")]
    public string? Name { get; set; }

    public DateTime? Birthday { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }
}

/// <summary>Request đổi mật khẩu.</summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

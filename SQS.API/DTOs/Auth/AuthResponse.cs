using SQS.API.Models;

namespace SQS.API.DTOs.Auth;

/// <summary>Response trả về sau khi đăng nhập / đăng ký thành công.</summary>
public class AuthResponse
{
    public string Token     { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserDto  User    { get; set; } = null!;
}

/// <summary>Thông tin người dùng trả về trong response (không chứa password).</summary>
public class UserDto
{
    public int      Id       { get; set; }
    public string   Name     { get; set; } = string.Empty;
    public string   Email    { get; set; } = string.Empty;
    public UserRole Role     { get; set; }
    public DateOnly? Birthday { get; set; }
    public string?  Address  { get; set; }
}

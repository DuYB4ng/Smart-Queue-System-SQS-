using Microsoft.EntityFrameworkCore;
using SQS.API.Data;
using SQS.API.DTOs.Auth;
using SQS.API.DTOs.Users;
using SQS.API.Models;

namespace SQS.API.Services;

/// <summary>
/// Xử lý logic nghiệp vụ liên quan đến tài khoản người dùng:
/// đăng ký, đăng nhập, cập nhật hồ sơ, đổi mật khẩu.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService   _jwt;

    public AuthService(AppDbContext db, JwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // ── REGISTER ──────────────────────────────────────────────────

    /// <summary>
    /// Đăng ký tài khoản Customer mới.
    /// </summary>
    /// <exception cref="InvalidOperationException">Email đã tồn tại.</exception>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        // Kiểm tra email trùng
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            throw new InvalidOperationException("Email đã được đăng ký. Vui lòng dùng email khác.");

        // Hash mật khẩu
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 11);

        // Tạo User
        var user = new User
        {
            Name         = req.Name.Trim(),
            Email        = req.Email.Trim().ToLower(),
            PasswordHash = hash,
            Birthday     = req.Birthday,
            Address      = req.Address?.Trim(),
            Role         = UserRole.Customer,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Tạo Customer record (1-1)
        _db.Customers.Add(new Customer { UserId = user.Id });
        await _db.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    // ── LOGIN ─────────────────────────────────────────────────────

    /// <summary>
    /// Đăng nhập bằng email + mật khẩu.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Sai thông tin đăng nhập.</exception>
    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower() && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        return BuildAuthResponse(user);
    }

    // ── GET PROFILE ───────────────────────────────────────────────

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    // ── UPDATE PROFILE ────────────────────────────────────────────

    /// <summary>Cập nhật thông tin cá nhân (name, birthday, address).</summary>
    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!string.IsNullOrWhiteSpace(req.Name))
            user.Name = req.Name.Trim();
        if (req.Birthday.HasValue)
            user.Birthday = req.Birthday;
        if (req.Address is not null)
            user.Address = req.Address.Trim();

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    // ── CHANGE PASSWORD ───────────────────────────────────────────

    /// <summary>Đổi mật khẩu — yêu cầu nhập mật khẩu hiện tại.</summary>
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 11);
        await _db.SaveChangesAsync();
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAt) = _jwt.GenerateToken(user);
        return new AuthResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            User      = MapToDto(user),
        };
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id       = user.Id,
        Name     = user.Name,
        Email    = user.Email,
        Role     = user.Role,
        Birthday = user.Birthday,
        Address  = user.Address,
    };
}

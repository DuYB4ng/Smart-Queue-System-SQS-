using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQS.API.DTOs.Auth;
using SQS.API.DTOs.Users;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Quản lý thông tin cá nhân người dùng.
/// GET    /api/users/me
/// PUT    /api/users/me
/// PUT    /api/users/me/password
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AuthService authService, ILogger<UsersController> logger)
    {
        _authService = authService;
        _logger      = logger;
    }

    // ── GET /api/users/me ──────────────────────────────────────────

    /// <summary>Lấy thông tin hồ sơ của người dùng hiện tại.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId  = JwtService.GetUserId(User);
        var profile = await _authService.GetProfileAsync(userId);

        return profile is null
            ? NotFound(new { message = "Không tìm thấy người dùng." })
            : Ok(profile);
    }

    // ── PUT /api/users/me ──────────────────────────────────────────

    /// <summary>Cập nhật thông tin hồ sơ (name, birthday, address).</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var userId  = JwtService.GetUserId(User);
            var updated = await _authService.UpdateProfileAsync(userId, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── PUT /api/users/me/password ─────────────────────────────────

    /// <summary>Đổi mật khẩu — yêu cầu nhập mật khẩu hiện tại.</summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var userId = JwtService.GetUserId(User);
            await _authService.ChangePasswordAsync(userId, request);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

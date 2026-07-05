using Microsoft.AspNetCore.Mvc;
using SQS.API.DTOs.Auth;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Xử lý đăng ký và đăng nhập.
/// POST /api/auth/register
/// POST /api/auth/login
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger      = logger;
    }

    // ── POST /api/auth/register ────────────────────────────────────

    /// <summary>Đăng ký tài khoản Customer mới.</summary>
    /// <response code="201">Đăng ký thành công, trả về JWT token.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="409">Email đã tồn tại.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request);
            _logger.LogInformation("Đăng ký thành công: {Email}", request.Email);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            // Email đã tồn tại
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đăng ký cho email: {Email}", request.Email);
            return StatusCode(500, new { message = "Đã có lỗi xảy ra, vui lòng thử lại." });
        }
    }

    // ── POST /api/auth/login ───────────────────────────────────────

    /// <summary>Đăng nhập bằng email và mật khẩu.</summary>
    /// <response code="200">Đăng nhập thành công, trả về JWT token.</response>
    /// <response code="401">Sai email hoặc mật khẩu.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("Đăng nhập thành công: {Email}", request.Email);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đăng nhập cho email: {Email}", request.Email);
            return StatusCode(500, new { message = "Đã có lỗi xảy ra, vui lòng thử lại." });
        }
    }
}

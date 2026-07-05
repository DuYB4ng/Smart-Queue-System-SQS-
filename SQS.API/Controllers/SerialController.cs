using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Quản lý và giám sát kết nối Serial Port / Arduino.
/// GET  /api/serial/status       — Trạng thái kết nối COM
/// POST /api/serial/test         — Test gửi lệnh thủ công
/// POST /api/serial/call/{number}— Gửi số lên LCD Arduino
/// POST /api/serial/reset        — Reset màn hình LCD
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public class SerialController : ControllerBase
{
    private readonly SerialPortService _serial;
    private readonly ILogger<SerialController> _logger;

    public SerialController(SerialPortService serial, ILogger<SerialController> logger)
    {
        _serial = serial;
        _logger = logger;
    }

    // ── GET /api/serial/status ─────────────────────────────────────

    /// <summary>Kiểm tra trạng thái kết nối COM port.</summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isConnected = _serial.IsConnected,
            portName    = _serial.PortName,
            message     = _serial.IsConnected
                ? $"Đã kết nối với {_serial.PortName}"
                : "Chưa kết nối COM port (có thể đang chạy không có phần cứng)"
        });
    }

    // ── POST /api/serial/call/{number} ─────────────────────────────

    /// <summary>Gửi số thứ tự lên màn hình LCD Arduino thủ công.</summary>
    [HttpPost("call/{number}")]
    public async Task<IActionResult> CallNumber(string number)
    {
        if (number.Length > 3 || !number.All(char.IsDigit))
            return BadRequest(new { message = "Số thứ tự phải là 1-3 chữ số (VD: 001, 042, 999)." });

        await _serial.SendCallNumberAsync(number.PadLeft(3, '0'));
        _logger.LogInformation("Manual COM call: {Number} bởi Staff/Admin {User}",
            number, User.Identity?.Name);

        return Ok(new { message = $"Đã gửi CALL:{number} tới Arduino.", sent = _serial.IsConnected });
    }

    // ── POST /api/serial/reset ─────────────────────────────────────

    /// <summary>Reset (xóa) màn hình LCD Arduino.</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _serial.SendResetAsync();
        return Ok(new { message = "Đã gửi RESET tới Arduino.", sent = _serial.IsConnected });
    }

    // ── POST /api/serial/message ───────────────────────────────────

    /// <summary>Gửi text tùy ý lên LCD Arduino (test/debug).</summary>
    [HttpPost("message")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message không được để trống." });

        await _serial.SendMessageAsync(request.Message);
        return Ok(new { message = "Đã gửi.", text = request.Message, sent = _serial.IsConnected });
    }
}

public record SendMessageRequest(string Message);

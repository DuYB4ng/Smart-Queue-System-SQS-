using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQS.API.DTOs.Tickets;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Quản lý phiên xếp hàng (Tickets) từ góc độ khách hàng.
/// POST   /api/tickets              — Lấy số (Customer đăng nhập)
/// POST   /api/tickets/guest        — Lấy số (Kiosk / Khách vãng lai)
/// GET    /api/tickets/{id}/status  — Xem trạng thái số
/// DELETE /api/tickets/{id}         — Hủy số (Customer)
/// GET    /api/tickets/queue        — Danh sách hàng đợi (public)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(TicketService ticketService, ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
        _logger        = logger;
    }

    // ── POST /api/tickets ─────────────────────────────────────────

    /// <summary>Customer đã đăng nhập lấy số.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var customerId = JwtService.GetUserId(User);
            var response   = await _ticketService.CreateForCustomerAsync(customerId, request.ServiceId);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/tickets/guest ────────────────────────────────────

    /// <summary>Khách vãng lai lấy số qua Kiosk (không cần đăng nhập).</summary>
    [HttpPost("guest")]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGuest([FromBody] CreateTicketRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { message = "Vui lòng nhập tên để lấy số." });

        try
        {
            var response = await _ticketService.CreateForGuestAsync(request.ServiceId, request.GuestName);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/tickets/appointment ─────────────────────────────

    /// <summary>Đặt lịch hẹn trước (Customer đã đăng nhập).</summary>
    [HttpPost("appointment")]
    [Authorize]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var customerId = JwtService.GetUserId(User);
            var response = await _ticketService.CreateAppointmentAsync(
                request.ServiceId,
                request.AppointmentDate,
                customerId: customerId,
                studentId: request.StudentId,
                phoneNumber: request.PhoneNumber,
                note: request.Note);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /api/tickets/appointment/guest ───────────────────────

    /// <summary>Đặt lịch hẹn trước (khách vãng lai không đăng nhập).</summary>
    [HttpPost("appointment/guest")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAppointmentGuest([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { message = "Vui lòng nhập tên để đặt hẹn." });

        try
        {
            var response = await _ticketService.CreateAppointmentAsync(
                request.ServiceId,
                request.AppointmentDate,
                guestName: request.GuestName,
                studentId: request.StudentId,
                phoneNumber: request.PhoneNumber,
                note: request.Note);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── GET /api/tickets/{id}/status ──────────────────────────────

    /// <summary>Xem trạng thái và vị trí trong hàng đợi của 1 ticket.</summary>
    [HttpGet("{id:int}/status")]
    [ProducesResponseType(typeof(TicketStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(int id)
    {
        var result = await _ticketService.GetStatusAsync(id);
        return result is null
            ? NotFound(new { message = "Không tìm thấy phiên xếp hàng." })
            : Ok(result);
    }

    // ── GET /api/tickets/queue?serviceId={id} ─────────────────────

    /// <summary>Lấy trạng thái hàng đợi của 1 dịch vụ (màn hình display + kiosk).</summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(QueueStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue([FromQuery] int serviceId)
    {
        if (serviceId <= 0)
            return BadRequest(new { message = "serviceId không hợp lệ." });

        try
        {
            var result = await _ticketService.GetQueueAsync(serviceId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── DELETE /api/tickets/{id} ──────────────────────────────────

    /// <summary>Customer hủy số của mình (chỉ khi đang Waiting).</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var customerId = JwtService.GetUserId(User);
            await _ticketService.CancelByCustomerAsync(id, customerId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

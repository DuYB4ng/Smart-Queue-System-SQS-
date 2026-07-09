using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQS.API.Data;
using SQS.API.DTOs.Tickets;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Các thao tác của nhân viên quầy (Staff).
/// POST /api/staff/call-next        — Gọi số tiếp theo
/// POST /api/staff/complete/{id}    — Hoàn thành phiên
/// POST /api/staff/skip/{id}        — Bỏ qua (khách không đến)
/// GET  /api/staff/my-queue         — Danh sách hàng đợi tại quầy của staff
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Staff,Admin")]
[Produces("application/json")]
public class StaffController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly AppDbContext _db;
    private readonly ILogger<StaffController> _logger;

    public StaffController(TicketService ticketService, AppDbContext db, ILogger<StaffController> logger)
    {
        _ticketService = ticketService;
        _db            = db;
        _logger        = logger;
    }

    // ── POST /api/staff/call-next ──────────────────────────────────

    /// <summary>
    /// Gọi số tiếp theo trong hàng đợi của quầy.
    /// Tìm ticket Waiting cũ nhất thuộc dịch vụ được phân công cho quầy này.
    /// Kích hoạt SignalR broadcast TicketCalled (sẽ tích hợp Phase 5).
    /// </summary>
    [HttpPost("call-next")]
    [ProducesResponseType(typeof(CallNextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CallNext([FromBody] CallNextRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var staffId  = JwtService.GetUserId(User);
            var response = await _ticketService.CallNextAsync(staffId, request.CounterId);

            // TODO Phase 5: Kích hoạt SignalR broadcast
            // await _hubContext.Clients.All.SendAsync("TicketCalled", new { ... });

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── POST /api/staff/complete/{id} ──────────────────────────────

    /// <summary>
    /// Đánh dấu hoàn thành phiên hiện tại.
    /// Staff nhận +1 KPI. Status chuyển từ Calling → Completed.
    /// </summary>
    [HttpPost("complete/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            var staffId = JwtService.GetUserId(User);
            var newKpi  = await _ticketService.CompleteAsync(id, staffId);

            // TODO Phase 5: SignalR QueueUpdated
            return Ok(new { message = "Hoàn thành phiên xếp hàng.", kpi = newKpi });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message + "\n" + ex.StackTrace });
        }
    }

    // ── POST /api/staff/skip/{id} ──────────────────────────────────

    /// <summary>
    /// Bỏ qua số hiện tại (khách không đến).
    /// Chỉ thực hiện được khi ticket đang ở trạng thái Calling.
    /// </summary>
    [HttpPost("skip/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Skip(int id)
    {
        try
        {
            var staffId = JwtService.GetUserId(User);
            await _ticketService.SkipAsync(id, staffId);

            // TODO Phase 5: SignalR QueueUpdated
            return Ok(new { message = "Đã bỏ qua số này." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ── GET /api/staff/my-queue?counterId={id} ────────────────────

    /// <summary>Xem hàng đợi hiện tại tại quầy của staff.</summary>
    [HttpGet("my-queue")]
    [ProducesResponseType(typeof(QueueStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyQueue([FromQuery] int counterId)
    {
        if (counterId <= 0) return BadRequest(new { message = "counterId không hợp lệ." });

        try
        {
            // Lấy serviceId đầu tiên của quầy này
            // Sẽ mở rộng sau cho quầy phục vụ nhiều dịch vụ
            var result = await _ticketService.GetQueueAsync(counterId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── GET /api/staff/current-ticket ──────────────────────────────

    /// <summary>Lấy ticket đang được phục vụ hiện tại của nhân viên.</summary>
    [HttpGet("current-ticket")]
    [ProducesResponseType(typeof(TicketStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentTicket()
    {
        var staffId = JwtService.GetUserId(User);
        var result = await _ticketService.GetCurrentTicketForStaffAsync(staffId);
        if (result == null) return NoContent();
        return Ok(result);
    }

    // ── GET /api/staff/info ──────────────────────────────────

    /// <summary>Lấy thông tin staff hiện tại (position, counter...).</summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetMyInfo()
    {
        var userId = JwtService.GetUserId(User);
        var staff = await _db.Staffs
            .Include(s => s.User)
            .Include(s => s.Counter)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (staff is null) return NotFound(new { message = "Không tìm thấy thông tin nhân viên." });

        return Ok(new
        {
            StaffId     = staff.UserId,
            Name        = staff.User.Name,
            Position    = staff.Position,
            CounterId   = staff.CounterId,
            CounterName = staff.Counter?.Name,
            Kpi         = staff.Kpi,
        });
    }

    // ── GET /api/staff/appointments ─────────────────────────

    /// <summary>Xem danh sách lịch hẹn cần xử lý.</summary>
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] int? serviceId,
        [FromQuery] DateTime? date)
    {
        var appointments = await _ticketService.GetAppointmentsAsync(serviceId, date);
        return Ok(appointments);
    }

    // ── POST /api/staff/call-appointment/{id} ──────────────────────

    /// <summary>Gọi đích danh một lịch hẹn (Chuyển trạng thái từ Waiting -> Calling).</summary>
    [HttpPost("call-appointment/{id:int}")]
    public async Task<IActionResult> CallAppointment(int id)
    {
        try
        {
            var staffId = JwtService.GetUserId(User);
            var response = await _ticketService.CallAppointmentAsync(id, staffId);
            return Ok(response);
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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using SQS.API.Data;
using SQS.API.DTOs.Tickets;
using SQS.API.Models;
using SQS.API.Services;

namespace SQS.API.Controllers;

/// <summary>
/// Dashboard và quản lý hệ thống cho Admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TicketService _ticketService;

    public AdminController(AppDbContext db, TicketService ticketService)
    {
        _db = db;
        _ticketService = ticketService;
    }

    // ══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ══════════════════════════════════════════════════════════════

    /// <summary>Thống kê tổng hợp ngày hôm nay.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var today = DateTime.Today;

        var statusCounts = await _db.Tickets
            .Where(t => t.TicketDate == today)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var byService = await _db.Tickets
            .Where(t => t.TicketDate == today)
            .GroupBy(t => new { t.IdService, t.Service.Name, t.Service.Code })
            .Select(g => new
            {
                ServiceId   = g.Key.IdService,
                ServiceName = g.Key.Name,
                ServiceCode = g.Key.Code,
                Total       = g.Count(),
                Completed   = g.Count(t => t.Status == TicketStatus.Completed),
                Canceled    = g.Count(t => t.Status == TicketStatus.Canceled),
                Waiting     = g.Count(t => t.Status == TicketStatus.Waiting),
                Calling     = g.Count(t => t.Status == TicketStatus.Calling),
            })
            .OrderBy(x => x.ServiceId)
            .ToListAsync();

        var topStaff = await _db.Staffs
            .Include(s => s.User)
            .Select(s => new
            {
                StaffId  = s.UserId,
                Name     = s.User.Name,
                Position = s.Position,
                TotalKpi = s.Kpi,
                TodayKpi = s.Tickets.Count(t =>
                    t.Status     == TicketStatus.Completed &&
                    t.TicketDate == today),
            })
            .OrderByDescending(s => s.TodayKpi)
            .Take(5)
            .ToListAsync();

        var totalToday      = statusCounts.Sum(s => s.Count);
        var completedToday  = statusCounts.FirstOrDefault(s => s.Status == "Completed")?.Count ?? 0;
        var canceledToday   = statusCounts.FirstOrDefault(s => s.Status == "Canceled")?.Count ?? 0;
        var waitingNow      = statusCounts.FirstOrDefault(s => s.Status == "Waiting")?.Count ?? 0;
        var callingNow      = statusCounts.FirstOrDefault(s => s.Status == "Calling")?.Count ?? 0;

        var appointmentCount = await _db.Tickets
            .CountAsync(t => t.TicketType == TicketType.Appointment && t.AppointmentDate == today);

        return Ok(new
        {
            Date    = today,
            Summary = new
            {
                Total     = totalToday,
                Completed = completedToday,
                Canceled  = canceledToday,
                Waiting   = waitingNow,
                Calling   = callingNow,
                Appointments = appointmentCount,
                CompletionRate = totalToday > 0
                    ? Math.Round((double)completedToday / totalToday * 100, 1)
                    : 0.0
            },
            ByService = byService,
            TopStaff  = topStaff,
        });
    }

    // ══════════════════════════════════════════════════════════════
    // STAFF MANAGEMENT
    // ══════════════════════════════════════════════════════════════

    /// <summary>Danh sách toàn bộ Staff và KPI của họ.</summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffList()
    {
        var today = DateTime.Today;

        var staffList = await _db.Staffs
            .Include(s => s.User)
            .Include(s => s.Counter)
            .Select(s => new
            {
                StaffId    = s.UserId,
                Name       = s.User.Name,
                Email      = s.User.Email,
                Position   = s.Position,
                CounterId  = s.CounterId,
                CounterName = s.Counter != null ? s.Counter.Name : null,
                TotalKpi   = s.Kpi,
                TodayKpi   = s.Tickets.Count(t =>
                    t.Status     == TicketStatus.Completed &&
                    t.TicketDate == today),
                IsActive   = s.User.IsActive,
            })
            .OrderByDescending(s => s.TotalKpi)
            .ToListAsync();

        return Ok(staffList);
    }

    /// <summary>Cập nhật thông tin Staff (position, counterId).</summary>
    [HttpPut("staff/{id:int}")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffRequest request)
    {
        var staff = await _db.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == id);
        if (staff is null) return NotFound(new { message = "Không tìm thấy nhân viên." });

        if (!string.IsNullOrWhiteSpace(request.Position))
            staff.Position = request.Position.Trim();

        if (request.CounterId.HasValue)
        {
            var counter = await _db.Counters.FindAsync(request.CounterId.Value);
            if (counter is null) return BadRequest(new { message = "Quầy không tồn tại." });
            staff.CounterId = request.CounterId.Value;
        }

        if (request.IsActive.HasValue)
            staff.User.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công.", staffId = id });
    }

    /// <summary>Chỉnh sửa hoặc reset KPI của 1 Staff.</summary>
    [HttpPut("staff/{id:int}/kpi")]
    public async Task<IActionResult> UpdateKpi(int id, [FromBody] UpdateKpiRequest request)
    {
        var staff = await _db.Staffs.FindAsync(id);
        if (staff is null) return NotFound(new { message = "Không tìm thấy nhân viên." });

        var oldKpi = staff.Kpi;
        staff.Kpi  = request.Kpi;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Đã cập nhật KPI từ {oldKpi} → {staff.Kpi}", kpi = staff.Kpi });
    }

    // ══════════════════════════════════════════════════════════════
    // SERVICES MANAGEMENT (CRUD)
    // ══════════════════════════════════════════════════════════════

    /// <summary>Tạo dịch vụ mới.</summary>
    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
    {
        if (await _db.Services.AnyAsync(s => s.Code == request.Code.ToUpper()))
            return Conflict(new { message = $"Mã dịch vụ '{request.Code}' đã tồn tại." });

        var service = new Service
        {
            Name        = request.Name.Trim(),
            Code        = request.Code.Trim().ToUpper(),
            Description = request.Description?.Trim(),
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { service.Id, service.Name, service.Code });
    }

    /// <summary>Cập nhật dịch vụ.</summary>
    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is null) return NotFound(new { message = "Dịch vụ không tồn tại." });

        if (!string.IsNullOrWhiteSpace(request.Name))
            service.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Description))
            service.Description = request.Description.Trim();
        if (request.IsActive.HasValue)
            service.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật dịch vụ thành công.", service.Id, service.Name });
    }

    /// <summary>Xóa (vô hiệu hóa) dịch vụ.</summary>
    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is null) return NotFound(new { message = "Dịch vụ không tồn tại." });

        service.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Đã vô hiệu hóa dịch vụ '{service.Name}'." });
    }

    // ══════════════════════════════════════════════════════════════
    // APPOINTMENTS MANAGEMENT
    // ══════════════════════════════════════════════════════════════

    /// <summary>Danh sách lịch hẹn (có filter theo dịch vụ và ngày).</summary>
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] int? serviceId,
        [FromQuery] DateTime? date)
    {
        var appointments = await _ticketService.GetAppointmentsAsync(serviceId, date);
        return Ok(appointments);
    }

    // ══════════════════════════════════════════════════════════════
    // TICKETS HISTORY
    // ══════════════════════════════════════════════════════════════

    /// <summary>Lịch sử phiên xếp hàng có filter ngày và trạng thái.</summary>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(
        [FromQuery] DateTime? date,
        [FromQuery] string?   status,
        [FromQuery] int?      serviceId,
        [FromQuery] string?   ticketType,
        [FromQuery] int       page  = 1,
        [FromQuery] int       limit = 20)
    {
        var query = _db.Tickets
            .Include(t => t.Service)
            .Include(t => t.Counter)
            .Include(t => t.Staff)
                .ThenInclude(s => s!.User)
            .AsQueryable();

        // Filters
        if (date.HasValue)
            query = query.Where(t => t.TicketDate == date.Value);
        else
            query = query.Where(t => t.TicketDate == DateTime.Today);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (serviceId.HasValue)
            query = query.Where(t => t.IdService == serviceId);

        if (!string.IsNullOrEmpty(ticketType) && Enum.TryParse<TicketType>(ticketType, true, out var typeEnum))
            query = query.Where(t => t.TicketType == typeEnum);

        var total = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                TicketType   = t.TicketType.ToString(),
                ServiceName  = t.Service.Name,
                CounterName  = t.Counter != null ? t.Counter.Name : null,
                StaffName    = t.Staff   != null ? t.Staff.User.Name : null,
                CustomerName = t.IdCustomer != null ? t.Customer!.User.Name : t.GuestName,
                t.Status,
                t.TicketDate,
                t.CreatedAt,
                t.CalledAt,
                t.CompletedAt,
                t.AppointmentDate,
                t.StudentId,
                t.PhoneNumber,
                t.Note,
            })
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            Page  = page,
            Limit = limit,
            Pages = (int)Math.Ceiling((double)total / limit),
            Data  = tickets,
        });
    }
}

// ── Admin Request DTOs ────────────────────────────────────────────

public record UpdateKpiRequest(int Kpi);

public record UpdateStaffRequest(
    string? Position,
    int?    CounterId,
    bool?   IsActive
);

public record CreateServiceRequest(
    [property: Required] string Name,
    [property: Required] string Code,
    string? Description
);

public record UpdateServiceRequest(
    string? Name,
    string? Description,
    bool?   IsActive
);


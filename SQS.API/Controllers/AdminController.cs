using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQS.API.Data;
using SQS.API.Models;

namespace SQS.API.Controllers;

/// <summary>
/// Dashboard và quản lý hệ thống cho Admin.
/// GET /api/admin/dashboard    — Thống kê tổng hợp hôm nay
/// GET /api/admin/staff        — Danh sách Staff + KPI
/// PUT /api/admin/staff/{id}/kpi — Reset/Chỉnh KPI
/// GET /api/admin/tickets      — Lịch sử phiên xếp hàng (có filter)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    // ── GET /api/admin/dashboard ───────────────────────────────────

    /// <summary>Thống kê tổng hợp ngày hôm nay.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var today = DateTime.Today;

        // Tổng hợp theo trạng thái
        var statusCounts = await _db.Tickets
            .Where(t => t.TicketDate == today)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        // Tổng hợp theo dịch vụ
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

        // Tổng hợp KPI hôm nay theo Staff
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
                CompletionRate = totalToday > 0
                    ? Math.Round((double)completedToday / totalToday * 100, 1)
                    : 0.0
            },
            ByService = byService,
            TopStaff  = topStaff,
        });
    }

    // ── GET /api/admin/staff ───────────────────────────────────────

    /// <summary>Danh sách toàn bộ Staff và KPI của họ.</summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffList()
    {
        var today = DateTime.Today;

        var staffList = await _db.Staffs
            .Include(s => s.User)
            .Select(s => new
            {
                StaffId   = s.UserId,
                Name      = s.User.Name,
                Email     = s.User.Email,
                Position  = s.Position,
                TotalKpi  = s.Kpi,
                TodayKpi  = s.Tickets.Count(t =>
                    t.Status     == TicketStatus.Completed &&
                    t.TicketDate == today),
                IsActive  = s.User.IsActive,
            })
            .OrderByDescending(s => s.TotalKpi)
            .ToListAsync();

        return Ok(staffList);
    }

    // ── PUT /api/admin/staff/{id}/kpi ─────────────────────────────

    /// <summary>Chỉnh sửa hoặc reset KPI của 1 Staff.</summary>
    [HttpPut("staff/{id:int}/kpi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateKpi(int id, [FromBody] UpdateKpiRequest request)
    {
        var staff = await _db.Staffs.FindAsync(id);
        if (staff is null) return NotFound(new { message = "Không tìm thấy nhân viên." });

        var oldKpi = staff.Kpi;
        staff.Kpi  = request.Kpi;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Đã cập nhật KPI từ {oldKpi} → {staff.Kpi}", kpi = staff.Kpi });
    }

    // ── GET /api/admin/tickets ─────────────────────────────────────

    /// <summary>Lịch sử phiên xếp hàng có filter ngày và trạng thái.</summary>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(
        [FromQuery] DateTime? date,
        [FromQuery] string?   status,
        [FromQuery] int?      serviceId,
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

        var total = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                ServiceName  = t.Service.Name,
                CounterName  = t.Counter != null ? t.Counter.Name : null,
                StaffName    = t.Staff   != null ? t.Staff.User.Name : null,
                CustomerName = t.IdCustomer != null ? t.Customer!.User.Name : t.GuestName,
                t.Status,
                t.TicketDate,
                t.CreatedAt,
                t.CalledAt,
                t.CompletedAt,
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

/// <summary>Request body cho PUT /api/admin/staff/{id}/kpi</summary>
public record UpdateKpiRequest(int Kpi);

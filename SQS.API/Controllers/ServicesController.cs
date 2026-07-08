using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQS.API.Data;

namespace SQS.API.Controllers;

/// <summary>
/// Danh mục dịch vụ — public, không cần đăng nhập.
/// GET /api/services          — Danh sách tất cả dịch vụ
/// GET /api/services/{id}     — Chi tiết 1 dịch vụ + quầy phục vụ
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db) => _db = db;

    // ── GET /api/services ──────────────────────────────────────────

    /// <summary>Lấy danh sách tất cả dịch vụ đang hoạt động.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.Description,
                Counters = s.CounterServices
                    .Where(cs => cs.Counter.IsActive)
                    .Select(cs => new { cs.Counter.Id, cs.Counter.Name, cs.Counter.Location })
                    .ToList()
            })
            .ToListAsync();

        return Ok(services);
    }

    // ── GET /api/services/{id} ─────────────────────────────────────

    /// <summary>Chi tiết dịch vụ + quầy + số lượng đang chờ hôm nay.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var service = await _db.Services
            .Where(s => s.Id == id && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.Description,
                WaitingCount = s.Tickets.Count(t =>
                    t.Status == Models.TicketStatus.Waiting &&
                    t.TicketDate == DateTime.Today),
                Counters = s.CounterServices
                    .Where(cs => cs.Counter.IsActive)
                    .Select(cs => new { cs.Counter.Id, cs.Counter.Name, cs.Counter.Location })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return service is null
            ? NotFound(new { message = "Dịch vụ không tồn tại." })
            : Ok(service);
    }
}

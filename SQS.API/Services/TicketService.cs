using Microsoft.EntityFrameworkCore;
using SQS.API.Data;
using SQS.API.DTOs.Tickets;
using SQS.API.Models;

namespace SQS.API.Services;

/// <summary>
/// Xử lý toàn bộ nghiệp vụ liên quan đến phiên xếp hàng (Ticket):
/// - Lấy số (Customer/Kiosk)
/// - Xem trạng thái
/// - Hủy số
/// - Gọi số tiếp theo (Staff)
/// - Hoàn thành / Bỏ qua (Staff)
/// </summary>
public class TicketService
{
    private readonly AppDbContext            _db;
    private readonly SequenceService         _sequence;
    private readonly QueueNotificationService _notify;
    private readonly SerialPortService       _serial;
    private readonly ILogger<TicketService>  _logger;

    public TicketService(
        AppDbContext db,
        SequenceService sequence,
        QueueNotificationService notify,
        SerialPortService serial,
        ILogger<TicketService> logger)
    {
        _db       = db;
        _sequence = sequence;
        _notify   = notify;
        _serial   = serial;
        _logger   = logger;
    }

    // ── CREATE TICKET (Lấy số) ─────────────────────────────────────

    /// <summary>
    /// Khách hàng đã đăng nhập lấy số.
    /// </summary>
    public async Task<CreateTicketResponse> CreateForCustomerAsync(int customerId, int serviceId)
    {
        await ValidateServiceAsync(serviceId);

        var today  = DateTime.Today;
        var number = await _sequence.GetNextNumberAsync(today);

        var ticket = new Ticket
        {
            TicketNumber = number,
            IdCustomer   = customerId,
            IdService    = serviceId,
            TicketDate   = today,
            Status       = TicketStatus.Waiting,
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Cập nhật lên màn hình Display
        await NotifyQueueUpdatedAsync(serviceId);

        return await BuildCreateResponseAsync(ticket);
    }

    /// <summary>
    /// Khách vãng lai lấy số qua Kiosk (không cần đăng nhập).
    /// </summary>
    public async Task<CreateTicketResponse> CreateForGuestAsync(int serviceId, string guestName)
    {
        if (string.IsNullOrWhiteSpace(guestName))
            throw new ArgumentException("Tên khách không được để trống.", nameof(guestName));

        await ValidateServiceAsync(serviceId);

        var today  = DateTime.Today;
        var number = await _sequence.GetNextNumberAsync(today);

        var ticket = new Ticket
        {
            TicketNumber = number,
            GuestName    = guestName.Trim(),
            IdService    = serviceId,
            TicketDate   = today,
            Status       = TicketStatus.Waiting,
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Cập nhật lên màn hình Display
        await NotifyQueueUpdatedAsync(serviceId);

        return await BuildCreateResponseAsync(ticket);
    }

    // ── GET STATUS ─────────────────────────────────────────────────

    /// <summary>Khách hàng xem trạng thái số của mình.</summary>
    public async Task<TicketStatusResponse?> GetStatusAsync(int ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Service)
            .Include(t => t.Counter)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return null;

        // Tính vị trí trong hàng đợi (nếu đang Waiting)
        int position = 0;
        if (ticket.Status == TicketStatus.Waiting)
        {
            position = await _db.Tickets.CountAsync(t =>
                t.IdService   == ticket.IdService &&
                t.TicketDate  == ticket.TicketDate &&
                t.Status      == TicketStatus.Waiting &&
                t.CreatedAt   <= ticket.CreatedAt &&
                t.Id          != ticket.Id) + 1;
        }

        return new TicketStatusResponse
        {
            TicketId      = ticket.Id,
            TicketNumber  = ticket.TicketNumber,
            ServiceName   = ticket.Service.Name,
            Status        = ticket.Status.ToString(),
            QueuePosition = position,
            CounterName   = ticket.Counter?.Name,
            CalledAt      = ticket.CalledAt,
            CreatedAt     = ticket.CreatedAt,
        };
    }

    // ── GET QUEUE ──────────────────────────────────────────────────

    /// <summary>Lấy danh sách hàng đợi hiện tại của 1 dịch vụ.</summary>
    public async Task<QueueStatusResponse> GetQueueAsync(int serviceId)
    {
        var service = await _db.Services.FindAsync(serviceId)
            ?? throw new KeyNotFoundException("Dịch vụ không tồn tại.");

        var today = DateTime.Today;

        // Số đang gọi
        var calling = await _db.Tickets
            .Include(t => t.Counter)
            .Where(t => t.IdService == serviceId && t.TicketDate == today && t.Status == TicketStatus.Calling)
            .OrderByDescending(t => t.CalledAt)
            .FirstOrDefaultAsync();

        // Danh sách đang chờ
        var waiting = await _db.Tickets
            .Where(t => t.IdService == serviceId && t.TicketDate == today && t.Status == TicketStatus.Waiting)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new QueueItem
            {
                TicketId     = t.Id,
                TicketNumber = t.TicketNumber,
                CustomerName = t.IdCustomer != null ? t.Customer!.User.Name : t.GuestName!,
                Position     = 0,  // Sẽ gán bên dưới
                CreatedAt    = t.CreatedAt,
            })
            .ToListAsync();

        // Gán position
        for (int i = 0; i < waiting.Count; i++)
            waiting[i].Position = i + 1;

        return new QueueStatusResponse
        {
            ServiceId      = serviceId,
            ServiceName    = service.Name,
            CurrentCalling = calling?.TicketNumber,
            CounterName    = calling?.Counter?.Name,
            WaitingCount   = waiting.Count,
            WaitingList    = waiting,
        };
    }

    // ── CANCEL TICKET (Hủy số) ─────────────────────────────────────

    /// <summary>
    /// Khách hàng hủy số của mình (chỉ được khi đang Waiting).
    /// </summary>
    public async Task CancelByCustomerAsync(int ticketId, int customerId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiên xếp hàng.");

        if (ticket.IdCustomer != customerId)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy phiên này.");

        if (ticket.Status != TicketStatus.Waiting)
            throw new InvalidOperationException($"Không thể hủy phiên đang ở trạng thái '{ticket.Status}'.");

        ticket.Status = TicketStatus.Canceled;
        await _db.SaveChangesAsync();

        // 📡 SignalR: cập nhật hàng đợi
        await NotifyQueueUpdatedAsync(ticket.IdService);
        await _notify.NotifyTicketStatusChangedAsync(new TicketStatusChangedPayload(
            ticket.Id, ticket.TicketNumber, "Canceled", null, "Bạn đã hủy số xếp hàng."));

        _logger.LogInformation("Customer {Id} hủy ticket {TicketId}", customerId, ticketId);
    }

    // ── CALL NEXT (Staff gọi số) ───────────────────────────────────

    /// <summary>
    /// Staff gọi số tiếp theo tại quầy của mình.
    /// Tìm ticket Waiting cũ nhất (FIFO) của dịch vụ thuộc quầy đó.
    /// </summary>
    public async Task<CallNextResponse> CallNextAsync(int staffId, int counterId)
    {
        // Kiểm tra quầy có dịch vụ nào không
        var serviceIds = await _db.CounterServices
            .Where(cs => cs.CounterId == counterId)
            .Select(cs => cs.ServiceId)
            .ToListAsync();

        if (!serviceIds.Any())
            throw new InvalidOperationException("Quầy này chưa được phân công dịch vụ nào.");

        // Kiểm tra quầy chưa có ticket đang Calling
        var hasActiveCalling = await _db.Tickets.AnyAsync(t =>
            t.IdCounter == counterId && t.Status == TicketStatus.Calling);
        if (hasActiveCalling)
            throw new InvalidOperationException("Quầy đang có khách. Vui lòng hoàn thành hoặc bỏ qua trước.");

        var today = DateTime.Today;

        // Lấy ticket Waiting cũ nhất (FIFO) — ưu tiên theo thời gian tạo
        var ticket = await _db.Tickets
            .Include(t => t.Service)
            .Where(t =>
                serviceIds.Contains(t.IdService) &&
                t.TicketDate == today &&
                t.Status     == TicketStatus.Waiting)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (ticket is null)
            throw new InvalidOperationException("Không còn ai trong hàng đợi.");

        // Cập nhật trạng thái
        ticket.Status    = TicketStatus.Calling;
        ticket.IdCounter = counterId;
        ticket.IdStaff   = staffId;
        ticket.CalledAt  = DateTime.UtcNow;

        // Cộng KPI cho staff
        var staff = await _db.Staffs.FindAsync(staffId);
        // KPI chỉ cộng khi Complete, không cộng ở đây

        await _db.SaveChangesAsync();

        var counter = await _db.Counters.FindAsync(counterId);

        // 📡 SignalR: broadcast toàn hệ thống
        await _notify.BroadcastTicketCalledAsync(
            new TicketCalledPayload(
                ticket.TicketNumber,
                counter?.Name ?? "",
                ticket.Service.Name,
                ticket.Id),
            ticket.IdService);

        // 📡 Thông báo riêng cho chủ ticket
        await _notify.NotifyTicketStatusChangedAsync(new TicketStatusChangedPayload(
            ticket.Id, ticket.TicketNumber, "Calling",
            counter?.Name,
            $"Đến quầy {counter?.Name} để được phục vụ!"));

        // 📡 Cập nhật số người chờ sau khi gọi
        await NotifyQueueUpdatedAsync(ticket.IdService, ticket.TicketNumber, counter?.Name);

        // 🔌 COM Port: gửi số lên màn hình LCD Arduino
        await _serial.SendCallNumberAsync(ticket.TicketNumber);

        _logger.LogInformation(
            "Staff {StaffId} gọi số {Ticket} tại quầy {Counter}",
            staffId, ticket.TicketNumber, counter?.Name);

        // Đếm số người còn lại
        var remainingWait = await _db.Tickets.CountAsync(t =>
            serviceIds.Contains(t.IdService) &&
            t.TicketDate == today &&
            t.Status     == TicketStatus.Waiting);

        return new CallNextResponse
        {
            TicketId      = ticket.Id,
            TicketNumber  = ticket.TicketNumber,
            CustomerName  = ticket.IdCustomer.HasValue
                            ? (await _db.Users.FindAsync(ticket.IdCustomer))?.Name ?? "N/A"
                            : ticket.GuestName ?? "Khách",
            ServiceName   = ticket.Service.Name,
            CounterName   = counter?.Name ?? "",
            RemainingWait = remainingWait,
        };
    }

    // ── COMPLETE (Hoàn thành) ──────────────────────────────────────

    /// <summary>
    /// Staff đánh dấu hoàn thành phiên đang xử lý.
    /// Cộng +1 KPI cho staff.
    /// </summary>
    public async Task<int> CompleteAsync(int ticketId, int staffId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiên xếp hàng.");

        if (ticket.Status != TicketStatus.Calling)
            throw new InvalidOperationException("Chỉ có thể hoàn thành phiên đang ở trạng thái 'Calling'.");

        if (ticket.IdStaff != staffId)
            throw new UnauthorizedAccessException("Bạn không có quyền hoàn thành phiên này.");

        ticket.Status      = TicketStatus.Completed;
        ticket.CompletedAt = DateTime.UtcNow;

        // Cộng KPI
        var staff = await _db.Staffs.FindAsync(staffId)
            ?? throw new KeyNotFoundException("Không tìm thấy thông tin nhân viên.");
        staff.Kpi += 1;

        await _db.SaveChangesAsync();

        // 📡 SignalR: thông báo cho chủ ticket
        await _notify.NotifyTicketStatusChangedAsync(new TicketStatusChangedPayload(
            ticket.Id, ticket.TicketNumber, "Completed", null, "Cảm ơn bạn đã sử dụng dịch vụ!"));

        // 📡 Cập nhật hàng đợi
        await NotifyQueueUpdatedAsync(ticket.IdService);

        _logger.LogInformation("Staff {StaffId} hoàn thành ticket {TicketId}, KPI = {Kpi}",
            staffId, ticketId, staff.Kpi);

        return staff.Kpi;
    }

    // ── SKIP (Bỏ qua) ─────────────────────────────────────────────

    /// <summary>
    /// Staff bỏ qua số (khách không đến).
    /// Chỉ được khi ticket đang ở trạng thái 'Calling'.
    /// </summary>
    public async Task SkipAsync(int ticketId, int staffId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiên xếp hàng.");

        if (ticket.Status != TicketStatus.Calling)
            throw new InvalidOperationException("Chỉ có thể bỏ qua phiên đang ở trạng thái 'Calling'.");

        if (ticket.IdStaff != staffId)
            throw new UnauthorizedAccessException("Bạn không có quyền bỏ qua phiên này.");

        ticket.Status = TicketStatus.Canceled;
        await _db.SaveChangesAsync();

        // 📡 SignalR
        await _notify.NotifyTicketStatusChangedAsync(new TicketStatusChangedPayload(
            ticket.Id, ticket.TicketNumber, "Canceled", null,
            "Số của bạn đã bị bỏ qua do không có mặt."));
        await NotifyQueueUpdatedAsync(ticket.IdService);

        _logger.LogInformation("Staff {StaffId} bỏ qua ticket {TicketId}", staffId, ticketId);
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private async Task ValidateServiceAsync(int serviceId)
    {
        var exists = await _db.Services.AnyAsync(s => s.Id == serviceId && s.IsActive);
        if (!exists)
            throw new KeyNotFoundException("Dịch vụ không tồn tại hoặc đã ngưng hoạt động.");
    }

    private async Task<CreateTicketResponse> BuildCreateResponseAsync(Ticket ticket)
    {
        var service = await _db.Services.FindAsync(ticket.IdService)!;

        // Số người chờ trước ticket này
        int estimatedWait = await _db.Tickets.CountAsync(t =>
            t.IdService  == ticket.IdService &&
            t.TicketDate == ticket.TicketDate &&
            t.Status     == TicketStatus.Waiting &&
            t.Id         != ticket.Id);

        return new CreateTicketResponse
        {
            TicketId      = ticket.Id,
            TicketNumber  = ticket.TicketNumber,
            ServiceName   = service!.Name,
            ServiceCode   = service.Code,
            EstimatedWait = estimatedWait,
            CreatedAt     = ticket.CreatedAt,
        };
    }

    /// <summary>Helper: tính waiting count và broadcast QueueUpdated.</summary>
    private async Task NotifyQueueUpdatedAsync(
        int serviceId,
        string? currentCalling = null,
        string? counterName    = null)
    {
        var service = await _db.Services.FindAsync(serviceId);
        var today   = DateTime.Today;

        int waitingCount = await _db.Tickets.CountAsync(t =>
            t.IdService  == serviceId &&
            t.TicketDate == today &&
            t.Status     == TicketStatus.Waiting);

        if (currentCalling == null)
        {
            var calling = await _db.Tickets
                .Where(t => t.IdService == serviceId && t.TicketDate == today && t.Status == TicketStatus.Calling)
                .OrderByDescending(t => t.CalledAt)
                .FirstOrDefaultAsync();
            currentCalling = calling?.TicketNumber ?? "--";
        }

        await _notify.BroadcastQueueUpdatedAsync(new QueueUpdatedPayload(
            serviceId,
            service?.Name ?? "",
            waitingCount,
            currentCalling));
    }
}

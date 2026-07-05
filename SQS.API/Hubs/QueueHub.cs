using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SQS.API.Hubs;

/// <summary>
/// SignalR Hub trung tâm cho hệ thống xếp hàng.
///
/// GROUPS (client subscribe):
///   "display"          → Màn hình TV tổng (public)
///   "service-{id}"     → Theo dõi hàng đợi 1 dịch vụ
///   "ticket-{id}"      → Theo dõi trạng thái 1 ticket cụ thể
///   "staff-{counterId}"→ Staff tại quầy cụ thể
///
/// CLIENT EVENTS (server → client):
///   TicketCalled        → Màn hình display + tất cả
///   QueueUpdated        → Cập nhật số người chờ theo dịch vụ
///   TicketStatusChanged → Thông báo riêng cho chủ ticket
///   StaffNotification   → Thông báo cho Staff cụ thể
/// </summary>
public class QueueHub : Hub
{
    private readonly ILogger<QueueHub> _logger;

    public QueueHub(ILogger<QueueHub> logger) => _logger = logger;

    // ── CONNECTION LIFECYCLE ───────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ── GROUP MANAGEMENT (Client gọi để subscribe) ─────────────────

    /// <summary>
    /// Client đăng ký nhận event của nhóm.
    /// VD: joinGroup("display"), joinGroup("service-1"), joinGroup("ticket-42")
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {Id} joined group '{Group}'", Context.ConnectionId, groupName);
    }

    /// <summary>Client rời khỏi nhóm (khi đóng trang hoặc navigation).</summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Connection {Id} left group '{Group}'", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Màn hình Display tự đăng ký nhóm "display".
    /// Gọi khi trang /display load xong.
    /// </summary>
    public async Task RegisterAsDisplay()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "display");
        _logger.LogInformation("Display screen registered: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("Registered", new { role = "display", groupName = "display" });
    }

    /// <summary>
    /// Staff đăng ký nhóm theo counterId để nhận thông báo riêng.
    /// </summary>
    [Authorize(Roles = "Staff,Admin")]
    public async Task RegisterAsStaff(int counterId)
    {
        var group = $"staff-{counterId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogInformation("Staff registered at counter {CounterId}: {ConnectionId}", counterId, Context.ConnectionId);
        await Clients.Caller.SendAsync("Registered", new { role = "staff", groupName = group });
    }

    /// <summary>
    /// Customer/App theo dõi 1 ticket cụ thể.
    /// </summary>
    public async Task WatchTicket(int ticketId)
    {
        var group = $"ticket-{ticketId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("Registered", new { role = "watcher", groupName = group });
    }

    /// <summary>Ping để giữ connection sống (optional).</summary>
    public Task Ping() => Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
}

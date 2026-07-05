using Microsoft.AspNetCore.SignalR;
using SQS.API.Hubs;

namespace SQS.API.Services;

// ── PAYLOAD RECORDS ───────────────────────────────────────────────

/// <summary>Event khi Staff gọi số — gửi đến màn hình Display + tất cả client.</summary>
public record TicketCalledPayload(
    string TicketNumber,
    string CounterName,
    string ServiceName,
    int    TicketId
);

/// <summary>Event cập nhật số người chờ theo dịch vụ.</summary>
public record QueueUpdatedPayload(
    int    ServiceId,
    string ServiceName,
    int    WaitingCount,
    string? CurrentCalling  // Số đang được gọi (null nếu chưa có)
);

/// <summary>Event thay đổi trạng thái ticket — gửi riêng cho chủ ticket.</summary>
public record TicketStatusChangedPayload(
    int    TicketId,
    string TicketNumber,
    string NewStatus,
    string? CounterName,
    string? Message         // Tin nhắn hiển thị cho user
);

/// <summary>Thông báo cho Staff tại 1 quầy.</summary>
public record StaffNotificationPayload(
    string Type,            // "new_ticket" | "customer_canceled"
    string Message,
    int    WaitingCount
);

// ── SERVICE ───────────────────────────────────────────────────────

/// <summary>
/// Service trung gian để các Controller/Service khác gọi SignalR.
/// Đảm bảo tách biệt logic business với SignalR infrastructure.
/// </summary>
public class QueueNotificationService
{
    private readonly IHubContext<QueueHub> _hub;
    private readonly ILogger<QueueNotificationService> _logger;

    public QueueNotificationService(
        IHubContext<QueueHub> hub,
        ILogger<QueueNotificationService> logger)
    {
        _hub    = hub;
        _logger = logger;
    }

    // ── BROADCAST: TICKET CALLED ──────────────────────────────────

    /// <summary>
    /// Phát sóng khi Staff gọi số.
    /// → Gửi tới: "display" group + "service-{id}" group + tất cả client.
    /// </summary>
    public async Task BroadcastTicketCalledAsync(TicketCalledPayload payload, int serviceId)
    {
        _logger.LogInformation(
            "SignalR BroadcastTicketCalled: Số {Number} tại {Counter}",
            payload.TicketNumber, payload.CounterName);

        // 1. Màn hình Display (TV tổng)
        await _hub.Clients
            .Group("display")
            .SendAsync("TicketCalled", payload);

        // 2. Mọi người đang xem dịch vụ này
        await _hub.Clients
            .Group($"service-{serviceId}")
            .SendAsync("TicketCalled", payload);

        // 3. Phát toàn cục (cho Kiosk và bất kỳ client nào)
        await _hub.Clients
            .All
            .SendAsync("TicketCalled", payload);
    }

    // ── BROADCAST: QUEUE UPDATED ──────────────────────────────────

    /// <summary>
    /// Cập nhật số người đang chờ sau mỗi thay đổi trạng thái.
    /// → Gửi tới: "service-{id}" group.
    /// </summary>
    public async Task BroadcastQueueUpdatedAsync(QueueUpdatedPayload payload)
    {
        _logger.LogDebug(
            "SignalR QueueUpdated: Service {Id}, Waiting={Count}",
            payload.ServiceId, payload.WaitingCount);

        await _hub.Clients
            .Group($"service-{payload.ServiceId}")
            .SendAsync("QueueUpdated", payload);

        // Cũng gửi cho Display để cập nhật số chờ trên TV
        await _hub.Clients
            .Group("display")
            .SendAsync("QueueUpdated", payload);
    }

    // ── SEND: TICKET STATUS CHANGED ───────────────────────────────

    /// <summary>
    /// Gửi thông báo riêng cho người đang theo dõi ticket cụ thể.
    /// → Gửi tới: "ticket-{id}" group.
    /// </summary>
    public async Task NotifyTicketStatusChangedAsync(TicketStatusChangedPayload payload)
    {
        _logger.LogDebug(
            "SignalR TicketStatusChanged: Ticket {Id} → {Status}",
            payload.TicketId, payload.NewStatus);

        await _hub.Clients
            .Group($"ticket-{payload.TicketId}")
            .SendAsync("TicketStatusChanged", payload);
    }

    // ── SEND: STAFF NOTIFICATION ──────────────────────────────────

    /// <summary>
    /// Thông báo cho Staff tại 1 quầy cụ thể.
    /// VD: có khách hủy số, có ticket mới.
    /// </summary>
    public async Task NotifyStaffAsync(int counterId, StaffNotificationPayload payload)
    {
        await _hub.Clients
            .Group($"staff-{counterId}")
            .SendAsync("StaffNotification", payload);
    }

    // ── CONVENIENCE: BROADCAST ALL QUEUES ─────────────────────────

    /// <summary>Gửi cập nhật hàng đợi cho tất cả services cùng lúc.</summary>
    public async Task BroadcastAllQueuesAsync(IEnumerable<QueueUpdatedPayload> payloads)
    {
        var tasks = payloads.Select(p => BroadcastQueueUpdatedAsync(p));
        await Task.WhenAll(tasks);
    }
}

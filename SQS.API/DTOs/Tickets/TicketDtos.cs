using System.ComponentModel.DataAnnotations;

namespace SQS.API.DTOs.Tickets;

// ── REQUEST DTOs ──────────────────────────────────────────────────

/// <summary>Tạo phiên xếp hàng (lấy số).</summary>
public class CreateTicketRequest
{
    [Required(ErrorMessage = "Vui lòng chọn loại dịch vụ")]
    public int ServiceId { get; set; }

    /// <summary>Tên khách vãng lai — bắt buộc nếu không có JWT token.</summary>
    [StringLength(100)]
    public string? GuestName { get; set; }
}

/// <summary>Staff gọi số tiếp theo tại quầy.</summary>
public class CallNextRequest
{
    [Required(ErrorMessage = "Vui lòng chỉ định quầy")]
    public int CounterId { get; set; }
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────

/// <summary>Response khi tạo ticket thành công.</summary>
public class CreateTicketResponse
{
    public int    TicketId      { get; set; }
    public string TicketNumber  { get; set; } = string.Empty;  // "007"
    public string ServiceName   { get; set; } = string.Empty;
    public string ServiceCode   { get; set; } = string.Empty;
    public int    EstimatedWait { get; set; }  // Số người đang chờ trước
    public DateTime CreatedAt  { get; set; }
}

/// <summary>Trạng thái của 1 ticket.</summary>
public class TicketStatusResponse
{
    public int          TicketId      { get; set; }
    public string       TicketNumber  { get; set; } = string.Empty;
    public string       ServiceName   { get; set; } = string.Empty;
    public string       Status        { get; set; } = string.Empty;
    public int          QueuePosition { get; set; }  // 0 nếu không còn đang chờ
    public string?      CounterName   { get; set; }
    public DateTime?    CalledAt      { get; set; }
    public DateTime     CreatedAt     { get; set; }
}

/// <summary>Response khi Staff gọi số thành công.</summary>
public class CallNextResponse
{
    public int    TicketId      { get; set; }
    public string TicketNumber  { get; set; } = string.Empty;
    public string CustomerName  { get; set; } = string.Empty;
    public string ServiceName   { get; set; } = string.Empty;
    public string CounterName   { get; set; } = string.Empty;
    public int    RemainingWait { get; set; }  // Số người vẫn còn chờ
}

/// <summary>Item trong danh sách hàng đợi.</summary>
public class QueueItem
{
    public int      TicketId     { get; set; }
    public string   TicketNumber { get; set; } = string.Empty;
    public string   CustomerName { get; set; } = string.Empty;
    public int      Position     { get; set; }
    public DateTime CreatedAt    { get; set; }
}

/// <summary>Danh sách hàng đợi của 1 dịch vụ.</summary>
public class QueueStatusResponse
{
    public int         ServiceId      { get; set; }
    public string      ServiceName    { get; set; } = string.Empty;
    public string?     CurrentCalling { get; set; }  // Số đang gọi
    public string?     CounterName    { get; set; }
    public int         WaitingCount   { get; set; }
    public List<QueueItem> WaitingList { get; set; } = new();
}

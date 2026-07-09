using System.ComponentModel.DataAnnotations;

namespace SQS.API.DTOs.Tickets;

// ── REQUEST DTOs ──────────────────────────────────────────────────

/// <summary>Tạo phiên xếp hàng (lấy số — WalkIn).</summary>
public class CreateTicketRequest
{
    [Required(ErrorMessage = "Vui lòng chọn loại dịch vụ")]
    public int ServiceId { get; set; }

    /// <summary>Tên khách vãng lai — bắt buộc nếu không có JWT token.</summary>
    [StringLength(100)]
    public string? GuestName { get; set; }
}

/// <summary>Đặt lịch hẹn trước (Appointment — không lấy số).</summary>
public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Vui lòng chọn loại dịch vụ")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày hẹn")]
    public DateTime AppointmentDate { get; set; }

    /// <summary>Tên người đặt — bắt buộc nếu không đăng nhập.</summary>
    [StringLength(100)]
    public string? GuestName { get; set; }

    /// <summary>Mã số sinh viên.</summary>
    [StringLength(20)]
    public string? StudentId { get; set; }

    /// <summary>Số điện thoại liên hệ.</summary>
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    /// <summary>Ghi chú, lý do đặt hẹn.</summary>
    [StringLength(500)]
    public string? Note { get; set; }
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
    public int     TicketId      { get; set; }
    public string? TicketNumber  { get; set; }           // null nếu là Appointment
    public string  TicketType    { get; set; } = "WalkIn";
    public string  ServiceName   { get; set; } = string.Empty;
    public string  ServiceCode   { get; set; } = string.Empty;
    public int     EstimatedWait { get; set; }           // Số người đang chờ trước
    public DateTime CreatedAt    { get; set; }
}

/// <summary>Response khi đặt hẹn thành công.</summary>
public class AppointmentResponse
{
    public int      TicketId        { get; set; }
    public string   ServiceName     { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string?  StudentId       { get; set; }
    public string?  PhoneNumber     { get; set; }
    public string?  Note            { get; set; }
    public string   Status          { get; set; } = "Waiting";
    public DateTime CreatedAt       { get; set; }
}

/// <summary>Trạng thái của 1 ticket.</summary>
public class TicketStatusResponse
{
    public int          TicketId      { get; set; }
    public string?      TicketNumber  { get; set; }
    public string?      CustomerName  { get; set; }
    public string       TicketType    { get; set; } = "WalkIn";
    public string       ServiceName   { get; set; } = string.Empty;
    public string       Status        { get; set; } = string.Empty;
    public int          QueuePosition { get; set; }  // 0 nếu không còn đang chờ
    public string?      CounterName   { get; set; }
    public DateTime?    CalledAt      { get; set; }
    public DateTime     CreatedAt     { get; set; }
    public DateTime?    AppointmentDate { get; set; }
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


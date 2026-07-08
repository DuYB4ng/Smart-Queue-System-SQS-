using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>
/// Phiên xếp hàng — bảng nghiệp vụ chính của hệ thống SQS.
/// Mỗi row đại diện cho 1 lượt lấy số (WalkIn) hoặc 1 lượt đặt hẹn (Appointment).
/// </summary>
[Table("tickets")]
public class Ticket
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>Loại ticket: WalkIn (trong ngày, lấy số) hoặc Appointment (hẹn trước, không lấy số).</summary>
    [Required]
    [Column("ticket_type")]
    public TicketType TicketType { get; set; } = TicketType.WalkIn;

    /// <summary>Số thứ tự trong ngày, định dạng 3 chữ số: "001", "002"... Null nếu là Appointment.</summary>
    [Column("ticket_number")]
    [StringLength(3)]
    public string? TicketNumber { get; set; }

    // ── Người lấy số ─────────────────────────────────────────────
    /// <summary>null nếu là khách vãng lai (lấy số qua Kiosk không đăng nhập).</summary>
    [Column("id_customer")]
    public int? IdCustomer { get; set; }

    /// <summary>null nếu khách đã đăng nhập qua app.</summary>
    [Column("guest_name")]
    [StringLength(100)]
    public string? GuestName { get; set; }

    // ── Dịch vụ & Quầy ───────────────────────────────────────────
    [Required]
    [Column("id_service")]
    public int IdService { get; set; }

    /// <summary>null cho đến khi Staff gọi số và xác nhận quầy.</summary>
    [Column("id_counter")]
    public int? IdCounter { get; set; }

    [Column("id_staff")]
    public int? IdStaff { get; set; }

    // ── Thời gian ────────────────────────────────────────────────
    [Required]
    [Column("ticket_date")]
    public DateTime TicketDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("called_at")]
    public DateTime? CalledAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // ── Trạng thái ───────────────────────────────────────────────
    [Required]
    [Column("status")]
    public TicketStatus Status { get; set; } = TicketStatus.Waiting;

    // ── Appointment (Đặt hẹn trước) ──────────────────────────────
    /// <summary>Ngày hẹn — chỉ dùng cho Appointment.</summary>
    [Column("appointment_date")]
    public DateTime? AppointmentDate { get; set; }

    /// <summary>Mã số sinh viên.</summary>
    [Column("student_id")]
    [StringLength(20)]
    public string? StudentId { get; set; }

    /// <summary>Số điện thoại liên hệ.</summary>
    [Column("phone")]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    /// <summary>Ghi chú, lý do đặt hẹn.</summary>
    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    // ── Navigation Properties ─────────────────────────────────────
    [ForeignKey("IdCustomer")]
    public Customer? Customer { get; set; }

    [ForeignKey("IdService")]
    public Service Service { get; set; } = null!;

    [ForeignKey("IdCounter")]
    public Counter? Counter { get; set; }

    [ForeignKey("IdStaff")]
    public Staff? Staff { get; set; }
}

public enum TicketType
{
    WalkIn,       // Trong ngày — lấy số xếp hàng
    Appointment   // Đặt hẹn trước — không lấy số
}

public enum TicketStatus
{
    Waiting,
    Calling,
    Completed,
    Canceled
}

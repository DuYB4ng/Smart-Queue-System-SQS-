using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>
/// Phiên xếp hàng — bảng nghiệp vụ chính của hệ thống SQS.
/// Mỗi row đại diện cho 1 lượt lấy số của khách hàng.
/// </summary>
[Table("tickets")]
public class Ticket
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>Số thứ tự trong ngày, định dạng 3 chữ số: "001", "002"...</summary>
    [Required]
    [Column("ticket_number")]
    [StringLength(3, MinimumLength = 3)]
    public string TicketNumber { get; set; } = string.Empty;

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("called_at")]
    public DateTime? CalledAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // ── Trạng thái ───────────────────────────────────────────────
    [Required]
    [Column("status")]
    public TicketStatus Status { get; set; } = TicketStatus.Waiting;

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

public enum TicketStatus
{
    Waiting,
    Calling,
    Completed,
    Canceled
}

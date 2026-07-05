using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>Quan hệ 1-1 với User, đại diện cho nhân viên quầy.</summary>
[Table("staffs")]
public class Staff
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("position")]
    [StringLength(100)]
    public string Position { get; set; } = "Nhân viên";

    /// <summary>Tổng số phiên đã hoàn thành (KPI).</summary>
    [Column("kpi")]
    public int Kpi { get; set; } = 0;

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>Danh mục loại dịch vụ cung cấp tại trường.</summary>
[Table("services")]
public class Service
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã viết tắt, VD: DK, HP, HS, TV, BG.</summary>
    [Required]
    [Column("code")]
    [StringLength(5)]
    public string Code { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CounterService> CounterServices { get; set; } = new List<CounterService>();
    public ICollection<Ticket>         Tickets         { get; set; } = new List<Ticket>();
}

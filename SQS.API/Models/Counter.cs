using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>Quầy phục vụ tại phòng ban.</summary>
[Table("counters")]
public class Counter
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    [StringLength(200)]
    public string? Location { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CounterService> CounterServices { get; set; } = new List<CounterService>();
    public ICollection<Ticket>         Tickets         { get; set; } = new List<Ticket>();
}

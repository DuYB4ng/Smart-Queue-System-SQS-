using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>Quan hệ 1-1 với User, đại diện cho khách hàng.</summary>
[Table("customers")]
public class Customer
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

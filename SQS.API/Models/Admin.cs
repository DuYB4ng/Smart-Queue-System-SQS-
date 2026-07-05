using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>Quan hệ 1-1 với User, đại diện cho quản trị viên.</summary>
[Table("admins")]
public class Admin
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}

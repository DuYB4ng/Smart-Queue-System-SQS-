using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>
/// Bảng gốc chứa thông tin chung của tất cả người dùng.
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [StringLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("address")]
    [StringLength(255)]
    public string? Address { get; set; }

    [Required]
    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Customer;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Customer? Customer { get; set; }
    public Staff?    Staff    { get; set; }
    public Admin?    Admin    { get; set; }
}

public enum UserRole
{
    Customer,
    Staff,
    Admin
}

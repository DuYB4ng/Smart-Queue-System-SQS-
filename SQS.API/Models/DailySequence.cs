using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>
/// Bộ đếm số thứ tự theo ngày (1 row/ngày).
/// Dùng SELECT FOR UPDATE để tránh race condition.
/// </summary>
[Table("daily_sequence")]
public class DailySequence
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>Ngày của chuỗi số (UNIQUE).</summary>
    [Required]
    [Column("seq_date")]
    public DateOnly SeqDate { get; set; }

    /// <summary>Số cuối cùng đã cấp trong ngày (0 = chưa có ai lấy).</summary>
    [Column("last_number")]
    public short LastNumber { get; set; } = 0;
}

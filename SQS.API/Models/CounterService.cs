using System.ComponentModel.DataAnnotations.Schema;

namespace SQS.API.Models;

/// <summary>
/// Bảng nối N-N giữa Counter và Service.
/// Xác định quầy nào phục vụ dịch vụ nào.
/// </summary>
[Table("counter_services")]
public class CounterService
{
    [Column("counter_id")]
    public int CounterId { get; set; }

    [Column("service_id")]
    public int ServiceId { get; set; }

    // Navigation
    [ForeignKey("CounterId")]
    public Counter Counter { get; set; } = null!;

    [ForeignKey("ServiceId")]
    public Service Service { get; set; } = null!;
}

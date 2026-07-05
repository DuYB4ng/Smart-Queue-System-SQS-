using Microsoft.EntityFrameworkCore;
using SQS.API.Data;

namespace SQS.API.Services;

/// <summary>
/// Tạo số thứ tự ticket theo ngày, đảm bảo thread-safe.
/// Dùng DB transaction + raw SQL để gọi stored procedure.
/// Format: "001", "002", ..., "999" — reset mỗi ngày.
/// </summary>
public class SequenceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SequenceService> _logger;

    public SequenceService(AppDbContext db, ILogger<SequenceService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <summary>
    /// Lấy số thứ tự tiếp theo cho ngày hôm nay.
    /// Dùng DB transaction để tránh race condition khi nhiều request đồng thời.
    /// </summary>
    /// <returns>Chuỗi 3 chữ số: "001", "002", ...</returns>
    /// <exception cref="InvalidOperationException">Hết số trong ngày (> 999).</exception>
    public async Task<string> GetNextNumberAsync(DateOnly date)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            // Upsert: tạo mới nếu chưa có record cho ngày này
            var sequence = await _db.DailySequences
                .FirstOrDefaultAsync(s => s.SeqDate == date);

            if (sequence is null)
            {
                sequence = new Models.DailySequence
                {
                    SeqDate    = date,
                    LastNumber = 0
                };
                _db.DailySequences.Add(sequence);
                await _db.SaveChangesAsync();
            }

            // Tăng số thứ tự
            var nextNumber = sequence.LastNumber + 1;

            if (nextNumber > 999)
                throw new InvalidOperationException("Đã hết số thứ tự trong ngày. Vui lòng liên hệ quản trị viên.");

            sequence.LastNumber = (short)nextNumber;
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            var ticketNumber = nextNumber.ToString("D3");  // "001", "042", "999"
            _logger.LogInformation("Cấp số {Number} cho ngày {Date}", ticketNumber, date);

            return ticketNumber;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

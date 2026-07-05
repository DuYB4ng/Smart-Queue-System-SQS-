using Microsoft.EntityFrameworkCore;
using SQS.API.Models;

namespace SQS.API.Data;

/// <summary>
/// EF Core DbContext cho Smart Queue System.
/// Cấu hình toàn bộ quan hệ, constraint và seed data qua Fluent API.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ──────────────────────────────────────────────────────
    public DbSet<User>          Users          { get; set; }
    public DbSet<Customer>      Customers      { get; set; }
    public DbSet<Staff>         Staffs         { get; set; }
    public DbSet<Admin>         Admins         { get; set; }
    public DbSet<Service>       Services       { get; set; }
    public DbSet<Counter>       Counters       { get; set; }
    public DbSet<CounterService> CounterServices { get; set; }
    public DbSet<Ticket>        Tickets        { get; set; }
    public DbSet<DailySequence> DailySequences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── users ──────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();

            e.Property(u => u.Role)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.Property(u => u.CreatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(u => u.UpdatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ── customers (1-1 với User) ───────────────────────────────
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(c => c.UserId);

            e.HasOne(c => c.User)
             .WithOne(u => u.Customer)
             .HasForeignKey<Customer>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── staffs (1-1 với User) ──────────────────────────────────
        modelBuilder.Entity<Staff>(e =>
        {
            e.ToTable("staffs");
            e.HasKey(s => s.UserId);

            e.HasOne(s => s.User)
             .WithOne(u => u.Staff)
             .HasForeignKey<Staff>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(s => s.Kpi).HasDefaultValue(0);
            e.Property(s => s.Position).HasDefaultValue("Nhân viên");
        });

        // ── admins (1-1 với User) ──────────────────────────────────
        modelBuilder.Entity<Admin>(e =>
        {
            e.ToTable("admins");
            e.HasKey(a => a.UserId);

            e.HasOne(a => a.User)
             .WithOne(u => u.Admin)
             .HasForeignKey<Admin>(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── services ───────────────────────────────────────────────
        modelBuilder.Entity<Service>(e =>
        {
            e.ToTable("services");
            e.HasIndex(s => s.Code).IsUnique();

            e.Property(s => s.CreatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ── counters ───────────────────────────────────────────────
        modelBuilder.Entity<Counter>(e =>
        {
            e.ToTable("counters");

            e.Property(c => c.CreatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ── counter_services (N-N, composite PK) ───────────────────
        modelBuilder.Entity<CounterService>(e =>
        {
            e.ToTable("counter_services");
            e.HasKey(cs => new { cs.CounterId, cs.ServiceId });

            e.HasOne(cs => cs.Counter)
             .WithMany(c => c.CounterServices)
             .HasForeignKey(cs => cs.CounterId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cs => cs.Service)
             .WithMany(s => s.CounterServices)
             .HasForeignKey(cs => cs.ServiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── daily_sequence ─────────────────────────────────────────
        modelBuilder.Entity<DailySequence>(e =>
        {
            e.ToTable("daily_sequence");
            e.HasIndex(d => d.SeqDate).IsUnique();
            e.Property(d => d.LastNumber).HasDefaultValue((short)0);
        });

        // ── tickets ────────────────────────────────────────────────
        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");

            // Enum → string trong DB
            e.Property(t => t.Status)
             .HasConversion<string>()
             .HasMaxLength(20)
             .HasDefaultValue(TicketStatus.Waiting);

            e.Property(t => t.CreatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // FK: Customer (nullable — khách vãng lai)
            e.HasOne(t => t.Customer)
             .WithMany(c => c.Tickets)
             .HasForeignKey(t => t.IdCustomer)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            // FK: Service
            e.HasOne(t => t.Service)
             .WithMany(s => s.Tickets)
             .HasForeignKey(t => t.IdService)
             .OnDelete(DeleteBehavior.Restrict);

            // FK: Counter (nullable)
            e.HasOne(t => t.Counter)
             .WithMany(c => c.Tickets)
             .HasForeignKey(t => t.IdCounter)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);

            // FK: Staff (nullable)
            e.HasOne(t => t.Staff)
             .WithMany(s => s.Tickets)
             .HasForeignKey(t => t.IdStaff)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);

            // Indexes tối ưu hiệu năng
            e.HasIndex(t => new { t.IdService, t.Status, t.CreatedAt })
             .HasDatabaseName("idx_tickets_service_status_created");

            e.HasIndex(t => new { t.IdCustomer, t.TicketDate })
             .HasDatabaseName("idx_tickets_customer_date");

            e.HasIndex(t => new { t.TicketDate, t.Status })
             .HasDatabaseName("idx_tickets_date_status");

            e.HasIndex(t => new { t.Status, t.IdCounter })
             .HasDatabaseName("idx_tickets_status_counter");
        });
    }

    /// <summary>
    /// Override SaveChangesAsync để tự động cập nhật UpdatedAt cho User.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
            entry.Entity.UpdatedAt = DateTime.UtcNow;

        return base.SaveChangesAsync(ct);
    }
}

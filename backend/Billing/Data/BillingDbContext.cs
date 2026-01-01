using Microsoft.EntityFrameworkCore;
using Billing.Entities;

namespace Billing.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    public DbSet<BillingRecord> BillingRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BillingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.BillingDate).HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => e.OrderId).IsUnique();
        });
    }
}
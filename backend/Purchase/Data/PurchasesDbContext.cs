using Microsoft.EntityFrameworkCore;
using Purchase.Entities;

namespace Purchase.Data;

public class PurchasesDbContext : DbContext
{
    public PurchasesDbContext(DbContextOptions<PurchasesDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.Purchase> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entities.Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        // Seed data
        modelBuilder.Entity<Entities.Purchase>().HasData(
            new Entities.Purchase { Id = 1, BuyerId = 1001, OfferId = 1, PurchaseDate = DateTime.UtcNow, Amount = 25000.00m, Status = "Completed" },
            new Entities.Purchase { Id = 2, BuyerId = 1002, OfferId = 2, PurchaseDate = DateTime.UtcNow, Amount = 32000.00m, Status = "Pending" },
            new Entities.Purchase { Id = 3, BuyerId = 1001, OfferId = 3, PurchaseDate = DateTime.UtcNow, Amount = 18500.00m, Status = "Processing" }
        );
    }
}

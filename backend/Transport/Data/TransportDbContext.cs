using Microsoft.EntityFrameworkCore;
using Transport.Entities;

namespace Transport.Data;

public class TransportDbContext : DbContext
{
    public TransportDbContext(DbContextOptions<TransportDbContext> options) : base(options)
    {
    }

    public DbSet<TransportEntity> Transports { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TransportEntity>(entity =>
        {
            entity.HasKey(e => e.TransportId);
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
            entity.Property(e => e.ActualCost).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Create indexes on important fields for better performance
            entity.HasIndex(e => e.CarrierId);
            entity.HasIndex(e => e.PurchaseId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduleDate);
            entity.HasIndex(e => e.PickupCity);
            entity.HasIndex(e => e.DeliveryCity);
        });

        // Configure OutboxMessage entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");
            // Index for efficient polling of unprocessed messages
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_outbox_unprocessed")
                .HasFilter("\"processed_at\" IS NULL");
        });

        // Seed data with detailed address information
        modelBuilder.Entity<TransportEntity>().HasData(
            new TransportEntity
            {
                TransportId = 1,
                CarrierId = 101,
                PurchaseId = 1001,
                PickupStreet = "123 Main St, Warehouse A",
                PickupCity = "New York",
                PickupStateCode = "NY",
                PickupCountry = "USA",
                PickupZipCode = "10001",
                DeliveryStreet = "456 Oak Ave",
                DeliveryCity = "Los Angeles",
                DeliveryStateCode = "CA",
                DeliveryCountry = "USA",
                DeliveryZipCode = "90001",
                ScheduleDate = DateTime.UtcNow.AddDays(1),
                Status = "Pending",
                EstimatedCost = 150.00m,
                Notes = "Handle with care - fragile items"
            },
            new TransportEntity
            {
                TransportId = 2,
                CarrierId = 102,
                PurchaseId = 1002,
                PickupStreet = "789 Industrial Blvd, Distribution Center B",
                PickupCity = "Chicago",
                PickupStateCode = "IL",
                PickupCountry = "USA",
                PickupZipCode = "60601",
                DeliveryStreet = "321 Commerce St, Retail Store",
                DeliveryCity = "Houston",
                DeliveryStateCode = "TX",
                DeliveryCountry = "USA",
                DeliveryZipCode = "77001",
                ScheduleDate = DateTime.UtcNow.AddDays(2),
                Status = "InTransit",
                EstimatedCost = 220.50m,
                ActualCost = 215.75m,
                Notes = "Express delivery required"
            },
            new TransportEntity
            {
                TransportId = 3,
                CarrierId = 103,
                PurchaseId = 1003,
                PickupStreet = "555 Factory Rd, Manufacturing Plant",
                PickupCity = "Detroit",
                PickupStateCode = "MI",
                PickupCountry = "USA",
                PickupZipCode = "48201",
                DeliveryStreet = "888 Business Park Dr, Corporate Office",
                DeliveryCity = "Seattle",
                DeliveryStateCode = "WA",
                DeliveryCountry = "USA",
                DeliveryZipCode = "98101",
                ScheduleDate = DateTime.UtcNow.AddDays(-1),
                Status = "Delivered",
                EstimatedCost = 180.00m,
                ActualCost = 175.25m,
                Notes = "Delivery completed successfully"
            }
        );
    }
}
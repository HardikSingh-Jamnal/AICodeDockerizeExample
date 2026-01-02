using Microsoft.EntityFrameworkCore;
using Offers.Domain.Entities;
using Offers.Domain.ValueObjects;

namespace Offers.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the Offers service.
/// Configures the Offers and OutboxMessages tables with proper indexing and JSONB support.
/// </summary>
public class OffersDbContext : DbContext
{
    public OffersDbContext(DbContextOptions<OffersDbContext> options) : base(options)
    {
    }

    public DbSet<Offer> Offers { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Offer entity
        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("offers");

            entity.HasKey(e => e.OfferId);
            entity.Property(e => e.OfferId)
                .HasColumnName("offer_id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.SellerId)
                .HasColumnName("seller_id")
                .IsRequired();

            entity.Property(e => e.Vin)
                .HasColumnName("vin")
                .HasMaxLength(17)
                .IsRequired();

            entity.Property(e => e.Make)
                .HasColumnName("make")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Model)
                .HasColumnName("model")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Year)
                .HasColumnName("year")
                .IsRequired();

            entity.Property(e => e.OfferAmount)
                .HasColumnName("offer_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            // Configure Location as JSONB
            entity.Property(e => e.Location)
                .HasColumnName("location")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Location>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Location()
                )
                .IsRequired();

            // Configure Condition as JSONB
            entity.Property(e => e.Condition)
                .HasColumnName("condition")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Condition>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Condition()
                )
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now() at time zone 'utc'");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            // Create indexes for efficient querying
            entity.HasIndex(e => e.SellerId).HasDatabaseName("idx_offers_seller_id");
            entity.HasIndex(e => e.Vin).HasDatabaseName("idx_offers_vin");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_offers_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_offers_created_at");

            // Unique constraint: VIN must be unique per seller
            entity.HasIndex(e => new { e.SellerId, e.Vin })
                .IsUnique()
                .HasDatabaseName("uq_seller_vin");
        });

        // Configure OutboxMessage entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now() at time zone 'utc'");

            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");

            entity.Property(e => e.RetryCount)
                .HasColumnName("retry_count")
                .HasDefaultValue(0);

            entity.Property(e => e.LastError)
                .HasColumnName("last_error");

            // Index for efficient polling of unprocessed messages
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_outbox_unprocessed")
                .HasFilter("processed_at IS NULL");
        });
    }
}

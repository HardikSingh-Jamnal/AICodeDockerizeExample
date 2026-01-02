using Offers.Domain.ValueObjects;

namespace Offers.Domain.Events;

/// <summary>
/// Immutable domain event representing an update to an existing offer.
/// Contains a fully denormalized snapshot of the updated offer data.
/// </summary>
public record OfferUpdatedEvent
{
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal OfferAmount { get; init; }
    public Location Location { get; init; } = new();
    public Condition Condition { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "OfferUpdated";
}
